using BlazorWebGame.Refactored.Domain.ValueObjects;

namespace BlazorWebGame.Refactored.Domain.Events;

/// <summary>
/// 领域事件接口
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}

/// <summary>
/// 领域事件基类
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public abstract string EventType { get; }
}

/// <summary>
/// 活动开始事件
/// </summary>
public record ActivityStartedEvent(
    Guid CharacterId,
    Guid ActivityId,
    ActivityType ActivityType
) : DomainEvent
{
    public override string EventType => "ActivityStarted";
}

/// <summary>
/// 活动取消事件
/// </summary>
public record ActivityCancelledEvent(
    Guid CharacterId,
    Guid ActivityId
) : DomainEvent
{
    public override string EventType => "ActivityCancelled";
}

/// <summary>
/// 活动完成事件
/// </summary>
public record ActivityCompletedEvent(
    Guid CharacterId,
    Guid ActivityId,
    ActivityType ActivityType,
    ActivityResult Result
) : DomainEvent
{
    public override string EventType => "ActivityCompleted";
}

/// <summary>
/// 角色升级事件
/// </summary>
public record CharacterLevelUpEvent(
    Guid CharacterId,
    int OldLevel,
    int NewLevel
) : DomainEvent
{
    public override string EventType => "CharacterLevelUp";
}

/// <summary>
/// 角色属性更新事件
/// </summary>
public record CharacterStatsUpdatedEvent(
    Guid CharacterId,
    CharacterStats NewStats
) : DomainEvent
{
    public override string EventType => "CharacterStatsUpdated";
}

/// <summary>
/// 战斗开始事件
/// </summary>
public record BattleStartedEvent(
    Guid CharacterId,
    Guid BattleId,
    Guid EnemyId,
    DateTime StartTime
) : DomainEvent
{
    public override string EventType => "BattleStarted";
}

/// <summary>
/// 战斗结束事件
/// </summary>
public record BattleEndedEvent(
    Guid CharacterId,
    Guid BattleId,
    bool Victory,
    ActivityResult Result
) : DomainEvent
{
    public override string EventType => "BattleEnded";
}

/// <summary>
/// 经验获得事件
/// </summary>
public record ExperienceGainedEvent(
    Guid CharacterId,
    BigNumber Amount,
    string Source
) : DomainEvent
{
    public override string EventType => "ExperienceGained";
}

/// <summary>
/// 物品获得事件
/// </summary>
public record ItemObtainedEvent(
    Guid CharacterId,
    string ItemId,
    int Quantity,
    string Source
) : DomainEvent
{
    public override string EventType => "ItemObtained";
}

/// <summary>
/// 金币变化事件
/// </summary>
public record GoldChangedEvent(
    Guid CharacterId,
    BigNumber OldAmount,
    BigNumber NewAmount,
    string Reason
) : DomainEvent
{
    public override string EventType => "GoldChanged";
}

/// <summary>
/// 资源消耗事件
/// </summary>
public record ResourceConsumedEvent(
    Guid CharacterId,
    string ResourceType,
    int Amount,
    string Purpose
) : DomainEvent
{
    public override string EventType => "ResourceConsumed";
}

/// <summary>
/// 技能使用事件
/// </summary>
public record SkillUsedEvent(
    Guid CharacterId,
    string SkillId,
    Guid? TargetId,
    bool Success
) : DomainEvent
{
    public override string EventType => "SkillUsed";
}

/// <summary>
/// 任务进度更新事件
/// </summary>
public record QuestProgressUpdatedEvent(
    Guid CharacterId,
    string QuestId,
    int OldProgress,
    int NewProgress
) : DomainEvent
{
    public override string EventType => "QuestProgressUpdated";
}

/// <summary>
/// 任务完成事件
/// </summary>
public record QuestCompletedEvent(
    Guid CharacterId,
    string QuestId,
    List<ItemReward> Rewards
) : DomainEvent
{
    public override string EventType => "QuestCompleted";
}

/// <summary>
/// 制作完成事件
/// </summary>
public record CraftingCompletedEvent(
    Guid CharacterId,
    string RecipeId,
    int Quantity,
    bool Success,
    List<ItemReward> Results
) : DomainEvent
{
    public override string EventType => "CraftingCompleted";
}

/// <summary>
/// 采集完成事件
/// </summary>
public record GatheringCompletedEvent(
    Guid CharacterId,
    GatheringType GatheringType,
    string NodeId,
    List<ItemReward> Results
) : DomainEvent
{
    public override string EventType => "GatheringCompleted";
}

/// <summary>
/// 角色创建事件
/// </summary>
public record CharacterCreatedEvent(
    Guid CharacterId,
    string CharacterName,
    CharacterClass CharacterClass,
    Guid UserId
) : DomainEvent
{
    public override string EventType => "CharacterCreated";
}

/// <summary>
/// 角色删除事件
/// </summary>
public record CharacterDeletedEvent(
    Guid CharacterId,
    Guid UserId,
    string Reason
) : DomainEvent
{
    public override string EventType => "CharacterDeleted";
}

/// <summary>
/// 角色登录事件
/// </summary>
public record CharacterLoginEvent(
    Guid CharacterId,
    string IpAddress,
    DateTime LoginTime
) : DomainEvent
{
    public override string EventType => "CharacterLogin";
}

/// <summary>
/// 角色登出事件
/// </summary>
public record CharacterLogoutEvent(
    Guid CharacterId,
    TimeSpan SessionDuration
) : DomainEvent
{
    public override string EventType => "CharacterLogout";
}

/// <summary>
/// 系统通知事件
/// </summary>
public record SystemNotificationEvent(
    Guid CharacterId,
    string Message,
    NotificationType Type,
    Dictionary<string, object>? Data = null
) : DomainEvent
{
    public override string EventType => "SystemNotification";
}

/// <summary>
/// 通知类型枚举
/// </summary>
public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success,
    Achievement
}