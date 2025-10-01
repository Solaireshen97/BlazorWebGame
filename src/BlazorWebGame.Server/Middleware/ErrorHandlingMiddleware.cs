using System.Net;
using System.Text.Json;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Server.Middleware;

/// <summary>
/// 全局错误处理中间件，统一处理异常并返回标准化的错误响应
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var clientIp = GetClientIpAddress(context);
        
        // 记录详细的错误信息
        _logger.LogError(exception,
            "[{RequestId}] Unhandled exception occurred: {ExceptionType} - {Message} for {Method} {Path} from {ClientIp}",
            requestId, exception.GetType().Name, exception.Message, 
            context.Request.Method, context.Request.Path, clientIp);

        // 根据异常类型确定HTTP状态码和错误响应
        var (statusCode, errorResponse) = GetErrorResponse(exception, requestId);

        // 设置响应
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        // 序列化并写入响应
        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// 根据异常类型生成适当的错误响应
    /// </summary>
    private (HttpStatusCode statusCode, ApiResponse<object> response) GetErrorResponse(Exception exception, string requestId)
    {
        return exception switch
        {
            ArgumentNullException or ArgumentException => (
                HttpStatusCode.BadRequest,
                new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = "Invalid request parameters",
                    Errors = _environment.IsDevelopment() ? new List<string> { exception.Message } : new List<string>(),
                    Timestamp = DateTime.UtcNow
                }
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = "Unauthorized access",
                    Errors = new List<string>(),
                    Timestamp = DateTime.UtcNow
                }
            ),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = "Resource not found",
                    Errors = new List<string>(),
                    Timestamp = DateTime.UtcNow
                }
            ),
            InvalidOperationException => (
                HttpStatusCode.Conflict,
                new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = "Operation cannot be performed at this time",
                    Errors = _environment.IsDevelopment() ? new List<string> { exception.Message } : new List<string>(),
                    Timestamp = DateTime.UtcNow
                }
            ),
            TimeoutException => (
                HttpStatusCode.RequestTimeout,
                new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = "Request timeout",
                    Errors = new List<string>(),
                    Timestamp = DateTime.UtcNow
                }
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = "An internal server error occurred",
                    Errors = _environment.IsDevelopment() 
                        ? new List<string> { $"[{requestId}] {exception.Message}", exception.StackTrace ?? string.Empty }
                        : new List<string> { $"Request ID: {requestId}" },
                    Timestamp = DateTime.UtcNow
                }
            )
        };
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
}