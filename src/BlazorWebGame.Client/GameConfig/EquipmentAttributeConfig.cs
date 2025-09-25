using System;
using System.Collections.Generic;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Services.Equipments;

namespace BlazorWebGame.GameConfig
{
    /// <summary>
    /// װ������ϵͳ�����ò���
    /// </summary>
    public static class EquipmentAttributeConfig
    {
        #region װ��Ʒ��ϵ��
        /// <summary>
        /// װ��Ʒ�������Ա���
        /// </summary>
        public static Dictionary<EquipmentGenerator.EquipmentQuality, double> QualityMainAttributeMultipliers { get; } = new Dictionary<EquipmentGenerator.EquipmentQuality, double>
        {
            { EquipmentGenerator.EquipmentQuality.Common, 10.0 },   // ��װ: 10 * �ȼ�
            { EquipmentGenerator.EquipmentQuality.Uncommon, 15.0 }, // ��װ: 15 * �ȼ�
            { EquipmentGenerator.EquipmentQuality.Rare, 20.0 },     // ��װ: 20 * �ȼ�
            { EquipmentGenerator.EquipmentQuality.Epic, 30.0 }      // ��װ: 30 * �ȼ�
        };

        /// <summary>
        /// ����Ʒ�����Ա���
        /// </summary>
        public static Dictionary<EquipmentGenerator.EquipmentQuality, double> WeaponMainAttributeMultipliers { get; } = new Dictionary<EquipmentGenerator.EquipmentQuality, double>
        {
            { EquipmentGenerator.EquipmentQuality.Common, 1.0 },    // ��װ: 1 * �ȼ�
            { EquipmentGenerator.EquipmentQuality.Uncommon, 1.25 }, // ��װ: 1.25 * �ȼ�
            { EquipmentGenerator.EquipmentQuality.Rare, 1.5 },      // ��װ: 1.5 * �ȼ�
            { EquipmentGenerator.EquipmentQuality.Epic, 2.0 }       // ��װ: 2 * �ȼ�
        };

        /// <summary>
        /// ��ͬƷ��װ���ĸ���������
        /// </summary>
        public static Dictionary<EquipmentGenerator.EquipmentQuality, int> QualitySecondaryAttributeCount { get; } = new Dictionary<EquipmentGenerator.EquipmentQuality, int>
        {
            { EquipmentGenerator.EquipmentQuality.Common, 0 },    // ��װ: 0��������
            { EquipmentGenerator.EquipmentQuality.Uncommon, 1 },  // ��װ: 1��������
            { EquipmentGenerator.EquipmentQuality.Rare, 2 },      // ��װ: 2��������
            { EquipmentGenerator.EquipmentQuality.Epic, 3 }       // ��װ: 3��������
        };

        /// <summary>
        /// Ʒ����ɫǰ׺
        /// </summary>
        public static Dictionary<EquipmentGenerator.EquipmentQuality, string> QualityPrefixes { get; } = new Dictionary<EquipmentGenerator.EquipmentQuality, string>
        {
            { EquipmentGenerator.EquipmentQuality.Common, "" },          // ��ɫ������Ҫǰ׺
            { EquipmentGenerator.EquipmentQuality.Uncommon, "[����] " },
            { EquipmentGenerator.EquipmentQuality.Rare, "[ϡ��] " },
            { EquipmentGenerator.EquipmentQuality.Epic, "[ʷʫ] " }
        };

        /// <summary>
        /// Ʒ����������
        /// </summary>
        public static Dictionary<EquipmentGenerator.EquipmentQuality, string> QualityDescriptions { get; } = new Dictionary<EquipmentGenerator.EquipmentQuality, string>
        {
            { EquipmentGenerator.EquipmentQuality.Common, "��ͨ��" },
            { EquipmentGenerator.EquipmentQuality.Uncommon, "���ʵ�" },
            { EquipmentGenerator.EquipmentQuality.Rare, "ϡ�е�" },
            { EquipmentGenerator.EquipmentQuality.Epic, "ʷʫ����" }
        };
        #endregion

