using BlazorWebGame.Refactored.Infrastructure.Events.Core;

namespace BlazorWebGame.Refactored.Domain.Events;

// ========== 战斗命令 ==========
public record StartBattleCommand(string CharacterId, string EnemyId) : IGameEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => "Command.StartBattle";
    public Dictionary<string, object> Metadata { get; } = new();
}

public record EndBattleCommand(Guid BattleId) : IGameEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => "Command.EndBattle";
    public Dictionary<string, object> Metadata { get; } = new();
}

public record AttackCommand(Guid BattleId, string AttackerId, string TargetId) : IGameEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => "Command.Attack";
    public Dictionary<string, object> Metadata { get; } = new();
}

public record UseSkillCommand(Guid BattleId, string AttackerId, string TargetId, string SkillId) : IGameEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => "Command.UseSkill";
    public Dictionary<string, object> Metadata { get; } = new();
}

// ========== 活动命令 ==========
public record StartActivityCommand(string CharacterId, EventActivityType ActivityType, string? TargetId = null) : IGameEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => "Command.StartActivity";
    public Dictionary<string, object> Metadata { get; } = new();
}

public record StopActivityCommand(Guid ActivityId, string CharacterId) : IGameEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => "Command.StopActivity";
    public Dictionary<string, object> Metadata { get; } = new();
}

// ========== 物品命令 ==========
public record UseItemCommand(string CharacterId, string ItemId, int Quantity = 1) : IGameEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => "Command.UseItem";
    public Dictionary<string, object> Metadata { get; } = new();
}

public record EquipItemCommand(string CharacterId, string ItemId, EquipmentSlot Slot) : IGameEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => "Command.EquipItem";
    public Dictionary<string, object> Metadata { get; } = new();
}

// ========== 角色命令 ==========
public record CreateCharacterCommand(string Name, string UserId) : IGameEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => "Command.CreateCharacter";
    public Dictionary<string, object> Metadata { get; } = new();
}

public record SelectCharacterCommand(string CharacterId, string UserId) : IGameEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => "Command.SelectCharacter";
    public Dictionary<string, object> Metadata { get; } = new();
}