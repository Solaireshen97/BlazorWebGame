using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Models; // For EquipmentSlot
using System;
using System.Collections.Generic;
using System.Linq;
using DTOWeaponType = BlazorWebGame.Shared.DTOs.WeaponType;
using DTOArmorType = BlazorWebGame.Shared.DTOs.ArmorType;
using BlazorWebGame.Shared.Enums;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 服务端装备生成器 - 从客户端迁移而来
/// 用于生成指定属性和品质的游戏装备
/// </summary>
public class ServerEquipmentGenerator
{
    private static readonly Random Random = new Random();
    private readonly ILogger<ServerEquipmentGenerator> _logger;

    public ServerEquipmentGenerator(ILogger<ServerEquipmentGenerator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 装备品质枚举
    /// </summary>
    public enum EquipmentQuality
    {
        Common,     // 普装
        Uncommon,   // 绿装
        Rare,       // 蓝装
        Epic        // 紫装
    }

    /// <summary>
    /// 属性等级枚举
    /// </summary>
    public enum AttributeTier
    {
        T1, // 低等级 (0.8±0.05)
        T2, // 中等级 (1±0.05)
        T3  // 高等级 (1.2±0.05)
    }

    /// <summary>
    /// 生成装备 - 主要生成方法
    /// </summary>
    public EquipmentDto GenerateEquipment(
        string name,
        int level,
        EquipmentSlot slot,
        EquipmentQuality quality,
        AttributeTier attributeTier = AttributeTier.T2,
        DTOWeaponType weaponType = DTOWeaponType.None,
        DTOArmorType armorType = DTOArmorType.None,
        bool isTwoHanded = false,
        List<string>? allowedProfessions = null,
        List<string>? secondaryAttributePool = null,
        Dictionary<string, object>? customAttributes = null)
    {
        _logger.LogDebug("Generating equipment: {Name}, Level: {Level}, Quality: {Quality}", 
            name, level, quality);

        // 创建装备实例
        var equipment = new EquipmentDto
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            RequiredLevel = level,
            Slot = slot.ToString(),
            WeaponType = weaponType.ToString(),
            ArmorType = armorType.ToString(),
            IsTwoHanded = isTwoHanded,
            AllowedProfessions = allowedProfessions ?? new List<string>(),
            Description = GenerateDescription(name, quality, level, slot, weaponType, armorType),
            AttributeBonuses = new Dictionary<string, int>
            {
                ["Strength"] = 0,
                ["Agility"] = 0,
                ["Intellect"] = 0,
                ["Spirit"] = 0,
                ["Stamina"] = 0
            },
            ElementalResistances = new Dictionary<string, double>()
        };

        // 根据装备品质添加对应的颜色
        equipment.Name = AddQualityColor(equipment.Name, quality);

        // 应用自定义属性(如果有)
        if (customAttributes != null && customAttributes.Any())
        {
            ApplyCustomAttributes(equipment, customAttributes);
        }
        else
        {
            // 根据装备类型应用不同的属性逻辑
            if (IsWeapon(slot))
            {
                GenerateWeaponAttributes(equipment, level, quality, attributeTier, weaponType, isTwoHanded);
            }
            else if (IsArmor(slot))
            {
                GenerateArmorAttributes(equipment, level, quality, attributeTier, armorType, slot);
            }
            else if (IsJewelry(slot))
            {
                GenerateJewelryAttributes(equipment, level, quality, attributeTier, slot);
            }

            // 生成副属性
            GenerateSecondaryAttributes(equipment, level, quality, attributeTier, secondaryAttributePool);
        }

        // 计算装备价值
        equipment.Value = CalculateEquipmentValue(equipment);

        _logger.LogDebug("Successfully generated equipment: {EquipmentName} (Value: {Value})", 
            equipment.Name, equipment.Value);

        return equipment;
    }

