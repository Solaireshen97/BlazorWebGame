using BlazorWebGame.Models;
using BlazorWebGame.Models.Monsters;
using System.Collections.Generic;
using System.Text;

namespace BlazorWebGame.Models
{
    /// <summary>
    /// 定义所有可用的装备槽位，灵感来源于经典MMORPG
    /// </summary>
    public enum EquipmentSlot
    {
        // --- 核心护甲 (左侧) ---
        Head,     // 头部
        Neck,     // 颈部
        Shoulder, // 肩部
        Back,     // 背部 (披风)
        Chest,    // 胸部
        Wrist,    // 手腕 (护腕)

        // --- 核心护甲 (右侧) ---
        Hands,    // 手部 (手套)
        Waist,    // 腰部 (腰带)
        Legs,     // 腿部
        Feet,     // 脚部

        // --- 饰品和戒指 (右侧) ---
        Finger1,  // 第一个戒指
        Finger2,  // 第二个戒指
        Trinket1, // 第一个饰品
        Trinket2, // 第二个饰品

        // --- 武器 (底部) ---
        MainHand, // 主手武器
        OffHand   // 副手 (可以是盾牌或副手武器)
    }

    /// <summary>
    /// 护甲类型，决定了装备的基本属性和可装备的职业
    /// </summary>
    public enum ArmorType
    {
        None,    // 无类型（如饰品）
        Cloth,   // 布甲（法师等）
        Leather, // 皮甲（猎人等）
        Mail,    // 锁甲（萨满等）
        Plate    // 板甲（战士等）
    }

    /// <summary>
    /// 武器类型，决定了武器的基本属性和攻击方式
    /// </summary>
    public enum WeaponType
    {
        None,         // 无类型（非武器）
        Sword,        // 剑
        Dagger,       // 匕首
        Axe,          // 斧
        Mace,         // 锤
        Staff,        // 法杖
        Wand,         // 魔杖
        Bow,          // 弓
        Crossbow,     // 弩
        Gun,          // 枪
        Shield,       // 盾牌
        TwoHandSword, // 双手剑
        TwoHandAxe,   // 双手斧
        TwoHandMace,  // 双手锤
        Polearm       // 长柄武器
    }

    public class Equipment : Item
    {
        // 基本装备信息
        public EquipmentSlot Slot { get; set; }
        public ArmorType ArmorType { get; set; } = ArmorType.None;
        public WeaponType WeaponType { get; set; } = WeaponType.None;

        // 装备等级要求
        public int RequiredLevel { get; set; } = 1;

        // 职业限制
        public List<BattleProfession> AllowedProfessions { get; set; } = new List<BattleProfession>();

        // 核心战斗属性
        // - 对武器
        public int WeaponDamage { get; set; } = 0;       // 武器伤害
        public double AttackSpeed { get; set; } = 0;     // 攻击速度（每秒）
        public bool IsTwoHanded { get; set; } = false;   // 是否为双手武器

        // - 对防具
        public int ArmorValue { get; set; } = 0;         // 护甲值
        public int BlockChance { get; set; } = 0;        // 格挡几率（盾牌）

        // 战斗属性加成
        public int AttackBonus { get; set; } = 0;        // 攻击力加成
        public int HealthBonus { get; set; } = 0;        // 生命值加成
        public double AttackSpeedBonus { get; set; } = 0; // 攻击速度加成
        public double CriticalChanceBonus { get; set; } = 0; // 暴击率加成
        public double CriticalDamageBonus { get; set; } = 0; // 暴击伤害加成
        public int AccuracyBonus { get; set; } = 0;      // 命中加成
        public double DodgeChanceBonus { get; set; } = 0; // 闪避几率加成

        // 生产/采集属性加成
        public double GatheringSpeedBonus { get; set; } = 0; // 采集速度加成
        public double ExtraLootChanceBonus { get; set; } = 0; // 额外战利品几率
        public double CraftingSuccessBonus { get; set; } = 0; // 制作成功率加成
        public double ResourceConservationBonus { get; set; } = 0; // 资源节约率

        // 属性加成
        public AttributeSet AttributeBonuses { get; set; } = new AttributeSet();

        // 元素抗性
        public Dictionary<ElementType, double> ElementalResistances { get; set; } = new Dictionary<ElementType, double>();

        public Equipment()
        {
            Type = ItemType.Equipment;
            IsStackable = false;
        }

