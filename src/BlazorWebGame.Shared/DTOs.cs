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
    
    // Enhanced battle state for server-side logic
    public List<BattleParticipantDto> Players { get; set; } = new();
    public List<BattleParticipantDto> Enemies { get; set; } = new();
    public BattleStatus Status { get; set; } = BattleStatus.Active;
    public Dictionary<string, string> PlayerTargets { get; set; } = new(); // playerId -> enemyName
    public List<BattleActionDto> RecentActions { get; set; } = new();
    public BattleResultDto? Result { get; set; }
}

/// <summary>
/// 战斗参与者（玩家或敌人）
/// </summary>
public class BattleParticipantDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int AttackPower { get; set; }
    public double AttacksPerSecond { get; set; } = 1.0;
    public double AttackCooldown { get; set; }
    public List<string> EquippedSkills { get; set; } = new();
    public Dictionary<string, double> SkillCooldowns { get; set; } = new();
    public bool IsPlayer { get; set; }
}

/// <summary>
/// 战斗动作记录
/// </summary>
public class BattleActionDto
{
    public string ActorId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public BattleActionType ActionType { get; set; }
    public int Damage { get; set; }
    public string? SkillId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsCritical { get; set; }
}

/// <summary>
/// 战斗结果
/// </summary>
public class BattleResultDto
{
    public bool Victory { get; set; }
    public int ExperienceGained { get; set; }
    public int GoldGained { get; set; }
    public List<string> ItemsLooted { get; set; } = new();
    public DateTime CompletedAt { get; set; }
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
/// 战斗动作请求
/// </summary>
public class BattleActionRequest
{
    public Guid BattleId { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public BattleActionType ActionType { get; set; }
    public string? TargetId { get; set; }
    public string? SkillId { get; set; }
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
/// 战斗状态
/// </summary>
public enum BattleStatus
{
    Active,
    Completed,
    Paused
}

/// <summary>
/// 战斗动作类型
/// </summary>
public enum BattleActionType
{
    Attack,
    UseSkill,
    Defend,
    Flee
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