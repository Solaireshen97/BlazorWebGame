using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.DTOs.Skill
{
    /// <summary>
    /// 技能定义DTO
    /// </summary>
    public class SkillDefinitionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Type { get; set; } = "Active"; // Active/Passive/Toggle
        public string Category { get; set; } = "Combat"; // Combat/Utility/Profession
        public int MaxLevel { get; set; } = 10;
        public int RequiredCharacterLevel { get; set; } = 1;
        public string? RequiredProfessionId { get; set; }
        public List<string> Prerequisites { get; set; } = new();
        public string? TrackAffinity { get; set; } // attack/special/none
        public HashSet<string> Tags { get; set; } = new();

        // 基础数值
        public SkillCostDto BaseCost { get; set; } = new();
        public SkillEffectsDto BaseEffects { get; set; } = new();
        public TimeSpan BaseCooldown { get; set; }
        public double BaseRange { get; set; }
        public double BaseCastTime { get; set; }

        // 每级成长
        public SkillScalingDto Scaling { get; set; } = new();
    }

    /// <summary>
    /// 技能消耗DTO
    /// </summary>
    public class SkillCostDto
    {
        public int? ManaCost { get; set; }
        public int? HealthCost { get; set; }
        public int? EnergyCost { get; set; }
        public Dictionary<string, double> ResourceCosts { get; set; } = new();
    }

    /// <summary>
    /// 技能效果DTO
    /// </summary>
    public class SkillEffectsDto
    {
        public double? Damage { get; set; }
        public double? Healing { get; set; }
        public string? DamageType { get; set; } // Physical/Magical/True
        public double? AreaRadius { get; set; }
        public int? MaxTargets { get; set; }
        public List<SkillBuffDto> Buffs { get; set; } = new();
        public List<SkillDebuffDto> Debuffs { get; set; } = new();
        public Dictionary<string, object> SpecialEffects { get; set; } = new();
    }

    /// <summary>
    /// 技能增益效果DTO
    /// </summary>
    public class SkillBuffDto
    {
        public string BuffId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public int MaxStacks { get; set; } = 1;
        public Dictionary<string, double> StatModifiers { get; set; } = new();
    }

    /// <summary>
    /// 技能减益效果DTO
    /// </summary>
    public class SkillDebuffDto
    {
        public string DebuffId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string Type { get; set; } = string.Empty; // Slow/Stun/Poison/Burn/etc
        public double Value { get; set; }
    }

    /// <summary>
    /// 技能成长DTO
    /// </summary>
    public class SkillScalingDto
    {
        public double DamagePerLevel { get; set; }
        public double HealingPerLevel { get; set; }
        public double CostReductionPerLevel { get; set; }
        public double CooldownReductionPerLevel { get; set; }
        public Dictionary<string, double> CustomScaling { get; set; } = new();
    }

    /// <summary>
    /// 已学习技能DTO
    /// </summary>
    public class LearnedSkillDto
    {
        public string SkillId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int CurrentLevel { get; set; }
        public int MaxLevel { get; set; }
        public int ExperienceToNextLevel { get; set; }
        public bool IsMaxLevel => CurrentLevel >= MaxLevel;

        // 当前等级的实际数值
        public SkillCostDto CurrentCost { get; set; } = new();
        public SkillEffectsDto CurrentEffects { get; set; } = new();
        public TimeSpan CurrentCooldown { get; set; }
        public double CurrentRange { get; set; }
        public double CurrentCastTime { get; set; }

        // 下一级预览（如果不是满级）
        public SkillCostDto? NextLevelCost { get; set; }
        public SkillEffectsDto? NextLevelEffects { get; set; }

        // 使用统计
        public int TimesUsed { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public long TotalDamageDealt { get; set; }
        public long TotalHealingDone { get; set; }
    }

    /// <summary>
    /// 技能栏配置DTO
    /// </summary>
    public class SkillBarDto
    {
        public string CharacterId { get; set; } = string.Empty;
        public List<SkillSlotConfigDto> ActiveSlots { get; set; } = new();
        public List<SkillSlotConfigDto> PassiveSlots { get; set; } = new();
        public string? AutoCastPolicy { get; set; } // Priority/Rotation/Smart
        public bool AutoCastEnabled { get; set; }
    }

    /// <summary>
    /// 技能槽位配置DTO
    /// </summary>
    public class SkillSlotConfigDto
    {
        public int SlotIndex { get; set; }
        public string? SkillId { get; set; }
        public string? SkillName { get; set; }
        public string? SkillIcon { get; set; }
        public string? Keybind { get; set; }
        public bool IsLocked { get; set; }
        public int UnlockLevel { get; set; }
        public int Priority { get; set; } // 用于自动施放优先级
        public AutoCastSettingDto? AutoCastSetting { get; set; }
    }

    /// <summary>
    /// 自动施放设置DTO
    /// </summary>
    public class AutoCastSettingDto
    {
        public bool Enabled { get; set; }
        public string Condition { get; set; } = "Always"; // Always/OnCooldown/OnResource/OnHealth
        public double? ThresholdValue { get; set; } // 触发阈值
        public string? TargetPriority { get; set; } // Nearest/Weakest/Strongest/Random
    }

    /// <summary>
    /// 技能树DTO
    /// </summary>
    public class SkillTreeDto
    {
        public string TreeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProfessionId { get; set; } = string.Empty;
        public List<SkillTreeNodeDto> Nodes { get; set; } = new();
        public int TotalPointsInvested { get; set; }
        public int AvailablePoints { get; set; }
    }

    /// <summary>
    /// 技能树节点DTO
    /// </summary>
    public class SkillTreeNodeDto
    {
        public string NodeId { get; set; } = string.Empty;
        public string SkillId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Tier { get; set; } // 层级
        public int Column { get; set; } // 列位置
        public int CurrentRank { get; set; }
        public int MaxRank { get; set; }
        public int RequiredPoints { get; set; } // 解锁需要的总点数
        public List<string> Prerequisites { get; set; } = new();
        public bool IsUnlocked { get; set; }
        public bool CanLearn { get; set; }
    }

    /// <summary>
    /// 技能组合DTO
    /// </summary>
    public class SkillComboDto
    {
        public string ComboId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> SkillSequence { get; set; } = new();
        public TimeSpan TimeWindow { get; set; }
        public SkillEffectsDto BonusEffects { get; set; } = new();
        public int TimesTriggered { get; set; }
    }

    /// <summary>
    /// 技能冷却状态DTO
    /// </summary>
    public class SkillCooldownDto
    {
        public string SkillId { get; set; } = string.Empty;
        public DateTime ReadyAt { get; set; }
        public TimeSpan TotalCooldown { get; set; }
        public TimeSpan RemainingCooldown => ReadyAt > DateTime.UtcNow ? ReadyAt - DateTime.UtcNow : TimeSpan.Zero;
        public bool IsReady => DateTime.UtcNow >= ReadyAt;
        public double CooldownPercentage => TotalCooldown.TotalSeconds > 0
            ? (1 - RemainingCooldown.TotalSeconds / TotalCooldown.TotalSeconds) * 100
            : 100;
    }

    /// <summary>
    /// 学习技能请求
    /// </summary>
    public class LearnSkillRequest
    {
        public string SkillId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 升级技能请求
    /// </summary>
    public class UpgradeSkillRequest
    {
        public string SkillId { get; set; } = string.Empty;
        public int TargetLevel { get; set; }
    }

    /// <summary>
    /// 装备技能到槽位请求
    /// </summary>
    public class EquipSkillRequest
    {
        public string SkillId { get; set; } = string.Empty;
        public int SlotIndex { get; set; }
        public bool IsPassive { get; set; }
    }

    /// <summary>
    /// 使用技能请求
    /// </summary>
    public class UseSkillRequest
    {
        public string SkillId { get; set; } = string.Empty;
        public string? TargetId { get; set; }
        public SkillTargetPosition? TargetPosition { get; set; }
    }

    /// <summary>
    /// 技能目标位置
    /// </summary>
    public class SkillTargetPosition
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    /// <summary>
    /// 技能使用结果
    /// </summary>
    public class SkillUseResult
    {
        public bool Success { get; set; }
        public string? FailureReason { get; set; }
        public List<SkillEffect> Effects { get; set; } = new();
        public SkillCooldownDto? Cooldown { get; set; }
    }

    /// <summary>
    /// 技能效果结果
    /// </summary>
    public class SkillEffect
    {
        public string Type { get; set; } = string.Empty; // Damage/Healing/Buff/Debuff
        public string TargetId { get; set; } = string.Empty;
        public double Value { get; set; }
        public bool IsCritical { get; set; }
    }

    /// <summary>
    /// 批量配置技能栏请求
    /// </summary>
    public class ConfigureSkillBarRequest
    {
        public Dictionary<int, string?> ActiveSkills { get; set; } = new();
        public Dictionary<int, string?> PassiveSkills { get; set; } = new();
        public Dictionary<int, AutoCastSettingDto> AutoCastSettings { get; set; } = new();
    }

    /// <summary>
    /// 重置技能点请求
    /// </summary>
    public class ResetSkillPointsRequest
    {
        public string? TreeId { get; set; } // 指定树，null表示全部重置
    }
}