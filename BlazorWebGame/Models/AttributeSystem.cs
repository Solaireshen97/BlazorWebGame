using System.Collections.Generic;

namespace BlazorWebGame.Models
{
    /// <summary>
    /// 角色基础属性类型
    /// </summary>
    public enum AttributeType
    {
        Strength,    // 力量：影响物理攻击力
        Agility,     // 敏捷：影响命中率和闪避
        Intellect,   // 智力：影响法术攻击力
        Spirit,      // 精神：影响回复能力
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
            return profession switch
            {
                BattleProfession.Warrior => new AttributeSet
                {
                    Strength = 8,  // 战士力量高
                    Agility = 6,
                    Intellect = 3, // 低智力
                    Spirit = 4,
                    Stamina = 7    // 高耐力
                },
                BattleProfession.Mage => new AttributeSet
                {
                    Strength = 3,  // 低力量
                    Agility = 4,
                    Intellect = 8, // 法师智力高
                    Spirit = 7,    // 精神高
                    Stamina = 5
                },
                _ => new AttributeSet()
            };
        }
        
        // 随等级增长获取属性提升
        public static AttributeSet GetLevelUpAttributes(BattleProfession profession)
        {
            return profession switch
            {
                BattleProfession.Warrior => new AttributeSet
                {
                    Strength = 2,  // 每级+2力量
                    Agility = 1,
                    Intellect = 0,
                    Spirit = 0,
                    Stamina = 1    // 每级+1耐力
                },
                BattleProfession.Mage => new AttributeSet
                {
                    Strength = 0,
                    Agility = 0,
                    Intellect = 2, // 每级+2智力
                    Spirit = 1,
                    Stamina = 1
                },
                _ => new AttributeSet()
            };
        }
    }
}