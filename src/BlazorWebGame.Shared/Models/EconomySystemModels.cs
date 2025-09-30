using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 经济系统监控
/// </summary>
public class EconomyMonitor
{
    private readonly Dictionary<string, EconomyMetric> _metrics = new();
    private readonly Queue<EconomyEvent> _recentEvents = new();
    private const int MaxEventHistory = 1000;

    /// <summary>
    /// 记录经济事件
    /// </summary>
    public void RecordEvent(EconomyEvent economyEvent)
    {
        _recentEvents.Enqueue(economyEvent);

        if (_recentEvents.Count > MaxEventHistory)
            _recentEvents.Dequeue();

        UpdateMetrics(economyEvent);
    }

    private void UpdateMetrics(EconomyEvent evt)
    {
        var key = evt.Type.ToString();
        if (!_metrics.ContainsKey(key))
        {
            _metrics[key] = new EconomyMetric(key);
        }

        var metric = _metrics[key];
        metric.RecordValue(evt.Amount);
    }

    /// <summary>
    /// 获取经济指标
    /// </summary>
    public Dictionary<string, object> GetMetrics()
    {
        var result = new Dictionary<string, object>();

        foreach (var metric in _metrics.Values)
        {
            result[metric.Name + "_total"] = metric.Total;
            result[metric.Name + "_count"] = metric.Count;
            result[metric.Name + "_avg"] = metric.Average;
        }

        // 计算净流入/流出
        var goldIn = _metrics.GetValueOrDefault("GoldGained")?.Total ?? 0;
        var goldOut = _metrics.GetValueOrDefault("GoldSpent")?.Total ?? 0;
        result["gold_net"] = goldIn - goldOut;

        return result;
    }
}

/// <summary>
/// 经济指标
/// </summary>
public class EconomyMetric
{
    public string Name { get; }
    public double Total { get; private set; }
    public int Count { get; private set; }
    public double Average => Count > 0 ? Total / Count : 0;
    public double Min { get; private set; } = double.MaxValue;
    public double Max { get; private set; } = double.MinValue;

    public EconomyMetric(string name)
    {
        Name = name;
    }

    public void RecordValue(double value)
    {
        Total += value;
        Count++;
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
    }
}

/// <summary>
/// 经济事件
/// </summary>
public class EconomyEvent : DomainEventBase
{
    public override string EventType => "Economy";
    public EconomyEventType Type { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Currency { get; set; } = "gold";
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 经济事件类型
/// </summary>
public enum EconomyEventType
{
    GoldGained,
    GoldSpent,
    ItemCrafted,
    ItemDisenchanted,
    ItemReforged,
    ItemRerolled,
    ItemPurchased,
    ItemSold,
    MaterialGathered,
    MaterialConsumed
}

/// <summary>
/// 重铸服务
/// </summary>
public class ReforgeService
{
    private readonly Dictionary<GearTier, ReforgeCost> _costs = new()
    {
        [GearTier.T1] = new ReforgeCost { Gold = 100, Materials = new() { ["tier1_essence"] = 5 } },
        [GearTier.T2] = new ReforgeCost { Gold = 500, Materials = new() { ["tier2_essence"] = 10 } },
        [GearTier.T3] = new ReforgeCost { Gold = 2000, Materials = new() { ["tier3_essence"] = 20 } }
    };

    /// <summary>
    /// 计算重铸费用
    /// </summary>
    public ReforgeCost CalculateCost(GearInstance gear, GearTier targetTier)
    {
        return _costs.GetValueOrDefault(targetTier) ?? new ReforgeCost();
    }

    /// <summary>
    /// 执行重铸
    /// </summary>
    public ReforgeResult Reforge(GearInstance gear, GearTier targetTier, Character character)
    {
        var cost = CalculateCost(gear, targetTier);

        // 检查资源
        if (character.Gold < cost.Gold)
            return new ReforgeResult { Success = false, Reason = "金币不足" };

        foreach (var mat in cost.Materials)
        {
            if (!character.Inventory.HasItem(mat.Key, mat.Value))
                return new ReforgeResult { Success = false, Reason = $"材料不足: {mat.Key}" };
        }

        // 扣除资源
        character.SpendGold(cost.Gold);
        foreach (var mat in cost.Materials)
        {
            character.Inventory.RemoveItem(mat.Key, mat.Value);
        }

        // 执行重铸
        var oldTier = gear.Tier;
        gear.ReforgeTier(targetTier);

        return new ReforgeResult
        {
            Success = true,
            OldTier = oldTier,
            NewTier = targetTier
        };
    }
}

/// <summary>
/// 重铸费用
/// </summary>
public class ReforgeCost
{
    public int Gold { get; set; }
    public Dictionary<string, int> Materials { get; set; } = new();
}

/// <summary>
/// 重铸结果
/// </summary>
public class ReforgeResult
{
    public bool Success { get; set; }
    public string? Reason { get; set; }
    public GearTier OldTier { get; set; }
    public GearTier NewTier { get; set; }
}

/// <summary>
/// 词条重置服务
/// </summary>
public class RerollService
{
    private int _baseRerollCost = 50;
    private double _costMultiplier = 1.5;

    /// <summary>
    /// 计算重置费用（递增）
    /// </summary>
    public int CalculateCost(int rerollCount)
    {
        return (int)(_baseRerollCost * Math.Pow(_costMultiplier, rerollCount));
    }

    /// <summary>
    /// 执行词条重置
    /// </summary>
    public RerollResult Reroll(GearInstance gear, List<AffixDefinition> affixPool,
                               Character character, RNGContext rng)
    {
        var rerollCount = GetRerollCount(gear.Id);
        var cost = CalculateCost(rerollCount);

        if (character.Gold < cost)
            return new RerollResult { Success = false, Reason = "金币不足" };

        character.SpendGold(cost);

        var oldAffixes = gear.Affixes.ToList();
        gear.RerollAffixes(affixPool, rng);

        IncrementRerollCount(gear.Id);

        return new RerollResult
        {
            Success = true,
            Cost = cost,
            OldAffixes = oldAffixes,
            NewAffixes = gear.Affixes.ToList()
        };
    }

    private readonly Dictionary<string, int> _rerollCounts = new();

    private int GetRerollCount(string gearId)
    {
        return _rerollCounts.GetValueOrDefault(gearId, 0);
    }

    private void IncrementRerollCount(string gearId)
    {
        _rerollCounts[gearId] = GetRerollCount(gearId) + 1;
    }
}

/// <summary>
/// 词条重置结果
/// </summary>
public class RerollResult
{
    public bool Success { get; set; }
    public string? Reason { get; set; }
    public int Cost { get; set; }
    public List<AffixInstance> OldAffixes { get; set; } = new();
    public List<AffixInstance> NewAffixes { get; set; } = new();
}