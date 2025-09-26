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
        /// ��ǰ��Ծ���ճ�����
        /// </summary>
        public List<Quest> DailyQuests { get; private set; } = new();
        
        /// <summary>
        /// ��ǰ��Ծ���ܳ�����
        /// </summary>
        public List<Quest> WeeklyQuests { get; private set; } = new();
        
        /// <summary>
        /// �ϴ��ճ���������ʱ��
        /// </summary>
        public DateTime LastDailyReset { get; private set; } = DateTime.UtcNow;
        
        /// <summary>
        /// �ϴ��ܳ���������ʱ��
        /// </summary>
        public DateTime LastWeeklyReset { get; private set; } = DateTime.UtcNow;
        
        /// <summary>
        /// ״̬����¼�
        /// </summary>
        public event Action? OnStateChanged;

        public QuestService(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
            // ��ʼ������
            RefreshDailyQuests();
            RefreshWeeklyQuests();
        }

        /// <summary>
        /// ��ȡ��ǰ�ճ������б�
        /// </summary>
        public List<Quest> GetDailyQuests()
        {
            // ����Ƿ���Ҫˢ���ճ�����
            CheckAndResetDailyQuests();
            return DailyQuests;
        }

        /// <summary>
        /// ��ȡ��ǰ�ܳ������б�
        /// </summary>
        public List<Quest> GetWeeklyQuests()
        {
            // ����Ƿ���Ҫˢ���ܳ�����
            CheckAndResetWeeklyQuests();
            return WeeklyQuests;
        }

        /// <summary>
        /// ��鲢�����ճ����������Ҫ��
        /// </summary>
        public void CheckAndResetDailyQuests()
        {
            // ����Ƿ��ѹ�һ�죨������ʱ�䣩
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
        /// ��鲢�����ܳ����������Ҫ��
        /// </summary>
        public void CheckAndResetWeeklyQuests()
        {
            // ����Ƿ��ѹ�һ�ܣ�����һΪ��㣩
            var now = DateTime.UtcNow;
            var daysSinceMonday = ((int)now.DayOfWeek - 1 + 7) % 7; // ת��Ϊ��һΪ0
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
        /// ˢ���ճ�����
        /// </summary>
        public void RefreshDailyQuests()
        {
            var random = new Random();
            DailyQuests = QuestData.DailyQuestPool
                .GroupBy(q => q.Faction) // ����ϵ����
                .SelectMany(g => g.OrderBy(_ => random.Next()).Take(2)) // ÿ����ϵѡȡ2������
                .Select(q => q.Clone()) // ��������ʵ��
                .ToList();
        }

        /// <summary>
        /// ˢ���ܳ�����
        /// </summary>
        public void RefreshWeeklyQuests()
        {
            WeeklyQuests = QuestData.WeeklyQuestPool
                .Select(q => q.Clone()) // ��������ʵ��
                .ToList();
        }

        /// <summary>
        /// ������ҵ��������
        /// </summary>
        public void UpdateQuestProgress(Player character, QuestType type, string targetId, int amount)
        {
            if (character == null || amount <= 0)
                return;

            // ��ȡ���л����
            var allQuests = DailyQuests.Concat(WeeklyQuests);

            foreach (var quest in allQuests)
            {
                // ��������ɵ�����
                if (character.CompletedQuestIds.Contains(quest.Id))
                    continue;
                
                // ƥ���������ͣ���Ŀ��IDƥ���Ϊ"any"
                if (quest.Type == type && (quest.TargetId == targetId || quest.TargetId == "any"))
                {
                    // ��ȡ��ǰ���ȣ������������Ϊ0
                    var currentProgress = character.QuestProgress.GetValueOrDefault(quest.Id, 0);
                    
                    // ���½��ȣ�������������Ҫ��
                    character.QuestProgress[quest.Id] = Math.Min(currentProgress + amount, quest.RequiredAmount);
                    
                    // ��������Ƿ�����Զ����
                    if (quest.AutoComplete && character.QuestProgress[quest.Id] >= quest.RequiredAmount)
                    {
                        TryCompleteQuest(character, quest.Id);
                    }
                }
            }
        }
        
        /// <summary>
        /// �����������
        /// </summary>
        public void TryCompleteQuest(Player character, string questId)
        {
            if (character == null) return;
            
            var quest = DailyQuests.Concat(WeeklyQuests).FirstOrDefault(q => q.Id == questId);
            if (quest == null || character.CompletedQuestIds.Contains(questId)) return;

            if (character.QuestProgress.GetValueOrDefault(questId, 0) >= quest.RequiredAmount)
            {
                // ���Ž�ҽ���
                character.Gold += quest.GoldReward;
                
                // ������������
                if (quest.ReputationReward > 0)
                {
                    character.Reputation[quest.Faction] = character.Reputation.GetValueOrDefault(quest.Faction, 0) + quest.ReputationReward;
                }

                // ������Ʒ����
                foreach (var itemReward in quest.ItemRewards)
                {
                    _inventoryService.AddItemToInventory(character, itemReward.Key, itemReward.Value);
                }

                // ���ž��齱��
                if (quest.ExperienceReward > 0)
                {
                    switch (quest.Type)
                    {
                        case QuestType.KillMonster:
                            character.AddBattleXP(character.SelectedBattleProfession, quest.ExperienceReward);
                            break;
                        case QuestType.GatherItem:
                            // ��������ѡ����ʵĲɼ�ְҵ
                            var gatheringProfession = GetGatheringProfessionFromQuestTarget(quest.TargetId);
                            character.AddGatheringXP(gatheringProfession, quest.ExperienceReward);
                            break;
                        case QuestType.CraftItem:
                            // ��������ѡ����ʵ�����ְҵ
                            var productionProfession = GetProductionProfessionFromQuestTarget(quest.TargetId);
                            character.AddProductionXP(productionProfession, quest.ExperienceReward);
                            break;
                    }
                }

                // �������Ϊ�����
                character.CompletedQuestIds.Add(questId);
                character.QuestProgress.Remove(questId);
                
                // ����Ƿ��к�������
                if (!string.IsNullOrEmpty(quest.NextQuestId))
                {
                    // TODO: ��Ӻ��������߼�
                }
                
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// ��ȡ��ҵĿ��������б����������кͿɽ�ȡ�ģ�
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
        /// �������Ƿ������ȡ�����Ҫ��
        /// </summary>
        private bool MeetsQuestRequirements(Player character, Quest quest)
        {
            // ���ǰ�������Ƿ����
            if (!string.IsNullOrEmpty(quest.PrerequisiteQuestId) && 
                !character.CompletedQuestIds.Contains(quest.PrerequisiteQuestId))
                return false;
                
            // �������Ҫ��
            if (quest.RequiredReputation > 0 && 
                character.Reputation.GetValueOrDefault(quest.Faction, 0) < quest.RequiredReputation)
                return false;
                
            // ���ȼ�Ҫ��
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
        /// ��������Ŀ���ȡ��Ӧ�Ĳɼ�ְҵ
        /// </summary>
        private GatheringProfession GetGatheringProfessionFromQuestTarget(string targetId)
        {
            // ����Ŀ��IDǰ׺�������߼�ȷ��ְҵ
            if (targetId.StartsWith("ORE_"))
                return GatheringProfession.Miner;
            else if (targetId.StartsWith("HERB_"))
                return GatheringProfession.Herbalist;
            else if (targetId.StartsWith("SKIN_"))
                return GatheringProfession.Fishing;
                
            return GatheringProfession.Miner; // Ĭ��ֵ
        }
        
        /// <summary>
        /// ��������Ŀ���ȡ��Ӧ������ְҵ
        /// </summary>
        private ProductionProfession GetProductionProfessionFromQuestTarget(string targetId)
        {
            // ����Ŀ��IDǰ׺�������߼�ȷ��ְҵ
            if (targetId.StartsWith("EQ_WEP_") || targetId.StartsWith("EQ_ARMOR_"))
                return ProductionProfession.Blacksmithing;
            else if (targetId.StartsWith("POTION_") || targetId.StartsWith("ELIXIR_"))
                return ProductionProfession.Alchemy;
            else if (targetId.StartsWith("FOOD_"))
                return ProductionProfession.Cooking;
                
            return ProductionProfession.Blacksmithing; // Ĭ��ֵ
        }
        
        /// <summary>
        /// ��ȡ������ڽ����е������б�
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
        /// ����Զ���������Ϸ��
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
        /// ����״̬����¼�
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}