using BlazorWebGame.Models.Monsters;
using System.Collections.Generic;

namespace BlazorWebGame.Models
{
    public class Enemy
    {
        public double EnemyAttackCooldown { get; set; }
        public string Name { get; set; } = "未知生物";
        public string Description { get; set; } = "";
        
        // 将默认值设为0，以便计算器根据等级计算实际值
        public int Health { get; set; } = 0;
        public int MaxHealth { get; set; } = 0;
        public int AttackPower { get; set; } = 0;
        public double AttacksPerSecond { get; set; } = 0;
        public int XpReward { get; set; } = 0;
        public int MinGold { get; set; } = 0;
        public int MaxGold { get; set; } = 0;
        public int Level { get; set; } = 1;

        // 物品价值属性
        public double ItemValue { get; set; } = 0;

        // 怪物类型属性
        public MonsterType Type { get; set; } = MonsterType.Normal;
        public MonsterRace Race { get; set; } = MonsterRace.Humanoid;
        
        // 闪避系统相关
        public int AvoidanceRating { get; set; } = 0;
        public double DodgeChance { get; set; } = 0.0;
        
        // 命中系统相关
        public int AccuracyRating { get; set; } = 0;
        
        // 暴击系统
        public double CriticalChance { get; set; } = 0.05;
        public double CriticalMultiplier { get; set; } = 1.5;
        
        // 元素系统
        public ElementType ElementType { get; set; } = ElementType.None;
        public Dictionary<ElementType, double> ElementalResistances { get; set; } = new();
        
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