        #region װ����λ���Է���
        /// <summary>
        /// װ����λ�����Է������
        /// </summary>
        public static Dictionary<EquipmentSlot, double> SlotMainAttributeRatios { get; } = new Dictionary<EquipmentSlot, double>
        {
            { EquipmentSlot.Head, 0.15 },      // ͷ��: 15%
            { EquipmentSlot.Neck, 0.08 },      // ����: 8%
            { EquipmentSlot.Shoulder, 0.12 },  // �粿: 12%
            { EquipmentSlot.Back, 0.10 },      // ����: 10%
            { EquipmentSlot.Chest, 0.16 },     // �ز�: 16%
            { EquipmentSlot.Wrist, 0.06 },     // ����: 6%
            { EquipmentSlot.Hands, 0.08 },     // �ֲ�: 8%
            { EquipmentSlot.Waist, 0.08 },     // ����: 8%
            { EquipmentSlot.Legs, 0.15 },      // �Ȳ�: 15%
            { EquipmentSlot.Feet, 0.10 },      // �Ų�: 10%
            { EquipmentSlot.Finger1, 0.05 },   // ��ָ1: 5%
            { EquipmentSlot.Finger2, 0.05 },   // ��ָ2: 5%
            { EquipmentSlot.Trinket1, 0.08 },  // ��Ʒ1: 8%
            { EquipmentSlot.Trinket2, 0.08 },  // ��Ʒ2: 8%
            { EquipmentSlot.MainHand, 1.0 },   // ����: �������
            { EquipmentSlot.OffHand, 0.5 }     // ����: �������
        };

        /// <summary>
        /// ���ײ�λ���η�
        /// </summary>
        public static Dictionary<EquipmentSlot, double> ArmorSlotModifiers { get; } = new Dictionary<EquipmentSlot, double>
        {
            { EquipmentSlot.Head, 1.0 },
            { EquipmentSlot.Chest, 1.2 },
            { EquipmentSlot.Legs, 1.1 },
            { EquipmentSlot.Shoulder, 0.9 },
            { EquipmentSlot.Hands, 0.7 },
            { EquipmentSlot.Feet, 0.8 },
            { EquipmentSlot.Wrist, 0.6 },
            { EquipmentSlot.Waist, 0.75 },
            { EquipmentSlot.Back, 0.65 }
        };
        
        /// <summary>
        /// װ����λ����
        /// </summary>
        public static Dictionary<EquipmentSlot, string> SlotDescriptions { get; } = new Dictionary<EquipmentSlot, string>
        {
            { EquipmentSlot.Head, "ͷ��" },
            { EquipmentSlot.Chest, "�ؼ�" },
            { EquipmentSlot.Legs, "�ȼ�" },
            { EquipmentSlot.Shoulder, "���" },
            { EquipmentSlot.Hands, "����" },
            { EquipmentSlot.Feet, "ѥ��" },
            { EquipmentSlot.Wrist, "����" },
            { EquipmentSlot.Waist, "����" },
            { EquipmentSlot.Back, "����" },
            { EquipmentSlot.Neck, "����" },
            { EquipmentSlot.Finger1, "��ָ" },
            { EquipmentSlot.Finger2, "��ָ" },
            { EquipmentSlot.Trinket1, "��Ʒ" },
            { EquipmentSlot.Trinket2, "��Ʒ" }
        };
        #endregion

        #region ��������ϵ��
        /// <summary>
        /// �������ͻ���ֵ���η�
        /// </summary>
        public static Dictionary<ArmorType, double> ArmorTypeModifiers { get; } = new Dictionary<ArmorType, double>
        {
            { ArmorType.Cloth, 0.8 },    // ����: 80%��������
            { ArmorType.Leather, 1.0 },  // Ƥ��: 100%��������
            { ArmorType.Mail, 1.3 },     // ����: 130%��������
            { ArmorType.Plate, 1.6 }     // ���: 160%��������
        };
        
