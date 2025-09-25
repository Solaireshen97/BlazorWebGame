using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Quests
{
    /// <summary>
    /// 存储所有任务数据
    /// </summary>
    public static class QuestData
    {
        // 每日任务池
        private static readonly List<Quest> _dailyQuestPool = new()
        {
            // 暴风城卫兵
            new Quest { 
                Title = "清剿哥布林", 
                Description = "为暴风城卫兵清除威胁，击杀2只哥布林",
                Faction = Faction.StormwindGuard, 
                Type = QuestType.KillMonster, 
                TargetId = "Goblin", 
                RequiredAmount = 2, 
                GoldReward = 100 
            },
            new Quest { 
                Title = "军备需求：铜锭", 
                Description = "暴风城军需处需要20个铜锭来制造装备",
                Faction = Faction.StormwindGuard, 
                Type = QuestType.GatherItem, 
                TargetId = "BAR_COPPER", 
                RequiredAmount = 20, 
                GoldReward = 150 
            },

            // 铁炉堡兄弟会
            new Quest { 
                Title = "锻造订单：铜质匕首", 
                Description = "铁炉堡兄弟会需要5把铜质匕首",
                Faction = Faction.IronforgeBrotherhood, 
                Type = QuestType.CraftItem, 
                TargetId = "EQ_WEP_COPPER_DAGGER", 
                RequiredAmount = 5, 
                GoldReward = 200 
            },
            new Quest { 
                Title = "矿石勘探", 
                Description = "为铁炉堡的工匠采集100块铜矿石",
                Faction = Faction.IronforgeBrotherhood, 
                Type = QuestType.GatherItem, 
                TargetId = "ORE_COPPER", 
                RequiredAmount = 100, 
                GoldReward = 250,
                ItemRewards = new Dictionary<string, int> { { "EQ_WEP_IRON_SWORD", 1 } } 
            },
            
            // 银色黎明
            new Quest {
                Title = "净化亡灵", 
                Description = "消灭10个亡灵生物，净化被污染的土地", 
                Faction = Faction.ArgentDawn,
                Type = QuestType.KillMonster, 
                TargetId = "Zombie", 
                RequiredAmount = 10, 
                GoldReward = 180,
                ReputationReward = 50
            },
            new Quest {
                Title = "草药收集", 
                Description = "收集30束银叶草，用于制作净化药剂", 
                Faction = Faction.ArgentDawn,
                Type = QuestType.GatherItem, 
                TargetId = "HERB_SILVERLEAF", 
                RequiredAmount = 30, 
                GoldReward = 200,
                ReputationReward = 75
            }
        };

        // 周常任务池
        private static readonly List<Quest> _weeklyQuestPool = new()
        {
            new Quest { 
                IsWeekly = true, 
                Title = "坚持不懈的战士", 
                Description = "累计击杀1000个任意怪物", 
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
                Title = "勤劳的工匠", 
                Description = "累计制作200件任意物品", 
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
                Title = "自然的馈赠", 
                Description = "累计采集500次任意资源", 
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
        /// 获取每日任务池
        /// </summary>
        public static List<Quest> DailyQuestPool => _dailyQuestPool;
        
        /// <summary>
        /// 获取周常任务池
        /// </summary>
        public static List<Quest> WeeklyQuestPool => _weeklyQuestPool;
        
        /// <summary>
        /// 根据ID获取任务
        /// </summary>
        public static Quest? GetQuestById(string id)
        {
            return _dailyQuestPool.Concat(_weeklyQuestPool)
                .FirstOrDefault(q => q.Id == id);
        }
    }
}