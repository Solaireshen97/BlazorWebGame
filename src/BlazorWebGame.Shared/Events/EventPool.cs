using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BlazorWebGame.Shared.Events
{
    /// <summary>
    /// 事件对象池 - 预分配事件对象以避免GC压力
    /// 使用ConcurrentQueue实现线程安全的对象池
    /// </summary>
    public class EventPool : IDisposable
    {
        private readonly ConcurrentQueue<EventWrapper> _pool;
        private readonly int _maxSize;
        private volatile bool _disposed;
        private long _totalAllocated;
        private long _totalReturned;
        private long _totalRequested;

        public int MaxSize => _maxSize;
        public int Count => _pool.Count;
        public long TotalAllocated => _totalAllocated;
        public long TotalReturned => _totalReturned;
        public long TotalRequested => _totalRequested;
        public double HitRatio => _totalRequested > 0 ? (double)(_totalRequested - _totalAllocated) / _totalRequested : 0.0;

        /// <summary>
        /// 创建事件池
        /// </summary>
        /// <param name="initialSize">初始预分配大小</param>
        /// <param name="maxSize">最大池大小</param>
        public EventPool(int initialSize = 1024, int maxSize = 4096)
        {
            if (initialSize < 0) throw new ArgumentException("Initial size must be non-negative", nameof(initialSize));
            if (maxSize < initialSize) throw new ArgumentException("Max size must be >= initial size", nameof(maxSize));

            _maxSize = maxSize;
            _pool = new ConcurrentQueue<EventWrapper>();
            
            // 预分配初始对象
            for (int i = 0; i < initialSize; i++)
            {
                _pool.Enqueue(new EventWrapper());
            }
            
            _totalAllocated = initialSize;
        }

        /// <summary>
        /// 获取事件包装器
        /// </summary>
        /// <returns>事件包装器，如果池空则新建</returns>
        public EventWrapper Rent()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EventPool));

            Interlocked.Increment(ref _totalRequested);

            if (_pool.TryDequeue(out var wrapper))
            {
                wrapper.Reset();
                return wrapper;
            }

            // 池为空，分配新对象
            Interlocked.Increment(ref _totalAllocated);
            return new EventWrapper();
        }

        /// <summary>
        /// 归还事件包装器到池中
        /// </summary>
        /// <param name="wrapper">要归还的包装器</param>
        public void Return(EventWrapper wrapper)
        {
            if (_disposed || wrapper == null)
                return;

            // 重置包装器状态
            wrapper.Reset();

            // 如果池未满则归还
            if (_pool.Count < _maxSize)
            {
                _pool.Enqueue(wrapper);
                Interlocked.Increment(ref _totalReturned);
            }
            // 如果池已满则丢弃（让GC处理）
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            while (_pool.TryDequeue(out _))
            {
                // 清空队列
            }
        }

        /// <summary>
        /// 获取池统计信息
        /// </summary>
        public PoolStatistics GetStatistics()
        {
            return new PoolStatistics
            {
                CurrentCount = Count,
                MaxSize = MaxSize,
                TotalAllocated = TotalAllocated,
                TotalReturned = TotalReturned,
                TotalRequested = TotalRequested,
                HitRatio = HitRatio
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Clear();
        }
    }

    /// <summary>
    /// 事件包装器 - 封装UnifiedEvent及其元数据
    /// </summary>
    public class EventWrapper
    {
        private UnifiedEvent _event;
        private DateTime _createdAt;
        private string? _sourceModule;
        private object? _additionalData;

        public ref UnifiedEvent Event => ref _event;
        public DateTime CreatedAt => _createdAt;
        public string? SourceModule => _sourceModule;
        public object? AdditionalData => _additionalData;

        public EventWrapper()
        {
            Reset();
        }

        /// <summary>
        /// 重置包装器状态
        /// </summary>
        public void Reset()
        {
            _event = default;
            _createdAt = DateTime.UtcNow;
            _sourceModule = null;
            _additionalData = null;
        }

        /// <summary>
        /// 设置事件数据
        /// </summary>
        public void SetEvent(UnifiedEvent evt, string? sourceModule = null, object? additionalData = null)
        {
            _event = evt;
            _sourceModule = sourceModule;
            _additionalData = additionalData;
        }

        /// <summary>
        /// 获取事件年龄（从创建到现在的时间）
        /// </summary>
        public TimeSpan Age => DateTime.UtcNow - _createdAt;
    }

    /// <summary>
    /// 对象池统计信息
    /// </summary>
    public struct PoolStatistics
    {
        public int CurrentCount;
        public int MaxSize;
        public long TotalAllocated;
        public long TotalReturned;
        public long TotalRequested;
        public double HitRatio;

        public override string ToString()
        {
            return $"Pool Stats: {CurrentCount}/{MaxSize} items, " +
                   $"Allocated: {TotalAllocated}, Returned: {TotalReturned}, " +
                   $"Requested: {TotalRequested}, Hit Ratio: {HitRatio:P2}";
        }
    }
}