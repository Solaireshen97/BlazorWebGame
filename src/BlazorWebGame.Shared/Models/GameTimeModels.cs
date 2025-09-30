using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 游戏时钟 - 统一时间管理
/// </summary>
public class GameClock
{
    private DateTime _currentTime;
    private readonly object _lock = new();

    public DateTime CurrentTime
    {
        get { lock (_lock) return _currentTime; }
    }

    public GameClock(DateTime? initialTime = null)
    {
        _currentTime = initialTime ?? DateTime.UtcNow;
    }

    /// <summary>
    /// 推进时间到指定时刻
    /// </summary>
    public void AdvanceTo(DateTime targetTime)
    {
        lock (_lock)
        {
            if (targetTime > _currentTime)
                _currentTime = targetTime;
        }
    }

    /// <summary>
    /// 推进指定时间间隔
    /// </summary>
    public void Advance(TimeSpan duration)
    {
        lock (_lock)
        {
            _currentTime = _currentTime.Add(duration);
        }
    }

    /// <summary>
    /// 获取时间戳（用于事件排序）
    /// </summary>
    public long GetTimestamp()
    {
        return CurrentTime.Ticks;
    }
}

/// <summary>
/// 事件调度器 - 基于优先队列的事件管理
/// </summary>
public class EventScheduler
{
    private readonly SortedSet<ScheduledEvent> _eventQueue = new(new EventTimeComparer());
    private readonly GameClock _gameClock;
    private readonly object _lock = new();

    public EventScheduler(GameClock gameClock)
    {
        _gameClock = gameClock;
    }

    /// <summary>
    /// 调度事件
    /// </summary>
    public void Schedule(IGameEvent gameEvent, DateTime triggerTime)
    {
        lock (_lock)
        {
            var scheduledEvent = new ScheduledEvent(gameEvent, triggerTime);
            _eventQueue.Add(scheduledEvent);
        }
    }

    /// <summary>
    /// 调度延迟事件
    /// </summary>
    public void ScheduleDelayed(IGameEvent gameEvent, TimeSpan delay)
    {
        Schedule(gameEvent, _gameClock.CurrentTime.Add(delay));
    }

    /// <summary>
    /// 获取并移除下一个事件
    /// </summary>
    public ScheduledEvent? PopNext()
    {
        lock (_lock)
        {
            if (_eventQueue.Count == 0)
                return null;

            var next = _eventQueue.Min;
            _eventQueue.Remove(next);
            return next;
        }
    }

    /// <summary>
    /// 查看下一个事件但不移除
    /// </summary>
    public ScheduledEvent? PeekNext()
    {
        lock (_lock)
        {
            return _eventQueue.Count > 0 ? _eventQueue.Min : null;
        }
    }

    /// <summary>
    /// 取消特定类型的所有事件
    /// </summary>
    public void CancelEvents<T>() where T : IGameEvent
    {
        lock (_lock)
        {
            var toRemove = _eventQueue.Where(e => e.Event is T).ToList();
            foreach (var evt in toRemove)
            {
                _eventQueue.Remove(evt);
            }
        }
    }

    public int Count => _eventQueue.Count;
}

/// <summary>
/// 调度事件包装
/// </summary>
public class ScheduledEvent
{
    public IGameEvent Event { get; }
    public DateTime TriggerTime { get; }
    public Guid Id { get; } = Guid.NewGuid();

    public ScheduledEvent(IGameEvent gameEvent, DateTime triggerTime)
    {
        Event = gameEvent;
        TriggerTime = triggerTime;
    }
}

/// <summary>
/// 事件时间比较器
/// </summary>
public class EventTimeComparer : IComparer<ScheduledEvent>
{
    public int Compare(ScheduledEvent? x, ScheduledEvent? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        var timeCompare = x.TriggerTime.CompareTo(y.TriggerTime);
        if (timeCompare != 0) return timeCompare;

        // 时间相同时用ID保证稳定排序
        return x.Id.CompareTo(y.Id);
    }
}

/// <summary>
/// 游戏事件基础接口
/// </summary>
public interface IGameEvent
{
    string EventType { get; }
    DateTime Timestamp { get; }
    void Execute(IGameContext context);
}

/// <summary>
/// 游戏上下文接口
/// </summary>
public interface IGameContext
{
    GameClock Clock { get; }
    EventScheduler Scheduler { get; }
    void EmitDomainEvent(IDomainEvent domainEvent);
}