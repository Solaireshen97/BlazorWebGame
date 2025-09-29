using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 物品基类
/// </summary>
public class Item
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ItemType Type { get; private set; } = ItemType.Consumable;
    public ItemRarity Rarity { get; private set; } = ItemRarity.Common;
    public int Value { get; private set; } = 0;
    public bool IsStackable { get; private set; } = true;
    public int MaxStackSize { get; private set; } = 99;
    public Dictionary<string, object> Properties { get; private set; } = new();

    // 私有构造函数，用于反序列化
    protected Item() { }

    public Item(string name, string description, ItemType type, ItemRarity rarity = ItemRarity.Common)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("物品名称不能为空", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        Rarity = rarity;
    }

    /// <summary>
    /// 设置价值
    /// </summary>
    public void SetValue(int value)
    {
        Value = Math.Max(0, value);
    }

    /// <summary>
    /// 设置堆叠属性
    /// </summary>
    public void SetStackable(bool isStackable, int maxStackSize = 99)
    {
        IsStackable = isStackable;
        MaxStackSize = isStackable ? Math.Max(1, maxStackSize) : 1;
    }

    /// <summary>
    /// 设置属性
    /// </summary>
    public void SetProperty(string key, object value)
    {
        Properties[key] = value;
    }

    /// <summary>
    /// 获取属性
    /// </summary>
    public T? GetProperty<T>(string key, T? defaultValue = default)
    {
        if (Properties.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }
}

/// <summary>
/// 装备物品
/// </summary>
public class Equipment : Item
{
    public EquipmentSlot Slot { get; private set; } = EquipmentSlot.MainHand;
    public WeaponType WeaponType { get; private set; } = WeaponType.None;
    public ArmorType ArmorType { get; private set; } = ArmorType.None;
    public int RequiredLevel { get; private set; } = 1;
    public bool IsTwoHanded { get; private set; } = false;
    public List<string> AllowedProfessions { get; private set; } = new();
    
    // 装备属性
    public EquipmentStats Stats { get; private set; } = new();
    
    // 私有构造函数，用于反序列化
    private Equipment() : base() { }

    public Equipment(string name, string description, EquipmentSlot slot, ItemRarity rarity = ItemRarity.Common)
        : base(name, description, ItemType.Equipment, rarity)
    {
        Slot = slot;
        SetStackable(false, 1); // 装备不可堆叠
    }

    /// <summary>
    /// 设置武器类型
    /// </summary>
    public void SetWeaponType(WeaponType weaponType, bool isTwoHanded = false)
    {
        WeaponType = weaponType;
        IsTwoHanded = isTwoHanded;
    }

    /// <summary>
    /// 设置护甲类型
    /// </summary>
    public void SetArmorType(ArmorType armorType)
    {
        ArmorType = armorType;
    }

    /// <summary>
    /// 设置等级需求
    /// </summary>
    public void SetRequiredLevel(int level)
    {
        RequiredLevel = Math.Max(1, level);
    }

