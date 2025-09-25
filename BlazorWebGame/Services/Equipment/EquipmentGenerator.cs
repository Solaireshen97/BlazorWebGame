using BlazorWebGame.Models;
using BlazorWebGame.Models.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;
using BlazorWebGame.GameConfig;

namespace BlazorWebGame.Services.Equipments
{
    /// <summary>
    /// 装备生成器：根据指定参数生成游戏装备
    /// </summary>
    public static class EquipmentGenerator
    {
        // 随机数生成器
        private static readonly Random Random = new Random();

        /// <summary>
        /// 装备品质枚举
        /// </summary>
        public enum EquipmentQuality
        {
            Common,     // 白装
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

        #region 主要生成方法
        /// <summary>
        /// 生成装备
        /// </summary>
        /// <param name="name">装备名称</param>
        /// <param name="level">装备等级</param>
        /// <param name="slot">装备槽位</param>
        /// <param name="quality">装备品质</param>
        /// <param name="attributeTier">属性等级</param>
        /// <param name="weaponType">武器类型(如果是武器)</param>
        /// <param name="armorType">护甲类型(如果是护甲)</param>
        /// <param name="isTwoHanded">是否为双手武器</param>
        /// <param name="allowedProfessions">允许使用的职业</param>
        /// <param name="secondaryAttributePool">可选的副属性池</param>
        /// <param name="customAttributes">自定义属性</param>
        public static Equipment GenerateEquipment(
            string name,
            int level,
            EquipmentSlot slot,
            EquipmentQuality quality,
            AttributeTier attributeTier = AttributeTier.T2,
            WeaponType weaponType = WeaponType.None,
            ArmorType armorType = ArmorType.None,
            bool isTwoHanded = false,
            List<BattleProfession>? allowedProfessions = null,
            List<string>? secondaryAttributePool = null,
            Dictionary<string, object>? customAttributes = null)
        {
            // 创建装备实例
            var equipment = new Equipment
            {
                Name = name,
                RequiredLevel = level,
                Slot = slot,
                WeaponType = weaponType,
                ArmorType = armorType,
                IsTwoHanded = isTwoHanded,
                AllowedProfessions = allowedProfessions ?? new List<BattleProfession>(),
                Description = GenerateDescription(name, quality, level, slot, weaponType, armorType)
            };

            // 设置装备品质对应的名称颜色
            equipment.Name = AddQualityColor(equipment.Name, quality);

            // 设置自定义属性(如果有)
            if (customAttributes != null)
            {
                ApplyCustomAttributes(equipment, customAttributes);
            }
            else
            {
                // 根据装备类型应用不同的生成逻辑
                if (IsWeapon(slot))
                {
                    GenerateWeaponAttributes(equipment, level, quality, attributeTier, weaponType, isTwoHanded);
                }
                else if (IsArmor(slot))
                {
                    GenerateArmorAttributes(equipment, level, quality, attributeTier, armorType);
                }
                else if (IsJewelry(slot))
                {
                    GenerateJewelryAttributes(equipment, level, quality, attributeTier, slot);
                }

                // 生成副属性
                GenerateSecondaryAttributes(equipment, level, quality, attributeTier, secondaryAttributePool);
            }

            return equipment;
        }
        #endregion

