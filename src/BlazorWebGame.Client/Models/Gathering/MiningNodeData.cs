using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Gathering
{
    /// <summary>
    /// 采矿职业的所有节点数据
    /// </summary>
    public static class MiningNodeData
    {
        private static readonly List<GatheringNode> _nodes = new()
        {
            new GatheringNode
            {
                Id = "NODE_COPPER_VEIN",
                Name = "铜矿脉",
                Description = "地表常见的铜矿，是锻造的入门材料。",
                GatheringTimeSeconds = 7,
                ResultingItemId = "ORE_COPPER",
                XpReward = 7,
                RequiredProfession = GatheringProfession.Miner,
                RequiredLevel = 1
            },
            new GatheringNode
            {
                Id = "NODE_IRON_VEIN",
                Name = "铁矿脉",
                Description = "更为坚固的铁矿，通常隐藏在洞穴深处。",
                GatheringTimeSeconds = 12,
                ResultingItemId = "ORE_IRON",
                XpReward = 15,
                RequiredProfession = GatheringProfession.Miner,
                RequiredLevel = 10
            },
            // 将来可以添加更多采矿节点
        };

        /// <summary>
        /// 获取所有采矿节点
        /// </summary>
        public static List<GatheringNode> Nodes => _nodes;

        /// <summary>
        /// 根据ID查找采矿节点
        /// </summary>
        public static GatheringNode? GetNodeById(string id) => _nodes.FirstOrDefault(n => n.Id == id);
    }
}