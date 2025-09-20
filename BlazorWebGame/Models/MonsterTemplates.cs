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
                SkillIds = new List<string> { "MON_001" }, // 猛击
                LootTable = new Dictionary<string, double>
                {
                    { "EQ_WEP_002", 0.1 } // 10% 掉落哥布林棍棒
                }
            },
            new Enemy
            {
                Name = "史莱姆",
                Health = 40, MaxHealth = 40,
                AttackPower = 3, AttacksPerSecond = 1.0,
                XpReward = 10, MinGold = 1, MaxGold = 4,
                SkillIds = new List<string> { "MON_003" }, // 腐蚀
                LootTable = new Dictionary<string, double>()
            },
            new Enemy
            {
                Name = "哥布林精英",
                Health = 80, MaxHealth = 80,
                AttackPower = 7, AttacksPerSecond = 0.7,
                XpReward = 25, MinGold = 5, MaxGold = 10,
                SkillIds = new List<string> { "MON_001", "MON_002" },
                LootTable = new Dictionary<string, double>
                {
                    { "EQ_WEP_002", 0.15 },
                    { "EQ_CHEST_001", 0.05 },
                    { "MAT_DEMON_ESSENCE", 0.02 } // 2% 几率掉落恶魔精华
                }
            }
        };
    }
}