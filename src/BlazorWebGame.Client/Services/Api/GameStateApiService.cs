using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 客户端游戏状态API服务 - 与服务端GameStateController通信
/// </summary>
public class GameStateApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GameStateApiService> _logger;

    public GameStateApiService(HttpClient httpClient, ILogger<GameStateApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取角色的完整游戏状态
    /// </summary>
    public async Task<ApiResponse<GameStateDto>> GetGameStateAsync(string characterId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ApiResponse<GameStateDto>>($"api/gamestate/{characterId}")
                ?? new ApiResponse<GameStateDto> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game state for character {CharacterId}", characterId);
            return new ApiResponse<GameStateDto>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 更新角色的动作状态
    /// </summary>
    public async Task<ApiResponse<PlayerActionStateDto>> UpdatePlayerActionAsync(string characterId, PlayerActionState actionState)
    {
        try
        {
            var request = new UpdatePlayerActionRequest
            {
                CharacterId = characterId,
                ActionState = actionState
            };

            var response = await _httpClient.PostAsJsonAsync("api/gamestate/action/update", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<PlayerActionStateDto>>()
                    ?? new ApiResponse<PlayerActionStateDto> { Success = false };
            }

            return new ApiResponse<PlayerActionStateDto>
            {
                Success = false,
                Message = $"服务器返回 {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating player action for character {CharacterId}", characterId);
            return new ApiResponse<PlayerActionStateDto>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 设置角色的自动化操作
    /// </summary>
    public async Task<ApiResponse<AutomationStateDto>> SetAutomationAsync(SetAutomationRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/gamestate/automation/set", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<AutomationStateDto>>()
                    ?? new ApiResponse<AutomationStateDto> { Success = false };
            }

            return new ApiResponse<AutomationStateDto>
            {
                Success = false,
                Message = $"服务器返回 {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting automation for character {CharacterId}", request.CharacterId);
            return new ApiResponse<AutomationStateDto>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 获取角色的自动化状态
    /// </summary>
    public async Task<ApiResponse<AutomationStateDto>> GetAutomationStateAsync(string characterId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ApiResponse<AutomationStateDto>>($"api/gamestate/automation/{characterId}")
                ?? new ApiResponse<AutomationStateDto> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting automation state for character {CharacterId}", characterId);
            return new ApiResponse<AutomationStateDto>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 处理角色复活
    /// </summary>
    public async Task<ApiResponse<CharacterStatusDto>> ReviveCharacterAsync(string characterId)
    {
        try
        {
            var request = new ReviveCharacterRequest { CharacterId = characterId };
            var response = await _httpClient.PostAsJsonAsync("api/gamestate/revive", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<CharacterStatusDto>>()
                    ?? new ApiResponse<CharacterStatusDto> { Success = false };
            }

            return new ApiResponse<CharacterStatusDto>
            {
                Success = false,
                Message = $"服务器返回 {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviving character {CharacterId}", characterId);
            return new ApiResponse<CharacterStatusDto>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 获取实时状态更新（用于轮询）
    /// </summary>
    public async Task<ApiResponse<GameStateUpdateDto>> GetUpdatesAsync(string characterId, long lastUpdateTick = 0)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ApiResponse<GameStateUpdateDto>>($"api/gamestate/updates/{characterId}?lastUpdateTick={lastUpdateTick}")
                ?? new ApiResponse<GameStateUpdateDto> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting updates for character {CharacterId}", characterId);
            return new ApiResponse<GameStateUpdateDto>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 重置角色状态到空闲
    /// </summary>
    public async Task<ApiResponse<string>> ResetCharacterStateAsync(string characterId)
    {
        try
        {
            var request = new ResetCharacterStateRequest { CharacterId = characterId };
            var response = await _httpClient.PostAsJsonAsync("api/gamestate/reset", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<string>>()
                    ?? new ApiResponse<string> { Success = false };
            }

            return new ApiResponse<string>
            {
                Success = false,
                Message = $"服务器返回 {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting character state for {CharacterId}", characterId);
            return new ApiResponse<string>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }
}