    /// <summary>
    /// 设置职业限制
    /// </summary>
    public void SetAllowedProfessions(params string[] professions)
    {
        AllowedProfessions.Clear();
        AllowedProfessions.AddRange(professions.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    /// <summary>
    /// 检查职业是否可以使用
    /// </summary>
    public bool CanUseProfession(string profession)
    {
        return AllowedProfessions.Count == 0 || AllowedProfessions.Contains(profession);
    }
}

/// <summary>
/// 装备属性
/// </summary>
public class EquipmentStats
{
    // 武器属性
    public int WeaponDamage { get; set; } = 0;
    public double AttackSpeed { get; set; } = 1.0;

    // 护甲属性  
    public int ArmorValue { get; set; } = 0;
    public int BlockChance { get; set; } = 0;

    // 主属性加成
    public int StrengthBonus { get; set; } = 0;
    public int AgilityBonus { get; set; } = 0;
    public int IntellectBonus { get; set; } = 0;
    public int SpiritBonus { get; set; } = 0;
    public int StaminaBonus { get; set; } = 0;

    // 战斗属性加成
    public int AttackPowerBonus { get; set; } = 0;
    public int HealthBonus { get; set; } = 0;
    public int ManaBonus { get; set; } = 0;
    public double CriticalChanceBonus { get; set; } = 0.0;
    public double CriticalDamageBonus { get; set; } = 0.0;
    public double AttackSpeedBonus { get; set; } = 0.0;
    public int AccuracyBonus { get; set; } = 0;
    public double DodgeChanceBonus { get; set; } = 0.0;

    // 生产和采集加成
    public double GatheringSpeedBonus { get; set; } = 0.0;
    public double ExtraLootChanceBonus { get; set; } = 0.0;
    public double CraftingSuccessBonus { get; set; } = 0.0;
    public double ResourceConservationBonus { get; set; } = 0.0;

    // 元素抗性
    public Dictionary<string, double> ElementalResistances { get; set; } = new();

    /// <summary>
    /// 设置元素抗性
    /// </summary>
    public void SetElementalResistance(string element, double resistance)
    {
        ElementalResistances[element] = Math.Clamp(resistance, 0.0, 1.0);
    }

    /// <summary>
    /// 获取元素抗性
    /// </summary>
    public double GetElementalResistance(string element)
    {
        return ElementalResistances.GetValueOrDefault(element, 0.0);
    }

    /// <summary>
    /// 克隆属性
    /// </summary>
    public EquipmentStats Clone()
    {
        return new EquipmentStats
        {
            WeaponDamage = WeaponDamage,
            AttackSpeed = AttackSpeed,
            ArmorValue = ArmorValue,
            BlockChance = BlockChance,
            StrengthBonus = StrengthBonus,
            AgilityBonus = AgilityBonus,
            IntellectBonus = IntellectBonus,
            SpiritBonus = SpiritBonus,
            StaminaBonus = StaminaBonus,
            AttackPowerBonus = AttackPowerBonus,
            HealthBonus = HealthBonus,
            ManaBonus = ManaBonus,
            CriticalChanceBonus = CriticalChanceBonus,
            CriticalDamageBonus = CriticalDamageBonus,
            AttackSpeedBonus = AttackSpeedBonus,
            AccuracyBonus = AccuracyBonus,
            DodgeChanceBonus = DodgeChanceBonus,
            GatheringSpeedBonus = GatheringSpeedBonus,
            ExtraLootChanceBonus = ExtraLootChanceBonus,
            CraftingSuccessBonus = CraftingSuccessBonus,
            ResourceConservationBonus = ResourceConservationBonus,
            ElementalResistances = new Dictionary<string, double>(ElementalResistances)
        };
    }

    /// <summary>
    /// 合并属性
    /// </summary>
    public void Add(EquipmentStats other)
    {
        WeaponDamage += other.WeaponDamage;
        AttackSpeed += other.AttackSpeed;
        ArmorValue += other.ArmorValue;
        BlockChance += other.BlockChance;
        StrengthBonus += other.StrengthBonus;
        AgilityBonus += other.AgilityBonus;
        IntellectBonus += other.IntellectBonus;
        SpiritBonus += other.SpiritBonus;
        StaminaBonus += other.StaminaBonus;
        AttackPowerBonus += other.AttackPowerBonus;
        HealthBonus += other.HealthBonus;
        ManaBonus += other.ManaBonus;
        CriticalChanceBonus += other.CriticalChanceBonus;
        CriticalDamageBonus += other.CriticalDamageBonus;
        AttackSpeedBonus += other.AttackSpeedBonus;
        AccuracyBonus += other.AccuracyBonus;
        DodgeChanceBonus += other.DodgeChanceBonus;
        GatheringSpeedBonus += other.GatheringSpeedBonus;
        ExtraLootChanceBonus += other.ExtraLootChanceBonus;
        CraftingSuccessBonus += other.CraftingSuccessBonus;
        ResourceConservationBonus += other.ResourceConservationBonus;

        foreach (var resistance in other.ElementalResistances)
        {
            var currentValue = GetElementalResistance(resistance.Key);
            ElementalResistances[resistance.Key] = Math.Clamp(currentValue + resistance.Value, 0.0, 1.0);
        }
    }
}

/// <summary>
/// 消耗品
/// </summary>
public class Consumable : Item
{
    public ConsumableType ConsumableType { get; private set; } = ConsumableType.Potion;
    public ConsumableEffect Effect { get; private set; } = new();
    public TimeSpan Cooldown { get; private set; } = TimeSpan.Zero;
    public int RequiredLevel { get; private set; } = 1;

    // 私有构造函数，用于反序列化
    private Consumable() : base() { }

    public Consumable(string name, string description, ConsumableType consumableType, ItemRarity rarity = ItemRarity.Common)
        : base(name, description, ItemType.Consumable, rarity)
    {
        ConsumableType = consumableType;
    }

    /// <summary>
    /// 设置效果
    /// </summary>
    public void SetEffect(string effectType, int value, TimeSpan? duration = null)
    {
        Effect = new ConsumableEffect
        {
            EffectType = effectType,
            Value = value,
            Duration = duration ?? TimeSpan.Zero,
            IsInstant = duration == null || duration == TimeSpan.Zero
        };
    }

    /// <summary>
    /// 设置冷却时间
    /// </summary>
    public void SetCooldown(TimeSpan cooldown)
    {
        Cooldown = cooldown;
    }

    /// <summary>
    /// 设置等级需求
    /// </summary>
    public void SetRequiredLevel(int level)
    {
        RequiredLevel = Math.Max(1, level);
    }
}

/// <summary>
/// 消耗品效果
/// </summary>
public class ConsumableEffect
{
    public string EffectType { get; set; } = string.Empty; // Health, Mana, Buff, etc.
    public int Value { get; set; } = 0;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public bool IsInstant { get; set; } = true;
    public Dictionary<string, object> AdditionalEffects { get; set; } = new();

    /// <summary>
    /// 添加附加效果
    /// </summary>
    public void AddAdditionalEffect(string key, object value)
    {
        AdditionalEffects[key] = value;
    }
}

/// <summary>
/// 材料物品
/// </summary>
public class Material : Item
{
    public MaterialType MaterialType { get; private set; } = MaterialType.Ore;
    public int Tier { get; private set; } = 1;

    // 私有构造函数，用于反序列化
    private Material() : base() { }

    public Material(string name, string description, MaterialType materialType, int tier = 1, ItemRarity rarity = ItemRarity.Common)
        : base(name, description, ItemType.Material, rarity)
    {
        MaterialType = materialType;
        Tier = Math.Max(1, tier);
    }

    /// <summary>
    /// 设置等级
    /// </summary>
    public void SetTier(int tier)
    {
        Tier = Math.Max(1, tier);
    }
}

/// <summary>
/// 任务物品
/// </summary>
public class QuestItem : Item
{
    public string QuestId { get; private set; } = string.Empty;
    public bool IsKeyItem { get; private set; } = false;

    // 私有构造函数，用于反序列化
    private QuestItem() : base() { }

    public QuestItem(string name, string description, string questId, bool isKeyItem = false)
        : base(name, description, ItemType.QuestItem, ItemRarity.Common)
    {
        QuestId = questId;
        IsKeyItem = isKeyItem;
        SetStackable(!isKeyItem, isKeyItem ? 1 : 99); // 关键物品不可堆叠
    }
}

/// <summary>
/// 物品类型枚举
/// </summary>
public enum ItemType
{
    Equipment,      // 装备
    Consumable,     // 消耗品
    Material,       // 材料
    QuestItem,      // 任务物品
    Currency,       // 货币
    Misc           // 杂项
}

/// <summary>
/// 物品稀有度枚举
/// </summary>
public enum ItemRarity
{
    Common,         // 普通 - 白色
    Uncommon,       // 不常见 - 绿色
    Rare,          // 稀有 - 蓝色
    Epic,          // 史诗 - 紫色
    Legendary,     // 传说 - 橙色
    Mythic         // 神话 - 红色
}

/// <summary>
/// 装备槽位枚举
/// </summary>
public enum EquipmentSlot
{
    MainHand,      // 主手
    OffHand,       // 副手
    Head,          // 头部
    Chest,         // 胸部
    Legs,          // 腿部
    Feet,          // 脚部
    Hands,         // 手部
    Shoulders,     // 肩膀
    Belt,          // 腰带
    Neck,          // 颈部
    Ring1,         // 戒指1
    Ring2,         // 戒指2
    Trinket1,      // 饰品1
    Trinket2       // 饰品2
}

/// <summary>
/// 武器类型枚举
/// </summary>
public enum WeaponType
{
    None,          // 无
    Sword,         // 剑
    Axe,           // 斧
    Mace,          // 锤
    Dagger,        // 匕首
    Staff,         // 法杖
    Bow,           // 弓
    TwoHandSword,  // 双手剑
    TwoHandAxe,    // 双手斧
    TwoHandMace,   // 双手锤
    Polearm,       // 长柄武器
    Shield         // 盾牌
}

/// <summary>
/// 护甲类型枚举
/// </summary>
public enum ArmorType
{
    None,          // 无
    Cloth,         // 布甲
    Leather,       // 皮甲
    Mail,          // 锁甲
    Plate          // 板甲
}

/// <summary>
/// 消耗品类型枚举
/// </summary>
public enum ConsumableType
{
    Potion,        // 药水
    Food,          // 食物
    Scroll,        // 卷轴
    Elixir,        // 药剂
    Buff,          // 增益
    Special        // 特殊
}

/// <summary>
/// 材料类型枚举
/// </summary>
public enum MaterialType
{
    Ore,           // 矿石
    Herb,          // 草药
    Fish,          // 鱼类
    Gem,           // 宝石
    Cloth,         // 布料
    Leather,       // 皮革
    Wood,          // 木材
    Essence,       // 精华
    Component      // 组件
}