using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 新的生产系统API服务实现
/// </summary>
public class ProductionApiServiceNew : BaseApiService
{
    public ProductionApiServiceNew(ConfigurableHttpClientFactory httpClientFactory, ILogger<ProductionApiServiceNew> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<ApiResponse<List<GatheringNodeDto>>> GetGatheringNodesAsync()
    {
        return await GetAsync<List<GatheringNodeDto>>("api/production/nodes");
    }

    public async Task<ApiResponse<GatheringNodeDto>> GetGatheringNodeAsync(string nodeId)
    {
        return await GetAsync<GatheringNodeDto>($"api/production/nodes/{nodeId}");
    }

    public async Task<ApiResponse<bool>> StartGatheringAsync(StartGatheringRequest request)
    {
        return await PostAsync<bool>("api/production/gathering/start", request);
    }

    public async Task<ApiResponse<GatheringResultDto>> StopGatheringAsync(StopGatheringRequest request)
    {
        return await PostAsync<GatheringResultDto>("api/production/gathering/stop", request);
    }

    public async Task<ApiResponse<GatheringStateDto>> GetGatheringStateAsync(string characterId)
    {
        return await GetAsync<GatheringStateDto>($"api/production/gathering/state/{characterId}");
    }
}