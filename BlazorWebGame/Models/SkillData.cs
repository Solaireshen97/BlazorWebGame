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
                Description = "战士的基础攻击技能，造成标准伤害。",
                Type = SkillType.Fixed,
                RequiredProfession = BattleProfession.Warrior,
                RequiredLevel = 1
            },
            new Skill
            {
                Id = "WAR_002", Name = "盾牌猛击",
                Description = "对敌人造成少量伤害，并使其眩晕（未来实现）。",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Warrior,
                RequiredLevel = 3
            },
            new Skill
            {
                Id = "WAR_003", Name = "顺劈斩",
                Description = "一次强大的范围攻击（未来实现）。",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Warrior,
                RequiredLevel = 5
            },
            new Skill
            {
                Id = "SHARED_001", Name = "肾上腺素",
                Description = "短时间内大幅提升攻击速度（未来实现）。",
                Type = SkillType.Shared,
                RequiredProfession = BattleProfession.Warrior,
                RequiredLevel = 10 // 战士10级解锁，之后法师也能用
            },
            
            // --- 法师技能 ---
            new Skill
            {
                Id = "MAGE_001", Name = "火球术",
                Description = "法师的基础攻击技能，造成火焰伤害。",
                Type = SkillType.Fixed,
                RequiredProfession = BattleProfession.Mage,
                RequiredLevel = 1
            },
            new Skill
            {
                Id = "MAGE_002", Name = "寒冰箭",
                Description = "对敌人造成冰霜伤害，并使其减速（未来实现）。",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Mage,
                RequiredLevel = 3
            },
            new Skill
            {
                Id = "MAGE_003", Name = "奥术智慧",
                Description = "提升自己的法术强度（未来实现）。",
                Type = SkillType.Profession,
                RequiredProfession = BattleProfession.Mage,
                RequiredLevel = 5
            },
        };

        public static List<Skill> AllSkills => _allSkills;

        public static Skill? GetSkillById(string id) => _allSkills.FirstOrDefault(s => s.Id == id);
    }
}