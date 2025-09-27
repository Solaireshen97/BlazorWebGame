using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Refactored.Infrastructure.Events.Core;

/// <summary>
/// 高性能事件总线实现
/// </summary>
public sealed class EventBus : IEventBus, IDisposable
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly Channel<(IGameEvent Event, TaskCompletionSource<bool> Completion)> _eventChannel;
    private readonly ILogger<EventBus> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _processingTask;
    private readonly SemaphoreSlim _handlerLock = new(1, 1);

    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger;
        
        // 创建无界通道以获得最佳性能
        _eventChannel = Channel.CreateUnbounded<(IGameEvent, TaskCompletionSource<bool>)>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        
        // 启动后台处理任务
        _processingTask = ProcessEventsAsync(_cancellationTokenSource.Token);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IGameEvent
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));
        
        var tcs = new TaskCompletionSource<bool>();
        
        await _eventChannel.Writer.WriteAsync((@event, tcs), cancellationToken);
        
        await tcs.Task;
        
        _logger.LogDebug("Event {EventType} with ID {EventId} published successfully", 
            @event.Type, @event.Id);
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) 
        where TEvent : IGameEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        _handlerLock.Wait();
        try
        {
            var eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _handlers[eventType] = handlers;
            }
            
            handlers.Add(handler);
            _logger.LogDebug("Handler subscribed for event type {EventType}", eventType.Name);
        }
        finally
        {
            _handlerLock.Release();
        }
    }

    public void SubscribeSync<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        // 将同步处理器包装为异步
        Func<TEvent, CancellationToken, Task> asyncHandler = (evt, ct) =>
        {
            handler(evt);
            return Task.CompletedTask;
        };
        
        Subscribe(asyncHandler);
    }

    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) 
        where TEvent : IGameEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        _handlerLock.Wait();
        try
        {
            var eventType = typeof(TEvent);
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    _handlers.TryRemove(eventType, out _);
                }
            }
        }
        finally
        {
            _handlerLock.Release();
        }
    }

    public void UnsubscribeSync<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
    {
        // 由于我们包装了同步处理器，这里简化处理
        // 实际应用中可能需要更复杂的跟踪机制
    }

    private async Task ProcessEventsAsync(CancellationToken cancellationToken)
    {
        await foreach (var (gameEvent, completion) in _eventChannel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await ProcessSingleEventAsync(gameEvent, cancellationToken);
                completion.SetResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventType}", gameEvent.Type);
                completion.SetException(ex);
            }
        }
    }

    private async Task ProcessSingleEventAsync(IGameEvent gameEvent, CancellationToken cancellationToken)
    {
        var eventType = gameEvent.GetType();
        var handlers = GetHandlersForEvent(eventType);
        
        if (!handlers.Any())
        {
            _logger.LogDebug("No handlers found for event type {EventType}", eventType.Name);
            return;
        }
        
        var tasks = handlers.Select(handler => InvokeHandlerAsync(handler, gameEvent, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private List<Delegate> GetHandlersForEvent(Type eventType)
    {
        var handlers = new List<Delegate>();
        
        // 获取直接订阅的处理器
        if (_handlers.TryGetValue(eventType, out var directHandlers))
        {
            handlers.AddRange(directHandlers);
        }
        
        // 获取基类和接口的处理器
        foreach (var kvp in _handlers)
        {
            if (kvp.Key.IsAssignableFrom(eventType) && kvp.Key != eventType)
            {
                handlers.AddRange(kvp.Value);
            }
        }
        
        return handlers;
    }

    private async Task InvokeHandlerAsync(Delegate handler, IGameEvent gameEvent, CancellationToken cancellationToken)
    {
        try
        {
            var handlerType = handler.GetType();
            var genericArgs = handlerType.GetGenericArguments();
            
            if (genericArgs.Length > 0 && genericArgs[0].IsAssignableFrom(gameEvent.GetType()))
            {
                await (Task)handler.DynamicInvoke(gameEvent, cancellationToken)!;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking handler for event {EventType}", gameEvent.Type);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _eventChannel.Writer.Complete();
        _processingTask.Wait(TimeSpan.FromSeconds(5));
        _cancellationTokenSource.Dispose();
        _handlerLock.Dispose();
    }
}