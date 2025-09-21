using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models
{
    public static class GatheringData
    {
        private static readonly List<GatheringNode> _allNodes = new()
        {
            // --- 草药学节点 ---
            new GatheringNode
            {
                Id = "NODE_PEACEBLOOM",
                Name = "宁神花丛",
                Description = "一片宁静的宁神花，随处可见。",
                GatheringTimeSeconds = 5,
                ResultingItemId = "HERB_PEACEBLOOM",
                XpReward = 5,
                RequiredProfession = GatheringProfession.Herbalist,
                RequiredLevel = 1,
            },
            new GatheringNode
            {
                Id = "NODE_SILVERLEAF",
                Name = "银叶草圃",
                Description = "只有击败了哥布林，才能在它们营地附近找到这种植物。",
                GatheringTimeSeconds = 8,
                ResultingItemId = "HERB_SILVERLEAF",
                XpReward = 10,
                RequiredProfession = GatheringProfession.Herbalist,
                RequiredLevel = 5,
                RequiredMonsterId = "Goblin" // 需要击败哥布林
            },
            new GatheringNode
            {
                Id = "NODE_MAGEROYAL",
                Name = "魔皇草",
                Description = "生长在强大魔法生物周围的稀有草药。",
                GatheringTimeSeconds = 15,
                ResultingItemId = "HERB_MAGEROYAL",
                XpReward = 25,
                RequiredProfession = GatheringProfession.Herbalist,
                RequiredLevel = 15,
                RequiredMonsterId = "GoblinElite" // 需要击败哥布林精英
            },

            // --- 新增：采矿节点 ---
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
            }
        };

        public static List<GatheringNode> AllNodes => _allNodes;

        public static GatheringNode? GetNodeById(string id) => _allNodes.FirstOrDefault(n => n.Id == id);
    }
}