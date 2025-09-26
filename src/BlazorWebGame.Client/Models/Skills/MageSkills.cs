using System.Collections.Generic;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Models.Skills
{
    /// <summary>
    /// 法师职业的所有技能数据
    /// </summary>
    public static class MageSkills
    {
        public static readonly List<Skill> Skills = new()
        {
            new Skill
            {
                Id = "MAGE_001", Name = "火球术",
                Description = "对敌人造成8点火焰伤害。",
                Type = SkillType.Fixed,
                RequiredProfession = BattleProfession.Mage, RequiredLevel = 1,
                InitialCooldownRounds = 0, CooldownRounds = 2,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 8
            },
            new Skill
            {
                Id = "MAGE_002", Name = "寒冰箭",
                Description = "对敌人造成12点冰霜伤害。",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Mage, RequiredLevel = 3,
                InitialCooldownRounds = 1, CooldownRounds = 3,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 12
            },
            new Skill
            {
                Id = "MAGE_003", Name = "生命虹吸",
                Description = "对敌人造成5点伤害，并为自己恢复5点生命。",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Mage, RequiredLevel = 5,
                InitialCooldownRounds = 2, CooldownRounds = 4,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 5
            }
        };
    }
}