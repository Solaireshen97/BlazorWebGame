using System.Diagnostics;
using System.Text;

namespace BlazorWebGame.Server.Middleware;

/// <summary>
/// 请求日志中间件，记录所有HTTP请求的详细信息用于监控和安全审计
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 记录请求开始时间
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];
        
        // 获取客户端IP地址
        var clientIp = GetClientIpAddress(context);
        var userAgent = context.Request.Headers.UserAgent.ToString();
        
        // 记录请求开始
        _logger.LogInformation(
            "[{RequestId}] Request started: {Method} {Path} from {ClientIp} - UserAgent: {UserAgent}",
            requestId, context.Request.Method, context.Request.Path, clientIp, userAgent);

        // 对于敏感操作，记录更详细的信息
        if (IsSensitiveOperation(context.Request.Path))
        {
            _logger.LogWarning(
                "[{RequestId}] Sensitive operation attempted: {Method} {Path} from {ClientIp}",
                requestId, context.Request.Method, context.Request.Path, clientIp);
        }

        // 记录请求体（仅对POST/PUT请求且小于指定大小）
        string requestBody = "";
        if (ShouldLogRequestBody(context.Request))
        {
            requestBody = await ReadRequestBodyAsync(context.Request);
            if (!string.IsNullOrEmpty(requestBody))
            {
                _logger.LogDebug("[{RequestId}] Request body: {RequestBody}", requestId, requestBody);
            }
        }

        try
        {
            // 继续处理请求
            await _next(context);
        }
        catch (Exception ex)
        {
            // 记录异常
            _logger.LogError(ex, 
                "[{RequestId}] Request failed: {Method} {Path} from {ClientIp} - Error: {Error}",
                requestId, context.Request.Method, context.Request.Path, clientIp, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // 记录请求完成
            var statusCode = context.Response.StatusCode;
            var logLevel = GetLogLevelForStatusCode(statusCode);
            
            _logger.Log(logLevel,
                "[{RequestId}] Request completed: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms from {ClientIp}",
                requestId, context.Request.Method, context.Request.Path, statusCode, stopwatch.ElapsedMilliseconds, clientIp);

            // 对于错误响应，记录额外信息
            if (statusCode >= 400)
            {
                _logger.LogWarning(
                    "[{RequestId}] Error response: {StatusCode} for {Method} {Path} from {ClientIp}",
                    requestId, statusCode, context.Request.Method, context.Request.Path, clientIp);
            }

            // 对于慢请求，记录性能警告
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "[{RequestId}] Slow request detected: {ElapsedMs}ms for {Method} {Path}",
                    requestId, stopwatch.ElapsedMilliseconds, context.Request.Method, context.Request.Path);
            }
        }
    }

    /// <summary>
    /// 获取客户端真实IP地址
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        // 检查X-Forwarded-For头（代理/负载均衡器设置）
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        // 检查X-Real-IP头
        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        // 回退到连接的远程IP地址
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// 判断是否为敏感操作
    /// </summary>
    private static bool IsSensitiveOperation(PathString path)
    {
        var sensitiveEndpoints = new[]
        {
            "/api/battle/start",
            "/api/battle/action",
            "/api/auth/login",
            "/api/auth/register",
            "/api/user/",
            "/api/admin/"
        };

        return sensitiveEndpoints.Any(endpoint => path.StartsWithSegments(endpoint));
    }

    /// <summary>
    /// 判断是否应该记录请求体
    /// </summary>
    private static bool ShouldLogRequestBody(HttpRequest request)
    {
        // 只记录POST和PUT请求
        if (request.Method != "POST" && request.Method != "PUT")
            return false;

        // 检查内容类型
        var contentType = request.ContentType?.ToLower();
        if (contentType == null || !contentType.Contains("application/json"))
            return false;

        // 检查内容长度（避免记录过大的请求体）
        if (request.ContentLength > 1024) // 1KB限制
            return false;

        return true;
    }

    /// <summary>
    /// 读取请求体内容
    /// </summary>
    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        try
        {
            request.EnableBuffering();
            var body = request.Body;
            body.Position = 0;

            using var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            body.Position = 0;

            return requestBody;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 根据状态码获取日志级别
    /// </summary>
    private static LogLevel GetLogLevelForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            >= 200 => LogLevel.Information,
            _ => LogLevel.Debug
        };
    }
}