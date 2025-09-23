using BlazorWebGame.Models;
using System;
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
            new Quest { IsWeekly = true, Title = "坚持不懈的战士", Description = "累计击杀1000个任意怪物", Type = QuestType.KillMonster, TargetId = "any", RequiredAmount = 1000, ReputationReward = 250, Faction = Faction.StormwindGuard },
            new Quest { IsWeekly = true, Title = "勤劳的工匠", Description = "累计制作200件任意物品", Type = QuestType.CraftItem, TargetId = "any", RequiredAmount = 200, ReputationReward = 250, Faction = Faction.IronforgeBrotherhood },
            new Quest { IsWeekly = true, Title = "自然的馈赠", Description = "累计采集500次任意资源", Type = QuestType.GatherItem, TargetId = "any", RequiredAmount = 500, ReputationReward = 250, Faction = Faction.ArgentDawn },
        };

        // 当前激活的日常任务和周常任务
        private List<Quest> _dailyQuests = new();
        private List<Quest> _weeklyQuests = new();

        public List<Quest> GetDailyQuests()
        {
            if (!_dailyQuests.Any())
            {
                // 每天从池中为每个阵营随机抽取1-2个任务
                var random = new Random();
                _dailyQuests = _dailyQuestPool.GroupBy(q => q.Faction)
                                            .Select(g => g.OrderBy(q => random.Next()).First())
                                            .ToList();
            }
            return _dailyQuests;
        }

        public List<Quest> GetWeeklyQuests()
        {
            if (!_weeklyQuests.Any())
            {
                // 周常任务是固定的
                _weeklyQuests = _weeklyQuestPool.ToList();
            }
            return _weeklyQuests;
        }

        /// <summary>
        /// 更新玩家的任务进度
        /// </summary>
        /// <param name="character">玩家角色</param>
        /// <param name="type">任务类型</param>
        /// <param name="targetId">目标ID（怪物ID、物品ID等）</param>
        /// <param name="amount">增加的数量</param>
        public void UpdateQuestProgress(Player character, QuestType type, string targetId, int amount)
        {
            if (character == null || amount <= 0)
                return;

            // 获取所有活动任务
            var allQuests = GetDailyQuests().Concat(GetWeeklyQuests());

            foreach (var quest in allQuests)
            {
                // 跳过已完成的任务
                if (character.CompletedQuestIds.Contains(quest.Id))
                    continue;
                
                // 匹配任务类型，且目标ID匹配或为"any"
                if (quest.Type == type && (quest.TargetId == targetId || quest.TargetId == "any"))
                {
                    // 获取当前进度，如果不存在则为0
                    var currentProgress = character.QuestProgress.GetValueOrDefault(quest.Id, 0);
                    
                    // 更新进度，但不超过任务要求
                    character.QuestProgress[quest.Id] = Math.Min(currentProgress + amount, quest.RequiredAmount);
                }
            }
        }
    }
}