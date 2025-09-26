using System.Collections.Generic;
using System.Linq;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Models.Items
{
    /// <summary>
    /// 包含所有消耗品类物品的数据
    /// </summary>
    public static class ConsumableData
    {
        private static readonly List<Consumable> _items = new()
        {
            // --- 药水 ---
            new Consumable
            {
                Id = "CON_HP_POTION_1", Name = "初级治疗药水",
                Description = "立即恢复50点生命值。",
                Value = 25,
                Category = ConsumableCategory.Potion,
                Effect = ConsumableEffectType.Heal,
                EffectValue = 50,
                CooldownSeconds = 20,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "消耗品", Price = 1 }
            },
            
            // --- 战斗食物 ---
            new Consumable
            {
                Id = "CON_FOOD_ATK_1", Name = "烤狼肉",
                Description = "在60秒内，提高5点攻击力。",
                Value = 15,
                Category = ConsumableCategory.Food,
                FoodType = FoodType.Combat,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.AttackPower,
                EffectValue = 5,
                DurationSeconds = 60,
                CooldownSeconds = 60,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "消耗品", Price = 30 }
            },
            new Consumable
            {
                Id = "FOOD_GOBLIN_OMELETTE", Name = "哥布林煎蛋",
                Description = "味道很奇怪，但能让你在战斗中更勇猛。食用后提高攻击力。",
                Type = ItemType.Consumable, IsStackable = true, Value = 25,
                Category = ConsumableCategory.Food, FoodType = FoodType.Combat,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.AttackPower,
                EffectValue = 2,
                DurationSeconds = 300, CooldownSeconds = 5
            },
            
            // --- 采集食物 ---
            new Consumable
            {
                Id = "CON_FOOD_GATHER_1", Name = "矿工炖菜",
                Description = "在120秒内，提高15%的采集速度。",
                Value = 20,
                Category = ConsumableCategory.Food,
                FoodType = FoodType.Gathering,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.GatheringSpeed,
                EffectValue = 15,
                DurationSeconds = 120,
                CooldownSeconds = 120,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "消耗品", Price = 50 }
            },
            new Consumable
            {
                Id = "CON_FOOD_LUCK_1", Name = "寻宝者点心",
                Description = "在120秒内，采集时有5%的几率获得额外收获。",
                Value = 35,
                Category = ConsumableCategory.Food,
                FoodType = FoodType.Gathering,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.ExtraLootChance,
                EffectValue = 5,
                DurationSeconds = 120,
                CooldownSeconds = 120,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "消耗品", Price = 75 }
            },
            new Consumable
            {
                Id = "FOOD_COOKED_TROUT", Name = "烤鳟鱼",
                Description = "简单的美味，食用后短时间内提高你的采集速度。",
                Type = ItemType.Consumable, IsStackable = true, Value = 10,
                Category = ConsumableCategory.Food, FoodType = FoodType.Gathering,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.GatheringSpeed,
                EffectValue = 5,
                DurationSeconds = 300, CooldownSeconds = 5
            },
            
            // --- 制作食物 ---
            new Consumable
            {
                Id = "CON_FOOD_CRAFT_1", Name = "工匠蜜糖面包",
                Description = "香甜的面包让你更加专注。在180秒内，提高10%的制作速度。",
                Value = 25,
                Category = ConsumableCategory.Food,
                FoodType = FoodType.Production,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.CraftingSpeed,
                EffectValue = 10,
                DurationSeconds = 180,
                CooldownSeconds = 180,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "消耗品", Price = 60 }
            },
            
            // --- 图纸配方 ---
            new Consumable
            {
                Id = "RECIPE_ITEM_GOBLIN_OMELETTE", Name = "食谱：哥布林煎蛋",
                Description = "教会你如何制作哥布林煎蛋。",
                Type = ItemType.Consumable, IsStackable = false, Value = 50,
                Category = ConsumableCategory.Recipe,
                Effect = ConsumableEffectType.LearnRecipe,
                RecipeIdToLearn = "RECIPE_GOBLIN_OMELETTE",
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "消耗品", Price = 1 }
            },
            new Consumable
            {
                Id = "RECIPE_ITEM_ENG_ROUGH_BOMB",
                Name = "结构图：劣质铜管炸弹",
                Description = "教会你如何制作劣质铜管炸弹。",
                Type = ItemType.Consumable,
                IsStackable = false,
                Value = 30,
                Category = ConsumableCategory.Recipe,
                Effect = ConsumableEffectType.LearnRecipe,
                RecipeIdToLearn = "RECIPE_ENG_ROUGH_BOMB"
            },
            
            // --- 工程学物品 ---
            new Consumable
            {
                Id = "CON_ENG_ROUGH_BOMB",
                Name = "劣质铜管炸弹",
                Description = "一个不稳定的爆炸物，可以对敌人造成少量范围伤害。",
                Type = ItemType.Consumable,
                Value = 15,
                IsStackable = true,
                Category = ConsumableCategory.Potion
            }
        };

        /// <summary>
        /// 获取所有消耗品
        /// </summary>
        public static List<Consumable> Items => _items;

        /// <summary>
        /// 根据ID查找消耗品
        /// </summary>
        public static Consumable? GetById(string id) => _items.FirstOrDefault(i => i.Id == id);
        
        /// <summary>
        /// 获取所有消耗品作为Item类型
        /// </summary>
        public static List<Item> AllAsItems => _items.Cast<Item>().ToList();
    }
}