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
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "未命名物品";
        public string Description { get; set; } = "";
        public ItemType Type { get; set; }
        public bool IsStackable { get; set; } = false;
        public int Value { get; set; } = 0; // 物品的售出价格
        
        /// <summary>
        /// 物品在商店中的购买信息。如果为null，表示该物品不出售。
        /// </summary>
        public PurchaseInfo? ShopPurchaseInfo { get; set; }
        
        /// <summary>
        /// 获取物品的属性描述
        /// </summary>
        public virtual string GetStatsDescription()
        {
            return string.Empty; // 基础实现为空
        }
    }
}