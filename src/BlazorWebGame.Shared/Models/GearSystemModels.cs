using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 装备定义
/// </summary>
public class GearDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EquipmentSlot Slot { get; set; }
    public int RequiredLevel { get; set; } = 1;
    public List<string> AllowedProfessions { get; set; } = new();

    // 基础属性范围
    public Dictionary<string, ValueRange> BaseStatRanges { get; set; } = new();

    // 允许的词条池
    public List<string> AllowedAffixIds { get; set; } = new();

    // 稀有度权重
    public Dictionary<ItemRarity, double> RarityWeights { get; set; } = new();

    // 套装信息
    public string? SetId { get; set; }
}

/// <summary>
/// 装备实例
/// </summary>
public class GearInstance
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string DefId { get; private set; } = string.Empty;
    public GearTier Tier { get; private set; } = GearTier.T1;
    public ItemRarity Rarity { get; private set; } = ItemRarity.Common;
    public List<AffixInstance> Affixes { get; private set; } = new();
    public string? SetId { get; private set; }
    public double QualityScore { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // 计算后的属性
    public Dictionary<string, double> ComputedStats { get; private set; } = new();

    public GearInstance(string defId, GearTier tier = GearTier.T1)
    {
        DefId = defId;
        Tier = tier;
    }

    /// <summary>
    /// 生成词条
    /// </summary>
    public void GenerateAffixes(List<AffixDefinition> affixPool, ItemRarity rarity, RNGContext rng)
    {
        Rarity = rarity;
        var affixCount = GetAffixCount(rarity);

        var selectedAffixes = new HashSet<string>();

        for (int i = 0; i < affixCount && affixPool.Count > 0; i++)
        {
            var weights = affixPool
                .Where(a => !selectedAffixes.Contains(a.Id))
                .Select(a => a.RarityWeight)
                .ToList();

            if (weights.Count == 0) break;

            var totalWeight = weights.Sum();
            var roll = rng.NextDouble() * totalWeight;
            var cumulative = 0.0;

            for (int j = 0; j < affixPool.Count; j++)
            {
                if (selectedAffixes.Contains(affixPool[j].Id))
                    continue;

                cumulative += affixPool[j].RarityWeight;
                if (roll <= cumulative)
                {
                    var affix = affixPool[j];
                    var instance = new AffixInstance
                    {
                        AffixId = affix.Id,
                        Value = affix.RollValue(rng)
                    };
                    Affixes.Add(instance);
                    selectedAffixes.Add(affix.Id);
                    break;
                }
            }
        }

        CalculateQualityScore();
    }

    private int GetAffixCount(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => 0,
            ItemRarity.Uncommon => 1,
            ItemRarity.Rare => 2,
            ItemRarity.Epic => 3,
            ItemRarity.Legendary => 4,
            ItemRarity.Mythic => 5,
            _ => 0
        };
    }

    /// <summary>
    /// 重铸品级
    /// </summary>
    public void ReforgeTier(GearTier newTier)
    {
        Tier = newTier;
        RecalculateStats();
    }

    /// <summary>
    /// 重置词条
    /// </summary>
    public void RerollAffixes(List<AffixDefinition> affixPool, RNGContext rng)
    {
        Affixes.Clear();
        GenerateAffixes(affixPool, Rarity, rng);
    }

    private void CalculateQualityScore()
    {
        QualityScore = Affixes.Sum(a => a.Value) * GetTierMultiplier();
    }

    private double GetTierMultiplier()
    {
        return Tier switch
        {
            GearTier.T1 => 0.8,
            GearTier.T2 => 1.0,
            GearTier.T3 => 1.2,
            _ => 1.0
        };
    }

    private void RecalculateStats()
    {
        ComputedStats.Clear();

        // TODO: 根据DefId获取基础属性，应用tier系数和affixes
        CalculateQualityScore();
    }
}

/// <summary>
/// 词条定义
/// </summary>
public class AffixDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AffixType Type { get; set; } = AffixType.Flat;
    public string StatKey { get; set; } = string.Empty;
    public ValueRange RollRange { get; set; } = new();
    public double RarityWeight { get; set; } = 1.0;

    public double RollValue(RNGContext rng)
    {
        return RollRange.Min + rng.NextDouble() * (RollRange.Max - RollRange.Min);
    }
}

/// <summary>
/// 词条实例
/// </summary>
public class AffixInstance
{
    public string AffixId { get; set; } = string.Empty;
    public double Value { get; set; }
}

/// <summary>
/// 值范围
/// </summary>
public class ValueRange
{
    public double Min { get; set; }
    public double Max { get; set; }
}

/// <summary>
/// 装备品级
/// </summary>
public enum GearTier
{
    T1,     // 0.8x
    T2,     // 1.0x
    T3      // 1.2x
}

/// <summary>
/// 词条类型
/// </summary>
public enum AffixType
{
    Flat,       // 固定值
    Percent,    // 百分比
    Proc        // 触发效果
}

/// <summary>
/// 分解服务
/// </summary>
public class DisenchantService
{
    private readonly Dictionary<string, DisenchantFormula> _formulas = new();

    /// <summary>
    /// 分解装备
    /// </summary>
    public DisenchantResult Disenchant(GearInstance gear)
    {
        var result = new DisenchantResult();

        // 基础材料根据tier和rarity
        var baseMaterials = GetBaseMaterials(gear.Tier, gear.Rarity);
        foreach (var mat in baseMaterials)
        {
            result.Materials[mat.Key] = mat.Value;
        }

        // 额外材料根据词条数量
        var bonusMaterials = gear.Affixes.Count * 5;
        result.Materials["essence_shard"] = bonusMaterials;

        return result;
    }

    private Dictionary<string, int> GetBaseMaterials(GearTier tier, ItemRarity rarity)
    {
        var materials = new Dictionary<string, int>();

        // 示例公式
        var tierMultiplier = tier switch
        {
            GearTier.T1 => 1,
            GearTier.T2 => 2,
            GearTier.T3 => 3,
            _ => 1
        };

        var rarityMultiplier = rarity switch
        {
            ItemRarity.Common => 1,
            ItemRarity.Uncommon => 2,
            ItemRarity.Rare => 3,
            ItemRarity.Epic => 5,
            ItemRarity.Legendary => 8,
            ItemRarity.Mythic => 12,
            _ => 1
        };

        materials[$"tier{(int)tier + 1}_essence"] = tierMultiplier * rarityMultiplier;
        materials["crafting_dust"] = 10 * rarityMultiplier;

        return materials;
    }
}

/// <summary>
/// 分解公式
/// </summary>
public class DisenchantFormula
{
    public string OutputMaterialId { get; set; } = string.Empty;
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public double Probability { get; set; } = 1.0;
}

/// <summary>
/// 分解结果
/// </summary>
public class DisenchantResult
{
    public Dictionary<string, int> Materials { get; set; } = new();
}