        #region 属性生成方法
        /// <summary>
        /// 生成武器属性
        /// </summary>
        private static void GenerateWeaponAttributes(
            Equipment equipment,
            int level,
            EquipmentQuality quality,
            AttributeTier attributeTier,
            WeaponType weaponType,
            bool isTwoHanded)
        {
            // 确保武器类型设置正确
            if (weaponType == WeaponType.None)
            {
                weaponType = equipment.Slot == EquipmentSlot.OffHand ? WeaponType.Shield : WeaponType.Sword;
            }

            // 如果是双手武器类型，确保IsTwoHanded标志设置正确
            if (weaponType == WeaponType.TwoHandSword || weaponType == WeaponType.TwoHandAxe ||
                weaponType == WeaponType.TwoHandMace || weaponType == WeaponType.Polearm ||
                weaponType == WeaponType.Staff)
            {
                equipment.IsTwoHanded = true;
            }

            // 获取武器类型修饰符
            var (damageModifier, speedBase) = EquipmentAttributeConfig.WeaponTypeModifiers[weaponType];

            // 双手武器额外增加伤害
            if (equipment.IsTwoHanded && !IsSpecificWeaponType(weaponType))
            {
                damageModifier *= EquipmentAttributeConfig.TwoHandedDamageMultiplier;
            }

            // 计算基础伤害
            double baseDPS = EquipmentAttributeConfig.BaseWeaponDPS *
                Math.Pow(EquipmentAttributeConfig.WeaponDPSLevelMultiplier, level - 1);

            // 应用品质修饰符
            double qualityMultiplier = EquipmentAttributeConfig.WeaponMainAttributeMultipliers[quality];
            baseDPS *= qualityMultiplier;

            // 应用等级修饰符
            baseDPS = ApplyAttributeTierMultiplier(baseDPS, attributeTier);

            // 计算攻击速度和武器伤害
            equipment.AttackSpeed = speedBase;
            equipment.WeaponDamage = (int)Math.Round(baseDPS / speedBase * damageModifier);

            // 主属性加成
            int mainAttributeBonus = (int)Math.Round(level * qualityMultiplier);
            mainAttributeBonus = (int)ApplyAttributeTierMultiplier(mainAttributeBonus, attributeTier);

            // 根据武器类型分配主属性
            switch (weaponType)
            {
                case WeaponType.Sword:
                case WeaponType.Axe:
                case WeaponType.Mace:
                case WeaponType.TwoHandSword:
                case WeaponType.TwoHandAxe:
                case WeaponType.TwoHandMace:
                case WeaponType.Polearm:
                    equipment.AttributeBonuses.Strength = mainAttributeBonus;
                    break;

                case WeaponType.Dagger:
                case WeaponType.Bow:
                case WeaponType.Crossbow:
                case WeaponType.Gun:
                    equipment.AttributeBonuses.Agility = mainAttributeBonus;
                    break;

                case WeaponType.Staff:
                case WeaponType.Wand:
                    equipment.AttributeBonuses.Intellect = mainAttributeBonus;
                    break;

                case WeaponType.Shield:
                    equipment.AttributeBonuses.Stamina = mainAttributeBonus;
                    equipment.BlockChance = (int)Math.Round(EquipmentAttributeConfig.BaseShieldBlockChance +
                        level * EquipmentAttributeConfig.ShieldBlockLevelBonus);
                    break;
            }
        }

