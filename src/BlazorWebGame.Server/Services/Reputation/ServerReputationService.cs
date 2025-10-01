using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BlazorWebGame.Server.Services.Data;

namespace BlazorWebGame.Server.Services.Reputation;

/// <summary>
/// 服务端声望管理服务
/// </summary>
public class ServerReputationService
{
    private readonly ILogger<ServerReputationService> _logger;
    private readonly DataStorageIntegrationService _dataIntegration;

    // 声望等级定义（从客户端 Player.cs 迁移）
    private static readonly List<ReputationTierInfo> ReputationTiers = new()
    {
        new("冷淡", 0, 1000, "bg-info"),
        new("友善", 1000, 3000, "bg-success"), 
        new("尊敬", 3000, 6000, "bg-primary"),
        new("崇拜", 6000, 6001, "bg-warning") // 崇拜是顶级
    };

    // 阵营名称映射
    private static readonly Dictionary<Faction, string> FactionNames = new()
    {
        { Faction.StormwindGuard, "StormwindGuard" },
        { Faction.IronforgeBrotherhood, "IronforgeBrotherhood" },
        { Faction.ArgentDawn, "ArgentDawn" }
    };

    public ServerReputationService(ILogger<ServerReputationService> logger, DataStorageIntegrationService dataIntegration)
    {
        _logger = logger;
        _dataIntegration = dataIntegration;
    }

