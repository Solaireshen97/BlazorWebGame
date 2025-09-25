namespace BlazorWebGame.Models
{
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