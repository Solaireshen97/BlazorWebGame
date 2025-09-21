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
            // --- 新增的图纸消耗品 ---
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

            // --- 新增的烹饪食物 (*** 这是修正点 ***) ---
            new Consumable
            {
                Id = "FOOD_COOKED_TROUT", Name = "烤鳟鱼",
                Description = "简单的美味，食用后短时间内提高你的采集速度。",
                Type = ItemType.Consumable, IsStackable = true, Value = 10,
                Category = ConsumableCategory.Food, FoodType = FoodType.Gathering,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.GatheringSpeed, // 修正: BuffType -> StatBuffType
                EffectValue = 5, // 5%采集速度
                DurationSeconds = 300, CooldownSeconds = 5
            },
            new Consumable
            {
                Id = "FOOD_GOBLIN_OMELETTE", Name = "哥布林煎蛋",
                Description = "味道很奇怪，但能让你在战斗中更勇猛。食用后提高攻击力。",
                Type = ItemType.Consumable, IsStackable = true, Value = 25,
                Category = ConsumableCategory.Food, FoodType = FoodType.Combat,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.AttackPower, // 修正: BuffType -> StatBuffType
                EffectValue = 2, // 2点攻击力
                DurationSeconds = 300, CooldownSeconds = 5
            },
            new Consumable
            {
                Id = "CON_FOOD_CRAFT_1", Name = "工匠蜜糖面包",
                Description = "香甜的面包让你更加专注。在180秒内，提高10%的制作速度。",
                Value = 25,
                Category = ConsumableCategory.Food,
                FoodType = FoodType.Production, // *** 使用新的食物类型 ***
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.CraftingSpeed,
                EffectValue = 10, // 10 代表 10%
                DurationSeconds = 180,
                CooldownSeconds = 180,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "消耗品", Price = 60 }
            },
            // --- 草药 (新增) ---
            new Item
            {
                Id = "HERB_PEACEBLOOM", Name = "宁神花",
                Description = "一种常见的白色小花，散发着安宁的香气。",
                Type = ItemType.Material, IsStackable = true, Value = 2,
                ShopPurchaseInfo = new PurchaseInfo
                {
                    ShopCategory = "素材",
                    Currency = CurrencyType.Gold,
                    Price = 10
                }
            },
            new Item
            {
                Id = "HERB_SILVERLEAF", Name = "银叶草",
                Description = "叶片上带有银色纹路的植物，在月光下会微微发光。",
                Type = ItemType.Material, IsStackable = true, Value = 5,
                ShopPurchaseInfo = new PurchaseInfo
                {
                    ShopCategory = "素材",
                    Currency = CurrencyType.Gold,
                    Price = 10
                }
            },
            new Item
            {
                Id = "HERB_MAGEROYAL", Name = "魔皇草",
                Description = "被认为蕴含着魔法能量的稀有植物，深受法师们的喜爱。",
                Type = ItemType.Material, IsStackable = true, Value = 15
            },

            // --- 新增：矿石 ---
            new Item
            {
                Id = "ORE_COPPER", Name = "铜矿石",
                Description = "一种基础的金属矿石，可以用于锻造。",
                Type = ItemType.Material, IsStackable = true, Value = 4,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "素材", Price = 10 }
            },
            new Item
            {
                Id = "ORE_IRON", Name = "铁矿石",
                Description = "比铜更坚固的金属矿石。",
                Type = ItemType.Material, IsStackable = true, Value = 10,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "素材", Price = 15 }
            },
            new Item
            {
                Id = "BAR_COPPER",
                Name = "铜锭",
                Description = "由铜矿石熔炼而成的金属锭，是锻造的基础材料。",
                Type = ItemType.Material,
                Value = 8, // 价值比矿石高
                IsStackable = true,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "素材", Price = 50 }
            },
        // ... 在 _allItems 列表中添加以下新物品 ...

// --- 珠宝加工材料 ---
new Item
{
    Id = "GEM_ROUGH_TIGERSEYE",
    Name = "劣质的虎眼石",
    Description = "一块未经打磨的宝石，内部似乎有微光流动。",
    Type = ItemType.Material,
    Value = 10,
    IsStackable = true,
                    ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "素材", Price = 50 }
},
new Item
{
    Id = "GEM_TIGERSEYE",
    Name = "虎眼石",
    Description = "经过精细切割的虎眼石，可以镶嵌在首饰上。",
    Type = ItemType.Material,
    Value = 25,
    IsStackable = true,
                    ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "素材", Price = 50 }
},
new Item
{
    Id = "MAT_COPPER_WIRE",
    Name = "铜丝",
    Description = "由铜锭拉成的细丝，用于制作珠宝的基座。",
    Type = ItemType.Material,
    Value = 12,
    IsStackable = true,
                    ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "素材", Price = 50 }
},

// --- 珠宝加工成品 ---
new Equipment
{
    Id = "EQ_FINGER_TIGERSEYE_RING",
    Name = "虎眼石戒指",
    Description = "一枚简单的铜戒指，镶嵌着一颗虎眼石。",
    Type = ItemType.Equipment,
    Value = 75,
    Slot = EquipmentSlot.Finger, // 使用我们新增的装备槽
    AttackBonus = 2, // 假设提供少量攻击力
    HealthBonus = 5, // 和少量生命值
    IsStackable = false
},
            // --- 新增装备 ---
            new Equipment
            {
                Id = "EQ_WEP_COPPER_DAGGER",
                Name = "铜质匕首",
                Description = "一把用铜打造的简易匕首，比新手武器要好一些。",
                Type = ItemType.Equipment,
                Value = 25,
                Slot = EquipmentSlot.Weapon,
                AttackBonus = 3, // 假设比初始武器攻击力高
                IsStackable = false
            },
            // --- 新增：鱼类 ---
            new Item
            {
                Id = "FISH_TROUT", Name = "生鳟鱼",
                Description = "一条普通的河鱼，可以用于烹饪。",
                Type = ItemType.Material, IsStackable = true, Value = 6
            },
            new Item
            {
                Id = "FISH_BASS", Name = "闪光鲈鱼",
                Description = "在阳光下鳞片闪闪发光，似乎很值钱。",
                Type = ItemType.Material, IsStackable = true, Value = 18
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
                Value = 10,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "武器", Price = 1 }
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