using BlazorWebGame.Models;

namespace BlazorWebGame.Models
{
    // 定义增益效果的具体类型
    public enum StatBuffType
    {
        AttackPower,      // 攻击力
        MaxHealth,        // 最大生命值
        GatheringSpeed,   // 采集速度
        ExtraLootChance,   // 额外掉落几率
        CraftingSpeed
    }

    public class Buff
    {
        public required string SourceItemId { get; set; }
        public StatBuffType BuffType { get; set; }
        public double BuffValue { get; set; }
        public double TimeRemainingSeconds { get; set; }
        public FoodType FoodType { get; set; } = FoodType.None; // 区分食物类型以避免同类叠加
    }
}