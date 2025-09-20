using System.Collections.Generic;

namespace BlazorWebGame.Models
{
    public static class MonsterTemplates
    {
        public static readonly List<Enemy> All = new()
        {
            new Enemy
            {
                Name = "哥布林",
                Health = 50, MaxHealth = 50,
                AttackPower = 5, AttacksPerSecond = 0.8,
                XpReward = 15, MinGold = 2, MaxGold = 6,
                // 为哥布林装备技能
                SkillIds = new List<string> { "MON_001" }, // 猛击
                LootTable = new Dictionary<string, double>
                {
                    { "EQ_WEP_002", 0.1 } // 10% 概率掉落哥布林棍棒
                }
            },
            new Enemy
            {
                Name = "史莱姆",
                Health = 40, MaxHealth = 40,
                AttackPower = 3, AttacksPerSecond = 1.0,
                XpReward = 10, MinGold = 1, MaxGold = 4,
                // 为史莱姆装备技能
                SkillIds = new List<string> { "MON_003" }, // 腐蚀
                LootTable = new Dictionary<string, double>() // 史莱姆不掉落装备
            },
            new Enemy
            {
                Name = "哥布林萨满",
                Health = 60, MaxHealth = 60,
                AttackPower = 4, AttacksPerSecond = 0.7,
                XpReward = 25, MinGold = 5, MaxGold = 10,
                // 装备两个技能
                SkillIds = new List<string> { "MON_001", "MON_002" }, // 猛击 + 小型治疗
                LootTable = new Dictionary<string, double>
                {
                    { "EQ_WEP_002", 0.15 }, // 15% 掉落棍棒
                    { "EQ_CHEST_001", 0.05 } // 5% 掉落皮甲
                }
            }
        };
    }
}