using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 客户端API服务，负责与服务器通信
/// </summary>
public class GameApiService
{
    private readonly ConfigurableHttpClientFactory _httpClientFactory;
    private readonly ILogger<GameApiService> _logger;

    public GameApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger<GameApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前配置的 HttpClient
    /// </summary>
    private HttpClient GetHttpClient() => _httpClientFactory.GetHttpClient();

    /// <summary>
    /// 开始战斗
    /// </summary>
    public async Task<ApiResponse<BattleStateDto>> StartBattleAsync(StartBattleRequest request)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync("api/battle/start", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<BattleStateDto>>() 
                    ?? new ApiResponse<BattleStateDto> { Success = false };
            }

            return new ApiResponse<BattleStateDto>
            {
                Success = false,
                Message = $"Server returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting battle");
            return new ApiResponse<BattleStateDto>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 获取战斗状态
    /// </summary>
    public async Task<ApiResponse<BattleStateDto>> GetBattleStateAsync(Guid battleId)
    {
        try
        {
            var httpClient = GetHttpClient();
            return await httpClient.GetFromJsonAsync<ApiResponse<BattleStateDto>>(
                $"api/battle/state/{battleId}") 
                ?? new ApiResponse<BattleStateDto> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting battle state for {BattleId}", battleId);
            return new ApiResponse<BattleStateDto>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 执行战斗动作
    /// </summary>
    public async Task<ApiResponse<bool>> ExecuteBattleActionAsync(BattleActionRequest request)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync("api/battle/action", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>() 
                    ?? new ApiResponse<bool> { Success = false };
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Server returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing battle action");
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 停止战斗
    /// </summary>
    public async Task<ApiResponse<bool>> StopBattleAsync(Guid battleId)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsync($"api/battle/stop/{battleId}", null);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>() 
                    ?? new ApiResponse<bool> { Success = false };
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Server returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping battle {BattleId}", battleId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 检查服务器连接状态
    /// </summary>
    public async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.GetAsync("api/battle/state/00000000-0000-0000-0000-000000000000");
            // 我们不关心返回内容，只要能连通就行
            return true;
        }
        catch
        {
            return false;
        }
    }

    #region Party API Methods

    /// <summary>
    /// 创建组队
    /// </summary>
    public async Task<ApiResponse<PartyDto>> CreatePartyAsync(string characterId)
    {
        try
        {
            var httpClient = GetHttpClient();
            var request = new CreatePartyRequest { CharacterId = characterId };
            var response = await httpClient.PostAsJsonAsync("api/party/create", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<PartyDto>>() 
                    ?? new ApiResponse<PartyDto> { Success = false };
            }

            return new ApiResponse<PartyDto>
            {
                Success = false,
                Message = $"Server returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating party for character {CharacterId}", characterId);
            return new ApiResponse<PartyDto>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 加入组队
    /// </summary>
    public async Task<ApiResponse<bool>> JoinPartyAsync(string characterId, Guid partyId)
    {
        try
        {
            var httpClient = GetHttpClient();
            var request = new JoinPartyRequest { CharacterId = characterId, PartyId = partyId };
            var response = await httpClient.PostAsJsonAsync("api/party/join", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>() 
                    ?? new ApiResponse<bool> { Success = false };
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Server returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining party for character {CharacterId}", characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 离开组队
    /// </summary>
    public async Task<ApiResponse<bool>> LeavePartyAsync(string characterId)
    {
        try
        {
            var httpClient = GetHttpClient();
            var request = new LeavePartyRequest { CharacterId = characterId };
            var response = await httpClient.PostAsJsonAsync("api/party/leave", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>() 
                    ?? new ApiResponse<bool> { Success = false };
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Server returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving party for character {CharacterId}", characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 获取角色的组队信息
    /// </summary>
    public async Task<ApiResponse<PartyDto>> GetPartyForCharacterAsync(string characterId)
    {
        try
        {
            var httpClient = GetHttpClient();
            return await httpClient.GetFromJsonAsync<ApiResponse<PartyDto>>(
                $"api/party/character/{characterId}") 
                ?? new ApiResponse<PartyDto> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting party for character {CharacterId}", characterId);
            return new ApiResponse<PartyDto>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 获取所有组队列表
    /// </summary>
    public async Task<ApiResponse<List<PartyDto>>> GetAllPartiesAsync()
    {
        try
        {
            var httpClient = GetHttpClient();
            return await httpClient.GetFromJsonAsync<ApiResponse<List<PartyDto>>>("api/party/all") 
                ?? new ApiResponse<List<PartyDto>> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all parties");
            return new ApiResponse<List<PartyDto>>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 根据ID获取组队信息
    /// </summary>
    public async Task<ApiResponse<PartyDto>> GetPartyAsync(Guid partyId)
    {
        try
        {
            var httpClient = GetHttpClient();
            return await httpClient.GetFromJsonAsync<ApiResponse<PartyDto>>($"api/party/{partyId}") 
                ?? new ApiResponse<PartyDto> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting party {PartyId}", partyId);
            return new ApiResponse<PartyDto>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    #endregion

    #region Offline Settlement and Synchronization API

    /// <summary>
    /// 更新角色数据
    /// </summary>
    public async Task<ApiResponse<object>> UpdateCharacterAsync(CharacterUpdateRequest request)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync("api/character/update", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<object>>() 
                    ?? new ApiResponse<object> { Success = false };
            }

            return new ApiResponse<object>
            {
                Success = false,
                Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
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
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync("api/team/progress", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<object>>() 
                    ?? new ApiResponse<object> { Success = false };
            }

            return new ApiResponse<object>
            {
                Success = false,
                Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
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
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync("api/offline-settlement", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<OfflineSettlementResultDto>>() 
                    ?? new ApiResponse<OfflineSettlementResultDto> { Success = false };
            }

            return new ApiResponse<OfflineSettlementResultDto>
            {
                Success = false,
                Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing offline settlement for player {PlayerId}", request.PlayerId);
            return new ApiResponse<OfflineSettlementResultDto>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 批量处理离线结算
    /// </summary>
    public async Task<ApiResponse<List<OfflineSettlementResultDto>>> ProcessBatchOfflineSettlementAsync(BatchOfflineSettlementRequestDto request)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync("api/offline-settlement/batch", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<List<OfflineSettlementResultDto>>>() 
                    ?? new ApiResponse<List<OfflineSettlementResultDto>> { Success = false };
            }

            return new ApiResponse<List<OfflineSettlementResultDto>>
            {
                Success = false,
                Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch offline settlement");
            return new ApiResponse<List<OfflineSettlementResultDto>>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    #endregion
}