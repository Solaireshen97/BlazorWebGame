using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Items
{
    /// <summary>
    /// 提供对所有物品的统一访问
    /// </summary>
    public static class ItemData
    {
        /// <summary>
        /// 获取所有物品的合并列表
        /// </summary>
        public static List<Item> AllItems =>
            EquipmentData.AllAsItems
            .Concat(ConsumableData.AllAsItems)
            .Concat(MaterialData.Items)
            .ToList();

        /// <summary>
        /// 根据ID查找任意物品
        /// </summary>
        public static Item? GetItemById(string id)
        {
            // 按照优先顺序查找各类物品
            return EquipmentData.GetById(id) as Item ??
                   ConsumableData.GetById(id) as Item ??
                   MaterialData.GetById(id);
        }

        /// <summary>
        /// 获取指定类型的所有物品
        /// </summary>
        public static List<Item> GetItemsByType(ItemType type)
        {
            return type switch
            {
                ItemType.Equipment => EquipmentData.AllAsItems,
                ItemType.Consumable => ConsumableData.AllAsItems,
                ItemType.Material => MaterialData.Items,
                _ => new List<Item>()
            };
        }

        /// <summary>
        /// 获取特定商店分类的所有物品
        /// </summary>
        public static List<Item> GetItemsByShopCategory(string category)
        {
            return AllItems.Where(item =>
                item.ShopPurchaseInfo?.ShopCategory == category).ToList();
        }

        /// <summary>
        /// 获取商店中可出售的所有物品
        /// </summary>
        /// <returns>可出售物品列表</returns>
        public static List<Item> GetShopItems()
        {
            return AllItems.Where(item => item.ShopPurchaseInfo != null).ToList();
        }

        /// <summary>
        /// 获取特定商店分类的物品
        /// </summary>
        /// <param name="category">商店分类名称</param>
        /// <returns>该分类的可出售物品列表</returns>
        public static List<Item> GetShopItemsByCategory(string category)
        {
            return AllItems.Where(item =>
                item.ShopPurchaseInfo != null &&
                item.ShopPurchaseInfo.ShopCategory == category).ToList();
        }

        /// <summary>
        /// 获取所有商店分类
        /// </summary>
        /// <returns>商店分类列表</returns>
        public static List<string> GetShopCategories()
        {
            return AllItems
                .Where(item => item.ShopPurchaseInfo != null)
                .Select(item => item.ShopPurchaseInfo!.ShopCategory)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }
}