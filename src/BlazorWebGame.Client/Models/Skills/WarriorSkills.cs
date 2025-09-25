using System.Collections.Generic;

namespace BlazorWebGame.Models.Skills
{
    /// <summary>
    /// սʿְҵ�����м�������
    /// </summary>
    public static class WarriorSkills
    {
        public static readonly List<Skill> Skills = new()
        {
            new Skill
            {
                Id = "WAR_001", Name = "Ӣ�´��",
                Description = "�Ե�����ɶ���5���˺���",
                Type = SkillType.Fixed,
                RequiredProfession = BattleProfession.Warrior, RequiredLevel = 1,
                InitialCooldownRounds = 0, CooldownRounds = 2,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 5
            },
            new Skill
            {
                Id = "WAR_002", Name = "�ָ�֮��",
                Description = "�ָ�����10������ֵ��",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Warrior, RequiredLevel = 3,
                InitialCooldownRounds = 1, CooldownRounds = 5,
                EffectType = SkillEffectType.Heal, EffectValue = 10
            },
            new Skill
            {
                Id = "WAR_003", Name = "������",
                Description = "�Ե�����ɻ����Ե�25���˺���",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Warrior, RequiredLevel = 5,
                InitialCooldownRounds = 2, CooldownRounds = 6,
                EffectType = SkillEffectType.DirectDamage, EffectValue = 25
            },
            new Skill
            {
                Id = "SHARED_001", Name = "��������",
                Description = "���ָ̻�5%���������ֵ��",
                Type = SkillType.Shared,
                RequiredProfession = BattleProfession.Warrior, RequiredLevel = 10,
                InitialCooldownRounds = 3, CooldownRounds = 10,
                EffectType = SkillEffectType.Heal, EffectValue = 0.05
            }
        };
    }
}