using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BlazorWebGame.Shared.Events
{
    /// <summary>
    /// 无锁环形缓冲区 - 多生产者单消费者 (MPSC)
    /// 使用原子操作确保线程安全，避免锁竞争
    /// </summary>
    public class LockFreeRingBuffer
    {
        private readonly UnifiedEvent[] _buffer;
        private readonly ulong _mask;
        
        // 使用cache line padding避免false sharing
        private readonly PaddedLong _head = new();
        private readonly PaddedLong _tail = new();
        
        public int Capacity { get; }
        
        /// <summary>
        /// 创建指定容量的环形缓冲区
        /// 容量必须是2的幂以优化取模运算
        /// </summary>
        public LockFreeRingBuffer(int capacity)
        {
            if (capacity <= 0 || (capacity & (capacity - 1)) != 0)
                throw new ArgumentException("Capacity must be a positive power of 2", nameof(capacity));
            
            Capacity = capacity;
            _buffer = new UnifiedEvent[capacity];
            _mask = (ulong)(capacity - 1);
        }

        /// <summary>
        /// 尝试入队事件
        /// </summary>
        /// <param name="evt">要入队的事件</param>
        /// <returns>是否成功入队</returns>
        public bool TryEnqueue(ref UnifiedEvent evt)
        {
            while (true)
            {
                var head = (ulong)Volatile.Read(ref _head.Value);
                var tail = (ulong)Volatile.Read(ref _tail.Value);
                
                // 检查队列是否已满
                if (head - tail >= (ulong)Capacity)
                {
                    return false; // 队列满
                }
                
                // 尝试原子性地获取头部位置
                var newHead = head + 1;
                if (Interlocked.CompareExchange(ref _head.Value, (long)newHead, (long)head) == (long)head)
                {
                    // 成功获取位置，写入数据
                    var index = head & _mask;
                    _buffer[index] = evt;
                    return true;
                }
                
                // CAS失败，重试
            }
        }

        /// <summary>
        /// 尝试出队事件
        /// </summary>
        /// <param name="evt">出队的事件</param>
        /// <returns>是否成功出队</returns>
        public bool TryDequeue(out UnifiedEvent evt)
        {
            var tail = (ulong)Volatile.Read(ref _tail.Value);
            var head = (ulong)Volatile.Read(ref _head.Value);
            
            if (tail >= head)
            {
                evt = default;
                return false; // 队列空
            }
            
            var index = tail & _mask;
            evt = _buffer[index];
            
            // 原子性地更新尾部位置
            Volatile.Write(ref _tail.Value, (long)(tail + 1));
            return true;
        }

        /// <summary>
        /// 批量出队事件到指定数组
        /// </summary>
        /// <param name="events">目标数组</param>
        /// <param name="maxCount">最大出队数量</param>
        /// <returns>实际出队数量</returns>
        public int TryDequeueBatch(UnifiedEvent[] events, int maxCount)
        {
            if (events == null || maxCount <= 0)
                return 0;
            
            var count = 0;
            var limit = Math.Min(maxCount, events.Length);
            
            while (count < limit && TryDequeue(out var evt))
            {
                events[count] = evt;
                count++;
            }
            
            return count;
        }

        /// <summary>
        /// 获取当前队列大小
        /// </summary>
        public int Count
        {
            get
            {
                var head = (ulong)Volatile.Read(ref _head.Value);
                var tail = (ulong)Volatile.Read(ref _tail.Value);
                return (int)(head - tail);
            }
        }

        /// <summary>
        /// 检查队列是否为空
        /// </summary>
        public bool IsEmpty => Count == 0;

        /// <summary>
        /// 检查队列是否已满
        /// </summary>
        public bool IsFull => Count >= Capacity;

        /// <summary>
        /// 获取队列使用率 (0.0 - 1.0)
        /// </summary>
        public double LoadFactor => (double)Count / Capacity;
    }

    /// <summary>
    /// 带缓存行填充的long，避免false sharing
    /// </summary>
    internal class PaddedLong
    {
        // 前填充 - 避免与其他数据共享缓存行
        private readonly long _padding1, _padding2, _padding3, _padding4;
        private readonly long _padding5, _padding6, _padding7;
        
        public long Value;
        
        // 后填充 - 避免与其他数据共享缓存行
        private readonly long _padding8, _padding9, _padding10, _padding11;
        private readonly long _padding12, _padding13, _padding14, _padding15;

        // 防止编译器优化掉填充字段
        [MethodImpl(MethodImplOptions.NoInlining)]
        public long GetPaddingSum() => _padding1 + _padding2 + _padding3 + _padding4 + 
                                      _padding5 + _padding6 + _padding7 + _padding8 + 
                                      _padding9 + _padding10 + _padding11 + _padding12 + 
                                      _padding13 + _padding14 + _padding15;
    }
}