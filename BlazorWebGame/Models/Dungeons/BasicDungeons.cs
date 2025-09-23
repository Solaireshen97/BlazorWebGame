using System.Collections.Generic;

namespace BlazorWebGame.Models.Dungeons
{
    /// <summary>
    /// �����������ݼ�
    /// </summary>
    public static class BasicDungeons
    {
        public static readonly List<Dungeon> Dungeons = new()
        {
            new Dungeon
            {
                Id = "forest_ruins",
                Name = "ɭ���ż�",
                Description = "һ���������ĹŴ��ż������ڱ�����Ұ�������ǿ��ռ�ݡ�",
                RecommendedLevel = 5,
                MinPlayers = 1,
                MaxPlayers = 3,
                AllowAutoRevive = true, // ɭ���ż������Զ�����
                Waves = new List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Description = "�������",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "ɭ����", Count = 3 },
                            new EnemySpawnInfo { EnemyTemplateName = "ǿ��", Count = 1 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Description = "�ڲ�Ѳ��",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "ǿ��", Count = 2 },
                            new EnemySpawnInfo { EnemyTemplateName = "ǿ��������", Count = 2 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 3,
                        Description = "����BOSS",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "ǿ��ͷĿ", Count = 1, IsElite = true, HealthMultiplier = 1.5 }
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

            // ����һ������ʾ��
            new Dungeon
            {
                Id = "abandoned_mine",
                Name = "������",
                Description = "һ���������ٵĿ󶴣����ڱ����ֿ����������ռ�ݡ�",
                RecommendedLevel = 8,
                MinPlayers = 2,
                MaxPlayers = 4,
                AllowAutoRevive = true,
                Waves = new List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Description = "�����",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "��֩��", Count = 4 },
                            new EnemySpawnInfo { EnemyTemplateName = "������", Count = 2 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Description = "���",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "������", Count = 3 },
                            new EnemySpawnInfo { EnemyTemplateName = "������", Count = 1, LevelAdjustment = 1 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 3,
                        Description = "�󶴺���",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "��Ԫ��", Count = 1, IsElite = true, HealthMultiplier = 2.0, LevelAdjustment = 2 }
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

            // �������й�����¸��� - �粼�ֶ�Ѩ
            new Dungeon
            {
                Id = "goblin_cave",
                Name = "�粼�ֶ�Ѩ",
                Description = "һ�����粼��ռ���ɽ������˵������д����Ʊ�����ϡװ����",
                RecommendedLevel = 3,
                MinPlayers = 1,
                MaxPlayers = 3,
                AllowAutoRevive = true,
                Waves = new List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Description = "��Ѩ���",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "Goblin", Count = 4 },
                            new EnemySpawnInfo { EnemyTemplateName = "Slime", Count = 2 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Description = "��������",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "Goblin", Count = 2 },
                            new EnemySpawnInfo { EnemyTemplateName = "GoblinElite", Count = 2, IsElite = true }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 3,
                        Description = "�������",
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