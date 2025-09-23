using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Items
{
    /// <summary>
    /// 包含所有材料类物品的数据
    /// </summary>
    public static class MaterialData
    {
        private static readonly List<Item> _items = new()
        {
            // --- 草药 ---
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
            
            // --- 矿石和金属 ---
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
                Value = 8,
                IsStackable = true,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "素材", Price = 50 }
            },
            
            // --- 宝石 ---
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
            
            // --- 制作材料 ---
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
            new Item
            {
                Id = "MAT_RUINED_LEATHER_SCRAPS",
                Name = "破损的皮革碎片",
                Description = "可以合成粗制皮革。",
                Type = ItemType.Material,
                Value = 1,
                IsStackable = true,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "素材", Price = 50 }
            },
            new Item
            {
                Id = "MAT_COARSE_THREAD",
                Name = "粗线",
                Description = "用于缝制皮革制品。",
                Type = ItemType.Material,
                Value = 2,
                IsStackable = true,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "素材", Price = 50 }
            },
            new Item
            {
                Id = "MAT_LINEN_CLOTH",
                Name = "亚麻布",
                Description = "由亚麻纤维织成的布料，是裁缝的基础材料。",
                Type = ItemType.Material,
                Value = 3,
                IsStackable = true,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "素材", Price = 50 }
            },
            new Item
            {
                Id = "MAT_ROUGH_STONE",
                Name = "劣质的石头",
                Description = "可以被磨成粉末，用于制作爆炸物。",
                Type = ItemType.Material,
                Value = 1,
                IsStackable = true
            },
            
            // --- 鱼类 ---
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
            }
        };

        /// <summary>
        /// 获取所有材料物品
        /// </summary>
        public static List<Item> Items => _items;

        /// <summary>
        /// 根据ID查找材料物品
        /// </summary>
        public static Item? GetById(string id) => _items.FirstOrDefault(i => i.Id == id);
    }
}