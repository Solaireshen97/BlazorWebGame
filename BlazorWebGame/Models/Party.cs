using System;
using System.Collections.Generic;

namespace BlazorWebGame.Models;

/// <summary>
/// 代表一个玩家队伍
/// </summary>
public class Party
{
    // --- vvv 添加队伍上限常量 vvv ---
    /// <summary>
    /// 队伍的最大成员数。
    /// </summary>
    public const int MaxMembers = 5;
    // --- ^^^ 添加结束 ^^^ ---

    /// <summary>
    /// 队伍的唯一ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 队长的角色ID
    /// </summary>
    public required string CaptainId { get; set; }

    /// <summary>
    /// 队伍中所有成员的角色ID列表
    /// </summary>
    public List<string> MemberIds { get; set; } = new();
}