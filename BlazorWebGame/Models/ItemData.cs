using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models
{
    public static class ItemData
    {
        private static readonly List<Item> _allItems = new()
        {
            // --- 新增采集装备 ---
            new Equipment
            {
                Id = "EQ_HANDS_001", Name = "工匠手套",
                Description = "一双结实的皮手套，让你的采集工作更有效率。",
                Slot = EquipmentSlot.Hands,
                GatheringSpeedBonus = 0.1, // +10% 采集速度
                Value = 50,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "防具", Price = 100 }
            },

            // --- 消耗品 ---
            new Consumable
            {
                Id = "CON_HP_POTION_1", Name = "初级治疗药水",
                Description = "立即恢复50点生命值。",
                Value = 25,
                Category = ConsumableCategory.Potion, // 修改
                Effect = ConsumableEffectType.Heal,
                EffectValue = 50,
                CooldownSeconds = 20,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "消耗品", Price = 1 }
            },
            new Consumable
            {
                Id = "CON_FOOD_ATK_1", Name = "烤狼肉",
                Description = "在60秒内，提高5点攻击力。",
                Value = 15,
                Category = ConsumableCategory.Food, // 修改
                FoodType = FoodType.Combat,        // 新增
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.AttackPower,
                EffectValue = 5,
                DurationSeconds = 60,
                CooldownSeconds = 60,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "消耗品", Price = 30 }
            },
            new Consumable
            {
                Id = "CON_FOOD_GATHER_1", Name = "矿工炖菜",
                Description = "在120秒内，提高15%的采集速度。",
                Value = 20,
                Category = ConsumableCategory.Food, // 修改
                FoodType = FoodType.Gathering,     // 新增
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
                Category = ConsumableCategory.Food, // 修改
                FoodType = FoodType.Gathering,     // 新增
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.ExtraLootChance,
                EffectValue = 5,
                DurationSeconds = 120,
                CooldownSeconds = 120,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "消耗品", Price = 75 }
            },
            // --- 草药 (新增) ---
            new Item
            {
                Id = "HERB_PEACEBLOOM", Name = "宁神花",
                Description = "一种常见的白色小花，散发着安宁的香气。",
                Type = ItemType.Material, IsStackable = true, Value = 2
            },
            new Item
            {
                Id = "HERB_SILVERLEAF", Name = "银叶草",
                Description = "叶片上带有银色纹路的植物，在月光下会微微发光。",
                Type = ItemType.Material, IsStackable = true, Value = 5
            },
            new Item
            {
                Id = "HERB_MAGEROYAL", Name = "魔皇草",
                Description = "被认为蕴含着魔法能量的稀有植物，深受法师们的喜爱。",
                Type = ItemType.Material, IsStackable = true, Value = 15
            },

            // --- 特殊货币 ---
            new Item
            {
                Id = "MAT_DEMON_ESSENCE", Name = "恶魔精华",
                Description = "从强大恶魔身上收集到的能量核心，可以用来交换稀有物品。",
                Type = ItemType.Material,
                IsStackable = true,
                Value = 100
            },

            // ... 其他物品 ...
            new Equipment
            {
                Id = "EQ_WEP_001", Name = "生锈的铁剑",
                Description = "一把看起来饱经风霜的剑。",
                Slot = EquipmentSlot.Weapon,
                AttackBonus = 3,
                Value = 5,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "武器", Price = 10 }
            },
            new Equipment
            {
                Id = "EQ_WEP_002", Name = "哥布林棍棒",
                Description = "哥布林常用的简陋武器。",
                Slot = EquipmentSlot.Weapon,
                AttackBonus = 5,
                Value = 10
            },
            new Equipment
            {
                Id = "EQ_WEP_003", Name = "恶魔之刃",
                Description = "一把燃烧着地狱之火的强大武器。",
                Slot = EquipmentSlot.Weapon,
                AttackBonus = 15,
                Value = 500,
                ShopPurchaseInfo = new PurchaseInfo
                {
                    ShopCategory = "特殊兑换",
                    Price = 1,
                    Currency = CurrencyType.Item,
                    CurrencyItemId = "MAT_DEMON_ESSENCE"
                }
            },
            new Equipment
            {
                Id = "EQ_CHEST_001", Name = "破旧的皮甲",
                Description = "能提供最基础的防护。",
                Slot = EquipmentSlot.Chest,
                HealthBonus = 20,
                Value = 15,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "防具", Price = 30 }
            }
        };

        public static List<Item> AllItems => _allItems;

        public static Item? GetItemById(string id) => _allItems.FirstOrDefault(i => i.Id == id);
    }
}