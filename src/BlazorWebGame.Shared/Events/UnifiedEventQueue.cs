using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

namespace BlazorWebGame.Shared.Events
{
    /// <summary>
    /// 统一事件队列系统 - 基于优先级的多队列架构
    /// 实现分层优先级事件队列系统(LPEQ)
    /// </summary>
    public class UnifiedEventQueue : IDisposable
    {
        // 每个优先级的环形缓冲区
        private readonly LockFreeRingBuffer[] _priorityRings;
        private readonly EventPool _eventPool;
        private readonly EventDispatcher _dispatcher;
        
        // 帧同步
        private long _currentFrame;
        private readonly object _frameLock = new();
        
        // 配置
        private readonly UnifiedEventQueueConfig _config;
        
        // 统计信息
        private long _totalEnqueued;
        private long _totalDequeued;
        private long _totalDropped;
        
        private volatile bool _disposed;

        public long CurrentFrame => Volatile.Read(ref _currentFrame);
        public EventDispatcher Dispatcher => _dispatcher;
        public EventPool Pool => _eventPool;

        /// <summary>
        /// 创建统一事件队列
        /// </summary>
        public UnifiedEventQueue(UnifiedEventQueueConfig? config = null)
        {
            _config = config ?? UnifiedEventQueueConfig.Default;
            
            // 初始化优先级队列
            _priorityRings = new LockFreeRingBuffer[4];
            _priorityRings[0] = new LockFreeRingBuffer(_config.GameplayQueueSize);    // Gameplay
            _priorityRings[1] = new LockFreeRingBuffer(_config.AIQueueSize);         // AI
            _priorityRings[2] = new LockFreeRingBuffer(_config.AnalyticsQueueSize);  // Analytics
            _priorityRings[3] = new LockFreeRingBuffer(_config.TelemetryQueueSize);  // Telemetry
            
            // 初始化事件池
            _eventPool = new EventPool(_config.InitialPoolSize, _config.MaxPoolSize);
            
            // 初始化分发器
            _dispatcher = new EventDispatcher(this, _config);
        }

        /// <summary>
        /// 入队事件 - 快速路径
        /// </summary>
        /// <param name="evt">事件</param>
        /// <returns>是否成功入队</returns>
        public bool Enqueue(ref UnifiedEvent evt)
        {
            if (_disposed)
                return false;

            // 设置帧号
            evt.Frame = (ulong)CurrentFrame;

            var priority = (EventPriority)evt.Priority;
            var ring = _priorityRings[(int)priority];

            if (ring.TryEnqueue(ref evt))
            {
                Interlocked.Increment(ref _totalEnqueued);
                return true;
            }

            // 队列满，应用背压策略
            return HandleBackpressure(ref evt, priority);
        }

        /// <summary>
        /// 便利方法：创建并入队事件
        /// </summary>
        public bool EnqueueEvent(ushort eventType, EventPriority priority = EventPriority.Gameplay, 
            ulong actorId = 0, ulong targetId = 0)
        {
            var evt = new UnifiedEvent(eventType, priority)
            {
                ActorId = actorId,
                TargetId = targetId
            };
            return Enqueue(ref evt);
        }

        /// <summary>
        /// 便利方法：创建并入队带数据的事件
        /// </summary>
        public bool EnqueueEvent<T>(ushort eventType, T data, EventPriority priority = EventPriority.Gameplay,
            ulong actorId = 0, ulong targetId = 0) where T : unmanaged
        {
            var evt = new UnifiedEvent(eventType, priority)
            {
                ActorId = actorId,
                TargetId = targetId
            };
            evt.SetData(data);
            return Enqueue(ref evt);
        }

        /// <summary>
        /// 收集当前帧的所有事件
        /// </summary>
        /// <param name="events">输出事件数组</param>
        /// <param name="maxEvents">最大事件数量</param>
        /// <returns>收集到的事件数量</returns>
        public int CollectFrameEvents(UnifiedEvent[] events, int maxEvents)
        {
            if (_disposed || events == null || maxEvents <= 0)
                return 0;

            var collected = 0;
            var remaining = Math.Min(maxEvents, events.Length);

            // 按优先级收集事件：Gameplay > AI > Analytics > Telemetry
            for (int priority = 0; priority < _priorityRings.Length && remaining > 0; priority++)
            {
                var ring = _priorityRings[priority];
                var batch = Math.Min(remaining, _config.MaxBatchSize);
                
                var tempEvents = new UnifiedEvent[batch];
                var count = ring.TryDequeueBatch(tempEvents, batch);
                
                if (count > 0)
                {
                    Array.Copy(tempEvents, 0, events, collected, count);
                    collected += count;
                    remaining -= count;
                    Interlocked.Add(ref _totalDequeued, count);
                }
            }

            return collected;
        }

