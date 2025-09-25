using BlazorWebGame.Models.Skills;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models
{
    /// <summary>
    /// 提供对所有技能的统一访问
    /// </summary>
    public static class SkillData
    {
        private static readonly List<Skill> _allSkills;

        static SkillData()
        {
            // 在静态构造函数中初始化所有技能
            _allSkills = new List<Skill>();
            _allSkills.AddRange(WarriorSkills.Skills);
            _allSkills.AddRange(MageSkills.Skills);
            _allSkills.AddRange(MonsterSkills.Skills);
        }

        /// <summary>
        /// 获取所有技能列表
        /// </summary>
        public static List<Skill> AllSkills => _allSkills;

        /// <summary>
        /// 根据ID获取技能
        /// </summary>
        public static Skill? GetSkillById(string id) =>
            _allSkills.FirstOrDefault(s => s.Id == id);

        /// <summary>
        /// 获取特定战斗职业的所有技能
        /// </summary>
        public static List<Skill> GetSkillsByProfession(BattleProfession profession) =>
            _allSkills.Where(s => s.RequiredProfession == profession).ToList();

        /// <summary>
        /// 获取特定类型的技能
        /// </summary>
        public static List<Skill> GetSkillsByType(SkillType type) =>
            _allSkills.Where(s => s.Type == type).ToList();

        /// <summary>
        /// 获取怪物技能
        /// </summary>
        public static List<Skill> GetMonsterSkills() =>
            _allSkills.Where(s => s.RequiredProfession == null).ToList();

        /// <summary>
        /// 获取特定等级及以下可用的职业技能
        /// </summary>
        public static List<Skill> GetAvailableSkills(BattleProfession profession, int level) =>
            _allSkills.Where(s =>
                s.RequiredProfession == profession &&
                s.RequiredLevel <= level &&
                s.Type != SkillType.Fixed).ToList();

        /// <summary>
        /// 获取角色的固定技能
        /// </summary>
        public static List<Skill> GetFixedSkills(BattleProfession profession) =>
            _allSkills.Where(s =>
                s.RequiredProfession == profession &&
                s.Type == SkillType.Fixed).ToList();

        /// <summary>
        /// 获取所有共享技能
        /// </summary>
        public static List<Skill> GetSharedSkills() =>
            _allSkills.Where(s => s.Type == SkillType.Shared).ToList();
    }
}