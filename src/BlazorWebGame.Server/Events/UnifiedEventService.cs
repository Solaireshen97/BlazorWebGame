using System;
using System.Threading;
using System.Threading.Tasks;
using BlazorWebGame.Shared.Events;
using BlazorWebGame.Server.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Services.Events
{
    /// <summary>
    /// 统一事件服务 - 管理事件队列和分发器的生命周期
    /// </summary>
    public class UnifiedEventService : BackgroundService
    {
        private readonly UnifiedEventQueue _eventQueue;
        private readonly EventDispatcher _dispatcher;
        private readonly ILogger<UnifiedEventService> _logger;
        private readonly Timer _frameTimer;
        private readonly int _frameIntervalMs = 16; // 60 FPS

        public UnifiedEventQueue Queue => _eventQueue;
        public EventDispatcher Dispatcher => _dispatcher;

        public UnifiedEventService(
            UnifiedEventQueue eventQueue,
            ILogger<UnifiedEventService> logger)
        {
            _eventQueue = eventQueue;
            _dispatcher = eventQueue.Dispatcher;
            _logger = logger;

            // 创建帧定时器
            _frameTimer = new Timer(ProcessFrame, null, Timeout.Infinite, Timeout.Infinite);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("统一事件服务启动");

            // 启动帧定时器
            _frameTimer.Change(0, _frameIntervalMs);

            // 保持服务运行
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);

                // 每秒输出统计
                var stats = _eventQueue.GetStatistics();
                if (stats.TotalEnqueued > 0)
                {
                    _logger.LogDebug("事件队列统计: {Stats}", stats);
                }
            }

            // 停止帧定时器
            _frameTimer.Change(Timeout.Infinite, Timeout.Infinite);

            _logger.LogInformation("统一事件服务停止");
        }

        private void ProcessFrame(object? state)
        {
            try
            {
                // EventDispatcher会自动处理帧tick
                // 这里主要是推进游戏帧
                _eventQueue.AdvanceFrame();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理事件帧时发生错误");
            }
        }

        /// <summary>
        /// 便捷方法：发布事件
        /// </summary>
        public bool EnqueueEvent(ushort eventType, EventPriority priority = EventPriority.Gameplay,
            ulong actorId = 0, ulong targetId = 0)
        {
            return _eventQueue.EnqueueEvent(eventType, priority, actorId, targetId);
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public UnifiedEventSystemStats GetStatistics()
        {
            return new UnifiedEventSystemStats
            {
                QueueStatistics = _eventQueue.GetStatistics(),
                DispatcherStatistics = _dispatcher.GetStatistics(),
                CurrentFrame = _eventQueue.CurrentFrame
            };
        }
    }

    public struct UnifiedEventSystemStats
    {
        public QueueStatistics QueueStatistics;
        public DispatcherStatistics DispatcherStatistics;
        public long CurrentFrame;
    }
}