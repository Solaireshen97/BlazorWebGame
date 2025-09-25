using System.Collections.Generic;
using System.Linq;
using BlazorWebGame.Services.Equipments;

namespace BlazorWebGame.Models.Items
{
    /// <summary>
    /// 包含所有装备类物品的数据
    /// </summary>
    public static class EquipmentData
    {
        // 静态构造函数，用于初始化生成的装备
        static EquipmentData()
        {
            // 添加预设的装备实例
            // 注：_items 已通过静态初始化器定义了一些基本装备

            // 添加通过装备生成器生成的装备
            AddGeneratedEquipment();
        }

        private static readonly List<Equipment> _items = new()
        {
            // --- 武器装备 ---
            new Equipment
            {
                Id = "EQ_WEP_001", Name = "生锈的铁剑",
                Description = "一把看起来饱经风霜的剑。",
                Slot = EquipmentSlot.MainHand,
                AttackBonus = 3,
                Value = 5,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "武器", Price = 10 }
            },
            new Equipment
            {
                Id = "EQ_WEP_002", Name = "哥布林棍棒",
                Description = "哥布林常用的简陋武器。",
                Slot = EquipmentSlot.MainHand,
                AttackBonus = 5,
                Value = 10,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "武器", Price = 1 }
            },
            new Equipment
            {
                Id = "EQ_WEP_003", Name = "恶魔之刃",
                Description = "一把燃烧着地狱之火的强大武器。",
                Slot = EquipmentSlot.MainHand,
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
                Id = "EQ_WEP_COPPER_DAGGER",
                Name = "铜质匕首",
                Description = "一把用铜打造的简易匕首，比新手武器要好一些。",
                Type = ItemType.Equipment,
                Value = 25,
                Slot = EquipmentSlot.MainHand,
                AttackBonus = 3,
                IsStackable = false
            },
            
            // --- 防具装备 ---
            new Equipment
            {
                Id = "EQ_CHEST_001", Name = "破旧的皮甲",
                Description = "能提供最基础的防护。",
                Slot = EquipmentSlot.Chest,
                HealthBonus = 20,
                Value = 15,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "防具", Price = 30 }
            },
            new Equipment
            {
                Id = "EQ_HANDS_001", Name = "工匠手套",
                Description = "一双结实的皮手套，让你的采集工作更有效率。",
                Slot = EquipmentSlot.Hands,
                GatheringSpeedBonus = 0.1, // +10% 采集速度
                Value = 50,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "防具", Price = 100 }
            },
            new Equipment
            {
                Id = "EQ_HANDS_RAGGED_GLOVES",
                Name = "破烂的皮手套",
                Description = "用皮革碎片勉强缝合在一起的手套。",
                Type = ItemType.Equipment,
                Value = 20,
                Slot = EquipmentSlot.Hands,
                HealthBonus = 2,
                IsStackable = false
            },
            new Equipment
            {
                Id = "EQ_CHEST_LINEN_SHIRT",
                Name = "简易亚麻衬衣",
                Description = "一件朴素的亚麻布衬衣，能提供些许防护。",
                Type = ItemType.Equipment,
                Value = 30,
                Slot = EquipmentSlot.Chest,
                HealthBonus = 5,
                IsStackable = false
            },
            
            // --- 饰品装备 ---
            new Equipment
            {
                Id = "EQ_FINGER_TIGERSEYE_RING",
                Name = "虎眼石戒指",
                Description = "一枚简单的铜戒指，镶嵌着一颗虎眼石。",
                Type = ItemType.Equipment,
                Value = 75,
                Slot = EquipmentSlot.Finger1,
                AttackBonus = 2,
                HealthBonus = 5,
                IsStackable = false
            }
        };

        /// <summary>
        /// 获取所有装备物品
        /// </summary>
        public static List<Equipment> Items => _items;

        /// <summary>
        /// 根据ID查找装备物品
        /// </summary>
        public static Equipment? GetById(string id) => _items.FirstOrDefault(i => i.Id == id);
        
        /// <summary>
        /// 获取所有装备物品作为Item类型
        /// </summary>
        public static List<Item> AllAsItems => _items.Cast<Item>().ToList();

        /// <summary>
        /// 添加通过装备生成器生成的装备
        /// </summary>
        private static void AddGeneratedEquipment()
        {
            // 生成不同级别、不同品质的武器装备
            var weapon1 = EquipmentGenerator.GenerateEquipment(
                name: "锋利的短剑",
                level: 1,
                slot: EquipmentSlot.MainHand,
                quality: EquipmentGenerator.EquipmentQuality.Common,
                weaponType: WeaponType.Sword);
            weapon1.Id = "EQ_WEP_GEN_001";
            weapon1.ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "武器", Price = 1 };
            _items.Add(weapon1);

            var weapon2 = EquipmentGenerator.GenerateEquipment(
                name: "精工猎弓",
                level: 10,
                slot: EquipmentSlot.MainHand,
                quality: EquipmentGenerator.EquipmentQuality.Uncommon,
                weaponType: WeaponType.Bow,
                isTwoHanded: true);
            weapon2.Id = "EQ_WEP_GEN_002";
            _items.Add(weapon2);

            // 生成防具装备
            var armor1 = EquipmentGenerator.GenerateEquipment(
                name: "坚固的皮甲",
                level: 8,
                slot: EquipmentSlot.Chest,
                quality: EquipmentGenerator.EquipmentQuality.Common,
                armorType: ArmorType.Leather);
            armor1.Id = "EQ_CHEST_GEN_001";
            _items.Add(armor1);

            // 生成饰品装备
            var accessory1 = EquipmentGenerator.GenerateEquipment(
                name: "幸运石戒指",
                level: 12,
                slot: EquipmentSlot.Finger1,
                quality: EquipmentGenerator.EquipmentQuality.Rare);
            accessory1.Id = "EQ_RING_GEN_001";
            _items.Add(accessory1);
        }

        /// <summary>
        /// 公开方法：添加一个新的生成的装备
        /// </summary>
        public static Equipment AddNewGeneratedEquipment(
            string name,
            int level,
            EquipmentSlot slot,
            EquipmentGenerator.EquipmentQuality quality,
            WeaponType weaponType = WeaponType.None,
            ArmorType armorType = ArmorType.None,
            bool isTwoHanded = false)
        {
            // 生成唯一ID
            string id = $"EQ_GEN_{_items.Count + 1:D3}";

            // 生成装备
            var equipment = EquipmentGenerator.GenerateEquipment(
                name: name,
                level: level,
                slot: slot,
                quality: quality,
                weaponType: weaponType,
                armorType: armorType,
                isTwoHanded: isTwoHanded);

            // 设置ID并添加到列表
            equipment.Id = id;
            _items.Add(equipment);

            return equipment;
        }
    }
}