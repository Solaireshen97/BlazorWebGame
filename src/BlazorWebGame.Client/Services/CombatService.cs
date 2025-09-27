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
            // 简化构造函数，移除所有本地战斗逻辑
        }

        #region 战斗状态查询接口 - 移除本地实现，需要时可调用服务器API

        /// <summary>
        /// 获取活跃战斗上下文 - 已移除本地实现
        /// </summary>
        public BattleContext? GetBattleContextForPlayer(string playerId)
        {
            // 本地战斗系统已移除，返回null
            // 如需战斗状态，请使用服务器API
            return null;
        }

        /// <summary>
        /// 获取活跃战斗上下文 - 已移除本地实现
        /// </summary>
        public BattleContext? GetBattleContextForParty(Guid partyId)
        {
            // 本地战斗系统已移除，返回null
            // 如需战斗状态，请使用服务器API
            return null;
        }

        /// <summary>
        /// 检查玩家是否在战斗刷新状态 - 已移除本地实现
        /// </summary>
        public bool IsPlayerInBattleRefresh(string playerId)
        {
            // 本地战斗系统已移除
            return false;
        }

        /// <summary>
        /// 获取玩家战斗刷新剩余时间 - 已移除本地实现
        /// </summary>
        public double GetPlayerBattleRefreshTime(string playerId)
        {
            // 本地战斗系统已移除
            return 0;
        }

        #endregion

        #region 战斗处理 - 已移除本地实现

        /// <summary>
        /// 处理所有活跃战斗 - 已移除本地实现
        /// </summary>
        public void ProcessAllBattles(double elapsedSeconds)
        {
            // 本地战斗处理已移除，所有战斗逻辑由服务器处理
        }

        /// <summary>
        /// 处理角色的战斗（旧方法） - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void ProcessCombat(Player character, double elapsedSeconds, Party? party)
        {
            // 本地战斗处理已移除
        }

        #endregion

        #region 战斗控制 - 已移除本地实现

        /// <summary>
        /// 智能开始战斗 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public bool SmartStartBattle(Player character, Enemy enemyTemplate, Party? party = null, bool ignoreRefreshCheck = false)
        {
            // 本地战斗系统已移除
            return false;
        }

        /// <summary>
        /// 开始战斗（旧方法） - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void StartCombat(Player character, Enemy enemyTemplate, Party? party)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 开始副本战斗 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public bool StartDungeon(Party party, string dungeonId)
        {
            // 本地战斗系统已移除
            return false;
        }

        /// <summary>
        /// 开始多敌人通关战斗 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public bool StartMultiEnemyBattle(Player character, List<Enemy> enemies, Party? party = null)
        {
            // 本地战斗系统已移除
            return false;
        }

        /// <summary>
        /// 停止战斗 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void StopBattle(BattleContext battleContext)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 停止玩家的战斗 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void StopPlayerBattle(string playerId)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 停止队伍战斗 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void StopPartyBattle(Guid partyId)
        {
            // 本地战斗系统已移除
        }

        #endregion

        #region 角色技能和职业方法 - 已移除本地实现

        /// <summary>
        /// 设置战斗职业 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void SetBattleProfession(Player? character, BattleProfession profession)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 装备技能 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void EquipSkill(Player? character, string skillId, int maxEquippedSkills)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 卸下技能 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void UnequipSkill(Player? character, string skillId)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 取消玩家战斗刷新 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void CancelPlayerBattleRefresh(string playerId)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 设置所有角色 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void SetAllCharacters(List<Player> characters)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 复活角色 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void ReviveCharacter(Player character)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 检查新技能解锁 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void CheckForNewSkillUnlocks(Player character)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 检查新技能解锁（重载方法） - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void CheckForNewSkillUnlocks(Player character, BattleProfession profession, int level, bool isLevelUp)
        {
            // 本地战斗系统已移除
        }

        /// <summary>
        /// 重置玩家技能冷却 - 已移除本地实现
        /// </summary>
        [Obsolete("本地战斗系统已移除，请使用服务器API")]
        public void ResetPlayerSkillCooldowns(Player character)
        {
            // 本地战斗系统已移除
        }

        #endregion

        #region 角色状态管理 - 已移除本地实现

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