using System.Collections.Generic;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Models.Skills
{
    /// <summary>
    /// ��ʦְҵ�����м�������
    /// </summary>
    public static class MageSkills
    {
        public static readonly List<Skill> Skills = new()
        {
            new Skill
            {
                Id = "MAGE_001", Name = "������",
                Description = "�Ե������8������˺���",
                Type = SkillType.Fixed,
                RequiredProfession = BattleProfession.Mage, RequiredLevel = 1,
                InitialCooldownRounds = 0, CooldownRounds = 2,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 8
            },
            new Skill
            {
                Id = "MAGE_002", Name = "������",
                Description = "�Ե������12���˪�˺���",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Mage, RequiredLevel = 3,
                InitialCooldownRounds = 1, CooldownRounds = 3,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 12
            },
            new Skill
            {
                Id = "MAGE_003", Name = "��������",
                Description = "�Ե������5���˺�����Ϊ�Լ��ָ�5��������",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Mage, RequiredLevel = 5,
                InitialCooldownRounds = 2, CooldownRounds = 4,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 5
            }
        };
    }
}