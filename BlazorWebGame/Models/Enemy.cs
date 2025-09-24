using BlazorWebGame.Models.Monsters;
using System.Collections.Generic;

namespace BlazorWebGame.Models
{
    public class Enemy
    {
        public double EnemyAttackCooldown { get; set; }
        public string Name { get; set; } = "未知生物";
        public string Description { get; set; } = ""; // 新增描述属性
        public int Health { get; set; } = 50;
        public int MaxHealth { get; set; } = 50;
        public int AttackPower { get; set; } = 5;
        public double AttacksPerSecond { get; set; } = 0.8;
        public int XpReward { get; set; } = 10;
        public int MinGold { get; set; } = 1;
        public int MaxGold { get; set; } = 5;
        public int Level { get; set; } = 1;

        // 怪物类型属性
        public MonsterType Type { get; set; } = MonsterType.Normal;
        public MonsterRace Race { get; set; } = MonsterRace.Humanoid;
        
        // 闪避系统相关
        public int AvoidanceRating { get; set; } = 0;  // 闪避值，越高越难被命中
        public double DodgeChance { get; set; } = 0.0; // 0-1之间，直接躲避攻击的几率
        
        // 命中系统相关
        public int AccuracyRating { get; set; } = 0;   // 命中等级，影响命中率
        
        // 暴击系统
        public double CriticalChance { get; set; } = 0.05;  // 暴击几率，默认5%
        public double CriticalMultiplier { get; set; } = 1.5;  // 暴击伤害倍率，默认150%
        
        // 元素系统
        public ElementType ElementType { get; set; } = ElementType.None;  // 怪物的元素类型
        public Dictionary<ElementType, double> ElementalResistances { get; set; } = new();  // 元素抗性
        
        // 现有属性
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
            // 为引用类型创建新的实例
            clone.SkillIds = new List<string>(this.SkillIds);
            clone.SkillCooldowns = new Dictionary<string, int>(this.SkillCooldowns);
            clone.LootTable = new Dictionary<string, double>(this.LootTable);
            clone.ElementalResistances = new Dictionary<ElementType, double>(this.ElementalResistances);
            return clone;
        }
    }
}