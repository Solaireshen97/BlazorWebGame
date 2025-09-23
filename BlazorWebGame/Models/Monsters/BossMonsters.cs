using System.Collections.Generic;

namespace BlazorWebGame.Models.Monsters
{
    /// <summary>
    /// Boss怪物模板数据
    /// </summary>
    public static class BossMonsters
    {
        public static readonly List<Enemy> Monsters = new()
        {
            // 示例Boss怪物
            new Enemy
            {
                Name = "GoblinChief",
                Description = "哥布林部落的首领，体型庞大，装备精良，拥有强大的战斗能力。",
                Level = 10,
                Type = MonsterType.Boss,
                Race = MonsterRace.Humanoid,
                Health = 500,
                MaxHealth = 500,
                AttackPower = 30,
                AttacksPerSecond = 0.5,
                XpReward = 100,
                MinGold = 50,
                MaxGold = 100,
                SkillIds = new List<string> 
                { 
                    "SKILL_HEAVY_STRIKE", 
                    "SKILL_WAR_CRY", 
                    "SKILL_SUMMON_MINIONS" 
                },
                LootTable = new Dictionary<string, double>
                {
                    { "EQ_WEP_003", 0.5 },        // 50% 恶魔之刃
                    { "MAT_DEMON_ESSENCE", 1.0 },  // 100% 恶魔精华
                    { "RECIPE_ITEM_GOBLIN_OMELETTE", 1.0 }  // 100% 食谱
                }
            }
            // 可以添加更多Boss怪物...
        };
    }
}