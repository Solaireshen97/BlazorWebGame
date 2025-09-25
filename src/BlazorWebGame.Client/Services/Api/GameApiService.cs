using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 客户端API服务，负责与服务器通信
/// </summary>
public class GameApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GameApiService> _logger;
    private string _baseUrl = "https://localhost:7000"; // 默认服务器地址

    public string BaseUrl => _baseUrl;

    public GameApiService(HttpClient httpClient, ILogger<GameApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // 配置基础地址
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }
    }

    /// <summary>
    /// 设置服务器基础地址
    /// </summary>
    public void SetBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    /// <summary>
    /// 开始战斗
    /// </summary>
    public async Task<ApiResponse<BattleStateDto>> StartBattleAsync(StartBattleRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/battle/start", request);
            
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
            return await _httpClient.GetFromJsonAsync<ApiResponse<BattleStateDto>>(
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
            var response = await _httpClient.PostAsJsonAsync("api/battle/action", request);
            
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
            var response = await _httpClient.PostAsync($"api/battle/stop/{battleId}", null);
            
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
            var response = await _httpClient.GetAsync("api/battle/state/00000000-0000-0000-0000-000000000000");
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
            var request = new CreatePartyRequest { CharacterId = characterId };
            var response = await _httpClient.PostAsJsonAsync("api/party/create", request);
            
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
            var request = new JoinPartyRequest { CharacterId = characterId, PartyId = partyId };
            var response = await _httpClient.PostAsJsonAsync("api/party/join", request);
            
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
            var request = new LeavePartyRequest { CharacterId = characterId };
            var response = await _httpClient.PostAsJsonAsync("api/party/leave", request);
            
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
            return await _httpClient.GetFromJsonAsync<ApiResponse<PartyDto>>(
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
            return await _httpClient.GetFromJsonAsync<ApiResponse<List<PartyDto>>>("api/party/all") 
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
            return await _httpClient.GetFromJsonAsync<ApiResponse<PartyDto>>($"api/party/{partyId}") 
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
}