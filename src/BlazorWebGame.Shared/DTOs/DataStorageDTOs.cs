using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.DTOs;

/// <summary>
/// 玩家数据传输对象
/// </summary>
public class PlayerStorageDto
{
    public string Id { get; set; } = string.Empty;
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // 复杂属性 - 在传输时已经反序列化
    public Dictionary<string, object> Attributes { get; set; } = new();
    public List<object> Inventory { get; set; } = new();
    public List<string> Skills { get; set; } = new();
    public Dictionary<string, string> Equipment { get; set; } = new();
}

/// <summary>
/// 队伍数据传输对象
/// </summary>
public class TeamStorageDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CaptainId { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = new();
    public int MaxMembers { get; set; } = 5;
    public string Status { get; set; } = "Active";
    public string? CurrentBattleId { get; set; }
    public DateTime LastBattleAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 动作目标数据传输对象
/// </summary>
public class ActionTargetStorageDto
{
    public string Id { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public double Progress { get; set; } = 0.0;
    public double Duration { get; set; } = 0.0;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; } = false;
    public Dictionary<string, object> ProgressData { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 战斗记录数据传输对象
/// </summary>
public class BattleRecordStorageDto
{
    public string Id { get; set; } = string.Empty;
    public string BattleId { get; set; } = string.Empty;
    public string BattleType { get; set; } = "Normal";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = "InProgress";
    public List<string> Participants { get; set; } = new();
    public List<object> Enemies { get; set; } = new();
    public List<object> Actions { get; set; } = new();
    public Dictionary<string, object> Results { get; set; } = new();
    public Guid? PartyId { get; set; }
    public string? DungeonId { get; set; }
    public int WaveNumber { get; set; } = 0;
    public int Duration { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 离线数据传输对象
/// </summary>
public class OfflineDataStorageDto
{
    public string Id { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime SyncedAt { get; set; } = DateTime.MinValue;
    public bool IsSynced { get; set; } = false;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 数据存储查询参数
/// </summary>
public class DataStorageQueryDto
{
    public string? PlayerId { get; set; }
    public string? TeamId { get; set; }
    public string? BattleId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// 批量操作请求
/// </summary>
public class BatchOperationRequestDto<T>
{
    public List<T> Items { get; set; } = new();
    public string Operation { get; set; } = "Create"; // Create, Update, Delete
    public bool IgnoreErrors { get; set; } = false;
}

/// <summary>
/// 批量操作响应
/// </summary>
public class BatchOperationResponseDto<T>
{
    public List<T> SuccessfulItems { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
}