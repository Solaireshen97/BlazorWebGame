using BlazorWebGame.Shared.Enums;

namespace BlazorWebGame.Shared.Models.Items
{
    public class Consumable : Item
    {
        public ConsumableCategory Category { get; set; }
        public FoodType FoodType { get; set; } = FoodType.None;
        public ConsumableEffectType Effect { get; set; }
        public double EffectValue { get; set; }
        public double? DurationSeconds { get; set; }
        public double CooldownSeconds { get; set; }
        public StatBuffType? BuffType { get; set; }
        public string? RecipeIdToLearn { get; set; }

        public Consumable()
        {
            Type = ItemType.Consumable;
        }
    }
}