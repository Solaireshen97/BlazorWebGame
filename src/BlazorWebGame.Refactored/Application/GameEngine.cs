using BlazorWebGame.Refactored.Application.Systems;
using BlazorWebGame.Refactored.Domain.Events;
using BlazorWebGame.Refactored.Infrastructure.Events.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace BlazorWebGame.Refactored.Application;

/// <summary>
/// 游戏引擎 - 管理游戏循环和系统协调 (简化版本，适用于Blazor WebAssembly)
/// </summary>
public sealed class GameEngine : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IEnumerable<IGameSystem> _systems;
    private readonly ILogger<GameEngine> _logger;
    private readonly GameEngineOptions _options;
    
    private readonly PeriodicTimer _timer;
    private readonly Stopwatch _stopwatch = new();
    private long _tickNumber;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task? _gameLoopTask;
    
    public GameEngine(
        IEventBus eventBus,
        IEnumerable<IGameSystem> systems,
        ILogger<GameEngine> logger,
        IOptions<GameEngineOptions> options)
    {
        _eventBus = eventBus;
        _systems = systems.OrderBy(s => s.Priority).ToList();
        _logger = logger;
        _options = options.Value;
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.TickIntervalMs));
    }
    
    public async Task StartAsync()
    {
        _logger.LogInformation("Game Engine starting with {SystemCount} systems", _systems.Count());
        
        // 初始化所有系统
        foreach (var system in _systems)
        {
            await system.InitializeAsync(_cancellationTokenSource.Token);
            _logger.LogDebug("Initialized system: {SystemName}", system.Name);
        }
        
        // 发布游戏初始化事件
        await _eventBus.PublishAsync(new GameInitializedEvent(), _cancellationTokenSource.Token);
        
        // 启动游戏循环
        _gameLoopTask = RunGameLoopAsync(_cancellationTokenSource.Token);
        
        _logger.LogInformation("Game Engine started successfully");
    }
    
    private async Task RunGameLoopAsync(CancellationToken stoppingToken)
    {
        _stopwatch.Start();
        var lastTickTime = _stopwatch.Elapsed;
        
        // 游戏主循环
        try
        {
            while (!stoppingToken.IsCancellationRequested && await _timer.WaitForNextTickAsync(stoppingToken))
            {
                var currentTime = _stopwatch.Elapsed;
                var deltaTime = (currentTime - lastTickTime).TotalSeconds;
                lastTickTime = currentTime;
                
                await ProcessGameTickAsync(deltaTime, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game loop encountered an error");
        }
        
        _logger.LogInformation("Game Engine loop stopped");
    }
    
    private async Task ProcessGameTickAsync(double deltaTime, CancellationToken cancellationToken)
    {
        _tickNumber++;
        
        var tickEvent = new GameTickEvent
        {
            DeltaTime = deltaTime,
            TickNumber = _tickNumber
        };
        
        // 发布tick事件
        await _eventBus.PublishAsync(tickEvent, cancellationToken);
        
        // 并行处理所有系统
        var tasks = _systems
            .Where(s => s.ShouldProcess(deltaTime))
            .Select(s => ProcessSystemAsync(s, deltaTime, cancellationToken));
        
        await Task.WhenAll(tasks);
    }
    
    private async Task ProcessSystemAsync(IGameSystem system, double deltaTime, CancellationToken cancellationToken)
    {
        try
        {
            using var activity = System.Diagnostics.Activity.StartActivity($"GameSystem.{system.Name}");
            await system.ProcessAsync(deltaTime, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing system {SystemName}", system.Name);
        }
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping Game Engine...");
        
        _cancellationTokenSource.Cancel();
        
        if (_gameLoopTask != null)
        {
            await _gameLoopTask;
        }
    }

    public void Dispose()
    {
        if (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            StopAsync().Wait(TimeSpan.FromSeconds(5));
        }
        
        _timer?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}

/// <summary>
/// 游戏引擎配置
/// </summary>
public class GameEngineOptions
{
    public int TickIntervalMs { get; set; } = 100;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableDiagnostics { get; set; } = false;
}