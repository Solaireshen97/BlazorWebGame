using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;

namespace BlazorWebGame.Events
{
    /// <summary>
    /// ��Ʒ�¼�����
    /// </summary>
    public class ItemEventArgs : GameEventArgs
    {
        /// <summary>
        /// ��ƷID
        /// </summary>
        public string? ItemId { get; }
        
        /// <summary>
        /// ��Ʒʵ��
        /// </summary>
        public Item? Item { get; }
        
        /// <summary>
        /// ��Ʒ����
        /// </summary>
        public int Quantity { get; }
        
        /// <summary>
        /// ��ұ仯��������
        /// </summary>
        public int? GoldChange { get; }
        
        /// <summary>
        /// װ����λ��������
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