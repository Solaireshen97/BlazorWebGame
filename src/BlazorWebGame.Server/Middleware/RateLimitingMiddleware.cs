using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Server.Middleware;

/// <summary>
/// 自定义速率限制中间件，防止恶意请求和DDoS攻击
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;
    
    // 存储每个IP的请求记录
    private static readonly ConcurrentDictionary<string, ClientRequestRecord> ClientRequests = new();
    
    // 存储每个用户的请求记录（如果已认证）
    private static readonly ConcurrentDictionary<string, ClientRequestRecord> UserRequests = new();

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, RateLimitOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);
        var userId = GetUserId(context);
        var endpoint = GetEndpointKey(context.Request);

        // 检查IP级别的速率限制
        if (!await CheckRateLimitAsync(clientIp, endpoint, _options.IpRateLimit, ClientRequests))
        {
            await HandleRateLimitExceededAsync(context, "IP", clientIp);
            return;
        }

        // 如果用户已认证，检查用户级别的速率限制
        if (!string.IsNullOrEmpty(userId))
        {
            if (!await CheckRateLimitAsync(userId, endpoint, _options.UserRateLimit, UserRequests))
            {
                await HandleRateLimitExceededAsync(context, "User", userId);
                return;
            }
        }

        // 检查是否为可疑的重复请求
        if (await IsSuspiciousDuplicateRequestAsync(context, clientIp, userId))
        {
            await HandleSuspiciousRequestAsync(context, clientIp, userId);
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// 检查速率限制
    /// </summary>
    private async Task<bool> CheckRateLimitAsync(string identifier, string endpoint, RateLimitRule rule, 
        ConcurrentDictionary<string, ClientRequestRecord> requestRecords)
    {
        var key = $"{identifier}:{endpoint}";
        var now = DateTime.UtcNow;

        var record = requestRecords.AddOrUpdate(key, 
            new ClientRequestRecord { FirstRequestTime = now, RequestCount = 1, LastRequestTime = now },
            (k, existing) =>
            {
                // 如果时间窗口已过期，重置计数器
                if (now - existing.FirstRequestTime > rule.TimeWindow)
                {
                    existing.FirstRequestTime = now;
                    existing.RequestCount = 1;
                }
                else
                {
                    existing.RequestCount++;
                }
                existing.LastRequestTime = now;
                return existing;
            });

        // 检查是否超过限制
        if (record.RequestCount > rule.MaxRequests)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {Identifier} on endpoint {Endpoint}: {RequestCount}/{MaxRequests} in {TimeWindow}",
                identifier, endpoint, record.RequestCount, rule.MaxRequests, rule.TimeWindow);
            
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查是否为可疑的重复请求
    /// </summary>
    private async Task<bool> IsSuspiciousDuplicateRequestAsync(HttpContext context, string clientIp, string? userId)
    {
        // 只检查POST请求
        if (context.Request.Method != "POST")
            return false;

        var requestSignature = await GenerateRequestSignatureAsync(context);
        var identifier = userId ?? clientIp;
        var key = $"dup:{identifier}:{requestSignature}";

        var now = DateTime.UtcNow;
        var duplicateWindow = TimeSpan.FromSeconds(5); // 5秒内的相同请求视为可疑

        var record = ClientRequests.AddOrUpdate(key,
            new ClientRequestRecord { FirstRequestTime = now, RequestCount = 1, LastRequestTime = now },
            (k, existing) =>
            {
                if (now - existing.FirstRequestTime <= duplicateWindow)
                {
                    existing.RequestCount++;
                    existing.LastRequestTime = now;
                }
                else
                {
                    // 超出时间窗口，重置
                    existing.FirstRequestTime = now;
                    existing.RequestCount = 1;
                    existing.LastRequestTime = now;
                }
                return existing;
            });

        // 如果在短时间内有相同的请求，视为可疑
        if (record.RequestCount > 1)
        {
            _logger.LogWarning(
                "Suspicious duplicate request detected from {Identifier}: {RequestCount} identical requests in {TimeSpan}s",
                identifier, record.RequestCount, duplicateWindow.TotalSeconds);
            
            return true;
        }

        return false;
    }

    /// <summary>
    /// 生成请求签名用于重复检测
    /// </summary>
    private async Task<string> GenerateRequestSignatureAsync(HttpContext context)
    {
        var path = context.Request.Path.ToString();
        var method = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.ToString();

        // 对于POST请求，包含请求体
        string body = "";
        if (context.Request.Method == "POST" && context.Request.ContentLength < 1024)
        {
            try
            {
                context.Request.EnableBuffering();
                var bodyStream = context.Request.Body;
                bodyStream.Position = 0;
                
                using var reader = new StreamReader(bodyStream, leaveOpen: true);
                body = await reader.ReadToEndAsync();
                bodyStream.Position = 0;
            }
            catch
            {
                // 忽略读取错误
            }
        }

        return $"{method}:{path}:{userAgent}:{body}".GetHashCode().ToString();
    }

    /// <summary>
    /// 处理速率限制超出
    /// </summary>
    private async Task HandleRateLimitExceededAsync(HttpContext context, string limitType, string identifier)
    {
        _logger.LogWarning(
            "Rate limit exceeded - {LimitType}: {Identifier} for {Method} {Path}",
            limitType, identifier, context.Request.Method, context.Request.Path);

        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "Too many requests. Please try again later.",
            Errors = new List<string> { $"Rate limit exceeded for {limitType.ToLower()}" },
            Timestamp = DateTime.UtcNow
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// 处理可疑请求
    /// </summary>
    private async Task HandleSuspiciousRequestAsync(HttpContext context, string clientIp, string? userId)
    {
        _logger.LogWarning(
            "Suspicious duplicate request blocked from IP: {ClientIp}, User: {UserId} for {Method} {Path}",
            clientIp, userId ?? "N/A", context.Request.Method, context.Request.Path);

        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "Duplicate request detected. Please wait before retrying.",
            Errors = new List<string> { "Suspicious activity detected" },
            Timestamp = DateTime.UtcNow
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// 获取用户ID（如果已认证）
    /// </summary>
    private static string? GetUserId(HttpContext context)
    {
        return context.User?.Identity?.IsAuthenticated == true 
            ? context.User.FindFirst("UserId")?.Value 
            : null;
    }

    /// <summary>
    /// 获取端点键值
    /// </summary>
    private static string GetEndpointKey(HttpRequest request)
    {
        var path = request.Path.ToString();
        
        // 将路径参数标准化，避免每个不同的ID都被当作不同的端点
        if (path.Contains("/battle/state/"))
            return "/api/battle/state/{id}";
        if (path.Contains("/battle/stop/"))
            return "/api/battle/stop/{id}";
        
        return path;
    }
}

/// <summary>
/// 速率限制配置选项
/// </summary>
public class RateLimitOptions
{
    public RateLimitRule IpRateLimit { get; set; } = new()
    {
        MaxRequests = 100,
        TimeWindow = TimeSpan.FromMinutes(1)
    };
    
    public RateLimitRule UserRateLimit { get; set; } = new()
    {
        MaxRequests = 200,
        TimeWindow = TimeSpan.FromMinutes(1)
    };
}

/// <summary>
/// 速率限制规则
/// </summary>
public class RateLimitRule
{
    public int MaxRequests { get; set; }
    public TimeSpan TimeWindow { get; set; }
}

/// <summary>
/// 客户端请求记录
/// </summary>
public class ClientRequestRecord
{
    public DateTime FirstRequestTime { get; set; }
    public DateTime LastRequestTime { get; set; }
    public int RequestCount { get; set; }
}