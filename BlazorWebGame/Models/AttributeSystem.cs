using System.Collections.Generic;

namespace BlazorWebGame.Models
{
    /// <summary>
    /// ��ɫ������������
    /// </summary>
    public enum AttributeType
    {
        Strength,    // ������Ӱ����������
        Agility,     // ���ݣ�Ӱ�������ʺ�����
        Intellect,   // ������Ӱ�취��������
        Spirit,      // ����Ӱ��ظ�����
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
            return profession switch
            {
                BattleProfession.Warrior => new AttributeSet
                {
                    Strength = 8,  // սʿ������
                    Agility = 6,
                    Intellect = 3, // ������
                    Spirit = 4,
                    Stamina = 7    // ������
                },
                BattleProfession.Mage => new AttributeSet
                {
                    Strength = 3,  // ������
                    Agility = 4,
                    Intellect = 8, // ��ʦ������
                    Spirit = 7,    // �����
                    Stamina = 5
                },
                _ => new AttributeSet()
            };
        }
        
        // ��ȼ�������ȡ��������
        public static AttributeSet GetLevelUpAttributes(BattleProfession profession)
        {
            return profession switch
            {
                BattleProfession.Warrior => new AttributeSet
                {
                    Strength = 2,  // ÿ��+2����
                    Agility = 1,
                    Intellect = 0,
                    Spirit = 0,
                    Stamina = 1    // ÿ��+1����
                },
                BattleProfession.Mage => new AttributeSet
                {
                    Strength = 0,
                    Agility = 0,
                    Intellect = 2, // ÿ��+2����
                    Spirit = 1,
                    Stamina = 1
                },
                _ => new AttributeSet()
            };
        }
    }
}