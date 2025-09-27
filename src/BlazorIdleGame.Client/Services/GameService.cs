using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using BlazorIdleGame.Client.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace BlazorIdleGame.Client.Services
{
    public interface IGameService
    {
        GameState? CurrentState { get; }
        bool IsConnected { get; }
        bool IsLoading { get; }
        
        event EventHandler<GameState>? StateUpdated;
        event EventHandler<bool>? ConnectionChanged;
        event EventHandler<string>? ErrorOccurred;
        
        Task InitializeAsync();
        Task<bool> StartActivityAsync(ActivityType type, string target);
        Task<bool> StopActivityAsync(string activityId);
        Task<bool> SendActionAsync(GameAction action);
        void Dispose();
    }
    
    public class GameService : IGameService, IDisposable
    {
        private readonly HttpClient _http;
        private readonly ILogger<GameService> _logger;
        private readonly IPartyService _partyService;
        private readonly IBattleService _battleService;
        
        private Timer? _syncTimer;
        private GameState? _state;
        private bool _isConnected;
        private bool _isLoading;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private CancellationTokenSource? _pollingCts;
        private Task? _pollingTask;

        // 动态同步间隔
        private int _syncInterval = 5000;
        
        public GameState? CurrentState => _state;
        public bool IsConnected => _isConnected;
        public bool IsLoading => _isLoading;
        
        public event EventHandler<GameState>? StateUpdated;
        public event EventHandler<bool>? ConnectionChanged;
        public event EventHandler<string>? ErrorOccurred;
        private readonly List<WeakReference> _eventSubscriptions = new();

        public GameService(
            HttpClient http, 
            ILogger<GameService> logger,
            IPartyService partyService,
            IBattleService battleService)
        {
            _http = http;
            _logger = logger;
            _partyService = partyService;
            _battleService = battleService;
        }
        
        public async Task InitializeAsync()
        {
            _logger.LogInformation("初始化游戏服务");
            _isLoading = true;
            
            try
            {
                // 初始化子服务
                await _partyService.InitializeAsync();
                await _battleService.InitializeAsync();

                // 订阅子服务事件
                var partyHandler = new EventHandler<PartyInfo>(OnPartyUpdated);
                _eventSubscriptions.Add(new WeakReference(partyHandler));
                _partyService.PartyUpdated += partyHandler;
                var battleHandler = new EventHandler<BattleState>(OnBattleUpdated);
                _eventSubscriptions.Add(new WeakReference(battleHandler));
                _battleService.BattleUpdated += battleHandler;
                
                // 首次同步
                await SyncStateAsync();
                
                // 启动定时同步
                StartPolling();
                
                _isLoading = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化失败");
                _isLoading = false;
                ErrorOccurred?.Invoke(this, "初始化失败，请刷新页面重试");
            }
        }

        private void StartPolling()
        {
            // 先取消旧的轮询
            StopPolling();

            _pollingCts = new CancellationTokenSource();
            _pollingTask = Task.Run(async () =>
            {
                while (!_pollingCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await SyncStateAsync();
                        await Task.Delay(_syncInterval, _pollingCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, _pollingCts.Token);
        }

        private void StopPolling()
        {
            _pollingCts?.Cancel();
            _pollingTask?.Wait(TimeSpan.FromSeconds(1));
            _pollingCts?.Dispose();
            _pollingCts = null;
            _pollingTask = null;
        }

        private async Task SyncStateAsync()
        {
            // 1. 避免在高频调用时创建过多任务
            if (!await _lock.WaitAsync(0))
                return;

            try
            {
                var lastVersion = _state?.Version ?? 0;

                // 2. 复用 HttpRequestMessage 和添加超时控制
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                // 3. 只在版本变化时请求完整数据
                var url = lastVersion > 0
                    ? $"api/game/state/delta?v={lastVersion}"  // 增量更新
                    : "api/game/state";  // 完整数据

                var response = await _http.GetFromJsonAsync<ApiResponse<GameState>>(
                    url,
                    cts.Token);

                if (response?.Success == true && response.Data != null)
                {
                    var newState = response.Data;

                    // 4. 只在版本真正变化时处理
                    if (newState.Version > lastVersion)
                    {
                        // 5. 批量更新，减少事件触发次数
                        var stateChanged = false;
                        var partyChanged = false;
                        var battleChanged = false;

                        // 比较并标记变化
                        if (_state?.CurrentParty?.Version != newState.CurrentParty?.Version)
                        {
                            partyChanged = true;
                        }

                        if (_state?.CurrentBattle?.Version != newState.CurrentBattle?.Version)
                        {
                            battleChanged = true;
                        }

                        // 6. 复用对象而不是每次创建新的
                        if (_state == null)
                        {
                            _state = newState;
                            stateChanged = true;
                        }
                        else
                        {
                            // 更新现有对象的属性，而不是替换整个对象
                            UpdateStateProperties(_state, newState);
                            stateChanged = true;
                        }

                        // 7. 批量更新子服务
                        if (partyChanged && newState.CurrentParty != null)
                        {
                            _partyService.UpdatePartyState(newState.CurrentParty);
                        }

                        if (battleChanged && newState.CurrentBattle != null)
                        {
                            _battleService.UpdateBattleState(newState.CurrentBattle);
                        }

                        // 8. 只触发一次状态更新事件
                        if (stateChanged)
                        {
                            StateUpdated?.Invoke(this, _state);
                        }
                    }

                    // 9. 根据数据新鲜度调整同步间隔
                    AdjustSyncIntervalOptimized(newState);

                    // 10. 连接状态管理
                    if (!_isConnected)
                    {
                        _isConnected = true;
                        ConnectionChanged?.Invoke(this, true);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // 超时不记录错误，正常处理
                _logger.LogDebug("同步请求超时");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "网络请求失败");
                HandleConnectionError();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "同步状态失败");
            }
            finally
            {
                _lock.Release();
            }
        }

        private void UpdateStateProperties(GameState target, GameState source)
        {
            target.Version = source.Version;
            target.ServerTime = source.ServerTime;
            target.LastSyncTime = source.LastSyncTime;

            // 更新 Player 属性而不是替换对象
            if (source.Player != null)
            {
                if (target.Player == null)
                {
                    target.Player = source.Player;
                }
                else
                {
                    // 复用现有对象，只更新属性
                    target.Player.Level = source.Player.Level;
                    target.Player.Experience = source.Player.Experience;
                    // ... 其他属性
                }
            }

            // 更新 Resources
            if (source.Resources != null)
            {
                if (target.Resources == null)
                {
                    target.Resources = source.Resources;
                }
                else
                {
                    // 复用现有对象
                    target.Resources.Gold = source.Resources.Gold;
                }
            }

            // 更新 Activities 列表 - 使用对象池或复用
            if (source.Activities != null)
            {
                target.Activities.Clear();
                target.Activities.AddRange(source.Activities);
            }

            // 更新 Party 和 Battle
            target.CurrentParty = source.CurrentParty;
            target.CurrentBattle = source.CurrentBattle;
        }

        // 优化的同步间隔调整
        private void AdjustSyncIntervalOptimized(GameState state)
        {
            var newInterval = _syncInterval;

            // 使用优先级系统
            if (state.CurrentBattle?.IsActive == true)
            {
                // 战斗最高优先级
                newInterval = state.CurrentBattle.IsPartyBattle ? 500 : 1000;
            }
            else if (state.CurrentParty?.IsInActivity == true)
            {
                // 组队活动次优先级
                newInterval = 1500;
            }
            else if (state.Activities?.Any(a => a.TimeRemaining.TotalSeconds < 10) == true)
            {
                // 活动即将结束
                newInterval = 2000;
            }
            else
            {
                // 空闲状态 - 降低频率
                newInterval = 10000;
            }

            // 只在变化超过20%时才调整
            var changeRatio = Math.Abs(newInterval - _syncInterval) / (double)_syncInterval;
            if (changeRatio > 0.2)
            {
                _logger.LogDebug($"调整同步间隔: {_syncInterval}ms -> {newInterval}ms");
                _syncInterval = newInterval;
            }
        }

        private void AdjustSyncInterval(GameState state)
        {
            var newInterval = _syncInterval;

            if (state.CurrentBattle?.IsActive == true)
            {
                // 战斗中加快同步
                newInterval = state.CurrentBattle.IsPartyBattle ? 1000 : 2000;
            }
            else if (state.CurrentParty?.IsInActivity == true)
            {
                // 组队活动中
                newInterval = 2000;
            }
            else if (state.Activities.Any(a => a.TimeRemaining.TotalSeconds < 10))
            {
                // 活动即将结束
                newInterval = 1000;
            }
            else
            {
                // 正常状态
                newInterval = 5000;
            }

            if (Math.Abs(newInterval - _syncInterval) > 500) // 添加阈值，避免频繁调整
            {
                _syncInterval = newInterval;
                // 不需要重启轮询，下次循环会使用新间隔
            }
        }

        public async Task<bool> StartActivityAsync(ActivityType type, string target)
        {
            var action = new GameAction
            {
                ActionType = "StartActivity",
                Parameters = new()
                {
                    ["type"] = type.ToString(),
                    ["target"] = target,
                    ["isPartyActivity"] = type == ActivityType.PartyBattle || type == ActivityType.PartyRaid
                }
            };
            
            return await SendActionAsync(action);
        }
        
        public async Task<bool> StopActivityAsync(string activityId)
        {
            var action = new GameAction
            {
                ActionType = "StopActivity",
                Parameters = new()
                {
                    ["activityId"] = activityId
                }
            };
            
            return await SendActionAsync(action);
        }
        
        public async Task<bool> SendActionAsync(GameAction action)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/game/action", action);
                
                if (response.IsSuccessStatusCode)
                {
                    // 立即同步新状态
                    await SyncStateAsync();
                    return true;
                }
                
                var error = await response.Content.ReadAsStringAsync();
                ErrorOccurred?.Invoke(this, $"操作失败: {error}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送操作失败");
                ErrorOccurred?.Invoke(this, "操作失败，请重试");
                return false;
            }
        }
        
        private void OnPartyUpdated(object? sender, PartyInfo party)
        {
            if (_state != null)
            {
                _state.CurrentParty = party;
                StateUpdated?.Invoke(this, _state);
            }
        }
        
        private void OnBattleUpdated(object? sender, BattleState battle)
        {
            if (_state != null)
            {
                _state.CurrentBattle = battle;
                StateUpdated?.Invoke(this, _state);
            }
        }

        // 添加连接恢复机制
        private int _connectionRetryCount = 0;
        private const int MaxRetryCount = 3;

        private void HandleConnectionError()
        {
            if (_isConnected)
            {
                _isConnected = false;
                ConnectionChanged?.Invoke(this, false);

                if (_connectionRetryCount < MaxRetryCount)
                {
                    _connectionRetryCount++;
                    var retryDelay = Math.Min(1000 * Math.Pow(2, _connectionRetryCount), 30000);
                    _logger.LogInformation($"连接断开，{retryDelay / 1000}秒后重试...");
                    ErrorOccurred?.Invoke(this, $"连接已断开，正在重连...({_connectionRetryCount}/{MaxRetryCount})");
                }
                else
                {
                    _logger.LogError("达到最大重试次数，停止重连");
                    ErrorOccurred?.Invoke(this, "无法连接到服务器，请检查网络后刷新页面");
                    StopPolling();
                }
            }
        }

        private void OnConnectionRestored()
        {
            _connectionRetryCount = 0;
        }

        public void Dispose()
        {
            StopPolling(); // 确保停止轮询
            _lock?.Dispose();

            // 清理所有事件订阅
            foreach (var weakRef in _eventSubscriptions)
            {
                if (weakRef.IsAlive && weakRef.Target is Delegate handler)
                {
                    // 根据类型取消订阅
                    if (handler is EventHandler<PartyInfo> partyHandler)
                        _partyService.PartyUpdated -= partyHandler;
                    // ... 其他类型
                }
            }
            _eventSubscriptions.Clear();

            _partyService?.Dispose();
            _battleService?.Dispose();
        }
    }
}