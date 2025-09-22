using BlazorWebGame.Models;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services
{
    public class QuestService
    {
        // 每日任务池
        private readonly List<Quest> _dailyQuestPool = new()
        {
            // 暴风城卫兵
            new Quest { Title = "清剿哥布林", Faction = Faction.StormwindGuard, Type = QuestType.KillMonster, TargetId = "Goblin", RequiredAmount = 2, GoldReward = 100 },
            new Quest { Title = "军备需求：铜锭", Faction = Faction.StormwindGuard, Type = QuestType.GatherItem, TargetId = "BAR_COPPER", RequiredAmount = 20, GoldReward = 150 },

            // 铁炉堡兄弟会
            new Quest { Title = "锻造订单：铜质匕首", Faction = Faction.IronforgeBrotherhood, Type = QuestType.CraftItem, TargetId = "EQ_WEP_COPPER_DAGGER", RequiredAmount = 5, GoldReward = 200 },
            new Quest { Title = "矿石勘探", Faction = Faction.IronforgeBrotherhood, Type = QuestType.GatherItem, TargetId = "ORE_COPPER", RequiredAmount = 100, GoldReward = 250 },
        };

        // 周常任务池
        private readonly List<Quest> _weeklyQuestPool = new()
        {
            new Quest { IsWeekly = true, Title = "坚持不懈的战士", Description = "累计击杀1000个任意怪物", Type = QuestType.KillMonster, TargetId = "any", RequiredAmount = 1, ReputationReward = 250, Faction = Faction.StormwindGuard },
            new Quest { IsWeekly = true, Title = "勤劳的工匠", Description = "累计制作200件任意物品", Type = QuestType.CraftItem, TargetId = "any", RequiredAmount = 200, ReputationReward = 250, Faction = Faction.IronforgeBrotherhood },
            new Quest { IsWeekly = true, Title = "自然的馈赠", Description = "累计采集500次任意资源", Type = QuestType.GatherItem, TargetId = "any", RequiredAmount = 500, ReputationReward = 250, Faction = Faction.ArgentDawn },
        };

        public List<Quest> GetDailyQuests()
        {
            // 每天从池中为每个阵营随机抽取1-2个任务
            var random = new Random();
            return _dailyQuestPool.GroupBy(q => q.Faction)
                                  .Select(g => g.OrderBy(q => random.Next()).First())
                                  .ToList();
        }

        public List<Quest> GetWeeklyQuests()
        {
            // 周常任务是固定的
            return _weeklyQuestPool;
        }
    }
}