using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 职业运行时系统 - 缺失的核心模型
/// </summary>
public class ProfessionRuntime
{
    public string ProfessionId { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;

    // 职业特定参数
    public double AttackHasteMod { get; set; } = 1.0;
    public double SpecialIntervalBase { get; set; } = 5.0;
    public Dictionary<string, double> ResourceMaxOverrides { get; set; } = new();
    public Dictionary<string, OverflowRule> OverflowRules { get; set; } = new();

    // 职业特殊机制
    public Dictionary<string, ProfessionMechanic> SpecialMechanics { get; set; } = new();

    /// <summary>
    /// 应用到战斗实例
    /// </summary>
    public void ApplyToBattle(BattleInstance battle, string combatantId)
    {
        // 修改攻击轨道
        if (battle.Tracks.TryGetValue("attack", out var attackTrack))
        {
            attackTrack.HasteFactor *= AttackHasteMod;
        }

        // 设置特殊轨道间隔
        if (battle.Tracks.TryGetValue("special", out var specialTrack))
        {
            specialTrack.BaseInterval = TimeSpan.FromSeconds(SpecialIntervalBase);
        }

        // 应用资源上限覆盖
        foreach (var kvp in ResourceMaxOverrides)
        {
            var bucketKey = $"{combatantId}_{kvp.Key}";
            if (battle.ResourceBuckets.TryGetValue(bucketKey, out var bucket))
            {
                bucket.Max = kvp.Value;
            }
        }

        // 应用溢出规则
        foreach (var rule in OverflowRules)
        {
            var bucketKey = $"{combatantId}_{rule.Key}";
            if (battle.ResourceBuckets.TryGetValue(bucketKey, out var bucket))
            {
                bucket.OverflowPolicy = rule.Value.Policy;
                bucket.ConvertTarget = rule.Value.ConvertTarget;
                bucket.ConvertRatio = rule.Value.ConvertRatio;
            }
        }
    }
}

/// <summary>
/// 溢出规则
/// </summary>
public class OverflowRule
{
    public OverflowPolicy Policy { get; set; }
    public string? ConvertTarget { get; set; }
    public double ConvertRatio { get; set; } = 1.0;
    public Action<ResourceOverflowContext>? CustomHandler { get; set; }
}

/// <summary>
/// 资源溢出上下文
/// </summary>
public class ResourceOverflowContext
{
    public string ResourceId { get; set; } = string.Empty;
    public double OverflowAmount { get; set; }
    public BattleInstance Battle { get; set; } = null!;
    public string OwnerId { get; set; } = string.Empty;
}

/// <summary>
/// 职业特殊机制
/// </summary>
public class ProfessionMechanic
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MechanicType Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Func<MechanicContext, MechanicResult>? Execute { get; set; }
}

public enum MechanicType
{
    OnAttack,
    OnSpecial,
    OnResourceGain,
    OnBuffStack,
    OnSkillCast,
    Periodic
}

public class MechanicContext
{
    public BattleInstance Battle { get; set; } = null!;
    public string TriggererId { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}

public class MechanicResult
{
    public bool Success { get; set; }
    public List<IGameEvent> GeneratedEvents { get; set; } = new();
}

/// <summary>
/// 职业定义 - 静态数据
/// </summary>
public class ProfessionDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProfessionArchetype Archetype { get; set; }

    // 基础资源配置
    public List<ResourceDefinition> Resources { get; set; } = new();

    // 轨道配置
    public TrackConfiguration AttackTrack { get; set; } = new();
    public TrackConfiguration SpecialTrack { get; set; } = new();

    // 职业技能树
    public List<string> CoreSkillIds { get; set; } = new();
    public Dictionary<int, List<string>> LevelUnlockSkills { get; set; } = new();

    // 转职路径
    public List<AdvancementPath> AdvancementPaths { get; set; } = new();
}

/// <summary>
/// 资源定义
/// </summary>
public class ResourceDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double InitialValue { get; set; } = 0;
    public double MaxValue { get; set; } = 100;
    public ResourceGenerationType GenerationType { get; set; }
    public double GenerationRate { get; set; } = 1.0;
    public string? GenerationTrack { get; set; } // attack/special/both
}

public enum ResourceGenerationType
{
    OnAttack,
    OnSpecial,
    OverTime,
    OnDamageDealt,
    OnDamageTaken
}

/// <summary>
/// 轨道配置
/// </summary>
public class TrackConfiguration
{
    public double BaseInterval { get; set; } = 1.0;
    public bool AffectedByHaste { get; set; } = true;
    public double MinInterval { get; set; } = 0.5;
    public double MaxInterval { get; set; } = 10.0;
}

/// <summary>
/// 转职路径
/// </summary>
public class AdvancementPath
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TargetProfessionId { get; set; } = string.Empty;
    public int RequiredLevel { get; set; } = 20;
    public string RequirementExpr { get; set; } = string.Empty;
    public Dictionary<string, int> MaterialCosts { get; set; } = new();
}

public enum ProfessionArchetype
{
    Warrior,    // 战士系
    Mage,       // 法师系
    Ranger,     // 游侠系
    Rogue,      // 盗贼系
    Priest,     // 牧师系
    Hybrid      // 混合系
}