namespace BlazorWebGame.Server.Services;

/// <summary>
/// 服务端游戏循环服务，使用 PeriodicTimer 替代客户端的 Timer
/// </summary>
public class GameLoopService : BackgroundService
{
    private readonly GameEngineService _gameEngine;
    private readonly ServerProductionService _productionService;
    private readonly ILogger<GameLoopService> _logger;
    private readonly TimeSpan _tickInterval = TimeSpan.FromMilliseconds(500); // 500ms 间隔

    public GameLoopService(
        GameEngineService gameEngine,
        ServerProductionService productionService,
        ILogger<GameLoopService> logger)
    {
        _gameEngine = gameEngine;
        _productionService = productionService;
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

                // 处理游戏逻辑 - 这已经包含了SignalR实时更新
                await _gameEngine.ProcessBattleTickAsync(deltaTime);
                
                // 处理生产系统逻辑
                await _productionService.UpdateGatheringStatesAsync(deltaTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in game loop");
            }
        }
    }
}