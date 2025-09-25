using BlazorWebGame.Models;
using BlazorWebGame.Models.Monsters;

namespace BlazorWebGame.Utils
{
    /// <summary>
    /// רҵ��صĸ�������
    /// </summary>
    public static class ProfessionHelper
    {
        /// <summary>
        /// ���㵱ǰ�ɼ�ʱ��
        /// </summary>
        public static double GetCurrentGatheringTime(Player character)
        {
            if (character.CurrentGatheringNode == null) return 0;
            double speedBonus = character.GetTotalGatheringSpeedBonus();
            return character.CurrentGatheringNode.GatheringTimeSeconds / (1 + speedBonus);
        }

        /// <summary>
        /// ����ɼ����Ȱٷֱ�
        /// </summary>
        public static double GetGatheringProgress(Player character)
        {
            var totalTime = GetCurrentGatheringTime(character);
            if (totalTime <= 0) return 0;
            var progress = (totalTime - character.GatheringCooldown) / totalTime;
            return Math.Clamp(progress * 100, 0, 100);
        }

        /// <summary>
        /// ��ȡ��ǰ�����ɼ��������
        /// </summary>
        public static string GetOtherGatheringActionName(Player character)
        {
            return character.CurrentGatheringNode?.RequiredProfession switch
            {
                GatheringProfession.Herbalist => "�ɼ���ҩ",
                GatheringProfession.Miner => "�ɿ�",
                GatheringProfession.Fishing => "����",
                _ => "���������ɼ�"
            };
        }

        /// <summary>
        /// ���ڵ��Ƿ��ѽ���
        /// </summary>
        public static (bool IsUnlocked, string Reason) IsNodeUnlocked(Player character, GatheringNode node)
        {
            if (character.GetLevel(node.RequiredProfession) < node.RequiredLevel)
            {
                return (false, $"��Ҫ {node.RequiredProfession} �ȼ��ﵽ {node.RequiredLevel}");
            }

            if (!string.IsNullOrEmpty(node.RequiredMonsterId) && !character.DefeatedMonsterIds.Contains(node.RequiredMonsterId))
            {
                var monsterName = MonsterTemplates.All.FirstOrDefault(m => m.Name == node.RequiredMonsterId)?.Name ?? "δ֪����";
                return (false, $"��Ҫ�Ȼ��� {monsterName}");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// ������ұ�����ָ����Ʒ������
        /// </summary>
        public static int GetItemCountInInventory(Player character, string itemId)
        {
            return character.Inventory.Where(s => s.ItemId == itemId).Sum(s => s.Quantity);
        }
    }
}