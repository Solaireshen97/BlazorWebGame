using BlazorWebGame.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 客户端声望API服务
/// </summary>
public class ReputationApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReputationApiService> _logger;

    public ReputationApiService(HttpClient httpClient, ILogger<ReputationApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取角色的声望信息
    /// </summary>
    public async Task<ApiResponse<ReputationDto>> GetReputationAsync(string characterId)
    {
        try
        {
            _logger.LogDebug("Getting reputation for character {CharacterId}", characterId);
            
            var response = await _httpClient.GetAsync($"api/reputation/{characterId}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReputationDto>>();
                return result ?? new ApiResponse<ReputationDto> { Success = false, Message = "响应数据为空" };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to get reputation for character {CharacterId}: {StatusCode} - {Content}", 
                characterId, response.StatusCode, errorContent);
            
            return new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = $"服务器返回错误: {response.StatusCode}"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error getting reputation for character {CharacterId}", characterId);
            return new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = "网络错误，请检查连接"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation for character {CharacterId}", characterId);
            return new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = "获取声望信息时发生错误"
            };
        }
    }

    /// <summary>
    /// 获取指定阵营的详细声望信息
    /// </summary>
    public async Task<ApiResponse<ReputationDetailDto>> GetReputationDetailAsync(string characterId, string factionName)
    {
        try
        {
            _logger.LogDebug("Getting reputation detail for character {CharacterId}, faction {FactionName}", characterId, factionName);
            
            var response = await _httpClient.GetAsync($"api/reputation/{characterId}/faction/{factionName}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReputationDetailDto>>();
                return result ?? new ApiResponse<ReputationDetailDto> { Success = false, Message = "响应数据为空" };
            }

            return new ApiResponse<ReputationDetailDto>
            {
                Success = false,
                Message = $"服务器返回错误: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation detail for character {CharacterId}, faction {FactionName}", characterId, factionName);
            return new ApiResponse<ReputationDetailDto>
            {
                Success = false,
                Message = "获取声望详情时发生错误"
            };
        }
    }

    /// <summary>
    /// 获取所有阵营的详细声望信息
    /// </summary>
    public async Task<ApiResponse<List<ReputationDetailDto>>> GetAllReputationDetailsAsync(string characterId)
    {
        try
        {
            _logger.LogDebug("Getting all reputation details for character {CharacterId}", characterId);
            
            var response = await _httpClient.GetAsync($"api/reputation/{characterId}/details");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ReputationDetailDto>>>();
                return result ?? new ApiResponse<List<ReputationDetailDto>> { Success = false, Message = "响应数据为空" };
            }

            return new ApiResponse<List<ReputationDetailDto>>
            {
                Success = false,
                Message = $"服务器返回错误: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all reputation details for character {CharacterId}", characterId);
            return new ApiResponse<List<ReputationDetailDto>>
            {
                Success = false,
                Message = "获取声望详情时发生错误"
            };
        }
    }

    /// <summary>
    /// 更新角色声望
    /// </summary>
    public async Task<ApiResponse<ReputationDto>> UpdateReputationAsync(UpdateReputationRequest request)
    {
        try
        {
            _logger.LogDebug("Updating reputation for character {CharacterId}, faction {FactionName}, amount {Amount}", 
                request.CharacterId, request.FactionName, request.Amount);
            
            var response = await _httpClient.PostAsJsonAsync("api/reputation/update", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReputationDto>>();
                return result ?? new ApiResponse<ReputationDto> { Success = false, Message = "响应数据为空" };
            }

            return new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = $"服务器返回错误: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reputation for character {CharacterId}", request.CharacterId);
            return new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = "更新声望时发生错误"
            };
        }
    }

    /// <summary>
    /// 批量更新角色声望
    /// </summary>
    public async Task<ApiResponse<ReputationDto>> BatchUpdateReputationAsync(BatchUpdateReputationRequest request)
    {
        try
        {
            _logger.LogDebug("Batch updating reputation for character {CharacterId}, {ChangeCount} changes", 
                request.CharacterId, request.Changes.Count);
            
            var response = await _httpClient.PostAsJsonAsync("api/reputation/batch-update", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReputationDto>>();
                return result ?? new ApiResponse<ReputationDto> { Success = false, Message = "响应数据为空" };
            }

            return new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = $"服务器返回错误: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch updating reputation for character {CharacterId}", request.CharacterId);
            return new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = "批量更新声望时发生错误"
            };
        }
    }

    /// <summary>
    /// 获取声望奖励信息
    /// </summary>
    public async Task<ApiResponse<List<ReputationRewardDto>>> GetReputationRewardsAsync(ReputationRewardsRequest request)
    {
        try
        {
            _logger.LogDebug("Getting reputation rewards for character {CharacterId}", request.CharacterId);
            
            var response = await _httpClient.PostAsJsonAsync("api/reputation/rewards", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ReputationRewardDto>>>();
                return result ?? new ApiResponse<List<ReputationRewardDto>> { Success = false, Message = "响应数据为空" };
            }

            return new ApiResponse<List<ReputationRewardDto>>
            {
                Success = false,
                Message = $"服务器返回错误: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation rewards for character {CharacterId}", request.CharacterId);
            return new ApiResponse<List<ReputationRewardDto>>
            {
                Success = false,
                Message = "获取声望奖励时发生错误"
            };
        }
    }

    /// <summary>
    /// 获取角色可获得的声望奖励
    /// </summary>
    public async Task<ApiResponse<List<ReputationRewardDto>>> GetAvailableRewardsAsync(string characterId)
    {
        try
        {
            _logger.LogDebug("Getting available rewards for character {CharacterId}", characterId);
            
            var response = await _httpClient.GetAsync($"api/reputation/{characterId}/available-rewards");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ReputationRewardDto>>>();
                return result ?? new ApiResponse<List<ReputationRewardDto>> { Success = false, Message = "响应数据为空" };
            }

            return new ApiResponse<List<ReputationRewardDto>>
            {
                Success = false,
                Message = $"服务器返回错误: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available rewards for character {CharacterId}", characterId);
            return new ApiResponse<List<ReputationRewardDto>>
            {
                Success = false,
                Message = "获取可用奖励时发生错误"
            };
        }
    }

    /// <summary>
    /// 获取声望历史记录
    /// </summary>
    public async Task<ApiResponse<List<ReputationHistoryDto>>> GetReputationHistoryAsync(string characterId, string? factionName = null, int days = 30)
    {
        try
        {
            _logger.LogDebug("Getting reputation history for character {CharacterId}", characterId);
            
            var url = $"api/reputation/{characterId}/history?days={days}";
            if (!string.IsNullOrEmpty(factionName))
            {
                url += $"&factionName={factionName}";
            }
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ReputationHistoryDto>>>();
                return result ?? new ApiResponse<List<ReputationHistoryDto>> { Success = false, Message = "响应数据为空" };
            }

            return new ApiResponse<List<ReputationHistoryDto>>
            {
                Success = false,
                Message = $"服务器返回错误: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation history for character {CharacterId}", characterId);
            return new ApiResponse<List<ReputationHistoryDto>>
            {
                Success = false,
                Message = "获取声望历史时发生错误"
            };
        }
    }

    /// <summary>
    /// 获取声望统计信息
    /// </summary>
    public async Task<ApiResponse<ReputationStatsDto>> GetReputationStatsAsync(string characterId)
    {
        try
        {
            _logger.LogDebug("Getting reputation stats for character {CharacterId}", characterId);
            
            var response = await _httpClient.GetAsync($"api/reputation/{characterId}/stats");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReputationStatsDto>>();
                return result ?? new ApiResponse<ReputationStatsDto> { Success = false, Message = "响应数据为空" };
            }

            return new ApiResponse<ReputationStatsDto>
            {
                Success = false,
                Message = $"服务器返回错误: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation stats for character {CharacterId}", characterId);
            return new ApiResponse<ReputationStatsDto>
            {
                Success = false,
                Message = "获取声望统计时发生错误"
            };
        }
    }

    /// <summary>
    /// 重置角色声望（管理员功能）
    /// </summary>
    public async Task<ApiResponse<ReputationDto>> ResetReputationAsync(string characterId, string? factionName = null)
    {
        // 这个功能通常需要管理员权限，暂不实现客户端调用
        await Task.CompletedTask;
        return new ApiResponse<ReputationDto>
        {
            Success = false,
            Message = "重置声望功能暂不可用"
        };
    }
}