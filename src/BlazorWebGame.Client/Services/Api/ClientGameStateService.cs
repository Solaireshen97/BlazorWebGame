using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Services;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 新的客户端游戏状态服务，使用服务器通信替代本地计时器
/// </summary>
public class ClientGameStateService : IAsyncDisposable
{
    private readonly GameApiService _gameApi;
    private readonly GameStateService _gameState; // 获取当前角色信息
    private readonly ILogger<ClientGameStateService> _logger;
    private HubConnection? _hubConnection;
    private System.Threading.Timer? _pollingTimer;
    private readonly Dictionary<Guid, BattleStateDto> _activeBattles = new();
    
    // 事件
    public event Action<BattleStateDto>? OnBattleStateChanged;
    public event Action<bool>? OnConnectionStatusChanged;

    // 配置
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMilliseconds(1000); // 1秒轮询一次
    private bool _isConnected = false;
    private bool _isDisposed = false;

    public bool IsConnected => _isConnected;
    public IReadOnlyDictionary<Guid, BattleStateDto> ActiveBattles => _activeBattles;

    public ClientGameStateService(GameApiService gameApi, GameStateService gameState, ILogger<ClientGameStateService> logger)
    {
        _gameApi = gameApi;
        _gameState = gameState;
        _logger = logger;
    }

    /// <summary>
    /// 初始化服务，建立SignalR连接
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isDisposed) return;

        try
        {
            // 建立SignalR连接
            await InitializeSignalRConnection();
            
            // 开始轮询作为备用机制
            StartPolling();
            
            _logger.LogInformation("ClientGameStateService initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ClientGameStateService");
            // 即使SignalR失败，也要启动轮询机制
            StartPolling();
        }
    }

    /// <summary>
    /// 建立SignalR连接
    /// </summary>
    private async Task InitializeSignalRConnection()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_gameApi.BaseUrl}/gamehub")
            .WithAutomaticReconnect()
            .Build();

        // 监听战斗更新事件
        _hubConnection.On<BattleStateDto>("BattleUpdate", HandleBattleUpdate);

        // 监听连接状态变化
        _hubConnection.Reconnecting += (ex) =>
        {
            _logger.LogWarning("SignalR connection lost, reconnecting...");
            UpdateConnectionStatus(false);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += (connectionId) =>
        {
            _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
            UpdateConnectionStatus(true);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += (ex) =>
        {
            _logger.LogWarning("SignalR connection closed");
            UpdateConnectionStatus(false);
            return Task.CompletedTask;
        };

        // 启动连接
        await _hubConnection.StartAsync();
        UpdateConnectionStatus(true);
        _logger.LogInformation("SignalR connection established");
    }

    /// <summary>
    /// 开始战斗（向服务器发送请求）
    /// </summary>
    public async Task<bool> StartBattleAsync(string enemyId, string? partyId = null)
    {
        try
        {
            // 从GameStateService获取当前激活的角色ID
            var characterId = _gameState.ActiveCharacter?.Id ?? "unknown-character";
            
            var request = new StartBattleRequest
            {
                CharacterId = characterId,
                EnemyId = enemyId,
                PartyId = partyId
            };

            var response = await _gameApi.StartBattleAsync(request);
            
            if (response.Success && response.Data != null)
            {
                // 加入SignalR组以接收实时更新
                if (_hubConnection?.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("JoinBattle", response.Data.BattleId.ToString());
                }

                // 更新本地状态
                _activeBattles[response.Data.BattleId] = response.Data;
                OnBattleStateChanged?.Invoke(response.Data);
                
                _logger.LogInformation("Battle started successfully: {BattleId}", response.Data.BattleId);
                return true;
            }

            _logger.LogWarning("Failed to start battle: {Message}", response.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting battle with enemy {EnemyId}", enemyId);
            return false;
        }
    }

    /// <summary>
    /// 执行战斗动作
    /// </summary>
    public async Task<bool> ExecuteBattleActionAsync(Guid battleId, string playerId, BattleActionType actionType, string? targetId = null, string? skillId = null)
    {
        try
        {
            var request = new BattleActionRequest
            {
                BattleId = battleId,
                PlayerId = playerId,
                ActionType = actionType,
                TargetId = targetId,
                SkillId = skillId
            };

            var response = await _gameApi.ExecuteBattleActionAsync(request);
            
            if (response.Success)
            {
                _logger.LogDebug("Battle action executed successfully for battle {BattleId}", battleId);
                return true;
            }

            _logger.LogWarning("Failed to execute battle action: {Message}", response.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing battle action for battle {BattleId}", battleId);
            return false;
        }
    }

    /// <summary>
    /// 停止战斗
    /// </summary>
    public async Task<bool> StopBattleAsync(Guid battleId)
    {
        try
        {
            var response = await _gameApi.StopBattleAsync(battleId);
            
            if (response.Success)
            {
                // 离开SignalR组
                if (_hubConnection?.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("LeaveBattle", battleId.ToString());
                }

                // 更新本地状态
                if (_activeBattles.TryGetValue(battleId, out var battle))
                {
                    battle.IsActive = false;
                    OnBattleStateChanged?.Invoke(battle);
                    _activeBattles.Remove(battleId);
                }
                
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping battle {BattleId}", battleId);
            return false;
        }
    }

    /// <summary>
    /// 开始轮询（备用机制）
    /// </summary>
    private void StartPolling()
    {
        _pollingTimer = new System.Threading.Timer(async _ =>
        {
            if (_isDisposed) return;

            try
            {
                // 检查服务器可用性
                var isAvailable = await _gameApi.IsServerAvailableAsync();
                UpdateConnectionStatus(isAvailable);

                // 轮询活跃战斗状态
                if (isAvailable && _activeBattles.Any())
                {
                    await PollActiveBattles();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during polling");
                UpdateConnectionStatus(false);
            }
        }, null, TimeSpan.Zero, _pollingInterval);
    }

    /// <summary>
    /// 轮询活跃战斗状态
    /// </summary>
    private async Task PollActiveBattles()
    {
        var battleIds = _activeBattles.Keys.ToList();
        
        foreach (var battleId in battleIds)
        {
            try
            {
                var response = await _gameApi.GetBattleStateAsync(battleId);
                if (response.Success && response.Data != null)
                {
                    HandleBattleUpdate(response.Data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling battle {BattleId}", battleId);
            }
        }
    }

    /// <summary>
    /// 处理战斗更新（来自SignalR或轮询）
    /// </summary>
    private void HandleBattleUpdate(BattleStateDto battleState)
    {
        _activeBattles[battleState.BattleId] = battleState;
        OnBattleStateChanged?.Invoke(battleState);

        // 如果战斗结束，清理状态
        if (!battleState.IsActive)
        {
            _activeBattles.Remove(battleState.BattleId);
        }
    }

    /// <summary>
    /// 更新连接状态
    /// </summary>
    private void UpdateConnectionStatus(bool isConnected)
    {
        if (_isConnected != isConnected)
        {
            _isConnected = isConnected;
            OnConnectionStatusChanged?.Invoke(isConnected);
            _logger.LogInformation("Connection status changed to: {IsConnected}", isConnected);
        }
    }

    /// <summary>
    /// 资源清理
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _pollingTimer?.Dispose();
        
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        _logger.LogInformation("ClientGameStateService disposed");
    }
}