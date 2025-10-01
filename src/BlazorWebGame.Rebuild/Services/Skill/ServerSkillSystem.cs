using BlazorWebGame.Shared.Models;
using System;
using System.Collections.Generic;

namespace BlazorWebGame.Rebuild.Services.Skill;

/// <summary>
/// 服务端技能系统 - 从客户端移植而来
/// </summary>
public class ServerSkillSystem
{
    private readonly ILogger<ServerSkillSystem> _logger;
    private readonly Dictionary<string, ServerSkillData> _skillDatabase;

    public ServerSkillSystem(ILogger<ServerSkillSystem> logger)
    {
        _logger = logger;
        _skillDatabase = InitializeSkillDatabase();
    }

    /// <summary>
    /// 初始化技能数据库
    /// </summary>
    private Dictionary<string, ServerSkillData> InitializeSkillDatabase()
    {
        return new Dictionary<string, ServerSkillData>
        {
            // 战士技能
            ["warrior_charge"] = new ServerSkillData
            {
                Id = "warrior_charge",
                Name = "冲锋",
                Description = "对目标造成额外伤害",
                EffectType = "DirectDamage",
                EffectValue = 20,
                CooldownSeconds = 5.0,
                ManaCost = 15,
                Profession = "Warrior"
            },
            ["warrior_shield_bash"] = new ServerSkillData
            {
                Id = "warrior_shield_bash",
                Name = "盾击",
                Description = "击晕敌人并造成伤害",
                EffectType = "DamageAndStun",
                EffectValue = 15,
                CooldownSeconds = 8.0,
                ManaCost = 20,
                Profession = "Warrior"
            },
            
            // 法师技能
            ["mage_fireball"] = new ServerSkillData
            {
                Id = "mage_fireball",
                Name = "火球术",
                Description = "对目标造成火焰伤害",
                EffectType = "DirectDamage",
                EffectValue = 25,
                CooldownSeconds = 3.0,
                ManaCost = 20,
                Profession = "Mage"
            },
            ["mage_heal"] = new ServerSkillData
            {
                Id = "mage_heal",
                Name = "治疗术",
                Description = "恢复生命值",
                EffectType = "Heal",
                EffectValue = 30,
                CooldownSeconds = 6.0,
                ManaCost = 25,
                Profession = "Mage"
            },

            // 盗贼技能
            ["rogue_backstab"] = new ServerSkillData
            {
                Id = "rogue_backstab",
                Name = "背刺",
                Description = "造成巨额伤害",
                EffectType = "CriticalDamage",
                EffectValue = 30,
                CooldownSeconds = 7.0,
                ManaCost = 18,
                Profession = "Rogue"
            }
        };
    }

    /// <summary>
    /// 应用角色技能效果
    /// </summary>
    public void ApplyPlayerSkills(ServerBattlePlayer player, ServerBattleEnemy target, ServerBattleContext battle, double deltaTime)
    {
        // 更新技能冷却
        UpdateSkillCooldowns(player, deltaTime);

        // 检查和应用被动技能
        ApplyPassiveSkills(player, target, battle);
    }

    /// <summary>
    /// 应用敌人技能效果
    /// </summary>
    public void ApplyEnemySkills(ServerBattleEnemy enemy, ServerBattlePlayer target, ServerBattleContext battle, double deltaTime)
    {
        // 更新技能冷却
        UpdateEnemySkillCooldowns(enemy, deltaTime);

        // 简单的AI技能使用
        TryUseEnemySkills(enemy, target, battle);
    }

