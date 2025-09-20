namespace BlazorWebGame.Models
{
    public enum ItemType
    {
        Equipment, // 装备
        Consumable, // 消耗品
        Material,  // 材料
    }

    public class Item
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ItemType Type { get; set; }
        public bool IsStackable { get; set; } = false; // 是否可堆叠
        public int Value { get; set; } = 1; // 物品的售出价格

        /// <summary>
        /// 物品在商店中的购买信息。如果为null，表示该物品不出售。
        /// </summary>
        public PurchaseInfo? ShopPurchaseInfo { get; set; }
    }
}