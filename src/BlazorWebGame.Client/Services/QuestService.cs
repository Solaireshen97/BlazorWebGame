using BlazorWebGame.Models;
using BlazorWebGame.Models.Quests;
using BlazorWebGame.Models.Items;
using System;
using System.Collections.Generic;
using System.Linq;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Services
{
    public class QuestService
    {
        private readonly InventoryService _inventoryService;
        
        /// <summary>
        /// 当前活跃的日常任务
        /// </summary>
        public List<Quest> DailyQuests { get; private set; } = new();
        
        /// <summary>
        /// 当前活跃的周常任务
        /// </summary>
        public List<Quest> WeeklyQuests { get; private set; } = new();
        
        /// <summary>
        /// 上次日常任务重置时间
        /// </summary>
        public DateTime LastDailyReset { get; private set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 上次周常任务重置时间
        /// </summary>
        public DateTime LastWeeklyReset { get; private set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event Action? OnStateChanged;

        public QuestService(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
            // 初始化任务
            RefreshDailyQuests();
            RefreshWeeklyQuests();
        }

        /// <summary>
        /// 获取当前日常任务列表
        /// </summary>
        public List<Quest> GetDailyQuests()
        {
            // 检查是否需要刷新日常任务
            CheckAndResetDailyQuests();
            return DailyQuests;
        }

        /// <summary>
        /// 获取当前周常任务列表
        /// </summary>
        public List<Quest> GetWeeklyQuests()
        {
            // 检查是否需要刷新周常任务
            CheckAndResetWeeklyQuests();
            return WeeklyQuests;
        }

        /// <summary>
        /// 检查并重置日常任务（如果需要）
        /// </summary>
        public void CheckAndResetDailyQuests()
        {
            // 检查是否已过一天（服务器时间）
            var now = DateTime.UtcNow;
            var nextResetTime = LastDailyReset.Date.AddDays(1);

            if (now >= nextResetTime)
            {
                RefreshDailyQuests();
                LastDailyReset = now;
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 检查并重置周常任务（如果需要）
        /// </summary>
        public void CheckAndResetWeeklyQuests()
        {
            // 检查是否已过一周（以周一为起点）
            var now = DateTime.UtcNow;
            var daysSinceMonday = ((int)now.DayOfWeek - 1 + 7) % 7; // 转换为周一为0
            var lastMonday = now.Date.AddDays(-daysSinceMonday);
            var nextMonday = lastMonday.AddDays(7);
            
            if (now >= nextMonday && LastWeeklyReset < lastMonday)
            {
                RefreshWeeklyQuests();
                LastWeeklyReset = now;
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 刷新日常任务
        /// </summary>
        public void RefreshDailyQuests()
        {
            var random = new Random();
            DailyQuests = QuestData.DailyQuestPool
                .GroupBy(q => q.Faction) // 按派系分组
                .SelectMany(g => g.OrderBy(_ => random.Next()).Take(2)) // 每个派系选取2个任务
                .Select(q => q.Clone()) // 复制任务实例
                .ToList();
        }

        /// <summary>
        /// 刷新周常任务
        /// </summary>
        public void RefreshWeeklyQuests()
        {
            WeeklyQuests = QuestData.WeeklyQuestPool
                .Select(q => q.Clone()) // 复制任务实例
                .ToList();
        }

        /// <summary>
        /// 更新玩家的任务进度
        /// </summary>
        public void UpdateQuestProgress(Player character, QuestType type, string targetId, int amount)
        {
            if (character == null || amount <= 0)
                return;

            // 获取所有活动任务
            var allQuests = DailyQuests.Concat(WeeklyQuests);

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
                    
                    // 检查任务是否可以自动完成
                    if (quest.AutoComplete && character.QuestProgress[quest.Id] >= quest.RequiredAmount)
                    {
                        TryCompleteQuest(character, quest.Id);
                    }
                }
            }
        }
        
        /// <summary>
        /// 尝试完成任务
        /// </summary>
        public void TryCompleteQuest(Player character, string questId)
        {
            if (character == null) return;
            
            var quest = DailyQuests.Concat(WeeklyQuests).FirstOrDefault(q => q.Id == questId);
            if (quest == null || character.CompletedQuestIds.Contains(questId)) return;

            if (character.QuestProgress.GetValueOrDefault(questId, 0) >= quest.RequiredAmount)
            {
                // 发放金币奖励
                character.Gold += quest.GoldReward;
                
                // 发放声望奖励
                if (quest.ReputationReward > 0)
                {
                    character.Reputation[quest.Faction] = character.Reputation.GetValueOrDefault(quest.Faction, 0) + quest.ReputationReward;
                }

                // 发放物品奖励
                foreach (var itemReward in quest.ItemRewards)
                {
                    _inventoryService.AddItemToInventory(character, itemReward.Key, itemReward.Value);
                }

                // 发放经验奖励
                if (quest.ExperienceReward > 0)
                {
                    switch (quest.Type)
                    {
                        case QuestType.KillMonster:
                            character.AddBattleXP(character.SelectedBattleProfession, quest.ExperienceReward);
                            break;
                        case QuestType.GatherItem:
                            // 根据任务选择合适的采集职业
                            var gatheringProfession = GetGatheringProfessionFromQuestTarget(quest.TargetId);
                            character.AddGatheringXP(gatheringProfession, quest.ExperienceReward);
                            break;
                        case QuestType.CraftItem:
                            // 根据任务选择合适的制作职业
                            var productionProfession = GetProductionProfessionFromQuestTarget(quest.TargetId);
                            character.AddProductionXP(productionProfession, quest.ExperienceReward);
                            break;
                    }
                }

                // 标记任务为已完成
                character.CompletedQuestIds.Add(questId);
                character.QuestProgress.Remove(questId);
                
                // 检查是否有后续任务
                if (!string.IsNullOrEmpty(quest.NextQuestId))
                {
                    // TODO: 添加后续任务逻辑
                }
                
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 获取玩家的可用任务列表（包括进行中和可接取的）
        /// </summary>
        public List<Quest> GetAvailableQuestsForCharacter(Player character)
        {
            if (character == null) return new List<Quest>();
            
            var allQuests = DailyQuests.Concat(WeeklyQuests);
            return allQuests
                .Where(q => !character.CompletedQuestIds.Contains(q.Id))
                .Where(q => MeetsQuestRequirements(character, q))
                .ToList();
        }
        
        /// <summary>
        /// 检查玩家是否满足接取任务的要求
        /// </summary>
        private bool MeetsQuestRequirements(Player character, Quest quest)
        {
            // 检查前置任务是否完成
            if (!string.IsNullOrEmpty(quest.PrerequisiteQuestId) && 
                !character.CompletedQuestIds.Contains(quest.PrerequisiteQuestId))
                return false;
                
            // 检查声望要求
            if (quest.RequiredReputation > 0 && 
                character.Reputation.GetValueOrDefault(quest.Faction, 0) < quest.RequiredReputation)
                return false;
                
            // 检查等级要求
            if (quest.RequiredLevel > 0)
            {
                switch (quest.Type)
                {
                    case QuestType.KillMonster:
                        if (character.GetLevel(character.SelectedBattleProfession) < quest.RequiredLevel)
                            return false;
                        break;
                    case QuestType.GatherItem:
                        var gatheringProfession = GetGatheringProfessionFromQuestTarget(quest.TargetId);
                        if (character.GetLevel(gatheringProfession) < quest.RequiredLevel)
                            return false;
                        break;
                    case QuestType.CraftItem:
                        var productionProfession = GetProductionProfessionFromQuestTarget(quest.TargetId);
                        if (character.GetLevel(productionProfession) < quest.RequiredLevel)
                            return false;
                        break;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 根据任务目标获取相应的采集职业
        /// </summary>
        private GatheringProfession GetGatheringProfessionFromQuestTarget(string targetId)
        {
            // 基于目标ID前缀或其他逻辑确定职业
            if (targetId.StartsWith("ORE_"))
                return GatheringProfession.Miner;
            else if (targetId.StartsWith("HERB_"))
                return GatheringProfession.Herbalist;
            else if (targetId.StartsWith("SKIN_"))
                return GatheringProfession.Fishing;
                
            return GatheringProfession.Miner; // 默认值
        }
        
        /// <summary>
        /// 根据任务目标获取相应的制作职业
        /// </summary>
        private ProductionProfession GetProductionProfessionFromQuestTarget(string targetId)
        {
            // 基于目标ID前缀或其他逻辑确定职业
            if (targetId.StartsWith("EQ_WEP_") || targetId.StartsWith("EQ_ARMOR_"))
                return ProductionProfession.Blacksmithing;
            else if (targetId.StartsWith("POTION_") || targetId.StartsWith("ELIXIR_"))
                return ProductionProfession.Alchemy;
            else if (targetId.StartsWith("FOOD_"))
                return ProductionProfession.Cooking;
                
            return ProductionProfession.Blacksmithing; // 默认值
        }
        
        /// <summary>
        /// 获取玩家正在进行中的任务列表
        /// </summary>
        public List<Quest> GetInProgressQuestsForCharacter(Player character)
        {
            if (character == null) return new List<Quest>();
            
            var allQuests = DailyQuests.Concat(WeeklyQuests);
            return allQuests
                .Where(q => !character.CompletedQuestIds.Contains(q.Id))
                .Where(q => character.QuestProgress.ContainsKey(q.Id))
                .ToList();
        }
        
        /// <summary>
        /// 添加自定义任务到游戏中
        /// </summary>
        public void AddCustomQuest(Quest quest)
        {
            if (quest == null) return;
            
            if (quest.IsWeekly)
                WeeklyQuests.Add(quest);
            else
                DailyQuests.Add(quest);
                
            NotifyStateChanged();
        }
        
        /// <summary>
        /// 触发状态变更事件
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}