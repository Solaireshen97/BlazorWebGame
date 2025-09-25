using System.Collections.Generic;
using System.Linq;
using BlazorWebGame.Services.Equipments;

namespace BlazorWebGame.Models.Items
{
    /// <summary>
    /// ��������װ������Ʒ������
    /// </summary>
    public static class EquipmentData
    {
        // ��̬���캯�������ڳ�ʼ�����ɵ�װ��
        static EquipmentData()
        {
            // ���Ԥ���װ��ʵ��
            // ע��_items ��ͨ����̬��ʼ����������һЩ����װ��

            // ���ͨ��װ�����������ɵ�װ��
            AddGeneratedEquipment();
        }

        private static readonly List<Equipment> _items = new()
        {
            // --- ����װ�� ---
            new Equipment
            {
                Id = "EQ_WEP_001", Name = "���������",
                Description = "һ�ѿ�����������˪�Ľ���",
                Slot = EquipmentSlot.MainHand,
                AttackBonus = 3,
                Value = 5,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "����", Price = 10 }
            },
            new Equipment
            {
                Id = "EQ_WEP_002", Name = "�粼�ֹ���",
                Description = "�粼�ֳ��õļ�ª������",
                Slot = EquipmentSlot.MainHand,
                AttackBonus = 5,
                Value = 10,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "����", Price = 1 }
            },
            new Equipment
            {
                Id = "EQ_WEP_003", Name = "��ħ֮��",
                Description = "һ��ȼ���ŵ���֮���ǿ��������",
                Slot = EquipmentSlot.MainHand,
                AttackBonus = 15,
                Value = 500,
                ShopPurchaseInfo = new PurchaseInfo
                {
                    ShopCategory = "����һ�",
                    Price = 1,
                    Currency = CurrencyType.Item,
                    CurrencyItemId = "MAT_DEMON_ESSENCE"
                }
            },
            new Equipment
            {
                Id = "EQ_WEP_COPPER_DAGGER",
                Name = "ͭ��ذ��",
                Description = "һ����ͭ����ļ���ذ�ף�����������Ҫ��һЩ��",
                Type = ItemType.Equipment,
                Value = 25,
                Slot = EquipmentSlot.MainHand,
                AttackBonus = 3,
                IsStackable = false
            },
            
            // --- ����װ�� ---
            new Equipment
            {
                Id = "EQ_CHEST_001", Name = "�ƾɵ�Ƥ��",
                Description = "���ṩ������ķ�����",
                Slot = EquipmentSlot.Chest,
                HealthBonus = 20,
                Value = 15,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "����", Price = 30 }
            },
            new Equipment
            {
                Id = "EQ_HANDS_001", Name = "��������",
                Description = "һ˫��ʵ��Ƥ���ף�����Ĳɼ���������Ч�ʡ�",
                Slot = EquipmentSlot.Hands,
                GatheringSpeedBonus = 0.1, // +10% �ɼ��ٶ�
                Value = 50,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "����", Price = 100 }
            },
            new Equipment
            {
                Id = "EQ_HANDS_RAGGED_GLOVES",
                Name = "���õ�Ƥ����",
                Description = "��Ƥ����Ƭ��ǿ�����һ������ס�",
                Type = ItemType.Equipment,
                Value = 20,
                Slot = EquipmentSlot.Hands,
                HealthBonus = 2,
                IsStackable = false
            },
            new Equipment
            {
                Id = "EQ_CHEST_LINEN_SHIRT",
                Name = "�����������",
                Description = "һ�����ص����鲼���£����ṩЩ�������",
                Type = ItemType.Equipment,
                Value = 30,
                Slot = EquipmentSlot.Chest,
                HealthBonus = 5,
                IsStackable = false
            },
            
            // --- ��Ʒװ�� ---
            new Equipment
            {
                Id = "EQ_FINGER_TIGERSEYE_RING",
                Name = "����ʯ��ָ",
                Description = "һö�򵥵�ͭ��ָ����Ƕ��һ�Ż���ʯ��",
                Type = ItemType.Equipment,
                Value = 75,
                Slot = EquipmentSlot.Finger1,
                AttackBonus = 2,
                HealthBonus = 5,
                IsStackable = false
            }
        };

        /// <summary>
        /// ��ȡ����װ����Ʒ
        /// </summary>
        public static List<Equipment> Items => _items;

        /// <summary>
        /// ����ID����װ����Ʒ
        /// </summary>
        public static Equipment? GetById(string id) => _items.FirstOrDefault(i => i.Id == id);
        
        /// <summary>
        /// ��ȡ����װ����Ʒ��ΪItem����
        /// </summary>
        public static List<Item> AllAsItems => _items.Cast<Item>().ToList();

        /// <summary>
        /// ���ͨ��װ�����������ɵ�װ��
        /// </summary>
        private static void AddGeneratedEquipment()
        {
            // ���ɲ�ͬ���𡢲�ͬƷ�ʵ�����װ��
            var weapon1 = EquipmentGenerator.GenerateEquipment(
                name: "�����Ķ̽�",
                level: 1,
                slot: EquipmentSlot.MainHand,
                quality: EquipmentGenerator.EquipmentQuality.Common,
                weaponType: WeaponType.Sword);
            weapon1.Id = "EQ_WEP_GEN_001";
            weapon1.ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "����", Price = 1 };
            _items.Add(weapon1);

            var weapon2 = EquipmentGenerator.GenerateEquipment(
                name: "�����Թ�",
                level: 10,
                slot: EquipmentSlot.MainHand,
                quality: EquipmentGenerator.EquipmentQuality.Uncommon,
                weaponType: WeaponType.Bow,
                isTwoHanded: true);
            weapon2.Id = "EQ_WEP_GEN_002";
            _items.Add(weapon2);

            // ���ɷ���װ��
            var armor1 = EquipmentGenerator.GenerateEquipment(
                name: "��̵�Ƥ��",
                level: 8,
                slot: EquipmentSlot.Chest,
                quality: EquipmentGenerator.EquipmentQuality.Common,
                armorType: ArmorType.Leather);
            armor1.Id = "EQ_CHEST_GEN_001";
            _items.Add(armor1);

            // ������Ʒװ��
            var accessory1 = EquipmentGenerator.GenerateEquipment(
                name: "����ʯ��ָ",
                level: 12,
                slot: EquipmentSlot.Finger1,
                quality: EquipmentGenerator.EquipmentQuality.Rare);
            accessory1.Id = "EQ_RING_GEN_001";
            _items.Add(accessory1);
        }

        /// <summary>
        /// �������������һ���µ����ɵ�װ��
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
            // ����ΨһID
            string id = $"EQ_GEN_{_items.Count + 1:D3}";

            // ����װ��
            var equipment = EquipmentGenerator.GenerateEquipment(
                name: name,
                level: level,
                slot: slot,
                quality: quality,
                weaponType: weaponType,
                armorType: armorType,
                isTwoHanded: isTwoHanded);

            // ����ID����ӵ��б�
            equipment.Id = id;
            _items.Add(equipment);

            return equipment;
        }
    }
}