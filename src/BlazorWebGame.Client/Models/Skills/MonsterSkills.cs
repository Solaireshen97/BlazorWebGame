using System.Collections.Generic;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Models.Skills
{
    /// <summary>
    /// ����ר�ü�������
    /// </summary>
    public static class MonsterSkills
    {
        public static readonly List<Skill> Skills = new()
        {
            new Skill
            {
                Id = "MON_001", Name = "�ͻ�",
                Description = "��������8������˺���",
                RequiredProfession = null,
                CooldownRounds = 3, InitialCooldownRounds = 1,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 8
            },
            new Skill
            {
                Id = "MON_002", Name = "С������",
                Description = "�ָ�����15������ֵ��",
                RequiredProfession = null,
                CooldownRounds = 5, InitialCooldownRounds = 3,
                EffectType = SkillEffectType.Heal, EffectValue = 15
            },
            new Skill
            {
                Id = "MON_003", Name = "��ʴ",
                Description = "��������3���˺���",
                RequiredProfession = null,
                CooldownRounds = 2, InitialCooldownRounds = 0,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 3
            }
        };
    }
}