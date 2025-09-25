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

/// <summary>
/// 角色基本信息DTO
/// </summary>
public class CharacterDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Gold { get; set; }
    public bool IsDead { get; set; }
    public double RevivalTimeRemaining { get; set; }
    public string CurrentAction { get; set; } = "Idle";
    public string SelectedBattleProfession { get; set; } = "Warrior";
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// 角色详细信息DTO
/// </summary>
public class CharacterDetailsDto : CharacterDto
{
    public Dictionary<string, int> BattleProfessionXP { get; set; } = new();
    public Dictionary<string, int> GatheringProfessionXP { get; set; } = new();
    public Dictionary<string, int> ProductionProfessionXP { get; set; } = new();
    public List<string> LearnedSharedSkills { get; set; } = new();
    public Dictionary<string, List<string>> EquippedSkills { get; set; } = new();
    public Dictionary<string, int> Reputation { get; set; } = new();
    public List<string> CompletedQuestIds { get; set; } = new();
    public Dictionary<string, int> QuestProgress { get; set; } = new();
}

/// <summary>
/// 创建角色请求
/// </summary>
public class CreateCharacterRequest
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 角色经验值更新请求
/// </summary>
public class AddExperienceRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string ProfessionType { get; set; } = string.Empty; // "Battle", "Gathering", "Production"
    public string Profession { get; set; } = string.Empty;
    public int Amount { get; set; }
}

/// <summary>
/// 角色状态更新请求
/// </summary>
public class UpdateCharacterStatusRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// 组队数据传输对象
/// </summary>
public class PartyDto
{
    public Guid Id { get; set; }
    public string CaptainId { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public int MaxMembers { get; set; } = 5;
}

/// <summary>
/// 创建组队请求
/// </summary>
public class CreatePartyRequest
{
    public string CharacterId { get; set; } = string.Empty;
}

/// <summary>
/// 加入组队请求
/// </summary>
public class JoinPartyRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
}

/// <summary>
/// 离开组队请求
/// </summary>
public class LeavePartyRequest
{
    public string CharacterId { get; set; } = string.Empty;
}

// ====== 生产系统 DTOs ======

/// <summary>
/// 采集节点 DTO
/// </summary>
public class GatheringNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double GatheringTimeSeconds { get; set; }
    public string ResultingItemId { get; set; } = string.Empty;
    public int ResultingItemQuantity { get; set; } = 1;
    public int XpReward { get; set; }
    public string RequiredProfession { get; set; } = string.Empty; // Mining, Herbalist, Fishing
    public int RequiredLevel { get; set; }
    public string? RequiredMonsterId { get; set; }
}

/// <summary>
/// 开始采集请求
/// </summary>
public class StartGatheringRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
}

/// <summary>
/// 采集状态 DTO
/// </summary>
public class GatheringStateDto
{
    public string CharacterId { get; set; } = string.Empty;
    public string? CurrentNodeId { get; set; }
    public double RemainingTimeSeconds { get; set; }
    public bool IsGathering { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EstimatedCompletionTime { get; set; }
}

/// <summary>
/// 采集完成结果
/// </summary>
public class GatheringResultDto
{
    public bool Success { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int XpGained { get; set; }
    public bool ExtraLoot { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 停止采集请求
/// </summary>
public class StopGatheringRequest
{
    public string CharacterId { get; set; } = string.Empty;
}

/// <summary>
/// 物品数据传输对象
/// </summary>
public class ItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public bool IsStackable { get; set; }
    public int MaxStackSize { get; set; } = 99;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// 物品槽数据传输对象
/// </summary>
public class InventorySlotDto
{
    public int SlotIndex { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Quantity <= 0;
}

/// <summary>
/// 角色库存数据传输对象
/// </summary>
public class InventoryDto
{
    public string CharacterId { get; set; } = string.Empty;
    public List<InventorySlotDto> Slots { get; set; } = new();
    public Dictionary<string, InventorySlotDto> Equipment { get; set; } = new();
    public Dictionary<string, List<InventorySlotDto>> QuickSlots { get; set; } = new();
}

/// <summary>
/// 添加物品请求
/// </summary>
public class AddItemRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// 使用物品请求
/// </summary>
public class UseItemRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int SlotIndex { get; set; } = -1;
}

/// <summary>
/// 装备物品请求
/// </summary>
public class EquipItemRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string EquipmentSlot { get; set; } = string.Empty;
}

/// <summary>
/// 出售物品请求
/// </summary>
public class SellItemRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// 任务数据传输对象
/// </summary>
public class QuestDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Daily, Weekly, Main, etc.
    public string Status { get; set; } = string.Empty; // Available, Active, Completed
    public List<QuestObjectiveDto> Objectives { get; set; } = new();
    public List<QuestRewardDto> Rewards { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// 任务目标数据传输对象
/// </summary>
public class QuestObjectiveDto
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Kill, Collect, Gather, etc.
    public string TargetId { get; set; } = string.Empty;
    public int RequiredCount { get; set; }
    public int CurrentCount { get; set; }
    public bool IsCompleted => CurrentCount >= RequiredCount;
}

/// <summary>
/// 任务奖励数据传输对象
/// </summary>
public class QuestRewardDto
{
    public string Type { get; set; } = string.Empty; // Experience, Gold, Item
    public string? ItemId { get; set; }
    public int Amount { get; set; }
    public string? ProfessionType { get; set; }
}

/// <summary>
/// 角色任务状态数据传输对象
/// </summary>
public class CharacterQuestStatusDto
{
    public string CharacterId { get; set; } = string.Empty;
    public List<QuestDto> ActiveQuests { get; set; } = new();
    public List<QuestDto> CompletedQuests { get; set; } = new();
    public List<QuestDto> AvailableQuests { get; set; } = new();
    public DateTime LastDailyReset { get; set; }
    public DateTime LastWeeklyReset { get; set; }
}

/// <summary>
/// 接受任务请求
/// </summary>
public class AcceptQuestRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string QuestId { get; set; } = string.Empty;
}

/// <summary>
/// 完成任务请求
/// </summary>
public class CompleteQuestRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string QuestId { get; set; } = string.Empty;
}

/// <summary>
/// 更新任务进度请求
/// </summary>
public class UpdateQuestProgressRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string QuestId { get; set; } = string.Empty;
    public string ObjectiveId { get; set; } = string.Empty;
    public int Progress { get; set; }
}