        /// <summary>
        /// 生成护甲属性
        /// </summary>
        private static void GenerateArmorAttributes(
            Equipment equipment,
            int level,
            EquipmentQuality quality,
            AttributeTier attributeTier,
            ArmorType armorType)
        {
            // 确保护甲类型设置正确
            if (armorType == ArmorType.None)
            {
                armorType = ArmorType.Leather;
            }
            equipment.ArmorType = armorType;

            // 计算基础护甲值
            double baseArmor = EquipmentAttributeConfig.BaseArmorValue *
                Math.Pow(EquipmentAttributeConfig.ArmorLevelMultiplier, level - 1);

            // 应用护甲类型修饰符
            baseArmor *= EquipmentAttributeConfig.ArmorTypeModifiers[armorType];

            // 应用部位修饰符
            baseArmor *= GetArmorSlotModifier(equipment.Slot);

            // 应用品质修饰符
            double qualityMultiplier = GetQualityMultiplier(quality) / EquipmentAttributeConfig.ArmorQualityDivisor;
            baseArmor *= qualityMultiplier;

            // 应用等级修饰符
            baseArmor = ApplyAttributeTierMultiplier(baseArmor, attributeTier);

            equipment.ArmorValue = (int)Math.Round(baseArmor);

            // 计算主属性
            double totalMainAttributes = level * EquipmentAttributeConfig.QualityMainAttributeMultipliers[quality];
            double slotRatio = EquipmentAttributeConfig.SlotMainAttributeRatios[equipment.Slot];
            int mainAttributeValue = (int)Math.Round(totalMainAttributes * slotRatio);

            // 应用等级修饰符
            mainAttributeValue = (int)ApplyAttributeTierMultiplier(mainAttributeValue, attributeTier);

            // 根据护甲类型分配主属性
            switch (armorType)
            {
                case ArmorType.Cloth:
                    equipment.AttributeBonuses.Intellect = mainAttributeValue;
                    break;

                case ArmorType.Leather:
                    equipment.AttributeBonuses.Agility = mainAttributeValue;
                    break;

                case ArmorType.Mail:
                    equipment.AttributeBonuses.Strength = (int)(mainAttributeValue * EquipmentAttributeConfig.MailArmorAttributeRatio);
                    equipment.AttributeBonuses.Agility = (int)(mainAttributeValue * EquipmentAttributeConfig.MailArmorAttributeRatio);
                    break;

                case ArmorType.Plate:
                    equipment.AttributeBonuses.Strength = mainAttributeValue;
                    break;
            }

            // 耐力是所有护甲的次要属性
            equipment.AttributeBonuses.Stamina = (int)(mainAttributeValue * EquipmentAttributeConfig.ArmorStaminaRatio);

            // 添加生命值加成
            equipment.HealthBonus = equipment.AttributeBonuses.Stamina * EquipmentAttributeConfig.StaminaToHealthRatio;
        }

        /// <summary>
        /// 生成饰品属性
        /// </summary>
        private static void GenerateJewelryAttributes(
            Equipment equipment,
            int level,
            EquipmentQuality quality,
            AttributeTier attributeTier,
            EquipmentSlot slot)
        {
            // 计算主属性总值
            double totalMainAttributes = level * EquipmentAttributeConfig.QualityMainAttributeMultipliers[quality];
            double slotRatio = EquipmentAttributeConfig.SlotMainAttributeRatios[slot];
            int mainAttributeValue = (int)Math.Round(totalMainAttributes * slotRatio);

            // 应用等级修饰符
            mainAttributeValue = (int)ApplyAttributeTierMultiplier(mainAttributeValue, attributeTier);

            // 根据饰品类型分配主属性
            switch (slot)
            {
                case EquipmentSlot.Neck:
                    // 项链均衡分配所有属性
                    int perStat = (int)(mainAttributeValue * EquipmentAttributeConfig.NecklaceEqualStatRatio);
                    equipment.AttributeBonuses.Strength = perStat;
                    equipment.AttributeBonuses.Agility = perStat;
                    equipment.AttributeBonuses.Intellect = mainAttributeValue - 2 * perStat;
                    equipment.AttributeBonuses.Stamina = (int)(perStat * EquipmentAttributeConfig.NecklaceStaminaRatio);
                    break;

                case EquipmentSlot.Finger1:
                case EquipmentSlot.Finger2:
                    // 戒指主要增加两种主属性
                    int primary = (int)(mainAttributeValue * EquipmentAttributeConfig.RingPrimaryStatRatio);
                    int secondary = mainAttributeValue - primary;

                    // 随机选择两种主属性
                    int randomPrimary = Random.Next(3);
                    int randomSecondary = (randomPrimary + 1 + Random.Next(2)) % 3;

                    switch (randomPrimary)
                    {
                        case 0: equipment.AttributeBonuses.Strength = primary; break;
                        case 1: equipment.AttributeBonuses.Agility = primary; break;
                        case 2: equipment.AttributeBonuses.Intellect = primary; break;
                    }

                    switch (randomSecondary)
                    {
                        case 0: equipment.AttributeBonuses.Strength = secondary; break;
                        case 1: equipment.AttributeBonuses.Agility = secondary; break;
                        case 2: equipment.AttributeBonuses.Intellect = secondary; break;
                    }

                    equipment.AttributeBonuses.Stamina = (int)(mainAttributeValue * EquipmentAttributeConfig.RingStaminaRatio);
                    break;

                case EquipmentSlot.Trinket1:
                case EquipmentSlot.Trinket2:
                    // 饰品专注于特殊效果，主属性较少
                    int trinketMainStat = (int)(mainAttributeValue * EquipmentAttributeConfig.TrinketMainStatRatio);

                    // 随机选择一种主属性
                    switch (Random.Next(4))
                    {
                        case 0: equipment.AttributeBonuses.Strength = trinketMainStat; break;
                        case 1: equipment.AttributeBonuses.Agility = trinketMainStat; break;
                        case 2: equipment.AttributeBonuses.Intellect = trinketMainStat; break;
                        case 3: equipment.AttributeBonuses.Spirit = trinketMainStat; break;
                    }

                    equipment.AttributeBonuses.Stamina = (int)(mainAttributeValue * EquipmentAttributeConfig.TrinketStaminaRatio);

                    // 饰品有特殊几率属性
                    equipment.CriticalChanceBonus = EquipmentAttributeConfig.TrinketBaseCriticalChance +
                        (level * EquipmentAttributeConfig.TrinketCriticalChanceLevelBonus);
                    equipment.ExtraLootChanceBonus = EquipmentAttributeConfig.TrinketBaseExtraLootChance +
                        (level * EquipmentAttributeConfig.TrinketExtraLootChanceLevelBonus);
                    break;
            }
        }

