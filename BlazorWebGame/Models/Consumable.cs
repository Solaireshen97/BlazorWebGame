namespace BlazorWebGame.Models
{
    /// <summary>
    /// 消耗品效果类型
    /// </summary>
    public enum ConsumableEffectType
    {
        Heal,       // 直接治疗
        StatBuff    // 属性增益
    }

    /// <summary>
    /// 属性增益的类型
    /// </summary>
    public enum StatBuffType
    {
        AttackPower, // 攻击力
        MaxHealth    // 最大生命值
    }

    /// <summary>
    /// 代表可消耗的物品，如药水、食物等
    /// </summary>
    public class Consumable : Item
    {
        public ConsumableEffectType Effect { get; set; }
        public double EffectValue { get; set; }
        public double? DurationSeconds { get; set; } // Buff的持续时间（秒）
        public StatBuffType? BuffType { get; set; }

        public Consumable()
        {
            Type = ItemType.Consumable;
            IsStackable = true; // 消耗品通常是可堆叠的
        }
    }
}