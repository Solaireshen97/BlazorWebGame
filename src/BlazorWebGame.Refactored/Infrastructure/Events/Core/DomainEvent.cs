namespace BlazorWebGame.Refactored.Infrastructure.Events.Core;

/// <summary>
/// 事件驱动架构的领域事件基类 (不同于现有的Domain.Events)
/// </summary>
public abstract class EventDrivenDomainEvent : IGameEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public abstract string Type { get; }
    public Dictionary<string, object> Metadata { get; } = new();
    
    protected EventDrivenDomainEvent()
    {
        Metadata["EventVersion"] = "1.0";
    }
}