    /// <summary>
    /// 执行特定技能
    /// </summary>
    public bool ExecuteSkill(string skillId, ServerBattleParticipant caster, ServerBattleParticipant target, ServerBattleContext battle)
    {
        if (!_skillDatabase.TryGetValue(skillId, out var skillData))
        {
            _logger.LogWarning("Skill not found: {SkillId}", skillId);
            return false;
        }

        // 检查技能是否在冷却中
        if (caster.SkillCooldowns.TryGetValue(skillId, out var remainingCooldown) && remainingCooldown > 0)
        {
            return false;
        }

        // 检查是否装备了此技能
        if (!caster.EquippedSkills.Contains(skillId))
        {
            return false;
        }

        // 应用技能效果
        ApplySkillEffect(skillData, caster, target, battle);

        // 设置冷却时间
        caster.SkillCooldowns[skillId] = skillData.CooldownSeconds;

        _logger.LogDebug("Skill {SkillName} used by {CasterName} on {TargetName}", 
            skillData.Name, caster.Name, target.Name);

        return true;
    }

    /// <summary>
    /// 应用技能效果
    /// </summary>
    private void ApplySkillEffect(ServerSkillData skill, ServerBattleParticipant caster, ServerBattleParticipant target, ServerBattleContext battle)
    {
        switch (skill.EffectType)
        {
            case "DirectDamage":
                ApplyDirectDamage(skill, caster, target, battle);
                break;
                
            case "CriticalDamage":
                ApplyCriticalDamage(skill, caster, target, battle);
                break;
                
            case "DamageAndStun":
                ApplyDirectDamage(skill, caster, target, battle);
                // TODO: 实现眩晕效果
                break;
                
            case "Heal":
                ApplyHeal(skill, caster, battle);
                break;
                
            case "BuffAttack":
                ApplyAttackBuff(skill, caster);
                break;
                
            case "BuffDefense":
                ApplyDefenseBuff(skill, caster);
                break;
                
            default:
                _logger.LogWarning("Unknown skill effect type: {EffectType}", skill.EffectType);
                break;
        }
    }

    /// <summary>
    /// 应用直接伤害
    /// </summary>
    private void ApplyDirectDamage(ServerSkillData skill, ServerBattleParticipant caster, ServerBattleParticipant target, ServerBattleContext battle)
    {
        var damage = CalculateSkillDamage(skill, caster);
        var originalHealth = target.Health;
        target.Health = Math.Max(0, target.Health - damage);
        var actualDamage = originalHealth - target.Health;

        // 记录战斗动作
        var action = new ServerBattleAction
        {
            ActorId = caster.Id,
            ActorName = caster.Name,
            TargetId = target.Id,
            TargetName = target.Name,
            ActionType = "UseSkill",
            SkillId = skill.Id,
            Damage = actualDamage,
            Timestamp = DateTime.UtcNow
        };

        battle.ActionHistory.Add(action);
    }

    /// <summary>
    /// 应用暴击伤害
    /// </summary>
    private void ApplyCriticalDamage(ServerSkillData skill, ServerBattleParticipant caster, ServerBattleParticipant target, ServerBattleContext battle)
    {
        var damage = CalculateSkillDamage(skill, caster);
        damage = (int)(damage * caster.CriticalMultiplier); // 暴击伤害

        var originalHealth = target.Health;
        target.Health = Math.Max(0, target.Health - damage);
        var actualDamage = originalHealth - target.Health;

        // 记录战斗动作
        var action = new ServerBattleAction
        {
            ActorId = caster.Id,
            ActorName = caster.Name,
            TargetId = target.Id,
            TargetName = target.Name,
            ActionType = "UseSkill",
            SkillId = skill.Id,
            Damage = actualDamage,
            IsCritical = true,
            Timestamp = DateTime.UtcNow
        };

        battle.ActionHistory.Add(action);
    }

    /// <summary>
    /// 应用治疗效果
    /// </summary>
    private void ApplyHeal(ServerSkillData skill, ServerBattleParticipant caster, ServerBattleContext battle)
    {
        var healAmount = (int)skill.EffectValue;
        var originalHealth = caster.Health;
        caster.Health = Math.Min(caster.MaxHealth, caster.Health + healAmount);
        var actualHeal = caster.Health - originalHealth;

        // 记录战斗动作
        var action = new ServerBattleAction
        {
            ActorId = caster.Id,
            ActorName = caster.Name,
            TargetId = caster.Id,
            TargetName = caster.Name,
            ActionType = "UseSkill",
            SkillId = skill.Id,
            Damage = -actualHeal, // 负数表示治疗
            Timestamp = DateTime.UtcNow
        };

        battle.ActionHistory.Add(action);
    }