        /// <summary>
        /// 生成副属性
        /// </summary>
        private static void GenerateSecondaryAttributes(
            Equipment equipment,
            int level,
            EquipmentQuality quality,
            AttributeTier attributeTier,
            List<string>? secondaryAttributePool)
        {
            // 确定副属性数量
            int secondaryCount = EquipmentAttributeConfig.QualitySecondaryAttributeCount[quality];
            if (secondaryCount <= 0) return;

            // 使用默认副属性池或自定义池
            var attributePool = secondaryAttributePool ?? GetDefaultSecondaryAttributePool(equipment);
            if (!attributePool.Any()) return;

            // 计算副属性基础值
            double baseSecondaryValue = level * EquipmentAttributeConfig.SecondaryAttributeBaseValueMultiplier;

            // 随机选择并应用副属性
            var selectedAttributes = new List<string>();

            for (int i = 0; i < secondaryCount && attributePool.Count > 0; i++)
            {
                // 随机选择一个副属性
                int index = Random.Next(attributePool.Count);
                string attribute = attributePool[index];
                attributePool.RemoveAt(index);
                selectedAttributes.Add(attribute);

                // 为该属性随机选择一个等级
                var randomTier = (AttributeTier)Random.Next(3);

                // 计算属性值
                double attributeValue = baseSecondaryValue;
                attributeValue = ApplyAttributeTierMultiplier(attributeValue, randomTier);

                // 应用副属性
                ApplySecondaryAttribute(equipment, attribute, attributeValue, level);
            }
        }

