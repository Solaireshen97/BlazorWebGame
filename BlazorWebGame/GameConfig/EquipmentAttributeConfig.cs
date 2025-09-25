using System;
using System.Collections.Generic;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Services.Equipments;

namespace BlazorWebGame.GameConfig
{
    /// <summary>
    /// 装备属性系统的配置参数
    /// </summary>
    public static class EquipmentAttributeConfig
    {
        #region 装备品质系数
        /// <summary>
        /// 装备品质主属性倍率
        /// </summary>
        public static Dictionary<EquipmentGenerator.EquipmentQuality, double> QualityMainAttributeMultipliers { get; } = new Dictionary<EquipmentGenerator.EquipmentQuality, double>
        {
            { EquipmentGenerator.EquipmentQuality.Common, 10.0 },   // 白装: 10 * 等级
            { EquipmentGenerator.EquipmentQuality.Uncommon, 15.0 }, // 绿装: 15 * 等级
            { EquipmentGenerator.EquipmentQuality.Rare, 20.0 },     // 蓝装: 20 * 等级
            { EquipmentGenerator.EquipmentQuality.Epic, 30.0 }      // 紫装: 30 * 等级
        };

        /// <summary>
        /// 武器品质属性倍率
        /// </summary>
        public static Dictionary<EquipmentGenerator.EquipmentQuality, double> WeaponMainAttributeMultipliers { get; } = new Dictionary<EquipmentGenerator.EquipmentQuality, double>
        {
            { EquipmentGenerator.EquipmentQuality.Common, 1.0 },    // 白装: 1 * 等级
            { EquipmentGenerator.EquipmentQuality.Uncommon, 1.25 }, // 绿装: 1.25 * 等级
            { EquipmentGenerator.EquipmentQuality.Rare, 1.5 },      // 蓝装: 1.5 * 等级
            { EquipmentGenerator.EquipmentQuality.Epic, 2.0 }       // 紫装: 2 * 等级
        };

        /// <summary>
        /// 不同品质装备的副属性数量
        /// </summary>
        public static Dictionary<EquipmentGenerator.EquipmentQuality, int> QualitySecondaryAttributeCount { get; } = new Dictionary<EquipmentGenerator.EquipmentQuality, int>
        {
            { EquipmentGenerator.EquipmentQuality.Common, 0 },    // 白装: 0个副属性
            { EquipmentGenerator.EquipmentQuality.Uncommon, 1 },  // 绿装: 1个副属性
            { EquipmentGenerator.EquipmentQuality.Rare, 2 },      // 蓝装: 2个副属性
            { EquipmentGenerator.EquipmentQuality.Epic, 3 }       // 紫装: 3个副属性
        };

        /// <summary>
        /// 品质颜色前缀
        /// </summary>
        public static Dictionary<EquipmentGenerator.EquipmentQuality, string> QualityPrefixes { get; } = new Dictionary<EquipmentGenerator.EquipmentQuality, string>
        {
            { EquipmentGenerator.EquipmentQuality.Common, "" },          // 白色，不需要前缀
            { EquipmentGenerator.EquipmentQuality.Uncommon, "[优质] " },
            { EquipmentGenerator.EquipmentQuality.Rare, "[稀有] " },
            { EquipmentGenerator.EquipmentQuality.Epic, "[史诗] " }
        };

        /// <summary>
        /// 品质描述文字
        /// </summary>
        public static Dictionary<EquipmentGenerator.EquipmentQuality, string> QualityDescriptions { get; } = new Dictionary<EquipmentGenerator.EquipmentQuality, string>
        {
            { EquipmentGenerator.EquipmentQuality.Common, "普通的" },
            { EquipmentGenerator.EquipmentQuality.Uncommon, "优质的" },
            { EquipmentGenerator.EquipmentQuality.Rare, "稀有的" },
            { EquipmentGenerator.EquipmentQuality.Epic, "史诗级的" }
        };
        #endregion