        /// <summary>
        /// 推进到下一帧
        /// </summary>
        public void AdvanceFrame()
        {
            lock (_frameLock)
            {
                Interlocked.Increment(ref _currentFrame);
            }
        }

        /// <summary>
        /// 获取队列统计信息
        /// </summary>
        public QueueStatistics GetStatistics()
        {
            var stats = new QueueStatistics
            {
                CurrentFrame = CurrentFrame,
                TotalEnqueued = _totalEnqueued,
                TotalDequeued = _totalDequeued,
                TotalDropped = _totalDropped,
                QueueDepths = new int[_priorityRings.Length]
            };

            for (int i = 0; i < _priorityRings.Length; i++)
            {
                stats.QueueDepths[i] = _priorityRings[i].Count;
            }

            stats.PoolStatistics = _eventPool.GetStatistics();
            return stats;
        }

        /// <summary>
        /// 处理背压情况
        /// </summary>
        private bool HandleBackpressure(ref UnifiedEvent evt, EventPriority priority)
        {
            switch (priority)
            {
                case EventPriority.Telemetry:
                    // 遥测事件直接丢弃
                    Interlocked.Increment(ref _totalDropped);
                    return false;

                case EventPriority.Analytics:
                    // 分析事件降级处理或限流
                    if (_config.EnableAnalyticsThrottling)
                    {
                        // 简单的令牌桶限流（这里简化为随机丢弃）
                        if (Random.Shared.NextDouble() < 0.5)
                        {
                            Interlocked.Increment(ref _totalDropped);
                            return false;
                        }
                    }
                    break;

                case EventPriority.AI:
                case EventPriority.Gameplay:
                    // 高优先级事件尝试等待一小段时间
                    Thread.SpinWait(100); // 短暂自旋等待
                    var ring = _priorityRings[(int)priority];
                    if (ring.TryEnqueue(ref evt))
                    {
                        Interlocked.Increment(ref _totalEnqueued);
                        return true;
                    }
                    break;
            }

            Interlocked.Increment(ref _totalDropped);
            return false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _dispatcher?.Dispose();
            _eventPool?.Dispose();
        }
    }

    /// <summary>
    /// 统一事件队列配置
    /// </summary>
    public class UnifiedEventQueueConfig
    {
        public int GameplayQueueSize { get; set; } = 8192;
        public int AIQueueSize { get; set; } = 4096;
        public int AnalyticsQueueSize { get; set; } = 2048;
        public int TelemetryQueueSize { get; set; } = 1024;
        
        public int InitialPoolSize { get; set; } = 1024;
        public int MaxPoolSize { get; set; } = 4096;
        
        public int MaxBatchSize { get; set; } = 256;
        public int FrameIntervalMs { get; set; } = 16; // 60 FPS
        
        public bool EnableAnalyticsThrottling { get; set; } = true;
        public bool EnableRedisIntegration { get; set; } = false;
        public string? RedisConnectionString { get; set; }

        public static UnifiedEventQueueConfig Default => new();
    }

    /// <summary>
    /// 队列统计信息
    /// </summary>
    public struct QueueStatistics
    {
        public long CurrentFrame;
        public long TotalEnqueued;
        public long TotalDequeued;
        public long TotalDropped;
        public int[] QueueDepths;
        public PoolStatistics PoolStatistics;

        public double DropRate => TotalEnqueued > 0 ? (double)TotalDropped / TotalEnqueued : 0.0;
        public int TotalQueueDepth 
        {
            get
            {
                var total = 0;
                foreach (var depth in QueueDepths)
                    total += depth;
                return total;
            }
        }

        public override string ToString()
        {
            return $"Frame: {CurrentFrame}, Enqueued: {TotalEnqueued}, " +
                   $"Dequeued: {TotalDequeued}, Dropped: {TotalDropped} ({DropRate:P2}), " +
                   $"Queue Depths: [{string.Join(", ", QueueDepths)}]";
        }
    }
}