    /// <summary>
    /// 生成武器属性
    /// </summary>
    private void GenerateWeaponAttributes(
        EquipmentDto equipment,
        int level,
        EquipmentQuality quality,
        AttributeTier attributeTier,
        DTOWeaponType weaponType,
        bool isTwoHanded)
    {
        // 确保武器类型设置正确
        if (weaponType == DTOWeaponType.None)
        {
            weaponType = equipment.Slot == EquipmentSlot.OffHand.ToString() ? DTOWeaponType.Shield : DTOWeaponType.Sword;
        }

        // 如果是双手武器类型，确保IsTwoHanded标志设置正确
        if (weaponType == DTOWeaponType.TwoHandSword || weaponType == DTOWeaponType.TwoHandAxe ||
            weaponType == DTOWeaponType.TwoHandMace || weaponType == DTOWeaponType.Polearm ||
            weaponType == DTOWeaponType.Staff)
        {
            equipment.IsTwoHanded = true;
        }

        // 计算基础伤害
        var baseDamage = CalculateBaseDamage(level, quality, weaponType, isTwoHanded);
        equipment.AttributeBonuses["MinDamage"] = (int)(baseDamage * 0.8);
        equipment.AttributeBonuses["MaxDamage"] = (int)(baseDamage * 1.2);

        // 攻击速度
        var attackSpeed = GetWeaponAttackSpeed(weaponType);
        equipment.AttributeBonuses["AttackSpeed"] = (int)(attackSpeed * 100); // 存储为百分比

        // 根据武器类型添加主属性
        AddWeaponPrimaryAttributes(equipment, weaponType, level, quality, attributeTier);

        _logger.LogDebug("Generated weapon attributes for {WeaponType}: Damage {MinDmg}-{MaxDmg}, Speed {Speed}", 
            weaponType, equipment.AttributeBonuses["MinDamage"], equipment.AttributeBonuses["MaxDamage"], attackSpeed);
    }

    /// <summary>
    /// 生成护甲属性
    /// </summary>
    private void GenerateArmorAttributes(
        EquipmentDto equipment,
        int level,
        EquipmentQuality quality,
        AttributeTier attributeTier,
        DTOArmorType armorType,
        EquipmentSlot slot)
    {
        // 基础护甲值
        var baseArmor = CalculateBaseArmor(level, quality, armorType, slot);
        equipment.AttributeBonuses["Armor"] = baseArmor;

        // 根据护甲类型添加属性
        AddArmorAttributes(equipment, armorType, level, quality, attributeTier);

        _logger.LogDebug("Generated armor attributes for {ArmorType}: Armor {Armor}", 
            armorType, baseArmor);
    }

    /// <summary>
    /// 生成饰品属性
    /// </summary>
    private void GenerateJewelryAttributes(
        EquipmentDto equipment,
        int level,
        EquipmentQuality quality,
        AttributeTier attributeTier,
        EquipmentSlot slot)
    {
        // 饰品主要提供属性加成
        AddJewelryAttributes(equipment, level, quality, attributeTier);
        
        _logger.LogDebug("Generated jewelry attributes for slot {Slot}", slot);
    }

    /// <summary>
    /// 计算基础伤害
    /// </summary>
    private double CalculateBaseDamage(int level, EquipmentQuality quality, DTOWeaponType weaponType, bool isTwoHanded)
    {
        var baseDamage = level * 8 + 20; // 基础伤害公式
        
        // 品质修正
        var qualityMultiplier = quality switch
        {
            EquipmentQuality.Common => 1.0,
            EquipmentQuality.Uncommon => 1.15,
            EquipmentQuality.Rare => 1.3,
            EquipmentQuality.Epic => 1.5,
            _ => 1.0
        };

        // 武器类型修正
        var weaponMultiplier = GetWeaponDamageMultiplier(weaponType);
        
        // 双手武器加成
        if (isTwoHanded)
        {
            weaponMultiplier *= 1.4;
        }

        return baseDamage * qualityMultiplier * weaponMultiplier;
    }

    /// <summary>
    /// 计算基础护甲值
    /// </summary>
    private int CalculateBaseArmor(int level, EquipmentQuality quality, DTOArmorType armorType, EquipmentSlot slot)
    {
        var baseArmor = level * 3 + 10;
        
        // 品质修正
        var qualityMultiplier = quality switch
        {
            EquipmentQuality.Common => 1.0,
            EquipmentQuality.Uncommon => 1.15,
            EquipmentQuality.Rare => 1.3,
            EquipmentQuality.Epic => 1.5,
            _ => 1.0
        };

        // 护甲类型修正
        var armorMultiplier = armorType switch
        {
            DTOArmorType.Cloth => 0.7,
            DTOArmorType.Leather => 1.0,
            DTOArmorType.Mail => 1.3,
            DTOArmorType.Plate => 1.6,
            _ => 1.0
        };

        // 部位修正
        var slotMultiplier = slot switch
        {
            EquipmentSlot.Chest => 1.5,
            EquipmentSlot.Legs => 1.3,
            EquipmentSlot.Head => 1.2,
            EquipmentSlot.Shoulder => 1.0,
            EquipmentSlot.Hands => 0.8,
            EquipmentSlot.Feet => 0.9,
            _ => 1.0
        };

        return (int)(baseArmor * qualityMultiplier * armorMultiplier * slotMultiplier);
    }