        /// <summary>
        /// ������������
        /// </summary>
        public static Dictionary<ArmorType, string> ArmorTypeDescriptions { get; } = new Dictionary<ArmorType, string>
        {
            { ArmorType.Cloth, "����" },
            { ArmorType.Leather, "Ƥ��" },
            { ArmorType.Mail, "����" },
            { ArmorType.Plate, "���" }
        };
        #endregion

        #region ����ϵ������
        /// <summary>
        /// �����������η� - [�����˺�ϵ��, �����ٶ�]
        /// </summary>
        public static Dictionary<WeaponType, (double damage, double speed)> WeaponTypeModifiers { get; } = new Dictionary<WeaponType, (double damage, double speed)>
        {
            { WeaponType.Sword, (4.0, 0.425) },         // ��: ��׼�˺�, �Ͽ칥��
            { WeaponType.Dagger, (2.8, 0.55) },         // ذ��: �ϵ��˺�, ��칥��
            { WeaponType.Axe, (4.8, 0.375) },           // ��: ���˺�, ��������
            { WeaponType.Mace, (5.2, 0.35) },           // ��: �ܸ��˺�, ������
            { WeaponType.Staff, (4.0, 0.4) },           // ����: ��׼�˺�, ��׼����
            { WeaponType.Wand, (3.2, 0.475) },          // ħ��: ���˺�, �칥��
            { WeaponType.Bow, (3.6, 0.45) },            // ��: �ϵ��˺�, �Ͽ칥��
            { WeaponType.Crossbow, (4.8, 0.35) },       // ��: ���˺�, ������
            { WeaponType.Gun, (4.4, 0.375) },           // ǹ: �ϸ��˺�, ��������
            { WeaponType.Shield, (2.0, 0.3) },          // ����: ����˺�, ��������
            { WeaponType.TwoHandSword, (7.2, 0.375) },  // ˫�ֽ�: �����˺�, ��������
            { WeaponType.TwoHandAxe, (8.0, 0.325) },    // ˫�ָ�: ����˺�, �ǳ�������
            { WeaponType.TwoHandMace, (7.6, 0.3) },     // ˫�ִ�: �����˺�, ��������
            { WeaponType.Polearm, (6.8, 0.4) }          // ��������: �ܸ��˺�, ��׼����
        };

        /// <summary>
        /// ������������
        /// </summary>
        public static Dictionary<WeaponType, string> WeaponTypeDescriptions { get; } = new Dictionary<WeaponType, string>
        {
            { WeaponType.Sword, "��" },
            { WeaponType.Dagger, "ذ��" },
            { WeaponType.Axe, "��" },
            { WeaponType.Mace, "��" },
            { WeaponType.Staff, "����" },
            { WeaponType.Wand, "ħ��" },
            { WeaponType.Bow, "��" },
            { WeaponType.Crossbow, "��" },
            { WeaponType.Gun, "ǹ" },
            { WeaponType.Shield, "����" },
            { WeaponType.TwoHandSword, "˫�ֽ�" },
            { WeaponType.TwoHandAxe, "˫�ָ�" },
            { WeaponType.TwoHandMace, "˫�ִ�" },
            { WeaponType.Polearm, "��������" },
            { WeaponType.None, "����" }
        };

        /// <summary>
        /// ����DPS����ֵ��1����
        /// </summary>
        public static double BaseWeaponDPS { get; set; } = 5.0;

        /// <summary>
        /// ����DPS�ȼ��ɳ�����
        /// </summary>
        public static double WeaponDPSLevelMultiplier { get; set; } = 1.35;

        /// <summary>
        /// ˫�������˺����ʣ��Է��ض�˫���������ͣ�
        /// </summary>
        public static double TwoHandedDamageMultiplier { get; set; } = 1.5;

