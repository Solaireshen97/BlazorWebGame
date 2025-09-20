using System.Collections.Generic;

namespace BlazorWebGame.Models
{
    public class Enemy
    {
        public string Name { get; set; } = "未知生物";
        public int Health { get; set; } = 50;
        public int MaxHealth { get; set; } = 50;
        public int AttackPower { get; set; } = 5;
        public double AttacksPerSecond { get; set; } = 0.8;
        public int XpReward { get; set; } = 10;
        public int MinGold { get; set; } = 1;
        public int MaxGold { get; set; } = 5;

        public List<string> SkillIds { get; set; } = new();

        public Dictionary<string, int> SkillCooldowns { get; set; } = new();

        public int GetGoldDropAmount()
        {
            return new System.Random().Next(MinGold, MaxGold + 1);
        }

        /// <summary>
        /// 执行深拷贝，确保引用类型被正确复制
        /// </summary>
        public Enemy Clone()
        {
            var clone = (Enemy)this.MemberwiseClone();
            // 为引用类型创建新的实例
            clone.SkillIds = new List<string>(this.SkillIds);
            clone.SkillCooldowns = new Dictionary<string, int>(this.SkillCooldowns);
            return clone;
        }
    }
}