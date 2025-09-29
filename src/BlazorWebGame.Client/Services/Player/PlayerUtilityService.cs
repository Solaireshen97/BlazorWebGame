using BlazorWebGame.GameConfig;
using BlazorWebGame.Models;
using System;
using System.Linq;

namespace BlazorWebGame.Services.PlayerServices;

/// <summary>
/// 玩家实用工具服务实现
/// </summary>
public class PlayerUtilityService : IPlayerUtilityService
{
    public bool HasItemInInventory(Models.Player player, string itemId)
    {
        return player.Inventory.Any(s => !s.IsEmpty && s.ItemId == itemId);
    }

    public ReputationTier GetReputationLevel(Models.Player player, Faction faction)
    {
        var rep = player.Reputation.GetValueOrDefault(faction, 0);
        // 从高到低查找，返回第一个满足最低条件的等级
        return Models.Player.ReputationTiers.LastOrDefault(t => rep >= t.MinValue) ?? Models.Player.ReputationTiers.First();
    }

    public double GetReputationProgressPercentage(Models.Player player, Faction faction)
    {
        var rep = player.Reputation.GetValueOrDefault(faction, 0);
        var tier = GetReputationLevel(player, faction);

        // 如果是最高等级，直接返回满进度
        if (tier.MaxValue - tier.MinValue <= 1)
        {
            return 100.0;
        }

        var progressInTier = rep - tier.MinValue;
        var totalForTier = tier.MaxValue - tier.MinValue;

        return (double)progressInTier / totalForTier * 100.0;
    }

    public void EnsureDataConsistency(Models.Player player)
    {
        InitializeCollections(player);
    }

    public void InitializeCollections(Models.Player player)
    {
        // 使用 '??=' 操作符，仅在集合为 null时才给它们分配一个新的实例。
        player.BattleProfessionXP ??= new();
        player.GatheringProfessionXP ??= new();
        player.ProductionProfessionXP ??= new();
        player.EquippedSkills ??= new();

        // 为所有可能的职业初始化经验字典和技能列表
        foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
        {
            player.BattleProfessionXP.TryAdd(profession, 10000);
            player.EquippedSkills.TryAdd(profession, new System.Collections.Generic.List<string>());
        }
        foreach (var profession in (GatheringProfession[])Enum.GetValues(typeof(GatheringProfession)))
        {
            player.GatheringProfessionXP.TryAdd(profession, 0);
        }
        foreach (var profession in (ProductionProfession[])Enum.GetValues(typeof(ProductionProfession)))
        {
            player.ProductionProfessionXP.TryAdd(profession, 0);
        }
        
        // 确保默认配方已被学习
        foreach (var recipe in RecipeData.AllRecipes.Where(r => r.IsDefault))
        {
            player.LearnedRecipeIds.Add(recipe.Id);
        }
        
        // 初始化背包
        if (player.Inventory == null || !player.Inventory.Any())
        {
            player.Inventory = new System.Collections.Generic.List<InventorySlot>();
            for (int i = 0; i < 20; i++)
            {
                player.Inventory.Add(new InventorySlot());
            }
        }

        // *** 确保各种关键集合和字典，确保现有和旧集合都是非空 ***
        player.ActiveBuffs ??= new();
        player.LearnedSharedSkills ??= new();
        player.SkillCooldowns ??= new();
        player.EquippedItems ??= new();
        player.AutoSellItemIds ??= new();
        player.DefeatedMonsterIds ??= new();
        player.ConsumableCooldowns ??= new();

        player.PotionQuickSlots ??= new();
        player.CombatFoodQuickSlots ??= new();
        player.GatheringFoodQuickSlots ??= new();
        player.ProductionFoodQuickSlots ??= new();
    }
}