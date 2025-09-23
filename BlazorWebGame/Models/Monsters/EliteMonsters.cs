using System.Collections.Generic;

namespace BlazorWebGame.Models.Monsters
{
    /// <summary>
    /// 精英怪物模板数据
    /// </summary>
    public static class EliteMonsters
    {
        public static readonly List<Enemy> Monsters = new()
        {
            new Enemy
            {
                Name = "GoblinElite",
                Description = "比普通哥布林更强壮、更聪明的精英战士，通常是部落的指挥官。",
                Level = 5,
                Type = MonsterType.Elite,
                Race = MonsterRace.Humanoid,
                AttackPower = 15,
                Health = 100,
                MaxHealth = 100,
                AttacksPerSecond = 0.7,
                MinGold = 10,
                MaxGold = 20,
                XpReward = 30,
                LootTable = new Dictionary<string, double>
                {
                    { "EQ_WEP_003", 0.1 }, // 恶魔之刃，替换无效的EQUIP_IRON_SWORD
                    { "MAT_DEMON_ESSENCE", 0.05 }, // 5%
                    { "RECIPE_ITEM_GOBLIN_OMELETTE", 0.2 } // 20% 掉落图纸
                },
                SkillIds = new List<string> { "SKILL_HEAVY_STRIKE" }
            },
            // 可以添加更多精英怪物...
        };
    }
}