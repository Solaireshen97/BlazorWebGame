using System.Collections.Generic;

namespace BlazorWebGame.Models.Dungeons
{
    /// <summary>
    /// 基础副本数据集
    /// </summary>
    public static class BasicDungeons
    {
        public static readonly List<Dungeon> Dungeons = new()
        {
            new Dungeon
            {
                Id = "forest_ruins",
                Name = "森林遗迹",
                Description = "一个被遗忘的古代遗迹，现在被各种野生动物和强盗占据。",
                RecommendedLevel = 5,
                MinPlayers = 1,
                MaxPlayers = 3,
                AllowAutoRevive = true, // 森林遗迹允许自动复活
                Waves = new List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Description = "入口守卫",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "森林狼", Count = 3 },
                            new EnemySpawnInfo { EnemyTemplateName = "强盗", Count = 1 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Description = "内部巡逻",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "强盗", Count = 2 },
                            new EnemySpawnInfo { EnemyTemplateName = "强盗弓箭手", Count = 2 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 3,
                        Description = "最终BOSS",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "强盗头目", Count = 1, IsElite = true, HealthMultiplier = 1.5 }
                        }
                    }
                },
                Rewards = new List<DungeonReward>
                {
                    new DungeonReward { Gold = 500, Experience = 1000 },
                    new DungeonReward { ItemId = "rare_sword", ItemQuantity = 1, DropChance = 0.3 },
                    new DungeonReward { ItemId = "healing_potion", ItemQuantity = 5, DropChance = 0.8 }
                },
                CooldownHours = 24
            },

            // 新增一个副本示例
            new Dungeon
            {
                Id = "abandoned_mine",
                Name = "废弃矿洞",
                Description = "一个曾经繁荣的矿洞，现在被各种矿洞生物和亡灵占据。",
                RecommendedLevel = 8,
                MinPlayers = 2,
                MaxPlayers = 4,
                AllowAutoRevive = true,
                Waves = new List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Description = "矿洞入口",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "矿洞蜘蛛", Count = 4 },
                            new EnemySpawnInfo { EnemyTemplateName = "废弃矿工", Count = 2 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Description = "矿洞深处",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "废弃矿工", Count = 3 },
                            new EnemySpawnInfo { EnemyTemplateName = "矿洞守卫", Count = 1, LevelAdjustment = 1 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 3,
                        Description = "矿洞核心",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "矿洞元素", Count = 1, IsElite = true, HealthMultiplier = 2.0, LevelAdjustment = 2 }
                        }
                    }
                },
                Rewards = new List<DungeonReward>
                {
                    new DungeonReward { Gold = 800, Experience = 1500 },
                    new DungeonReward { ItemId = "miner_pickaxe", ItemQuantity = 1, DropChance = 0.25 },
                    new DungeonReward { ItemId = "health_elixir", ItemQuantity = 3, DropChance = 0.7 }
                },
                CooldownHours = 12
            },

            // 基于现有怪物的新副本 - 哥布林洞穴
            new Dungeon
            {
                Id = "goblin_cave",
                Name = "哥布林洞穴",
                Description = "一个被哥布林占领的山洞，传说里面藏有大量财宝和珍稀装备。",
                RecommendedLevel = 3,
                MinPlayers = 1,
                MaxPlayers = 3,
                AllowAutoRevive = true,
                Waves = new List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Description = "洞穴入口",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "Goblin", Count = 4 },
                            new EnemySpawnInfo { EnemyTemplateName = "Slime", Count = 2 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Description = "中央区域",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "Goblin", Count = 2 },
                            new EnemySpawnInfo { EnemyTemplateName = "GoblinElite", Count = 2, IsElite = true }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 3,
                        Description = "首领大厅",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "GoblinChief", Count = 1, IsElite = true, HealthMultiplier = 1.2 }
                        }
                    }
                },
                Rewards = new List<DungeonReward>
                {
                    new DungeonReward { Gold = 300, Experience = 500, DropChance = 1.0 },
                    new DungeonReward { ItemId = "EQ_WEP_003", ItemQuantity = 1, DropChance = 0.5 },
                    new DungeonReward { ItemId = "MAT_DEMON_ESSENCE", ItemQuantity = 3, DropChance = 0.8 }
                },
                CooldownHours = 8
            }
        };
    }
}