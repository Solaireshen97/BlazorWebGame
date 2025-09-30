using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 自动施放引擎
/// </summary>
public class AutoCastEngine
{
    private readonly SkillRepository _skillRepository;
    private readonly IConditionContext _conditionContext;

    public AutoCastEngine(SkillRepository skillRepository, IConditionContext conditionContext)
    {
        _skillRepository = skillRepository;
        _conditionContext = conditionContext;
    }

    /// <summary>
    /// 尝试自动施放技能
    /// </summary>
    public AutoCastResult TryAutoCast(
        BattleInstance battle,
        CombatantState caster,
        List<string> skillPriority,
        GameClock clock)
    {
        foreach (var skillId in skillPriority)
        {
            var skill = _skillRepository.GetSkill(skillId);
            if (skill == null) continue;

            // 检查冷却
            if (battle.SkillCooldowns.TryGetValue($"{caster.Id}_{skillId}", out var cd))
            {
                if (!cd.IsReady(clock.CurrentTime))
                    continue;
            }

            // 检查资源
            var resources = battle.ResourceBuckets
                .Where(kvp => kvp.Key.StartsWith(caster.Id))
                .ToDictionary(kvp => kvp.Key.Replace($"{caster.Id}_", ""), kvp => kvp.Value);

            if (!skill.CanAffordCosts(resources))
                continue;

            // 检查条件要求
            if (!string.IsNullOrEmpty(skill.RequirementExpr))
            {
                var condition = new ConditionExpr(skill.RequirementExpr);
                if (!condition.Evaluate(_conditionContext))
                    continue;
            }

            // 可以施放
            return new AutoCastResult
            {
                Success = true,
                SkillId = skillId,
                CasterId = caster.Id,
                TargetId = caster.CurrentTargetId
            };
        }

        return new AutoCastResult { Success = false };
    }

    /// <summary>
    /// 执行技能施放
    /// </summary>
    public void ExecuteCast(
        BattleInstance battle,
        CombatantState caster,
        Skill skill,
        string? targetId,
        GameClock clock,
        IGameContext context)
    {
        // 消耗资源
        foreach (var cost in skill.ResourceCosts)
        {
            var bucketKey = $"{caster.Id}_{cost.Key}";
            if (battle.ResourceBuckets.TryGetValue(bucketKey, out var bucket))
            {
                bucket.Consume(cost.Value);
            }
        }

        // 设置冷却
        var cooldownKey = $"{caster.Id}_{skill.Id}";
        battle.SkillCooldowns[cooldownKey] = new CooldownState
        {
            SkillId = skill.Id,
            ReadyAt = clock.CurrentTime.Add(skill.Cooldown)
        };

        // 发出技能施放事件
        context.EmitDomainEvent(new SkillCastEvent
        {
            AggregateId = battle.Id.ToString(),
            SkillId = skill.Id,
            CasterId = caster.Id,
            TargetId = targetId,
            ResourceCosts = skill.ResourceCosts
        });

        // 应用技能效果
        ApplySkillEffects(battle, caster, skill, targetId, clock, context);
    }

    private void ApplySkillEffects(
        BattleInstance battle,
        CombatantState caster,
        Skill skill,
        string? targetId,
        GameClock clock,
        IGameContext context)
    {
        foreach (var effect in skill.Effects)
        {
            switch (effect.Type)
            {
                case SkillEffectType.Damage:
                    if (!string.IsNullOrEmpty(targetId))
                    {
                        var damageEvent = new DamageEvent
                        {
                            AggregateId = battle.Id.ToString(),
                            SourceId = caster.Id,
                            TargetId = targetId,
                            Amount = effect.Value,
                            SkillId = skill.Id
                        };
                        context.EmitDomainEvent(damageEvent);
                    }
                    break;

                case SkillEffectType.BuffStat:
                    var buff = new BuffInstance
                    {
                        BuffDefId = effect.Id,
                        SourceId = caster.Id,
                        TargetId = targetId ?? caster.Id,
                        AppliedAt = clock.CurrentTime,
                        ExpiresAt = effect.Duration.HasValue
                            ? clock.CurrentTime.Add(effect.Duration.Value)
                            : null
                    };
                    battle.ActiveBuffs.Add(buff);
                    break;

                case SkillEffectType.Heal:
                    // TODO: 实现治疗逻辑
                    break;
            }
        }
    }
}

/// <summary>
/// 自动施放结果
/// </summary>
public class AutoCastResult
{
    public bool Success { get; set; }
    public string? SkillId { get; set; }
    public string? CasterId { get; set; }
    public string? TargetId { get; set; }
    public string? FailureReason { get; set; }
}

/// <summary>
/// 自动施放策略
/// </summary>
public abstract class AutoCastPolicy
{
    public abstract List<string> DetermineSkillOrder(
        CombatantState caster,
        BattleInstance battle,
        List<string> availableSkills);
}

/// <summary>
/// 基础优先级策略
/// </summary>
public class PriorityAutocastPolicy : AutoCastPolicy
{
    public override List<string> DetermineSkillOrder(
        CombatantState caster,
        BattleInstance battle,
        List<string> availableSkills)
    {
        // 简单返回槽位顺序
        return availableSkills;
    }
}

/// <summary>
/// 轮换策略
/// </summary>
public class RotationAutocastPolicy : AutoCastPolicy
{
    private readonly Dictionary<string, int> _lastCastIndex = new();

    public override List<string> DetermineSkillOrder(
        CombatantState caster,
        BattleInstance battle,
        List<string> availableSkills)
    {
        if (!_lastCastIndex.ContainsKey(caster.Id))
            _lastCastIndex[caster.Id] = -1;

        var lastIndex = _lastCastIndex[caster.Id];
        var nextIndex = (lastIndex + 1) % availableSkills.Count;

        _lastCastIndex[caster.Id] = nextIndex;

        // 返回从nextIndex开始的循环顺序
        var result = new List<string>();
        for (int i = 0; i < availableSkills.Count; i++)
        {
            var index = (nextIndex + i) % availableSkills.Count;
            result.Add(availableSkills[index]);
        }

        return result;
    }
}

/// <summary>
/// 技能仓库
/// </summary>
public class SkillRepository
{
    private readonly Dictionary<string, Skill> _skills = new();

    public void LoadSkills(ConfigBundle config)
    {
        // TODO: 从配置加载技能定义
    }

    public Skill? GetSkill(string skillId)
    {
        return _skills.GetValueOrDefault(skillId);
    }
}