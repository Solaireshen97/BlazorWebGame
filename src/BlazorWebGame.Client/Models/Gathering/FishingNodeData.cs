using BlazorWebGame.Shared.Enums;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Gathering
{
    /// <summary>
    /// ����ְҵ�����нڵ�����
    /// </summary>
    public static class FishingNodeData
    {
        private static readonly List<GatheringNode> _nodes = new()
        {
            new GatheringNode
            {
                Id = "NODE_RIVER_FISHING",
                Name = "�ӱߵ���",
                Description = "һ��ƽ���ĺ��壬�ʺ����ִ�����",
                GatheringTimeSeconds = 10,
                ResultingItemId = "FISH_TROUT",
                XpReward = 8,
                RequiredProfession = GatheringProfession.Fishing,
                RequiredLevel = 1
            },
            new GatheringNode
            {
                Id = "NODE_LAKE_FISHING",
                Name = "���ĵ���",
                Description = "�����ĵ���ˮ������˵�и����ϡ�е��㡣",
                GatheringTimeSeconds = 18,
                ResultingItemId = "FISH_BASS",
                XpReward = 20,
                RequiredProfession = GatheringProfession.Fishing,
                RequiredLevel = 10
            },
            // ����������Ӹ������ڵ�
        };

        /// <summary>
        /// ��ȡ���е���ڵ�
        /// </summary>
        public static List<GatheringNode> Nodes => _nodes;

        /// <summary>
        /// ����ID���ҵ���ڵ�
        /// </summary>
        public static GatheringNode? GetNodeById(string id) => _nodes.FirstOrDefault(n => n.Id == id);
    }
}