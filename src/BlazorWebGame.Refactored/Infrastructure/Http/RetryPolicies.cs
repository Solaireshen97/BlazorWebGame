using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Refactored.Infrastructure.Http;

public static class RetryPolicies
{
    /// <summary>
    /// 标准HTTP重试策略：处理临时故障
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // HttpRequestException and 5XX and 408 status codes
            .OrResult(msg => !msg.IsSuccessStatusCode && ShouldRetry(msg.StatusCode))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2, 4, 8 seconds
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var exception = outcome.Exception;
                    var result = outcome.Result;
                    
                    if (exception != null)
                    {
                        logger.LogWarning("HTTP request retry {RetryCount}/3 after {Delay}ms due to exception: {Exception}", 
                            retryCount, timespan.TotalMilliseconds, exception.Message);
                    }
                    else if (result != null)
                    {
                        logger.LogWarning("HTTP request retry {RetryCount}/3 after {Delay}ms due to status: {StatusCode}", 
                            retryCount, timespan.TotalMilliseconds, result.StatusCode);
                    }
                });
    }

    /// <summary>
    /// 熔断器策略：在服务不可用时快速失败
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    logger.LogError("Circuit breaker opened for {Duration}s due to: {Exception}", 
                        duration.TotalSeconds, exception.Exception?.Message ?? exception.Result?.StatusCode.ToString());
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker reset - requests will be allowed again");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker half-open - testing if service is available");
                });
    }

    /// <summary>
    /// 超时策略：防止长时间等待
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(timeout);
    }

    /// <summary>
    /// 组合策略：重试 + 熔断器 + 超时
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(ILogger logger)
    {
        var retryPolicy = GetRetryPolicy(logger);
        var circuitBreakerPolicy = GetCircuitBreakerPolicy(logger);
        var timeoutPolicy = GetTimeoutPolicy(TimeSpan.FromSeconds(30));

        // 策略执行顺序：Timeout -> Retry -> CircuitBreaker
        return Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
    }

    /// <summary>
    /// 判断HTTP状态码是否应该重试
    /// </summary>
    private static bool ShouldRetry(System.Net.HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.RequestTimeout => true,
            System.Net.HttpStatusCode.TooManyRequests => true,
            System.Net.HttpStatusCode.InternalServerError => true,
            System.Net.HttpStatusCode.BadGateway => true,
            System.Net.HttpStatusCode.ServiceUnavailable => true,
            System.Net.HttpStatusCode.GatewayTimeout => true,
            _ => false
        };
    }
}