using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorWebGame.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Services.Activities;

/// <summary>
/// 循环活动处理器 - 处理采集和生产等基于周期的活动
/// 实现设计文档中的"k次完成数+余数"模式
/// </summary>
public class RecurringActivityProcessor : IOfflineActivityProcessor
{
    private readonly ActivityType _activityType;
    private readonly ILogger _logger;
    
    // 活动配置
    private readonly Dictionary<ActivityType, ActivityConfig> _activityConfigs = new()
    {
        [ActivityType.Gathering] = new ActivityConfig
        {
            BaseCycleDuration = TimeSpan.FromMinutes(3), // 基础采集周期3分钟
            BaseExperienceReward = 25,
            BaseGoldReward = 8,
            BaseResourceReward = 2,
            EfficiencyBonus = 0.05,
            MaxEfficiency = 2.0,
            ResourceName = "采集资源"
        },
        [ActivityType.Production] = new ActivityConfig
        {
            BaseCycleDuration = TimeSpan.FromMinutes(5), // 基础制作周期5分钟
            BaseExperienceReward = 40,
            BaseGoldReward = 15,
            BaseResourceReward = 1,
            EfficiencyBonus = 0.08,
            MaxEfficiency = 2.5,
            ResourceName = "制作产品"
        }
    };

    public RecurringActivityProcessor(ActivityType activityType, ILogger logger)
    {
        _activityType = activityType;
        _logger = logger;
    }

    public string GetActivityName() => _activityType.ToString();

    public TimeSpan GetBaseCycleDuration(PlayerStorageDto player)
    {
        var config = _activityConfigs[_activityType];
        var efficiency = CalculateActivityEfficiency(player);
        
        // 效率提高减少周期时间
        var adjustedDuration = config.BaseCycleDuration.TotalSeconds / efficiency;
        return TimeSpan.FromSeconds(Math.Max(30, adjustedDuration)); // 最少30秒
    }

