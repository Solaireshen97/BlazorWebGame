using BlazorWebGame.Models;
using BlazorWebGame.Models.Monsters;

namespace BlazorWebGame.Utils
{
    /// <summary>
    /// 专业相关的辅助方法
    /// </summary>
    public static class ProfessionHelper
    {
        /// <summary>
        /// 计算当前采集时间
        /// </summary>
        public static double GetCurrentGatheringTime(Player character)
        {
            if (character.CurrentGatheringNode == null) return 0;
            double speedBonus = character.GetTotalGatheringSpeedBonus();
            return character.CurrentGatheringNode.GatheringTimeSeconds / (1 + speedBonus);
        }

        /// <summary>
        /// 计算采集进度百分比
        /// </summary>
        public static double GetGatheringProgress(Player character)
        {
            var totalTime = GetCurrentGatheringTime(character);
            if (totalTime <= 0) return 0;
            var progress = (totalTime - character.GatheringCooldown) / totalTime;
            return Math.Clamp(progress * 100, 0, 100);
        }

        /// <summary>
        /// 获取当前其他采集活动的名称
        /// </summary>
        public static string GetOtherGatheringActionName(Player character)
        {
            return character.CurrentGatheringNode?.RequiredProfession switch
            {
                GatheringProfession.Herbalist => "采集草药",
                GatheringProfession.Miner => "采矿",
                GatheringProfession.Fishing => "钓鱼",
                _ => "进行其他采集"
            };
        }

        /// <summary>
        /// 检查节点是否已解锁
        /// </summary>
        public static (bool IsUnlocked, string Reason) IsNodeUnlocked(Player character, GatheringNode node)
        {
            if (character.GetLevel(node.RequiredProfession) < node.RequiredLevel)
            {
                return (false, $"需要 {node.RequiredProfession} 等级达到 {node.RequiredLevel}");
            }

            if (!string.IsNullOrEmpty(node.RequiredMonsterId) && !character.DefeatedMonsterIds.Contains(node.RequiredMonsterId))
            {
                var monsterName = MonsterTemplates.All.FirstOrDefault(m => m.Name == node.RequiredMonsterId)?.Name ?? "未知怪物";
                return (false, $"需要先击败 {monsterName}");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 计算玩家背包中指定物品的数量
        /// </summary>
        public static int GetItemCountInInventory(Player character, string itemId)
        {
            return character.Inventory.Where(s => s.ItemId == itemId).Sum(s => s.Quantity);
        }
    }
}