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
    /// 简化事件服务 - 现在只使用服务器模式
    /// </summary>
    public class HybridEventService : IAsyncDisposable
    {
        private readonly SharedGameEventManager _localEventManager; // 仅用于UI兼容性
        private readonly ILogger<HybridEventService> _logger;
        private HubConnection? _hubConnection;
        private string? _serverUrl;

        /// <summary>
        /// 是否启用服务器模式 - 现在总是true
        /// </summary>
        [Obsolete("事件服务现在总是使用服务器模式")]
        public bool UseServerMode 
        { 
            get => true; 
            set => _logger.LogInformation("Event service is now always in server mode");
        }

        public HybridEventService(ILogger<HybridEventService> logger)
        {
            _localEventManager = new SharedGameEventManager();
            _logger = logger;
        }

        /// <summary>
        /// 初始化事件服务 - 现在只使用服务器模式
        /// </summary>
        public async Task InitializeAsync(string? serverUrl = null, bool useServerMode = true)
        {
            _serverUrl = serverUrl ?? "https://localhost:7000";

            try
            {
                await InitializeServerConnection();
                _logger.LogInformation("Event service initialized in server mode");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize server connection for events");
            }
        }

        /// <summary>
        /// 初始化服务器连接
        /// </summary>
        private async Task InitializeServerConnection()
        {
            if (string.IsNullOrEmpty(_serverUrl))
            {
                _logger.LogWarning("Server URL not provided for event service");
                return;
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_serverUrl}/gamehub")
                .Build();

            // 监听服务器事件
            _hubConnection.On<string, object>("GameEvent", OnServerGameEvent);

            await _hubConnection.StartAsync();
            _logger.LogInformation("Connected to server event hub");
        }

        /// <summary>
        /// 处理服务器游戏事件
        /// </summary>
        private void OnServerGameEvent(string eventType, object eventData)
        {
            _logger.LogDebug("Received server event: {EventType}", eventType);
            
            // 转发到本地事件管理器以保持UI兼容性
            if (Enum.TryParse<SharedGameEventType>(eventType, out var parsedType))
            {
                var args = new SharedGameEventArgs(parsedType, eventData?.ToString());
                _localEventManager.Raise(args);
            }
        }

        /// <summary>
        /// 订阅事件 - 使用本地事件管理器以保持UI兼容性
        /// </summary>
        public void Subscribe(SharedGameEventType eventType, Action<SharedGameEventArgs> handler)
        {
            _localEventManager.Subscribe(eventType, handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe(SharedGameEventType eventType, Action<SharedGameEventArgs> handler)
        {
            _localEventManager.Unsubscribe(eventType, handler);
        }

        /// <summary>
        /// 触发事件 - 现在只发送到服务器
        /// </summary>
        public async Task RaiseEventAsync(SharedGameEventType eventType, object? data = null)
        {
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.SendAsync("TriggerGameEvent", eventType.ToString(), data);
                }
                else
                {
                    _logger.LogWarning("Cannot raise event - no server connection");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising event to server");
            }
        }

        /// <summary>
        /// 本地触发事件（仅用于UI兼容性）
        /// </summary>
        [Obsolete("建议使用异步版本 RaiseEventAsync")]
        public void RaiseEvent(SharedGameEventType eventType, object? data = null)
        {
            _ = Task.Run(async () => await RaiseEventAsync(eventType, data));
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
