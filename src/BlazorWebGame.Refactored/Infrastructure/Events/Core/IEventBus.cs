namespace BlazorWebGame.Refactored.Infrastructure.Events.Core;

/// <summary>
/// 事件总线接口
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IGameEvent;
    
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) 
        where TEvent : IGameEvent;
    
    void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) 
        where TEvent : IGameEvent;
    
    void SubscribeSync<TEvent>(Action<TEvent> handler) 
        where TEvent : IGameEvent;
    
    void UnsubscribeSync<TEvent>(Action<TEvent> handler) 
        where TEvent : IGameEvent;
}