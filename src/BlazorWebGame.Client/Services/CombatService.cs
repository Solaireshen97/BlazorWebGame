using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Battles;
using BlazorWebGame.Models.Dungeons;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Models.Skills;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// 简化的战斗服务 - 仅保留UI状态管理，所有战斗逻辑由服务器处理
    /// </summary>
    public class CombatService
    {
        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action? OnStateChanged;

        public CombatService(
            InventoryService inventoryService,
            List<Player> allCharacters)
        {
            // 简化构造函数，本地战斗逻辑已移除
        }

        #region 战斗状态查询接口 - 本地实现已移除，需要时调用服务器API

        /// <summary>
        /// 获取活跃战斗上下文 - 本地实现已移除
        /// </summary>
        public BattleContext? GetBattleContextForPlayer(string playerId)
        {
            // 本地战斗系统已移除，返回null
            // 如需战斗状态，请使用服务器API
            return null;
        }

        /// <summary>
        /// 获取活跃战斗上下文 - 本地实现已移除
        /// </summary>
        public BattleContext? GetBattleContextForParty(Guid partyId)
        {
            // 本地战斗系统已移除，返回null
            // 如需战斗状态，请使用服务器API
            return null;
        }

        /// <summary>
        /// 检查玩家是否在战斗刷新状态 - 本地实现已移除
        /// </summary>
        public bool IsPlayerInBattleRefresh(string playerId)
        {
            // 本地战斗系统已移除
            return false;
        }

        /// <summary>
        /// 获取玩家战斗刷新剩余时间 - 本地实现已移除
        /// </summary>
        public double GetPlayerBattleRefreshTime(string playerId)
        {
            // 本地战斗系统已移除
            return 0;
        }

        /// <summary>
        /// 检查新技能解锁 - 本地实现已移除
        /// </summary>
        public void CheckForNewSkillUnlocks(Player player, BattleProfession profession, int level, bool forceCheck)
        {
            // 本地战斗系统已移除，技能解锁由服务器处理
        }

        /// <summary>
        /// 重置玩家技能冷却 - 本地实现已移除
        /// </summary>
        public void ResetPlayerSkillCooldowns(Player player)
        {
            // 本地战斗系统已移除，技能冷却由服务器处理
        }

        #endregion

        #region 战斗处理 - 本地实现已移除，全部由服务器处理

        /// <summary>
        /// 处理所有活跃战斗 - 本地实现已移除
        /// </summary>
        public void ProcessAllBattles(double elapsedSeconds)
        {
            // 本地战斗处理已移除，所有战斗逻辑由服务器处理
        }

        /// <summary>
        /// 处理角色的战斗（旧方法） - 本地实现已移除
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void ProcessCombat(Player character, double elapsedSeconds, Party? party)
        {
            // 本地战斗处理已移除
        }

        #endregion

        #region 向后兼容的方法 - 保留以避免编译错误

        /// <summary>
        /// 为兼容性保留的方法，设置角色列表引用
        /// </summary>
        public void SetAllCharacters(List<Player> characters)
        {
            // 保留方法签名以避免编译错误
        }

        /// <summary>
        /// 智能开始战斗 - 本地实现已移除
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public bool SmartStartBattle(Player character, Enemy enemyTemplate, Party? party = null, bool ignoreRefreshCheck = false)
        {
            // 本地战斗系统已移除
            return false;
        }

        /// <summary>
        /// 开始副本战斗 - 本地实现已移除
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public bool StartDungeon(Party party, string dungeonId)
        {
            // 本地战斗系统已移除
            return false;
        }

        /// <summary>
        /// 装备技能 - 本地实现已移除
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void EquipSkill(Player? character, string skillId, int maxEquippedSkills)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 卸下技能 - 本地实现已移除
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void UnequipSkill(Player? character, string skillId)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 复活角色 - 本地实现已移除
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void ReviveCharacter(Player character)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 通知状态变更
        /// </summary>
        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }

        #endregion

        // 所有其他本地战斗逻辑已移除
        // 如需战斗功能，请使用服务器API：
        // - GameApiService.StartBattleAsync()
        // - GameApiService.GetBattleStateAsync()
        // - GameApiService.StopBattleAsync()
        // - ClientGameStateService.StartBattleAsync()
    }
}