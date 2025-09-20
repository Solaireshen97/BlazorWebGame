using System.Collections.Generic;

namespace BlazorWebGame.Models
{
    /// <summary>
    /// 定义商店中可出售的物品
    /// </summary>
    public static class ShopData
    {
        /// <summary>
        /// 商店中出售物品的ID列表
        /// </summary>
        public static readonly List<string> ForSaleItemIds = new()
        {
            "EQ_WEP_001",   // 生锈的铁剑
            "EQ_CHEST_001"  // 破旧的皮甲
        };
    }
}