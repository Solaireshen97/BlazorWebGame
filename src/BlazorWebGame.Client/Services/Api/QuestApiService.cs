using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 任务系统API服务实现
/// </summary>
public class QuestApiService : BaseApiService
{
    public QuestApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger<QuestApiService> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<ApiResponse<CharacterQuestStatusDto>> GetQuestStatusAsync(string characterId)
    {
        return await GetAsync<CharacterQuestStatusDto>($"api/quest/status/{characterId}");
    }

    public async Task<ApiResponse<List<QuestDto>>> GetDailyQuestsAsync(string characterId)
    {
        return await GetAsync<List<QuestDto>>($"api/quest/daily/{characterId}");
    }

    public async Task<ApiResponse<List<QuestDto>>> GetWeeklyQuestsAsync(string characterId)
    {
        return await GetAsync<List<QuestDto>>($"api/quest/weekly/{characterId}");
    }

    public async Task<ApiResponse<bool>> AcceptQuestAsync(AcceptQuestRequest request)
    {
        return await PostAsync<bool>("api/quest/accept", request);
    }

    public async Task<ApiResponse<bool>> UpdateQuestProgressAsync(UpdateQuestProgressRequest request)
    {
        return await PostAsync<bool>("api/quest/progress", request);
    }

    public async Task<ApiResponse<List<QuestRewardDto>>> CompleteQuestAsync(CompleteQuestRequest request)
    {
        return await PostAsync<List<QuestRewardDto>>("api/quest/complete", request);
    }
}