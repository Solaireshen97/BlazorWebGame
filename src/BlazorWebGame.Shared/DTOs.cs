using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.DTOs;

/// <summary>
/// API响应基类
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

/// <summary>
/// 战斗状态传输对象
/// </summary>
public class BattleStateDto
{
    public Guid BattleId { get; set; }
    public string CharacterId { get; set; } = string.Empty;
    public string EnemyId { get; set; } = string.Empty;
    public string? PartyId { get; set; }
    public bool IsActive { get; set; }
    public int PlayerHealth { get; set; }
    public int PlayerMaxHealth { get; set; }
    public int EnemyHealth { get; set; }
    public int EnemyMaxHealth { get; set; }
    public DateTime LastUpdated { get; set; }
    public BattleType BattleType { get; set; }
    public List<string> PartyMemberIds { get; set; } = new();
}

/// <summary>
/// 开始战斗请求
/// </summary>
public class StartBattleRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string EnemyId { get; set; } = string.Empty;
    public string? PartyId { get; set; }
}

/// <summary>
/// 战斗类型
/// </summary>
public enum BattleType
{
    Normal,
    Dungeon
}

/// <summary>
/// 离线操作类型
/// </summary>
public enum OfflineActionType
{
    StartBattle,
    StopBattle,
    UpdateCharacter
}

/// <summary>
/// 离线操作记录
/// </summary>
public class OfflineAction
{
    public OfflineActionType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public string Data { get; set; } = string.Empty;
}