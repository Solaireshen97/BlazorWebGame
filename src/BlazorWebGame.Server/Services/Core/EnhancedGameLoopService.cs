using BlazorWebGame.Shared.Events;
using BlazorWebGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using BlazorWebGame.Server.Services.Battle;
using BlazorWebGame.Server.Services.Profession;
using BlazorWebGame.Server.Services.GameSystem;

namespace BlazorWebGame.Server.Services.Core;

/// <summary>
/// 增强的游戏循环服务，集成统一事件队列系统
/// 提供高性能的事件驱动游戏逻辑处理
/// </summary>
public class EnhancedGameLoopService : BackgroundService
{
    private readonly UnifiedEventService _eventService;
    private readonly EventDrivenBattleEngine _battleEngine;
    private readonly EventDrivenProfessionService _professionService;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<EnhancedGameLoopService> _logger;
    
    // 游戏循环配置
    private readonly TimeSpan _tickInterval = TimeSpan.FromMilliseconds(16.67); // ~60 FPS
    private readonly TimeSpan _slowTickInterval = TimeSpan.FromMilliseconds(100); // ~10 FPS for slow updates
    
    // 性能监控
    private DateTime _lastSlowTick = DateTime.UtcNow;
    private long _totalTicks = 0;
    private long _totalSlowTicks = 0;
    private readonly object _statsLock = new();

