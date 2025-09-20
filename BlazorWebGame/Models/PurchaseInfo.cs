namespace BlazorWebGame.Models
{
    /// <summary>
    /// 货币类型
    /// </summary>
    public enum CurrencyType
    {
        Gold, // 金币
        Item  // 特殊物品
    }

    /// <summary>
    /// 定义物品在商店中的购买信息
    /// </summary>
    public class PurchaseInfo
    {
        /// <summary>
        /// 商品所属的商店分类 (例如 "武器", "消耗品")
        /// </summary>
        public string ShopCategory { get; set; } = "杂项";

        /// <summary>
        /// 购买价格
        /// </summary>
        public int Price { get; set; } = 1;

        /// <summary>
        /// 购买所需的货币类型
        /// </summary>
        public CurrencyType Currency { get; set; } = CurrencyType.Gold;

        /// <summary>
        /// 如果货币类型是Item，则这里是所需物品的ID
        /// </summary>
        public string? CurrencyItemId { get; set; }
    }
}