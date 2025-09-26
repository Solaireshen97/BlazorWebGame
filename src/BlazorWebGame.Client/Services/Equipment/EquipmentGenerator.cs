using BlazorWebGame.Models;
using BlazorWebGame.Models.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;
using BlazorWebGame.GameConfig;

using BlazorWebGame.Shared.Enums;
using SharedEquipment = BlazorWebGame.Shared.Models.Items.Equipment;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Services.Equipments
{
    /// <summary>
    /// װ��������������ָ������������Ϸװ��
    /// </summary>
    public static class EquipmentGenerator
    {
        // �����������
        private static readonly Random Random = new Random();

        /// <summary>
        /// װ��Ʒ��ö��
        /// </summary>
        public enum EquipmentQuality
        {
            Common,     // ��װ
            Uncommon,   // ��װ
            Rare,       // ��װ
            Epic        // ��װ
        }

        /// <summary>
        /// ���Եȼ�ö��
        /// </summary>
        public enum AttributeTier
        {
            T1, // �͵ȼ� (0.8��0.05)
            T2, // �еȼ� (1��0.05)
            T3  // �ߵȼ� (1.2��0.05)
        }

        #region ��Ҫ���ɷ���
        /// <summary>
        /// ����װ��
        /// </summary>
        /// <param name="name">װ������</param>
        /// <param name="level">װ���ȼ�</param>
        /// <param name="slot">װ����λ</param>
        /// <param name="quality">װ��Ʒ��</param>
        /// <param name="attributeTier">���Եȼ�</param>
        /// <param name="weaponType">��������(���������)</param>
        /// <param name="armorType">��������(����ǻ���)</param>
        /// <param name="isTwoHanded">�Ƿ�Ϊ˫������</param>
        /// <param name="allowedProfessions">����ʹ�õ�ְҵ</param>
        /// <param name="secondaryAttributePool">��ѡ�ĸ����Գ�</param>
        /// <param name="customAttributes">�Զ�������</param>
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
            // ����װ��ʵ��
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

            // ����װ��Ʒ�ʶ�Ӧ��������ɫ
            equipment.Name = AddQualityColor(equipment.Name, quality);

            // �����Զ�������(�����)
            if (customAttributes != null)
            {
                ApplyCustomAttributes(equipment, customAttributes);
            }
            else
            {
                // ����װ������Ӧ�ò�ͬ�������߼�
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

                // ���ɸ�����
                GenerateSecondaryAttributes(equipment, level, quality, attributeTier, secondaryAttributePool);
            }

            return equipment;
        }
        #endregion

        #region �������ɷ���
        /// <summary>
        /// ������������
        /// </summary>
        private static void GenerateWeaponAttributes(
            Equipment equipment,
            int level,
            EquipmentQuality quality,
            AttributeTier attributeTier,
            WeaponType weaponType,
            bool isTwoHanded)
        {
            // ȷ����������������ȷ
            if (weaponType == WeaponType.None)
            {
                weaponType = equipment.Slot == EquipmentSlot.OffHand ? WeaponType.Shield : WeaponType.Sword;
            }

            // �����˫���������ͣ�ȷ��IsTwoHanded��־������ȷ
            if (weaponType == WeaponType.TwoHandSword || weaponType == WeaponType.TwoHandAxe ||
                weaponType == WeaponType.TwoHandMace || weaponType == WeaponType.Polearm ||
                weaponType == WeaponType.Staff)
            {
                equipment.IsTwoHanded = true;
            }

            // ��ȡ�����������η�
            var (damageModifier, speedBase) = EquipmentAttributeConfig.WeaponTypeModifiers[weaponType];

            // ˫���������������˺�
            if (equipment.IsTwoHanded && !IsSpecificWeaponType(weaponType))
            {
                damageModifier *= EquipmentAttributeConfig.TwoHandedDamageMultiplier;
            }

            // ��������˺�
            double baseDPS = EquipmentAttributeConfig.BaseWeaponDPS *
                Math.Pow(EquipmentAttributeConfig.WeaponDPSLevelMultiplier, level - 1);

            // Ӧ��Ʒ�����η�
            double qualityMultiplier = EquipmentAttributeConfig.WeaponMainAttributeMultipliers[quality];
            baseDPS *= qualityMultiplier;

            // Ӧ�õȼ����η�
            baseDPS = ApplyAttributeTierMultiplier(baseDPS, attributeTier);

            // ���㹥���ٶȺ������˺�
            equipment.AttackSpeed = speedBase;
            equipment.WeaponDamage = (int)Math.Round(baseDPS / speedBase * damageModifier);

            // �����Լӳ�
            int mainAttributeBonus = (int)Math.Round(level * qualityMultiplier);
            mainAttributeBonus = (int)ApplyAttributeTierMultiplier(mainAttributeBonus, attributeTier);

            // �����������ͷ���������
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
        /// ���ɻ�������
        /// </summary>
        private static void GenerateArmorAttributes(
            Equipment equipment,
            int level,
            EquipmentQuality quality,
            AttributeTier attributeTier,
            ArmorType armorType)
        {
            // ȷ����������������ȷ
            if (armorType == ArmorType.None)
            {
                armorType = ArmorType.Leather;
            }
            equipment.ArmorType = armorType;

            // �����������ֵ
            double baseArmor = EquipmentAttributeConfig.BaseArmorValue *
                Math.Pow(EquipmentAttributeConfig.ArmorLevelMultiplier, level - 1);

            // Ӧ�û����������η�
            baseArmor *= EquipmentAttributeConfig.ArmorTypeModifiers[armorType];

            // Ӧ�ò�λ���η�
            baseArmor *= GetArmorSlotModifier(equipment.Slot);

            // Ӧ��Ʒ�����η�
            double qualityMultiplier = GetQualityMultiplier(quality) / EquipmentAttributeConfig.ArmorQualityDivisor;
            baseArmor *= qualityMultiplier;

            // Ӧ�õȼ����η�
            baseArmor = ApplyAttributeTierMultiplier(baseArmor, attributeTier);

            equipment.ArmorValue = (int)Math.Round(baseArmor);

            // ����������
            double totalMainAttributes = level * EquipmentAttributeConfig.QualityMainAttributeMultipliers[quality];
            double slotRatio = EquipmentAttributeConfig.SlotMainAttributeRatios[equipment.Slot];
            int mainAttributeValue = (int)Math.Round(totalMainAttributes * slotRatio);

            // Ӧ�õȼ����η�
            mainAttributeValue = (int)ApplyAttributeTierMultiplier(mainAttributeValue, attributeTier);

            // ���ݻ������ͷ���������
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

            // ���������л��׵Ĵ�Ҫ����
            equipment.AttributeBonuses.Stamina = (int)(mainAttributeValue * EquipmentAttributeConfig.ArmorStaminaRatio);

            // ��������ֵ�ӳ�
            equipment.HealthBonus = equipment.AttributeBonuses.Stamina * EquipmentAttributeConfig.StaminaToHealthRatio;
        }

        /// <summary>
        /// ������Ʒ����
        /// </summary>
        private static void GenerateJewelryAttributes(
            Equipment equipment,
            int level,
            EquipmentQuality quality,
            AttributeTier attributeTier,
            EquipmentSlot slot)
        {
            // ������������ֵ
            double totalMainAttributes = level * EquipmentAttributeConfig.QualityMainAttributeMultipliers[quality];
            double slotRatio = EquipmentAttributeConfig.SlotMainAttributeRatios[slot];
            int mainAttributeValue = (int)Math.Round(totalMainAttributes * slotRatio);

            // Ӧ�õȼ����η�
            mainAttributeValue = (int)ApplyAttributeTierMultiplier(mainAttributeValue, attributeTier);

            // ������Ʒ���ͷ���������
            switch (slot)
            {
                case EquipmentSlot.Neck:
                    // �������������������
                    int perStat = (int)(mainAttributeValue * EquipmentAttributeConfig.NecklaceEqualStatRatio);
                    equipment.AttributeBonuses.Strength = perStat;
                    equipment.AttributeBonuses.Agility = perStat;
                    equipment.AttributeBonuses.Intellect = mainAttributeValue - 2 * perStat;
                    equipment.AttributeBonuses.Stamina = (int)(perStat * EquipmentAttributeConfig.NecklaceStaminaRatio);
                    break;

                case EquipmentSlot.Finger1:
                case EquipmentSlot.Finger2:
                    // ��ָ��Ҫ��������������
                    int primary = (int)(mainAttributeValue * EquipmentAttributeConfig.RingPrimaryStatRatio);
                    int secondary = mainAttributeValue - primary;

                    // ���ѡ������������
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
                    // ��Ʒרע������Ч���������Խ���
                    int trinketMainStat = (int)(mainAttributeValue * EquipmentAttributeConfig.TrinketMainStatRatio);

                    // ���ѡ��һ��������
                    switch (Random.Next(4))
                    {
                        case 0: equipment.AttributeBonuses.Strength = trinketMainStat; break;
                        case 1: equipment.AttributeBonuses.Agility = trinketMainStat; break;
                        case 2: equipment.AttributeBonuses.Intellect = trinketMainStat; break;
                        case 3: equipment.AttributeBonuses.Spirit = trinketMainStat; break;
                    }

                    equipment.AttributeBonuses.Stamina = (int)(mainAttributeValue * EquipmentAttributeConfig.TrinketStaminaRatio);

                    // ��Ʒ�����⼸������
                    equipment.CriticalChanceBonus = EquipmentAttributeConfig.TrinketBaseCriticalChance +
                        (level * EquipmentAttributeConfig.TrinketCriticalChanceLevelBonus);
                    equipment.ExtraLootChanceBonus = EquipmentAttributeConfig.TrinketBaseExtraLootChance +
                        (level * EquipmentAttributeConfig.TrinketExtraLootChanceLevelBonus);
                    break;
            }
        }

        /// <summary>
        /// ���ɸ�����
        /// </summary>
        private static void GenerateSecondaryAttributes(
            Equipment equipment,
            int level,
            EquipmentQuality quality,
            AttributeTier attributeTier,
            List<string>? secondaryAttributePool)
        {
            // ȷ������������
            int secondaryCount = EquipmentAttributeConfig.QualitySecondaryAttributeCount[quality];
            if (secondaryCount <= 0) return;

            // ʹ��Ĭ�ϸ����Գػ��Զ����
            var attributePool = secondaryAttributePool ?? GetDefaultSecondaryAttributePool(equipment);
            if (!attributePool.Any()) return;

            // ���㸱���Ի���ֵ
            double baseSecondaryValue = level * EquipmentAttributeConfig.SecondaryAttributeBaseValueMultiplier;

            // ���ѡ��Ӧ�ø�����
            var selectedAttributes = new List<string>();

            for (int i = 0; i < secondaryCount && attributePool.Count > 0; i++)
            {
                // ���ѡ��һ��������
                int index = Random.Next(attributePool.Count);
                string attribute = attributePool[index];
                attributePool.RemoveAt(index);
                selectedAttributes.Add(attribute);

                // Ϊ���������ѡ��һ���ȼ�
                var randomTier = (AttributeTier)Random.Next(3);

                // ��������ֵ
                double attributeValue = baseSecondaryValue;
                attributeValue = ApplyAttributeTierMultiplier(attributeValue, randomTier);

                // Ӧ�ø�����
                ApplySecondaryAttribute(equipment, attribute, attributeValue, level);
            }
        }

        /// <summary>
        /// Ӧ�ø�����
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
                // ���������Ժ�Ԫ�ؿ���
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

        #region ��������
        /// <summary>
        /// ��ȡ���ײ�λ���η�
        /// </summary>
        private static double GetArmorSlotModifier(EquipmentSlot slot)
        {
            if (EquipmentAttributeConfig.ArmorSlotModifiers.TryGetValue(slot, out double modifier))
            {
                return modifier;
            }
            return 0.5; // Ĭ��ֵ
        }

        /// <summary>
        /// ��ȡƷ�ʱ���
        /// </summary>
        private static double GetQualityMultiplier(EquipmentQuality quality)
        {
            return EquipmentAttributeConfig.QualityMainAttributeMultipliers[quality];
        }

        /// <summary>
        /// Ӧ�����Եȼ�����
        /// </summary>
        private static double ApplyAttributeTierMultiplier(double value, AttributeTier tier)
        {
            var (min, max) = EquipmentAttributeConfig.AttributeTierMultipliers[tier];
            return value * (min + (Random.NextDouble() * (max - min)));
        }

        /// <summary>
        /// �ж��Ƿ�Ϊ������λ
        /// </summary>
        private static bool IsWeapon(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.MainHand || slot == EquipmentSlot.OffHand;
        }

        /// <summary>
        /// �ж��Ƿ�Ϊ���ײ�λ
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
        /// �ж��Ƿ�Ϊ��Ʒ��λ
        /// </summary>
        private static bool IsJewelry(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.Neck || slot == EquipmentSlot.Finger1 ||
                   slot == EquipmentSlot.Finger2 || slot == EquipmentSlot.Trinket1 ||
                   slot == EquipmentSlot.Trinket2;
        }

        /// <summary>
        /// �Ƿ�Ϊ�ض���������(˫������)
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
        /// ����Ԫ�ؿ���
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
        /// ��ȡĬ�ϸ����Գ�
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

            // ���ڷ�ս��װ������������/�ɼ�����
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
        /// Ӧ���Զ�������
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
                        // �����Զ�������...
                }
            }
        }

        /// <summary>
        /// ����װ������
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

            return $"{qualityDesc}{typeDesc}���ʺ�{level}��ð����ʹ�á�";
        }

        /// <summary>
        /// ����Ʒ����ɫǰ׺
        /// </summary>
        private static string AddQualityColor(string name, EquipmentQuality quality)
        {
            return EquipmentAttributeConfig.QualityPrefixes[quality] + name;
        }
        #endregion

        #region ����ʵ�÷���
        /// <summary>
        /// ��ȡװ���������ֵ�����ڳ��ۼ۸����
        /// </summary>
        public static int CalculateEquipmentValue(Equipment equipment)
        {
            int baseValue = equipment.RequiredLevel * EquipmentAttributeConfig.EquipmentBasePricePerLevel;

            // ����Ʒ�����Ӽ�ֵ
            if (equipment.Name.StartsWith("[ʷʫ]"))
                baseValue *= EquipmentAttributeConfig.QualityPriceMultipliers[EquipmentQuality.Epic];
            else if (equipment.Name.StartsWith("[ϡ��]"))
                baseValue *= EquipmentAttributeConfig.QualityPriceMultipliers[EquipmentQuality.Rare];
            else if (equipment.Name.StartsWith("[����]"))
                baseValue *= EquipmentAttributeConfig.QualityPriceMultipliers[EquipmentQuality.Uncommon];

            // �����ͻ����ж����ֵ
            if (equipment.WeaponDamage > 0)
            {
                baseValue += equipment.WeaponDamage * EquipmentAttributeConfig.WeaponDamagePriceMultiplier;
            }

            if (equipment.ArmorValue > 0)
            {
                baseValue += equipment.ArmorValue * EquipmentAttributeConfig.ArmorValuePriceMultiplier;
            }

            // ���������Ӽ�ֵ
            baseValue += equipment.AttributeBonuses.Strength * EquipmentAttributeConfig.MainAttributePriceMultiplier;
            baseValue += equipment.AttributeBonuses.Agility * EquipmentAttributeConfig.MainAttributePriceMultiplier;
            baseValue += equipment.AttributeBonuses.Intellect * EquipmentAttributeConfig.MainAttributePriceMultiplier;
            baseValue += equipment.AttributeBonuses.Spirit * EquipmentAttributeConfig.MainAttributePriceMultiplier;
            baseValue += equipment.AttributeBonuses.Stamina * EquipmentAttributeConfig.MainAttributePriceMultiplier;

            // ���������������Ӽ�ֵ
            baseValue += equipment.HealthBonus / EquipmentAttributeConfig.HealthBonusPriceDivisor;
            baseValue += equipment.AttackBonus * EquipmentAttributeConfig.AttackBonusPriceMultiplier;
            baseValue += (int)(equipment.CriticalChanceBonus * EquipmentAttributeConfig.CriticalChancePriceMultiplier);
            baseValue += (int)(equipment.CriticalDamageBonus * EquipmentAttributeConfig.CriticalDamagePriceMultiplier);

            return baseValue;
        }

        /// <summary>
        /// �������Ʋ²�װ������������
        /// </summary>
        public static WeaponType GuessWeaponTypeFromName(string name)
        {
            name = name.ToLower();

            if (name.Contains("��")) return name.Contains("˫��") ? WeaponType.TwoHandSword : WeaponType.Sword;
            if (name.Contains("ذ��") || name.Contains("�̽�")) return WeaponType.Dagger;
            if (name.Contains("��")) return name.Contains("˫��") ? WeaponType.TwoHandAxe : WeaponType.Axe;
            if (name.Contains("��") || name.Contains("�")) return name.Contains("˫��") ? WeaponType.TwoHandMace : WeaponType.Mace;
            if (name.Contains("����") || name.Contains("ħ��")) return name.Contains("����") ? WeaponType.Staff : WeaponType.Wand;
            if (name.Contains("��")) return name.Contains("��") ? WeaponType.Crossbow : WeaponType.Bow;
            if (name.Contains("ǹ")) return WeaponType.Gun;
            if (name.Contains("��") || name.Contains("shield")) return WeaponType.Shield;
            if (name.Contains("����") || name.Contains("ì") || name.Contains("ǹ")) return WeaponType.Polearm;

            // Ĭ��Ϊ��
            return WeaponType.Sword;
        }

        /// <summary>
        /// �������Ʋ²�װ���Ļ�������
        /// </summary>
        public static ArmorType GuessArmorTypeFromName(string name)
        {
            name = name.ToLower();

            if (name.Contains("��") || name.Contains("��") || name.Contains("��ʦ")) return ArmorType.Cloth;
            if (name.Contains("Ƥ") || name.Contains("����")) return ArmorType.Leather;
            if (name.Contains("��") || name.Contains("��") || name.Contains("����")) return ArmorType.Mail;
            if (name.Contains("��") || name.Contains("սʿ")) return ArmorType.Plate;

            // Ĭ��ΪƤ��
            return ArmorType.Leather;
        }
        #endregion
    }
}