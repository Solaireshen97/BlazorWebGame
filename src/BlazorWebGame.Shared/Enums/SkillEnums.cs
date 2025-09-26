namespace BlazorWebGame.Shared.Enums
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
    /// 定义技能效果的类型
    /// </summary>
    public enum SkillEffectType
    {
        /// <summary>
        /// 造成额外的直接伤害
        /// </summary>
        DirectDamage,

        /// <summary>
        /// 治疗玩家自身
        /// </summary>
        Heal,

        // 未来可以扩展，例如：
        // AttackPowerBuff,     // 攻击力增益
        // DamageOverTime,      // 持续伤害 (DoT)
        // Stun,                // 眩晕
    }
}