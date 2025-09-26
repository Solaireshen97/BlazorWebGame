using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 战斗系统API服务实现
/// </summary>
public class BattleApiService : BaseApiService
{
    public BattleApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger<BattleApiService> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<ApiResponse<BattleStateDto>> StartBattleAsync(StartBattleRequest request)
    {
        return await PostAsync<BattleStateDto>("api/battle/start", request);
    }

    public async Task<ApiResponse<BattleStateDto>> GetBattleStateAsync(Guid battleId)
    {
        return await GetAsync<BattleStateDto>($"api/battle/state/{battleId}");
    }

    public async Task<ApiResponse<bool>> ExecuteBattleActionAsync(BattleActionRequest request)
    {
        return await PostAsync<bool>("api/battle/action", request);
    }

    public async Task<ApiResponse<bool>> StopBattleAsync(Guid battleId)
    {
        return await PostAsync<bool>($"api/battle/stop/{battleId}");
    }

    public async Task<ApiResponse<List<BattleStateDto>>> GetActiveBattlesAsync()
    {
        return await GetAsync<List<BattleStateDto>>("api/battle/active");
    }
}