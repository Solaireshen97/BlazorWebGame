namespace BlazorWebGame.Models
{
    public enum ConsumableCategory { Potion, Food, Recipe }
    public enum FoodType
    {
        None,       // 非食物，或其他
        Combat,     // 战斗食物
        Gathering,  // 采集食物
        Production  // *** 这是新增的行：生产食物 ***
    }
    public enum ConsumableEffectType { Heal, StatBuff, LearnRecipe }

    public class Consumable : Item
    {
        public ConsumableCategory Category { get; set; }
        public FoodType FoodType { get; set; } = FoodType.None;
        public ConsumableEffectType Effect { get; set; }
        public double EffectValue { get; set; }
        public double? DurationSeconds { get; set; }
        public double CooldownSeconds { get; set; }
        public StatBuffType? BuffType { get; set; } // *** 这是修正点 ***
        public string? RecipeIdToLearn { get; set; }
    }
}