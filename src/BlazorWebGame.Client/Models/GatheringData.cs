using BlazorWebGame.Models.Gathering;
using System.Collections.Generic;
using System.Linq;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Models
{
    /// <summary>
    /// 提供对所有采集节点的统一访问
    /// </summary>
    public static class GatheringData
    {
        /// <summary>
        /// 获取所有采集节点的合并列表
        /// </summary>
        public static List<GatheringNode> AllNodes =>
            MiningNodeData.Nodes
            .Concat(HerbalistNodeData.Nodes)
            .Concat(FishingNodeData.Nodes)
            .ToList();

        /// <summary>
        /// 根据ID查找任意采集节点
        /// </summary>
        public static GatheringNode? GetNodeById(string id) =>
            MiningNodeData.GetNodeById(id) ??
            HerbalistNodeData.GetNodeById(id) ??
            FishingNodeData.GetNodeById(id);

        /// <summary>
        /// 获取特定职业的所有采集节点
        /// </summary>
        public static List<GatheringNode> GetNodesByProfession(GatheringProfession profession)
        {
            return profession switch
            {
                GatheringProfession.Miner => MiningNodeData.Nodes,
                GatheringProfession.Herbalist => HerbalistNodeData.Nodes,
                GatheringProfession.Fishing => FishingNodeData.Nodes,
                _ => new List<GatheringNode>()
            };
        }

        /// <summary>
        /// 获取角色可用的采集节点（根据等级和已击败的怪物）
        /// </summary>
        public static List<GatheringNode> GetAvailableNodesForPlayer(Player player, GatheringProfession profession)
        {
            var professionLevel = player.GetLevel(profession);
            return GetNodesByProfession(profession)
                .Where(node =>
                    node.RequiredLevel <= professionLevel &&
                    (string.IsNullOrEmpty(node.RequiredMonsterId) ||
                     player.DefeatedMonsterIds.Contains(node.RequiredMonsterId)))
                .ToList();
        }
    }
}