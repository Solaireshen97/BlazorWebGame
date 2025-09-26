using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 监控系统API服务实现
/// </summary>
public class MonitoringApiService : BaseApiService, IMonitoringApi
{
    public MonitoringApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger<MonitoringApiService> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<ApiResponse<SystemMetricsDto>> GetSystemMetricsAsync()
    {
        return await GetAsync<SystemMetricsDto>("api/monitoring/system-metrics");
    }

    public async Task<ApiResponse<OperationMetricsDto>> GetOperationMetricsAsync()
    {
        return await GetAsync<OperationMetricsDto>("api/monitoring/operation-metrics");
    }

    public async Task<ApiResponse<GameStatusDto>> GetGameStatusAsync()
    {
        return await GetAsync<GameStatusDto>("api/monitoring/game-status");
    }

    public async Task<ApiResponse<bool>> ForceGarbageCollectionAsync()
    {
        return await PostAsync<bool>("api/monitoring/force-gc");
    }
}