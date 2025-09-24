using System.Collections.Generic;

namespace BlazorWebGame.Models.Monsters
{
    /// <summary>
    /// 普通怪物模板数据
    /// </summary>
    public static class NormalMonsters
    {
        // 静态构造函数，用于初始化时添加通过计算器生成的怪物
        static NormalMonsters()
        {
            // 添加预设的怪物实例
            //itializeMonsters();

            // 添加通过计算器生成的怪物
            AddCalculatedMonsters();
        }

        public static readonly List<Enemy> Monsters = new();

        /// <summary>
        /// 初始化预设的怪物实例
        /// </summary>
        private static void InitializeMonsters()
        {
            Monsters.Add(new Enemy
            {
                Name = "Goblin",
                Description = "一个矮小但机敏的绿皮生物，喜欢收集闪亮的物品。",
                Level = 1,
                Type = MonsterType.Normal,
                Race = MonsterRace.Humanoid,
                Health = 50,
                MaxHealth = 50,
                AttackPower = 5,
                AttacksPerSecond = 0.8,
                XpReward = 15,
                MinGold = 2,
                MaxGold = 6,
                SkillIds = new List<string> { "MON_001" }, // 猛击
                LootTable = new Dictionary<string, double>
                {
                    { "EQ_WEP_002", 0.1 } // 10% 掉落哥布林棍棒
                }
            });

            Monsters.Add(new Enemy
            {
                Name = "Slime",
                Description = "一团不断蠕动的黏液生物，可以溶解接触到的物体。",
                Level = 1,
                Type = MonsterType.Normal,
                Race = MonsterRace.Elemental,
                Health = 40,
                MaxHealth = 40,
                AttackPower = 3,
                AttacksPerSecond = 1.0,
                XpReward = 10,
                MinGold = 1,
                MaxGold = 4,
                SkillIds = new List<string> { "MON_003" }, // 腐蚀
                LootTable = new Dictionary<string, double>()
            });
        }

        /// <summary>
        /// 添加通过计算器生成的怪物
        /// </summary>
        private static void AddCalculatedMonsters()
        {
            var monsterAttribute1 = new Enemy
            {
                Name = "染病的幼狼",
                Description = "",
                Level = 1,
                Type = MonsterType.Normal,
                Race = MonsterRace.Beast,
                ElementType = ElementType.None,
                SkillIds = new List<string> { },
                LootTable = new Dictionary<string, double>
                {
                }
            };

            var monsters1 = MonsterAttributeCalculator.GenerateMonster(
                level: 1,
                expRatio: 0.6,
                lootRatio: 0.2,
                monsterType: MonsterType.Normal,
                predefinedEnemy: monsterAttribute1);
            Monsters.Add(monsters1);

            var monsterAttribute2 = new Enemy
            {
                Name = "染病的森林狼",
                Description = "",
                Level = 3,
                Type = MonsterType.Normal,
                Race = MonsterRace.Beast,
                ElementType = ElementType.None,
                SkillIds = new List<string> { },
                LootTable = new Dictionary<string, double>
                {
                }
            };

            var monsters2 = MonsterAttributeCalculator.GenerateMonster(
                level: 3,
                expRatio: 0.7,
                lootRatio: 0.2,
                monsterType: MonsterType.Normal,
                predefinedEnemy: monsterAttribute2);
            Monsters.Add(monsters2);
        }

        /// <summary>
        /// 公开方法：添加一个新的计算生成的怪物
        /// </summary>
        public static void AddNewCalculatedMonster(string name, string description, int level,
            MonsterRace race = MonsterRace.Humanoid, ElementType elementType = ElementType.None,
            double expRatio = 0.6, double lootRatio = 0.15)
        {
            var predefinedMonster = new Enemy
            {
                Name = name,
                Description = description,
                Level = level,
                Type = MonsterType.Normal,
                Race = race,
                ElementType = elementType
            };

            var calculatedMonster = MonsterAttributeCalculator.GenerateMonster(
                level: level,
                expRatio: expRatio,
                lootRatio: lootRatio,
                monsterType: MonsterType.Normal,
                predefinedEnemy: predefinedMonster);

            Monsters.Add(calculatedMonster);
        }
    }
}