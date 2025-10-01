using BlazorWebGame.Shared.DTOs.Skill;
using System;
using System.Collections.Generic;
namespace BlazorWebGame.Shared.DTOs;

/// <summary>
/// 用户数据传输对象
/// </summary>
public class UserStorageDto
{
    // 基本用户信息
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false; 

    // 登录相关信息
    public DateTime LastLoginAt { get; set; } = DateTime.MinValue;
    public string LastLoginIp { get; set; } = string.Empty;

    // 安全信息
    public int LoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastPasswordChange { get; set; }
    public List<string> LoginHistory { get; set; } = new();
    public List<string> Roles { get; set; } = new() { "Player" };

    // 密码哈希 - 数据库存储需要，但不会返回给客户端
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;

    // 个人资料信息
    public string DisplayName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public Dictionary<string, object> CustomProperties { get; set; } = new();

    // 用户拥有的游戏角色ID
    public List<string> CharacterIds { get; set; } = new();

    // 审计信息
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 角色数据存储传输对象
/// </summary>
public class CharacterStorageDto
{
    // 基本信息
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int Gold { get; set; } = 0;
    public bool IsOnline { get; set; } = false;
    public string? CurrentRegionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public Guid? PartyId { get; set; }

    // 生命值和法力值
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int Mana { get; set; } = 100;
    public int MaxMana { get; set; } = 100;
    public bool IsDead { get; set; } = false;
    public DateTime? DeathTime { get; set; }

    // 基础属性
    public int Strength { get; set; } = 10;
    public int Agility { get; set; } = 10;
    public int Intellect { get; set; } = 10;
    public int Spirit { get; set; } = 10;
    public int Stamina { get; set; } = 10;
    public int AttributePoints { get; set; } = 0;

    // 职业信息
    public string ProfessionId { get; set; } = "Warrior"; // 当前选择的战斗职业
    public Dictionary<string, ProfessionLevelDto> BattleProfessions { get; set; } = new();
    public Dictionary<string, ProfessionLevelDto> GatheringProfessions { get; set; } = new();
    public Dictionary<string, ProfessionLevelDto> ProductionProfessions { get; set; } = new();

    // 背包
    public List<InventoryItemDto> Items { get; set; } = new();
    public Dictionary<string, string> EquippedItems { get; set; } = new(); // slot -> itemId

    // 消耗品装载
    public List<ConsumableSlotDto> GeneralConsumableSlots { get; set; } = new();
    public List<ConsumableSlotDto> CombatConsumableSlots { get; set; } = new();

    // 声望系统
    public Dictionary<string, int> Reputations { get; set; } = new();

    // 任务系统
    public List<string> ActiveQuestIds { get; set; } = new();
    public List<string> CompletedQuestIds { get; set; } = new();
    public Dictionary<string, int> QuestProgress { get; set; } = new();

    // 技能系统
    public Dictionary<string, LearnedSkillDto> LearnedSkills { get; set; } = new();
    public Dictionary<string, List<string>> EquippedSkills { get; set; } = new();

    // 活动系统
    public List<ActivitySlotDto> ActivitySlots { get; set; } = new();

    // 离线记录
    public OfflineRecordDto? LastOfflineRecord { get; set; }
}

/// <summary>
/// 职业等级DTO
/// </summary>
public class ProfessionLevelDto
{
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
}

/// <summary>
/// 背包物品DTO
/// </summary>
public class InventoryItemDto
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// 消耗品槽位DTO
/// </summary>
public class ConsumableSlotDto
{
    public string SlotId { get; set; } = string.Empty;
    public string? ItemId { get; set; }
    public string UsePolicy { get; set; } = "OnStart";
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// 已学习技能DTO
/// </summary>
public class LearnedSkillDto
{
    // 基本信息
    public string SkillId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    // 等级信息
    public int CurrentLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 10;
    public int ExperienceToNextLevel { get; set; } = 100;
    public bool IsMaxLevel => CurrentLevel >= MaxLevel;

