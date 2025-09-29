using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 角色状态API服务，专门用于查询和轮询角色状态信息
/// </summary>
public class CharacterStateApiService : BaseApiService
{
    public CharacterStateApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger<CharacterStateApiService> logger)
        : base(httpClientFactory, logger)
    {
    }

    /// <summary>
    /// 获取单个角色的状态信息
    /// </summary>
    public async Task<ApiResponse<CharacterStateDto>> GetCharacterStateAsync(string characterId)
    {
        try
        {
            using var httpClient = GetHttpClient();
            var response = await httpClient.GetFromJsonAsync<ApiResponse<CharacterStateDto>>(
                $"api/player/{characterId}/state");
            
            return response ?? new ApiResponse<CharacterStateDto>
            {
                Success = false,
                Message = "No response received"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character state for {CharacterId}", characterId);
            return new ApiResponse<CharacterStateDto>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// 批量获取多个角色的状态信息（用于轮询）
    /// </summary>
    public async Task<ApiResponse<CharacterStatesResponse>> GetCharacterStatesAsync(CharacterStatesRequest request)
    {
        try
        {
            using var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync("api/player/states", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<CharacterStatesResponse>>();
                return result ?? new ApiResponse<CharacterStatesResponse>
                {
                    Success = false,
                    Message = "No response received"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new ApiResponse<CharacterStatesResponse>
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character states for batch request");
            return new ApiResponse<CharacterStatesResponse>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// 获取所有活跃角色的状态
    /// </summary>
    public async Task<ApiResponse<List<CharacterStateDto>>> GetAllActiveCharacterStatesAsync()
    {
        try
        {
            using var httpClient = GetHttpClient();
            var response = await httpClient.GetFromJsonAsync<ApiResponse<List<CharacterStateDto>>>(
                "api/player/states/active");
            
            return response ?? new ApiResponse<List<CharacterStateDto>>
            {
                Success = false,
                Message = "No response received"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all active character states");
            return new ApiResponse<List<CharacterStateDto>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// 更新角色在线状态
    /// </summary>
    public async Task<ApiResponse<bool>> UpdateCharacterOnlineStatusAsync(string characterId, bool isOnline)
    {
        try
        {
            using var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync($"api/player/{characterId}/online-status", isOnline);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result ?? new ApiResponse<bool>
                {
                    Success = false,
                    Message = "No response received"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating online status for character {CharacterId}", characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// 获取角色状态服务统计信息
    /// </summary>
    public async Task<ApiResponse<CharacterStateServiceStats>> GetCharacterStateStatsAsync()
    {
        try
        {
            using var httpClient = GetHttpClient();
            var response = await httpClient.GetFromJsonAsync<ApiResponse<CharacterStateServiceStats>>(
                "api/player/states/stats");
            
            return response ?? new ApiResponse<CharacterStateServiceStats>
            {
                Success = false,
                Message = "No response received"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character state service stats");
            return new ApiResponse<CharacterStateServiceStats>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}