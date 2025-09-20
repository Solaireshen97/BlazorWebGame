namespace BlazorWebGame.Models
{
    /// <summary>
    /// 代表背包中的一个格子
    /// </summary>
    public class InventorySlot
    {
        /// <summary>
        /// 格子中的物品ID。如果为null，表示格子为空。
        /// </summary>
        public string? ItemId { get; set; }

        /// <summary>
        /// 物品数量
        /// </summary>
        public int Quantity { get; set; }

        public bool IsEmpty => ItemId == null || Quantity <= 0;

        public InventorySlot(string? itemId = null, int quantity = 0)
        {
            ItemId = itemId;
            Quantity = quantity;
        }
    }
}