using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// 简化的物品系统服务 - 仅保留UI状态管理，所有库存逻辑由服务器处理
    /// </summary>
    public class InventoryService
    {
        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action? OnStateChanged;

        #region 物品管理 - 已移除本地实现

        /// <summary>
        /// 添加物品到角色背包 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public void AddItemToInventory(Player character, string itemId, int quantity)
        {
            // 本地库存系统已移除，所有库存操作由服务器处理
        }

        /// <summary>
        /// 从角色背包移除物品 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public bool RemoveItemFromInventory(Player character, string itemId, int quantityToRemove, out int actuallyRemoved)
        {
            actuallyRemoved = 0;
            // 本地库存系统已移除
            return false;
        }

        /// <summary>
        /// 检查角色是否有足够物品 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public bool HasItem(Player character, string itemId, int quantity = 1)
        {
            // 本地库存系统已移除
            return false;
        }

        /// <summary>
        /// 获取角色拥有的物品数量 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public int GetItemCount(Player character, string itemId)
        {
            // 本地库存系统已移除
            return 0;
        }

        #endregion

        #region 装备系统 - 已移除本地实现

        /// <summary>
        /// 穿戴装备 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public void EquipItem(Player character, string itemId)
        {
            // 本地装备系统已移除
        }

        /// <summary>
        /// 卸下装备 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public void UnequipItem(Player character, EquipmentSlot slot)
        {
            // 本地装备系统已移除
        }

        #endregion

        #region 物品使用 - 已移除本地实现

        /// <summary>
        /// 使用物品 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public void UseItem(Player character, string itemId)
        {
            // 本地物品使用系统已移除
        }

        /// <summary>
        /// 使用消耗品 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public void UseConsumable(Player character, Consumable consumable)
        {
            // 本地消耗品系统已移除
        }

        #endregion

        #region 商店系统 - 已移除本地实现

        /// <summary>
        /// 购买物品 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public bool BuyItem(Player character, string itemId, int quantity = 1)
        {
            // 本地商店系统已移除
            return false;
        }

        /// <summary>
        /// 出售物品 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public void SellItem(Player character, string itemId, int quantity = 1)
        {
            // 本地商店系统已移除
        }

        #endregion

        #region 快捷栏管理 - 已移除本地实现

        /// <summary>
        /// 设置快捷栏物品 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public void SetQuickSlotItem(Player character, ConsumableCategory category, int slotId, string itemId)
        {
            // 本地快捷栏系统已移除
        }

        /// <summary>
        /// 清理快捷栏物品 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public void ClearQuickSlotItem(Player character, ConsumableCategory category, int slotId, FoodType foodType = FoodType.None)
        {
            // 本地快捷栏系统已移除
        }

        #endregion

        #region 自动出售 - 已移除本地实现

        /// <summary>
        /// 切换物品自动出售状态 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public void ToggleAutoSellItem(Player character, string itemId)
        {
            // 本地自动出售系统已移除
        }

        #endregion

        #region 配方检查 - 已移除本地实现

        /// <summary>
        /// 检查是否能够制作配方 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public bool CanAffordRecipe(Player character, Recipe recipe)
        {
            // 本地配方系统已移除
            return false;
        }

        #endregion

        #region 消耗品处理 - 已移除本地实现

        /// <summary>
        /// 更新消耗品冷却时间 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public void UpdateConsumableCooldowns(Player character, double elapsedSeconds)
        {
            // 本地消耗品系统已移除
        }

        /// <summary>
        /// 处理自动消耗品 - 已移除本地实现
        /// </summary>
        [Obsolete("本地库存系统已移除，请使用服务器API")]
        public void ProcessAutoConsumables(Player character)
        {
            // 本地消耗品系统已移除
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 触发状态改变事件
        /// </summary>
        public void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }

        #endregion
    }
}