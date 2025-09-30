using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 领域事件基础接口
/// </summary>
public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
    string AggregateId { get; }
    string EventType { get; }
}

/// <summary>
/// 领域事件基类
/// </summary>
public abstract class DomainEventBase : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string AggregateId { get; protected set; } = string.Empty;
    public abstract string EventType { get; }
}

/// <summary>
/// 技能施放事件
/// </summary>
public class SkillCastEvent : DomainEventBase
{
    public override string EventType => "SkillCast";
    public string SkillId { get; set; } = string.Empty;
    public string CasterId { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public Dictionary<string, double> ResourceCosts { get; set; } = new();
}

/// <summary>
/// 资源溢出事件
/// </summary>
public class ResourceOverflowEvent : DomainEventBase
{
    public override string EventType => "ResourceOverflow";
    public string ResourceId { get; set; } = string.Empty;
    public double OverflowAmount { get; set; }
    public string? ConvertedTo { get; set; }
    public double ConvertedAmount { get; set; }
}

/// <summary>
/// 计划完成事件
/// </summary>
public class PlanCompletedEvent : DomainEventBase
{
    public override string EventType => "PlanCompleted";
    public string PlanId { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// 战斗片段刷新事件
/// </summary>
public class SegmentFlushedEvent : DomainEventBase
{
    public override string EventType => "SegmentFlushed";
    public CombatSegment Segment { get; set; } = new();
}

/// <summary>
/// 伤害事件
/// </summary>
public class DamageEvent : DomainEventBase, IGameEvent
{
    public override string EventType => "Damage";
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public bool IsCritical { get; set; }
    public string? SkillId { get; set; }

    public DateTime Timestamp => OccurredAt;

    public void Execute(IGameContext context)
    {
        // 执行伤害逻辑
    }
}

/// <summary>
/// 资源获得事件
/// </summary>
public class ResourceGainEvent : DomainEventBase, IGameEvent
{
    public override string EventType => "ResourceGain";
    public string ResourceId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Source { get; set; } = string.Empty;

    public DateTime Timestamp => OccurredAt;

    public void Execute(IGameContext context)
    {
        // 执行资源获得逻辑
    }
}

/// <summary>
/// 攻击触发事件
/// </summary>
public class AttackTickEvent : DomainEventBase, IGameEvent
{
    public override string EventType => "AttackTick";
    public string AttackerId { get; set; } = string.Empty;
    public string? TargetId { get; set; }

    public DateTime Timestamp => OccurredAt;

    public void Execute(IGameContext context)
    {
        // 执行攻击逻辑
    }
}

/// <summary>
/// 特殊脉冲事件
/// </summary>
public class SpecialPulseEvent : DomainEventBase, IGameEvent
{
    public override string EventType => "SpecialPulse";
    public string OwnerId { get; set; } = string.Empty;
    public int PulseCount { get; set; }

    public DateTime Timestamp => OccurredAt;

    public void Execute(IGameContext context)
    {
    
    }
}