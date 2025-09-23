using System.Collections.Generic;

namespace BlazorWebGame.Models.Monsters
{
    /// <summary>
    /// 普通怪物模板数据
    /// </summary>
    public static class NormalMonsters
    {
        public static readonly List<Enemy> Monsters = new()
        {
            new Enemy
            {
                Name = "Goblin",
                Description = "一个矮小但机敏的绿皮生物，喜欢收集闪亮的物品。",
                Level = 1,
                Type = MonsterType.Normal,
                Race = MonsterRace.Humanoid,
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
                Name = "Slime",
                Description = "一团不断蠕动的黏液生物，可以溶解接触到的物体。",
                Level = 1,
                Type = MonsterType.Normal,
                Race = MonsterRace.Elemental,
                Health = 40, MaxHealth = 40,
                AttackPower = 3, AttacksPerSecond = 1.0,
                XpReward = 10, MinGold = 1, MaxGold = 4,
                SkillIds = new List<string> { "MON_003" }, // 腐蚀
                LootTable = new Dictionary<string, double>()
            },
            // 可以添加更多普通怪物...
        };
    }
}