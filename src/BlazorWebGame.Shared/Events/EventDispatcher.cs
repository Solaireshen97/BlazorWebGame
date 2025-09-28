using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BlazorWebGame.Shared.Events
{
    /// <summary>
    /// 事件分发器 - 管理事件处理和工作线程池
    /// 实现帧同步处理和批量事件分发
    /// </summary>
    public class EventDispatcher : IDisposable
    {
        private readonly UnifiedEventQueue _eventQueue;
        private readonly UnifiedEventQueueConfig _config;
        private readonly ConcurrentDictionary<ushort, List<IEventHandler>> _handlers;
        private readonly WorkerPool _workerPool;
        private readonly Timer _frameTimer;
        private readonly object _dispatchLock = new();
        
        // 性能监控
        private readonly PerformanceCounters _perfCounters;
        
        // 帧缓冲区
        private readonly UnifiedEvent[] _frameBuffer;
        private volatile bool _disposed;

        public EventDispatcher(UnifiedEventQueue eventQueue, UnifiedEventQueueConfig config)
        {
            _eventQueue = eventQueue ?? throw new ArgumentNullException(nameof(eventQueue));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            _handlers = new ConcurrentDictionary<ushort, List<IEventHandler>>();
            _workerPool = new WorkerPool(Environment.ProcessorCount);
            _perfCounters = new PerformanceCounters();
            _frameBuffer = new UnifiedEvent[config.MaxBatchSize * 4]; // 为所有优先级预留空间
            
            // 启动帧同步定时器
            _frameTimer = new Timer(ProcessFrameTick, null, 
                TimeSpan.FromMilliseconds(config.FrameIntervalMs),
                TimeSpan.FromMilliseconds(config.FrameIntervalMs));
        }

        /// <summary>
        /// 注册事件处理器
        /// </summary>
        public void RegisterHandler(ushort eventType, IEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers.AddOrUpdate(eventType,
                new List<IEventHandler> { handler },
                (key, existing) =>
                {
                    lock (existing)
                    {
                        existing.Add(handler);
                        return existing;
                    }
                });
        }

        /// <summary>
        /// 注册函数式事件处理器
        /// </summary>
        public void RegisterHandler(ushort eventType, Action<UnifiedEvent> handler)
        {
            RegisterHandler(eventType, new FunctionalEventHandler(handler));
        }

        /// <summary>
        /// 取消注册事件处理器
        /// </summary>
        public bool UnregisterHandler(ushort eventType, IEventHandler handler)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers))
                return false;

            lock (handlers)
            {
                return handlers.Remove(handler);
            }
        }

        /// <summary>
        /// 处理帧周期 - 主分发循环
        /// </summary>
        private void ProcessFrameTick(object? state)
        {
            if (_disposed)
                return;

            lock (_dispatchLock)
            {
                var stopwatch = Stopwatch.StartNew();
                
                try
                {
                    // 收集当前帧的所有事件
                    var eventCount = _eventQueue.CollectFrameEvents(_frameBuffer, _frameBuffer.Length);
                    
                    if (eventCount > 0)
                    {
                        // 批量处理事件
                        ProcessEventBatch(_frameBuffer, eventCount);
                        _perfCounters.RecordFrameProcessed(eventCount, stopwatch.Elapsed);
                    }
                    
                    // 推进帧计数
                    _eventQueue.AdvanceFrame();
                }
                catch (Exception ex)
                {
                    _perfCounters.RecordError();
                    // 这里应该记录到日志系统
                    Console.WriteLine($"Frame processing error: {ex.Message}");
                }
                finally
                {
                    stopwatch.Stop();
                    
                    // 检查处理时间是否超过帧预算
                    if (stopwatch.ElapsedMilliseconds > _config.FrameIntervalMs * 0.8)
                    {
                        _perfCounters.RecordFrameTimeout();
                        // 考虑动态调整批大小或增加工作线程
                    }
                }
            }
        }

        /// <summary>
        /// 批量处理事件
        /// </summary>
        private void ProcessEventBatch(UnifiedEvent[] events, int count)
        {
            // 按事件类型分组以提高缓存效率
            var eventGroups = GroupEventsByType(events, count);
            
            // 并行处理不同类型的事件
            var tasks = new List<Task>();
            
            foreach (var group in eventGroups)
            {
                if (_handlers.TryGetValue(group.Key, out var handlers))
                {
                    // 创建处理任务
                    var task = _workerPool.QueueWork(() => ProcessEventGroup(group.Value, handlers));
                    tasks.Add(task);
                }
            }
            
            // 等待所有处理完成（帧同步）
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromMilliseconds(_config.FrameIntervalMs));
        }

        /// <summary>
        /// 将事件按类型分组
        /// </summary>
        private Dictionary<ushort, List<UnifiedEvent>> GroupEventsByType(UnifiedEvent[] events, int count)
        {
            var groups = new Dictionary<ushort, List<UnifiedEvent>>();
            
            for (int i = 0; i < count; i++)
            {
                var evt = events[i];
                if (!groups.TryGetValue(evt.EventType, out var group))
                {
                    group = new List<UnifiedEvent>();
                    groups[evt.EventType] = group;
                }
                group.Add(evt);
            }
            
            return groups;
        }

        /// <summary>
        /// 处理同类型事件组
        /// </summary>
        private void ProcessEventGroup(List<UnifiedEvent> events, List<IEventHandler> handlers)
        {
            foreach (var evt in events)
            {
                if (evt.IsCancelled)
                    continue;

                foreach (var handler in handlers)
                {
                    try
                    {
                        handler.Handle(evt);
                        _perfCounters.RecordEventHandled();
                        
                        if (evt.IsCancelled)
                            break; // 事件被取消，停止后续处理
                    }
                    catch (Exception ex)
                    {
                        _perfCounters.RecordError();
                        // 记录处理器错误但继续处理其他处理器
                        Console.WriteLine($"Event handler error: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public DispatcherStatistics GetStatistics()
        {
            return new DispatcherStatistics
            {
                TotalFramesProcessed = _perfCounters.TotalFramesProcessed,
                TotalEventsHandled = _perfCounters.TotalEventsHandled,
                TotalErrors = _perfCounters.TotalErrors,
                TotalFrameTimeouts = _perfCounters.TotalFrameTimeouts,
                AverageFrameTime = _perfCounters.AverageFrameTime,
                AverageEventsPerFrame = _perfCounters.AverageEventsPerFrame,
                WorkerPoolStats = _workerPool.GetStatistics()
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _frameTimer?.Dispose();
            _workerPool?.Dispose();
        }
    }

    /// <summary>
    /// 事件处理器接口
    /// </summary>
    public interface IEventHandler
    {
        void Handle(UnifiedEvent evt);
    }

    /// <summary>
    /// 函数式事件处理器实现
    /// </summary>
    internal class FunctionalEventHandler : IEventHandler
    {
        private readonly Action<UnifiedEvent> _handler;

        public FunctionalEventHandler(Action<UnifiedEvent> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void Handle(UnifiedEvent evt)
        {
            _handler(evt);
        }
    }

    /// <summary>
    /// 工作线程池
    /// </summary>
    internal class WorkerPool : IDisposable
    {
        private readonly TaskFactory _taskFactory;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private long _totalTasksQueued;
        private long _totalTasksCompleted;
        private volatile bool _disposed;

        public WorkerPool(int maxConcurrency)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var scheduler = new LimitedConcurrencyLevelTaskScheduler(maxConcurrency);
            _taskFactory = new TaskFactory(scheduler);
        }

        public Task QueueWork(Action work)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WorkerPool));

            Interlocked.Increment(ref _totalTasksQueued);
            
            return _taskFactory.StartNew(() =>
            {
                try
                {
                    work();
                }
                finally
                {
                    Interlocked.Increment(ref _totalTasksCompleted);
                }
            }, _cancellationTokenSource.Token);
        }

        public WorkerPoolStatistics GetStatistics()
        {
            return new WorkerPoolStatistics
            {
                TotalTasksQueued = _totalTasksQueued,
                TotalTasksCompleted = _totalTasksCompleted,
                PendingTasks = _totalTasksQueued - _totalTasksCompleted
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }

    /// <summary>
    /// 性能计数器
    /// </summary>
    internal class PerformanceCounters
    {
        private long _totalFramesProcessed;
        private long _totalEventsHandled;
        private long _totalErrors;
        private long _totalFrameTimeouts;
        private long _totalFrameTimeMs;

        public long TotalFramesProcessed => _totalFramesProcessed;
        public long TotalEventsHandled => _totalEventsHandled;
        public long TotalErrors => _totalErrors;
        public long TotalFrameTimeouts => _totalFrameTimeouts;
        
        public double AverageFrameTime => _totalFramesProcessed > 0 ? 
            (double)_totalFrameTimeMs / _totalFramesProcessed : 0.0;
        
        public double AverageEventsPerFrame => _totalFramesProcessed > 0 ? 
            (double)_totalEventsHandled / _totalFramesProcessed : 0.0;

        public void RecordFrameProcessed(int eventCount, TimeSpan frameTime)
        {
            Interlocked.Increment(ref _totalFramesProcessed);
            Interlocked.Add(ref _totalEventsHandled, eventCount);
            Interlocked.Add(ref _totalFrameTimeMs, (long)frameTime.TotalMilliseconds);
        }

        public void RecordEventHandled()
        {
            // 已在RecordFrameProcessed中计算
        }

        public void RecordError()
        {
            Interlocked.Increment(ref _totalErrors);
        }

        public void RecordFrameTimeout()
        {
            Interlocked.Increment(ref _totalFrameTimeouts);
        }
    }

    /// <summary>
    /// 限制并发级别的任务调度器
    /// </summary>
    internal class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
    {
        private readonly ThreadLocal<bool> _currentThreadIsProcessingItems;
        private readonly LinkedList<Task> _tasks = new();
        private readonly int _maxDegreeOfParallelism;
        private int _delegatesQueuedOrRunning;

        public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            _currentThreadIsProcessingItems = new ThreadLocal<bool>();
        }

        protected sealed override void QueueTask(Task task)
        {
            lock (_tasks)
            {
                _tasks.AddLast(task);
                if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
                {
                    ++_delegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                _currentThreadIsProcessingItems.Value = true;
                try
                {
                    while (true)
                    {
                        Task item;
                        lock (_tasks)
                        {
                            if (_tasks.Count == 0)
                            {
                                --_delegatesQueuedOrRunning;
                                break;
                            }

                            item = _tasks.First!.Value;
                            _tasks.RemoveFirst();
                        }

                        TryExecuteTask(item);
                    }
                }
                finally
                {
                    _currentThreadIsProcessingItems.Value = false;
                }
            }, null);
        }

        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (!_currentThreadIsProcessingItems.Value) return false;

            if (taskWasPreviouslyQueued)
                if (TryDequeue(task))
                    return TryExecuteTask(task);
                else
                    return false;
            else
                return TryExecuteTask(task);
        }

        protected sealed override bool TryDequeue(Task task)
        {
            lock (_tasks) { return _tasks.Remove(task); }
        }

        public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }

        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken) return _tasks;
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_tasks);
            }
        }
    }

    /// <summary>
    /// 分发器统计信息
    /// </summary>
    public struct DispatcherStatistics
    {
        public long TotalFramesProcessed;
        public long TotalEventsHandled;
        public long TotalErrors;
        public long TotalFrameTimeouts;
        public double AverageFrameTime;
        public double AverageEventsPerFrame;
        public WorkerPoolStatistics WorkerPoolStats;

        public override string ToString()
        {
            return $"Frames: {TotalFramesProcessed}, Events: {TotalEventsHandled}, " +
                   $"Errors: {TotalErrors}, Timeouts: {TotalFrameTimeouts}, " +
                   $"Avg Frame Time: {AverageFrameTime:F2}ms, Avg Events/Frame: {AverageEventsPerFrame:F1}";
        }
    }

    /// <summary>
    /// 工作池统计信息
    /// </summary>
    public struct WorkerPoolStatistics
    {
        public long TotalTasksQueued;
        public long TotalTasksCompleted;
        public long PendingTasks;

        public override string ToString()
        {
            return $"Queued: {TotalTasksQueued}, Completed: {TotalTasksCompleted}, Pending: {PendingTasks}";
        }
    }
}