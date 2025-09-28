using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlazorIdleGame.Client.Models.Core;
//using BlazorIdleGame.Client.Models.Activities;
using Microsoft.Extensions.Logging;

namespace BlazorIdleGame.Client.Services.Core
{
    public interface IGameSyncService
    {
        GameState? CurrentState { get; }
        bool IsConnected { get; }

        event EventHandler<GameState>? StateUpdated;
        event EventHandler<OfflineRewards>? OfflineRewardsReceived;

        Task InitializeAsync();
        Task<bool> SendActionAsync(string actionType, object parameters);
        void Dispose();
    }

    public class GameSyncService : IGameSyncService, IDisposable
    {
        private readonly IGameCommunicationService _communication;
        private readonly ILogger<GameSyncService> _logger;
        // 暂时注释掉未实现的服务
        // private readonly IActivityQueueService _queueService;
        // private readonly IPartyService _partyService;

        private Timer? _syncTimer;
        private GameState? _state;
        private bool _isConnected;
        private int _syncInterval = 5000;
        private readonly SemaphoreSlim _syncLock = new(1, 1);

        public GameState? CurrentState => _state;
        public bool IsConnected => _isConnected;

        public event EventHandler<GameState>? StateUpdated;
        public event EventHandler<OfflineRewards>? OfflineRewardsReceived;

        public GameSyncService(
            IGameCommunicationService communication,
            ILogger<GameSyncService> logger
        // 暂时注释掉未实现的服务
        // IActivityQueueService queueService,
        // IPartyService partyService
        )
        {
            _communication = communication;
            _logger = logger;
            // _queueService = queueService;
            // _partyService = partyService;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("初始化游戏同步服务");

            // 首次同步，可能包含离线收益
            await SyncStateAsync(true);

            // 启动定时同步
            StartPolling();
        }

        private void StartPolling()
        {
            _syncTimer?.Dispose();
            _syncTimer = new Timer(
                async _ => await SyncStateAsync(false),
                null,
                TimeSpan.FromMilliseconds(_syncInterval),
                TimeSpan.FromMilliseconds(_syncInterval)
            );
        }

