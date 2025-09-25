using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Gathering
{
    /// <summary>
    /// �ɿ�ְҵ�����нڵ�����
    /// </summary>
    public static class MiningNodeData
    {
        private static readonly List<GatheringNode> _nodes = new()
        {
            new GatheringNode
            {
                Id = "NODE_COPPER_VEIN",
                Name = "ͭ����",
                Description = "�ر�����ͭ���Ƕ�������Ų��ϡ�",
                GatheringTimeSeconds = 7,
                ResultingItemId = "ORE_COPPER",
                XpReward = 7,
                RequiredProfession = GatheringProfession.Miner,
                RequiredLevel = 1
            },
            new GatheringNode
            {
                Id = "NODE_IRON_VEIN",
                Name = "������",
                Description = "��Ϊ��̵�����ͨ�������ڶ�Ѩ���",
                GatheringTimeSeconds = 12,
                ResultingItemId = "ORE_IRON",
                XpReward = 15,
                RequiredProfession = GatheringProfession.Miner,
                RequiredLevel = 10
            },
            // ����������Ӹ���ɿ�ڵ�
        };

        /// <summary>
        /// ��ȡ���вɿ�ڵ�
        /// </summary>
        public static List<GatheringNode> Nodes => _nodes;

        /// <summary>
        /// ����ID���Ҳɿ�ڵ�
        /// </summary>
        public static GatheringNode? GetNodeById(string id) => _nodes.FirstOrDefault(n => n.Id == id);
    }
}