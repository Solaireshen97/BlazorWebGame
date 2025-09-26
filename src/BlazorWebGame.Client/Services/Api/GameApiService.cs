using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 客户端API服务，负责与服务器通信 (向后兼容的统一接口)
/// </summary>
public class GameApiService
{
    private readonly GameApiClient _apiClient;
    private readonly ILogger<GameApiService> _logger;

    public GameApiService(GameApiClient apiClient, ILogger<GameApiService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// 设置认证令牌 - 为需要认证的API调用做准备
    /// </summary>
    public async Task<string> SetupAuthenticationAsync()
    {
        return await _apiClient.SetupAuthenticationAsync();
    }

    #region Battle API Methods (向后兼容)

    /// <summary>
    /// 开始战斗
    /// </summary>
    public async Task<ApiResponse<BattleStateDto>> StartBattleAsync(StartBattleRequest request)
    {
        return await _apiClient.Battle.StartBattleAsync(request);
    }

    /// <summary>
    /// 获取战斗状态
    /// </summary>
    public async Task<ApiResponse<BattleStateDto>> GetBattleStateAsync(Guid battleId)
    {
        return await _apiClient.Battle.GetBattleStateAsync(battleId);
    }

    /// <summary>
    /// 执行战斗动作
    /// </summary>
    public async Task<ApiResponse<bool>> ExecuteBattleActionAsync(BattleActionRequest request)
    {
        return await _apiClient.Battle.ExecuteBattleActionAsync(request);
    }

    /// <summary>
    /// 停止战斗
    /// </summary>
    public async Task<ApiResponse<bool>> StopBattleAsync(Guid battleId)
    {
        return await _apiClient.Battle.StopBattleAsync(battleId);
    }

    /// <summary>
    /// 检查服务器连接状态
    /// </summary>
    public async Task<bool> IsServerAvailableAsync()
    {
        return await _apiClient.IsServerAvailableAsync();
    }

    #endregion

    #region Party API Methods (向后兼容)

    /// <summary>
    /// 创建组队
    /// </summary>
    public async Task<ApiResponse<PartyDto>> CreatePartyAsync(string characterId)
    {
        return await _apiClient.Party.CreatePartyAsync(characterId);
    }

    /// <summary>
    /// 加入组队
    /// </summary>
    public async Task<ApiResponse<bool>> JoinPartyAsync(string characterId, Guid partyId)
    {
        return await _apiClient.Party.JoinPartyAsync(characterId, partyId);
    }

    /// <summary>
    /// 离开组队
    /// </summary>
    public async Task<ApiResponse<bool>> LeavePartyAsync(string characterId)
    {
        return await _apiClient.Party.LeavePartyAsync(characterId);
    }

    /// <summary>
    /// 获取角色的组队信息
    /// </summary>
    public async Task<ApiResponse<PartyDto>> GetPartyForCharacterAsync(string characterId)
    {
        return await _apiClient.Party.GetPartyForCharacterAsync(characterId);
    }

    /// <summary>
    /// 获取所有组队列表
    /// </summary>
    public async Task<ApiResponse<List<PartyDto>>> GetAllPartiesAsync()
    {
        return await _apiClient.Party.GetAllPartiesAsync();
    }

    /// <summary>
    /// 根据ID获取组队信息
    /// </summary>
    public async Task<ApiResponse<PartyDto>> GetPartyAsync(Guid partyId)
    {
        return await _apiClient.Party.GetPartyAsync(partyId);
    }

    #endregion

    #region Offline Settlement and Synchronization API (向后兼容)

    /// <summary>
    /// 更新角色数据
    /// </summary>
    public async Task<ApiResponse<object>> UpdateCharacterAsync(CharacterUpdateRequest request)
    {
        // 这个方法在新的API结构中可能需要重新映射到Character API
        try
        {
            var result = await _apiClient.Character.UpdateCharacterStatusAsync(request.CharacterId, new UpdateCharacterStatusRequest
            {
                CharacterId = request.CharacterId,
                Data = request.Updates
            });
            
            return new ApiResponse<object>
            {
                Success = result.Success,
                Message = result.Message,
                Data = result.Data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character {CharacterId}", request.CharacterId);
            return new ApiResponse<object>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 更新队伍进度
    /// </summary>
    public async Task<ApiResponse<object>> UpdateTeamProgressAsync(TeamProgressUpdateRequest request)
    {
        // 将队伍进度更新映射到Party API或其他相关API
        try
        {
            // 这里可能需要根据实际的服务端API实现来调整
            _logger.LogWarning("UpdateTeamProgressAsync not yet implemented in new API structure");
            return new ApiResponse<object>
            {
                Success = false,
                Message = "Not implemented in new API structure"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating team progress {PartyId}", request.PartyId);
            return new ApiResponse<object>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 处理离线结算
    /// </summary>
    public async Task<ApiResponse<OfflineSettlementResultDto>> ProcessOfflineSettlementAsync(OfflineSettlementRequestDto request)
    {
        return await _apiClient.OfflineSettlement.ProcessPlayerOfflineSettlementAsync(request.PlayerId, request);
    }

    /// <summary>
    /// 批量处理离线结算
    /// </summary>
    public async Task<ApiResponse<List<OfflineSettlementResultDto>>> ProcessBatchOfflineSettlementAsync(BatchOfflineSettlementRequestDto request)
    {
        return await _apiClient.OfflineSettlement.ProcessBatchOfflineSettlementAsync(request);
    }

    #endregion

    /// <summary>
    /// 获取新的API客户端实例，用于访问所有功能模块
    /// </summary>
    public GameApiClient GetApiClient() => _apiClient;
}