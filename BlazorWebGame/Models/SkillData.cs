using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models
{
    public static class SkillData
    {
        private static readonly List<Skill> _allSkills = new()
        {
            // --- 战士技能 ---
            new Skill
            {
                Id = "WAR_001", Name = "英勇打击",
                Description = "对敌人造成额外5点伤害。",
                Type = SkillType.Fixed,
                RequiredProfession = BattleProfession.Warrior, RequiredLevel = 1,
                InitialCooldownRounds = 0, CooldownRounds = 2, // 每2回合触发一次
                EffectType = SkillEffectType.DirectDamage, EffectValue = 5
            },
            new Skill
            {
                Id = "WAR_002", Name = "恢复之风",
                Description = "恢复自身10点生命值。",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Warrior, RequiredLevel = 3,
                InitialCooldownRounds = 1, CooldownRounds = 5, // 每5回合触发一次
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
                EffectType = SkillEffectType.Heal, EffectValue = 0.05 // 使用百分比
            },
            
            // --- 法师技能 ---
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
                EffectType = SkillEffectType.DirectDamage, EffectValue = 5 // 这里可以做一个复合效果，暂时简化
            },
            // --- 新增：怪物技能 ---
            new Skill
            {
                Id = "MON_001", Name = "猛击",
                Description = "对玩家造成8点额外伤害。",
                RequiredProfession = null, // 表示怪物技能
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

        public static List<Skill> AllSkills => _allSkills;

        public static Skill? GetSkillById(string id) => _allSkills.FirstOrDefault(s => s.Id == id);
    }
}