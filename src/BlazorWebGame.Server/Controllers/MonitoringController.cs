using Microsoft.AspNetCore.Mvc;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Server.Services.Core;
using BlazorWebGame.Server.Services.GameSystem;

namespace BlazorWebGame.Server.Controllers;

/// <summary>
/// 监控和诊断API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly PerformanceMonitoringService _performanceService;
    private readonly GameEngineService _gameEngine;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        PerformanceMonitoringService performanceService,
        GameEngineService gameEngine,
        ILogger<MonitoringController> logger)
    {
        _performanceService = performanceService;
        _gameEngine = gameEngine;
        _logger = logger;
    }

    /// <summary>
    /// 获取系统性能指标
    /// </summary>
    [HttpGet("system-metrics")]
    public ActionResult<ApiResponse<SystemPerformanceSnapshot>> GetSystemMetrics()
    {
        using var tracker = _performanceService.TrackOperation("GetSystemMetrics");
        
        var metrics = _performanceService.GetSystemMetrics();
        
        return Ok(new ApiResponse<SystemPerformanceSnapshot>
        {
            IsSuccess = true,
            Data = metrics,
            Message = "系统性能指标获取成功"
        });
    }

    /// <summary>
    /// 获取操作性能统计
    /// </summary>
    [HttpGet("operation-metrics")]
    public ActionResult<ApiResponse<Dictionary<string, PerformanceMetrics>>> GetOperationMetrics()
    {
        using var tracker = _performanceService.TrackOperation("GetOperationMetrics");
        
        var metrics = _performanceService.GetOperationMetrics();
        
        return Ok(new ApiResponse<Dictionary<string, PerformanceMetrics>>
        {
            IsSuccess = true,
            Data = metrics,
            Message = $"获取到 {metrics.Count} 个操作的性能统计"
        });
    }

    /// <summary>
    /// 获取游戏服务器状态
    /// </summary>
    [HttpGet("game-status")]
    public ActionResult<ApiResponse<GameServerStatus>> GetGameStatus()
    {
        using var tracker = _performanceService.TrackOperation("GetGameStatus");
        
        var activeBattles = _gameEngine.GetAllBattleUpdates();
        var systemMetrics = _performanceService.GetSystemMetrics();
        
        var status = new GameServerStatus
        {
            ActiveBattles = activeBattles.Count,
            TotalMemoryMB = systemMetrics.TotalMemoryMB,
            WorkingSetMB = systemMetrics.MemoryUsageMB,
            ThreadCount = systemMetrics.ThreadCount,
            GCCollections = new Dictionary<string, int>
            {
                ["Gen0"] = systemMetrics.GCGen0Collections,
                ["Gen1"] = systemMetrics.GCGen1Collections,
                ["Gen2"] = systemMetrics.GCGen2Collections
            },
            Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            LastUpdated = DateTime.UtcNow
        };
        
        return Ok(new ApiResponse<GameServerStatus>
        {
            IsSuccess = true,
            Data = status,
            Message = "游戏服务器状态获取成功"
        });
    }

    /// <summary>
    /// 强制进行垃圾回收（仅限开发环境）
    /// </summary>
    [HttpPost("force-gc")]
    public ActionResult<ApiResponse<string>> ForceGarbageCollection()
    {
        if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return BadRequest(new ApiResponse<string>
            {
                IsSuccess = false,
                Message = "此操作仅限开发环境使用"
            });
        }

        using var tracker = _performanceService.TrackOperation("ForceGC");
        
        var beforeMemory = GC.GetTotalMemory(false) / 1024 / 1024;
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var afterMemory = GC.GetTotalMemory(false) / 1024 / 1024;
        
        _logger.LogInformation("Forced garbage collection completed. Memory before: {BeforeMB}MB, after: {AfterMB}MB", 
            beforeMemory, afterMemory);
        
        return Ok(new ApiResponse<string>
        {
            IsSuccess = true,
            Data = $"垃圾回收完成。内存释放: {beforeMemory - afterMemory}MB",
            Message = "垃圾回收执行成功"
        });
    }
}

/// <summary>
/// 游戏服务器状态信息
/// </summary>
public class GameServerStatus
{
    public int ActiveBattles { get; set; }
    public long TotalMemoryMB { get; set; }
    public long WorkingSetMB { get; set; }
    public int ThreadCount { get; set; }
    public Dictionary<string, int> GCCollections { get; set; } = new();
    public TimeSpan Uptime { get; set; }
    public DateTime LastUpdated { get; set; }
}
