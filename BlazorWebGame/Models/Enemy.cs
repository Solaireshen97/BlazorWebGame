using BlazorWebGame.Models.Monsters;
using System.Collections.Generic;

namespace BlazorWebGame.Models
{
    public class Enemy
    {
        public double EnemyAttackCooldown { get; set; }
        public string Name { get; set; } = "δ֪����";
        public string Description { get; set; } = ""; // ������������
        public int Health { get; set; } = 50;
        public int MaxHealth { get; set; } = 50;
        public int AttackPower { get; set; } = 5;
        public double AttacksPerSecond { get; set; } = 0.8;
        public int XpReward { get; set; } = 10;
        public int MinGold { get; set; } = 1;
        public int MaxGold { get; set; } = 5;
        public int Level { get; set; } = 1;

        // ������������
        public MonsterType Type { get; set; } = MonsterType.Normal;
        public MonsterRace Race { get; set; } = MonsterRace.Humanoid;
        
        // ����ϵͳ���
        public int AvoidanceRating { get; set; } = 0;  // ����ֵ��Խ��Խ�ѱ�����
        public double DodgeChance { get; set; } = 0.0; // 0-1֮�䣬ֱ�Ӷ�ܹ����ļ���
        
        // ����ϵͳ���
        public int AccuracyRating { get; set; } = 0;   // ���еȼ���Ӱ��������
        
        // ����ϵͳ
        public double CriticalChance { get; set; } = 0.05;  // �������ʣ�Ĭ��5%
        public double CriticalMultiplier { get; set; } = 1.5;  // �����˺����ʣ�Ĭ��150%
        
        // Ԫ��ϵͳ
        public ElementType ElementType { get; set; } = ElementType.None;  // �����Ԫ������
        public Dictionary<ElementType, double> ElementalResistances { get; set; } = new();  // Ԫ�ؿ���
        
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