using BlazorWebGame.GameConfig;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using System.Linq;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Services.PlayerServices;

/// <summary>
/// 玩家属性管理服务实现
/// </summary>
public class PlayerAttributeService : IPlayerAttributeService
{
    public AttributeSet GetTotalAttributes(Models.Player player)
    {
        if (player.BaseAttributes == null)
        {
            player.BaseAttributes = new AttributeSet();
        }
        
        // 获取基础属性
        var total = player.BaseAttributes.Clone();
        
        // 添加装备属性加成
        foreach (var itemId in player.EquippedItems.Values)
        {
            if (string.IsNullOrEmpty(itemId)) continue;
            
            var item = ItemData.GetItemById(itemId) as Equipment;
            if (item?.AttributeBonuses != null)
            {
                total.Add(item.AttributeBonuses);
            }
        }
        
        // TODO: 添加buff属性加成，等待相关系统
        
        return total;
    }

    public int GetPrimaryAttributeValue(Models.Player player)
    {
        var primaryAttr = ProfessionAttributes.GetPrimaryAttribute(player.SelectedBattleProfession);
        var attrs = GetTotalAttributes(player);
        
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

    public void UpdateBaseAttributes(Models.Player player)
    {
        if (player.BaseAttributes == null)
        {
            player.BaseAttributes = new AttributeSet();
        }

        // 获取职业初始属性
        var initialAttrs = ProfessionAttributes.GetInitialAttributes(player.SelectedBattleProfession);

        // 获取当前等级
        int level = player.GetLevel(player.SelectedBattleProfession);

        // 设置基础属性 = 初始属性
        player.BaseAttributes = initialAttrs.Clone();

        // 累计每级成长
        for (int i = 1; i < level; i++)
        {
            int currentLevel = i + 1; // 从第2级开始计算升级成长
            var levelUpAttrs = ProfessionAttributes.GetLevelUpAttributesForLevel(player.SelectedBattleProfession, currentLevel);
            player.BaseAttributes.Add(levelUpAttrs);
        }
    }

    public void InitializePlayerAttributes(Models.Player player)
    {
        if (player == null) return;

        // 确保基础属性已初始化
        if (player.BaseAttributes == null)
        {
            player.BaseAttributes = new AttributeSet();
        }

        // 更新基础属性
        UpdateBaseAttributes(player);

        // 设置生命值为最大值
        player.MaxHealth = GetTotalMaxHealth(player);
        player.Health = player.MaxHealth;
    }

    public int GetTotalAttackPower(Models.Player player)
    {
        var attrs = GetTotalAttributes(player);

        // 获取主属性值
        int primaryAttrValue = GetPrimaryAttributeValue(player);

        // 使用配置中的主属性到攻击力转换系数
        double apRatio = AttributeSystemConfig.MainAttributeToAPRatio;
        int primaryAttrBonus = (int)(primaryAttrValue * apRatio);

        // 添加装备和buff加成
        var equipmentAttack = player.EquippedItems
            .Select(kv => ItemData.GetItemById(kv.Value) as Equipment)
            .Where(eq => eq != null)
            .Sum(eq => eq!.AttackBonus);

        var buffAttack = player.ActiveBuffs
            .Where(b => b.BuffType == StatBuffType.AttackPower)
            .Sum(b => b.BuffValue);

        return primaryAttrBonus + equipmentAttack + (int)buffAttack;
    }

    public double GetDamageMultiplier(Models.Player player)
    {
        // 获取主属性值
        int primaryAttrValue = GetPrimaryAttributeValue(player);

        // 使用配置中的主属性到伤害倍数系数
        double damageMultiplier = 1.0 + (primaryAttrValue * AttributeSystemConfig.MainAttributeToDamageMultiplier);

        return damageMultiplier;
    }

    public int GetTotalMaxHealth(Models.Player player)
    {
        var attrs = GetTotalAttributes(player);

        // 使用配置中的基础生命值
        int baseHealth = AttributeSystemConfig.BaseHealth;

        // 使用配置中的体力转换系数
        double staminaRatio = AttributeSystemConfig.StaminaToHealthRatio;
        var staminaBonus = (int)(attrs.Stamina * staminaRatio);

        // 添加装备和buff加成
        var equipmentHealth = player.EquippedItems
            .Select(kv => ItemData.GetItemById(kv.Value) as Equipment)
            .Where(eq => eq != null)
            .Sum(eq => eq!.HealthBonus);

        var buffHealth = player.ActiveBuffs
            .Where(b => b.BuffType == StatBuffType.MaxHealth)
            .Sum(b => b.BuffValue);

        return baseHealth + staminaBonus + equipmentHealth + (int)buffHealth;
    }

    public int GetTotalAccuracy(Models.Player player)
    {
        // 获取主属性值
        var attrs = GetTotalAttributes(player);

        // 主属性加成
        int primaryAttrBonus = GetPrimaryAttributeValue(player) / 2;

        // 装备直接精确度加成
        var equipmentAccuracy = player.EquippedItems.Values
            .Select(itemId => ItemData.GetItemById(itemId) as Equipment)
            .Where(eq => eq != null)
            .Sum(eq => eq!.AccuracyBonus);

        // TODO: 添加buff精确度加成，等待相关系统

        return primaryAttrBonus + equipmentAccuracy;
    }
}