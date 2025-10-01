using BlazorWebGame.GameConfig;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Shared.DTOs;
using System.Linq;

namespace BlazorWebGame.Server.Services.Character
{
    /// <summary>
    /// 服务端玩家属性管理服务
    /// </summary>
    public class ServerPlayerAttributeService
    {
        private readonly ILogger<ServerPlayerAttributeService> _logger;

        public ServerPlayerAttributeService(ILogger<ServerPlayerAttributeService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取角色的总属性值（基础+装备+buff）
        /// </summary>
        public AttributeSetDto GetTotalAttributes(CharacterDetailsDto character)
        {
            var baseAttributes = ConvertToDto(character.BaseAttributes) ?? new AttributeSetDto();
            var total = baseAttributes.Clone();

            // 添加装备属性加成
            foreach (var itemId in character.EquippedItems?.Values ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrEmpty(itemId)) continue;

                var item = ItemData.GetItemById(itemId) as Equipment;
                if (item?.AttributeBonuses != null)
                {
                    total.Add(ConvertToDto(item.AttributeBonuses));
                }
            }

            // TODO: 添加buff属性加成，等待相关系统

            return total;
        }

        /// <summary>
        /// 获取当前职业的主属性值
        /// </summary>
        public int GetPrimaryAttributeValue(CharacterDetailsDto character)
        {
            var profession = Enum.TryParse<BattleProfession>(character.SelectedBattleProfession, out var p) ? p : BattleProfession.Warrior;
            var primaryAttr = ProfessionAttributes.GetPrimaryAttribute(profession);
            var attrs = GetTotalAttributes(character);

            return primaryAttr switch
            {
                AttributeType.Strength => attrs.Strength,
                AttributeType.Agility => attrs.Agility,
                AttributeType.Intellect => attrs.Intellect,
                AttributeType.Spirit => attrs.Spirit,
                AttributeType.Stamina => attrs.Stamina,
                _ => 0
            };
        }

        /// <summary>
        /// 更新角色基础属性，应在角色升级或职业时调用
        /// </summary>
        public void UpdateBaseAttributes(CharacterDetailsDto character)
        {
            var profession = Enum.TryParse<BattleProfession>(character.SelectedBattleProfession, out var p) ? p : BattleProfession.Warrior;

            if (character.BaseAttributes == null)
            {
                character.BaseAttributes = new AttributeSetDto();
            }

            // 获取职业初始属性
            var initialAttrs = ProfessionAttributes.GetInitialAttributes(profession);

            // 获取当前等级
            int level = GetLevel(character, profession);

            // 设置基础属性 = 初始属性
            character.BaseAttributes = ConvertToDto(initialAttrs);

            // 累计每级成长
            for (int i = 1; i < level; i++)
            {
                int currentLevel = i + 1; // 从第2级开始计算升级成长
                var levelUpAttrs = ProfessionAttributes.GetLevelUpAttributesForLevel(profession, currentLevel);
                character.BaseAttributes.Add(ConvertToDto(levelUpAttrs));
            }
        }

        /// <summary>
        /// 初始化角色属性
        /// </summary>
        public void InitializePlayerAttributes(CharacterDetailsDto character)
        {
            if (character == null) return;

            // 确保基础属性已初始化
            if (character.BaseAttributes == null)
            {
                character.BaseAttributes = new AttributeSetDto();
            }

            // 更新基础属性
            UpdateBaseAttributes(character);

            // 设置生命值为最大值
            character.MaxHealth = GetTotalMaxHealth(character);
            character.Health = character.MaxHealth;
        }

        /// <summary>
        /// 获取总攻击力
        /// </summary>
        public int GetTotalAttackPower(CharacterDetailsDto character)
        {
            var attrs = GetTotalAttributes(character);

            // 获取主属性值
            int primaryAttrValue = GetPrimaryAttributeValue(character);

            // 使用配置中的主属性到攻击力转换系数
            double apRatio = AttributeSystemConfig.MainAttributeToAPRatio;
            int primaryAttrBonus = (int)(primaryAttrValue * apRatio);

            // 添加装备加成
            var equipmentAttack = (character.EquippedItems?.Values ?? Enumerable.Empty<string>())
                .Select(itemId => ItemData.GetItemById(itemId) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.AttackBonus);

            // TODO: 添加buff攻击力加成，等待相关系统

            return primaryAttrBonus + equipmentAttack;
        }

        /// <summary>
        /// 获取伤害倍数
        /// </summary>
        public double GetDamageMultiplier(CharacterDetailsDto character)
        {
            // 获取主属性值
            int primaryAttrValue = GetPrimaryAttributeValue(character);

            // 使用配置中的主属性到伤害倍数系数
            double damageMultiplier = 1.0 + primaryAttrValue * AttributeSystemConfig.MainAttributeToDamageMultiplier;

            return damageMultiplier;
        }

        /// <summary>
        /// 获取总生命值上限
        /// </summary>
        public int GetTotalMaxHealth(CharacterDetailsDto character)
        {
            var attrs = GetTotalAttributes(character);

            // 使用配置中的基础生命值
            int baseHealth = AttributeSystemConfig.BaseHealth;

            // 使用配置中的体力转换系数
            double staminaRatio = AttributeSystemConfig.StaminaToHealthRatio;
            var staminaBonus = (int)(attrs.Stamina * staminaRatio);

            // 添加装备加成
            var equipmentHealth = (character.EquippedItems?.Values ?? Enumerable.Empty<string>())
                .Select(itemId => ItemData.GetItemById(itemId) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.HealthBonus);

            // TODO: 添加buff生命值加成，等待相关系统

            return baseHealth + staminaBonus + equipmentHealth;
        }

        /// <summary>
        /// 获取总精确度
        /// </summary>
        public int GetTotalAccuracy(CharacterDetailsDto character)
        {
            // 获取主属性值
            var attrs = GetTotalAttributes(character);

            // 主属性加成
            int primaryAttrBonus = GetPrimaryAttributeValue(character) / 2;

            // 装备直接精确度加成
            var equipmentAccuracy = (character.EquippedItems?.Values ?? Enumerable.Empty<string>())
                .Select(itemId => ItemData.GetItemById(itemId) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.AccuracyBonus);

            // TODO: 添加buff精确度加成，等待相关系统

            return primaryAttrBonus + equipmentAccuracy;
        }

        /// <summary>
        /// 获取等级（从角色数据计算）
        /// </summary>
        private int GetLevel(CharacterDetailsDto character, BattleProfession profession)
        {
            var professionName = profession.ToString();
            int xp = character.BattleProfessionXP?.GetValueOrDefault(professionName, 0) ?? 0;
            return ExpSystem.GetLevelFromExp(xp);
        }

        /// <summary>
        /// 将客户端AttributeSet转换为DTO
        /// </summary>
        private AttributeSetDto ConvertToDto(AttributeSet? attributeSet)
        {
            if (attributeSet == null)
                return new AttributeSetDto();

            return new AttributeSetDto
            {
                Strength = attributeSet.Strength,
                Agility = attributeSet.Agility,
                Intellect = attributeSet.Intellect,
                Spirit = attributeSet.Spirit,
                Stamina = attributeSet.Stamina
            };
        }

        /// <summary>
        /// 将DTO的AttributeSetDto转换为AttributeSetDto（避免混合类型）
        /// </summary>
        private AttributeSetDto ConvertToDto(AttributeSetDto? attributeSet)
        {
            return attributeSet?.Clone() ?? new AttributeSetDto();
        }
    }
}