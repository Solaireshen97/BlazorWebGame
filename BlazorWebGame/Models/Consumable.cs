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
    /// 消耗品的大分类
    /// </summary>
    public enum ConsumableCategory
    {
        Potion, // 药剂
        Food    // 食物
    }

    /// <summary>
    /// 食物的具体子分类，用于区分用途和快捷栏
    /// </summary>
    public enum FoodType
    {
        None,
        Combat,    // 战斗食物
        Gathering, // 采集食物
        Production // 生产食物
    }


    /// <summary>
    /// 属性增益的类型
    /// </summary>
    public enum StatBuffType
    {
        AttackPower,     // 攻击力
        MaxHealth,       // 最大生命值
        GatheringSpeed,  // 采集速度
        ExtraLootChance, // 额外掉落几率
    }

    /// <summary>
    /// 代表可消耗的物品，如药水、食物等
    /// </summary>
    public class Consumable : Item
    {
        public ConsumableEffectType Effect { get; set; }
        public ConsumableCategory Category { get; set; }
        public FoodType FoodType { get; set; } = FoodType.None;
        public double EffectValue { get; set; }
        public double? DurationSeconds { get; set; } // Buff的持续时间（秒）
        public StatBuffType? BuffType { get; set; }
        public double CooldownSeconds { get; set; } = 1.0;

        public Consumable()
        {
            Type = ItemType.Consumable;
            IsStackable = true; // 消耗品通常是可堆叠的
        }
    }
}