    /// <summary>
    /// 批量处理循环活动段落 - 使用k次完成公式
    /// </summary>
    public async Task<OfflineActivityResult> ProcessBulkSegmentsAsync(
        PlayerStorageDto player, 
        TimeSpan segmentDuration, 
        int segmentCount)
    {
        var result = new OfflineActivityResult
        {
            Success = true,
            PlayerId = player.Id,
            ActivityType = _activityType.ToString()
        };

        try
        {
            var config = _activityConfigs[_activityType];
            var efficiency = CalculateActivityEfficiency(player);
            var adjustedCycleDuration = GetBaseCycleDuration(player);
            
            var totalProcessingTime = TimeSpan.FromSeconds(segmentDuration.TotalSeconds * segmentCount);
            
            _logger.LogInformation("批量处理 {ActivityType} 活动：玩家 {PlayerId}，{SegmentCount} 段，总时长 {TotalTime}，效率 {Efficiency:F2}",
                _activityType, SafeLogId(player.Id), segmentCount, totalProcessingTime, efficiency);

            // 计算完成的周期数（k次完成）
            var totalCycles = (int)(totalProcessingTime.TotalSeconds / adjustedCycleDuration.TotalSeconds);
            
            if (totalCycles > 0)
            {
                // 批量计算奖励（使用公式避免循环）
                var totalExperience = totalCycles * (int)(config.BaseExperienceReward * efficiency);
                var totalGold = totalCycles * (int)(config.BaseGoldReward * efficiency);
                var totalResources = totalCycles * (int)(config.BaseResourceReward * efficiency);

                result.TotalExperience = totalExperience;
                result.TotalGold = totalGold;

                // 添加奖励记录
                result.Rewards.Add(new OfflineRewardDto
                {
                    Type = _activityType.ToString(),
                    Description = $"离线{config.ResourceName} {totalCycles} 次",
                    Experience = totalExperience,
                    Gold = totalGold,
                    Items = GenerateResourceItems(totalResources),
                    AdditionalRewards = new Dictionary<string, object>
                    {
                        ["CycleCount"] = totalCycles,
                        ["Efficiency"] = efficiency,
                        ["ResourcesGained"] = totalResources,
                        ["AverageCycleTime"] = adjustedCycleDuration.TotalSeconds
                    }
                });

                // 记录活动数据
                result.AdditionalData["CompletedCycles"] = totalCycles;
                result.AdditionalData["TotalResources"] = totalResources;
                result.AdditionalData["EfficiencyUsed"] = efficiency;
                result.AdditionalData["ProcessingMode"] = "BulkFormula";

                _logger.LogInformation("{ActivityType} 批量处理完成：玩家 {PlayerId}，完成 {Cycles} 个周期，获得 {Experience} 经验，{Gold} 金币",
                    _activityType, SafeLogId(player.Id), totalCycles, totalExperience, totalGold);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量处理 {ActivityType} 活动时发生错误：玩家 {PlayerId}", _activityType, SafeLogId(player.Id));
            result.Success = false;
            result.Message = $"{_activityType} 活动处理失败: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 处理余数时间 - 精确的进度计算
    /// </summary>
    public async Task<OfflineActivityResult> ProcessRemainingTimeAsync(
        PlayerStorageDto player, 
        TimeSpan remainingTime)
    {
        var result = new OfflineActivityResult
        {
            Success = true,
            PlayerId = player.Id,
            ActivityType = _activityType.ToString()
        };

        try
        {
            var config = _activityConfigs[_activityType];
            var efficiency = CalculateActivityEfficiency(player);
            var adjustedCycleDuration = GetBaseCycleDuration(player);

            _logger.LogDebug("处理 {ActivityType} 余数时间：玩家 {PlayerId}，余数时间 {RemainingTime}，周期时长 {CycleDuration}",
                _activityType, SafeLogId(player.Id), remainingTime, adjustedCycleDuration);

            // 检查是否能完成至少一个周期
            var completeCycles = (int)(remainingTime.TotalSeconds / adjustedCycleDuration.TotalSeconds);
            var partialTime = remainingTime - TimeSpan.FromSeconds(completeCycles * adjustedCycleDuration.TotalSeconds);

            if (completeCycles > 0)
            {
                // 完整周期奖励
                var cycleExperience = completeCycles * (int)(config.BaseExperienceReward * efficiency);
                var cycleGold = completeCycles * (int)(config.BaseGoldReward * efficiency);
                var cycleResources = completeCycles * (int)(config.BaseResourceReward * efficiency);

                result.TotalExperience += cycleExperience;
                result.TotalGold += cycleGold;

                result.Rewards.Add(new OfflineRewardDto
                {
                    Type = $"{_activityType}Remainder",
                    Description = $"余数时间完成 {completeCycles} 个{config.ResourceName}周期",
                    Experience = cycleExperience,
                    Gold = cycleGold,
                    Items = GenerateResourceItems(cycleResources)
                });

                result.AdditionalData["RemainderCompleteCycles"] = completeCycles;
            }

            // 计算部分进度
            if (partialTime.TotalSeconds > 0)
            {
                var partialProgress = partialTime.TotalSeconds / adjustedCycleDuration.TotalSeconds;
                result.AdditionalData["PartialProgress"] = partialProgress;
                
                _logger.LogDebug("部分进度：{Progress:P2}，下次完成还需 {NextCycleTime}",
                    partialProgress, adjustedCycleDuration - partialTime);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 {ActivityType} 余数时间时发生错误：玩家 {PlayerId}", _activityType, SafeLogId(player.Id));
            result.Success = false;
            result.Message = $"{_activityType} 余数处理失败: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 计算活动效率
    /// </summary>
    private double CalculateActivityEfficiency(PlayerStorageDto player)
    {
        var config = _activityConfigs[_activityType];
        
        // 基础效率
        var baseEfficiency = 1.0;
        
        // 等级加成
        var levelBonus = player.Level * config.EfficiencyBonus;
        
        // 职业加成
        var professionBonus = GetProfessionBonus(player.SelectedBattleProfession);
        
        // 装备加成（这里可以扩展）
        var equipmentBonus = 0.0;
        
        var totalEfficiency = baseEfficiency + levelBonus + professionBonus + equipmentBonus;
        
        // 限制最大效率
        return Math.Min(config.MaxEfficiency, totalEfficiency);
    }

    /// <summary>
    /// 获取职业加成
    /// </summary>
    private double GetProfessionBonus(string profession)
    {
        return _activityType switch
        {
            ActivityType.Gathering => profession.ToLower() switch
            {
                "ranger" or "druid" => 0.2,
                "warrior" => 0.1,
                _ => 0.0
            },
            ActivityType.Production => profession.ToLower() switch
            {
                "engineer" or "smith" => 0.25,
                "mage" or "alchemist" => 0.15,
                _ => 0.0
            },
            _ => 0.0
        };
    }

    /// <summary>
    /// 生成资源物品列表
    /// </summary>
    private List<string> GenerateResourceItems(int count)
    {
        if (count <= 0) return new List<string>();

        var items = new List<string>();
        var config = _activityConfigs[_activityType];

        // 根据活动类型生成不同的资源
        var resourceTypes = _activityType switch
        {
            ActivityType.Gathering => new[] { "木材", "矿石", "草药", "皮革" },
            ActivityType.Production => new[] { "工具", "药剂", "装备", "消耗品" },
            _ => new[] { "通用资源" }
        };

        var random = new Random();
        for (int i = 0; i < count; i++)
        {
            var resourceType = resourceTypes[random.Next(resourceTypes.Length)];
            var quality = random.NextDouble() switch
            {
                > 0.95 => "传说",
                > 0.85 => "史诗",
                > 0.70 => "稀有",
                > 0.40 => "优秀",
                _ => "普通"
            };
            
            items.Add($"{quality}{resourceType}");
        }

        return items;
    }

    /// <summary>
    /// 检查资源约束（可扩展）
    /// </summary>
    private bool CheckResourceConstraints(PlayerStorageDto player, int requiredCycles)
    {
        // 这里可以检查背包空间、材料消耗等约束
        // 例如：检查背包是否有足够空间存放产出
        // 或者检查是否有足够的材料进行生产
        
        return true; // 简化实现，总是返回 true
    }

    /// <summary>
    /// 处理资源枯竭或材料不足的情况
    /// </summary>
    private async Task<OfflineActivityResult> HandleResourceConstraints(
        PlayerStorageDto player, 
        TimeSpan availableTime)
    {
        var result = new OfflineActivityResult
        {
            Success = true,
            PlayerId = player.Id,
            ActivityType = _activityType.ToString()
        };

        // 实现资源约束处理逻辑
        // 例如：自动切换到其他活动或进入空闲状态
        
        result.Warnings.Add($"{_activityType} 活动受到资源约束限制");
        _logger.LogWarning("玩家 {PlayerId} 的 {ActivityType} 活动受到资源约束", SafeLogId(player.Id), _activityType);

        return result;
    }

    private static string SafeLogId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "[empty]";
        
        var sanitized = new string(id.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return sanitized.Substring(0, Math.Min(8, sanitized.Length)) + (sanitized.Length > 8 ? "..." : "");
    }
}

/// <summary>
/// 活动配置
/// </summary>
public class ActivityConfig
{
    public TimeSpan BaseCycleDuration { get; set; }
    public int BaseExperienceReward { get; set; }
    public int BaseGoldReward { get; set; }
    public int BaseResourceReward { get; set; }
    public double EfficiencyBonus { get; set; }
    public double MaxEfficiency { get; set; }
    public string ResourceName { get; set; } = string.Empty;
}

/// <summary>
/// 空闲活动处理器 - 用于无特定活动时的基础收益
/// </summary>
public class IdleActivityProcessor : IOfflineActivityProcessor
{
    private readonly ILogger _logger;
    private readonly double _baseIdleExperiencePerHour = 10.0;
    private readonly double _baseIdleGoldPerHour = 5.0;

    public IdleActivityProcessor(ILogger logger)
    {
        _logger = logger;
    }

    public string GetActivityName() => "Idle";

    public TimeSpan GetBaseCycleDuration(PlayerStorageDto player)
    {
        return TimeSpan.FromHours(1); // 空闲状态每小时结算
    }

    public async Task<OfflineActivityResult> ProcessBulkSegmentsAsync(
        PlayerStorageDto player, 
        TimeSpan segmentDuration, 
        int segmentCount)
    {
        var totalHours = segmentDuration.TotalHours * segmentCount;
        var experience = (int)(totalHours * _baseIdleExperiencePerHour);
        var gold = (int)(totalHours * _baseIdleGoldPerHour);

        return new OfflineActivityResult
        {
            Success = true,
            PlayerId = player.Id,
            ActivityType = "Idle",
            TotalExperience = experience,
            TotalGold = gold,
            Rewards = new List<OfflineRewardDto>
            {
                new OfflineRewardDto
                {
                    Type = "Idle",
                    Description = $"离线休息 {totalHours:F1} 小时",
                    Experience = experience,
                    Gold = gold
                }
            }
        };
    }

    public async Task<OfflineActivityResult> ProcessRemainingTimeAsync(
        PlayerStorageDto player, 
        TimeSpan remainingTime)
    {
        var hours = remainingTime.TotalHours;
        var experience = (int)(hours * _baseIdleExperiencePerHour);
        var gold = (int)(hours * _baseIdleGoldPerHour);

        return new OfflineActivityResult
        {
            Success = true,
            PlayerId = player.Id,
            ActivityType = "Idle",
            TotalExperience = experience,
            TotalGold = gold,
            AdditionalData = new Dictionary<string, object>
            {
                ["IdleHours"] = hours,
                ["IdleMode"] = "Remainder"
            }
        };
    }
}