        /// <summary>
        /// 应用副属性
        /// </summary>
        private static void ApplySecondaryAttribute(Equipment equipment, string attribute, double value, int level)
        {
            if (EquipmentAttributeConfig.SecondaryAttributeEffectMultipliers.TryGetValue(attribute, out double multiplier))
            {
                switch (attribute)
                {
                    case "critical_chance":
                        equipment.CriticalChanceBonus += value * multiplier;
                        break;

                    case "critical_damage":
                        equipment.CriticalDamageBonus += value * multiplier;
                        break;

                    case "attack_power":
                        equipment.AttackBonus += (int)Math.Round(value * multiplier);
                        break;

                    case "attack_speed":
                        equipment.AttackSpeedBonus += value * multiplier;
                        break;

                    case "health":
                        equipment.HealthBonus += (int)Math.Round(value * multiplier);
                        break;

                    case "accuracy":
                        equipment.AccuracyBonus += (int)Math.Round(value * multiplier);
                        break;

                    case "dodge":
                        equipment.DodgeChanceBonus += value * multiplier;
                        break;

                    case "gathering_speed":
                        equipment.GatheringSpeedBonus += value * multiplier;
                        break;

                    case "extra_loot":
                        equipment.ExtraLootChanceBonus += value * multiplier;
                        break;

                    case "crafting_success":
                        equipment.CraftingSuccessBonus += value * multiplier;
                        break;

                    case "resource_conservation":
                        equipment.ResourceConservationBonus += value * multiplier;
                        break;
                }
            }
            else
            {
                // 处理主属性和元素抗性
                switch (attribute)
                {
                    case "strength":
                        equipment.AttributeBonuses.Strength += (int)Math.Round(value);
                        break;

                    case "agility":
                        equipment.AttributeBonuses.Agility += (int)Math.Round(value);
                        break;

                    case "intellect":
                        equipment.AttributeBonuses.Intellect += (int)Math.Round(value);
                        break;

                    case "spirit":
                        equipment.AttributeBonuses.Spirit += (int)Math.Round(value);
                        break;

                    case "stamina":
                        equipment.AttributeBonuses.Stamina += (int)Math.Round(value);
                        equipment.HealthBonus += (int)Math.Round(value * EquipmentAttributeConfig.StaminaToHealthRatio);
                        break;

                    case "fire_resistance":
                        AddElementalResistance(equipment, ElementType.Fire, value * EquipmentAttributeConfig.SecondaryAttributeEffectMultipliers["fire_resistance"]);
                        break;

                    case "ice_resistance":
                        AddElementalResistance(equipment, ElementType.Ice, value * EquipmentAttributeConfig.SecondaryAttributeEffectMultipliers["ice_resistance"]);
                        break;

                    case "lightning_resistance":
                        AddElementalResistance(equipment, ElementType.Lightning, value * EquipmentAttributeConfig.SecondaryAttributeEffectMultipliers["lightning_resistance"]);
                        break;

                    case "nature_resistance":
                        AddElementalResistance(equipment, ElementType.Nature, value * EquipmentAttributeConfig.SecondaryAttributeEffectMultipliers["nature_resistance"]);
                        break;

                    case "shadow_resistance":
                        AddElementalResistance(equipment, ElementType.Shadow, value * EquipmentAttributeConfig.SecondaryAttributeEffectMultipliers["shadow_resistance"]);
                        break;

                    case "holy_resistance":
                        AddElementalResistance(equipment, ElementType.Holy, value * EquipmentAttributeConfig.SecondaryAttributeEffectMultipliers["holy_resistance"]);
                        break;
                }
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 获取护甲槽位修饰符
        /// </summary>
        private static double GetArmorSlotModifier(EquipmentSlot slot)
        {
            if (EquipmentAttributeConfig.ArmorSlotModifiers.TryGetValue(slot, out double modifier))
            {
                return modifier;
            }
            return 0.5; // 默认值
        }

        /// <summary>
        /// 获取品质倍率
        /// </summary>
        private static double GetQualityMultiplier(EquipmentQuality quality)
        {
            return EquipmentAttributeConfig.QualityMainAttributeMultipliers[quality];
        }

        /// <summary>
        /// 应用属性等级倍率
        /// </summary>
        private static double ApplyAttributeTierMultiplier(double value, AttributeTier tier)
        {
            var (min, max) = EquipmentAttributeConfig.AttributeTierMultipliers[tier];
            return value * (min + (Random.NextDouble() * (max - min)));
        }

        /// <summary>
        /// 判断是否为武器槽位
        /// </summary>
        private static bool IsWeapon(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.MainHand || slot == EquipmentSlot.OffHand;
        }

        /// <summary>
        /// 判断是否为护甲槽位
        /// </summary>
        private static bool IsArmor(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.Head || slot == EquipmentSlot.Chest ||
                   slot == EquipmentSlot.Legs || slot == EquipmentSlot.Shoulder ||
                   slot == EquipmentSlot.Hands || slot == EquipmentSlot.Feet ||
                   slot == EquipmentSlot.Wrist || slot == EquipmentSlot.Waist ||
                   slot == EquipmentSlot.Back;
        }

        /// <summary>
        /// 判断是否为饰品槽位
        /// </summary>
        private static bool IsJewelry(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.Neck || slot == EquipmentSlot.Finger1 ||
                   slot == EquipmentSlot.Finger2 || slot == EquipmentSlot.Trinket1 ||
                   slot == EquipmentSlot.Trinket2;
        }

        /// <summary>
        /// 是否为特定武器类型(双手武器)
        /// </summary>
        private static bool IsSpecificWeaponType(WeaponType weaponType)
        {
            return weaponType == WeaponType.TwoHandSword ||
                   weaponType == WeaponType.TwoHandAxe ||
                   weaponType == WeaponType.TwoHandMace ||
                   weaponType == WeaponType.Polearm ||
                   weaponType == WeaponType.Staff;
        }

        /// <summary>
        /// 添加元素抗性
        /// </summary>
        private static void AddElementalResistance(Equipment equipment, ElementType element, double value)
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

        /// <summary>
        /// 获取默认副属性池
        /// </summary>
        private static List<string> GetDefaultSecondaryAttributePool(Equipment equipment)
        {
            var pool = new List<string>
            {
                "critical_chance", "critical_damage", "attack_power", "attack_speed",
                "health", "accuracy", "dodge", "strength", "agility", "intellect",
                "spirit", "stamina", "fire_resistance", "ice_resistance", "lightning_resistance",
                "nature_resistance", "shadow_resistance", "holy_resistance"
            };

            // 对于非战斗装备，添加生产/采集属性
            if (IsJewelry(equipment.Slot) || equipment.Slot == EquipmentSlot.Back ||
                equipment.Slot == EquipmentSlot.Hands)
            {
                pool.AddRange(new[]
                {
                    "gathering_speed", "extra_loot", "crafting_success", "resource_conservation"
                });
            }

            return pool;
        }

        /// <summary>
        /// 应用自定义属性
        /// </summary>
        private static void ApplyCustomAttributes(Equipment equipment, Dictionary<string, object> attributes)
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
                    case "AttributeBonuses" when pair.Value is AttributeSet attributeBonuses:
                        equipment.AttributeBonuses = attributeBonuses;
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
                        // 其他自定义属性...
                }
            }
        }