    /// <summary>
    /// 获取武器伤害倍数
    /// </summary>
    private double GetWeaponDamageMultiplier(DTOWeaponType weaponType) => weaponType switch
    {
        DTOWeaponType.Dagger => 0.8,
        DTOWeaponType.Sword => 1.0,
        DTOWeaponType.Axe => 1.1,
        DTOWeaponType.Mace => 1.05,
        DTOWeaponType.Staff => 0.9,
        DTOWeaponType.Bow => 1.0,
        DTOWeaponType.TwoHandSword => 1.0,
        DTOWeaponType.TwoHandAxe => 1.1,
        DTOWeaponType.TwoHandMace => 1.05,
        DTOWeaponType.Polearm => 1.0,
        _ => 1.0
    };

    /// <summary>
    /// 获取武器攻击速度
    /// </summary>
    private double GetWeaponAttackSpeed(DTOWeaponType weaponType) => weaponType switch
    {
        DTOWeaponType.Dagger => 1.6,
        DTOWeaponType.Sword => 1.4,
        DTOWeaponType.Axe => 1.2,
        DTOWeaponType.Mace => 1.3,
        DTOWeaponType.Staff => 1.5,
        DTOWeaponType.Bow => 1.3,
        DTOWeaponType.TwoHandSword => 1.0,
        DTOWeaponType.TwoHandAxe => 0.9,
        DTOWeaponType.TwoHandMace => 0.95,
        DTOWeaponType.Polearm => 1.1,
        _ => 1.0
    };

    /// <summary>
    /// 添加武器主属性
    /// </summary>
    private void AddWeaponPrimaryAttributes(EquipmentDto equipment, DTOWeaponType weaponType, int level, EquipmentQuality quality, AttributeTier attributeTier)
    {
        var attributeValue = CalculateAttributeValue(level, quality, attributeTier);
        
        switch (weaponType)
        {
            case DTOWeaponType.Sword:
            case DTOWeaponType.Axe:
            case DTOWeaponType.Mace:
            case DTOWeaponType.TwoHandSword:
            case DTOWeaponType.TwoHandAxe:
            case DTOWeaponType.TwoHandMace:
                equipment.AttributeBonuses["Strength"] += attributeValue;
                break;
            case DTOWeaponType.Dagger:
            case DTOWeaponType.Bow:
                equipment.AttributeBonuses["Agility"] += attributeValue;
                break;
            case DTOWeaponType.Staff:
                equipment.AttributeBonuses["Intellect"] += attributeValue;
                break;
            case DTOWeaponType.Polearm:
                equipment.AttributeBonuses["Strength"] += attributeValue / 2;
                equipment.AttributeBonuses["Agility"] += attributeValue / 2;
                break;
        }
    }

    /// <summary>
    /// 添加护甲属性
    /// </summary>
    private void AddArmorAttributes(EquipmentDto equipment, DTOArmorType armorType, int level, EquipmentQuality quality, AttributeTier attributeTier)
    {
        var attributeValue = CalculateAttributeValue(level, quality, attributeTier);
        
        switch (armorType)
        {
            case DTOArmorType.Plate:
                equipment.AttributeBonuses["Strength"] += attributeValue;
                equipment.AttributeBonuses["Stamina"] += attributeValue;
                break;
            case DTOArmorType.Mail:
                equipment.AttributeBonuses["Agility"] += attributeValue;
                equipment.AttributeBonuses["Stamina"] += attributeValue / 2;
                break;
            case DTOArmorType.Leather:
                equipment.AttributeBonuses["Agility"] += attributeValue;
                break;
            case DTOArmorType.Cloth:
                equipment.AttributeBonuses["Intellect"] += attributeValue;
                equipment.AttributeBonuses["Spirit"] += attributeValue / 2;
                break;
        }
    }

    /// <summary>
    /// 添加饰品属性
    /// </summary>
    private void AddJewelryAttributes(EquipmentDto equipment, int level, EquipmentQuality quality, AttributeTier attributeTier)
    {
        var attributeValue = CalculateAttributeValue(level, quality, attributeTier);
        
        // 饰品可以提供多种属性
        var attributeTypes = new[] { "Strength", "Agility", "Intellect", "Spirit", "Stamina" };
        var selectedAttributes = attributeTypes.OrderBy(x => Random.Next()).Take(2).ToArray();
        
        foreach (var attr in selectedAttributes)
        {
            equipment.AttributeBonuses[attr] += attributeValue / 2;
        }
    }

