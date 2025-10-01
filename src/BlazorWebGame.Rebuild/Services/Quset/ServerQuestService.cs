using BlazorWebGame.Rebuild.Services.Inventory;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Rebuild.Services.Quset;

/// <summary>
/// 服务端任务系统服务 - 管理所有角色的任务状态
/// </summary>
public class ServerQuestService
{
    private readonly ILogger<ServerQuestService> _logger;
    private readonly ServerInventoryService _inventoryService;
    private readonly Dictionary<string, CharacterQuestStatusDto> _characterQuests = new();
    
    public event Action<string>? OnQuestStatusChanged; // characterId

    public ServerQuestService(ILogger<ServerQuestService> logger, ServerInventoryService inventoryService)
    {
        _logger = logger;
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// 获取角色的任务状态
    /// </summary>
    public CharacterQuestStatusDto GetCharacterQuestStatus(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return new CharacterQuestStatusDto { CharacterId = characterId };

        if (!_characterQuests.ContainsKey(characterId))
        {
            _characterQuests[characterId] = CreateDefaultQuestStatus(characterId);
        }

        return _characterQuests[characterId];
    }

    /// <summary>
    /// 获取可用的每日任务
    /// </summary>
    public async Task<ApiResponse<List<QuestDto>>> GetDailyQuestsAsync(string characterId)
    {
        try
        {
            var questStatus = GetCharacterQuestStatus(characterId);
            
            // 检查是否需要重置每日任务
            if (ShouldResetDailyQuests(questStatus.LastDailyReset))
            {
                await ResetDailyQuestsAsync(characterId);
                questStatus = GetCharacterQuestStatus(characterId);
            }

            var availableQuests = questStatus.AvailableQuests
                .Where(q => q.Type == "Daily")
                .ToList();

            return new ApiResponse<List<QuestDto>>
            {
                Success = true,
                Data = availableQuests
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily quests for character {CharacterId}", characterId);
            return new ApiResponse<List<QuestDto>>
            {
                Success = false,
                Message = "获取每日任务时发生错误"
            };
        }
    }

    /// <summary>
    /// 获取可用的每周任务
    /// </summary>
    public async Task<ApiResponse<List<QuestDto>>> GetWeeklyQuestsAsync(string characterId)
    {
        try
        {
            var questStatus = GetCharacterQuestStatus(characterId);
            
            // 检查是否需要重置每周任务
            if (ShouldResetWeeklyQuests(questStatus.LastWeeklyReset))
            {
                await ResetWeeklyQuestsAsync(characterId);
                questStatus = GetCharacterQuestStatus(characterId);
            }

            var availableQuests = questStatus.AvailableQuests
                .Where(q => q.Type == "Weekly")
                .ToList();

            return new ApiResponse<List<QuestDto>>
            {
                Success = true,
                Data = availableQuests
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weekly quests for character {CharacterId}", characterId);
            return new ApiResponse<List<QuestDto>>
            {
                Success = false,
                Message = "获取每周任务时发生错误"
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
            var questStatus = GetCharacterQuestStatus(characterId);
            var quest = questStatus.AvailableQuests.FirstOrDefault(q => q.Id == questId);
            
            if (quest == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "任务不存在或不可接受"
                };
            }

            // 检查是否已经有太多活跃任务
            if (questStatus.ActiveQuests.Count >= 10) // 假设最多10个活跃任务
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "活跃任务数量已达上限"
                };
            }

            // 移动任务从可用列表到活跃列表
            questStatus.AvailableQuests.Remove(quest);
            quest.Status = "Active";
            questStatus.ActiveQuests.Add(quest);

            OnQuestStatusChanged?.Invoke(characterId);
            _logger.LogInformation("Character {CharacterId} accepted quest {QuestId}", characterId, questId);

            return new ApiResponse<bool> { Success = true, Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting quest {QuestId} for character {CharacterId}", questId, characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "接受任务时发生错误"
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
            var questStatus = GetCharacterQuestStatus(characterId);
            var quest = questStatus.ActiveQuests.FirstOrDefault(q => q.Id == questId);
            
            if (quest == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "任务不存在或未激活"
                };
            }

            var objective = quest.Objectives.FirstOrDefault(o => o.Id == objectiveId);
            if (objective == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "任务目标不存在"
                };
            }

            objective.CurrentCount = Math.Min(objective.CurrentCount + progress, objective.RequiredCount);

            // 检查任务是否可以完成
            bool canComplete = quest.Objectives.All(o => o.IsCompleted);
            if (canComplete && quest.Status == "Active")
            {
                quest.Status = "ReadyToComplete";
            }

            OnQuestStatusChanged?.Invoke(characterId);
            _logger.LogInformation("Updated quest progress for character {CharacterId}, quest {QuestId}, objective {ObjectiveId}", 
                characterId, questId, objectiveId);

            return new ApiResponse<bool> { Success = true, Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quest progress for character {CharacterId}", characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "更新任务进度时发生错误"
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
            var questStatus = GetCharacterQuestStatus(characterId);
            var quest = questStatus.ActiveQuests.FirstOrDefault(q => q.Id == questId);
            
            if (quest == null)
            {
                return new ApiResponse<List<QuestRewardDto>>
                {
                    Success = false,
                    Message = "任务不存在或未激活"
                };
            }

            // 检查任务是否满足完成条件
            bool canComplete = quest.Objectives.All(o => o.IsCompleted);
            if (!canComplete)
            {
                return new ApiResponse<List<QuestRewardDto>>
                {
                    Success = false,
                    Message = "任务条件未满足"
                };
            }

            // 发放奖励
            var rewards = new List<QuestRewardDto>();
            foreach (var reward in quest.Rewards)
            {
                await ProcessRewardAsync(characterId, reward);
                rewards.Add(reward);
            }

            // 标记任务为完成
            quest.Status = "Completed";
            quest.CompletedAt = DateTime.UtcNow;
            
            // 移动到完成列表
            questStatus.ActiveQuests.Remove(quest);
            questStatus.CompletedQuests.Add(quest);

            OnQuestStatusChanged?.Invoke(characterId);
            _logger.LogInformation("Character {CharacterId} completed quest {QuestId}", characterId, questId);

            return new ApiResponse<List<QuestRewardDto>>
            {
                Success = true,
                Data = rewards,
                Message = "任务完成！"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing quest {QuestId} for character {CharacterId}", questId, characterId);
            return new ApiResponse<List<QuestRewardDto>>
            {
                Success = false,
                Message = "完成任务时发生错误"
            };
        }
    }

    /// <summary>
    /// 重置每日任务
    /// </summary>
    private async Task ResetDailyQuestsAsync(string characterId)
    {
        var questStatus = GetCharacterQuestStatus(characterId);
        
        // 清除已完成的每日任务
        questStatus.CompletedQuests.RemoveAll(q => q.Type == "Daily");
        
        // 重新生成每日任务
        questStatus.AvailableQuests.RemoveAll(q => q.Type == "Daily");
        var newDailyQuests = GenerateDailyQuests();
        questStatus.AvailableQuests.AddRange(newDailyQuests);
        
        questStatus.LastDailyReset = DateTime.UtcNow;
        
        _logger.LogInformation("Reset daily quests for character {CharacterId}", characterId);
    }

    /// <summary>
    /// 重置每周任务
    /// </summary>
    private async Task ResetWeeklyQuestsAsync(string characterId)
    {
        var questStatus = GetCharacterQuestStatus(characterId);
        
        // 清除已完成的每周任务
        questStatus.CompletedQuests.RemoveAll(q => q.Type == "Weekly");
        
        // 重新生成每周任务
        questStatus.AvailableQuests.RemoveAll(q => q.Type == "Weekly");
        var newWeeklyQuests = GenerateWeeklyQuests();
        questStatus.AvailableQuests.AddRange(newWeeklyQuests);
        
        questStatus.LastWeeklyReset = DateTime.UtcNow;
        
        _logger.LogInformation("Reset weekly quests for character {CharacterId}", characterId);
    }

    /// <summary>
    /// 处理任务奖励
    /// </summary>
    private async Task ProcessRewardAsync(string characterId, QuestRewardDto reward)
    {
        switch (reward.Type.ToLower())
        {
            case "item":
                if (!string.IsNullOrEmpty(reward.ItemId))
                {
                    await _inventoryService.AddItemAsync(characterId, reward.ItemId, reward.Amount);
                }
                break;
            case "gold":
                // 这里应该调用角色服务来增加金币
                // await _characterService.AddGoldAsync(characterId, reward.Amount);
                _logger.LogInformation("Awarded {Amount} gold to character {CharacterId}", reward.Amount, characterId);
                break;
            case "experience":
                // 这里应该调用经验服务来增加经验
                // await _experienceService.AddExperienceAsync(characterId, reward.ProfessionType, reward.Amount);
                _logger.LogInformation("Awarded {Amount} experience to character {CharacterId}", reward.Amount, characterId);
                break;
        }
    }

    /// <summary>
    /// 创建默认任务状态
    /// </summary>
    private CharacterQuestStatusDto CreateDefaultQuestStatus(string characterId)
    {
        var status = new CharacterQuestStatusDto
        {
            CharacterId = characterId,
            LastDailyReset = DateTime.UtcNow,
            LastWeeklyReset = DateTime.UtcNow
        };

        // 生成初始任务
        status.AvailableQuests.AddRange(GenerateDailyQuests());
        status.AvailableQuests.AddRange(GenerateWeeklyQuests());

        return status;
    }

    /// <summary>
    /// 生成每日任务
    /// </summary>
    private List<QuestDto> GenerateDailyQuests()
    {
        var quests = new List<QuestDto>();
        var random = new Random();

        // 杀怪任务
        quests.Add(new QuestDto
        {
            Id = $"daily_kill_{DateTime.UtcNow:yyyyMMdd}_{random.Next(1000)}",
            Name = "清理威胁",
            Description = "击败10只怪物来保护村民",
            Type = "Daily",
            Status = "Available",
            Objectives = new List<QuestObjectiveDto>
            {
                new QuestObjectiveDto
                {
                    Id = "kill_monsters",
                    Description = "击败怪物",
                    Type = "Kill",
                    TargetId = "any",
                    RequiredCount = 10,
                    CurrentCount = 0
                }
            },
            Rewards = new List<QuestRewardDto>
            {
                new QuestRewardDto { Type = "Experience", Amount = 100 },
                new QuestRewardDto { Type = "Gold", Amount = 50 }
            },
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });

        // 采集任务
        quests.Add(new QuestDto
        {
            Id = $"daily_gather_{DateTime.UtcNow:yyyyMMdd}_{random.Next(1000)}",
            Name = "收集资源",
            Description = "采集5个木材来帮助建设",
            Type = "Daily",
            Status = "Available",
            Objectives = new List<QuestObjectiveDto>
            {
                new QuestObjectiveDto
                {
                    Id = "gather_wood",
                    Description = "采集木材",
                    Type = "Gather",
                    TargetId = "wood",
                    RequiredCount = 5,
                    CurrentCount = 0
                }
            },
            Rewards = new List<QuestRewardDto>
            {
                new QuestRewardDto { Type = "Experience", Amount = 75, ProfessionType = "Gathering" },
                new QuestRewardDto { Type = "Item", ItemId = "gathering_tool", Amount = 1 }
            },
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });

        return quests;
    }

    /// <summary>
    /// 生成每周任务
    /// </summary>
    private List<QuestDto> GenerateWeeklyQuests()
    {
        var quests = new List<QuestDto>();
        var random = new Random();

        // 挑战任务
        quests.Add(new QuestDto
        {
            Id = $"weekly_challenge_{DateTime.UtcNow:yyyyMMdd}_{random.Next(1000)}",
            Name = "周挑战",
            Description = "完成一项艰难的挑战来证明你的实力",
            Type = "Weekly",
            Status = "Available",
            Objectives = new List<QuestObjectiveDto>
            {
                new QuestObjectiveDto
                {
                    Id = "defeat_boss",
                    Description = "击败1个强大的敌人",
                    Type = "Kill",
                    TargetId = "boss",
                    RequiredCount = 1,
                    CurrentCount = 0
                }
            },
            Rewards = new List<QuestRewardDto>
            {
                new QuestRewardDto { Type = "Experience", Amount = 500 },
                new QuestRewardDto { Type = "Gold", Amount = 200 },
                new QuestRewardDto { Type = "Item", ItemId = "rare_equipment", Amount = 1 }
            },
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        return quests;
    }

    /// <summary>
    /// 检查是否需要重置每日任务
    /// </summary>
    private bool ShouldResetDailyQuests(DateTime lastReset)
    {
        return DateTime.UtcNow.Date > lastReset.Date;
    }

    /// <summary>
    /// 检查是否需要重置每周任务
    /// </summary>
    private bool ShouldResetWeeklyQuests(DateTime lastReset)
    {
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        var lastResetWeekStart = lastReset.Date.AddDays(-(int)lastReset.DayOfWeek);
        return weekStart > lastResetWeekStart;
    }

    /// <summary>
    /// 同步客户端任务状态到服务端
    /// </summary>
    public async Task<ApiResponse<bool>> SyncQuestStatusAsync(string characterId, CharacterQuestStatusDto clientStatus)
    {
        try
        {
            _characterQuests[characterId] = clientStatus;
            OnQuestStatusChanged?.Invoke(characterId);
            
            _logger.LogInformation("Synced quest status for character {CharacterId}", characterId);
            return new ApiResponse<bool> { Success = true, Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing quest status for character {CharacterId}", characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "同步任务状态时发生错误"
            };
        }
    }
}