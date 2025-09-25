using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 服务端组队模型
/// </summary>
public class ServerParty
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CaptainId { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public int MaxMembers { get; set; } = 5;

    /// <summary>
    /// 检查组队是否已满
    /// </summary>
    public bool IsFull => MemberIds.Count >= MaxMembers;

    /// <summary>
    /// 检查角色是否是队长
    /// </summary>
    public bool IsCaptain(string characterId) => CaptainId == characterId;

    /// <summary>
    /// 检查角色是否在队伍中
    /// </summary>
    public bool HasMember(string characterId) => MemberIds.Contains(characterId);

    /// <summary>
    /// 添加成员到队伍
    /// </summary>
    public bool AddMember(string characterId)
    {
        if (IsFull || HasMember(characterId))
        {
            return false;
        }

        MemberIds.Add(characterId);
        LastUpdated = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// 从队伍移除成员
    /// </summary>
    public bool RemoveMember(string characterId)
    {
        if (!HasMember(characterId))
        {
            return false;
        }

        MemberIds.Remove(characterId);
        LastUpdated = DateTime.UtcNow;

        // 如果队长离开，解散队伍
        if (CaptainId == characterId)
        {
            MemberIds.Clear();
            return true;
        }

        return true;
    }

    /// <summary>
    /// 获取有效的队伍成员数量
    /// </summary>
    public int GetMemberCount() => MemberIds.Count;
}