        /// <summary>
        /// 生成装备描述
        /// </summary>
        private static string GenerateDescription(
            string name,
            EquipmentQuality quality,
            int level,
            EquipmentSlot slot,
            WeaponType weaponType,
            ArmorType armorType)
        {
            string qualityDesc = EquipmentAttributeConfig.QualityDescriptions[quality];

            string typeDesc;
            if (IsWeapon(slot))
            {
                typeDesc = EquipmentAttributeConfig.WeaponTypeDescriptions[weaponType];
            }
            else if (IsArmor(slot))
            {
                string armorTypeDesc = EquipmentAttributeConfig.ArmorTypeDescriptions[armorType];
                string slotDesc = EquipmentAttributeConfig.SlotDescriptions[slot];
                typeDesc = $"{armorTypeDesc}{slotDesc}";
            }
            else
            {
                typeDesc = EquipmentAttributeConfig.SlotDescriptions[slot];
            }

            return $"{qualityDesc}{typeDesc}，适合{level}级冒险者使用。";
        }

        /// <summary>
        /// 添加品质颜色前缀
        /// </summary>
        private static string AddQualityColor(string name, EquipmentQuality quality)
        {
            return EquipmentAttributeConfig.QualityPrefixes[quality] + name;
        }
        #endregion

        #region 公共实用方法
        /// <summary>
        /// 获取装备的虚拟价值，用于出售价格计算
        /// </summary>
        public static int CalculateEquipmentValue(Equipment equipment)
        {
            int baseValue = equipment.RequiredLevel * EquipmentAttributeConfig.EquipmentBasePricePerLevel;

            // 根据品质增加价值
            if (equipment.Name.StartsWith("[史诗]"))
                baseValue *= EquipmentAttributeConfig.QualityPriceMultipliers[EquipmentQuality.Epic];
            else if (equipment.Name.StartsWith("[稀有]"))
                baseValue *= EquipmentAttributeConfig.QualityPriceMultipliers[EquipmentQuality.Rare];
            else if (equipment.Name.StartsWith("[优质]"))
                baseValue *= EquipmentAttributeConfig.QualityPriceMultipliers[EquipmentQuality.Uncommon];

            // 武器和护甲有额外价值
            if (equipment.WeaponDamage > 0)
            {
                baseValue += equipment.WeaponDamage * EquipmentAttributeConfig.WeaponDamagePriceMultiplier;
            }

            if (equipment.ArmorValue > 0)
            {
                baseValue += equipment.ArmorValue * EquipmentAttributeConfig.ArmorValuePriceMultiplier;
            }

            // 主属性增加价值
            baseValue += equipment.AttributeBonuses.Strength * EquipmentAttributeConfig.MainAttributePriceMultiplier;
            baseValue += equipment.AttributeBonuses.Agility * EquipmentAttributeConfig.MainAttributePriceMultiplier;
            baseValue += equipment.AttributeBonuses.Intellect * EquipmentAttributeConfig.MainAttributePriceMultiplier;
            baseValue += equipment.AttributeBonuses.Spirit * EquipmentAttributeConfig.MainAttributePriceMultiplier;
            baseValue += equipment.AttributeBonuses.Stamina * EquipmentAttributeConfig.MainAttributePriceMultiplier;

            // 其他特殊属性增加价值
            baseValue += equipment.HealthBonus / EquipmentAttributeConfig.HealthBonusPriceDivisor;
            baseValue += equipment.AttackBonus * EquipmentAttributeConfig.AttackBonusPriceMultiplier;
            baseValue += (int)(equipment.CriticalChanceBonus * EquipmentAttributeConfig.CriticalChancePriceMultiplier);
            baseValue += (int)(equipment.CriticalDamageBonus * EquipmentAttributeConfig.CriticalDamagePriceMultiplier);

            return baseValue;
        }