        // 获取装备属性描述
        public override string GetStatsDescription()
        {
            var sb = new StringBuilder();

            // 添加装备类型和要求信息
            if (ArmorType != ArmorType.None)
                sb.AppendLine($"{GetArmorTypeName(ArmorType)}");

            if (WeaponType != WeaponType.None)
                sb.AppendLine($"{GetWeaponTypeName(WeaponType)}{(IsTwoHanded ? " (双手)" : "")}");

            if (RequiredLevel > 1)
                sb.AppendLine($"需要等级: {RequiredLevel}");

            if (AllowedProfessions.Count > 0)
            {
                sb.Append("职业限制: ");
                sb.AppendLine(string.Join(", ", AllowedProfessions.Select(p => GetProfessionName(p))));
            }

            sb.AppendLine();

            // 添加核心战斗属性
            if (WeaponDamage > 0)
                sb.AppendLine($"{WeaponDamage} 伤害");

            if (AttackSpeed > 0)
                sb.AppendLine($"攻击速度: {AttackSpeed:F2}");

            if (ArmorValue > 0)
                sb.AppendLine($"{ArmorValue} 护甲");

            if (BlockChance > 0)
                sb.AppendLine($"{BlockChance}% 格挡几率");

            // 添加战斗属性加成
            if (AttackBonus > 0)
                sb.AppendLine($"+{AttackBonus} 攻击力");

            if (HealthBonus > 0)
                sb.AppendLine($"+{HealthBonus} 生命值");

            if (AttackSpeedBonus > 0)
                sb.AppendLine($"+{AttackSpeedBonus:P0} 攻击速度");

            if (CriticalChanceBonus > 0)
                sb.AppendLine($"+{CriticalChanceBonus:P0} 暴击率");

            if (CriticalDamageBonus > 0)
                sb.AppendLine($"+{CriticalDamageBonus:P0} 暴击伤害");

            if (AccuracyBonus > 0)
                sb.AppendLine($"+{AccuracyBonus} 命中");

            if (DodgeChanceBonus > 0)
                sb.AppendLine($"+{DodgeChanceBonus:P0} 闪避几率");

            // 添加生产/采集属性加成
            bool hasProductionBonus = false;
            if (GatheringSpeedBonus > 0 || ExtraLootChanceBonus > 0 ||
                CraftingSuccessBonus > 0 || ResourceConservationBonus > 0)
            {
                sb.AppendLine();
                hasProductionBonus = true;
            }

            if (GatheringSpeedBonus > 0)
                sb.AppendLine($"+{GatheringSpeedBonus:P0} 采集速度");

            if (ExtraLootChanceBonus > 0)
                sb.AppendLine($"+{ExtraLootChanceBonus:P0} 额外战利品几率");

            if (CraftingSuccessBonus > 0)
                sb.AppendLine($"+{CraftingSuccessBonus:P0} 制作成功率");

            if (ResourceConservationBonus > 0)
                sb.AppendLine($"+{ResourceConservationBonus:P0} 资源节约");

            // 添加属性加成描述
            bool hasAttributeBonus = false;
            if (AttributeBonuses.Strength > 0 || AttributeBonuses.Agility > 0 ||
                AttributeBonuses.Intellect > 0 || AttributeBonuses.Spirit > 0 ||
                AttributeBonuses.Stamina > 0)
            {
                if (!hasProductionBonus)
                    sb.AppendLine();
                hasAttributeBonus = true;
            }

            if (AttributeBonuses.Strength > 0)
                sb.AppendLine($"+{AttributeBonuses.Strength} 力量");

            if (AttributeBonuses.Agility > 0)
                sb.AppendLine($"+{AttributeBonuses.Agility} 敏捷");

            if (AttributeBonuses.Intellect > 0)
                sb.AppendLine($"+{AttributeBonuses.Intellect} 智力");

            if (AttributeBonuses.Spirit > 0)
                sb.AppendLine($"+{AttributeBonuses.Spirit} 精神");

            if (AttributeBonuses.Stamina > 0)
                sb.AppendLine($"+{AttributeBonuses.Stamina} 耐力");

            // 添加元素抗性
            if (ElementalResistances.Count > 0)
            {
                if (!hasAttributeBonus && !hasProductionBonus)
                    sb.AppendLine();

                foreach (var resistance in ElementalResistances)
                {
                    sb.AppendLine($"+{resistance.Value:P0} {GetElementTypeName(resistance.Key)}抗性");
                }
            }

            return sb.ToString();
        }

        // 辅助方法：获取护甲类型名称
        private string GetArmorTypeName(ArmorType type)
        {
            return type switch
            {
                ArmorType.Cloth => "布甲",
                ArmorType.Leather => "皮甲",
                ArmorType.Mail => "锁甲",
                ArmorType.Plate => "板甲",
                _ => "其他"
            };
        }

        // 辅助方法：获取武器类型名称
        private string GetWeaponTypeName(WeaponType type)
        {
            return type switch
            {
                WeaponType.Sword => "剑",
                WeaponType.Dagger => "匕首",
                WeaponType.Axe => "斧",
                WeaponType.Mace => "锤",
                WeaponType.Staff => "法杖",
                WeaponType.Wand => "魔杖",
                WeaponType.Bow => "弓",
                WeaponType.Crossbow => "弩",
                WeaponType.Gun => "枪",
                WeaponType.Shield => "盾牌",
                WeaponType.TwoHandSword => "双手剑",
                WeaponType.TwoHandAxe => "双手斧",
                WeaponType.TwoHandMace => "双手锤",
                WeaponType.Polearm => "长柄武器",
                _ => "其他"
            };
        }

        // 辅助方法：获取职业名称
        private string GetProfessionName(BattleProfession profession)
        {
            return profession switch
            {
                BattleProfession.Warrior => "战士",
                BattleProfession.Mage => "法师",
                _ => profession.ToString()
            };
        }

        // 辅助方法：获取元素类型名称
        private string GetElementTypeName(ElementType elementType)
        {
            return elementType switch
            {
                ElementType.Fire => "火焰",
                ElementType.Ice => "冰霜",
                ElementType.Lightning => "闪电",
                ElementType.Nature => "自然",
                ElementType.Shadow => "暗影",
                ElementType.Holy => "神圣",
                _ => elementType.ToString()
            };
        }
    }
}