using BlazorWebGame.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 服务端装备生成服务 - 从客户端迁移而来
/// </summary>
public class ServerEquipmentService
{
    private readonly ILogger<ServerEquipmentService> _logger;
    private readonly Random _random = new();

    public ServerEquipmentService(ILogger<ServerEquipmentService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 生成装备 - 主要生成方法
    /// </summary>
    public EquipmentDto GenerateEquipment(EquipmentGenerationRequest request)
    {
        _logger.LogDebug("生成装备: {Name}, 等级: {Level}, 品质: {Quality}", 
            request.Name, request.Level, request.Quality);

        // 创建装备DTO实例
        var equipment = new EquipmentDto
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            RequiredLevel = request.Level,
            Slot = request.Slot,
            WeaponType = request.WeaponType,
            ArmorType = request.ArmorType,
            IsTwoHanded = request.IsTwoHanded,
            AllowedProfessions = request.AllowedProfessions ?? new List<string>(),
            Description = GenerateDescription(request.Name, request.Quality, request.Level, 
                request.Slot, request.WeaponType, request.ArmorType),
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
        equipment.Name = AddQualityColor(equipment.Name, request.Quality);

        // 应用自定义属性(如果有)
        if (request.CustomAttributes != null && request.CustomAttributes.Any())
        {
            ApplyCustomAttributes(equipment, request.CustomAttributes);
        }
        else
        {
            // 根据装备类型应用不同的属性逻辑
            if (IsWeapon(request.Slot))
            {
                GenerateWeaponAttributes(equipment, request.Level, request.Quality, 
                    request.AttributeTier, request.WeaponType, request.IsTwoHanded);
            }
            else if (IsArmor(request.Slot))
            {
                GenerateArmorAttributes(equipment, request.Level, request.Quality, 
                    request.AttributeTier, request.ArmorType);
            }
            else if (IsJewelry(request.Slot))
            {
                GenerateJewelryAttributes(equipment, request.Level, request.Quality, 
                    request.AttributeTier, request.Slot);
            }

            // 生成副属性
            GenerateSecondaryAttributes(equipment, request.Level, request.Quality, 
                request.AttributeTier, request.SecondaryAttributePool);
        }

        // 计算装备价值
        equipment.Value = CalculateEquipmentValue(equipment);

        _logger.LogDebug("成功生成装备: {EquipmentName} (价值: {Value})", equipment.Name, equipment.Value);

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
        string weaponType,
        bool isTwoHanded)
    {
        // 确保武器类型设置正确
        if (weaponType == "None")
        {
            weaponType = equipment.Slot == "OffHand" ? "Shield" : "Sword";
        }

        // 根据双手武器类型，确保IsTwoHanded标志设置正确
        if (weaponType == "TwoHandSword" || weaponType == "TwoHandAxe" ||
            weaponType == "TwoHandMace" || weaponType == "Polearm" ||
            weaponType == "Staff")
        {
            equipment.IsTwoHanded = true;
        }

        // 基于等级计算基础伤害
        double baseDPS = 10 * Math.Pow(1.1, level - 1);
        double qualityMultiplier = GetQualityMultiplier(quality);
        baseDPS *= qualityMultiplier;

        // 应用等级调整倍率
        baseDPS = ApplyAttributeTierMultiplier(baseDPS, attributeTier);

        // 计算攻击速度和武器伤害
        equipment.AttackSpeed = GetWeaponSpeedBase(weaponType);
        double damageModifier = GetWeaponDamageModifier(weaponType);
        
        // 双手武器增加额外伤害
        if (equipment.IsTwoHanded && !IsSpecificWeaponType(weaponType))
        {
            damageModifier *= 1.3;
        }

        equipment.WeaponDamage = (int)Math.Round(baseDPS / equipment.AttackSpeed * damageModifier);

        // 主属性加成
        int mainAttributeBonus = (int)Math.Round(level * qualityMultiplier);
        mainAttributeBonus = (int)ApplyAttributeTierMultiplier(mainAttributeBonus, attributeTier);

        // 根据武器类型分配主属性
        switch (weaponType)
        {
            case "Sword":
            case "Axe": 
            case "Mace":
            case "TwoHandSword":
            case "TwoHandAxe":
            case "TwoHandMace":
            case "Polearm":
                equipment.AttributeBonuses["Strength"] = mainAttributeBonus;
                break;

            case "Dagger":
            case "Bow":
            case "Crossbow":
            case "Gun":
                equipment.AttributeBonuses["Agility"] = mainAttributeBonus;
                break;

            case "Staff":
            case "Wand":
                equipment.AttributeBonuses["Intellect"] = mainAttributeBonus;
                break;

            case "Shield":
                equipment.AttributeBonuses["Stamina"] = mainAttributeBonus;
                equipment.BlockChance = (int)Math.Round(5 + level * 0.5);
                break;
        }
    }

    /// <summary>
    /// 生成护甲属性
    /// </summary>
    private void GenerateArmorAttributes(
        EquipmentDto equipment,
        int level,
        EquipmentQuality quality,
        AttributeTier attributeTier,
        string armorType)
    {
        // 确保护甲类型设置正确
        if (armorType == "None")
        {
            armorType = "Leather";
        }
        equipment.ArmorType = armorType;

        // 计算基础护甲值
        double baseArmor = 5 * Math.Pow(1.1, level - 1);
        baseArmor *= GetArmorTypeModifier(armorType);
        baseArmor *= GetArmorSlotModifier(equipment.Slot);
        baseArmor *= GetQualityMultiplier(quality) / 2;
        baseArmor = ApplyAttributeTierMultiplier(baseArmor, attributeTier);

        equipment.ArmorValue = (int)Math.Round(baseArmor);

        // 计算主属性
        double totalMainAttributes = level * GetQualityMultiplier(quality);
        double slotRatio = GetSlotMainAttributeRatio(equipment.Slot);
        int mainAttributeValue = (int)Math.Round(totalMainAttributes * slotRatio);
        mainAttributeValue = (int)ApplyAttributeTierMultiplier(mainAttributeValue, attributeTier);

        // 根据护甲类型分配主属性
        switch (armorType)
        {
            case "Cloth":
                equipment.AttributeBonuses["Intellect"] = mainAttributeValue;
                break;
            case "Leather":
                equipment.AttributeBonuses["Agility"] = mainAttributeValue;
                break;
            case "Mail":
                equipment.AttributeBonuses["Strength"] = (int)(mainAttributeValue * 0.5);
                equipment.AttributeBonuses["Agility"] = (int)(mainAttributeValue * 0.5);
                break;
            case "Plate":
                equipment.AttributeBonuses["Strength"] = mainAttributeValue;
                break;
        }

        // 所有护甲都有的耐力
        equipment.AttributeBonuses["Stamina"] = (int)(mainAttributeValue * 0.3);
        equipment.HealthBonus = equipment.AttributeBonuses["Stamina"] * 10;
    }

    /// <summary>
    /// 生成饰品属性
    /// </summary>
    private void GenerateJewelryAttributes(
        EquipmentDto equipment,
        int level,
        EquipmentQuality quality,
        AttributeTier attributeTier,
        string slot)
    {
        // 计算基础属性值
        double totalMainAttributes = level * GetQualityMultiplier(quality);
        double slotRatio = GetSlotMainAttributeRatio(slot);
        int mainAttributeValue = (int)Math.Round(totalMainAttributes * slotRatio);
        mainAttributeValue = (int)ApplyAttributeTierMultiplier(mainAttributeValue, attributeTier);

        // 根据饰品类型分配主属性
        switch (slot)
        {
            case "Neck":
                // 项链平均分配属性
                int perStat = (int)(mainAttributeValue * 0.25);
                equipment.AttributeBonuses["Strength"] = perStat;
                equipment.AttributeBonuses["Agility"] = perStat;
                equipment.AttributeBonuses["Intellect"] = mainAttributeValue - 2 * perStat;
                equipment.AttributeBonuses["Stamina"] = (int)(perStat * 0.5);
                break;

            case "Finger1":
            case "Finger2":
                // 戒指主要给两个随机属性
                int primary = (int)(mainAttributeValue * 0.6);
                int secondary = mainAttributeValue - primary;

                // 随机选择主次属性
                var attributes = new[] { "Strength", "Agility", "Intellect" };
                int randomPrimary = _random.Next(3);
                int randomSecondary = (randomPrimary + 1 + _random.Next(2)) % 3;

                equipment.AttributeBonuses[attributes[randomPrimary]] = primary;
                equipment.AttributeBonuses[attributes[randomSecondary]] = secondary;
                equipment.AttributeBonuses["Stamina"] = (int)(mainAttributeValue * 0.2);
                break;

            case "Trinket1":
            case "Trinket2":
                // 饰品专注于特殊效果，少量主属性
                int trinketMainStat = (int)(mainAttributeValue * 0.4);

                // 随机选择一个主属性
                var allAttributes = new[] { "Strength", "Agility", "Intellect", "Spirit" };
                equipment.AttributeBonuses[allAttributes[_random.Next(4)]] = trinketMainStat;
                equipment.AttributeBonuses["Stamina"] = (int)(mainAttributeValue * 0.3);

                // 饰品提供额外效果
                equipment.CriticalChanceBonus = 2 + (level * 0.2);
                equipment.ExtraLootChanceBonus = 1 + (level * 0.1);
                break;
        }
    }

    /// <summary>
    /// 生成副属性
    /// </summary>
    private void GenerateSecondaryAttributes(
        EquipmentDto equipment,
        int level,
        EquipmentQuality quality,
        AttributeTier attributeTier,
        List<string>? secondaryAttributePool)
    {
        // 确定副属性数量
        int secondaryCount = GetQualitySecondaryAttributeCount(quality);
        if (secondaryCount <= 0) return;

        // 使用默认副属性池或自定义
        var attributePool = secondaryAttributePool ?? GetDefaultSecondaryAttributePool(equipment);
        if (!attributePool.Any()) return;

        // 计算副属性基础值
        double baseSecondaryValue = level * 0.5;

        // 随机选择应用副属性
        var selectedAttributes = new List<string>();

        for (int i = 0; i < secondaryCount && attributePool.Count > 0; i++)
        {
            // 随机选择一个属性
            int index = _random.Next(attributePool.Count);
            string attribute = attributePool[index];
            attributePool.RemoveAt(index);
            selectedAttributes.Add(attribute);

            // 为属性随机选择一个等级
            var randomTier = (AttributeTier)_random.Next(3);

            // 计算属性值
            double attributeValue = baseSecondaryValue;
            attributeValue = ApplyAttributeTierMultiplier(attributeValue, randomTier);

            // 应用副属性
            ApplySecondaryAttribute(equipment, attribute, attributeValue, level);
        }
    }

    #region 辅助方法

    private double GetQualityMultiplier(EquipmentQuality quality)
    {
        return quality switch
        {
            EquipmentQuality.Common => 1.0,
            EquipmentQuality.Uncommon => 1.2,
            EquipmentQuality.Rare => 1.5,
            EquipmentQuality.Epic => 2.0,
            _ => 1.0
        };
    }

    private double ApplyAttributeTierMultiplier(double value, AttributeTier tier)
    {
        var (min, max) = tier switch
        {
            AttributeTier.T1 => (0.75, 0.85),
            AttributeTier.T2 => (0.95, 1.05),
            AttributeTier.T3 => (1.15, 1.25),
            _ => (0.95, 1.05)
        };
        return value * (min + (_random.NextDouble() * (max - min)));
    }

    private double GetWeaponSpeedBase(string weaponType)
    {
        return weaponType switch
        {
            "Dagger" => 2.0,
            "Sword" => 1.5,
            "Axe" => 1.3,
            "Mace" => 1.2,
            "TwoHandSword" => 1.0,
            "TwoHandAxe" => 0.9,
            "TwoHandMace" => 0.8,
            "Polearm" => 1.1,
            "Bow" => 1.4,
            "Crossbow" => 1.2,
            "Gun" => 1.3,
            "Staff" => 1.0,
            "Wand" => 1.8,
            _ => 1.5
        };
    }

    private double GetWeaponDamageModifier(string weaponType)
    {
        return weaponType switch
        {
            "Dagger" => 0.8,
            "Sword" => 1.0,
            "Axe" => 1.1,
            "Mace" => 1.2,
            "TwoHandSword" => 1.5,
            "TwoHandAxe" => 1.6,
            "TwoHandMace" => 1.7,
            "Polearm" => 1.4,
            "Bow" => 1.0,
            "Crossbow" => 1.1,
            "Gun" => 1.2,
            "Staff" => 1.0,
            "Wand" => 0.7,
            "Shield" => 0.3,
            _ => 1.0
        };
    }

    private double GetArmorTypeModifier(string armorType)
    {
        return armorType switch
        {
            "Cloth" => 0.7,
            "Leather" => 1.0,
            "Mail" => 1.3,
            "Plate" => 1.6,
            _ => 1.0
        };
    }

    private double GetArmorSlotModifier(string slot)
    {
        return slot switch
        {
            "Head" => 1.0,
            "Chest" => 1.5,
            "Legs" => 1.3,
            "Shoulder" => 0.8,
            "Hands" => 0.7,
            "Feet" => 0.8,
            "Wrist" => 0.5,
            "Waist" => 0.6,
            "Back" => 0.9,
            _ => 0.5
        };
    }

    private double GetSlotMainAttributeRatio(string slot)
    {
        return slot switch
        {
            "Head" => 1.0,
            "Chest" => 1.5,
            "Legs" => 1.3,
            "Shoulder" => 0.8,
            "Hands" => 0.7,
            "Feet" => 0.8,
            "Wrist" => 0.5,
            "Waist" => 0.6,
            "Back" => 0.9,
            "Neck" => 1.2,
            "Finger1" => 0.6,
            "Finger2" => 0.6,
            "Trinket1" => 0.8,
            "Trinket2" => 0.8,
            _ => 0.5
        };
    }

    private int GetQualitySecondaryAttributeCount(EquipmentQuality quality)
    {
        return quality switch
        {
            EquipmentQuality.Common => 0,
            EquipmentQuality.Uncommon => 1,
            EquipmentQuality.Rare => 2,
            EquipmentQuality.Epic => 3,
            _ => 0
        };
    }

    private bool IsWeapon(string slot)
    {
        return slot == "MainHand" || slot == "OffHand";
    }

    private bool IsArmor(string slot)
    {
        return slot == "Head" || slot == "Chest" ||
               slot == "Legs" || slot == "Shoulder" ||
               slot == "Hands" || slot == "Feet" ||
               slot == "Wrist" || slot == "Waist" ||
               slot == "Back";
    }

    private bool IsJewelry(string slot)
    {
        return slot == "Neck" || slot == "Finger1" ||
               slot == "Finger2" || slot == "Trinket1" ||
               slot == "Trinket2";
    }

    private bool IsSpecificWeaponType(string weaponType)
    {
        return weaponType == "TwoHandSword" ||
               weaponType == "TwoHandAxe" ||
               weaponType == "TwoHandMace" ||
               weaponType == "Polearm" ||
               weaponType == "Staff";
    }

    private List<string> GetDefaultSecondaryAttributePool(EquipmentDto equipment)
    {
        var pool = new List<string>
        {
            "critical_chance", "critical_damage", "attack_power", "attack_speed",
            "health", "accuracy", "dodge", "strength", "agility", "intellect",
            "spirit", "stamina", "fire_resistance", "ice_resistance", "lightning_resistance",
            "nature_resistance", "shadow_resistance", "holy_resistance"
        };

        // 对于非战斗装备增加生产/采集属性
        if (IsJewelry(equipment.Slot) || equipment.Slot == "Back" ||
            equipment.Slot == "Hands")
        {
            pool.AddRange(new[]
            {
                "gathering_speed", "extra_loot", "crafting_success", "resource_conservation"
            });
        }

        return pool;
    }

    private void ApplySecondaryAttribute(EquipmentDto equipment, string attribute, double value, int level)
    {
        switch (attribute)
        {
            case "critical_chance":
                equipment.CriticalChanceBonus += value * 0.5;
                break;
            case "critical_damage":
                equipment.CriticalDamageBonus += value * 2;
                break;
            case "attack_power":
                equipment.AttackBonus += (int)Math.Round(value * 2);
                break;
            case "attack_speed":
                equipment.AttackSpeedBonus += value * 0.05;
                break;
            case "health":
                equipment.HealthBonus += (int)Math.Round(value * 10);
                break;
            case "accuracy":
                equipment.AccuracyBonus += (int)Math.Round(value);
                break;
            case "dodge":
                equipment.DodgeChanceBonus += value * 0.3;
                break;
            case "gathering_speed":
                equipment.GatheringSpeedBonus += value * 0.1;
                break;
            case "extra_loot":
                equipment.ExtraLootChanceBonus += value * 0.2;
                break;
            case "crafting_success":
                equipment.CraftingSuccessBonus += value * 0.3;
                break;
            case "resource_conservation":
                equipment.ResourceConservationBonus += value * 0.2;
                break;

            // 主属性副属性
            case "strength":
                equipment.AttributeBonuses["Strength"] += (int)Math.Round(value);
                break;
            case "agility":
                equipment.AttributeBonuses["Agility"] += (int)Math.Round(value);
                break;
            case "intellect":
                equipment.AttributeBonuses["Intellect"] += (int)Math.Round(value);
                break;
            case "spirit":
                equipment.AttributeBonuses["Spirit"] += (int)Math.Round(value);
                break;
            case "stamina":
                equipment.AttributeBonuses["Stamina"] += (int)Math.Round(value);
                equipment.HealthBonus += (int)Math.Round(value * 10);
                break;

            // 元素抗性
            case "fire_resistance":
                AddElementalResistance(equipment, "Fire", value * 2);
                break;
            case "ice_resistance":
                AddElementalResistance(equipment, "Ice", value * 2);
                break;
            case "lightning_resistance":
                AddElementalResistance(equipment, "Lightning", value * 2);
                break;
            case "nature_resistance":
                AddElementalResistance(equipment, "Nature", value * 2);
                break;
            case "shadow_resistance":
                AddElementalResistance(equipment, "Shadow", value * 2);
                break;
            case "holy_resistance":
                AddElementalResistance(equipment, "Holy", value * 2);
                break;
        }
    }

    private void AddElementalResistance(EquipmentDto equipment, string element, double value)
    {
        if (equipment.ElementalResistances.ContainsKey(element))
        {
            equipment.ElementalResistances[element] += value;
        }
        else
        {
            equipment.ElementalResistances[element] = value;
        }
    }

    private void ApplyCustomAttributes(EquipmentDto equipment, Dictionary<string, object> attributes)
    {
        foreach (var pair in attributes)
        {
            switch (pair.Key)
            {
                case "ArmorValue" when pair.Value is int armorValue:
                    equipment.ArmorValue = armorValue;
                    break;
                case "WeaponDamage" when pair.Value is int weaponDamage:
                    equipment.WeaponDamage = weaponDamage;
                    break;
                case "AttackSpeed" when pair.Value is double attackSpeed:
                    equipment.AttackSpeed = attackSpeed;
                    break;
                case "BlockChance" when pair.Value is int blockChance:
                    equipment.BlockChance = blockChance;
                    break;
                case "AttackBonus" when pair.Value is int attackBonus:
                    equipment.AttackBonus = attackBonus;
                    break;
                case "HealthBonus" when pair.Value is int healthBonus:
                    equipment.HealthBonus = healthBonus;
                    break;
                case "CriticalChanceBonus" when pair.Value is double critChance:
                    equipment.CriticalChanceBonus = critChance;
                    break;
                case "CriticalDamageBonus" when pair.Value is double critDamage:
                    equipment.CriticalDamageBonus = critDamage;
                    break;
            }
        }
    }

    private string GenerateDescription(
        string name,
        EquipmentQuality quality,
        int level,
        string slot,
        string weaponType,
        string armorType)
    {
        string qualityDesc = quality switch
        {
            EquipmentQuality.Common => "普通",
            EquipmentQuality.Uncommon => "精良",
            EquipmentQuality.Rare => "稀有",
            EquipmentQuality.Epic => "史诗",
            _ => "普通"
        };

        string typeDesc;
        if (IsWeapon(slot))
        {
            typeDesc = weaponType switch
            {
                "Sword" => "剑",
                "Axe" => "斧",
                "Mace" => "锤",
                "Dagger" => "匕首",
                "Bow" => "弓",
                "Staff" => "法杖",
                "Shield" => "盾牌",
                _ => "武器"
            };
        }
        else if (IsArmor(slot))
        {
            string armorTypeDesc = armorType switch
            {
                "Cloth" => "布甲",
                "Leather" => "皮甲",
                "Mail" => "锁甲",
                "Plate" => "板甲",
                _ => "护甲"
            };
            string slotDesc = slot switch
            {
                "Head" => "头盔",
                "Chest" => "胸甲",
                "Legs" => "护腿",
                _ => "护甲"
            };
            typeDesc = $"{armorTypeDesc}{slotDesc}";
        }
        else
        {
            typeDesc = slot switch
            {
                "Neck" => "项链",
                "Finger1" => "戒指",
                "Finger2" => "戒指",
                _ => "饰品"
            };
        }

        return $"{qualityDesc}的{typeDesc}，适合{level}级冒险者使用。";
    }

    private string AddQualityColor(string name, EquipmentQuality quality)
    {
        string prefix = quality switch
        {
            EquipmentQuality.Common => "",
            EquipmentQuality.Uncommon => "[精良]",
            EquipmentQuality.Rare => "[稀有]",
            EquipmentQuality.Epic => "[史诗]",
            _ => ""
        };
        return prefix + name;
    }

    /// <summary>
    /// 计算装备的商店价值，用于出售价格计算
    /// </summary>
    public int CalculateEquipmentValue(EquipmentDto equipment)
    {
        int baseValue = equipment.RequiredLevel * 10;

        // 根据品质增加价值
        if (equipment.Name.StartsWith("[史诗]"))
            baseValue *= 8;
        else if (equipment.Name.StartsWith("[稀有]"))
            baseValue *= 4;
        else if (equipment.Name.StartsWith("[精良]"))
            baseValue *= 2;

        // 根据武器伤害增加价值
        if (equipment.WeaponDamage > 0)
        {
            baseValue += equipment.WeaponDamage * 2;
        }

        // 根据护甲值增加价值
        if (equipment.ArmorValue > 0)
        {
            baseValue += equipment.ArmorValue * 3;
        }

        // 根据主属性增加价值
        baseValue += equipment.AttributeBonuses.GetValueOrDefault("Strength", 0) * 5;
        baseValue += equipment.AttributeBonuses.GetValueOrDefault("Agility", 0) * 5;
        baseValue += equipment.AttributeBonuses.GetValueOrDefault("Intellect", 0) * 5;
        baseValue += equipment.AttributeBonuses.GetValueOrDefault("Spirit", 0) * 5;
        baseValue += equipment.AttributeBonuses.GetValueOrDefault("Stamina", 0) * 5;

        // 根据其他属性增加价值
        baseValue += equipment.HealthBonus / 10;
        baseValue += equipment.AttackBonus * 3;
        baseValue += (int)(equipment.CriticalChanceBonus * 20);
        baseValue += (int)(equipment.CriticalDamageBonus * 10);

        return Math.Max(baseValue, 1);
    }

    /// <summary>
    /// 根据名称猜测武器类型
    /// </summary>
    public string GuessWeaponTypeFromName(string name)
    {
        name = name.ToLower();

        if (name.Contains("剑")) return name.Contains("双手") ? "TwoHandSword" : "Sword";
        if (name.Contains("匕首") || name.Contains("短剑")) return "Dagger";
        if (name.Contains("斧")) return name.Contains("双手") ? "TwoHandAxe" : "Axe";
        if (name.Contains("锤") || name.Contains("鎚")) return name.Contains("双手") ? "TwoHandMace" : "Mace";
        if (name.Contains("法杖") || name.Contains("魔杖")) return name.Contains("法杖") ? "Staff" : "Wand";
        if (name.Contains("弓")) return name.Contains("弩") ? "Crossbow" : "Bow";
        if (name.Contains("枪")) return "Gun";
        if (name.Contains("盾") || name.Contains("shield")) return "Shield";
        if (name.Contains("长枪") || name.Contains("矛") || name.Contains("戟")) return "Polearm";

        // 默认为剑
        return "Sword";
    }

    /// <summary>
    /// 根据名称猜测装备的护甲类型
    /// </summary>
    public string GuessArmorTypeFromName(string name)
    {
        name = name.ToLower();

        if (name.Contains("布") || name.Contains("袍") || name.Contains("法师")) return "Cloth";
        if (name.Contains("皮") || name.Contains("皮革")) return "Leather";
        if (name.Contains("锁") || name.Contains("环") || name.Contains("萨满")) return "Mail";
        if (name.Contains("板") || name.Contains("战士")) return "Plate";

        // 默认为皮甲
        return "Leather";
    }

    #endregion
}