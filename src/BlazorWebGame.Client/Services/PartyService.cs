using BlazorWebGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// 简化的队伍服务 - 仅保留UI状态管理，所有队伍逻辑由服务器处理
    /// </summary>
    public class PartyService
    {
        /// <summary>
        /// 游戏中存在的所有队伍列表 - UI展示用
        /// </summary>
        public List<Party> Parties { get; private set; } = new();

        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action? OnStateChanged;

        // 所有角色引用 - 由GameStateService提供
        private List<Player> _allCharacters;

        public PartyService(List<Player> allCharacters)
        {
            _allCharacters = allCharacters;
        }

        #region 队伍查询 - 保留基础UI支持

        /// <summary>
        /// 根据角色ID查找其所在的队伍。
        /// 如果角色不在任何队伍中，则返回 null。
        /// </summary>
        /// <param name="characterId">要查找的角色ID</param>
        /// <returns>角色所在的队伍对象，或 null</returns>
        public Party? GetPartyForCharacter(string characterId)
        {
            return Parties.FirstOrDefault(p => p.MemberIds.Contains(characterId));
        }

        #endregion

        #region 队伍管理 - 已移除本地实现

        /// <summary>
        /// 创建新队伍 - 已移除本地实现
        /// </summary>
        [Obsolete("本地队伍系统已移除，请使用服务器API")]
        public bool CreateParty(Player character)
        {
            // 本地队伍系统已移除
            return false;
        }

        /// <summary>
        /// 加入队伍 - 已移除本地实现
        /// </summary>
        [Obsolete("本地队伍系统已移除，请使用服务器API")]
        public bool JoinParty(Player character, Guid partyId)
        {
            // 本地队伍系统已移除
            return false;
        }

        /// <summary>
        /// 离开队伍 - 已移除本地实现
        /// </summary>
        [Obsolete("本地队伍系统已移除，请使用服务器API")]
        public bool LeaveParty(Player character)
        {
            // 本地队伍系统已移除
            return false;
        }

        /// <summary>
        /// 踢出队员 - 已移除本地实现
        /// </summary>
        [Obsolete("本地队伍系统已移除，请使用服务器API")]
        public bool KickFromParty(string kickerId, string targetId)
        {
            // 本地队伍系统已移除
            return false;
        }

        /// <summary>
        /// 转让队长 - 已移除本地实现
        /// </summary>
        [Obsolete("本地队伍系统已移除，请使用服务器API")]
        public bool TransferLeadership(string currentLeaderId, string newLeaderId)
        {
            // 本地队伍系统已移除
            return false;
        }

        /// <summary>
        /// 解散队伍 - 已移除本地实现
        /// </summary>
        [Obsolete("本地队伍系统已移除，请使用服务器API")]
        public bool DisbandParty(string leaderId, Party party)
        {
            // 本地队伍系统已移除
            return false;
        }

        /// <summary>
        /// 设置所有角色 - 已移除本地实现
        /// </summary>
        [Obsolete("本地队伍系统已移除，请使用服务器API")]
        public void SetAllCharacters(List<Player> characters)
        {
            // 本地队伍系统已移除
            _allCharacters = characters;
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