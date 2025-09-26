using System;
using System.Collections.Generic;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Models
{
    public enum QuestType
    {
        KillMonster,
        GatherItem,
        CraftItem,
        DeliverItem,   // 新增：物品递送任务
        Exploration    // 新增：探索任务
    }

    public class Quest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Faction Faction { get; set; }
        public QuestType Type { get; set; }
        public string TargetId { get; set; } = string.Empty; // 怪物ID, 物品ID等
        public int RequiredAmount { get; set; }
        public bool IsWeekly { get; set; } = false;
        public bool AutoComplete { get; set; } = false; // 是否自动完成
        public string? PrerequisiteQuestId { get; set; } // 前置任务ID
        public string? NextQuestId { get; set; } // 后续任务ID
        public int RequiredLevel { get; set; } // 等级要求
        public int RequiredReputation { get; set; } // 声望要求

        // --- 奖励 ---
        public int GoldReward { get; set; }
        public int ExperienceReward { get; set; }
        public int ReputationReward { get; set; }
        public Dictionary<string, int> ItemRewards { get; set; } = new();
        
        /// <summary>
        /// 创建任务的副本
        /// </summary>
        public Quest Clone()
        {
            return new Quest
            {
                Id = Guid.NewGuid().ToString(), // 生成新ID
                Title = this.Title,
                Description = this.Description,
                Faction = this.Faction,
                Type = this.Type,
                TargetId = this.TargetId,
                RequiredAmount = this.RequiredAmount,
                IsWeekly = this.IsWeekly,
                AutoComplete = this.AutoComplete,
                PrerequisiteQuestId = this.PrerequisiteQuestId,
                NextQuestId = this.NextQuestId,
                RequiredLevel = this.RequiredLevel,
                RequiredReputation = this.RequiredReputation,
                GoldReward = this.GoldReward,
                ExperienceReward = this.ExperienceReward,
                ReputationReward = this.ReputationReward,
                ItemRewards = new Dictionary<string, int>(this.ItemRewards)
            };
        }
    }
}