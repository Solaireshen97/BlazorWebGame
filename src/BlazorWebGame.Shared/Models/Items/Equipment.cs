using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Base;

namespace BlazorWebGame.Shared.Models.Items
{
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
        public double AttackSpeed { get; set; } = 0;     // 攻击速度（每秒攻击次数）
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
    }
}