    // 当前等级的实际数值
    public SkillCostDto CurrentCost { get; set; } = new();
    public SkillEffectsDto CurrentEffects { get; set; } = new();
    public TimeSpan CurrentCooldown { get; set; } = TimeSpan.FromSeconds(5);
    public double CurrentRange { get; set; } = 5.0;
    public double CurrentCastTime { get; set; } = 1.0;

    // 下一级预览（如果不是满级）
    public SkillCostDto? NextLevelCost { get; set; }
    public SkillEffectsDto? NextLevelEffects { get; set; }

    // 使用统计
    public int TimesUsed { get; set; } = 0;
    public DateTime LearnedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public long TotalDamageDealt { get; set; } = 0;
    public long TotalHealingDone { get; set; } = 0;
}


/// <summary>
/// 活动槽位DTO
/// </summary>
public class ActivitySlotDto
{
    public int SlotIndex { get; set; }  // 这里与引用参数中的 Index 不一致
    public bool IsUnlocked { get; set; } = true;  // 缺失属性
    public ActivityPlanDto? CurrentPlan { get; set; }
    public List<ActivityPlanDto> Queue { get; set; } = new();  // 缺失属性
    public int MaxQueueSize { get; set; } = 1;  // 缺失属性
}

/// <summary>
/// 活动计划DTO
/// </summary>
public class ActivityPlanDto
{
    public string Id { get; set; } = string.Empty;
    public string ActivityId { get; set; } = string.Empty;
    public string ActivityName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    // 时间相关
    public DateTime? StartTime { get; set; }  // 主要使用字段
    public DateTime? StartedAt { get; set; }  // 向后兼容
    public DateTime? EndTime { get; set; }    // 主要使用字段
    public DateTime? CompletedAt { get; set; } // 向后兼容

    // 状态和进度
    public string State { get; set; } = "Pending";
    public int? RepeatCount { get; set; }
    public TimeSpan? Duration { get; set; }
    public double Progress { get; set; } = 0.0;
    public ProgressInfoDto? ProgressInfo { get; set; }

    // 参数和限制
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, object> Payload { get; set; } = new(); // 向后兼容
    public LimitSpecDto Limit { get; set; } = new();
}

/// <summary>
/// 活动进度信息DTO
/// </summary>
public class ProgressInfoDto
{
    public double Current { get; set; } = 0;
    public double Total { get; set; } = 100;
    public double Percentage => Total > 0 ? (Current / Total * 100) : 0;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastUpdateTime { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// 限制规格DTO
/// </summary>
public class LimitSpecDto
{
    public string Type { get; set; } = "Time";
    public double Value { get; set; } = 0;
    public double Remaining { get; set; } = 0;
}

/// <summary>
/// 离线记录DTO
/// </summary>
public class OfflineRecordDto
{
    public DateTime OfflineAt { get; set; }
    public CharacterSnapshotDto? CharacterState { get; set; }
    public List<ActivityPlanDto> ActivePlans { get; set; } = new();
}

/// <summary>
/// 角色快照DTO
/// </summary>
public class CharacterSnapshotDto
{
    public string CharacterId { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Experience { get; set; }
    public int Gold { get; set; }
    public string? CurrentRegionId { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }
    public int Strength { get; set; }
    public int Agility { get; set; }
    public int Intellect { get; set; }
    public int Spirit { get; set; }
    public int Stamina { get; set; }
    public DateTime Timestamp { get; set; }
}

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
/// 用户角色关联数据传输对象
/// </summary>
public class UserCharacterStorageDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;

    // 新增: 角色槽位索引
    public int SlotIndex { get; set; } = 0;

    // 新增: 记录角色所在职业
    public string ProfessionName { get; set; } = "Warrior";

    // 新增: 角色等级，用于显示在角色选择界面
    public int Level { get; set; } = 1;

    // 时间相关信息
    public DateTime LastPlayedAt { get; set; } = DateTime.UtcNow;
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