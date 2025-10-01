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
/// 用户实体 - 用于用户账号数据存储
/// </summary>
public class UserEntity : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false;
    public DateTime LastLoginAt { get; set; } = DateTime.MinValue;
    public string LastLoginIp { get; set; } = string.Empty;
    public int LoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastPasswordChange { get; set; }
    public string RolesJson { get; set; } = "[]";
    public string ProfileJson { get; set; } = "{}";
    public string LoginHistoryJson { get; set; } = "[]"; // 新增字段 - 登录历史
    public string CharacterIdsJson { get; set; } = "[]"; // 新增字段 - 角色ID列表
    public string DisplayName { get; set; } = string.Empty; // 新增字段 - 显示名称
    public string Avatar { get; set; } = string.Empty; // 新增字段 - 头像
}

/// <summary>
/// 角色实体 - 用于游戏角色数据存储
/// </summary>
public class CharacterEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int Gold { get; set; } = 0;
    public bool IsOnline { get; set; } = false;
    public string? CurrentRegionId { get; set; }
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
    public string ProfessionId { get; set; } = "Warrior";

    // JSON序列化的复杂数据
    public string BattleProfessionsJson { get; set; } = "{}";
    public string GatheringProfessionsJson { get; set; } = "{}";
    public string ProductionProfessionsJson { get; set; } = "{}";
    public string ReputationsJson { get; set; } = "{}";
    public string InventoryJson { get; set; } = "[]";
    public string EquipmentJson { get; set; } = "{}";
    public string LearnedSkillsJson { get; set; } = "{}"; // 替换 SkillsJson
    public string EquippedSkillsJson { get; set; } = "{}"; // 新增
    public string ActiveQuestsJson { get; set; } = "[]"; // 替换 QuestsJson
    public string CompletedQuestsJson { get; set; } = "[]"; // 新增
    public string QuestProgressJson { get; set; } = "{}"; // 新增
    public string OfflineRecordJson { get; set; } = "{}";

    public string GeneralConsumableSlotsJson { get; set; } = "[]"; // 新增
    public string CombatConsumableSlotsJson { get; set; } = "[]"; // 新增
    public string ActivitySlotsJson { get; set; } = "[]"; // 新增
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
/// 用户角色关联实体 - 建立用户与游戏角色的关系
/// </summary>
public class UserCharacterEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;

    // 新增字段
    public int SlotIndex { get; set; } = 0;
    public string ProfessionName { get; set; } = "Warrior";
    public int Level { get; set; } = 1;

    public DateTime LastPlayedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
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