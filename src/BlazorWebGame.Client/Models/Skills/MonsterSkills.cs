using System.Collections.Generic;

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