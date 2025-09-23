using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;

namespace BlazorWebGame.Events
{
    /// <summary>
    /// 物品事件参数
    /// </summary>
    public class ItemEventArgs : GameEventArgs
    {
        /// <summary>
        /// 物品ID
        /// </summary>
        public string? ItemId { get; }
        
        /// <summary>
        /// 物品实例
        /// </summary>
        public Item? Item { get; }
        
        /// <summary>
        /// 物品数量
        /// </summary>
        public int Quantity { get; }
        
        /// <summary>
        /// 金币变化，如适用
        /// </summary>
        public int? GoldChange { get; }
        
        /// <summary>
        /// 装备槽位，如适用
        /// </summary>
        public EquipmentSlot? Slot { get; }

        public ItemEventArgs(
            GameEventType eventType, 
            Player? player = null,
            string? itemId = null,
            Item? item = null,
            int quantity = 1,
            int? goldChange = null,
            EquipmentSlot? slot = null) : base(eventType, player)
        {
            ItemId = itemId;
            Item = item;
            Quantity = quantity;
            GoldChange = goldChange;
            Slot = slot;
        }
    }
}