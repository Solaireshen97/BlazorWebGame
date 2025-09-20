using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models
{
    public static class ItemData
    {
        private static readonly List<Item> _allItems = new()
        {
            // --- 特殊货币 ---
            new Item
            {
                Id = "MAT_DEMON_ESSENCE", Name = "恶魔精华",
                Description = "从强大恶魔身上收集到的能量核心，可以用来交换稀有物品。",
                Type = ItemType.Material,
                IsStackable = true,
                Value = 100 // 它的卖出价也很高
            },

            // --- 武器 ---
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
                // 这个物品怪物掉落，但商店不出售，所以 ShopPurchaseInfo 为 null
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
            
            // --- 护甲 ---
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