using System.Collections.Generic;

namespace BlazorWebGame.Models.Skills
{
    /// <summary>
    /// 怪物专用技能数据
    /// </summary>
    public static class MonsterSkills
    {
        public static readonly List<Skill> Skills = new()
        {
            new Skill
            {
                Id = "MON_001", Name = "猛击",
                Description = "对玩家造成8点额外伤害。",
                RequiredProfession = null,
                CooldownRounds = 3, InitialCooldownRounds = 1,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 8
            },
            new Skill
            {
                Id = "MON_002", Name = "小型治疗",
                Description = "恢复自身15点生命值。",
                RequiredProfession = null,
                CooldownRounds = 5, InitialCooldownRounds = 3,
                EffectType = SkillEffectType.Heal, EffectValue = 15
            },
            new Skill
            {
                Id = "MON_003", Name = "腐蚀",
                Description = "对玩家造成3点伤害。",
                RequiredProfession = null,
                CooldownRounds = 2, InitialCooldownRounds = 0,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 3
            }
        };
    }
}