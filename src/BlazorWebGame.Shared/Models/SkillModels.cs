using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 技能领域模型
/// </summary>
public class Skill
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public SkillType Type { get; private set; } = SkillType.Active;
    public SkillCategory Category { get; private set; } = SkillCategory.Combat;
    public int RequiredLevel { get; private set; } = 1;
    public string RequiredProfession { get; private set; } = string.Empty;
    public TimeSpan Cooldown { get; private set; } = TimeSpan.Zero;
    public int ManaCost { get; private set; } = 0;
    public bool IsShared { get; private set; } = false; // 是否为共享技能

    // 技能效果
    private readonly List<SkillEffect> _effects = new();
    public IReadOnlyList<SkillEffect> Effects => _effects.AsReadOnly();

    // 前置技能
    private readonly List<string> _prerequisites = new();
    public IReadOnlyList<string> Prerequisites => _prerequisites.AsReadOnly();

    // 私有构造函数，用于反序列化
    private Skill() { }

    /// <summary>
    /// 创建新技能
    /// </summary>
    public Skill(string name, string description, SkillType type, SkillCategory category, string requiredProfession = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("技能名称不能为空", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        Category = category;
        RequiredProfession = requiredProfession?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// 设置等级需求
    /// </summary>
    public void SetRequiredLevel(int level)
    {
        RequiredLevel = Math.Max(1, level);
    }

    /// <summary>
    /// 设置冷却时间
    /// </summary>
    public void SetCooldown(TimeSpan cooldown)
    {
        Cooldown = cooldown;
    }

    /// <summary>
    /// 设置法力消耗
    /// </summary>
    public void SetManaCost(int manaCost)
    {
        ManaCost = Math.Max(0, manaCost);
    }

    /// <summary>
    /// 设置为共享技能
    /// </summary>
    public void SetAsShared(bool isShared = true)
    {
        IsShared = isShared;
    }

    /// <summary>
    /// 添加技能效果
    /// </summary>
    public void AddEffect(SkillEffectType effectType, int value, TimeSpan? duration = null, string? targetType = null)
    {
        var effect = new SkillEffect(effectType, value, duration, targetType);
        _effects.Add(effect);
    }

    /// <summary>
    /// 添加前置技能
    /// </summary>
    public void AddPrerequisite(string skillId)
    {
        if (!string.IsNullOrWhiteSpace(skillId) && !_prerequisites.Contains(skillId))
        {
            _prerequisites.Add(skillId);
        }
    }

    /// <summary>
    /// 检查是否可以使用技能
    /// </summary>
    public bool CanUse(string profession, int level, int currentMana)
    {
        // 检查职业要求
        if (!string.IsNullOrEmpty(RequiredProfession) && RequiredProfession != profession && !IsShared)
            return false;

        // 检查等级要求
        if (level < RequiredLevel)
            return false;

        // 检查法力消耗
        if (currentMana < ManaCost)
            return false;

        return true;
    }
}

/// <summary>
/// 技能效果
/// </summary>
public class SkillEffect
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public SkillEffectType Type { get; private set; } = SkillEffectType.Damage;
    public int Value { get; private set; } = 0;
    public TimeSpan? Duration { get; private set; }
    public string? TargetType { get; private set; } // Self, Enemy, Ally, Area
    public bool IsInstant => !Duration.HasValue || Duration == TimeSpan.Zero;
    public Dictionary<string, object> Properties { get; private set; } = new();

    // 私有构造函数，用于反序列化
    private SkillEffect() { }

    /// <summary>
    /// 创建技能效果
    /// </summary>
    public SkillEffect(SkillEffectType type, int value, TimeSpan? duration = null, string? targetType = null)
    {
        Type = type;
        Value = value;
        Duration = duration;
        TargetType = targetType ?? "Enemy";
    }

    /// <summary>
    /// 设置属性
    /// </summary>
    public void SetProperty(string key, object value)
    {
        Properties[key] = value;
    }

    /// <summary>
    /// 获取属性
    /// </summary>
    public T? GetProperty<T>(string key, T? defaultValue = default)
    {
        if (Properties.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }
}

/// <summary>
/// 角色学会的技能
/// </summary>
public class LearnedSkill
{
    public string SkillId { get; private set; } = string.Empty;
    public DateTime LearnedAt { get; private set; } = DateTime.UtcNow;
    public int UsageCount { get; private set; } = 0;
    public DateTime? LastUsedAt { get; private set; }

    // 私有构造函数，用于反序列化
    private LearnedSkill() { }

    /// <summary>
    /// 创建已学技能记录
    /// </summary>
    public LearnedSkill(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            throw new ArgumentException("技能ID不能为空", nameof(skillId));

        SkillId = skillId;
        LearnedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 记录技能使用
    /// </summary>
    public void RecordUsage()
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// 技能冷却管理
/// </summary>
public class SkillCooldownManager
{
    private readonly Dictionary<string, DateTime> _cooldowns = new();

    /// <summary>
    /// 设置技能冷却
    /// </summary>
    public void SetCooldown(string skillId, TimeSpan cooldown)
    {
        _cooldowns[skillId] = DateTime.UtcNow.Add(cooldown);
    }

    /// <summary>
    /// 检查技能是否在冷却中
    /// </summary>
    public bool IsOnCooldown(string skillId)
    {
        if (_cooldowns.TryGetValue(skillId, out var endTime))
        {
            if (DateTime.UtcNow < endTime)
                return true;
            
            // 冷却结束，移除记录
            _cooldowns.Remove(skillId);
        }
        return false;
    }

    /// <summary>
    /// 获取技能剩余冷却时间
    /// </summary>
    public TimeSpan GetRemainingCooldown(string skillId)
    {
        if (_cooldowns.TryGetValue(skillId, out var endTime))
        {
            var remaining = endTime - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        return TimeSpan.Zero;
    }

    /// <summary>
    /// 清除技能冷却
    /// </summary>
    public void ClearCooldown(string skillId)
    {
        _cooldowns.Remove(skillId);
    }

    /// <summary>
    /// 清除所有冷却
    /// </summary>
    public void ClearAllCooldowns()
    {
        _cooldowns.Clear();
    }

    /// <summary>
    /// 获取所有冷却中的技能
    /// </summary>
    public Dictionary<string, TimeSpan> GetAllCooldowns()
    {
        var result = new Dictionary<string, TimeSpan>();
        var now = DateTime.UtcNow;
        
        foreach (var kvp in _cooldowns.ToList())
        {
            var remaining = kvp.Value - now;
            if (remaining > TimeSpan.Zero)
            {
                result[kvp.Key] = remaining;
            }
            else
            {
                _cooldowns.Remove(kvp.Key); // 清理过期的冷却
            }
        }
        
        return result;
    }
}

/// <summary>
/// 角色技能管理器
/// </summary>
public class CharacterSkillManager
{
    // 学会的技能
    private readonly Dictionary<string, LearnedSkill> _learnedSkills = new();
    
    // 每个职业装备的技能
    private readonly Dictionary<string, List<string>> _equippedSkills = new();
    
    // 技能冷却管理
    public SkillCooldownManager CooldownManager { get; private set; } = new();

    public IReadOnlyDictionary<string, LearnedSkill> LearnedSkills => _learnedSkills;
    public IReadOnlyDictionary<string, List<string>> EquippedSkills => _equippedSkills.ToDictionary(
        kvp => kvp.Key, 
        kvp => kvp.Value.AsReadOnly().ToList()
    );

    /// <summary>
    /// 学习技能
    /// </summary>
    public bool LearnSkill(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId) || _learnedSkills.ContainsKey(skillId))
            return false;

        _learnedSkills[skillId] = new LearnedSkill(skillId);
        return true;
    }

    /// <summary>
    /// 忘记技能
    /// </summary>
    public bool ForgetSkill(string skillId)
    {
        if (!_learnedSkills.ContainsKey(skillId))
            return false;

        _learnedSkills.Remove(skillId);
        
        // 从所有职业的装备技能中移除
        foreach (var profession in _equippedSkills.Keys.ToList())
        {
            _equippedSkills[profession].Remove(skillId);
        }

        return true;
    }

    /// <summary>
    /// 装备技能到职业
    /// </summary>
    public bool EquipSkill(string profession, string skillId)
    {
        if (!_learnedSkills.ContainsKey(skillId))
            return false;

        if (!_equippedSkills.TryGetValue(profession, out var skills))
        {
            skills = new List<string>();
            _equippedSkills[profession] = skills;
        }

        if (!skills.Contains(skillId))
        {
            skills.Add(skillId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 卸下技能
    /// </summary>
    public bool UnequipSkill(string profession, string skillId)
    {
        if (_equippedSkills.TryGetValue(profession, out var skills))
        {
            return skills.Remove(skillId);
        }
        return false;
    }

    /// <summary>
    /// 使用技能
    /// </summary>
    public bool UseSkill(string skillId)
    {
        if (!_learnedSkills.TryGetValue(skillId, out var learnedSkill))
            return false;

        if (CooldownManager.IsOnCooldown(skillId))
            return false;

        learnedSkill.RecordUsage();
        return true;
    }

    /// <summary>
    /// 检查是否学会了技能
    /// </summary>
    public bool HasLearnedSkill(string skillId)
    {
        return _learnedSkills.ContainsKey(skillId);
    }

    /// <summary>
    /// 获取职业的装备技能
    /// </summary>
    public List<string> GetEquippedSkills(string profession)
    {
        return _equippedSkills.GetValueOrDefault(profession, new List<string>()).ToList();
    }

    /// <summary>
    /// 获取技能使用次数
    /// </summary>
    public int GetSkillUsageCount(string skillId)
    {
        return _learnedSkills.GetValueOrDefault(skillId)?.UsageCount ?? 0;
    }
}

/// <summary>
/// 技能类型枚举
/// </summary>
public enum SkillType
{
    Active,     // 主动技能
    Passive,    // 被动技能
    Buff,       // 增益技能
    Debuff,     // 减益技能
    Utility     // 功能技能
}

/// <summary>
/// 技能分类枚举
/// </summary>
public enum SkillCategory
{
    Combat,         // 战斗技能
    Gathering,      // 采集技能
    Production,     // 生产技能
    Social,         // 社交技能
    Movement,       // 移动技能
    Defensive,      // 防御技能
    Support,        // 支援技能
    Ultimate        // 终极技能
}

/// <summary>
/// 技能效果类型枚举
/// </summary>
public enum SkillEffectType
{
    Damage,             // 伤害
    Heal,               // 治疗
    BuffStat,           // 属性增益
    DebuffStat,         // 属性减益
    Shield,             // 护盾
    Stun,               // 眩晕
    Slow,               // 减速
    Haste,              // 加速
    Invisible,          // 隐身
    Teleport,           // 传送
    SummonCreature,     // 召唤生物
    AreaDamage,         // 范围伤害
    LifeSteal,          // 生命偷取
    ManaSteal,          // 法力偷取
    Cleanse,            // 净化
    Resurrect,          // 复活
    GatheringBonus,     // 采集加成
    CraftingBonus,      // 制作加成
    ExperienceBonus,    // 经验加成
    MovementSpeed,      // 移动速度
    Custom              // 自定义效果
}