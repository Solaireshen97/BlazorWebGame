using System.Collections.Generic;

namespace BlazorWebGame.Models.Dungeons
{
    /// <summary>
    /// 精英副本数据集
    /// </summary>
    public static class EliteDungeons
    {
        public static readonly List<Dungeon> Dungeons = new()
        {
            new Dungeon
            {
                Id = "dragon_lair",
                Name = "龙之巢穴",
                Description = "一个古老的巨龙栖息地，充满危险的挑战和丰厚的奖励。",
                RecommendedLevel = 20,
                MinPlayers = 3,
                MaxPlayers = 5,
                AllowAutoRevive = false, // 高难度副本不允许自动复活
                Waves = new List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Description = "守护者试炼",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "龙族守卫", Count = 3, LevelAdjustment = 1 },
                            new EnemySpawnInfo { EnemyTemplateName = "火元素", Count = 2 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Description = "龙族仆从",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "龙族萨满", Count = 1, IsElite = true },
                            new EnemySpawnInfo { EnemyTemplateName = "龙族战士", Count = 4 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 3,
                        Description = "龙之幼崽",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "幼龙", Count = 2, IsElite = true, HealthMultiplier = 1.5 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 4,
                        Description = "巨龙首领",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "远古火龙", Count = 1, IsElite = true, HealthMultiplier = 3.0, LevelAdjustment = 3 }
                        }
                    }
                },
                Rewards = new List<DungeonReward>
                {
                    new DungeonReward { Gold = 5000, Experience = 8000, DropChance = 1.0 },
                    new DungeonReward { ItemId = "dragon_scale_armor", ItemQuantity = 1, DropChance = 0.4 },
                    new DungeonReward { ItemId = "dragon_fang_sword", ItemQuantity = 1, DropChance = 0.3 },
                    new DungeonReward { ItemId = "dragon_essence", ItemQuantity = 5, DropChance = 0.7 }
                },
                CooldownHours = 168 // 一周冷却时间
            }
        };
    }
}