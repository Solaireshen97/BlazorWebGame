using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Quests
{
    /// <summary>
    /// �洢������������
    /// </summary>
    public static class QuestData
    {
        // ÿ�������
        private static readonly List<Quest> _dailyQuestPool = new()
        {
            // ���������
            new Quest { 
                Title = "��˸粼��", 
                Description = "Ϊ��������������в����ɱ2ֻ�粼��",
                Faction = Faction.StormwindGuard, 
                Type = QuestType.KillMonster, 
                TargetId = "Goblin", 
                RequiredAmount = 2, 
                GoldReward = 100 
            },
            new Quest { 
                Title = "��������ͭ��", 
                Description = "����Ǿ��账��Ҫ20��ͭ��������װ��",
                Faction = Faction.StormwindGuard, 
                Type = QuestType.GatherItem, 
                TargetId = "BAR_COPPER", 
                RequiredAmount = 20, 
                GoldReward = 150 
            },

            // ��¯���ֵܻ�
            new Quest { 
                Title = "���충����ͭ��ذ��", 
                Description = "��¯���ֵܻ���Ҫ5��ͭ��ذ��",
                Faction = Faction.IronforgeBrotherhood, 
                Type = QuestType.CraftItem, 
                TargetId = "EQ_WEP_COPPER_DAGGER", 
                RequiredAmount = 5, 
                GoldReward = 200 
            },
            new Quest { 
                Title = "��ʯ��̽", 
                Description = "Ϊ��¯���Ĺ����ɼ�100��ͭ��ʯ",
                Faction = Faction.IronforgeBrotherhood, 
                Type = QuestType.GatherItem, 
                TargetId = "ORE_COPPER", 
                RequiredAmount = 100, 
                GoldReward = 250,
                ItemRewards = new Dictionary<string, int> { { "EQ_WEP_IRON_SWORD", 1 } } 
            },
            
            // ��ɫ����
            new Quest {
                Title = "��������", 
                Description = "����10�����������������Ⱦ������", 
                Faction = Faction.ArgentDawn,
                Type = QuestType.KillMonster, 
                TargetId = "Zombie", 
                RequiredAmount = 10, 
                GoldReward = 180,
                ReputationReward = 50
            },
            new Quest {
                Title = "��ҩ�ռ�", 
                Description = "�ռ�30����Ҷ�ݣ�������������ҩ��", 
                Faction = Faction.ArgentDawn,
                Type = QuestType.GatherItem, 
                TargetId = "HERB_SILVERLEAF", 
                RequiredAmount = 30, 
                GoldReward = 200,
                ReputationReward = 75
            }
        };

        // �ܳ������
        private static readonly List<Quest> _weeklyQuestPool = new()
        {
            new Quest { 
                IsWeekly = true, 
                Title = "��ֲ�и��սʿ", 
                Description = "�ۼƻ�ɱ1000���������", 
                Type = QuestType.KillMonster, 
                TargetId = "any", 
                RequiredAmount = 1000, 
                ReputationReward = 250, 
                Faction = Faction.StormwindGuard,
                GoldReward = 500,
                ItemRewards = new Dictionary<string, int> { { "POTION_HEROISM", 3 } }
            },
            new Quest { 
                IsWeekly = true, 
                Title = "���͵Ĺ���", 
                Description = "�ۼ�����200��������Ʒ", 
                Type = QuestType.CraftItem, 
                TargetId = "any", 
                RequiredAmount = 200, 
                ReputationReward = 250, 
                Faction = Faction.IronforgeBrotherhood,
                GoldReward = 550,
                ItemRewards = new Dictionary<string, int> { { "RECIPE_STEEL_PLATE", 1 } }
            },
            new Quest { 
                IsWeekly = true, 
                Title = "��Ȼ������", 
                Description = "�ۼƲɼ�500��������Դ", 
                Type = QuestType.GatherItem, 
                TargetId = "any", 
                RequiredAmount = 500, 
                ReputationReward = 250, 
                Faction = Faction.ArgentDawn,
                GoldReward = 450,
                ItemRewards = new Dictionary<string, int> { { "FOOD_GATHERING_FEAST", 5 } }
            },
        };

        /// <summary>
        /// ��ȡÿ�������
        /// </summary>
        public static List<Quest> DailyQuestPool => _dailyQuestPool;
        
        /// <summary>
        /// ��ȡ�ܳ������
        /// </summary>
        public static List<Quest> WeeklyQuestPool => _weeklyQuestPool;
        
        /// <summary>
        /// ����ID��ȡ����
        /// </summary>
        public static Quest? GetQuestById(string id)
        {
            return _dailyQuestPool.Concat(_weeklyQuestPool)
                .FirstOrDefault(q => q.Id == id);
        }
    }
}