        private async Task SyncStateAsync(bool isInitialSync)
        {
            if (!await _syncLock.WaitAsync(0))
                return;

            try
            {
                var url = $"api/game/state?v={_state?.Version ?? 0}";
                if (isInitialSync)
                    url += "&includeOffline=true";

                var response = await _communication.GetAsync<ApiResponse<GameStateResponse>>(url);

                if (response?.Success == true && response.Data != null)
                {
                    var data = response.Data;

                    // 处理离线收益
                    if (isInitialSync && data.OfflineRewards != null)
                    {
                        OfflineRewardsReceived?.Invoke(this, data.OfflineRewards);
                    }

                    // 更新状态
                    if (_state == null || _state.Version != data.State.Version)
                    {
                        _state = data.State;

                        // 注释掉未实现的功能
                        // 更新活动队列
                        // _queueService.UpdateQueues(_state.ActivityQueues);

                        // 更新组队状态
                        // if (_state.CurrentParty != null)
                        //     _partyService.UpdatePartyState(_state.CurrentParty);

                        // 调整同步频率
                        AdjustSyncInterval(_state);

                        StateUpdated?.Invoke(this, _state);
                    }

                    if (!_isConnected)
                    {
                        _isConnected = true;
                        _logger.LogInformation("已连接到服务器");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "同步游戏状态失败");
                _isConnected = false;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private void AdjustSyncInterval(GameState state)
        {
            // 简化版本的同步间隔调整
            var newInterval = 5000; // 默认5秒

            // 注释掉复杂的游戏状态检查逻辑
            /*
            // 检查所有活动队列
            var hasActiveActivities = state.ActivityQueues
                .SelectMany(q => q.Slots)
                .Any(s => s.Activity?.Status == ActivityStatus.Active);

            // 检查是否有即将触发的活动（10秒内）
            var hasSoonTrigger = state.ActivityQueues
                .SelectMany(q => q.Slots)
                .Where(s => s.Activity != null)
                .Any(s => s.Activity!.GetRemainingTime(DateTime.UtcNow).TotalSeconds < 10);

            // 检查是否在战斗中
            var inBattle = state.ActivityQueues
                .SelectMany(q => q.Slots)
                .Any(s => s.Activity?.Type == ActivityType.Battle &&
                         s.Activity.Status == ActivityStatus.Active);

            // 检查是否在副本中
            var inDungeon = state.ActivityQueues
                .SelectMany(q => q.Slots)
                .Any(s => s.Activity?.Type == ActivityType.DungeonBattle &&
                         s.Activity.Status == ActivityStatus.Active);

            if (inDungeon)
            {
                newInterval = 1000; // 副本中1秒同步
            }
            else if (inBattle || (state.CurrentParty?.InBattle == true))
            {
                newInterval = 2000; // 战斗中2秒同步
            }
            else if (hasSoonTrigger)
            {
                newInterval = 1000; // 即将触发时1秒同步
            }
            else if (hasActiveActivities)
            {
                newInterval = 5000; // 有活动时5秒同步
            }
            else
            {
                newInterval = 10000; // 空闲时10秒同步
            }
            */

            if (newInterval != _syncInterval)
            {
                _syncInterval = newInterval;
                StartPolling();
                _logger.LogDebug("调整同步间隔为 {Interval}ms", newInterval);
            }
        }

        public async Task<bool> SendActionAsync(string actionType, object parameters)
        {
            try
            {
                var action = new GameAction
                {
                    ActionType = actionType,
                    Parameters = ConvertToDict(parameters),
                    ClientTime = DateTime.UtcNow
                };

                var success = await _communication.PostAsync("api/game/action", action);

                if (success)
                {
                    // 立即同步新状态
                    await SyncStateAsync(false);
                    return true;
                }

                _logger.LogWarning("操作失败: {ActionType}", actionType);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送操作失败");
                return false;
            }
        }

        private Dictionary<string, object> ConvertToDict(object parameters)
        {
            // 简单的对象转字典实现
            var dict = new Dictionary<string, object>();
            var type = parameters.GetType();

            foreach (var prop in type.GetProperties())
            {
                var value = prop.GetValue(parameters);
                if (value != null)
                    dict[prop.Name] = value;
            }

            return dict;
        }

        public void Dispose()
        {
            _syncTimer?.Dispose();
            _syncLock?.Dispose();
        }
    }

    // 响应模型
    public class GameStateResponse
    {
        public GameState State { get; set; } = new();
        public OfflineRewards? OfflineRewards { get; set; }
    }

    public class GameState
    {
        public PlayerInfo Player { get; set; } = new();
        public Resources Resources { get; set; } = new();
        //public List<ActivityQueue> ActivityQueues { get; set; } = new();
        //public PartyInfo? CurrentParty { get; set; }
        public DateTime ServerTime { get; set; }
        public int Version { get; set; }
    }

    public class OfflineRewards
    {
        public TimeSpan OfflineDuration { get; set; }
        public TimeSpan EffectiveDuration { get; set; } // 实际结算时长（最大12小时）
        public Dictionary<string, long> ResourcesGained { get; set; } = new();
        public long ExperienceGained { get; set; }
        public List<OfflineActivity> ProcessedActivities { get; set; } = new();
        public string Summary { get; set; } = "";
    }

    public class OfflineActivity
    {
        public string ActivityName { get; set; } = "";
        public int CompletedCycles { get; set; }
        public Dictionary<string, long> Rewards { get; set; } = new();
    }

    // 游戏动作模型
    public class GameAction
    {
        public string ActionType { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime ClientTime { get; set; }
    }

    // API响应模型
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }
    }
}