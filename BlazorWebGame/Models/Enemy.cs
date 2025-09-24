using BlazorWebGame.Models.Monsters;
using System.Collections.Generic;

namespace BlazorWebGame.Models
{
    public class Enemy
    {
        public double EnemyAttackCooldown { get; set; }
        public string Name { get; set; } = "δ֪����";
        public string Description { get; set; } = "";
        
        // ��Ĭ��ֵ��Ϊ0���Ա���������ݵȼ�����ʵ��ֵ
        public int Health { get; set; } = 0;
        public int MaxHealth { get; set; } = 0;
        public int AttackPower { get; set; } = 0;
        public double AttacksPerSecond { get; set; } = 0;
        public int XpReward { get; set; } = 0;
        public int MinGold { get; set; } = 0;
        public int MaxGold { get; set; } = 0;
        public int Level { get; set; } = 1;

        // ��Ʒ��ֵ����
        public double ItemValue { get; set; } = 0;

        // ������������
        public MonsterType Type { get; set; } = MonsterType.Normal;
        public MonsterRace Race { get; set; } = MonsterRace.Humanoid;
        
        // ����ϵͳ���
        public int AvoidanceRating { get; set; } = 0;
        public double DodgeChance { get; set; } = 0.0;
        
        // ����ϵͳ���
        public int AccuracyRating { get; set; } = 0;
        
        // ����ϵͳ
        public double CriticalChance { get; set; } = 0.05;
        public double CriticalMultiplier { get; set; } = 1.5;
        
        // Ԫ��ϵͳ
        public ElementType ElementType { get; set; } = ElementType.None;
        public Dictionary<ElementType, double> ElementalResistances { get; set; } = new();
        
        // ��������
        public List<string> SkillIds { get; set; } = new();
        public Dictionary<string, int> SkillCooldowns { get; set; } = new();
        public Dictionary<string, double> LootTable { get; set; } = new();

        public int GetGoldDropAmount()
        {
            return new System.Random().Next(MinGold, MaxGold + 1);
        }

        public Enemy Clone()
        {
            var clone = (Enemy)this.MemberwiseClone();
            // Ϊ�������ʹ����µ�ʵ��
            clone.SkillIds = new List<string>(this.SkillIds);
            clone.SkillCooldowns = new Dictionary<string, int>(this.SkillCooldowns);
            clone.LootTable = new Dictionary<string, double>(this.LootTable);
            clone.ElementalResistances = new Dictionary<ElementType, double>(this.ElementalResistances);
            return clone;
        }
    }
}