        #region 装备部位属性分配
        /// <summary>
        /// 装备部位主属性分配比例
        /// </summary>
        public static Dictionary<EquipmentSlot, double> SlotMainAttributeRatios { get; } = new Dictionary<EquipmentSlot, double>
        {
            { EquipmentSlot.Head, 0.15 },      // 头部: 15%
            { EquipmentSlot.Neck, 0.08 },      // 颈部: 8%
            { EquipmentSlot.Shoulder, 0.12 },  // 肩部: 12%
            { EquipmentSlot.Back, 0.10 },      // 背部: 10%
            { EquipmentSlot.Chest, 0.16 },     // 胸部: 16%
            { EquipmentSlot.Wrist, 0.06 },     // 手腕: 6%
            { EquipmentSlot.Hands, 0.08 },     // 手部: 8%
            { EquipmentSlot.Waist, 0.08 },     // 腰部: 8%
            { EquipmentSlot.Legs, 0.15 },      // 腿部: 15%
            { EquipmentSlot.Feet, 0.10 },      // 脚部: 10%
            { EquipmentSlot.Finger1, 0.05 },   // 戒指1: 5%
            { EquipmentSlot.Finger2, 0.05 },   // 戒指2: 5%
            { EquipmentSlot.Trinket1, 0.08 },  // 饰品1: 8%
            { EquipmentSlot.Trinket2, 0.08 },  // 饰品2: 8%
            { EquipmentSlot.MainHand, 1.0 },   // 主手: 特殊计算
            { EquipmentSlot.OffHand, 0.5 }     // 副手: 特殊计算
        };

        /// <summary>
        /// 护甲槽位修饰符
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
        /// 装备槽位描述
        /// </summary>
        public static Dictionary<EquipmentSlot, string> SlotDescriptions { get; } = new Dictionary<EquipmentSlot, string>
        {
            { EquipmentSlot.Head, "头盔" },
            { EquipmentSlot.Chest, "胸甲" },
            { EquipmentSlot.Legs, "腿甲" },
            { EquipmentSlot.Shoulder, "肩甲" },
            { EquipmentSlot.Hands, "手套" },
            { EquipmentSlot.Feet, "靴子" },
            { EquipmentSlot.Wrist, "护腕" },
            { EquipmentSlot.Waist, "腰带" },
            { EquipmentSlot.Back, "披风" },
            { EquipmentSlot.Neck, "项链" },
            { EquipmentSlot.Finger1, "戒指" },
            { EquipmentSlot.Finger2, "戒指" },
            { EquipmentSlot.Trinket1, "饰品" },
            { EquipmentSlot.Trinket2, "饰品" }
        };
        #endregion

        #region 护甲类型系数
        /// <summary>
        /// 护甲类型基础值修饰符
        /// </summary>
        public static Dictionary<ArmorType, double> ArmorTypeModifiers { get; } = new Dictionary<ArmorType, double>
        {
            { ArmorType.Cloth, 0.8 },    // 布甲: 80%基础护甲
            { ArmorType.Leather, 1.0 },  // 皮甲: 100%基础护甲
            { ArmorType.Mail, 1.3 },     // 锁甲: 130%基础护甲
            { ArmorType.Plate, 1.6 }     // 板甲: 160%基础护甲
        };
        
        /// <summary>
        /// 护甲类型描述
        /// </summary>
        public static Dictionary<ArmorType, string> ArmorTypeDescriptions { get; } = new Dictionary<ArmorType, string>
        {
            { ArmorType.Cloth, "布质" },
            { ArmorType.Leather, "皮质" },
            { ArmorType.Mail, "锁链" },
            { ArmorType.Plate, "板甲" }
        };
        #endregion

        #region 武器系数配置
        /// <summary>
        /// 武器类型修饰符 - [基础伤害系数, 攻击速度]
        /// </summary>
        public static Dictionary<WeaponType, (double damage, double speed)> WeaponTypeModifiers { get; } = new Dictionary<WeaponType, (double damage, double speed)>
        {
            { WeaponType.Sword, (4.0, 0.425) },         // 剑: 标准伤害, 较快攻速
            { WeaponType.Dagger, (2.8, 0.55) },         // 匕首: 较低伤害, 最快攻速
            { WeaponType.Axe, (4.8, 0.375) },           // 斧: 高伤害, 较慢攻速
            { WeaponType.Mace, (5.2, 0.35) },           // 锤: 很高伤害, 慢攻速
            { WeaponType.Staff, (4.0, 0.4) },           // 法杖: 标准伤害, 标准攻速
            { WeaponType.Wand, (3.2, 0.475) },          // 魔杖: 低伤害, 快攻速
            { WeaponType.Bow, (3.6, 0.45) },            // 弓: 较低伤害, 较快攻速
            { WeaponType.Crossbow, (4.8, 0.35) },       // 弩: 高伤害, 慢攻速
            { WeaponType.Gun, (4.4, 0.375) },           // 枪: 较高伤害, 较慢攻速
            { WeaponType.Shield, (2.0, 0.3) },          // 盾牌: 最低伤害, 最慢攻速
            { WeaponType.TwoHandSword, (7.2, 0.375) },  // 双手剑: 极高伤害, 较慢攻速
            { WeaponType.TwoHandAxe, (8.0, 0.325) },    // 双手斧: 最高伤害, 非常慢攻速
            { WeaponType.TwoHandMace, (7.6, 0.3) },     // 双手锤: 极高伤害, 最慢攻速
            { WeaponType.Polearm, (6.8, 0.4) }          // 长柄武器: 很高伤害, 标准攻速
        };

