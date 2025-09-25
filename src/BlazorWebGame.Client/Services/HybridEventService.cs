using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Shared.Events;
using Microsoft.AspNetCore.SignalR.Client;
using SharedGameEventManager = BlazorWebGame.Shared.Events.GameEventManager;
using SharedGameEventType = BlazorWebGame.Shared.Events.GameEventType;
using SharedGameEventArgs = BlazorWebGame.Shared.Events.GameEventArgs;

namespace BlazorWebGame.Client.Services
{
    /// <summary>
    /// 混合事件服务 - 可以使用本地事件管理器或通过SignalR与服务器通信
    /// </summary>
    public class HybridEventService : IAsyncDisposable
    {
        private readonly SharedGameEventManager _localEventManager;
        private readonly ILogger<HybridEventService> _logger;
        private HubConnection? _hubConnection;
        private bool _useServerMode = false;
        private string? _serverUrl;

        /// <summary>
        /// 是否启用服务器模式
        /// </summary>
        public bool UseServerMode 
        { 
            get => _useServerMode; 
            set
            {
                if (_useServerMode != value)
                {
                    _useServerMode = value;
                    _logger.LogInformation($"Event service switched to {(value ? "server" : "local")} mode");
                }
            }
        }

        public HybridEventService(ILogger<HybridEventService> logger)
        {
            _localEventManager = new SharedGameEventManager();
            _logger = logger;
        }

        /// <summary>
        /// 初始化事件服务
        /// </summary>
        public async Task InitializeAsync(string? serverUrl = null, bool useServerMode = false)
        {
            _serverUrl = serverUrl;
            UseServerMode = useServerMode;

            if (UseServerMode && !string.IsNullOrEmpty(_serverUrl))
            {
                await InitializeServerConnectionAsync();
            }
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe(SharedGameEventType eventType, Action<SharedGameEventArgs> handler)
        {
            _localEventManager.Subscribe(eventType, handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public bool Unsubscribe(SharedGameEventType eventType, Action<SharedGameEventArgs> handler)
        {
            return _localEventManager.Unsubscribe(eventType, handler);
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public void RaiseEvent(SharedGameEventArgs args)
        {
            // 始终在本地处理事件
            _localEventManager.Raise(args);

            // 如果启用服务器模式，也发送到服务器
            if (UseServerMode && _hubConnection?.State == HubConnectionState.Connected)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _hubConnection.SendAsync("TriggerGameEvent", new
                        {
                            EventType = args.EventType.ToString(),
                            PlayerId = args.PlayerId,
                            Timestamp = args.Timestamp,
                            Data = args.Data
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send event to server: {EventType}", args.EventType);
                    }
                });
            }
        }

        /// <summary>
        /// 触发简单游戏事件
        /// </summary>
        public void RaiseEvent(SharedGameEventType eventType, string? playerId = null, object? data = null)
        {
            RaiseEvent(new SharedGameEventArgs(eventType, playerId, data));
        }

        /// <summary>
        /// 兼容性方法：与旧的GameStateService兼容
        /// </summary>
        public void RaiseEvent(BlazorWebGame.Events.GameEventArgs args)
        {
            // 转换旧的事件参数到新的格式
            var newEventType = ConvertLegacyEventType(args.EventType);
            var newArgs = new SharedGameEventArgs(newEventType, args.Player?.Id, args);
            RaiseEvent(newArgs);
        }

        /// <summary>
        /// 兼容性方法：触发简单游戏事件
        /// </summary>
        public void RaiseEvent(BlazorWebGame.Events.GameEventType eventType, Player? player = null)
        {
            var newEventType = ConvertLegacyEventType(eventType);
            RaiseEvent(new SharedGameEventArgs(newEventType, player?.Id));
        }

        /// <summary>
        /// 清除所有事件订阅
        /// </summary>
        public void ClearAllSubscriptions()
        {
            _localEventManager.ClearAllSubscriptions();
        }

        /// <summary>
        /// 清除特定事件类型的所有订阅
        /// </summary>
        public void ClearSubscriptions(SharedGameEventType eventType)
        {
            _localEventManager.ClearSubscriptions(eventType);
        }

        /// <summary>
        /// 初始化与服务器的连接
        /// </summary>
        private async Task InitializeServerConnectionAsync()
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{_serverUrl}/gamehub")
                    .Build();

                // 订阅服务器发送的游戏事件
                _hubConnection.On<object>("GameEvent", OnServerGameEvent);
                _hubConnection.On("RefreshState", OnServerRefreshState);

                await _hubConnection.StartAsync();
                _logger.LogInformation("Connected to event hub");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to event hub");
                _hubConnection = null;
                UseServerMode = false; // 回退到本地模式
            }
        }

        /// <summary>
        /// 处理服务器发送的游戏事件
        /// </summary>
        private void OnServerGameEvent(object eventData)
        {
            try
            {
                _logger.LogDebug("Received game event from server: {EventData}", eventData);
                
                // 触发通用状态变化事件，让UI知道需要刷新
                _localEventManager.Raise(new SharedGameEventArgs(SharedGameEventType.GenericStateChanged));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process server game event");
            }
        }

        /// <summary>
        /// 处理服务器刷新状态通知
        /// </summary>
        private void OnServerRefreshState()
        {
            _logger.LogDebug("Received refresh state from server");
            _localEventManager.Raise(new SharedGameEventArgs(SharedGameEventType.GenericStateChanged));
        }

