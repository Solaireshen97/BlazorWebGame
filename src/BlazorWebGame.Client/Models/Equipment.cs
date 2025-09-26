// This file now extends the shared Equipment class with client-specific display methods
using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Models.Monsters;
using System.Text;

namespace BlazorWebGame.Models
{
    // Re-export shared enums for backward compatibility
    using EquipmentSlot = BlazorWebGame.Shared.Enums.EquipmentSlot;
    using ArmorType = BlazorWebGame.Shared.Enums.ArmorType;
    using WeaponType = BlazorWebGame.Shared.Enums.WeaponType;

    /// <summary>
    /// Client-specific Equipment class that extends the shared Equipment with display methods
    /// </summary>
    public class Equipment : BlazorWebGame.Shared.Models.Items.Equipment
    {
        // Inherit all properties from the shared Equipment class

        /// <summary>
        /// Override the base GetStatsDescription to provide localized display
        /// </summary>
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