        /// <summary>
        /// 武器类型描述
        /// </summary>
        public static Dictionary<WeaponType, string> WeaponTypeDescriptions { get; } = new Dictionary<WeaponType, string>
        {
            { WeaponType.Sword, "剑" },
            { WeaponType.Dagger, "匕首" },
            { WeaponType.Axe, "斧" },
            { WeaponType.Mace, "锤" },
            { WeaponType.Staff, "法杖" },
            { WeaponType.Wand, "魔杖" },
            { WeaponType.Bow, "弓" },
            { WeaponType.Crossbow, "弩" },
            { WeaponType.Gun, "枪" },
            { WeaponType.Shield, "盾牌" },
            { WeaponType.TwoHandSword, "双手剑" },
            { WeaponType.TwoHandAxe, "双手斧" },
            { WeaponType.TwoHandMace, "双手锤" },
            { WeaponType.Polearm, "长柄武器" },
            { WeaponType.None, "武器" }
        };

        /// <summary>
        /// 武器DPS基础值（1级）
        /// </summary>
        public static double BaseWeaponDPS { get; set; } = 5.0;

        /// <summary>
        /// 武器DPS等级成长倍率
        /// </summary>
        public static double WeaponDPSLevelMultiplier { get; set; } = 1.35;

        /// <summary>
        /// 双手武器伤害倍率（对非特定双手武器类型）
        /// </summary>
        public static double TwoHandedDamageMultiplier { get; set; } = 1.5;

        /// <summary>
        /// 盾牌基础格挡值
        /// </summary>
        public static int BaseShieldBlockChance { get; set; } = 5;

        /// <summary>
        /// 盾牌格挡等级加成
        /// </summary>
        public static double ShieldBlockLevelBonus { get; set; } = 0.5;
        #endregion

        #region 属性等级系数
        /// <summary>
        /// 属性等级系数范围
        /// </summary>
        public static Dictionary<EquipmentGenerator.AttributeTier, (double min, double max)> AttributeTierMultipliers { get; } = new Dictionary<EquipmentGenerator.AttributeTier, (double min, double max)>
        {
            { EquipmentGenerator.AttributeTier.T1, (0.75, 0.85) }, // T1: 0.8±0.05
            { EquipmentGenerator.AttributeTier.T2, (0.95, 1.05) }, // T2: 1±0.05
            { EquipmentGenerator.AttributeTier.T3, (1.15, 1.25) }  // T3: 1.2±0.05
        };
        #endregion

        #region 副属性系数
        /// <summary>
        /// 副属性基础值系数(相对于等级)
        /// </summary>
        public static double SecondaryAttributeBaseValueMultiplier { get; set; } = 0.8;

        /// <summary>
        /// 副属性效果系数
        /// </summary>
        public static Dictionary<string, double> SecondaryAttributeEffectMultipliers { get; } = new Dictionary<string, double>
        {
            // 战斗属性
            { "critical_chance", 0.001 },      // 每点增加0.1%暴击
            { "critical_damage", 0.01 },       // 每点增加1%暴击伤害
            { "attack_power", 2.0 },           // 每点增加2点攻击力
            { "attack_speed", 0.001 },         // 每点增加0.1%攻击速度
            { "health", 10.0 },                // 每点增加10点生命值
            { "accuracy", 2.0 },               // 每点增加2点命中
            { "dodge", 0.001 },                // 每点增加0.1%闪避
            
            // 生产/采集属性
            { "gathering_speed", 0.005 },      // 每点增加0.5%采集速度
            { "extra_loot", 0.002 },           // 每点增加0.2%额外战利品几率
            { "crafting_success", 0.005 },     // 每点增加0.5%制作成功率
            { "resource_conservation", 0.003 }, // 每点增加0.3%资源节约率
            
            // 元素抗性
            { "fire_resistance", 0.002 },      // 每点增加0.2%火焰抗性
            { "ice_resistance", 0.002 },       // 每点增加0.2%冰霜抗性
            { "lightning_resistance", 0.002 }, // 每点增加0.2%闪电抗性
            { "nature_resistance", 0.002 },    // 每点增加0.2%自然抗性
            { "shadow_resistance", 0.002 },    // 每点增加0.2%暗影抗性
            { "holy_resistance", 0.002 }       // 每点增加0.2%神圣抗性
        };
        #endregion

