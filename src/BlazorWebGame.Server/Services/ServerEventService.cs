using BlazorWebGame.Shared.Events;
using BlazorWebGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlazorWebGame.Server.Services
{
    /// <summary>
    /// 服务端事件管理服务，负责事件的处理和SignalR广播
    /// </summary>
    public class ServerEventService
    {
        private readonly GameEventManager _eventManager;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly ILogger<ServerEventService> _logger;

        public ServerEventService(GameEventManager eventManager, IHubContext<GameHub> hubContext, ILogger<ServerEventService> logger)
        {
            _eventManager = eventManager;
            _hubContext = hubContext;
            _logger = logger;

            // 订阅所有游戏事件以进行日志记录和SignalR广播
            SubscribeToGameEvents();
        }

        /// <summary>
        /// 触发游戏事件
        /// </summary>
        public void RaiseEvent(GameEventArgs eventArgs)
        {
            _eventManager.Raise(eventArgs);
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe(GameEventType eventType, Action<GameEventArgs> handler)
        {
            _eventManager.Subscribe(eventType, handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public bool Unsubscribe(GameEventType eventType, Action<GameEventArgs> handler)
        {
            return _eventManager.Unsubscribe(eventType, handler);
        }

        /// <summary>
        /// 向特定用户发送事件通知
        /// </summary>
        public async Task NotifyUserAsync(string userId, GameEventArgs eventArgs)
        {
            try
            {
                await _hubContext.Clients.User(userId).SendAsync("GameEvent", new
                {
                    EventType = eventArgs.EventType.ToString(),
                    PlayerId = eventArgs.PlayerId,
                    Timestamp = eventArgs.Timestamp,
                    Data = eventArgs.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send event {eventArgs.EventType} to user {userId}");
            }
        }

        /// <summary>
        /// 向所有用户广播事件
        /// </summary>
        public async Task BroadcastEventAsync(GameEventArgs eventArgs)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("GameEvent", new
                {
                    EventType = eventArgs.EventType.ToString(),
                    PlayerId = eventArgs.PlayerId,
                    Timestamp = eventArgs.Timestamp,
                    Data = eventArgs.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to broadcast event {eventArgs.EventType}");
            }
        }

        /// <summary>
        /// 向特定组发送事件通知
        /// </summary>
        public async Task NotifyGroupAsync(string groupName, GameEventArgs eventArgs)
        {
            try
            {
                await _hubContext.Clients.Group(groupName).SendAsync("GameEvent", new
                {
                    EventType = eventArgs.EventType.ToString(),
                    PlayerId = eventArgs.PlayerId,
                    Timestamp = eventArgs.Timestamp,
                    Data = eventArgs.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send event {eventArgs.EventType} to group {groupName}");
            }
        }

        /// <summary>
        /// 订阅游戏事件进行处理
        /// </summary>
        private void SubscribeToGameEvents()
        {
            // 订阅角色相关事件
            _eventManager.Subscribe(GameEventType.CharacterCreated, OnCharacterEvent);
            _eventManager.Subscribe(GameEventType.CharacterLevelUp, OnCharacterEvent);
            _eventManager.Subscribe(GameEventType.LevelUp, OnCharacterEvent);
            _eventManager.Subscribe(GameEventType.ExperienceGained, OnCharacterEvent);
            _eventManager.Subscribe(GameEventType.CharacterStatChanged, OnCharacterEvent);
            _eventManager.Subscribe(GameEventType.ActiveCharacterChanged, OnCharacterEvent);

            // 订阅战斗相关事件
            _eventManager.Subscribe(GameEventType.CombatStarted, OnCombatEvent);
            _eventManager.Subscribe(GameEventType.CombatEnded, OnCombatEvent);
            _eventManager.Subscribe(GameEventType.BattleCompleted, OnCombatEvent);
            _eventManager.Subscribe(GameEventType.BattleDefeated, OnCombatEvent);

            // 订阅物品相关事件
            _eventManager.Subscribe(GameEventType.ItemAcquired, OnItemEvent);
            _eventManager.Subscribe(GameEventType.ItemSold, OnItemEvent);
            _eventManager.Subscribe(GameEventType.GoldChanged, OnItemEvent);

            // 订阅通用状态变化事件
            _eventManager.Subscribe(GameEventType.GenericStateChanged, OnGenericStateChanged);

            _logger.LogInformation("Subscribed to game events for SignalR broadcasting");
        }

        /// <summary>
        /// 处理角色相关事件
        /// </summary>
        private async void OnCharacterEvent(GameEventArgs eventArgs)
        {
            _logger.LogInformation($"Character event: {eventArgs.EventType}, Player: {eventArgs.PlayerId}");

            // 如果有特定玩家，只通知该玩家
            if (!string.IsNullOrEmpty(eventArgs.PlayerId))
            {
                await NotifyUserAsync(eventArgs.PlayerId, eventArgs);
            }
            else
            {
                // 否则广播给所有用户
                await BroadcastEventAsync(eventArgs);
            }
        }

        /// <summary>
        /// 处理战斗相关事件
        /// </summary>
        private async void OnCombatEvent(GameEventArgs eventArgs)
        {
            _logger.LogInformation($"Combat event: {eventArgs.EventType}, Player: {eventArgs.PlayerId}");

            // 战斗事件可能需要通知队伍成员或相关玩家
            if (!string.IsNullOrEmpty(eventArgs.PlayerId))
            {
                await NotifyUserAsync(eventArgs.PlayerId, eventArgs);
                
                // 如果有队伍信息，也通知队伍成员
                if (eventArgs.Data is { } data && data.GetType().GetProperty("PartyId")?.GetValue(data) is string partyId)
                {
                    await NotifyGroupAsync($"party_{partyId}", eventArgs);
                }
            }
        }

        /// <summary>
        /// 处理物品相关事件
        /// </summary>
        private async void OnItemEvent(GameEventArgs eventArgs)
        {
            _logger.LogInformation($"Item event: {eventArgs.EventType}, Player: {eventArgs.PlayerId}");

            // 物品事件通常只通知相关玩家
            if (!string.IsNullOrEmpty(eventArgs.PlayerId))
            {
                await NotifyUserAsync(eventArgs.PlayerId, eventArgs);
            }
        }

        /// <summary>
        /// 处理通用状态变化事件
        /// </summary>
        private async void OnGenericStateChanged(GameEventArgs eventArgs)
        {
            // 对于通用状态变化，我们不重复广播，只记录日志
            _logger.LogDebug($"Generic state changed event triggered by: {eventArgs.EventType}");
            
            // 通知相关玩家刷新状态
            if (!string.IsNullOrEmpty(eventArgs.PlayerId))
            {
                await _hubContext.Clients.User(eventArgs.PlayerId).SendAsync("RefreshState");
            }
        }
    }
}