        /// <summary>
        /// 根据名称猜测装备的武器类型
        /// </summary>
        public static WeaponType GuessWeaponTypeFromName(string name)
        {
            name = name.ToLower();

            if (name.Contains("剑")) return name.Contains("双手") ? WeaponType.TwoHandSword : WeaponType.Sword;
            if (name.Contains("匕首") || name.Contains("短剑")) return WeaponType.Dagger;
            if (name.Contains("斧")) return name.Contains("双手") ? WeaponType.TwoHandAxe : WeaponType.Axe;
            if (name.Contains("锤") || name.Contains("槌")) return name.Contains("双手") ? WeaponType.TwoHandMace : WeaponType.Mace;
            if (name.Contains("法杖") || name.Contains("魔杖")) return name.Contains("法杖") ? WeaponType.Staff : WeaponType.Wand;
            if (name.Contains("弓")) return name.Contains("弩") ? WeaponType.Crossbow : WeaponType.Bow;
            if (name.Contains("枪")) return WeaponType.Gun;
            if (name.Contains("盾") || name.Contains("shield")) return WeaponType.Shield;
            if (name.Contains("长柄") || name.Contains("矛") || name.Contains("枪")) return WeaponType.Polearm;

            // 默认为剑
            return WeaponType.Sword;
        }

        /// <summary>
        /// 根据名称猜测装备的护甲类型
        /// </summary>
        public static ArmorType GuessArmorTypeFromName(string name)
        {
            name = name.ToLower();

            if (name.Contains("布") || name.Contains("袍") || name.Contains("法师")) return ArmorType.Cloth;
            if (name.Contains("皮") || name.Contains("猎人")) return ArmorType.Leather;
            if (name.Contains("锁") || name.Contains("链") || name.Contains("萨满")) return ArmorType.Mail;
            if (name.Contains("板") || name.Contains("战士")) return ArmorType.Plate;

            // 默认为皮甲
            return ArmorType.Leather;
        }
        #endregion
    }
}