        #region 饰品特殊系数
        /// <summary>
        /// 项链各主属性分配比例
        /// </summary>
        public static double NecklaceEqualStatRatio { get; set; } = 0.33; // 项链均衡分配三种属性

        /// <summary>
        /// 项链耐力系数
        /// </summary>
        public static double NecklaceStaminaRatio { get; set; } = 0.5; // 项链耐力为主属性的50%

        /// <summary>
        /// 戒指主要属性系数
        /// </summary>
        public static double RingPrimaryStatRatio { get; set; } = 0.6; // 戒指主属性占60%

        /// <summary>
        /// 戒指耐力系数
        /// </summary>
        public static double RingStaminaRatio { get; set; } = 0.3; // 戒指耐力为总属性的30%

        /// <summary>
        /// 饰品主属性系数
        /// </summary>
        public static double TrinketMainStatRatio { get; set; } = 0.7; // 饰品主属性占70%

        /// <summary>
        /// 饰品耐力系数
        /// </summary>
        public static double TrinketStaminaRatio { get; set; } = 0.2; // 饰品耐力为总属性的20%

        /// <summary>
        /// 饰品暴击加成基础值
        /// </summary>
        public static double TrinketBaseCriticalChance { get; set; } = 0.01;

        /// <summary>
        /// 饰品暴击加成等级系数
        /// </summary>
        public static double TrinketCriticalChanceLevelBonus { get; set; } = 0.002;

        /// <summary>
        /// 饰品掉落加成基础值
        /// </summary>
        public static double TrinketBaseExtraLootChance { get; set; } = 0.02;

        /// <summary>
        /// 饰品掉落加成等级系数
        /// </summary>
        public static double TrinketExtraLootChanceLevelBonus { get; set; } = 0.003;
        #endregion

        #region 护甲特殊系数
        /// <summary>
        /// 护甲基础值（1级）
        /// </summary>
        public static double BaseArmorValue { get; set; } = 5.0;

        /// <summary>
        /// 护甲等级成长倍率
        /// </summary>
        public static double ArmorLevelMultiplier { get; set; } = 1.35;

        /// <summary>
        /// 护甲品质系数除数
        /// </summary>
        public static double ArmorQualityDivisor { get; set; } = 10.0;

        /// <summary>
        /// 护甲耐力系数
        /// </summary>
        public static double ArmorStaminaRatio { get; set; } = 0.6;

        /// <summary>
        /// 锁甲力量敏捷分配比例
        /// </summary>
        public static double MailArmorAttributeRatio { get; set; } = 0.5; // 锁甲力量敏捷各占50%

        /// <summary>
        /// 耐力到生命值的转换系数
        /// </summary>
        public static int StaminaToHealthRatio { get; set; } = 10; // 每点耐力提供10点生命值
        #endregion

        #region 装备价值计算系数
        /// <summary>
        /// 装备基础价值系数(相对于等级)
        /// </summary>
        public static int EquipmentBasePricePerLevel { get; set; } = 10;

        /// <summary>
        /// 装备品质价值倍率
        /// </summary>
        public static Dictionary<EquipmentGenerator.EquipmentQuality, int> QualityPriceMultipliers { get; } = new Dictionary<EquipmentGenerator.EquipmentQuality, int>
        {
            { EquipmentGenerator.EquipmentQuality.Common, 1 },
            { EquipmentGenerator.EquipmentQuality.Uncommon, 2 },
            { EquipmentGenerator.EquipmentQuality.Rare, 5 },
            { EquipmentGenerator.EquipmentQuality.Epic, 10 }
        };

        /// <summary>
        /// 武器伤害价值系数
        /// </summary>
        public static int WeaponDamagePriceMultiplier { get; set; } = 5;

        /// <summary>
        /// 护甲值价值系数
        /// </summary>
        public static int ArmorValuePriceMultiplier { get; set; } = 3;

        /// <summary>
        /// 主属性价值系数
        /// </summary>
        public static int MainAttributePriceMultiplier { get; set; } = 2;

        /// <summary>
        /// 生命值加成价值系数
        /// </summary>
        public static int HealthBonusPriceDivisor { get; set; } = 5;

        /// <summary>
        /// 攻击力加成价值系数
        /// </summary>
        public static int AttackBonusPriceMultiplier { get; set; } = 3;

        /// <summary>
        /// 暴击几率价值系数
        /// </summary>
        public static int CriticalChancePriceMultiplier { get; set; } = 1000;

        /// <summary>
        /// 暴击伤害价值系数
        /// </summary>
        public static int CriticalDamagePriceMultiplier { get; set; } = 500;
        #endregion
    }
}