        /// <summary>
        /// 转换旧的事件类型到新的格式
        /// </summary>
        private SharedGameEventType ConvertLegacyEventType(BlazorWebGame.Events.GameEventType legacyType)
        {
            return legacyType switch
            {
                BlazorWebGame.Events.GameEventType.CharacterCreated => SharedGameEventType.CharacterCreated,
                BlazorWebGame.Events.GameEventType.CharacterLevelUp => SharedGameEventType.CharacterLevelUp,
                BlazorWebGame.Events.GameEventType.CharacterDeath => SharedGameEventType.CharacterDeath,
                BlazorWebGame.Events.GameEventType.CharacterRevived => SharedGameEventType.CharacterRevived,
                BlazorWebGame.Events.GameEventType.CharacterStatChanged => SharedGameEventType.CharacterStatChanged,
                BlazorWebGame.Events.GameEventType.ActiveCharacterChanged => SharedGameEventType.ActiveCharacterChanged,
                BlazorWebGame.Events.GameEventType.LevelUp => SharedGameEventType.LevelUp,
                BlazorWebGame.Events.GameEventType.ExperienceGained => SharedGameEventType.ExperienceGained,
                BlazorWebGame.Events.GameEventType.CombatStarted => SharedGameEventType.CombatStarted,
                BlazorWebGame.Events.GameEventType.CombatEnded => SharedGameEventType.CombatEnded,
                BlazorWebGame.Events.GameEventType.EnemyDamaged => SharedGameEventType.EnemyDamaged,
                BlazorWebGame.Events.GameEventType.EnemyKilled => SharedGameEventType.EnemyKilled,
                BlazorWebGame.Events.GameEventType.PlayerDamaged => SharedGameEventType.PlayerDamaged,
                BlazorWebGame.Events.GameEventType.SkillUsed => SharedGameEventType.SkillUsed,
                BlazorWebGame.Events.GameEventType.DungeonWaveStarted => SharedGameEventType.DungeonWaveStarted,
                BlazorWebGame.Events.GameEventType.BattleCompleted => SharedGameEventType.BattleCompleted,
                BlazorWebGame.Events.GameEventType.BattleDefeated => SharedGameEventType.BattleDefeated,
                BlazorWebGame.Events.GameEventType.DungeonCompleted => SharedGameEventType.DungeonCompleted,
                BlazorWebGame.Events.GameEventType.CombatStatusChanged => SharedGameEventType.CombatStatusChanged,
                BlazorWebGame.Events.GameEventType.BattleCancelled => SharedGameEventType.BattleCancelled,
                BlazorWebGame.Events.GameEventType.AttackMissed => SharedGameEventType.AttackMissed,
                BlazorWebGame.Events.GameEventType.ItemAcquired => SharedGameEventType.ItemAcquired,
                BlazorWebGame.Events.GameEventType.ItemSold => SharedGameEventType.ItemSold,
                BlazorWebGame.Events.GameEventType.ItemUsed => SharedGameEventType.ItemUsed,
                BlazorWebGame.Events.GameEventType.ItemEquipped => SharedGameEventType.ItemEquipped,
                BlazorWebGame.Events.GameEventType.ItemUnequipped => SharedGameEventType.ItemUnequipped,
                BlazorWebGame.Events.GameEventType.GoldChanged => SharedGameEventType.GoldChanged,
                BlazorWebGame.Events.GameEventType.InventoryFull => SharedGameEventType.InventoryFull,
                BlazorWebGame.Events.GameEventType.PartyCreated => SharedGameEventType.PartyCreated,
                BlazorWebGame.Events.GameEventType.PartyJoined => SharedGameEventType.PartyJoined,
                BlazorWebGame.Events.GameEventType.PartyLeft => SharedGameEventType.PartyLeft,
                BlazorWebGame.Events.GameEventType.PartyDisbanded => SharedGameEventType.PartyDisbanded,
                BlazorWebGame.Events.GameEventType.GatheringStarted => SharedGameEventType.GatheringStarted,
                BlazorWebGame.Events.GameEventType.GatheringCompleted => SharedGameEventType.GatheringCompleted,
                BlazorWebGame.Events.GameEventType.CraftingStarted => SharedGameEventType.CraftingStarted,
                BlazorWebGame.Events.GameEventType.CraftingCompleted => SharedGameEventType.CraftingCompleted,
                BlazorWebGame.Events.GameEventType.ProfessionLevelUp => SharedGameEventType.ProfessionLevelUp,
                BlazorWebGame.Events.GameEventType.QuestAccepted => SharedGameEventType.QuestAccepted,
                BlazorWebGame.Events.GameEventType.QuestUpdated => SharedGameEventType.QuestUpdated,
                BlazorWebGame.Events.GameEventType.QuestCompleted => SharedGameEventType.QuestCompleted,
                BlazorWebGame.Events.GameEventType.DailyQuestsRefreshed => SharedGameEventType.DailyQuestsRefreshed,
                BlazorWebGame.Events.GameEventType.WeeklyQuestsRefreshed => SharedGameEventType.WeeklyQuestsRefreshed,
                BlazorWebGame.Events.GameEventType.GameInitialized => SharedGameEventType.GameInitialized,
                BlazorWebGame.Events.GameEventType.GameStateLoaded => SharedGameEventType.GameStateLoaded,
                BlazorWebGame.Events.GameEventType.GameStateSaved => SharedGameEventType.GameStateSaved,
                BlazorWebGame.Events.GameEventType.GameError => SharedGameEventType.GameError,
                BlazorWebGame.Events.GameEventType.GenericStateChanged => SharedGameEventType.GenericStateChanged,
                _ => SharedGameEventType.GenericStateChanged
            };
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}