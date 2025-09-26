using BlazorWebGame.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Tests
{
    /// <summary>
    /// 统一事件系统测试
    /// </summary>
    public static class UnifiedEventSystemTest
    {
        /// <summary>
        /// 运行统一事件系统测试
        /// </summary>
        public static void RunEventSystemTest(IServiceProvider services, ILogger logger)
        {
            logger.LogInformation("=== Running Unified Event System Test ===");

            try
            {
                TestBasicEventQueue(logger);
                TestEventPersistence(logger);
                TestEventReplay(logger);
                
                logger.LogInformation("✅ All unified event system tests passed!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Unified event system test failed");
                throw;
            }
        }

        /// <summary>
        /// 测试基本事件队列功能
        /// </summary>
        private static void TestBasicEventQueue(ILogger logger)
        {
            logger.LogInformation("Testing basic event queue functionality...");

            var config = new UnifiedEventQueueConfig
            {
                GameplayQueueSize = 1024,
                AIQueueSize = 512,
                AnalyticsQueueSize = 256,
                TelemetryQueueSize = 128,
                FrameIntervalMs = 16
            };

            using var eventQueue = new UnifiedEventQueue(config);

            // 测试入队不同优先级的事件
            var gameplayEvent = new UnifiedEvent(GameEventTypes.BATTLE_STARTED, EventPriority.Gameplay)
            {
                ActorId = 12345,
                TargetId = 67890
            };

            var aiEvent = new UnifiedEvent(GameEventTypes.SKILL_USED, EventPriority.AI)
            {
                ActorId = 11111,
                TargetId = 22222
            };

            var analyticsEvent = new UnifiedEvent(GameEventTypes.STATE_CHANGED, EventPriority.Analytics);
            var telemetryEvent = new UnifiedEvent(GameEventTypes.SYSTEM_TICK, EventPriority.Telemetry);

            // 入队测试
            if (!eventQueue.Enqueue(ref gameplayEvent))
                throw new Exception("Failed to enqueue gameplay event");

            if (!eventQueue.Enqueue(ref aiEvent))
                throw new Exception("Failed to enqueue AI event");

            if (!eventQueue.Enqueue(ref analyticsEvent))
                throw new Exception("Failed to enqueue analytics event");

            if (!eventQueue.Enqueue(ref telemetryEvent))
                throw new Exception("Failed to enqueue telemetry event");

            // 收集事件测试
            var collectedEvents = new UnifiedEvent[10];
            var count = eventQueue.CollectFrameEvents(collectedEvents, 10);

            if (count != 4)
                throw new Exception($"Expected 4 events, got {count}");

            // 验证优先级顺序（Gameplay > AI > Analytics > Telemetry）
            if (collectedEvents[0].EventType != GameEventTypes.BATTLE_STARTED)
                throw new Exception("Priority ordering failed - Gameplay event should be first");

            if (collectedEvents[1].EventType != GameEventTypes.SKILL_USED)
                throw new Exception("Priority ordering failed - AI event should be second");

            logger.LogInformation("✅ Basic event queue test passed");
        }

        /// <summary>
        /// 测试事件持久化
        /// </summary>
        private static void TestEventPersistence(ILogger logger)
        {
            logger.LogInformation("Testing event persistence...");

            using var persistence = new InMemoryEventPersistence();
            
            // 创建测试事件
            var events = new UnifiedEvent[]
            {
                new UnifiedEvent(GameEventTypes.BATTLE_STARTED, EventPriority.Gameplay) { Frame = 100, ActorId = 1 },
                new UnifiedEvent(GameEventTypes.DAMAGE_DEALT, EventPriority.Gameplay) { Frame = 100, ActorId = 2 },
                new UnifiedEvent(GameEventTypes.ENEMY_KILLED, EventPriority.Gameplay) { Frame = 100, ActorId = 3 }
            };

            // 持久化测试
            var persistTask = persistence.PersistFrameAsync(100, events, events.Length);
            persistTask.Wait();

            // 加载测试
            var loadTask = persistence.LoadFrameAsync(100);
            loadTask.Wait();
            var loadedEvents = loadTask.Result;

            if (loadedEvents.Length != 3)
                throw new Exception($"Expected 3 persisted events, got {loadedEvents.Length}");

            if (loadedEvents[0].EventType != GameEventTypes.BATTLE_STARTED)
                throw new Exception("Event persistence failed - incorrect event type");

            if (loadedEvents[0].ActorId != 1)
                throw new Exception("Event persistence failed - incorrect actor ID");

            // 范围查询测试
            var rangeTask = persistence.LoadFrameRangeAsync(100, 100);
            rangeTask.Wait();
            var rangeEvents = rangeTask.Result;

            if (rangeEvents.Length != 3)
                throw new Exception($"Expected 3 events in range, got {rangeEvents.Length}");

            logger.LogInformation("✅ Event persistence test passed");
        }

        /// <summary>
        /// 测试事件重放功能
        /// </summary>
        private static void TestEventReplay(ILogger logger)
        {
            logger.LogInformation("Testing event replay...");

            using var persistence = new InMemoryEventPersistence();
            using var eventQueue = new UnifiedEventQueue(UnifiedEventQueueConfig.Default);
            var replayService = new EventReplayService(persistence, eventQueue, logger);

            // 准备测试数据
            var events = new UnifiedEvent[]
            {
                new UnifiedEvent(GameEventTypes.BATTLE_STARTED, EventPriority.Gameplay) { Frame = 200 },
                new UnifiedEvent(GameEventTypes.BATTLE_ENDED, EventPriority.Gameplay) { Frame = 200 }
            };

            // 持久化事件
            var persistTask = persistence.PersistFrameAsync(200, events, events.Length);
            persistTask.Wait();

            // 重放测试
            var replayTask = replayService.ReplayFrameAsync(200);
            replayTask.Wait();
            var replayedCount = replayTask.Result;

            if (replayedCount != 2)
                throw new Exception($"Expected 2 replayed events, got {replayedCount}");

            // 验证完整性
            var integrityTask = replayService.ValidateFrameIntegrityAsync(200, 200);
            integrityTask.Wait();
            var integrityReport = integrityTask.Result;

            if (!integrityReport.IsComplete)
                throw new Exception("Frame integrity validation failed");

            if (integrityReport.ValidFrames != 1)
                throw new Exception($"Expected 1 valid frame, got {integrityReport.ValidFrames}");

            logger.LogInformation("✅ Event replay test passed");
        }

        /// <summary>
        /// 性能基准测试
        /// </summary>
        public static void RunPerformanceBenchmark(ILogger logger)
        {
            logger.LogInformation("=== Running Event System Performance Benchmark ===");

            const int eventCount = 100000;
            const int iterations = 10;

            using var eventQueue = new UnifiedEventQueue(new UnifiedEventQueueConfig
            {
                GameplayQueueSize = 131072, // 128K - power of 2
                FrameIntervalMs = 1 // 1ms for faster testing
            });

            var totalEnqueueTime = TimeSpan.Zero;
            var totalDequeueTime = TimeSpan.Zero;

            for (int iter = 0; iter < iterations; iter++)
            {
                // 入队基准测试
                var enqueueStart = DateTime.UtcNow;
                for (int i = 0; i < eventCount; i++)
                {
                    var evt = new UnifiedEvent(GameEventTypes.BATTLE_TICK, EventPriority.Gameplay)
                    {
                        ActorId = (ulong)i
                    };
                    eventQueue.Enqueue(ref evt);
                }
                var enqueueTime = DateTime.UtcNow - enqueueStart;
                totalEnqueueTime += enqueueTime;

                // 出队基准测试
                var dequeueStart = DateTime.UtcNow;
                var buffer = new UnifiedEvent[1000];
                var totalDequeued = 0;
                while (totalDequeued < eventCount)
                {
                    var count = eventQueue.CollectFrameEvents(buffer, buffer.Length);
                    totalDequeued += count;
                    if (count == 0) break; // 避免无限循环
                }
                var dequeueTime = DateTime.UtcNow - dequeueStart;
                totalDequeueTime += dequeueTime;

                eventQueue.AdvanceFrame(); // 推进帧计数以重置状态
            }

            var avgEnqueueTime = totalEnqueueTime.TotalMilliseconds / iterations;
            var avgDequeueTime = totalDequeueTime.TotalMilliseconds / iterations;
            var enqueueRate = eventCount / (avgEnqueueTime / 1000.0);
            var dequeueRate = eventCount / (avgDequeueTime / 1000.0);

            logger.LogInformation("📊 Performance Benchmark Results:");
            logger.LogInformation("   Enqueue Rate: {EnqueueRate:N0} events/sec", enqueueRate);
            logger.LogInformation("   Dequeue Rate: {DequeueRate:N0} events/sec", dequeueRate);
            logger.LogInformation("   Average Enqueue Time: {EnqueueTime:F2}ms for {Count:N0} events", 
                avgEnqueueTime, eventCount);
            logger.LogInformation("   Average Dequeue Time: {DequeueTime:F2}ms for {Count:N0} events", 
                avgDequeueTime, eventCount);

            // 性能目标检查
            if (enqueueRate < 1_000_000) // 目标: 1M events/sec
            {
                logger.LogWarning("⚠️  Enqueue rate {Rate:N0} is below target of 1,000,000 events/sec", enqueueRate);
            }
            else
            {
                logger.LogInformation("✅ Enqueue performance target met");
            }

            if (dequeueRate < 500_000) // 目标: 500K events/sec
            {
                logger.LogWarning("⚠️  Dequeue rate {Rate:N0} is below target of 500,000 events/sec", dequeueRate);
            }
            else
            {
                logger.LogInformation("✅ Dequeue performance target met");
            }
        }
    }
}