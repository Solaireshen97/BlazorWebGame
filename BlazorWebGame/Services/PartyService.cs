using BlazorWebGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// 负责管理游戏中的队伍系统
    /// </summary>
    public class PartyService
    {
        /// <summary>
        /// 游戏中存在的所有队伍列表
        /// </summary>
        public List<Party> Parties { get; private set; } = new();

        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event Action? OnStateChanged;

        // 所有玩家引用 - 由GameStateService提供
        private List<Player> _allCharacters;

        public PartyService(List<Player> allCharacters)
        {
            _allCharacters = allCharacters;
        }

        /// <summary>
        /// 根据角色ID查找他所在的队伍。
        /// 如果角色不在任何队伍中，则返回 null。
        /// </summary>
        /// <param name="characterId">要查找的角色ID</param>
        /// <returns>角色所在的队伍对象，或 null</returns>
        public Party? GetPartyForCharacter(string characterId)
        {
            return Parties.FirstOrDefault(p => p.MemberIds.Contains(characterId));
        }

        /// <summary>
        /// 创建新队伍，指定角色将成为队长
        /// </summary>
        /// <param name="character">要创建队伍的角色</param>
        /// <returns>成功创建返回true，否则返回false</returns>
        public bool CreateParty(Player character)
        {
            // 安全检查：确保角色存在且不在任何队伍中
            if (character == null || GetPartyForCharacter(character.Id) != null)
            {
                return false;
            }

            // 创建一个新的队伍实例
            var newParty = new Party
            {
                CaptainId = character.Id,
                MemberIds = new List<string> { character.Id } // 队长自己也是队伍的第一个成员
            };

            // 将新队伍添加到队伍列表中
            Parties.Add(newParty);
            
            // 通知状态变化
            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// 让指定角色加入一个队伍
        /// </summary>
        /// <param name="character">要加入队伍的角色</param>
        /// <param name="partyId">目标队伍的ID</param>
        /// <returns>成功加入返回true，否则返回false</returns>
        public bool JoinParty(Player character, Guid partyId)
        {
            // 安全检查：角色不能为空且不能已经在队伍中
            if (character == null || GetPartyForCharacter(character.Id) != null)
            {
                return false;
            }

            // 查找目标队伍
            var partyToJoin = Parties.FirstOrDefault(p => p.Id == partyId);
            if (partyToJoin == null)
            {
                return false; // 队伍不存在
            }

            // 检查队伍是否已满
            if (partyToJoin.MemberIds.Count >= Party.MaxMembers)
            {
                return false; // 队伍已满，无法加入
            }

            // 执行加入操作
            partyToJoin.MemberIds.Add(character.Id);
            
            // 通知状态变化
            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// 让指定角色离开他所在的队伍
        /// </summary>
        /// <param name="character">要离开队伍的角色</param>
        /// <returns>成功离开返回true，否则返回false</returns>
        public bool LeaveParty(Player character)
        {
            // 安全检查：角色不能为空
            if (character == null) return false;
            
            // 查找角色所在的队伍
            var party = GetPartyForCharacter(character.Id);
            if (party == null)
            {
                return false; // 角色不在任何队伍中
            }

            // 判断是队长离开还是成员离开
            if (party.CaptainId == character.Id)
            {
                // 如果是队长离开，则解散整个队伍
                Parties.Remove(party);
            }
            else
            {
                // 如果只是普通成员离开，则从成员列表中移除自己
                party.MemberIds.Remove(character.Id);
            }
            
            // 通知状态变化
            NotifyStateChanged();
            return true;
        }
        
        /// <summary>
        /// 停止队伍中所有成员的当前战斗状态
        /// </summary>
        public void StopPartyAction(Party party)
        {
            if (party == null) return;
            
            // 清除队伍的战斗目标
            party.CurrentEnemy = null;
        }

        /// <summary>
        /// 设置所有角色列表
        /// </summary>
        public void SetAllCharacters(List<Player> characters)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));
            
            _allCharacters = characters;
        }

        /// <summary>
        /// 触发状态变更事件
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}