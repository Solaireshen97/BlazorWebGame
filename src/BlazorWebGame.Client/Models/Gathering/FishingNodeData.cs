using BlazorWebGame.Shared.Enums;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Gathering
{
    /// <summary>
    /// 钓鱼职业的所有节点数据
    /// </summary>
    public static class FishingNodeData
    {
        private static readonly List<GatheringNode> _nodes = new()
        {
            new GatheringNode
            {
                Id = "NODE_RIVER_FISHING",
                Name = "河边钓点",
                Description = "一个平静的河湾，适合新手垂钓。",
                GatheringTimeSeconds = 10,
                ResultingItemId = "FISH_TROUT",
                XpReward = 8,
                RequiredProfession = GatheringProfession.Fishing,
                RequiredLevel = 1
            },
            new GatheringNode
            {
                Id = "NODE_LAKE_FISHING",
                Name = "湖心钓点",
                Description = "湖中心的深水区，据说有更大更稀有的鱼。",
                GatheringTimeSeconds = 18,
                ResultingItemId = "FISH_BASS",
                XpReward = 20,
                RequiredProfession = GatheringProfession.Fishing,
                RequiredLevel = 10
            },
            // 将来可以添加更多钓鱼节点
        };

        /// <summary>
        /// 获取所有钓鱼节点
        /// </summary>
        public static List<GatheringNode> Nodes => _nodes;

        /// <summary>
        /// 根据ID查找钓鱼节点
        /// </summary>
        public static GatheringNode? GetNodeById(string id) => _nodes.FirstOrDefault(n => n.Id == id);
    }
}