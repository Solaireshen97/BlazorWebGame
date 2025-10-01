using System.ComponentModel.DataAnnotations;

namespace BlazorWebGame.Rebuild.Configuration;

/// <summary>
/// 监控配置选项
/// </summary>
public class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    /// <summary>
    /// 启用性能日志记录
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = true;

    /// <summary>
    /// 慢请求阈值（毫秒）
    /// </summary>
    [Range(100, 30000)]
    public int SlowRequestThresholdMs { get; set; } = 1000;

    /// <summary>
    /// 启用安全审计日志
    /// </summary>
    public bool EnableSecurityAuditLog { get; set; } = true;

    /// <summary>
    /// 记录请求正文（仅用于调试，生产环境不建议启用）
    /// </summary>
    public bool LogRequestBodies { get; set; } = false;

    /// <summary>
    /// 启用健康检查
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// 启用指标收集
    /// </summary>
    public bool EnableMetrics { get; set; } = false;
}
