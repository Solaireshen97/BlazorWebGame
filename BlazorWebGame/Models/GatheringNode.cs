namespace BlazorWebGame.Models
{
    /// <summary>
    /// 代表一个可采集的资源点，如矿脉或草药丛
    /// </summary>
    public class GatheringNode
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 采集此资源所需的时间（秒）
        /// </summary>
        public double GatheringTimeSeconds { get; set; }

        /// <summary>
        /// 采集成功后获得的物品ID
        /// </summary>
        public string ResultingItemId { get; set; } = string.Empty;

        /// <summary>
        /// 每次采集成功后获得的物品数量
        /// </summary>
        public int ResultingItemQuantity { get; set; } = 1;

        /// <summary>
        /// 采集成功后获得的经验值
        /// </summary>
        public int XpReward { get; set; }

        // --- 解锁条件 ---

        /// <summary>
        /// 采集此资源所需的职业
        /// </summary>
        public GatheringProfession RequiredProfession { get; set; }

        /// <summary>
        /// 采集所需的职业等级
        /// </summary>
        public int RequiredLevel { get; set; }

        /// <summary>
        /// 解锁此资源点需要击败的怪物ID。如果为null，则无此要求。
        /// </summary>
        public string? RequiredMonsterId { get; set; }
    }
}