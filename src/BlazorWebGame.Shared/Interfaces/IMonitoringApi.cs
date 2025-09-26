using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 监控系统API接口定义
/// </summary>
public interface IMonitoringApi
{
    /// <summary>
    /// 获取系统性能指标
    /// </summary>
    Task<ApiResponse<SystemMetricsDto>> GetSystemMetricsAsync();

    /// <summary>
    /// 获取操作性能指标
    /// </summary>
    Task<ApiResponse<OperationMetricsDto>> GetOperationMetricsAsync();

    /// <summary>
    /// 获取游戏状态信息
    /// </summary>
    Task<ApiResponse<GameStatusDto>> GetGameStatusAsync();

    /// <summary>
    /// 强制执行垃圾回收
    /// </summary>
    Task<ApiResponse<bool>> ForceGarbageCollectionAsync();
}

/// <summary>
/// 系统性能指标DTO
/// </summary>
public class SystemMetricsDto
{
    public double CpuUsage { get; set; }
    public long MemoryUsed { get; set; }
    public long MemoryTotal { get; set; }
    public int ActiveConnections { get; set; }
    public TimeSpan Uptime { get; set; }
}

/// <summary>
/// 操作性能指标DTO
/// </summary>
public class OperationMetricsDto
{
    public Dictionary<string, int> RequestCounts { get; set; } = new();
    public Dictionary<string, double> AverageResponseTimes { get; set; } = new();
    public Dictionary<string, int> ErrorCounts { get; set; } = new();
}

/// <summary>
/// 游戏状态DTO
/// </summary>
public class GameStatusDto
{
    public int ActivePlayers { get; set; }
    public int ActiveBattles { get; set; }
    public int ActiveParties { get; set; }
    public string ServerStatus { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}