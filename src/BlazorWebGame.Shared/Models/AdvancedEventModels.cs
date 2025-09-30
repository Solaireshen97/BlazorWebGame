using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 装备触发事件
/// </summary>
public class OnHitEvent : DomainEventBase, IGameEvent
{
    public override string EventType => "OnHit";
    public string AttackerId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public double Damage { get; set; }
    public bool IsCritical { get; set; }
    public List<string> TriggeredGearIds { get; set; } = new();

    public DateTime Timestamp => OccurredAt;

    public void Execute(IGameContext context)
    {
        // 触发装备特效
        foreach (var gearId in TriggeredGearIds)
        {
            // TODO: 查找装备并执行特效
        }
    }
}

/// <summary>
/// Buff过期事件
/// </summary>
public class BuffExpireEvent : DomainEventBase, IGameEvent
{
    public override string EventType => "BuffExpire";
    public string BuffId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public int FinalStacks { get; set; }

    public DateTime Timestamp => OccurredAt;

    public void Execute(IGameContext context)
    {
        // 处理buff过期逻辑
    }
}

/// <summary>
/// 区域事件
/// </summary>
public class RegionEvent : DomainEventBase
{
    public override string EventType => "RegionEvent";
    public string RegionId { get; set; } = string.Empty;
    public RegionEventType RegionEventType { get; set; }
    public Dictionary<string, object> EventData { get; set; } = new();
}

public enum RegionEventType
{
    MonsterInvasion,    // 怪物入侵
    ResourceBonus,      // 资源加成
    RareSpawn,          // 稀有生成
    Weather,            // 天气变化
    Merchant            // 商人到访
}

/// <summary>
/// 世界事件
/// </summary>
public class WorldEvent : DomainEventBase
{
    public override string EventType => "WorldEvent";
    public string WorldEventId { get; set; } = string.Empty;
    public WorldEventType WorldEventType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public enum WorldEventType
{
    WorldBoss,          // 世界Boss
    DoubleExp,          // 双倍经验
    DoubleDrops,        // 双倍掉落
    SpecialDungeon,     // 限时副本
    GlobalBuff          // 全局增益
}