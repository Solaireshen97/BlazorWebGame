using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models
{
    public static class ItemData
    {
        private static readonly List<Item> _allItems = new()
        {
            // --- 武器 ---
            new Equipment
            {
                Id = "EQ_WEP_001", Name = "生锈的铁剑",
                Description = "一把看起来饱经风霜的剑。",
                Slot = EquipmentSlot.Weapon,
                AttackBonus = 3,
                Value = 5
            },
            new Equipment
            {
                Id = "EQ_WEP_002", Name = "哥布林棍棒",
                Description = "哥布林常用的简陋武器。",
                Slot = EquipmentSlot.Weapon,
                AttackBonus = 5,
                Value = 10
            },
            
            // --- 护甲 ---
            new Equipment
            {
                Id = "EQ_CHEST_001", Name = "破旧的皮甲",
                Description = "能提供最基础的防护。",
                Slot = EquipmentSlot.Chest,
                HealthBonus = 20,
                Value = 15
            }
        };

        public static List<Item> AllItems => _allItems;

        public static Item? GetItemById(string id) => _allItems.FirstOrDefault(i => i.Id == id);
    }
}