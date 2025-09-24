using System.Collections.Generic;
using BlazorWebGame.GameConfig;

namespace BlazorWebGame.Models
{
    /// <summary>
    /// 角色基础属性类型
    /// </summary>
    public enum AttributeType
    {
        Strength,    // 力量
        Agility,     // 敏捷
        Intellect,   // 智力
        Spirit,      // 精神
        Stamina      // 耐力：影响生命值
    }
    
    /// <summary>
    /// 角色属性集，包含所有基础属性
    /// </summary>
    public class AttributeSet
    {
        public int Strength { get; set; } = 5;     // 基础力量
        public int Agility { get; set; } = 5;      // 基础敏捷  
        public int Intellect { get; set; } = 5;    // 基础智力
        public int Spirit { get; set; } = 5;       // 基础精神
        public int Stamina { get; set; } = 5;      // 基础耐力
        
        // 克隆方法，用于处理属性集的复制
        public AttributeSet Clone()
        {
            return new AttributeSet
            {
                Strength = this.Strength,
                Agility = this.Agility,
                Intellect = this.Intellect,
                Spirit = this.Spirit,
                Stamina = this.Stamina
            };
        }
        
        // 将另一个属性集的值加到当前属性集上
        public void Add(AttributeSet other)
        {
            if (other == null) return;
            
            Strength += other.Strength;
            Agility += other.Agility;
            Intellect += other.Intellect;
            Spirit += other.Spirit;
            Stamina += other.Stamina;
        }
    }
    
    /// <summary>
    /// 职业属性配置，定义不同职业的主属性和初始属性分配
    /// </summary>
    public static class ProfessionAttributes
    {
        // 获取职业的主属性
        public static AttributeType GetPrimaryAttribute(BattleProfession profession)
        {
            return profession switch
            {
                BattleProfession.Warrior => AttributeType.Strength,
                BattleProfession.Mage => AttributeType.Intellect,
                _ => AttributeType.Strength
            };
        }
        
        // 获取职业的初始属性分配
        public static AttributeSet GetInitialAttributes(BattleProfession profession)
        {
            // 使用配置中的基础主属性和耐力值
            int baseMainAttr = AttributeSystemConfig.BaseMainAttribute;
            int baseStamina = AttributeSystemConfig.BaseStamina;
            
            return profession switch
            {
                BattleProfession.Warrior => new AttributeSet
                {
                    Strength = baseMainAttr,  // 战士力量高
                    Agility = baseMainAttr - 4,
                    Intellect = baseMainAttr - 7, // 低智力
                    Spirit = baseMainAttr - 6,
                    Stamina = baseStamina    // 基础耐力
                },
                BattleProfession.Mage => new AttributeSet
                {
                    Strength = baseMainAttr - 7,  // 低力量
                    Agility = baseMainAttr - 6,
                    Intellect = baseMainAttr, // 法师智力高
                    Spirit = baseMainAttr - 3,    // 精神高
                    Stamina = baseStamina - 2 // 法师耐力略低
                },
                _ => new AttributeSet()
            };
        }
        
        // 获取属性随等级增长值
        public static AttributeSet GetLevelUpAttributes(BattleProfession profession)
        {
            // 获取属性成长系数
            int lowLevelMainAttr = AttributeSystemConfig.LowLevelMainAttributeGrowth;
            int lowLevelStamina = AttributeSystemConfig.LowLevelStaminaGrowth;
            int highLevelMainAttr = AttributeSystemConfig.HighLevelMainAttributeGrowth;
            int highLevelStamina = AttributeSystemConfig.HighLevelStaminaGrowth;
            
            // 取得当前层级的成长值
            int mainAttrGrowth = lowLevelMainAttr; // 默认使用低等级成长
            int staminaGrowth = lowLevelStamina;
            
            return profession switch
            {
                BattleProfession.Warrior => new AttributeSet
                {
                    Strength = mainAttrGrowth,  // 主属性成长
                    Agility = mainAttrGrowth / 2,
                    Intellect = 0,
                    Spirit = 0,
                    Stamina = staminaGrowth    // 耐力成长
                },
                BattleProfession.Mage => new AttributeSet
                {
                    Strength = 0,
                    Agility = 0,
                    Intellect = mainAttrGrowth, // 主属性成长
                    Spirit = mainAttrGrowth / 2,
                    Stamina = staminaGrowth
                },
                _ => new AttributeSet()
            };
        }
        
        /// <summary>
        /// 获取特定等级下属性的成长值
        /// </summary>
        public static AttributeSet GetLevelUpAttributesForLevel(BattleProfession profession, int level)
        {
            var baseAttrs = GetLevelUpAttributes(profession);
            
            // 根据等级应用不同成长率
            if (level > AttributeSystemConfig.LevelThreshold)
            {
                // 高等级时属性成长更快
                float growthMultiplier = AttributeSystemConfig.HighLevelMainAttributeGrowth / (float)AttributeSystemConfig.LowLevelMainAttributeGrowth;
                
                // 应用高等级成长率
                baseAttrs.Strength = (int)(baseAttrs.Strength * growthMultiplier);
                baseAttrs.Agility = (int)(baseAttrs.Agility * growthMultiplier);
                baseAttrs.Intellect = (int)(baseAttrs.Intellect * growthMultiplier);
                baseAttrs.Spirit = (int)(baseAttrs.Spirit * growthMultiplier);
                baseAttrs.Stamina = (int)(baseAttrs.Stamina * growthMultiplier);
            }
            
            return baseAttrs;
        }
    }
}