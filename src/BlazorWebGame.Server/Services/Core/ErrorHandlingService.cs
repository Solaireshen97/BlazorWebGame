using BlazorWebGame.Shared.DTOs;
using System.Net;

namespace BlazorWebGame.Server.Services.Core;

/// <summary>
/// 统一错误处理服务
/// </summary>
public class ErrorHandlingService
{
    private readonly ILogger<ErrorHandlingService> _logger;

    public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 处理异常并返回标准化响应
    /// </summary>
    public ApiResponse<T> HandleException<T>(Exception ex, string operation, string? context = null)
    {
        var errorId = Guid.NewGuid().ToString("N")[..8];
        
        _logger.LogError(ex, "Operation '{Operation}' failed. Context: {Context}. ErrorId: {ErrorId}", 
            operation, context ?? "N/A", errorId);

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = GetUserFriendlyMessage(ex),
            Errors = new List<string> { GetErrorCode(ex), $"ErrorId: {errorId}" }
        };
    }

    /// <summary>
    /// 获取用户友好的错误消息
    /// </summary>
    private static string GetUserFriendlyMessage(Exception ex)
    {
        return ex switch
        {
            ArgumentException => "请求参数无效",
            UnauthorizedAccessException => "访问被拒绝",
            InvalidOperationException => "操作无效",
            TimeoutException => "操作超时",
            NotImplementedException => "功能暂未实现",
            _ => "服务器内部错误"
        };
    }

    /// <summary>
    /// 获取错误代码
    /// </summary>
    private static string GetErrorCode(Exception ex)
    {
        return ex switch
        {
            ArgumentException => "INVALID_ARGUMENT",
            UnauthorizedAccessException => "UNAUTHORIZED",
            InvalidOperationException => "INVALID_OPERATION",
            TimeoutException => "TIMEOUT",
            NotImplementedException => "NOT_IMPLEMENTED",
            _ => "INTERNAL_ERROR"
        };
    }

    /// <summary>
    /// 获取HTTP状态码
    /// </summary>
    public static HttpStatusCode GetHttpStatusCode(Exception ex)
    {
        return ex switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            InvalidOperationException => HttpStatusCode.BadRequest,
            TimeoutException => HttpStatusCode.RequestTimeout,
            NotImplementedException => HttpStatusCode.NotImplemented,
            _ => HttpStatusCode.InternalServerError
        };
    }
}
