using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Quests;
using BlazorWebGame.Services;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services;

/// <summary>
/// 混合任务系统服务 - 支持逐步从客户端迁移到服务端
/// </summary>
public class HybridQuestService
{
    private readonly ClientQuestApiService _questApi;
    private readonly QuestService _legacyQuestService;
    private readonly ILogger<HybridQuestService> _logger;
    
    // 配置标志 - 是否使用服务端任务系统
    private bool _useServerQuests = true; // 默认使用服务端
    
    public event Action? OnQuestStatusChanged;

    public HybridQuestService(
        ClientQuestApiService questApi,
        QuestService legacyQuestService,
        ILogger<HybridQuestService> logger)
    {
        _questApi = questApi;
        _legacyQuestService = legacyQuestService;
        _logger = logger;
        
        // 订阅客户端服务事件
        _legacyQuestService.OnStateChanged += () => OnQuestStatusChanged?.Invoke();
    }

    /// <summary>
    /// 设置是否使用服务端任务系统
    /// </summary>
    public void SetUseServerQuests(bool useServer)
    {
        _useServerQuests = useServer;
        _logger.LogInformation("Quest system switched to {Mode}", useServer ? "Server" : "Client");
    }

    /// <summary>
    /// 获取每日任务
    /// </summary>
    public async Task<List<QuestDto>> GetDailyQuestsAsync(string characterId)
    {
        if (_useServerQuests)
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
                    // 降级到客户端模式
                    return ConvertQuestsToDto(_legacyQuestService.GetDailyQuests());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily quests from server, falling back to client");
                return ConvertQuestsToDto(_legacyQuestService.GetDailyQuests());
            }
        }
        else
        {
            return ConvertQuestsToDto(_legacyQuestService.GetDailyQuests());
        }
    }

    /// <summary>
    /// 获取每周任务
    /// </summary>
    public async Task<List<QuestDto>> GetWeeklyQuestsAsync(string characterId)
    {
        if (_useServerQuests)
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
                    // 降级到客户端模式
                    return ConvertQuestsToDto(_legacyQuestService.GetWeeklyQuests());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weekly quests from server, falling back to client");
                return ConvertQuestsToDto(_legacyQuestService.GetWeeklyQuests());
            }
        }
        else
        {
            return ConvertQuestsToDto(_legacyQuestService.GetWeeklyQuests());
        }
    }

    /// <summary>
    /// 获取角色任务状态
    /// </summary>
    public async Task<CharacterQuestStatusDto?> GetQuestStatusAsync(string characterId)
    {
        if (_useServerQuests)
        {
            try
            {
                var response = await _questApi.GetQuestStatusAsync(characterId);
                if (response.Success && response.Data != null)
                {
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to get quest status from server: {Message}", response.Message);
                    // 降级到客户端模式
                    return CreateClientQuestStatus(characterId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quest status from server, falling back to client");
                return CreateClientQuestStatus(characterId);
            }
        }
        else
        {
            return CreateClientQuestStatus(characterId);
        }
    }

    /// <summary>
    /// 接受任务
    /// </summary>
    public async Task<bool> AcceptQuestAsync(Player character, string questId)
    {
        if (_useServerQuests)
        {
            try
            {
                var response = await _questApi.AcceptQuestAsync(character.Id, questId);
                if (response.Success)
                {
                    OnQuestStatusChanged?.Invoke();
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to accept quest via server: {Message}", response.Message);
                    // 降级到客户端模式
                    // 客户端版本暂时不支持接受任务
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting quest via server, falling back to client");
                return false;
            }
        }
        else
        {
            // 客户端版本暂时不支持接受任务
            return false;
        }
    }

    /// <summary>
    /// 更新任务进度
    /// </summary>
    public async Task<bool> UpdateQuestProgressAsync(Player character, string questId, string objectiveId, int progress)
    {
        if (_useServerQuests)
        {
            try
            {
                var response = await _questApi.UpdateQuestProgressAsync(character.Id, questId, objectiveId, progress);
                if (response.Success)
                {
                    OnQuestStatusChanged?.Invoke();
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to update quest progress via server: {Message}", response.Message);
                    // 降级到客户端模式 - 暂时不做任何操作
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quest progress via server");
                return false;
            }
        }
        else
        {
            // 客户端版本暂时不支持更新任务进度
            return false;
        }
    }

    /// <summary>
    /// 完成任务
    /// </summary>
    public async Task<List<QuestRewardDto>?> CompleteQuestAsync(Player character, string questId)
    {
        if (_useServerQuests)
        {
            try
            {
                var response = await _questApi.CompleteQuestAsync(character.Id, questId);
                if (response.Success && response.Data != null)
                {
                    OnQuestStatusChanged?.Invoke();
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to complete quest via server: {Message}", response.Message);
                    // 降级到客户端模式
                    _legacyQuestService.TryCompleteQuest(character, questId);
                    return new List<QuestRewardDto>(); // 客户端版本不返回详细奖励
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing quest via server, falling back to client");
                _legacyQuestService.TryCompleteQuest(character, questId);
                return new List<QuestRewardDto>();
            }
        }
        else
        {
            _legacyQuestService.TryCompleteQuest(character, questId);
            return new List<QuestRewardDto>();
        }
    }

    /// <summary>
    /// 同步客户端任务状态到服务端
    /// </summary>
    public async Task<bool> SyncToServerAsync(Player character)
    {
        if (!_useServerQuests) return false;

        try
        {
            // 将客户端任务状态转换为DTO
            var statusDto = CreateClientQuestStatus(character.Id);
            if (statusDto == null) return false;

            var response = await _questApi.SyncQuestStatusAsync(statusDto);
            
            if (response.Success)
            {
                _logger.LogInformation("Successfully synced quest status to server for character {CharacterId}", character.Id);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to sync quest status to server: {Message}", response.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing quest status to server");
            return false;
        }
    }

    /// <summary>
    /// 将Quest对象转换为QuestDto
    /// </summary>
    private List<QuestDto> ConvertQuestsToDto(List<Quest> quests)
    {
        return quests.Select(q => new QuestDto
        {
            Id = q.Id,
            Name = q.Name,
            Description = q.Description,
            Type = q.Type.ToString(),
            Status = "Available", // 客户端版本默认为可用
            Objectives = q.Objectives?.Select(o => new QuestObjectiveDto
            {
                Id = o.Id,
                Description = o.Description,
                Type = o.Type.ToString(),
                TargetId = o.TargetId,
                RequiredCount = o.RequiredCount,
                CurrentCount = o.CurrentCount
            }).ToList() ?? new List<QuestObjectiveDto>(),
            Rewards = q.Rewards?.Select(r => new QuestRewardDto
            {
                Type = r.Type.ToString(),
                ItemId = r.ItemId,
                Amount = r.Amount
            }).ToList() ?? new List<QuestRewardDto>(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = q.Type == QuestType.Daily ? DateTime.UtcNow.AddDays(1) : DateTime.UtcNow.AddDays(7)
        }).ToList();
    }

    /// <summary>
    /// 从客户端数据创建任务状态
    /// </summary>
    private CharacterQuestStatusDto CreateClientQuestStatus(string characterId)
    {
        return new CharacterQuestStatusDto
        {
            CharacterId = characterId,
            ActiveQuests = new List<QuestDto>(), // 客户端版本暂时没有活跃任务追踪
            CompletedQuests = new List<QuestDto>(),
            AvailableQuests = ConvertQuestsToDto(_legacyQuestService.GetDailyQuests())
                .Concat(ConvertQuestsToDto(_legacyQuestService.GetWeeklyQuests())).ToList(),
            LastDailyReset = _legacyQuestService.LastDailyReset,
            LastWeeklyReset = _legacyQuestService.LastWeeklyReset
        };
    }

    /// <summary>
    /// 自动任务进度更新 - 用于在特定游戏事件后更新任务进度
    /// </summary>
    public async Task AutoUpdateQuestProgress(Player character, string eventType, string targetId, int amount = 1)
    {
        if (!_useServerQuests) return;

        try
        {
            // 获取当前任务状态
            var status = await GetQuestStatusAsync(character.Id);
            if (status?.ActiveQuests == null) return;

            // 查找匹配的任务目标
            foreach (var quest in status.ActiveQuests)
            {
                foreach (var objective in quest.Objectives)
                {
                    if (objective.Type.Equals(eventType, StringComparison.OrdinalIgnoreCase) &&
                        (objective.TargetId == "any" || objective.TargetId.Equals(targetId, StringComparison.OrdinalIgnoreCase)))
                    {
                        await UpdateQuestProgressAsync(character, quest.Id, objective.Id, amount);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-updating quest progress for character {CharacterId}", character.Id);
        }
    }
}