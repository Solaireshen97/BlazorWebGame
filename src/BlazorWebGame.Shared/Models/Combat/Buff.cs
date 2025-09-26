using BlazorWebGame.Shared.Enums;

namespace BlazorWebGame.Shared.Models.Combat
{
    public class Buff
    {
        public required string SourceItemId { get; set; }
        public StatBuffType BuffType { get; set; }
        public double BuffValue { get; set; }
        public double TimeRemainingSeconds { get; set; }
        public FoodType FoodType { get; set; } = FoodType.None; // 区分食物类型以避免同类叠加
    }
}