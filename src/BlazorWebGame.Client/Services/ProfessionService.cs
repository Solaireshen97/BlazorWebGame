using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using System;
using System.Collections.Generic;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// 简化的专业技能服务 - 仅保留UI状态管理，所有生产逻辑由服务器处理
    /// </summary>
    public class ProfessionService
    {
        private readonly InventoryService _inventoryService;
        private readonly QuestService _questService;

        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action? OnStateChanged;

        public ProfessionService(InventoryService inventoryService, QuestService questService)
        {
            _inventoryService = inventoryService;
            _questService = questService;
        }

        #region 采集系统 - 已移除本地实现

        /// <summary>
        /// 处理采集 - 已移除本地实现
        /// </summary>
        [Obsolete("本地生产系统已移除，请使用服务器API")]
        public void ProcessGathering(Player character, double elapsedSeconds)
        {
            // 本地采集处理已移除，所有采集逻辑由服务器处理
        }

        /// <summary>
        /// 开始采集 - 已移除本地实现
        /// </summary>
        [Obsolete("本地生产系统已移除，请使用服务器API")]
        public void StartGathering(Player character, GatheringNode node)
        {
            // 本地采集系统已移除
        }

        /// <summary>
        /// 停止采集 - 已移除本地实现
        /// </summary>
        [Obsolete("本地生产系统已移除，请使用服务器API")]
        public void StopGathering(Player character)
        {
            // 本地采集系统已移除
        }

        /// <summary>
        /// 获取当前采集时间 - 已移除本地实现
        /// </summary>
        [Obsolete("本地生产系统已移除，请使用服务器API")]
        public double GetCurrentGatheringTime(Player character)
        {
            // 本地采集系统已移除
            return 0;
        }

        #endregion

        #region 制作系统 - 已移除本地实现

        /// <summary>
        /// 处理制作 - 已移除本地实现
        /// </summary>
        [Obsolete("本地生产系统已移除，请使用服务器API")]
        public void ProcessCrafting(Player character, double elapsedSeconds)
        {
            // 本地制作处理已移除，所有制作逻辑由服务器处理
        }

        /// <summary>
        /// 开始制作 - 已移除本地实现
        /// </summary>
        [Obsolete("本地生产系统已移除，请使用服务器API")]
        public void StartCrafting(Player character, Recipe recipe)
        {
            // 本地制作系统已移除
        }

        /// <summary>
        /// 停止制作 - 已移除本地实现
        /// </summary>
        [Obsolete("本地生产系统已移除，请使用服务器API")]
        public void StopCrafting(Player character)
        {
            // 本地制作系统已移除
        }

        /// <summary>
        /// 获取当前制作时间 - 已移除本地实现
        /// </summary>
        [Obsolete("本地生产系统已移除，请使用服务器API")]
        public double GetCurrentCraftingTime(Player character)
        {
            // 本地制作系统已移除
            return 0;
        }

        /// <summary>
        /// 批量制作 - 已移除本地实现
        /// </summary>
        [Obsolete("本地生产系统已移除，请使用服务器API")]
        public void StartBatchCrafting(Player character, Recipe recipe, int quantity)
        {
            // 本地制作系统已移除
        }

        /// <summary>
        /// 停止当前动作 - 已移除本地实现
        /// </summary>
        [Obsolete("本地生产系统已移除，请使用服务器API")]
        public void StopCurrentAction(Player character)
        {
            // 本地制作系统已移除
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