using Microsoft.Extensions.Diagnostics.HealthChecks;
using BlazorWebGame.Server.Configuration;
using Microsoft.Extensions.Options;
using BlazorWebGame.Server.Services.Core;

namespace BlazorWebGame.Server.Services.System;

/// <summary>
/// 游戏服务器健康检查服务
/// </summary>
public class GameHealthCheckService : IHealthCheck
{
    private readonly GameEngineService _gameEngine;
    private readonly PerformanceMonitoringService _performanceService;
    private readonly ILogger<GameHealthCheckService> _logger;
    private readonly GameServerOptions _options;

    public GameHealthCheckService(
        GameEngineService gameEngine,
        PerformanceMonitoringService performanceService,
        ILogger<GameHealthCheckService> logger,
        IOptions<GameServerOptions> options)
    {
        _gameEngine = gameEngine;
        _performanceService = performanceService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthData = new Dictionary<string, object>();
            var isHealthy = true;
            var issues = new List<string>();

            // 检查活跃战斗数量
            var activeBattles = _gameEngine.GetAllBattleUpdates();
            healthData["ActiveBattles"] = activeBattles.Count;
            
            if (activeBattles.Count > _options.MaxConcurrentBattles * 0.8) // 80%阈值
            {
                issues.Add($"High battle load: {activeBattles.Count}/{_options.MaxConcurrentBattles}");
                isHealthy = false;
            }

            // 检查系统性能
            var systemMetrics = _performanceService.GetSystemMetrics();
            healthData["MemoryUsageMB"] = systemMetrics.MemoryUsageMB;
            healthData["ThreadCount"] = systemMetrics.ThreadCount;
            healthData["GCGen2Collections"] = systemMetrics.GCGen2Collections;

            // 内存使用检查 (警告阈值: 1GB)
            if (systemMetrics.MemoryUsageMB > 1024)
            {
                issues.Add($"High memory usage: {systemMetrics.MemoryUsageMB}MB");
                if (systemMetrics.MemoryUsageMB > 2048) // 2GB为不健康
                {
                    isHealthy = false;
                }
            }

            // 线程数检查
            if (systemMetrics.ThreadCount > 100)
            {
                issues.Add($"High thread count: {systemMetrics.ThreadCount}");
                if (systemMetrics.ThreadCount > 200)
                {
                    isHealthy = false;
                }
            }

            // 检查操作性能
            var operationMetrics = _performanceService.GetOperationMetrics();
            var slowOperations = operationMetrics
                .Where(kvp => kvp.Value.TotalCalls > 0 && 
                             kvp.Value.TotalTimeMs / (double)kvp.Value.TotalCalls > _options.GameLoopIntervalMs * 2)
                .ToList();

            if (slowOperations.Any())
            {
                healthData["SlowOperations"] = slowOperations.Count;
                issues.Add($"Detected {slowOperations.Count} slow operations");
            }

            // 设置健康状态
            var status = isHealthy ? HealthStatus.Healthy : 
                        issues.Any(i => i.Contains("High memory") || i.Contains("High battle load")) ? 
                        HealthStatus.Unhealthy : HealthStatus.Degraded;

            var description = issues.Any() ? string.Join("; ", issues) : "All systems operating normally";

            return new HealthCheckResult(status, description, null, healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return new HealthCheckResult(HealthStatus.Unhealthy, "Health check failed", ex);
        }
    }
}
