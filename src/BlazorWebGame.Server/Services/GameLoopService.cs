using BlazorWebGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 服务端游戏循环服务，使用 PeriodicTimer 替代客户端的 Timer
/// </summary>
public class GameLoopService : BackgroundService
{
    private readonly GameEngineService _gameEngine;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameLoopService> _logger;
    private readonly TimeSpan _tickInterval = TimeSpan.FromMilliseconds(500); // 500ms 间隔

    public GameLoopService(
        GameEngineService gameEngine, 
        IHubContext<GameHub> hubContext,
        ILogger<GameLoopService> logger)
    {
        _gameEngine = gameEngine;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Game loop service started with tick interval: {Interval}ms", 
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

                // 处理游戏逻辑
                _gameEngine.ProcessBattleTick(deltaTime);

                // 通知客户端更新
                await NotifyClients();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in game loop");
            }
        }
    }

    private async Task NotifyClients()
    {
        // 获取所有活跃战斗并通知相关客户端
        var updates = _gameEngine.GetAllBattleUpdates();
        
        foreach (var update in updates)
        {
            await _hubContext.Clients
                .Group($"battle-{update.BattleId}")
                .SendAsync("BattleUpdate", update);
        }
    }
}