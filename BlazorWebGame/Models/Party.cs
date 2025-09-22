using System;
using System.Collections.Generic;

namespace BlazorWebGame.Models;

/// <summary>
/// 代表一个玩家队伍
/// </summary>
public class Party
{
    public const int MaxMembers = 5;

    public Guid Id { get; set; } = Guid.NewGuid();

    public required string CaptainId { get; set; }

    public List<string> MemberIds { get; set; } = new();

    // --- vvv 添加这一行 vvv ---
    /// <summary>
    /// 队伍当前正在集中攻击的敌人。
    /// 如果为 null，则表示队伍未处于战斗状态。
    /// </summary>
    public Enemy? CurrentEnemy { get; set; }
    // --- ^^^ 添加结束 ^^^ ---
}