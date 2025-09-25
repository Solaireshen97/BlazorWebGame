using System.Collections.Generic;
using BlazorWebGame.GameConfig;

namespace BlazorWebGame.Models
{
    /// <summary>
    /// ��ɫ������������
    /// </summary>
    public enum AttributeType
    {
        Strength,    // ����
        Agility,     // ����
        Intellect,   // ����
        Spirit,      // ����
        Stamina      // ������Ӱ������ֵ
    }
    
    /// <summary>
    /// ��ɫ���Լ����������л�������
    /// </summary>
    public class AttributeSet
    {
        public int Strength { get; set; } = 5;     // ��������
        public int Agility { get; set; } = 5;      // ��������  
        public int Intellect { get; set; } = 5;    // ��������
        public int Spirit { get; set; } = 5;       // ��������
        public int Stamina { get; set; } = 5;      // ��������
        
        // ��¡���������ڴ������Լ��ĸ���
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
        
        // ����һ�����Լ���ֵ�ӵ���ǰ���Լ���
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
    /// ְҵ�������ã����岻ְͬҵ�������Ժͳ�ʼ���Է���
    /// </summary>
    public static class ProfessionAttributes
    {
        // ��ȡְҵ��������
        public static AttributeType GetPrimaryAttribute(BattleProfession profession)
        {
            return profession switch
            {
                BattleProfession.Warrior => AttributeType.Strength,
                BattleProfession.Mage => AttributeType.Intellect,
                _ => AttributeType.Strength
            };
        }
        
        // ��ȡְҵ�ĳ�ʼ���Է���
        public static AttributeSet GetInitialAttributes(BattleProfession profession)
        {
            // ʹ�������еĻ��������Ժ�����ֵ
            int baseMainAttr = AttributeSystemConfig.BaseMainAttribute;
            int baseStamina = AttributeSystemConfig.BaseStamina;
            
            return profession switch
            {
                BattleProfession.Warrior => new AttributeSet
                {
                    Strength = baseMainAttr,  // սʿ������
                    Agility = baseMainAttr - 4,
                    Intellect = baseMainAttr - 7, // ������
                    Spirit = baseMainAttr - 6,
                    Stamina = baseStamina    // ��������
                },
                BattleProfession.Mage => new AttributeSet
                {
                    Strength = baseMainAttr - 7,  // ������
                    Agility = baseMainAttr - 6,
                    Intellect = baseMainAttr, // ��ʦ������
                    Spirit = baseMainAttr - 3,    // �����
                    Stamina = baseStamina - 2 // ��ʦ�����Ե�
                },
                _ => new AttributeSet()
            };
        }
        
        // ��ȡ������ȼ�����ֵ
        public static AttributeSet GetLevelUpAttributes(BattleProfession profession)
        {
            // ��ȡ���Գɳ�ϵ��
            int lowLevelMainAttr = AttributeSystemConfig.LowLevelMainAttributeGrowth;
            int lowLevelStamina = AttributeSystemConfig.LowLevelStaminaGrowth;
            int highLevelMainAttr = AttributeSystemConfig.HighLevelMainAttributeGrowth;
            int highLevelStamina = AttributeSystemConfig.HighLevelStaminaGrowth;
            
            // ȡ�õ�ǰ�㼶�ĳɳ�ֵ
            int mainAttrGrowth = lowLevelMainAttr; // Ĭ��ʹ�õ͵ȼ��ɳ�
            int staminaGrowth = lowLevelStamina;
            
            return profession switch
            {
                BattleProfession.Warrior => new AttributeSet
                {
                    Strength = mainAttrGrowth,  // �����Գɳ�
                    Agility = mainAttrGrowth / 2,
                    Intellect = 0,
                    Spirit = 0,
                    Stamina = staminaGrowth    // �����ɳ�
                },
                BattleProfession.Mage => new AttributeSet
                {
                    Strength = 0,
                    Agility = 0,
                    Intellect = mainAttrGrowth, // �����Գɳ�
                    Spirit = mainAttrGrowth / 2,
                    Stamina = staminaGrowth
                },
                _ => new AttributeSet()
            };
        }
        
        /// <summary>
        /// ��ȡ�ض��ȼ������Եĳɳ�ֵ
        /// </summary>
        public static AttributeSet GetLevelUpAttributesForLevel(BattleProfession profession, int level)
        {
            var baseAttrs = GetLevelUpAttributes(profession);
            
            // ���ݵȼ�Ӧ�ò�ͬ�ɳ���
            if (level > AttributeSystemConfig.LevelThreshold)
            {
                // �ߵȼ�ʱ���Գɳ�����
                float growthMultiplier = AttributeSystemConfig.HighLevelMainAttributeGrowth / (float)AttributeSystemConfig.LowLevelMainAttributeGrowth;
                
                // Ӧ�øߵȼ��ɳ���
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