    public EnhancedGameLoopService(
        UnifiedEventService eventService,
        EventDrivenBattleEngine battleEngine,
        EventDrivenProfessionService professionService,
        IHubContext<GameHub> hubContext,
        ILogger<EnhancedGameLoopService> logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _battleEngine = battleEngine ?? throw new ArgumentNullException(nameof(battleEngine));
        _professionService = professionService ?? throw new ArgumentNullException(nameof(professionService));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 主游戏循环执行方法
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Enhanced game loop service started with unified event system");
        _logger.LogInformation("Tick interval: {TickInterval}ms, Slow tick interval: {SlowTickInterval}ms", 
            _tickInterval.TotalMilliseconds, _slowTickInterval.TotalMilliseconds);

        var timer = new PeriodicTimer(_tickInterval);
        var lastTick = DateTime.UtcNow;

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var frameStartTime = DateTime.UtcNow;
            
            try
            {
                // 计算delta time
                var deltaTime = (frameStartTime - lastTick).TotalSeconds;
                lastTick = frameStartTime;

                // 快速更新 (60 FPS)
                await ProcessFastTick(deltaTime);

                // 慢速更新 (10 FPS)
                if (frameStartTime - _lastSlowTick >= _slowTickInterval)
                {
                    await ProcessSlowTick(frameStartTime - _lastSlowTick);
                    _lastSlowTick = frameStartTime;
                    
                    lock (_statsLock)
                    {
                        _totalSlowTicks++;
                    }
                }

                // 更新统计
                lock (_statsLock)
                {
                    _totalTicks++;
                }

                // 监控帧时间
                var frameTime = (DateTime.UtcNow - frameStartTime).TotalMilliseconds;
                if (frameTime > _tickInterval.TotalMilliseconds * 0.8)
                {
                    _logger.LogWarning("Frame processing took {FrameTime}ms, approaching frame budget limit", frameTime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in enhanced game loop tick");
            }
        }

        _logger.LogInformation("Enhanced game loop service stopped");
    }

    /// <summary>
    /// 快速更新处理 - 60 FPS
    /// 处理需要高频更新的逻辑：战斗、移动、实时交互
    /// </summary>
    private async Task ProcessFastTick(double deltaTime)
    {
        var tickStartTime = DateTime.UtcNow;

        // 1. 处理战斗逻辑
        await _battleEngine.ProcessBattleFrameAsync(deltaTime);

        // 2. 生成系统tick事件，让事件队列处理其他逻辑
        _eventService.EnqueueEvent(GameEventTypes.SYSTEM_TICK, EventPriority.Gameplay, 0, 0);

        // 3. 让事件队列的分发器处理累积的事件
        // EventDispatcher 会自动处理帧同步，这里不需要额外操作

        var tickTime = (DateTime.UtcNow - tickStartTime).TotalMilliseconds;
        if (tickTime > 5.0) // 如果快速tick超过5ms，记录警告
        {
            _logger.LogDebug("Fast tick took {TickTime}ms", tickTime);
        }
    }

    /// <summary>
    /// 慢速更新处理 - 10 FPS
    /// 处理不需要高频更新的逻辑：职业活动进度、数据持久化、清理
    /// </summary>
    private async Task ProcessSlowTick(TimeSpan elapsedTime)
    {
        var slowTickStartTime = DateTime.UtcNow;

        try
        {
            // 1. 更新职业活动状态
            await _professionService.UpdateProfessionActivitiesAsync(elapsedTime.TotalSeconds);

            // 2. 广播系统状态更新给客户端
            await BroadcastSystemStatus();

            // 3. 性能监控和统计
            await PerformMaintenanceTasks();

            var slowTickTime = (DateTime.UtcNow - slowTickStartTime).TotalMilliseconds;
            if (slowTickTime > 50.0) // 如果慢速tick超过50ms，记录警告
            {
                _logger.LogWarning("Slow tick took {SlowTickTime}ms", slowTickTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in slow tick processing");
        }
    }

    /// <summary>
    /// 向客户端广播系统状态
    /// </summary>
    private async Task BroadcastSystemStatus()
    {
        try
        {
            // 获取系统统计信息
            var eventStats = _eventService.GetStatistics();
            var battleStats = _battleEngine.GetStatistics();
            var professionStats = _professionService.GetStats();

            // 创建系统状态快照
            var systemStatus = new SystemStatusDto
            {
                Timestamp = DateTime.UtcNow,
                TotalActiveBattles = battleStats.ActiveBattles,
                TotalActiveActivities = professionStats.ActiveActivities,
                EventQueueDepth = eventStats.QueueStatistics.TotalQueueDepth,
                CurrentFrame = eventStats.CurrentFrame,
                EventProcessingRate = CalculateEventProcessingRate(eventStats)
            };

            // 只向需要系统状态的客户端发送（如管理员面板）
            await _hubContext.Clients.Group("admin").SendAsync("SystemStatusUpdate", systemStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting system status");
        }
    }

    /// <summary>
    /// 执行维护任务
    /// </summary>
    private async Task PerformMaintenanceTasks()
    {
        try
        {
            // 每10秒记录一次性能统计
            if (_totalSlowTicks % 100 == 0) // 100 * 100ms = 10秒
            {
                await LogPerformanceStatistics();
            }

            // 每分钟清理过期数据
            if (_totalSlowTicks % 600 == 0) // 600 * 100ms = 60秒
            {
                await PerformDataCleanup();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in maintenance tasks");
        }
    }

    /// <summary>
    /// 记录性能统计
    /// </summary>
    private async Task LogPerformanceStatistics()
    {
        var eventStats = _eventService.GetStatistics();
        var battleStats = _battleEngine.GetStatistics();
        var professionStats = _professionService.GetStats();

        _logger.LogInformation("Performance Statistics - " +
            "Ticks: {FastTicks}/{SlowTicks}, " +
            "Events: {QueueStats}, " +
            "Battles: {BattleStats}, " +
            "Professions: {ProfessionStats}",
            _totalTicks, _totalSlowTicks,
            eventStats.QueueStatistics.ToString(),
            $"Active: {battleStats.ActiveBattles}, Total Events: {battleStats.TotalEventsProcessed}",
            $"Active: {professionStats.ActiveActivities}, Completion Rate: {professionStats.CompletionRate:P2}");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 执行数据清理
    /// </summary>
    private async Task PerformDataCleanup()
    {
        _logger.LogDebug("Performing periodic data cleanup");
        
        // 这里可以添加具体的清理逻辑：
        // - 清理已完成的战斗数据
        // - 清理过期的职业活动状态
        // - 清理事件队列中的过期事件
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 计算事件处理速率
    /// </summary>
    private double CalculateEventProcessingRate(UnifiedEventSystemStats stats)
    {
        var queueStats = stats.QueueStatistics;
        if (queueStats.TotalDequeued == 0) return 0.0;

        // 简单的处理速率计算：每秒处理的事件数
        return queueStats.TotalDequeued / Math.Max(1.0, _totalTicks * _tickInterval.TotalSeconds);
    }

    /// <summary>
    /// 获取游戏循环统计信息
    /// </summary>
    public GameLoopStats GetStats()
    {
        lock (_statsLock)
        {
            return new GameLoopStats
            {
                TotalFastTicks = _totalTicks,
                TotalSlowTicks = _totalSlowTicks,
                UptimeSeconds = (DateTime.UtcNow - _lastSlowTick).TotalSeconds,
                AverageTickRate = _totalTicks > 0 ? _totalTicks / Math.Max(1.0, (DateTime.UtcNow - _lastSlowTick).TotalSeconds) : 0.0
            };
        }
    }
}

/// <summary>
/// 系统状态DTO
/// </summary>
public class SystemStatusDto
{
    public DateTime Timestamp { get; set; }
    public int TotalActiveBattles { get; set; }
    public int TotalActiveActivities { get; set; }
    public int EventQueueDepth { get; set; }
    public long CurrentFrame { get; set; }
    public double EventProcessingRate { get; set; }
}

/// <summary>
/// 游戏循环统计信息
/// </summary>
public struct GameLoopStats
{
    public long TotalFastTicks;
    public long TotalSlowTicks;
    public double UptimeSeconds;
    public double AverageTickRate;

    public override string ToString()
    {
        return $"Fast Ticks: {TotalFastTicks}, Slow Ticks: {TotalSlowTicks}, " +
               $"Uptime: {UptimeSeconds:F1}s, Avg Tick Rate: {AverageTickRate:F1}/s";
    }
}