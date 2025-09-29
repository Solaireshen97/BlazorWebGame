using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Quests;
using BlazorWebGame.Services;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services;

/// <summary>
/// 简化任务服务 - 现在只使用服务器API
/// </summary>
public class HybridQuestService
{
    private readonly ClientQuestApiService _questApi;
    private readonly QuestService _legacyQuestService; // 仅用于UI兼容性
    private readonly ILogger<HybridQuestService> _logger;
    
    public event Action? OnQuestStatusChanged;

    public HybridQuestService(
        ClientQuestApiService questApi,
        QuestService legacyQuestService,
        ILogger<HybridQuestService> logger)
    {
        _questApi = questApi;
        _legacyQuestService = legacyQuestService;
        _logger = logger;
        
        // 订阅客户端服务事件（用于UI兼容性）
        _legacyQuestService.OnStateChanged += () => OnQuestStatusChanged?.Invoke();
    }

    /// <summary>
    /// 设置任务系统模式 - 现在总是使用服务端
    /// </summary>
    [Obsolete("任务系统现在总是使用服务器模式")]
    public void SetUseServerQuests(bool useServer)
    {
        _logger.LogInformation("Quest system is now always in server mode");
    }

    /// <summary>
    /// 获取每日任务 - 现在只从服务器获取
    /// </summary>
    public async Task<List<QuestDto>> GetDailyQuestsAsync(string characterId)
    {
        try
        {
            var response = await _questApi.GetDailyQuestsAsync(characterId);
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }
            else
            {
                _logger.LogWarning("Failed to get daily quests from server: {Message}", response.Message);
                return new List<QuestDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily quests from server");
            return new List<QuestDto>();
        }
    }

    /// <summary>
    /// 获取周任务 - 现在只从服务器获取
    /// </summary>
    public async Task<List<QuestDto>> GetWeeklyQuestsAsync(string characterId)
    {
        try
        {
            var response = await _questApi.GetWeeklyQuestsAsync(characterId);
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }
            else
            {
                _logger.LogWarning("Failed to get weekly quests from server: {Message}", response.Message);
                return new List<QuestDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weekly quests from server");
            return new List<QuestDto>();
        }
    }

    /// <summary>
    /// 完成任务 - 现在只通过服务器API
    /// </summary>
    public async Task<bool> CompleteQuestAsync(string characterId, string questId)
    {
        try
        {
            var response = await _questApi.CompleteQuestAsync(characterId, questId);
            if (response.Success)
            {
                OnQuestStatusChanged?.Invoke();
                // 返回是否有奖励，表示任务完成成功
                return response.Data != null && response.Data.Count > 0;
            }
            else
            {
                _logger.LogWarning("Failed to complete quest via server: {Message}", response.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing quest via server");
            return false;
        }
    }

    /// <summary>
    /// 更新任务进度 - 现在只通过服务器API
    /// </summary>
    public async Task<bool> UpdateQuestProgressAsync(string characterId, string questId, int progress)
    {
        try
        {
            // 需要添加 objectiveId 参数，这里使用默认值
            var response = await _questApi.UpdateQuestProgressAsync(characterId, questId, "default", progress);
            if (response.Success)
            {
                OnQuestStatusChanged?.Invoke();
                return response.Data;
            }
            else
            {
                _logger.LogWarning("Failed to update quest progress via server: {Message}", response.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quest progress via server");
            return false;
        }
    }

    // 保留一些同步方法用于UI兼容性（标记为过时）
    [Obsolete("请使用异步版本 CompleteQuestAsync")]
    public void CompleteQuest(Player character, string questId)
    {
        _ = Task.Run(async () => await CompleteQuestAsync(character.Id, questId));
    }

    [Obsolete("请使用异步版本 UpdateQuestProgressAsync")]
    public void UpdateQuestProgress(Player character, QuestType questType, string objectiveId, int amount)
    {
        _logger.LogWarning("Synchronous quest progress update is obsolete - use server API");
    }

    [Obsolete("任务生成现在由服务器处理")]
    public void GenerateDailyQuests()
    {
        _logger.LogWarning("Quest generation is now handled by server");
    }

    [Obsolete("任务生成现在由服务器处理")]
    public void GenerateWeeklyQuests()
    {
        _logger.LogWarning("Quest generation is now handled by server");
    }
}