    /// <summary>
    /// 获取角色声望信息
    /// </summary>
    public async Task<ReputationDto> GetReputationAsync(string characterId)
    {
        try
        {
            var character = await _dataIntegration.LoadPlayerFromStorageAsync(characterId);
            if (character == null)
            {
                throw new ArgumentException($"Character not found: {characterId}");
            }

            var reputationDto = new ReputationDto
            {
                CharacterId = characterId,
                FactionReputation = character.Reputation.ToDictionary(
                    kvp => GetFactionName(kvp.Key), 
                    kvp => kvp.Value
                ),
                LastUpdated = DateTime.UtcNow
            };

            return reputationDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation for character {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// 获取指定阵营的详细声望信息
    /// </summary>
    public async Task<ReputationDetailDto> GetReputationDetailAsync(string characterId, string factionName)
    {
        try
        {
            var character = await _dataIntegration.LoadPlayerFromStorageAsync(characterId);
            if (character == null)
            {
                throw new ArgumentException($"Character not found: {characterId}");
            }

            var faction = GetFactionFromName(factionName);
            var currentValue = character.Reputation.GetValueOrDefault(faction, 0);
            var currentTier = GetReputationTier(currentValue);
            var nextTier = GetNextTier(currentTier);
            var progressPercentage = CalculateProgressPercentage(currentValue, currentTier);

            return new ReputationDetailDto
            {
                CharacterId = characterId,
                FactionName = factionName,
                CurrentValue = currentValue,
                CurrentTier = new ReputationTierDto
                {
                    Name = currentTier.Name,
                    MinValue = currentTier.MinValue,
                    MaxValue = currentTier.MaxValue,
                    BarColorClass = currentTier.BarColorClass,
                    ProgressPercentage = progressPercentage
                },
                NextTier = nextTier != null ? new ReputationTierDto
                {
                    Name = nextTier.Name,
                    MinValue = nextTier.MinValue,
                    MaxValue = nextTier.MaxValue,
                    BarColorClass = nextTier.BarColorClass
                } : null,
                ProgressPercentage = progressPercentage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation detail for character {CharacterId}, faction {FactionName}", characterId, factionName);
            throw;
        }
    }

    /// <summary>
    /// 获取所有阵营的详细声望信息
    /// </summary>
    public async Task<List<ReputationDetailDto>> GetAllReputationDetailsAsync(string characterId)
    {
        var details = new List<ReputationDetailDto>();
        
        foreach (var faction in Enum.GetValues<Faction>())
        {
            var factionName = GetFactionName(faction);
            var detail = await GetReputationDetailAsync(characterId, factionName);
            details.Add(detail);
        }

        return details;
    }

    /// <summary>
    /// 更新角色声望
    /// </summary>
    public async Task<ReputationDto> UpdateReputationAsync(UpdateReputationRequest request)
    {
        try
        {
            var character = await _dataIntegration.LoadPlayerFromStorageAsync(request.CharacterId);
            if (character == null)
            {
                throw new ArgumentException($"Character not found: {request.CharacterId}");
            }

            var faction = GetFactionFromName(request.FactionName);
            var oldValue = character.Reputation.GetValueOrDefault(faction, 0);
            var newValue = Math.Max(0, oldValue + request.Amount); // 声望不能为负数

            character.Reputation[faction] = newValue;
            await _dataIntegration.SyncPlayerToStorageAsync(character);

            // 记录声望变更历史
            await RecordReputationHistory(request.CharacterId, request.FactionName, request.Amount, newValue, request.Reason);

            _logger.LogInformation("Updated reputation for character {CharacterId}, faction {FactionName}: {OldValue} -> {NewValue} ({Change})", 
                request.CharacterId, request.FactionName, oldValue, newValue, request.Amount);

            return await GetReputationAsync(request.CharacterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reputation for character {CharacterId}", request.CharacterId);
            throw;
        }
    }

    /// <summary>
    /// 批量更新角色声望
    /// </summary>
    public async Task<ReputationDto> BatchUpdateReputationAsync(BatchUpdateReputationRequest request)
    {
        try
        {
            var character = await _dataIntegration.LoadPlayerFromStorageAsync(request.CharacterId);
            if (character == null)
            {
                throw new ArgumentException($"Character not found: {request.CharacterId}");
            }

            foreach (var change in request.Changes)
            {
                var faction = GetFactionFromName(change.FactionName);
                var oldValue = character.Reputation.GetValueOrDefault(faction, 0);
                var newValue = Math.Max(0, oldValue + change.Amount);
                
                character.Reputation[faction] = newValue;

                // 记录每个变更
                await RecordReputationHistory(request.CharacterId, change.FactionName, change.Amount, newValue, request.Reason);
                
                _logger.LogInformation("Batch updated reputation for character {CharacterId}, faction {FactionName}: {OldValue} -> {NewValue} ({Change})", 
                    request.CharacterId, change.FactionName, oldValue, newValue, change.Amount);
            }

            await _dataIntegration.SyncPlayerToStorageAsync(character);
            return await GetReputationAsync(request.CharacterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch updating reputation for character {CharacterId}", request.CharacterId);
            throw;
        }
    }

    /// <summary>
    /// 获取声望奖励信息
    /// </summary>
    public async Task<List<ReputationRewardDto>> GetReputationRewardsAsync(ReputationRewardsRequest request)
    {
        var rewards = new List<ReputationRewardDto>();

        try
        {
            // 这里可以从配置文件或数据库加载奖励信息
            // 目前使用硬编码的示例奖励
            var factions = request.FactionName != null 
                ? new[] { GetFactionFromName(request.FactionName) }
                : Enum.GetValues<Faction>();

            foreach (var faction in factions)
            {
                var factionName = GetFactionName(faction);
                foreach (var tier in ReputationTiers)
                {
                    var reward = new ReputationRewardDto
                    {
                        FactionName = factionName,
                        TierName = tier.Name,
                        RequiredReputation = tier.MinValue,
                        Items = GetTierRewards(faction, tier),
                        Perks = GetTierPerks(faction, tier)
                    };
                    rewards.Add(reward);
                }
            }

            return rewards;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation rewards for character {CharacterId}", request.CharacterId);
            throw;
        }
    }

    /// <summary>
    /// 获取角色可获得的声望奖励
    /// </summary>
    public async Task<List<ReputationRewardDto>> GetAvailableRewardsAsync(string characterId)
    {
        try
        {
            var character = await _dataIntegration.LoadPlayerFromStorageAsync(characterId);
            if (character == null)
            {
                throw new ArgumentException($"Character not found: {characterId}");
            }

            var availableRewards = new List<ReputationRewardDto>();
            
            foreach (var faction in Enum.GetValues<Faction>())
            {
                var factionName = GetFactionName(faction);
                var currentRep = character.Reputation.GetValueOrDefault(faction, 0);
                var currentTier = GetReputationTier(currentRep);
                var nextTier = GetNextTier(currentTier);

                if (nextTier != null)
                {
                    var reward = new ReputationRewardDto
                    {
                        FactionName = factionName,
                        TierName = nextTier.Name,
                        RequiredReputation = nextTier.MinValue,
                        Items = GetTierRewards(faction, nextTier),
                        Perks = GetTierPerks(faction, nextTier)
                    };
                    availableRewards.Add(reward);
                }
            }

            return availableRewards;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available rewards for character {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// 获取声望统计信息
    /// </summary>
    public async Task<ReputationStatsDto> GetReputationStatsAsync(string characterId)
    {
        try
        {
            var character = await _dataIntegration.LoadPlayerFromStorageAsync(characterId);
            if (character == null)
            {
                throw new ArgumentException($"Character not found: {characterId}");
            }

            var totalEarned = character.Reputation.Values.Sum();
            var factionTotals = character.Reputation.ToDictionary(
                kvp => GetFactionName(kvp.Key), 
                kvp => kvp.Value
            );

            var highestEntry = character.Reputation.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
            var highestFaction = highestEntry.Key != default ? GetFactionName(highestEntry.Key) : string.Empty;
            var highestValue = highestEntry.Value;

            return new ReputationStatsDto
            {
                CharacterId = characterId,
                TotalReputationEarned = totalEarned,
                FactionTotals = factionTotals,
                HighestFaction = highestFaction,
                HighestReputationValue = highestValue,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation stats for character {CharacterId}", characterId);
            throw;
        }
    }

    #region 私有辅助方法

    private static string GetFactionName(Faction faction)
    {
        return FactionNames.TryGetValue(faction, out var name) ? name : faction.ToString();
    }

    private static Faction GetFactionFromName(string factionName)
    {
        var faction = FactionNames.FirstOrDefault(kvp => kvp.Value == factionName).Key;
        if (faction == default && Enum.TryParse<Faction>(factionName, out var parsedFaction))
        {
            faction = parsedFaction;
        }
        return faction;
    }

    private static ReputationTierInfo GetReputationTier(int reputation)
    {
        return ReputationTiers.LastOrDefault(t => reputation >= t.MinValue) ?? ReputationTiers.First();
    }

    private static ReputationTierInfo? GetNextTier(ReputationTierInfo currentTier)
    {
        var currentIndex = ReputationTiers.IndexOf(currentTier);
        return currentIndex >= 0 && currentIndex < ReputationTiers.Count - 1 
            ? ReputationTiers[currentIndex + 1] 
            : null;
    }

    private static double CalculateProgressPercentage(int reputation, ReputationTierInfo tier)
    {
        if (tier.MaxValue - tier.MinValue <= 1)
        {
            return 100.0;
        }

        var progressInTier = reputation - tier.MinValue;
        var totalForTier = tier.MaxValue - tier.MinValue;
        return Math.Max(0, Math.Min(100, (double)progressInTier / totalForTier * 100.0));
    }

    private static List<RewardItemDto> GetTierRewards(Faction faction, ReputationTierInfo tier)
    {
        // 这里可以从配置或数据库加载实际的奖励物品
        // 现在返回示例奖励
        return new List<RewardItemDto>
        {
            new()
            {
                ItemId = $"REP_{faction}_{tier.Name.ToUpper()}_001",
                ItemName = $"{tier.Name}徽章",
                Quantity = 1,
                Description = $"获得{GetFactionDisplayName(faction)} {tier.Name}声望时的奖励徽章"
            }
        };
    }

    private static List<string> GetTierPerks(Faction faction, ReputationTierInfo tier)
    {
        var perks = new List<string>();
        
        switch (tier.Name)
        {
            case "友善":
                perks.Add("商店折扣 5%");
                break;
            case "尊敬":
                perks.Add("商店折扣 10%");
                perks.Add("特殊任务访问");
                break;
            case "崇拜":
                perks.Add("商店折扣 15%");
                perks.Add("特殊任务访问");
                perks.Add("专属装备购买权");
                break;
        }

        return perks;
    }

    private static string GetFactionDisplayName(Faction faction)
    {
        return faction switch
        {
            Faction.StormwindGuard => "暴风城卫兵",
            Faction.IronforgeBrotherhood => "铁炉堡兄弟会",
            Faction.ArgentDawn => "银色黎明",
            _ => "未知势力"
        };
    }

    private async Task RecordReputationHistory(string characterId, string factionName, int amountChanged, int newTotal, string reason)
    {
        try
        {
            // 这里可以实现声望历史记录功能
            // 可以保存到数据库或日志文件中
            var historyEntry = new ReputationHistoryDto
            {
                Timestamp = DateTime.UtcNow,
                FactionName = factionName,
                AmountChanged = amountChanged,
                NewTotal = newTotal,
                Reason = reason,
                Source = "ServerReputationService"
            };

            _logger.LogInformation("Reputation history recorded for character {CharacterId}: {FactionName} {AmountChanged} -> {NewTotal} ({Reason})", 
                characterId, factionName, amountChanged, newTotal, reason);

            // 在实际实现中，这里应该保存到数据库
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording reputation history for character {CharacterId}", characterId);
        }
    }

    #endregion

    /// <summary>
    /// 声望等级信息
    /// </summary>
    private record ReputationTierInfo(string Name, int MinValue, int MaxValue, string BarColorClass);
}