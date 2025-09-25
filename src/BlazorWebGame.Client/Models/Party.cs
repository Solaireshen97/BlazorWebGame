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

    [Obsolete("使用新的战斗系统")]
    public Enemy? CurrentEnemy { get; set; }
}