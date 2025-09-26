using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 离线结算系统API服务实现
/// </summary>
public class OfflineSettlementApiService : BaseApiService
{
    public OfflineSettlementApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger<OfflineSettlementApiService> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<ApiResponse<OfflineSettlementResultDto>> ProcessPlayerOfflineSettlementAsync(string playerId, OfflineSettlementRequestDto request)
    {
        return await PostAsync<OfflineSettlementResultDto>($"api/offline-settlement/player/{playerId}", request);
    }

    public async Task<ApiResponse<OfflineSettlementResultDto>> ProcessTeamOfflineSettlementAsync(string teamId, OfflineSettlementRequestDto request)
    {
        return await PostAsync<OfflineSettlementResultDto>($"api/offline-settlement/team/{teamId}", request);
    }

    public async Task<ApiResponse<List<OfflineSettlementResultDto>>> ProcessBatchOfflineSettlementAsync(BatchOfflineSettlementRequestDto request)
    {
        return await PostAsync<List<OfflineSettlementResultDto>>("api/offline-settlement/batch", request);
    }

    public async Task<ApiResponse<PlayerOfflineInfoDto>> GetPlayerOfflineInfoAsync(string playerId)
    {
        return await GetAsync<PlayerOfflineInfoDto>($"api/offline-settlement/player/{playerId}/offline-info");
    }

    public async Task<ApiResponse<OfflineSettlementStatisticsDto>> GetOfflineSettlementStatisticsAsync()
    {
        return await GetAsync<OfflineSettlementStatisticsDto>("api/offline-settlement/statistics");
    }

    /// <summary>
    /// 同步离线战斗进度
    /// </summary>
    public async Task<ApiResponse<object>> SyncOfflineBattleProgressAsync(string playerId, OfflineBattleProgressSyncRequest request)
    {
        return await PostAsync<object>($"api/offline-settlement/player/{playerId}/battle-progress", request);
    }
}