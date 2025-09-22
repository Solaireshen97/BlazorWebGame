namespace BlazorWebGame.Models
{
    public enum QuestType
    {
        KillMonster,
        GatherItem,
        CraftItem
    }

    public class Quest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Description { get; set; }
        public Faction Faction { get; set; }
        public QuestType Type { get; set; }
        public string TargetId { get; set; } // 怪物ID, 物品ID等
        public int RequiredAmount { get; set; }
        public bool IsWeekly { get; set; } = false;

        // --- 奖励 ---
        public int GoldReward { get; set; }
        public int ExperienceReward { get; set; }
        public int ReputationReward { get; set; }
        public Dictionary<string, int> ItemRewards { get; set; } = new();
    }
}