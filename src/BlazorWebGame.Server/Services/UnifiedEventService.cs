using BlazorWebGame.Shared.Events;
using BlazorWebGame.Server.Hubs;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace BlazorWebGame.Server.Services
{
    /// <summary>
    /// 统一事件服务 - 服务端实现
    /// 整合新的统一事件队列系统与现有架构
    /// </summary>
    public class UnifiedEventService : IDisposable
    {
        private readonly UnifiedEventQueue _eventQueue;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly ILogger<UnifiedEventService> _logger;
        private readonly ServerEventService _legacyEventService;
        
        // 事件处理器映射
        private readonly Dictionary<ushort, List<IUnifiedEventHandler>> _handlers;
        private readonly object _handlersLock = new();

        public UnifiedEventQueue EventQueue => _eventQueue;
        public EventDispatcher Dispatcher => _eventQueue.Dispatcher;

        public UnifiedEventService(
            IHubContext<GameHub> hubContext, 
            ILogger<UnifiedEventService> logger,
            ServerEventService legacyEventService)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _legacyEventService = legacyEventService ?? throw new ArgumentNullException(nameof(legacyEventService));
            
            // 创建统一事件队列
            var config = new UnifiedEventQueueConfig
            {
                GameplayQueueSize = 8192,
                AIQueueSize = 4096,
                AnalyticsQueueSize = 2048,
                TelemetryQueueSize = 1024,
                FrameIntervalMs = 16, // 60 FPS
                MaxBatchSize = 256
            };
            
            _eventQueue = new UnifiedEventQueue(config);
            _handlers = new Dictionary<ushort, List<IUnifiedEventHandler>>();
            
            // 注册核心事件处理器
            RegisterCoreHandlers();
            
            _logger.LogInformation("UnifiedEventService initialized with 60fps processing");
        }

        /// <summary>
        /// 注册核心事件处理器
        /// </summary>
        private void RegisterCoreHandlers()
        {
            // 战斗事件处理器
            RegisterHandler(GameEventTypes.BATTLE_STARTED, new BattleEventHandler(_hubContext, _logger));
            RegisterHandler(GameEventTypes.BATTLE_ENDED, new BattleEventHandler(_hubContext, _logger));
            RegisterHandler(GameEventTypes.DAMAGE_DEALT, new DamageEventHandler(_hubContext, _logger));
            RegisterHandler(GameEventTypes.SKILL_USED, new SkillEventHandler(_hubContext, _logger));
            
            // 角色事件处理器
            RegisterHandler(GameEventTypes.CHARACTER_LEVEL_UP, new CharacterEventHandler(_hubContext, _logger));
            RegisterHandler(GameEventTypes.EXPERIENCE_GAINED, new CharacterEventHandler(_hubContext, _logger));
            
            // 物品事件处理器
            RegisterHandler(GameEventTypes.ITEM_ACQUIRED, new ItemEventHandler(_hubContext, _logger));
            RegisterHandler(GameEventTypes.GOLD_CHANGED, new CurrencyEventHandler(_hubContext, _logger));
            
            // 系统事件处理器
            RegisterHandler(GameEventTypes.STATE_CHANGED, new StateChangeEventHandler(_hubContext, _logger));
            
            // 遗留系统兼容性处理器
            RegisterHandler(GameEventTypes.STATE_CHANGED, new LegacyCompatibilityHandler(_legacyEventService, _logger));
        }

        /// <summary>
        /// 注册事件处理器
        /// </summary>
        public void RegisterHandler(ushort eventType, IUnifiedEventHandler handler)
        {
            lock (_handlersLock)
            {
                if (!_handlers.TryGetValue(eventType, out var handlerList))
                {
                    handlerList = new List<IUnifiedEventHandler>();
                    _handlers[eventType] = handlerList;
                }
                handlerList.Add(handler);
            }

            // 同时在分发器中注册
            _eventQueue.Dispatcher.RegisterHandler(eventType, evt => handler.HandleAsync(evt).Wait());
        }

        /// <summary>
        /// 入队战斗事件
        /// </summary>
        public bool EnqueueBattleEvent(ushort eventType, ulong battleId, ulong actorId = 0, ulong targetId = 0, object? eventData = null)
        {
            var evt = new UnifiedEvent(eventType, EventPriority.Gameplay)
            {
                ActorId = actorId,
                TargetId = targetId
            };

            // 如果有数据，尝试内联存储
            if (eventData != null)
            {
                switch (eventData)
                {
                    case DamageEventData damageData:
                        evt.SetData(damageData);
                        break;
                    case ExperienceEventData expData:
                        evt.SetData(expData);
                        break;
                    case int intValue:
                        evt.SetData(intValue);
                        break;
                    // 可以添加更多数据类型的支持
                }
            }

            return _eventQueue.Enqueue(ref evt);
        }

        /// <summary>
        /// 入队角色事件
        /// </summary>
        public bool EnqueueCharacterEvent(ushort eventType, string characterId, object? eventData = null)
        {
            var actorId = HashString(characterId);
            
            var evt = new UnifiedEvent(eventType, EventPriority.Gameplay)
            {
                ActorId = actorId
            };

            if (eventData != null && eventData is ExperienceEventData expData)
            {
                evt.SetData(expData);
            }

            return _eventQueue.Enqueue(ref evt);
        }

        /// <summary>
        /// 入队系统事件
        /// </summary>
        public bool EnqueueSystemEvent(ushort eventType, EventPriority priority = EventPriority.Analytics)
        {
            var evt = new UnifiedEvent(eventType, priority);
            return _eventQueue.Enqueue(ref evt);
        }

        /// <summary>
        /// 批量入队事件 - 用于高性能场景
        /// </summary>
        public int EnqueueBatch(UnifiedEvent[] events, int count)
        {
            var successCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (_eventQueue.Enqueue(ref events[i]))
                {
                    successCount++;
                }
            }
            return successCount;
        }

        /// <summary>
        /// 获取系统统计信息
        /// </summary>
        public UnifiedEventSystemStats GetStatistics()
        {
            var queueStats = _eventQueue.GetStatistics();
            var dispatcherStats = _eventQueue.Dispatcher.GetStatistics();
            
            return new UnifiedEventSystemStats
            {
                QueueStatistics = queueStats,
                DispatcherStatistics = dispatcherStats,
                RegisteredHandlers = _handlers.Count,
                CurrentFrame = queueStats.CurrentFrame
            };
        }

        /// <summary>
        /// 字符串哈希函数 - 将字符串ID转换为ulong
        /// </summary>
        private static ulong HashString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            // 简单的FNV-1a哈希
            const ulong FnvPrime = 1099511628211UL;
            const ulong FnvOffsetBasis = 14695981039346656037UL;

            var hash = FnvOffsetBasis;
            foreach (var c in input)
            {
                hash ^= c;
                hash *= FnvPrime;
            }
            return hash;
        }

        public void Dispose()
        {
            _eventQueue?.Dispose();
        }
    }

    /// <summary>
    /// 统一事件处理器接口
    /// </summary>
    public interface IUnifiedEventHandler
    {
        Task HandleAsync(UnifiedEvent evt);
    }

    /// <summary>
    /// 战斗事件处理器
    /// </summary>
    public class BattleEventHandler : IUnifiedEventHandler
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly ILogger _logger;

        public BattleEventHandler(IHubContext<GameHub> hubContext, ILogger logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task HandleAsync(UnifiedEvent evt)
        {
            var battleId = evt.ActorId.ToString();
            var groupName = $"battle-{battleId}";

            var eventData = new
            {
                EventType = evt.EventType,
                BattleId = battleId,
                ActorId = evt.ActorId,
                TargetId = evt.TargetId,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(evt.TimestampNs / 1_000_000).DateTime,
                Frame = evt.Frame
            };

            try
            {
                await _hubContext.Clients.Group(groupName).SendAsync("BattleEvent", eventData);
                _logger.LogDebug("Battle event {EventType} broadcasted for battle {BattleId}", evt.EventType, battleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast battle event {EventType} for battle {BattleId}", evt.EventType, battleId);
            }
        }
    }

    /// <summary>
    /// 伤害事件处理器
    /// </summary>
    public class DamageEventHandler : IUnifiedEventHandler
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly ILogger _logger;

        public DamageEventHandler(IHubContext<GameHub> hubContext, ILogger logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task HandleAsync(UnifiedEvent evt)
        {
            var damageData = evt.GetData<DamageEventData>();
            
            var eventData = new
            {
                EventType = "DamageDealt",
                ActorId = evt.ActorId.ToString(),
                TargetId = evt.TargetId.ToString(),
                Damage = damageData.Damage,
                ActualDamage = damageData.ActualDamage,
                IsCritical = damageData.IsCritical > 0,
                Frame = evt.Frame
            };

            try
            {
                // 通知相关用户
                await _hubContext.Clients.User(evt.ActorId.ToString()).SendAsync("DamageEvent", eventData);
                await _hubContext.Clients.User(evt.TargetId.ToString()).SendAsync("DamageEvent", eventData);
                
                _logger.LogDebug("Damage event processed: {ActorId} -> {TargetId} for {Damage} damage", 
                    evt.ActorId, evt.TargetId, damageData.ActualDamage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process damage event");
            }
        }
    }

    /// <summary>
    /// 技能事件处理器
    /// </summary>
    public class SkillEventHandler : IUnifiedEventHandler
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly ILogger _logger;

        public SkillEventHandler(IHubContext<GameHub> hubContext, ILogger logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task HandleAsync(UnifiedEvent evt)
        {
            // 技能使用事件的特殊处理
            var eventData = new
            {
                EventType = "SkillUsed",
                ActorId = evt.ActorId.ToString(),
                TargetId = evt.TargetId.ToString(),
                Frame = evt.Frame
            };

            await _hubContext.Clients.All.SendAsync("SkillEvent", eventData);
        }
    }

    /// <summary>
    /// 角色事件处理器
    /// </summary>
    public class CharacterEventHandler : IUnifiedEventHandler
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly ILogger _logger;

        public CharacterEventHandler(IHubContext<GameHub> hubContext, ILogger logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task HandleAsync(UnifiedEvent evt)
        {
            var characterId = evt.ActorId.ToString();
            
            if (evt.EventType == GameEventTypes.EXPERIENCE_GAINED)
            {
                var expData = evt.GetData<ExperienceEventData>();
                var eventData = new
                {
                    EventType = "ExperienceGained",
                    CharacterId = characterId,
                    Amount = expData.Amount,
                    NewLevel = expData.NewLevel,
                    ProfessionId = expData.ProfessionId
                };

                await _hubContext.Clients.User(characterId).SendAsync("CharacterEvent", eventData);
            }
            else
            {
                var eventData = new
                {
                    EventType = evt.EventType == GameEventTypes.CHARACTER_LEVEL_UP ? "LevelUp" : "CharacterChanged",
                    CharacterId = characterId,
                    Frame = evt.Frame
                };

                await _hubContext.Clients.User(characterId).SendAsync("CharacterEvent", eventData);
            }
        }
    }

    /// <summary>
    /// 物品事件处理器
    /// </summary>
    public class ItemEventHandler : IUnifiedEventHandler
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly ILogger _logger;

        public ItemEventHandler(IHubContext<GameHub> hubContext, ILogger logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task HandleAsync(UnifiedEvent evt)
        {
            var characterId = evt.ActorId.ToString();
            var eventData = new
            {
                EventType = "ItemAcquired",
                CharacterId = characterId,
                ItemId = evt.TargetId.ToString(),
                Frame = evt.Frame
            };

            await _hubContext.Clients.User(characterId).SendAsync("ItemEvent", eventData);
        }
    }

    /// <summary>
    /// 货币事件处理器
    /// </summary>
    public class CurrencyEventHandler : IUnifiedEventHandler
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly ILogger _logger;

        public CurrencyEventHandler(IHubContext<GameHub> hubContext, ILogger logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task HandleAsync(UnifiedEvent evt)
        {
            var characterId = evt.ActorId.ToString();
            var goldChange = evt.GetData<int>();
            
            var eventData = new
            {
                EventType = "GoldChanged",
                CharacterId = characterId,
                Amount = goldChange,
                Frame = evt.Frame
            };

            await _hubContext.Clients.User(characterId).SendAsync("CurrencyEvent", eventData);
        }
    }

    /// <summary>
    /// 状态变化事件处理器
    /// </summary>
    public class StateChangeEventHandler : IUnifiedEventHandler
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly ILogger _logger;

        public StateChangeEventHandler(IHubContext<GameHub> hubContext, ILogger logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task HandleAsync(UnifiedEvent evt)
        {
            // 通用状态变化通知
            await _hubContext.Clients.All.SendAsync("StateChanged", new
            {
                Frame = evt.Frame,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(evt.TimestampNs / 1_000_000).DateTime
            });
        }
    }

    /// <summary>
    /// 遗留系统兼容性处理器
    /// </summary>
    public class LegacyCompatibilityHandler : IUnifiedEventHandler
    {
        private readonly ServerEventService _legacyEventService;
        private readonly ILogger _logger;

        public LegacyCompatibilityHandler(ServerEventService legacyEventService, ILogger logger)
        {
            _legacyEventService = legacyEventService;
            _logger = logger;
        }

        public async Task HandleAsync(UnifiedEvent evt)
        {
            // 将新事件转换为旧事件格式
            var legacyEventType = ConvertToLegacyEventType(evt.EventType);
            if (legacyEventType.HasValue)
            {
                var legacyArgs = new BlazorWebGame.Shared.Events.GameEventArgs(
                    legacyEventType.Value, 
                    evt.ActorId.ToString());
                
                _legacyEventService.RaiseEvent(legacyArgs);
            }
        }

        private BlazorWebGame.Shared.Events.GameEventType? ConvertToLegacyEventType(ushort newEventType)
        {
            return newEventType switch
            {
                GameEventTypes.BATTLE_STARTED => BlazorWebGame.Shared.Events.GameEventType.CombatStarted,
                GameEventTypes.BATTLE_ENDED => BlazorWebGame.Shared.Events.GameEventType.CombatEnded,
                GameEventTypes.CHARACTER_LEVEL_UP => BlazorWebGame.Shared.Events.GameEventType.CharacterLevelUp,
                GameEventTypes.EXPERIENCE_GAINED => BlazorWebGame.Shared.Events.GameEventType.ExperienceGained,
                GameEventTypes.ITEM_ACQUIRED => BlazorWebGame.Shared.Events.GameEventType.ItemAcquired,
                GameEventTypes.GOLD_CHANGED => BlazorWebGame.Shared.Events.GameEventType.GoldChanged,
                GameEventTypes.STATE_CHANGED => BlazorWebGame.Shared.Events.GameEventType.GenericStateChanged,
                _ => null
            };
        }
    }

    /// <summary>
    /// 统一事件系统统计信息
    /// </summary>
    public struct UnifiedEventSystemStats
    {
        public QueueStatistics QueueStatistics;
        public DispatcherStatistics DispatcherStatistics;
        public int RegisteredHandlers;
        public long CurrentFrame;

        public override string ToString()
        {
            return $"Frame: {CurrentFrame}, Handlers: {RegisteredHandlers}, {QueueStatistics}, {DispatcherStatistics}";
        }
    }
}