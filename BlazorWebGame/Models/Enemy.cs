using System;

namespace BlazorWebGame.Models
{
    public class Enemy
    {
        public string Name { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int AttackPower { get; set; }
        public double AttacksPerSecond { get; set; }
        public int GoldDropMin { get; set; }
        public int GoldDropMax { get; set; }
        public int XpReward { get; set; }

        public Enemy(string name, int maxHealth, int attackPower, double attacksPerSecond, int goldDropMin, int goldDropMax, int xpReward)
        {
            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
            AttackPower = attackPower;
            AttacksPerSecond = attacksPerSecond;
            GoldDropMin = goldDropMin;
            GoldDropMax = goldDropMax;
            XpReward = xpReward;
        }

        public Enemy Clone()
        {
            return new Enemy(Name, MaxHealth, AttackPower, AttacksPerSecond, GoldDropMin, GoldDropMax, XpReward);
        }

        public int GetGoldDropAmount()
        {
            return new Random().Next(GoldDropMin, GoldDropMax + 1);
        }
    }
}