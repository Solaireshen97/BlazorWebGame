using System.ComponentModel.DataAnnotations;

namespace BlazorWebGame.Server.Configuration;

/// <summary>
/// 安全配置选项
/// </summary>
public class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// CORS配置
    /// </summary>
    public CorsOptions Cors { get; set; } = new();

    /// <summary>
    /// 速率限制配置
    /// </summary>
    public RateLimitConfiguration RateLimit { get; set; } = new();
}

/// <summary>
/// CORS配置选项
/// </summary>
public class CorsOptions
{
    /// <summary>
    /// 允许的源地址
    /// </summary>
    public string[] AllowedOrigins { get; set; } = { "https://localhost:7051", "http://localhost:5190" };

    /// <summary>
    /// 是否允许凭据
    /// </summary>
    public bool AllowCredentials { get; set; } = true;
}

/// <summary>
/// 速率限制配置
/// </summary>
public class RateLimitConfiguration
{
    /// <summary>
    /// IP级别速率限制
    /// </summary>
    public RateLimitRule IpRateLimit { get; set; } = new();

    /// <summary>
    /// 用户级别速率限制
    /// </summary>
    public RateLimitRule UserRateLimit { get; set; } = new();

    /// <summary>
    /// 战斗端点特殊限制
    /// </summary>
    public RateLimitRule BattleEndpoints { get; set; } = new();
}

/// <summary>
/// 速率限制规则
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// 最大请求数
    /// </summary>
    [Range(1, 10000)]
    public int MaxRequests { get; set; } = 100;

    /// <summary>
    /// 时间窗口（分钟）
    /// </summary>
    [Range(1, 1440)] // 最大24小时
    public int TimeWindowMinutes { get; set; } = 1;

    /// <summary>
    /// 获取时间窗口TimeSpan
    /// </summary>
    public TimeSpan TimeWindow => TimeSpan.FromMinutes(TimeWindowMinutes);
}
