using BlazorWebGame.GameConfig;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using System.Linq;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Services.PlayerServices;

/// <summary>
/// 玩家专业技能管理服务实现
/// </summary>
public class PlayerProfessionService : IPlayerProfessionService
{
    public double GetTotalGatheringSpeedBonus(Models.Player player)
    {
        double equipmentBonus = player.EquippedItems.Values
            .Select(itemId => ItemData.GetItemById(itemId) as Equipment)
            .Where(eq => eq != null)
            .Sum(eq => eq!.GatheringSpeedBonus);
            
        double buffBonus = player.ActiveBuffs
            .Where(b => b.BuffType == StatBuffType.GatheringSpeed)
            .Sum(b => b.BuffValue / 100.0);
            
        return equipmentBonus + buffBonus;
    }

    public double GetTotalExtraLootChance(Models.Player player)
    {
        double equipmentBonus = player.EquippedItems.Values
            .Select(itemId => ItemData.GetItemById(itemId) as Equipment)
            .Where(eq => eq != null)
            .Sum(eq => eq!.ExtraLootChanceBonus);
            
        double buffBonus = player.ActiveBuffs
            .Where(b => b.BuffType == StatBuffType.ExtraLootChance)
            .Sum(b => b.BuffValue / 100.0);
            
        return equipmentBonus + buffBonus;
    }

    public double GetTotalCraftingSpeedBonus(Models.Player player)
    {
        // 未来可添加装备制作速度加成
        double equipmentBonus = 0.0;

        // 从Buff中获取加成
        double buffBonus = player.ActiveBuffs
            .Where(b => b.BuffType == StatBuffType.CraftingSpeed)
            .Sum(b => b.BuffValue / 100.0); // 将百分比 (如 15) 转换为小数 (0.15)

        return equipmentBonus + buffBonus;
    }

    public (bool LeveledUp, int OldLevel, int NewLevel) AddBattleXP(Models.Player player, BattleProfession profession, int amount)
    {
        if (player.BattleProfessionXP.ContainsKey(profession))
        {
            int oldLevel = GetLevel(player, profession);
            
            // 添加经验值
            player.BattleProfessionXP[profession] += amount;
            
            // 检查是否升级
            int newLevel = GetLevel(player, profession);
            bool leveledUp = newLevel > oldLevel;
            
            // 返回升级信息，由调用方决定未来处理
            return (leveledUp, oldLevel, newLevel);
        }
        
        return (false, 0, 0);
    }

    public void AddGatheringXP(Models.Player player, GatheringProfession profession, int amount)
    {
        if (player.GatheringProfessionXP.ContainsKey(profession))
        {
            player.GatheringProfessionXP[profession] += amount;
        }
    }

    public (bool LeveledUp, int OldLevel, int NewLevel) AddProductionXP(Models.Player player, ProductionProfession profession, int amount)
    {
        if (player.ProductionProfessionXP.ContainsKey(profession))
        {
            int oldLevel = GetLevel(player, profession);
            
            // 添加经验值
            player.ProductionProfessionXP[profession] += amount;
            
            // 检查是否升级
            int newLevel = GetLevel(player, profession);
            bool leveledUp = newLevel > oldLevel;
            
            return (leveledUp, oldLevel, newLevel);
        }
        
        return (false, 0, 0);
    }

    public int GetLevel(Models.Player player, BattleProfession profession)
    {
        return player.BattleProfessionXP.TryGetValue(profession, out var xp) ? 
            ExpSystem.GetLevelFromExp(xp) : 1;
    }

    public int GetLevel(Models.Player player, GatheringProfession profession)
    {
        return player.GatheringProfessionXP.TryGetValue(profession, out var xp) ? 
            ExpSystem.GetLevelFromExp(xp) : 1;
    }

    public int GetLevel(Models.Player player, ProductionProfession profession)
    {
        return ExpSystem.GetLevelFromExp(player.ProductionProfessionXP.GetValueOrDefault(profession, 0));
    }

    public double GetLevelProgress(Models.Player player, BattleProfession profession)
    {
        return player.BattleProfessionXP.TryGetValue(profession, out var xp) ?
            ExpSystem.GetLevelProgressPercentage(xp) : 0;
    }

    public double GetLevelProgress(Models.Player player, GatheringProfession profession)
    {
        return player.GatheringProfessionXP.TryGetValue(profession, out var xp) ?
            ExpSystem.GetLevelProgressPercentage(xp) : 0;
    }

    public double GetLevelProgress(Models.Player player, ProductionProfession profession)
    {
        return ExpSystem.GetLevelProgressPercentage(player.ProductionProfessionXP.GetValueOrDefault(profession, 0));
    }
}