        /// <summary>
        /// ���ƻ�����ֵ
        /// </summary>
        public static int BaseShieldBlockChance { get; set; } = 5;

        /// <summary>
        /// ���Ƹ񵲵ȼ��ӳ�
        /// </summary>
        public static double ShieldBlockLevelBonus { get; set; } = 0.5;
        #endregion

        #region ���Եȼ�ϵ��
        /// <summary>
        /// ���Եȼ�ϵ����Χ
        /// </summary>
        public static Dictionary<EquipmentGenerator.AttributeTier, (double min, double max)> AttributeTierMultipliers { get; } = new Dictionary<EquipmentGenerator.AttributeTier, (double min, double max)>
        {
            { EquipmentGenerator.AttributeTier.T1, (0.75, 0.85) }, // T1: 0.8��0.05
            { EquipmentGenerator.AttributeTier.T2, (0.95, 1.05) }, // T2: 1��0.05
            { EquipmentGenerator.AttributeTier.T3, (1.15, 1.25) }  // T3: 1.2��0.05
        };
        #endregion

        #region ������ϵ��
        /// <summary>
        /// �����Ի���ֵϵ��(����ڵȼ�)
        /// </summary>
        public static double SecondaryAttributeBaseValueMultiplier { get; set; } = 0.8;

        /// <summary>
        /// ������Ч��ϵ��
        /// </summary>
        public static Dictionary<string, double> SecondaryAttributeEffectMultipliers { get; } = new Dictionary<string, double>
        {
            // ս������
            { "critical_chance", 0.001 },      // ÿ������0.1%����
            { "critical_damage", 0.01 },       // ÿ������1%�����˺�
            { "attack_power", 2.0 },           // ÿ������2�㹥����
            { "attack_speed", 0.001 },         // ÿ������0.1%�����ٶ�
            { "health", 10.0 },                // ÿ������10������ֵ
            { "accuracy", 2.0 },               // ÿ������2������
            { "dodge", 0.001 },                // ÿ������0.1%����
            
            // ����/�ɼ�����
            { "gathering_speed", 0.005 },      // ÿ������0.5%�ɼ��ٶ�
            { "extra_loot", 0.002 },           // ÿ������0.2%����ս��Ʒ����
            { "crafting_success", 0.005 },     // ÿ������0.5%�����ɹ���
            { "resource_conservation", 0.003 }, // ÿ������0.3%��Դ��Լ��
            
            // Ԫ�ؿ���
            { "fire_resistance", 0.002 },      // ÿ������0.2%���濹��
            { "ice_resistance", 0.002 },       // ÿ������0.2%��˪����
            { "lightning_resistance", 0.002 }, // ÿ������0.2%���翹��
            { "nature_resistance", 0.002 },    // ÿ������0.2%��Ȼ����
            { "shadow_resistance", 0.002 },    // ÿ������0.2%��Ӱ����
            { "holy_resistance", 0.002 }       // ÿ������0.2%��ʥ����
        };
        #endregion

        #region ��Ʒ����ϵ��
        /// <summary>
        /// �����������Է������
        /// </summary>
        public static double NecklaceEqualStatRatio { get; set; } = 0.33; // �������������������

        /// <summary>
        /// ��������ϵ��
        /// </summary>
        public static double NecklaceStaminaRatio { get; set; } = 0.5; // ��������Ϊ�����Ե�50%

        /// <summary>
        /// ��ָ��Ҫ����ϵ��
        /// </summary>
        public static double RingPrimaryStatRatio { get; set; } = 0.6; // ��ָ������ռ60%

        /// <summary>
        /// ��ָ����ϵ��
        /// </summary>
        public static double RingStaminaRatio { get; set; } = 0.3; // ��ָ����Ϊ�����Ե�30%

        /// <summary>
        /// ��Ʒ������ϵ��
        /// </summary>
        public static double TrinketMainStatRatio { get; set; } = 0.7; // ��Ʒ������ռ70%

