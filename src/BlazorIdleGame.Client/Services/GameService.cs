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
        
        // 动态同步间隔
        private int _syncInterval = 5000;
        
        public GameState? CurrentState => _state;
        public bool IsConnected => _isConnected;
        public bool IsLoading => _isLoading;
        
        public event EventHandler<GameState>? StateUpdated;
        public event EventHandler<bool>? ConnectionChanged;
        public event EventHandler<string>? ErrorOccurred;
        
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
                _partyService.PartyUpdated += OnPartyUpdated;
                _battleService.BattleUpdated += OnBattleUpdated;
                
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
            _syncTimer?.Dispose();
            _syncTimer = new Timer(
                async _ => await SyncStateAsync(),
                null,
                TimeSpan.FromMilliseconds(_syncInterval),
                TimeSpan.FromMilliseconds(_syncInterval)
            );
        }
        
        private async Task SyncStateAsync()
        {
            if (!await _lock.WaitAsync(0))
                return;
                
            try
            {
                var response = await _http.GetFromJsonAsync<ApiResponse<GameState>>(
                    $"api/game/state?v={_state?.Version ?? 0}");
                
                if (response?.Success == true && response.Data != null)
                {
                    var newState = response.Data;
                    
                    // 根据状态调整同步频率
                    AdjustSyncInterval(newState);
                    
                    if (_state == null || _state.Version != newState.Version)
                    {
                        _state = newState;
                        StateUpdated?.Invoke(this, newState);
                        
                        // 更新子服务状态
                        if (newState.CurrentParty != null)
                            _partyService.UpdatePartyState(newState.CurrentParty);
                        if (newState.CurrentBattle != null)
                            _battleService.UpdateBattleState(newState.CurrentBattle);
                    }
                    
                    if (!_isConnected)
                    {
                        _isConnected = true;
                        ConnectionChanged?.Invoke(this, true);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "网络请求失败");
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
            
            if (newInterval != _syncInterval)
            {
                _syncInterval = newInterval;
                StartPolling(); // 重启定时器
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
        
        private void HandleConnectionError()
        {
            if (_isConnected)
            {
                _isConnected = false;
                ConnectionChanged?.Invoke(this, false);
                ErrorOccurred?.Invoke(this, "连接已断开，正在重连...");
            }
        }
        
        public void Dispose()
        {
            _syncTimer?.Dispose();
            _lock?.Dispose();
            _partyService.PartyUpdated -= OnPartyUpdated;
            _battleService.BattleUpdated -= OnBattleUpdated;
            _partyService.Dispose();
            _battleService.Dispose();
        }
    }
}