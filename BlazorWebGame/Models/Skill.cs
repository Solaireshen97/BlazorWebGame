namespace BlazorWebGame.Models
{
    /// <summary>
    /// 技能类型
    /// </summary>
    public enum SkillType
    {
        /// <summary>
        /// 固定技能，职业自带且必须携带
        /// </summary>
        Fixed,
        /// <summary>
        /// 职业技能，只有该职业能学习和使用
        /// </summary>
        Profession,
        /// <summary>
        /// 共享技能，被一个职业学会后，所有职业都可以使用
        /// </summary>
        Shared
    }

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