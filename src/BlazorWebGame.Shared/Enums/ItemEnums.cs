namespace BlazorWebGame.Shared.Enums
{
    public enum ItemType
    {
        Equipment, // 装备
        Consumable, // 消耗品
        Material,  // 材料
    }

    public enum ConsumableCategory
    {
        Potion,
        Food,
        Recipe
    }

    public enum FoodType
    {
        None,       // 非食物，或其他
        Combat,     // 战斗食物
        Gathering,  // 采集食物
        Production  // 生产食物
    }

    public enum ConsumableEffectType
    {
        Heal,
        StatBuff,
        LearnRecipe
    }

    public enum CurrencyType
    {
        Gold, // 金币
        Item  // 特殊物品
    }
}