        /// <summary>
        /// ��Ʒ����ϵ��
        /// </summary>
        public static double TrinketStaminaRatio { get; set; } = 0.2; // ��Ʒ����Ϊ�����Ե�20%

        /// <summary>
        /// ��Ʒ�����ӳɻ���ֵ
        /// </summary>
        public static double TrinketBaseCriticalChance { get; set; } = 0.01;

        /// <summary>
        /// ��Ʒ�����ӳɵȼ�ϵ��
        /// </summary>
        public static double TrinketCriticalChanceLevelBonus { get; set; } = 0.002;

        /// <summary>
        /// ��Ʒ����ӳɻ���ֵ
        /// </summary>
        public static double TrinketBaseExtraLootChance { get; set; } = 0.02;

        /// <summary>
        /// ��Ʒ����ӳɵȼ�ϵ��
        /// </summary>
        public static double TrinketExtraLootChanceLevelBonus { get; set; } = 0.003;
        #endregion

        #region ��������ϵ��
        /// <summary>
        /// ���׻���ֵ��1����
        /// </summary>
        public static double BaseArmorValue { get; set; } = 5.0;

        /// <summary>
        /// ���׵ȼ��ɳ�����
        /// </summary>
        public static double ArmorLevelMultiplier { get; set; } = 1.35;

        /// <summary>
        /// ����Ʒ��ϵ������
        /// </summary>
        public static double ArmorQualityDivisor { get; set; } = 10.0;

        /// <summary>
        /// ��������ϵ��
        /// </summary>
        public static double ArmorStaminaRatio { get; set; } = 0.6;

        /// <summary>
        /// �����������ݷ������
        /// </summary>
        public static double MailArmorAttributeRatio { get; set; } = 0.5; // �����������ݸ�ռ50%

        /// <summary>
        /// ����������ֵ��ת��ϵ��
        /// </summary>
        public static int StaminaToHealthRatio { get; set; } = 10; // ÿ�������ṩ10������ֵ
        #endregion

        #region װ����ֵ����ϵ��
        /// <summary>
        /// װ��������ֵϵ��(����ڵȼ�)
        /// </summary>
        public static int EquipmentBasePricePerLevel { get; set; } = 10;

        /// <summary>
        /// װ��Ʒ�ʼ�ֵ����
        /// </summary>
        public static Dictionary<EquipmentGenerator.EquipmentQuality, int> QualityPriceMultipliers { get; } = new Dictionary<EquipmentGenerator.EquipmentQuality, int>
        {
            { EquipmentGenerator.EquipmentQuality.Common, 1 },
            { EquipmentGenerator.EquipmentQuality.Uncommon, 2 },
            { EquipmentGenerator.EquipmentQuality.Rare, 5 },
            { EquipmentGenerator.EquipmentQuality.Epic, 10 }
        };

        /// <summary>
        /// �����˺���ֵϵ��
        /// </summary>
        public static int WeaponDamagePriceMultiplier { get; set; } = 5;

        /// <summary>
        /// ����ֵ��ֵϵ��
        /// </summary>
        public static int ArmorValuePriceMultiplier { get; set; } = 3;

        /// <summary>
        /// �����Լ�ֵϵ��
        /// </summary>
        public static int MainAttributePriceMultiplier { get; set; } = 2;

        /// <summary>
        /// ����ֵ�ӳɼ�ֵϵ��
        /// </summary>
        public static int HealthBonusPriceDivisor { get; set; } = 5;

        /// <summary>
        /// �������ӳɼ�ֵϵ��
        /// </summary>
        public static int AttackBonusPriceMultiplier { get; set; } = 3;

        /// <summary>
        /// �������ʼ�ֵϵ��
        /// </summary>
        public static int CriticalChancePriceMultiplier { get; set; } = 1000;

        /// <summary>
        /// �����˺���ֵϵ��
        /// </summary>
        public static int CriticalDamagePriceMultiplier { get; set; } = 500;
        #endregion
    }
}