    /// <summary>
    /// 应用攻击增益
    /// </summary>
    private void ApplyAttackBuff(ServerSkillData skill, ServerBattleParticipant caster)
    {
        // 临时增加攻击力
        caster.BaseAttackPower += (int)skill.EffectValue;
        // 实际实现应该使用buff系统
    }

    /// <summary>
    /// 应用防御增益
    /// </summary>
    private void ApplyDefenseBuff(ServerSkillData skill, ServerBattleParticipant caster)
    {
        // 临时增加躲避率
        caster.DodgeChance = Math.Min(0.8, caster.DodgeChance + skill.EffectValue / 100.0);
    }

    /// <summary>
    /// 计算技能伤害
    /// </summary>
    private int CalculateSkillDamage(ServerSkillData skill, ServerBattleParticipant caster)
    {
        var baseDamage = skill.EffectValue;
        var attackPowerBonus = caster.BaseAttackPower * 0.3; // 30% 攻击力加成
        return (int)(baseDamage + attackPowerBonus);
    }

    /// <summary>
    /// 更新技能冷却时间
    /// </summary>
    private void UpdateSkillCooldowns(ServerBattleParticipant participant, double deltaTime)
    {
        var skillsToUpdate = participant.SkillCooldowns.Keys.ToList();
        foreach (var skillId in skillsToUpdate)
        {
            var cooldown = participant.SkillCooldowns[skillId];
            if (cooldown > 0)
            {
                participant.SkillCooldowns[skillId] = Math.Max(0, cooldown - deltaTime);
            }
        }
    }

    /// <summary>
    /// 更新敌人技能冷却
    /// </summary>
    private void UpdateEnemySkillCooldowns(ServerBattleEnemy enemy, double deltaTime)
    {
        UpdateSkillCooldowns(enemy, deltaTime);
    }

    /// <summary>
    /// 应用被动技能
    /// </summary>
    private void ApplyPassiveSkills(ServerBattlePlayer player, ServerBattleEnemy target, ServerBattleContext battle)
    {
        // 这里可以实现被动技能逻辑
        // 例如：每回合恢复生命值、增加攻击力等
    }

    /// <summary>
    /// 尝试使用敌人技能
    /// </summary>
    private void TryUseEnemySkills(ServerBattleEnemy enemy, ServerBattlePlayer target, ServerBattleContext battle)
    {
        // 简单的AI：随机使用可用的技能
        var availableSkills = enemy.EquippedSkills
            .Where(skillId => !enemy.SkillCooldowns.TryGetValue(skillId, out var cd) || cd <= 0)
            .ToList();

        if (availableSkills.Any() && Random.Shared.NextDouble() < 0.3) // 30% 概率使用技能
        {
            var randomSkill = availableSkills[Random.Shared.Next(availableSkills.Count)];
            ExecuteSkill(randomSkill, enemy, target, battle);
        }
    }

    /// <summary>
    /// 获取技能信息
    /// </summary>
    public ServerSkillData? GetSkillById(string skillId)
    {
        return _skillDatabase.TryGetValue(skillId, out var skill) ? skill : null;
    }
}

/// <summary>
/// 服务端技能数据
/// </summary>
public class ServerSkillData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EffectType { get; set; } = string.Empty;
    public double EffectValue { get; set; }
    public double CooldownSeconds { get; set; }
    public int ManaCost { get; set; }
    public string Profession { get; set; } = string.Empty;
    public bool IsPassive { get; set; }
    public int Level { get; set; } = 1;
}