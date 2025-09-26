using BlazorWebGame.Shared.Enums;

namespace BlazorWebGame.Shared.Models.Skills
{
    /// <summary>
    /// 定义一个技能
    /// </summary>
    public class Skill
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SkillType Type { get; set; }

        /// <summary>
        /// 学习此技能所需的主要职业。
        /// 如果为 null，则表示这是怪物专属技能。
        /// </summary>
        public BattleProfession? RequiredProfession { get; set; }

        public int RequiredLevel { get; set; }

        public int InitialCooldownRounds { get; set; } = 0;
        public int CooldownRounds { get; set; } = 1;
        public SkillEffectType EffectType { get; set; }
        public double EffectValue { get; set; } = 0;
    }
}