using System.Collections.Generic;

namespace BlazorWebGame.Models.Dungeons
{
    /// <summary>
    /// ��Ӣ�������ݼ�
    /// </summary>
    public static class EliteDungeons
    {
        public static readonly List<Dungeon> Dungeons = new()
        {
            new Dungeon
            {
                Id = "dragon_lair",
                Name = "��֮��Ѩ",
                Description = "һ�����ϵľ�����Ϣ�أ�����Σ�յ���ս�ͷ��Ľ�����",
                RecommendedLevel = 20,
                MinPlayers = 3,
                MaxPlayers = 5,
                AllowAutoRevive = false, // ���Ѷȸ����������Զ�����
                Waves = new List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Description = "�ػ�������",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "��������", Count = 3, LevelAdjustment = 1 },
                            new EnemySpawnInfo { EnemyTemplateName = "��Ԫ��", Count = 2 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Description = "�����ʹ�",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "��������", Count = 1, IsElite = true },
                            new EnemySpawnInfo { EnemyTemplateName = "����սʿ", Count = 4 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 3,
                        Description = "��֮����",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "����", Count = 2, IsElite = true, HealthMultiplier = 1.5 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 4,
                        Description = "��������",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "Զ�Ż���", Count = 1, IsElite = true, HealthMultiplier = 3.0, LevelAdjustment = 3 }
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
                CooldownHours = 168 // һ����ȴʱ��
            }
        };
    }
}