    /// <summary>
    /// 计算属性值
    /// </summary>
    private int CalculateAttributeValue(int level, EquipmentQuality quality, AttributeTier attributeTier)
    {
        var baseValue = level * 2 + 5;
        
        var qualityMultiplier = quality switch
        {
            EquipmentQuality.Common => 1.0,
            EquipmentQuality.Uncommon => 1.2,
            EquipmentQuality.Rare => 1.4,
            EquipmentQuality.Epic => 1.7,
            _ => 1.0
        };

        var tierMultiplier = attributeTier switch
        {
            AttributeTier.T1 => 0.8,
            AttributeTier.T2 => 1.0,
            AttributeTier.T3 => 1.2,
            _ => 1.0
        };

        return (int)(baseValue * qualityMultiplier * tierMultiplier);
    }

    /// <summary>
    /// 生成副属性
    /// </summary>
    private void GenerateSecondaryAttributes(EquipmentDto equipment, int level, EquipmentQuality quality, AttributeTier attributeTier, List<string>? secondaryAttributePool)
    {
        var numSecondaryAttrs = quality switch
        {
            EquipmentQuality.Common => 0,
            EquipmentQuality.Uncommon => 1,
            EquipmentQuality.Rare => 2,
            EquipmentQuality.Epic => 3,
            _ => 0
        };

        if (numSecondaryAttrs > 0)
        {
            var availableAttrs = secondaryAttributePool ?? GetDefaultSecondaryAttributes();
            var selectedAttrs = availableAttrs.OrderBy(x => Random.Next()).Take(numSecondaryAttrs);
            
            foreach (var attr in selectedAttrs)
            {
                var value = CalculateAttributeValue(level, quality, attributeTier) / 3;
                if (equipment.AttributeBonuses.ContainsKey(attr))
                {
                    equipment.AttributeBonuses[attr] += value;
                }
                else
                {
                    equipment.AttributeBonuses[attr] = value;
                }
            }
        }
    }

    /// <summary>
    /// 获取默认副属性池
    /// </summary>
    private List<string> GetDefaultSecondaryAttributes()
    {
        return new List<string>
        {
            "CriticalStrike", "Haste", "Mastery", "Versatility",
            "FireResistance", "IceResistance", "NatureResistance", "ShadowResistance"
        };
    }

    /// <summary>
    /// 添加品质颜色
    /// </summary>
    private string AddQualityColor(string name, EquipmentQuality quality)
    {
        var colorCode = quality switch
        {
            EquipmentQuality.Common => "#FFFFFF",    // 白色
            EquipmentQuality.Uncommon => "#1EFF00",  // 绿色
            EquipmentQuality.Rare => "#0070DD",      // 蓝色
            EquipmentQuality.Epic => "#A335EE",      // 紫色
            _ => "#FFFFFF"
        };

        return $"<color={colorCode}>{name}</color>";
    }

    /// <summary>
    /// 应用自定义属性
    /// </summary>
    private void ApplyCustomAttributes(EquipmentDto equipment, Dictionary<string, object> customAttributes)
    {
        foreach (var kvp in customAttributes)
        {
            if (int.TryParse(kvp.Value.ToString(), out var intValue))
            {
                equipment.AttributeBonuses[kvp.Key] = intValue;
            }
        }
    }

    /// <summary>
    /// 生成装备描述
    /// </summary>
    private string GenerateDescription(string name, EquipmentQuality quality, int level, EquipmentSlot slot, DTOWeaponType weaponType, DTOArmorType armorType)
    {
        var qualityDesc = quality switch
        {
            EquipmentQuality.Common => "普通品质",
            EquipmentQuality.Uncommon => "优秀品质",
            EquipmentQuality.Rare => "稀有品质",
            EquipmentQuality.Epic => "史诗品质",
            _ => "未知品质"
        };

        return $"{qualityDesc}的{name}，适合{level}级角色使用。";
    }

    /// <summary>
    /// 计算装备价值
    /// </summary>
    private int CalculateEquipmentValue(EquipmentDto equipment)
    {
        var baseValue = equipment.RequiredLevel * 10;
        var attributeValue = equipment.AttributeBonuses.Values.Sum() * 2;
        return baseValue + attributeValue;
    }

    /// <summary>
    /// 判断是否为武器
    /// </summary>
    private bool IsWeapon(EquipmentSlot slot) => slot is EquipmentSlot.MainHand or EquipmentSlot.OffHand;

    /// <summary>
    /// 判断是否为护甲
    /// </summary>
    private bool IsArmor(EquipmentSlot slot) => slot is EquipmentSlot.Head or EquipmentSlot.Chest or EquipmentSlot.Legs or EquipmentSlot.Hands or EquipmentSlot.Feet;

    /// <summary>
    /// 判断是否为饰品
    /// </summary>
    private bool IsJewelry(EquipmentSlot slot) => slot is EquipmentSlot.Finger1 or EquipmentSlot.Finger2 or EquipmentSlot.Neck or EquipmentSlot.Trinket1 or EquipmentSlot.Trinket2;
}