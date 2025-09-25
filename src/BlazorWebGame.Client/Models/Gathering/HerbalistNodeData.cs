using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Gathering
{
    /// <summary>
    /// ��ҩѧְҵ�����нڵ�����
    /// </summary>
    public static class HerbalistNodeData
    {
        private static readonly List<GatheringNode> _nodes = new()
        {
            new GatheringNode
            {
                Id = "NODE_PEACEBLOOM",
                Name = "���񻨴�",
                Description = "һƬ���������񻨣��洦�ɼ���",
                GatheringTimeSeconds = 5,
                ResultingItemId = "HERB_PEACEBLOOM",
                XpReward = 5,
                RequiredProfession = GatheringProfession.Herbalist,
                RequiredLevel = 1,
            },
            new GatheringNode
            {
                Id = "NODE_SILVERLEAF",
                Name = "��Ҷ����",
                Description = "ֻ�л����˸粼�֣�����������Ӫ�ظ����ҵ�����ֲ�",
                GatheringTimeSeconds = 8,
                ResultingItemId = "HERB_SILVERLEAF",
                XpReward = 10,
                RequiredProfession = GatheringProfession.Herbalist,
                RequiredLevel = 5,
                RequiredMonsterId = "Goblin" // ��Ҫ���ܸ粼��
            },
            new GatheringNode
            {
                Id = "NODE_MAGEROYAL",
                Name = "ħ�ʲ�",
                Description = "������ǿ��ħ��������Χ��ϡ�в�ҩ��",
                GatheringTimeSeconds = 15,
                ResultingItemId = "HERB_MAGEROYAL",
                XpReward = 25,
                RequiredProfession = GatheringProfession.Herbalist,
                RequiredLevel = 15,
                RequiredMonsterId = "GoblinElite" // ��Ҫ���ܸ粼�־�Ӣ
            },
            // ����������Ӹ����ҩѧ�ڵ�
        };

        /// <summary>
        /// ��ȡ���в�ҩѧ�ڵ�
        /// </summary>
        public static List<GatheringNode> Nodes => _nodes;

        /// <summary>
        /// ����ID���Ҳ�ҩѧ�ڵ�
        /// </summary>
        public static GatheringNode? GetNodeById(string id) => _nodes.FirstOrDefault(n => n.Id == id);
    }
}