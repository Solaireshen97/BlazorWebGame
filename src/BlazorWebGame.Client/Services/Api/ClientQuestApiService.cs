using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 客户端任务系统API服务
/// </summary>
public class ClientQuestApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClientQuestApiService> _logger;

    public ClientQuestApiService(HttpClient httpClient, ILogger<ClientQuestApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取角色任务状态
    /// </summary>
    public async Task<ApiResponse<CharacterQuestStatusDto>> GetQuestStatusAsync(string characterId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ApiResponse<CharacterQuestStatusDto>>($"api/quest/status/{characterId}")
                ?? new ApiResponse<CharacterQuestStatusDto> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quest status for character {CharacterId}", characterId);
            return new ApiResponse<CharacterQuestStatusDto>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 获取每日任务
    /// </summary>
    public async Task<ApiResponse<List<QuestDto>>> GetDailyQuestsAsync(string characterId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ApiResponse<List<QuestDto>>>($"api/quest/daily/{characterId}")
                ?? new ApiResponse<List<QuestDto>> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily quests for character {CharacterId}", characterId);
            return new ApiResponse<List<QuestDto>>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 获取每周任务
    /// </summary>
    public async Task<ApiResponse<List<QuestDto>>> GetWeeklyQuestsAsync(string characterId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ApiResponse<List<QuestDto>>>($"api/quest/weekly/{characterId}")
                ?? new ApiResponse<List<QuestDto>> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weekly quests for character {CharacterId}", characterId);
            return new ApiResponse<List<QuestDto>>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 接受任务
    /// </summary>
    public async Task<ApiResponse<bool>> AcceptQuestAsync(string characterId, string questId)
    {
        try
        {
            var request = new AcceptQuestRequest
            {
                CharacterId = characterId,
                QuestId = questId
            };

            var response = await _httpClient.PostAsJsonAsync("api/quest/accept", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>()
                ?? new ApiResponse<bool> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting quest {QuestId} for character {CharacterId}", questId, characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 更新任务进度
    /// </summary>
    public async Task<ApiResponse<bool>> UpdateQuestProgressAsync(string characterId, string questId, string objectiveId, int progress)
    {
        try
        {
            var request = new UpdateQuestProgressRequest
            {
                CharacterId = characterId,
                QuestId = questId,
                ObjectiveId = objectiveId,
                Progress = progress
            };

            var response = await _httpClient.PostAsJsonAsync("api/quest/progress", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>()
                ?? new ApiResponse<bool> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quest progress for character {CharacterId}", characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 完成任务
    /// </summary>
    public async Task<ApiResponse<List<QuestRewardDto>>> CompleteQuestAsync(string characterId, string questId)
    {
        try
        {
            var request = new CompleteQuestRequest
            {
                CharacterId = characterId,
                QuestId = questId
            };

            var response = await _httpClient.PostAsJsonAsync("api/quest/complete", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<List<QuestRewardDto>>>()
                ?? new ApiResponse<List<QuestRewardDto>> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing quest {QuestId} for character {CharacterId}", questId, characterId);
            return new ApiResponse<List<QuestRewardDto>>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 同步任务状态到服务器
    /// </summary>
    public async Task<ApiResponse<bool>> SyncQuestStatusAsync(CharacterQuestStatusDto status)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/quest/sync", status);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>()
                ?? new ApiResponse<bool> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing quest status for character {CharacterId}", status.CharacterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }
}