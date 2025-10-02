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
/// 增强版战斗实体 - 用于战斗数据存储
/// </summary>
public class EnhancedBattleEntity : BaseEntity
{
    public string BattleType { get; set; } = "Normal"; // Normal, Boss, Dungeon, Raid, PvP, Event
    public string Status { get; set; } = "Preparing"; // Preparing, InProgress, Completed, Abandoned
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? PartyId { get; set; }
    public string? DungeonId { get; set; }
    public string? RegionId { get; set; }
    public int WaveNumber { get; set; } = 0;
    public int CurrentTurn { get; set; } = 0;
    public string? CurrentActorId { get; set; }
    public string BattleModeType { get; set; } = "RealTime"; // RealTime, TurnBased, Hybrid
    public string DifficultyLevel { get; set; } = "Normal"; // Easy, Normal, Hard, Heroic
    public string EnvironmentType { get; set; } = "Default"; // 战斗环境，可影响战斗效果
    public string? BattleRulesJson { get; set; } = "{}"; // 特殊规则设置
    public string? WeatherEffectsJson { get; set; } = "{}"; // 天气效果
    public string? TerrainEffectsJson { get; set; } = "{}"; // 地形效果
    public bool IsPrivate { get; set; } = false; // 是否为私人战斗
    public string? InviteCode { get; set; } // 邀请码（用于私人战斗）
    public string? ParticipantsJson { get; set; } = "[]"; // 参与者信息
    public string? EventsJson { get; set; } = "[]"; // 战斗事件记录
    public string? StateJson { get; set; } = "{}"; // 战斗状态
    public string? ResultJson { get; set; } // 战斗结果
}

/// <summary>
/// 增强版战斗参与者实体
/// </summary>
public class EnhancedBattleParticipantEntity : BaseEntity
{
    public string BattleId { get; set; } = string.Empty;
    public string ParticipantType { get; set; } = "Player"; // Player, Enemy, NPC, Summon, Pet
    public string Name { get; set; } = string.Empty;
    public string? SourceId { get; set; } // 关联的角色/怪物ID
    public int Team { get; set; } = 0; // 0=玩家方，1=敌方，可扩展更多队伍
    public int Position { get; set; } = 0; // 战场位置
    public int Level { get; set; } = 1;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int Mana { get; set; } = 100;
    public int MaxMana { get; set; } = 100;
    public int Shield { get; set; } = 0; // 护盾值
    public bool IsAlive { get; set; } = true;
    public DateTime? DeathTime { get; set; }
    public int TurnOrder { get; set; } = 0; // 行动顺序
    public int Initiative { get; set; } = 0; // 先攻值
    public bool HasActedThisTurn { get; set; } = false;
    public string? CombatStatsJson { get; set; } = "{}"; // 战斗属性
    public string? SkillsJson { get; set; } = "[]"; // 可用技能
    public string? BuffsJson { get; set; } = "[]"; // BUFF状态
    public string? DebuffsJson { get; set; } = "[]"; // DEBUFF状态
    public string? EquipmentEffectsJson { get; set; } = "{}"; // 装备效果
    public string? CooldownsJson { get; set; } = "{}"; // 技能冷却
    public string? ResourcesJson { get; set; } = "{}"; // 战斗资源（怒气、能量等）
    public string? AIStrategyJson { get; set; } // AI行为策略
}

/// <summary>
/// 增强版战斗事件实体
/// </summary>
public class EnhancedBattleEventEntity : BaseEntity
{
    public string BattleId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // Attack, Skill, Item, Status, Movement, Environment
    public DateTime Timestamp { get; set; }
    public string? ActorId { get; set; }
    public string? TargetId { get; set; }
    public string? TargetsJson { get; set; } // 多目标
    public int TurnNumber { get; set; } = 0;
    public int SequenceOrder { get; set; } = 0; // 同一回合内的顺序
    public string? SkillId { get; set; }
    public string? ItemId { get; set; }
    public int? Damage { get; set; }
    public int? Healing { get; set; }
    public string? DamageType { get; set; } // Physical, Magical, True
    public bool IsCritical { get; set; } = false;
    public bool IsEvaded { get; set; } = false;
    public bool IsBlocked { get; set; } = false;
    public bool IsInterrupted { get; set; } = false;
    public string? EffectsAppliedJson { get; set; } // 应用的效果
    public string? EventDetailsJson { get; set; } // 事件详情
    public string? VisualEffectsJson { get; set; } // 视觉效果
    public string? AnimationData { get; set; } // 动画数据
}

/// <summary>
/// 增强版战斗结果实体
/// </summary>
public class EnhancedBattleResultEntity : BaseEntity
{
    public string BattleId { get; set; } = string.Empty;
    public string Outcome { get; set; } = "Undecided"; // Victory, Defeat, Draw, Abandoned
    public int Duration { get; set; } = 0; // 战斗时长（秒）
    public int TotalTurns { get; set; } = 0; // 总回合数
    public string WinningTeam { get; set; } = string.Empty;
    public string? SurvivorIdsJson { get; set; } = "[]"; // 幸存者
    public string? MVPId { get; set; } // 最有价值玩家
    public DateTime CompletedAt { get; set; }
    public string? RewardsJson { get; set; } = "{}"; // 战斗奖励
    public string? ExperienceDistributionJson { get; set; } = "{}"; // 经验分配
    public string? ItemDropsJson { get; set; } = "[]"; // 物品掉落
    public string? SpecialRewardsJson { get; set; } = "[]"; // 特殊奖励
    public string? BattleStatisticsJson { get; set; } = "{}"; // 战斗统计
    public string? PlayerPerformanceJson { get; set; } = "{}"; // 玩家表现评分
    public string? StoryProgressionJson { get; set; } // 故事进展
    public string? UnlockedContentJson { get; set; } // 解锁的内容
    public bool SavedForReplay { get; set; } = false; // 是否保存战斗回放
}

/// <summary>
/// 增强版战斗系统配置实体
/// </summary>
public class EnhancedBattleSystemConfigEntity : BaseEntity
{
    public string ConfigType { get; set; } = "Global"; // Global, BattleType, SpecialEvent
    public string? BattleTypeReference { get; set; } // 适用于特定战斗类型
    public string Name { get; set; } = "Default Configuration";
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public string? TurnMechanicsJson { get; set; } = "{}"; // 回合机制设置
    public string? AIMechanicsJson { get; set; } = "{}"; // AI机制
    public string? DamageFormulaJson { get; set; } = "{}"; // 伤害公式
    public string? HealingFormulaJson { get; set; } = "{}"; // 治疗公式
    public string? StatusEffectRulesJson { get; set; } = "{}"; // 状态效果规则
    public string? BattleBalanceSettingsJson { get; set; } = "{}"; // 平衡设置
    public string? DifficultyScalingJson { get; set; } = "{}"; // 难度缩放
    public string? RewardCalculationRulesJson { get; set; } = "{}"; // 奖励计算规则
    public string? SpecialMechanicsJson { get; set; } = "{}"; // 特殊机制
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidUntil { get; set; }
    public string? Description { get; set; }
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