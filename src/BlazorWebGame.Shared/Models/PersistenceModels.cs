using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 事件存储
/// </summary>
public class EventStore
{
    private readonly List<StoredEvent> _events = new();
    private readonly Dictionary<string, int> _aggregateVersions = new();

    /// <summary>
    /// 追加事件
    /// </summary>
    public void Append(IDomainEvent domainEvent)
    {
        var aggregateId = domainEvent.AggregateId;
        var version = GetNextVersion(aggregateId);

        var storedEvent = new StoredEvent
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateVersion = version,
            EventType = domainEvent.EventType,
            EventData = SerializeEvent(domainEvent),
            Timestamp = domainEvent.OccurredAt
        };

        _events.Add(storedEvent);
        _aggregateVersions[aggregateId] = version;
    }

    /// <summary>
    /// 获取聚合事件流
    /// </summary>
    public List<StoredEvent> GetEvents(string aggregateId, int? fromVersion = null)
    {
        return _events
            .Where(e => e.AggregateId == aggregateId)
            .Where(e => !fromVersion.HasValue || e.AggregateVersion > fromVersion.Value)
            .OrderBy(e => e.AggregateVersion)
            .ToList();
    }

    private int GetNextVersion(string aggregateId)
    {
        return _aggregateVersions.GetValueOrDefault(aggregateId, 0) + 1;
    }

    private string SerializeEvent(IDomainEvent domainEvent)
    {
        // TODO: 实现JSON序列化
        return System.Text.Json.JsonSerializer.Serialize(domainEvent);
    }
}

/// <summary>
/// 存储的事件
/// </summary>
public class StoredEvent
{
    public Guid Id { get; set; }
    public string AggregateId { get; set; } = string.Empty;
    public int AggregateVersion { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 快照存储
/// </summary>
public class SnapshotStore
{
    private readonly Dictionary<string, List<Snapshot>> _snapshots = new();
    private const int MaxSnapshotsPerAggregate = 5;

    /// <summary>
    /// 保存快照
    /// </summary>
    public void Save(Snapshot snapshot)
    {
        if (!_snapshots.ContainsKey(snapshot.AggregateId))
        {
            _snapshots[snapshot.AggregateId] = new List<Snapshot>();
        }

        var list = _snapshots[snapshot.AggregateId];
        list.Add(snapshot);

        // 保留最近的N个快照
        if (list.Count > MaxSnapshotsPerAggregate)
        {
            list.RemoveAt(0);
        }
    }

    /// <summary>
    /// 获取最新快照
    /// </summary>
    public Snapshot? GetLatest(string aggregateId)
    {
        if (!_snapshots.ContainsKey(aggregateId))
            return null;

        return _snapshots[aggregateId].LastOrDefault();
    }
}

/// <summary>
/// 快照
/// </summary>
public class Snapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AggregateId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Data { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 聚合重建器
/// </summary>
public class AggregateRebuilder
{
    private readonly EventStore _eventStore;
    private readonly SnapshotStore _snapshotStore;

    public AggregateRebuilder(EventStore eventStore, SnapshotStore snapshotStore)
    {
        _eventStore = eventStore;
        _snapshotStore = snapshotStore;
    }

    /// <summary>
    /// 重建角色状态
    /// </summary>
    public Character RebuildCharacter(string characterId)
    {
        // 获取最新快照
        var snapshot = _snapshotStore.GetLatest(characterId);

        Character character;
        int fromVersion = 0;

        if (snapshot != null)
        {
            // 从快照恢复
            character = DeserializeCharacter(snapshot.Data);
            fromVersion = snapshot.Version;
        }
        else
        {
            // 创建新角色（需要初始事件）
            character = new Character("Unknown");
        }

        // 应用快照后的事件
        var events = _eventStore.GetEvents(characterId, fromVersion);
        foreach (var evt in events)
        {
            ApplyEvent(character, evt);
        }

        return character;
    }

    private Character DeserializeCharacter(string data)
    {
        // TODO: 实现反序列化
        return new Character("Restored");
    }

    private void ApplyEvent(Character character, StoredEvent evt)
    {
        // TODO: 根据事件类型应用状态变更
        switch (evt.EventType)
        {
            case "LevelUp":
                // character.Level++;
                break;
            case "GoldGained":
                // character.Gold += amount;
                break;
                // ... 其他事件类型
        }
    }
}