using BlazorWebGame.Shared.Events;
using StackExchange.Redis;
using System.Text.Json;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// Redis 事件持久化服务 - 实现事件的持久化存储和跨节点同步
/// </summary>
public class RedisEventPersistenceService : IRedisEventPersistence
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisEventPersistenceService> _logger;
    
    private const string EVENT_STREAM_KEY = "game:events";
    private const string EVENT_BATCH_KEY = "game:event_batches";
    private const string EVENT_CHANNEL = "game:event_notifications";
    
    private readonly string _nodeId;
    private readonly SemaphoreSlim _batchSemaphore = new(1, 1);

    public RedisEventPersistenceService(
        IConnectionMultiplexer redis,
        ILogger<RedisEventPersistenceService> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _subscriber = redis.GetSubscriber();
        _logger = logger;
        _nodeId = Environment.MachineName + "-" + Environment.ProcessId;
        
        // 订阅事件通知
        _ = Task.Run(SubscribeToEventNotifications);
    }

    /// <summary>
    /// 持久化单个事件
    /// </summary>
    public async Task<bool> PersistEventAsync(UnifiedEvent eventData)
    {
        try
        {
            var eventJson = JsonSerializer.Serialize(new EventPersistenceData
            {
                Event = eventData,
                NodeId = _nodeId,
                Timestamp = DateTime.UtcNow
            });

            var streamEntry = new NameValueEntry[]
            {
                new("event_type", eventData.Type.ToString()),
                new("event_data", eventJson),
                new("node_id", _nodeId),
                new("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString())
            };

            var messageId = await _database.StreamAddAsync(EVENT_STREAM_KEY, streamEntry);
            
            if (messageId.IsNull)
            {
                _logger.LogError("Failed to add event {EventType} to Redis stream", eventData.Type);
                return false;
            }

            // 发布事件通知
            await _subscriber.PublishAsync(EVENT_CHANNEL, JsonSerializer.Serialize(new
            {
                MessageId = messageId.ToString(),
                EventType = eventData.Type.ToString(),
                NodeId = _nodeId
            }));

            _logger.LogDebug("Event {EventType} persisted with ID: {MessageId}", eventData.Type, messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting event {EventType}", eventData.Type);
            return false;
        }
    }

    /// <summary>
    /// 批量持久化事件
    /// </summary>
    public async Task<int> PersistEventBatchAsync(UnifiedEvent[] events)
    {
        if (events == null || events.Length == 0)
            return 0;

        await _batchSemaphore.WaitAsync();
        try
        {
            var batchId = Guid.NewGuid().ToString();
            var successCount = 0;
            var batchTimestamp = DateTime.UtcNow;

            // 使用事务确保批量操作的原子性
            var transaction = _database.CreateTransaction();
            var tasks = new List<Task<RedisValue>>();

            foreach (var eventData in events)
            {
                try
                {
                    var eventJson = JsonSerializer.Serialize(new EventPersistenceData
                    {
                        Event = eventData,
                        NodeId = _nodeId,
                        Timestamp = batchTimestamp,
                        BatchId = batchId
                    });

                    var streamEntry = new NameValueEntry[]
                    {
                        new("event_type", eventData.Type.ToString()),
                        new("event_data", eventJson),
                        new("node_id", _nodeId),
                        new("batch_id", batchId),
                        new("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString())
                    };

                    tasks.Add(transaction.StreamAddAsync(EVENT_STREAM_KEY, streamEntry));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error preparing event {EventType} for batch persistence", eventData.Type);
                }
            }

            // 执行事务
            if (await transaction.ExecuteAsync())
            {
                successCount = tasks.Count(t => !t.Result.IsNull);
                
                // 记录批次信息
                await _database.HashSetAsync(EVENT_BATCH_KEY, batchId, JsonSerializer.Serialize(new
                {
                    NodeId = _nodeId,
                    EventCount = successCount,
                    Timestamp = batchTimestamp,
                    Events = events.Select(e => e.Type.ToString()).ToArray()
                }));

                // 发布批次通知
                await _subscriber.PublishAsync(EVENT_CHANNEL, JsonSerializer.Serialize(new
                {
                    BatchId = batchId,
                    EventCount = successCount,
                    NodeId = _nodeId
                }));

                _logger.LogDebug("Batch of {Count} events persisted with batch ID: {BatchId}", 
                    successCount, batchId);
            }
            else
            {
                _logger.LogError("Failed to execute batch persistence transaction for {Count} events", events.Length);
            }

            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch event persistence for {Count} events", events.Length);
            return 0;
        }
        finally
        {
            _batchSemaphore.Release();
        }
    }

    /// <summary>
    /// 加载指定时间范围的事件
    /// </summary>
    public async Task<List<UnifiedEvent>> LoadEventsAsync(DateTime startTime, DateTime endTime)
    {
        try
        {
            var startTimestamp = new DateTimeOffset(startTime).ToUnixTimeMilliseconds();
            var endTimestamp = new DateTimeOffset(endTime).ToUnixTimeMilliseconds();

            var events = await _database.StreamRangeAsync(
                EVENT_STREAM_KEY,
                minId: $"{startTimestamp}-0",
                maxId: $"{endTimestamp}-0");

            var result = new List<UnifiedEvent>();

            foreach (var entry in events)
            {
                try
                {
                    var eventDataJson = entry.Values.FirstOrDefault(v => v.Name == "event_data").Value;
                    if (!eventDataJson.IsNull)
                    {
                        var persistenceData = JsonSerializer.Deserialize<EventPersistenceData>(eventDataJson);
                        if (persistenceData?.Event != null)
                        {
                            result.Add(persistenceData.Event);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing event from stream entry {EntryId}", entry.Id);
                }
            }

            _logger.LogDebug("Loaded {Count} events from time range {StartTime} to {EndTime}", 
                result.Count, startTime, endTime);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading events from time range {StartTime} to {EndTime}", startTime, endTime);
            return new List<UnifiedEvent>();
        }
    }

    /// <summary>
    /// 获取最新的事件（用于实时同步）
    /// </summary>
    public async Task<List<UnifiedEvent>> GetLatestEventsAsync(int count = 100)
    {
        try
        {
            var events = await _database.StreamRangeAsync(EVENT_STREAM_KEY, count: count, order: Order.Descending);
            var result = new List<UnifiedEvent>();

            foreach (var entry in events.Reverse()) // 保持时间顺序
            {
                try
                {
                    var eventDataJson = entry.Values.FirstOrDefault(v => v.Name == "event_data").Value;
                    if (!eventDataJson.IsNull)
                    {
                        var persistenceData = JsonSerializer.Deserialize<EventPersistenceData>(eventDataJson);
                        if (persistenceData?.Event != null)
                        {
                            result.Add(persistenceData.Event);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing latest event from stream entry {EntryId}", entry.Id);
                }
            }

            _logger.LogDebug("Retrieved {Count} latest events", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest events");
            return new List<UnifiedEvent>();
        }
    }

    /// <summary>
    /// 清理过期事件
    /// </summary>
    public async Task<long> CleanupExpiredEventsAsync(DateTime cutoffTime)
    {
        try
        {
            var cutoffTimestamp = new DateTimeOffset(cutoffTime).ToUnixTimeMilliseconds();
            var maxId = $"{cutoffTimestamp}-0";

            // 使用 XTRIM 命令清理过期事件
            var removed = await _database.StreamTrimAsync(EVENT_STREAM_KEY, maxLength: 0, useApproximateMaxLength: false);
            
            // 也可以使用时间窗口清理
            // var removed = await _database.ExecuteAsync("XTRIM", EVENT_STREAM_KEY, "MAXLEN", "~", "10000");

            _logger.LogInformation("Cleaned up {Count} expired events before {CutoffTime}", removed, cutoffTime);
            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired events before {CutoffTime}", cutoffTime);
            return 0;
        }
    }

    /// <summary>
    /// 获取事件流统计信息
    /// </summary>
    public async Task<EventStreamStatistics> GetStatisticsAsync()
    {
        try
        {
            var streamInfo = await _database.StreamInfoAsync(EVENT_STREAM_KEY);
            var batchCount = await _database.HashLengthAsync(EVENT_BATCH_KEY);

            var stats = new EventStreamStatistics
            {
                TotalEvents = streamInfo.Length,
                StreamLength = streamInfo.Length,
                TotalBatches = batchCount,
                FirstEventId = streamInfo.FirstId,
                LastEventId = streamInfo.LastId,
                NodeId = _nodeId,
                Timestamp = DateTime.UtcNow
            };

            // 获取最近一小时的事件统计
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentEvents = await LoadEventsAsync(oneHourAgo, DateTime.UtcNow);
            stats.RecentEventCount = recentEvents.Count;

            // 按类型统计
            stats.EventTypeDistribution = recentEvents
                .GroupBy(e => e.Type.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event stream statistics");
            return new EventStreamStatistics
            {
                NodeId = _nodeId,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 订阅事件通知（用于跨节点同步）
    /// </summary>
    private async Task SubscribeToEventNotifications()
    {
        try
        {
            await _subscriber.SubscribeAsync(EVENT_CHANNEL, (channel, message) =>
            {
                try
                {
                    var notification = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
                    if (notification != null && notification.TryGetValue("NodeId", out var nodeId))
                    {
                        // 只处理其他节点的事件
                        if (nodeId.ToString() != _nodeId)
                        {
                            _logger.LogDebug("Received event notification from node: {NodeId}", nodeId);
                            // 这里可以实现跨节点事件同步逻辑
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event notification: {Message}", message);
                }
            });

            _logger.LogInformation("Subscribed to event notifications on channel: {Channel}", EVENT_CHANNEL);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to event notifications");
        }
    }

    /// <summary>
    /// 资源清理
    /// </summary>
    public void Dispose()
    {
        _batchSemaphore?.Dispose();
    }
}

/// <summary>
/// 事件持久化数据结构
/// </summary>
public class EventPersistenceData
{
    public UnifiedEvent Event { get; set; } = null!;
    public string NodeId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? BatchId { get; set; }
}

/// <summary>
/// 事件流统计信息
/// </summary>
public class EventStreamStatistics
{
    public long TotalEvents { get; set; }
    public long StreamLength { get; set; }
    public long TotalBatches { get; set; }
    public long RecentEventCount { get; set; }
    public string FirstEventId { get; set; } = string.Empty;
    public string LastEventId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, int> EventTypeDistribution { get; set; } = new();
}