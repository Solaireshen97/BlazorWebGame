using System.Collections.Generic;

namespace BlazorWebGame.Models.Skills
{
    /// <summary>
    /// 战士职业的所有技能数据
    /// </summary>
    public static class WarriorSkills
    {
        public static readonly List<Skill> Skills = new()
        {
            new Skill
            {
                Id = "WAR_001", Name = "英勇打击",
                Description = "对敌人造成额外5点伤害。",
                Type = SkillType.Fixed,
                RequiredProfession = BattleProfession.Warrior, RequiredLevel = 1,
                InitialCooldownRounds = 0, CooldownRounds = 2,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 5
            },
            new Skill
            {
                Id = "WAR_002", Name = "恢复之风",
                Description = "恢复自身10点生命值。",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Warrior, RequiredLevel = 3,
                InitialCooldownRounds = 1, CooldownRounds = 5,
                EffectType = SkillEffectType.Heal, EffectValue = 10
            },
            new Skill
            {
                Id = "WAR_003", Name = "毁灭打击",
                Description = "对敌人造成毁灭性的25点伤害。",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Warrior, RequiredLevel = 5,
                InitialCooldownRounds = 2, CooldownRounds = 6,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 25
            },
            new Skill
            {
                Id = "SHARED_001", Name = "肾上腺素",
                Description = "立刻恢复5%的最大生命值。",
                Type = SkillType.Shared,
                RequiredProfession = BattleProfession.Warrior, RequiredLevel = 10,
                InitialCooldownRounds = 3, CooldownRounds = 10,
                EffectType = SkillEffectType.Heal, EffectValue = 0.05
            }
        };
    }
}