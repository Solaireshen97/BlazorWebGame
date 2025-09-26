using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 数据存储实体基类
/// </summary>
public abstract class BaseEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 玩家实体 - 用于数据存储
/// </summary>
public class PlayerEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int Gold { get; set; } = 0;
    public string SelectedBattleProfession { get; set; } = "Warrior";
    public string CurrentAction { get; set; } = "Idle";
    public string? CurrentActionTargetId { get; set; }
    public Guid? PartyId { get; set; }
    public bool IsOnline { get; set; } = true;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    
    // JSON序列化的复杂属性
    public string AttributesJson { get; set; } = "{}";
    public string InventoryJson { get; set; } = "[]";
    public string SkillsJson { get; set; } = "[]";
    public string EquipmentJson { get; set; } = "{}";
}

/// <summary>
/// 队伍实体 - 用于数据存储
/// </summary>
public class TeamEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string CaptainId { get; set; } = string.Empty;
    public int MaxMembers { get; set; } = 5;
    public string Status { get; set; } = "Active"; // Active, Disbanded, InBattle
    public string MemberIdsJson { get; set; } = "[]";
    public string? CurrentBattleId { get; set; }
    public DateTime LastBattleAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 当前动作目标实体 - 用于数据存储
/// </summary>
public class ActionTargetEntity : BaseEntity
{
    public string PlayerId { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty; // Enemy, GatheringNode, Recipe, Quest
    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty; // Combat, Gathering, Crafting, Quest
    public double Progress { get; set; } = 0.0;
    public double Duration { get; set; } = 0.0;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; } = false;
    public string ProgressDataJson { get; set; } = "{}";
}

/// <summary>
/// 战斗记录实体 - 用于数据存储
/// </summary>
public class BattleRecordEntity : BaseEntity
{
    public string BattleId { get; set; } = Guid.NewGuid().ToString();
    public string BattleType { get; set; } = "Normal"; // Normal, Dungeon, PvP
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = "InProgress"; // InProgress, Victory, Defeat, Abandoned
    public string ParticipantsJson { get; set; } = "[]"; // Player IDs
    public string EnemiesJson { get; set; } = "[]"; // Enemy data
    public string ActionsJson { get; set; } = "[]"; // Battle actions/log
    public string ResultsJson { get; set; } = "{}"; // Rewards, XP, etc.
    public Guid? PartyId { get; set; }
    public string? DungeonId { get; set; }
    public int WaveNumber { get; set; } = 0;
    public int Duration { get; set; } = 0; // in seconds
}

/// <summary>
/// 离线数据实体 - 用于离线模式数据存储
/// </summary>
public class OfflineDataEntity : BaseEntity
{
    public string PlayerId { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty; // PlayerProgress, BattleState, TeamState
    public string DataJson { get; set; } = "{}";
    public DateTime SyncedAt { get; set; } = DateTime.MinValue;
    public bool IsSynced { get; set; } = false;
    public int Version { get; set; } = 1;
}