using BlazorWebGame.Shared.Models;
using BlazorWebGame.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Server.Services.Equipments;

/// <summary>
/// 服务端战利品系统 - 从客户端移植而来
/// </summary>
public class ServerLootService
{
    private readonly ILogger<ServerLootService> _logger;
    private readonly Random _random = new();

    public ServerLootService(ILogger<ServerLootService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 计算战斗奖励
    /// </summary>
    public BattleResultDto CalculateBattleRewards(ServerBattleContext battle, bool victory)
    {
        var result = new BattleResultDto
        {
            Victory = victory,
            CompletedAt = DateTime.UtcNow
        };

        if (!victory)
        {
            // 失败时给予少量安慰奖励
            result.ExperienceGained = battle.Enemies.Sum(e => e.XpReward) / 10;
            result.GoldGained = battle.Enemies.Sum(e => _random.Next(1, e.MinGoldReward + 1));
            return result;
        }

        // 计算基础奖励
        result.ExperienceGained = CalculateExperienceReward(battle);
        result.GoldGained = CalculateGoldReward(battle);
        
        // 计算掉落物品
        result.ItemsLooted = CalculateItemDrops(battle);

        // 应用奖励加成
        ApplyRewardBonuses(battle, result);

        _logger.LogInformation("Battle rewards calculated: {Exp} XP, {Gold} gold, {Items} items", 
            result.ExperienceGained, result.GoldGained, result.ItemsLooted.Count);

        return result;
    }

    /// <summary>
    /// 计算经验奖励
    /// </summary>
    private int CalculateExperienceReward(ServerBattleContext battle)
    {
        int baseExperience = battle.Enemies.Sum(e => e.XpReward);
        
        // 根据战斗时长给予奖励加成
        var battleDuration = DateTime.UtcNow - battle.StartTime;
        double durationBonus = 1.0;
        
        if (battleDuration.TotalMinutes < 1) // 快速战斗奖励
        {
            durationBonus = 1.2;
        }
        else if (battleDuration.TotalMinutes > 5) // 长时间战斗惩罚
        {
            durationBonus = 0.8;
        }

        // 根据玩家等级差异调整经验
        var averagePlayerLevel = battle.Players.Average(p => p.Level);
        var averageEnemyLevel = battle.Enemies.Average(e => e.Level);
        double levelDifferenceMultiplier = CalculateLevelDifferenceMultiplier(averagePlayerLevel, averageEnemyLevel);

        int finalExperience = (int)(baseExperience * durationBonus * levelDifferenceMultiplier);
        
        return Math.Max(1, finalExperience); // 至少给1点经验
    }

    /// <summary>
    /// 计算金币奖励
    /// </summary>
    private int CalculateGoldReward(ServerBattleContext battle)
    {
        int totalGold = 0;
        
        foreach (var enemy in battle.Enemies)
        {
            int enemyGold = _random.Next(enemy.MinGoldReward, enemy.MaxGoldReward + 1);
            totalGold += enemyGold;
        }

        // 根据战斗类型调整金币奖励
        if (battle.BattleType == "Dungeon")
        {
            totalGold = (int)(totalGold * 1.5); // 地牢奖励加成
        }

        // 添加随机奖励金币
        if (_random.NextDouble() < 0.1) // 10% 概率额外奖励
        {
            int bonusGold = totalGold / 2;
            totalGold += bonusGold;
            _logger.LogDebug("Bonus gold awarded: {BonusGold}", bonusGold);
        }

        return totalGold;
    }

    /// <summary>
    /// 计算物品掉落
    /// </summary>
    private List<string> CalculateItemDrops(ServerBattleContext battle)
    {
        var lootedItems = new List<string>();

        foreach (var enemy in battle.Enemies)
        {
            foreach (var lootEntry in enemy.LootTable)
            {
                string itemId = lootEntry.Key;
                double dropChance = lootEntry.Value;

                // 应用掉落率加成
                double modifiedDropChance = ApplyDropRateModifiers(dropChance, battle);

                if (_random.NextDouble() < modifiedDropChance)
                {
                    lootedItems.Add(itemId);
                    _logger.LogDebug("Item dropped: {ItemId} from {EnemyName}", itemId, enemy.Name);
                }
            }

            // 额外的稀有物品掉落检查
            CheckForRareDrops(enemy, lootedItems);
        }

        return lootedItems;
    }

    /// <summary>
    /// 应用掉落率修饰符
    /// </summary>
    private double ApplyDropRateModifiers(double baseDropChance, ServerBattleContext battle)
    {
        double modifiedChance = baseDropChance;

        // 玩家等级低于敌人时增加掉落率
        var averagePlayerLevel = battle.Players.Average(p => p.Level);
        var averageEnemyLevel = battle.Enemies.Average(e => e.Level);
        
        if (averagePlayerLevel < averageEnemyLevel)
        {
            double levelDifference = averageEnemyLevel - averagePlayerLevel;
            modifiedChance *= 1.0 + levelDifference * 0.1; // 每级差异增加10%掉落率
        }

        // 多人组队时略微降低掉落率
        if (battle.Players.Count > 1)
        {
            modifiedChance *= 0.9; // 组队时掉落率降低10%
        }

        return Math.Min(1.0, modifiedChance); // 掉落率不能超过100%
    }

    /// <summary>
    /// 检查稀有物品掉落
    /// </summary>
    private void CheckForRareDrops(ServerBattleEnemy enemy, List<string> lootedItems)
    {
        // 基于敌人等级的稀有物品掉落
        double rareDropChance = 0.01 + enemy.Level * 0.002; // 基础1%，每级增加0.2%

        if (_random.NextDouble() < rareDropChance)
        {
            var rareItems = GetRareItemsForEnemyType(enemy.EnemyType, enemy.Level);
            if (rareItems.Any())
            {
                var rareItem = rareItems[_random.Next(rareItems.Count)];
                lootedItems.Add(rareItem);
                _logger.LogInformation("Rare item dropped: {RareItem} from {EnemyName}", rareItem, enemy.Name);
            }
        }
    }

    /// <summary>
    /// 获取敌人类型对应的稀有物品
    /// </summary>
    private List<string> GetRareItemsForEnemyType(string enemyType, int enemyLevel)
    {
        var rareItems = new List<string>();

        switch (enemyType.ToLower())
        {
            case "goblin":
                rareItems.AddRange(new[] { "goblin_ear", "rusty_dagger", "small_gem" });
                if (enemyLevel >= 5) rareItems.Add("goblin_crown");
                break;
                
            case "orc":
                rareItems.AddRange(new[] { "orc_tusk", "battle_axe", "leather_armor" });
                if (enemyLevel >= 10) rareItems.Add("orc_warchief_helm");
                break;
                
            case "skeleton":
                rareItems.AddRange(new[] { "bone_fragment", "ancient_coin", "tattered_scroll" });
                if (enemyLevel >= 8) rareItems.Add("necromancer_staff");
                break;
                
            case "dragon":
                rareItems.AddRange(new[] { "dragon_scale", "dragon_claw", "fire_gem" });
                if (enemyLevel >= 20) rareItems.Add("dragon_heart");
                break;
                
            default:
                rareItems.AddRange(new[] { "mystery_box", "gold_coin", "magic_crystal" });
                break;
        }

        return rareItems;
    }

    /// <summary>
    /// 计算等级差异倍数
    /// </summary>
    private double CalculateLevelDifferenceMultiplier(double playerLevel, double enemyLevel)
    {
        double levelDifference = enemyLevel - playerLevel;
        
        if (levelDifference > 0)
        {
            // 挑战更高等级的敌人，经验加成
            return 1.0 + levelDifference * 0.1; // 每级差异增加10%经验
        }
        else if (levelDifference < -5)
        {
            // 挑战远低于自己等级的敌人，经验惩罚
            return Math.Max(0.1, 1.0 + levelDifference * 0.05); // 每级差异减少5%经验，最少10%
        }
        
        return 1.0; // 同等级无修正
    }

    /// <summary>
    /// 应用奖励加成
    /// </summary>
    private void ApplyRewardBonuses(ServerBattleContext battle, BattleResultDto result)
    {
        // 首次击杀奖励
        bool isFirstKill = CheckFirstKillBonus(battle);
        if (isFirstKill)
        {
            result.ExperienceGained = (int)(result.ExperienceGained * 1.5);
            result.GoldGained = (int)(result.GoldGained * 1.3);
            _logger.LogInformation("First kill bonus applied to battle {BattleId}", battle.BattleId);
        }

        // 连胜奖励
        int winStreak = GetPlayerWinStreak(battle.Players.FirstOrDefault()?.Id);
        if (winStreak > 0)
        {
            double streakMultiplier = 1.0 + Math.Min(winStreak * 0.05, 0.5); // 每连胜增加5%，最多50%
            result.ExperienceGained = (int)(result.ExperienceGained * streakMultiplier);
            result.GoldGained = (int)(result.GoldGained * streakMultiplier);
        }

        // 完美胜利奖励（所有玩家生命值高于50%）
        bool isPerfectVictory = battle.Players.All(p => p.HealthPercentage > 0.5);
        if (isPerfectVictory)
        {
            result.ExperienceGained = (int)(result.ExperienceGained * 1.2);
            result.ItemsLooted.Add("perfect_victory_token");
            _logger.LogInformation("Perfect victory bonus applied to battle {BattleId}", battle.BattleId);
        }
    }

    /// <summary>
    /// 检查是否为首次击杀
    /// </summary>
    private bool CheckFirstKillBonus(ServerBattleContext battle)
    {
        // 简化实现：随机返回，实际应该查询数据库
        return _random.NextDouble() < 0.1; // 10% 概率首次击杀
    }

    /// <summary>
    /// 获取玩家连胜次数
    /// </summary>
    private int GetPlayerWinStreak(string? playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return 0;
        
        // 简化实现：随机返回，实际应该查询数据库
        return _random.Next(0, 6); // 0-5 连胜
    }

    /// <summary>
    /// 分配经验给玩家
    /// </summary>
    public void DistributeExperienceToPlayers(List<ServerBattlePlayer> players, int totalExperience)
    {
        if (!players.Any()) return;

        // 平均分配经验
        int experiencePerPlayer = totalExperience / players.Count;
        
        foreach (var player in players)
        {
            int playerExperience = experiencePerPlayer;
            
            // 基于玩家贡献调整经验（简化版）
            double contributionMultiplier = CalculatePlayerContribution(player);
            playerExperience = (int)(playerExperience * contributionMultiplier);
            
            player.Experience += playerExperience;
            _logger.LogDebug("Player {PlayerName} gained {Experience} experience", 
                player.Name, playerExperience);
        }
    }

    /// <summary>
    /// 生成战斗掉落物品
    /// </summary>
    public List<string> GenerateBattleLoot(ServerBattleContext battle, ServerBattlePlayer player)
    {
        var lootItems = new List<string>();
        
        foreach (var enemy in battle.Enemies)
        {
            if (enemy.LootTable != null && enemy.LootTable.Any())
            {
                // 使用敌人的掉落表
                foreach (var lootEntry in enemy.LootTable)
                {
                    if (_random.NextDouble() < lootEntry.Value)
                    {
                        lootItems.Add(lootEntry.Key);
                    }
                }
            }
            else
            {
                // 默认掉落逻辑 - 使用现有的GenerateRandomLoot method for the enemy type
                var enemyLoot = new List<string>();
                
                // 简单的掉落逻辑
                if (_random.NextDouble() < 0.4) // 40% 概率掉落金币
                {
                    enemyLoot.Add("gold");
                }
                
                if (_random.NextDouble() < 0.2) // 20% 概率掉落物品
                {
                    var possibleItems = GetPossibleLootByEnemyType(enemy.Name, enemy.Level);
                    if (possibleItems.Any())
                    {
                        enemyLoot.Add(possibleItems[_random.Next(possibleItems.Count)]);
                    }
                }
                
                lootItems.AddRange(enemyLoot);
            }
        }
        
        // 额外随机掉落
        if (_random.NextDouble() < 0.3) // 30% 概率额外掉落
        {
            lootItems.Add("bonus_gold");
        }
        
        _logger.LogDebug("Generated {Count} loot items for player {PlayerId} from battle {BattleId}", 
            lootItems.Count, player.Id, battle.BattleId);
        
        return lootItems;
    }

    /// <summary>
    /// 根据敌人类型获取可能的掉落物品
    /// </summary>
    private List<string> GetPossibleLootByEnemyType(string enemyName, int enemyLevel)
    {
        var possibleLoot = new List<string>();
        
        switch (enemyName.ToLower())
        {
            case "goblin":
                possibleLoot.AddRange(new[] { "goblin_ear", "rusty_dagger", "small_potion" });
                if (enemyLevel >= 5) possibleLoot.Add("goblin_crown");
                break;
                
            case "orc":
                possibleLoot.AddRange(new[] { "orc_tusk", "iron_sword", "leather_armor" });
                if (enemyLevel >= 10) possibleLoot.Add("orc_axe");
                break;
                
            case "skeleton":
                possibleLoot.AddRange(new[] { "bone_fragment", "ancient_coin", "scroll" });
                if (enemyLevel >= 8) possibleLoot.Add("bone_staff");
                break;
                
            default:
                possibleLoot.AddRange(new[] { "gold", "health_potion", "common_gem" });
                break;
        }
        
        return possibleLoot;
    }

    /// <summary>
    /// 计算玩家贡献度
    /// </summary>
    private double CalculatePlayerContribution(ServerBattlePlayer player)
    {
        // 简化的贡献度计算：基于生存率
        double survivalRate = player.HealthPercentage;
        return 0.7 + survivalRate * 0.6; // 70%-130% 经验基于生存率
    }
}