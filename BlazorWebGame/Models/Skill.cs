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
        /// <summary>
        /// 唯一的技能ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 技能名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 技能描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 技能类型
        /// </summary>
        public SkillType Type { get; set; }

        /// <summary>
        /// 学习此技能所需的主要职业
        /// </summary>
        public BattleProfession RequiredProfession { get; set; }

        /// <summary>
        /// 学习此技能所需的等级
        /// </summary>
        public int RequiredLevel { get; set; }
    }
}