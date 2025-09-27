using BlazorWebGame.Refactored.Infrastructure.Events.Core;

namespace BlazorWebGame.Refactored.Domain.Events;

// ========== 新事件驱动架构的事件定义 ==========
// ========== 游戏生命周期事件 ==========
public class GameInitializedEvent : EventDrivenDomainEvent
{
    public override string Type => "Game.Initialized";
    public string Version { get; init; } = "1.0.0";
}

public class GameReadyEvent : EventDrivenDomainEvent
{
    public override string Type => "Game.Ready";
}

public class GameTickEvent : EventDrivenDomainEvent
{
    public override string Type => "Game.Tick";
    public double DeltaTime { get; init; }
    public long TickNumber { get; init; }
}

// ========== 角色事件 ==========
public class NewCharacterCreatedEvent : EventDrivenDomainEvent
{
    public override string Type => "Character.Created";
    public required string CharacterId { get; init; }
    public required string Name { get; init; }
}

public class NewCharacterLeveledUpEvent : EventDrivenDomainEvent
{
    public override string Type => "Character.LeveledUp";
    public required string CharacterId { get; init; }
    public int OldLevel { get; init; }
    public int NewLevel { get; init; }
    public int AttributePoints { get; init; }
}

public class NewCharacterDiedEvent : EventDrivenDomainEvent
{
    public override string Type => "Character.Died";
    public required string CharacterId { get; init; }
    public string? KilledBy { get; init; }
}

// ========== 战斗事件 ==========
public class NewBattleStartedEvent : EventDrivenDomainEvent
{
    public override string Type => "Battle.Started";
    public required Guid BattleId { get; init; }
    public required string CharacterId { get; init; }
    public required string EnemyId { get; init; }
}

public class NewBattleEndedEvent : EventDrivenDomainEvent
{
    public override string Type => "Battle.Ended";
    public required Guid BattleId { get; init; }
    public required BattleResult Result { get; init; }
    public List<Reward> Rewards { get; init; } = new();
}

public class DamageDealtEvent : EventDrivenDomainEvent
{
    public override string Type => "Battle.DamageDealt";
    public required string AttackerId { get; init; }
    public required string TargetId { get; init; }
    public int Damage { get; init; }
    public bool IsCritical { get; init; }
}

// ========== 活动事件 ==========
public class NewActivityStartedEvent : EventDrivenDomainEvent
{
    public override string Type => "Activity.Started";
    public required Guid ActivityId { get; init; }
    public required string CharacterId { get; init; }
    public required EventActivityType ActivityType { get; init; }
    public TimeSpan Duration { get; init; }
}

public class NewActivityCompletedEvent : EventDrivenDomainEvent
{
    public override string Type => "Activity.Completed";
    public required Guid ActivityId { get; init; }
    public required string CharacterId { get; init; }
    public List<Reward> Rewards { get; init; } = new();
}

public class ActivityProgressEvent : EventDrivenDomainEvent
{
    public override string Type => "Activity.Progress";
    public required Guid ActivityId { get; init; }
    public required string CharacterId { get; init; }
    public double Progress { get; init; }
    public TimeSpan RemainingTime { get; init; }
}

// ========== 物品事件 ==========
public class ItemAcquiredEvent : EventDrivenDomainEvent
{
    public override string Type => "Item.Acquired";
    public required string CharacterId { get; init; }
    public required string ItemId { get; init; }
    public int Quantity { get; init; }
    public string? Source { get; init; }
}

public class ItemEquippedEvent : EventDrivenDomainEvent
{
    public override string Type => "Item.Equipped";
    public required string CharacterId { get; init; }
    public required string ItemId { get; init; }
    public required EquipmentSlot Slot { get; init; }
}

public class ItemUsedEvent : EventDrivenDomainEvent
{
    public override string Type => "Item.Used";
    public required string CharacterId { get; init; }
    public required string ItemId { get; init; }
    public int Quantity { get; init; }
}

// ========== 值对象 ==========
public record BattleResult(bool Victory, int ExperienceGained, int GoldGained);
public record Reward(string Type, string Id, int Quantity);

public enum EventActivityType 
{ 
    Mining, 
    Fishing, 
    Crafting, 
    Gathering,
    Battle,
    Quest
}

public enum EquipmentSlot 
{ 
    Head, 
    Chest, 
    Legs, 
    Feet, 
    MainHand, 
    OffHand,
    Ring,
    Necklace
}