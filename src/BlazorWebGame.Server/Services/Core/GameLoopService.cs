using BlazorWebGame.Server.Configuration;
using BlazorWebGame.Server.Services.Profession;
using BlazorWebGame.Server.Services.GameSystem;
using BlazorWebGame.Shared.Events;
using Microsoft.Extensions.Options;

namespace BlazorWebGame.Server.Services.Core;

/// <summary>
/// 服务端游戏循环服务 - 重构为使用统一事件队列系统
/// 实现帧同步处理和事件驱动架构
/// </summary>
public class GameLoopService : BackgroundService
{
    private readonly GameEngineService _gameEngine;
    private readonly ServerProductionService _productionService;
    private readonly UnifiedEventService _eventService;
    private readonly ILogger<GameLoopService> _logger;
    private readonly GameServerOptions _options;
    private readonly TimeSpan _tickInterval;

    // 性能监控
    private long _totalFrames;
    private DateTime _lastStatsReport = DateTime.UtcNow;

    public GameLoopService(
        GameEngineService gameEngine,
        ServerProductionService productionService,
        UnifiedEventService eventService,
        ILogger<GameLoopService> logger,
        IOptions<GameServerOptions> options)
    {
        _gameEngine = gameEngine;
        _productionService = productionService;
        _eventService = eventService;
        _logger = logger;
        _options = options.Value;
        _tickInterval = TimeSpan.FromMilliseconds(_options.GameLoopIntervalMs);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Game loop service started with unified event system, tick interval: {Interval}ms", 
            _tickInterval.TotalMilliseconds);

        var timer = new PeriodicTimer(_tickInterval);
        var lastTick = DateTime.UtcNow;

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var now = DateTime.UtcNow;
                var deltaTime = (now - lastTick).TotalSeconds;
                lastTick = now;

                await ProcessGameTick(deltaTime);
                
                _totalFrames++;
                
                // 定期报告统计信息
                if (now - _lastStatsReport > TimeSpan.FromMinutes(1))
                {
                    ReportStatistics();
                    _lastStatsReport = now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in game loop tick");
                // 发送系统错误事件
                _eventService.EnqueueSystemEvent(GameEventTypes.SYSTEM_ERROR, EventPriority.Telemetry);
            }
        }
    }

    /// <summary>
    /// 处理单个游戏tick - 事件驱动版本
    /// </summary>
    private async Task ProcessGameTick(double deltaTime)
    {
        // 发送系统tick事件
        _eventService.EnqueueSystemEvent(GameEventTypes.SYSTEM_TICK, EventPriority.Analytics);

        // 处理游戏逻辑 - 现在是事件收集而非直接处理
        await _gameEngine.ProcessBattleTickAsync(deltaTime);
        
        // 处理生产系统逻辑
        await _productionService.UpdateGatheringStatesAsync(deltaTime);
        
        // 事件处理由EventDispatcher自动进行，无需在这里等待
        // 这实现了真正的异步事件处理和帧率稳定
    }

    /// <summary>
    /// 报告性能统计信息
    /// </summary>
    private void ReportStatistics()
    {
        try
        {
            var eventStats = _eventService.GetStatistics();
            
            _logger.LogInformation(
                "Game Loop Stats - Total Frames: {TotalFrames}, " +
                "Event System: {EventStats}",
                _totalFrames, eventStats);

            // 检查性能警告
            var queueStats = eventStats.QueueStatistics;
            if (queueStats.DropRate > 0.01) // 1%的丢弃率警告
            {
                _logger.LogWarning("High event drop rate detected: {DropRate:P2}, " +
                    "consider tuning queue sizes or reducing event load", queueStats.DropRate);
            }

            if (queueStats.TotalQueueDepth > 1000)
            {
                _logger.LogWarning("High queue depth detected: {TotalDepth}, " +
                    "system may be under heavy load", queueStats.TotalQueueDepth);
            }

            var dispatcherStats = eventStats.DispatcherStatistics;
            if (dispatcherStats.AverageFrameTime > _options.GameLoopIntervalMs * 0.8)
            {
                _logger.LogWarning("Frame processing time approaching limit: {FrameTime:F2}ms / {Limit}ms",
                    dispatcherStats.AverageFrameTime, _options.GameLoopIntervalMs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting game loop statistics");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Game loop service stopping...");
        
        // 最后的统计报告
        ReportStatistics();
        
        await base.StopAsync(cancellationToken);
        
        _logger.LogInformation("Game loop service stopped. Total frames processed: {TotalFrames}", _totalFrames);
    }
}