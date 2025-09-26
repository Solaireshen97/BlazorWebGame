using BlazorWebGame.Server.Configuration;
using Microsoft.Extensions.Options;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 服务端游戏循环服务，使用 PeriodicTimer 替代客户端的 Timer
/// </summary>
public class GameLoopService : BackgroundService
{
    private readonly GameEngineService _gameEngine;
    private readonly ServerProductionService _productionService;
    private readonly ServerGameStateService _gameStateService;
    private readonly ILogger<GameLoopService> _logger;
    private readonly GameServerOptions _options;
    private readonly TimeSpan _tickInterval;

    public GameLoopService(
        GameEngineService gameEngine,
        ServerProductionService productionService,
        ServerGameStateService gameStateService,
        ILogger<GameLoopService> logger,
        IOptions<GameServerOptions> options)
    {
        _gameEngine = gameEngine;
        _productionService = productionService;
        _gameStateService = gameStateService;
        _logger = logger;
        _options = options.Value;
        _tickInterval = TimeSpan.FromMilliseconds(_options.GameLoopIntervalMs);
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

                // 处理战斗逻辑
                _gameEngine.ProcessBattleTick(deltaTime);
                
                // 处理采集系统逻辑
                await _productionService.UpdateGatheringStatesAsync(deltaTime);
                
                // 处理制作系统逻辑
                await _productionService.ProcessCraftingTickAsync(deltaTime);
                
                // 处理游戏状态更新（角色恢复、自动化等）
                await _gameStateService.ProcessGameTickAsync(deltaTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in game loop");
            }
        }
    }
}