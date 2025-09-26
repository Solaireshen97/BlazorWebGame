using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Services;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 新的客户端游戏状态服务，使用服务器通信替代本地计时器
/// 增强版本：支持角色状态轮询和战斗状态同步
/// </summary>
public class ClientGameStateService : IAsyncDisposable
{
    private readonly GameApiService _gameApi;
    private readonly GameStateService _gameState; // 获取当前角色信息
    private readonly ServerConfigurationService _serverConfig;
    private readonly CharacterStateApiService _characterStateApi;
    private readonly ILogger<ClientGameStateService> _logger;
    private HubConnection? _hubConnection;
    private System.Threading.Timer? _pollingTimer;
    private System.Threading.Timer? _characterStatePollingTimer;
    
    // 战斗状态
    private readonly Dictionary<Guid, BattleStateDto> _activeBattles = new();
    
    // 角色状态缓存
    private readonly Dictionary<string, CharacterStateDto> _characterStates = new();
    private readonly List<string> _trackedCharacterIds = new();
    private DateTime _lastCharacterStateUpdate = DateTime.MinValue;
    
    // 事件
    public event Action<BattleStateDto>? OnBattleStateChanged;
    public event Action<bool>? OnConnectionStatusChanged;
    public event Action<List<CharacterStateDto>>? OnCharacterStatesUpdated;
    public event Action<CharacterStateDto>? OnCharacterStateChanged;

    // 配置
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMilliseconds(1000); // 1秒轮询一次
    private readonly TimeSpan _characterStatePollingInterval = TimeSpan.FromMilliseconds(2000); // 2秒轮询角色状态
    private bool _isConnected = false;
    private bool _isDisposed = false;
    private bool _characterStatePollingEnabled = true;

    public bool IsConnected => _isConnected;
    public IReadOnlyDictionary<Guid, BattleStateDto> ActiveBattles => _activeBattles;
    public IReadOnlyDictionary<string, CharacterStateDto> CharacterStates => _characterStates;
    public bool CharacterStatePollingEnabled 
    { 
        get => _characterStatePollingEnabled; 
        set => _characterStatePollingEnabled = value; 
    }

    public ClientGameStateService(
        GameApiService gameApi, 
        GameStateService gameState, 
        ServerConfigurationService serverConfig,
        CharacterStateApiService characterStateApi,
        ILogger<ClientGameStateService> logger)
    {
        _gameApi = gameApi;
        _gameState = gameState;
        _serverConfig = serverConfig;
        _characterStateApi = characterStateApi;
        _logger = logger;

        // 初始化跟踪的角色列表（获取当前用户的所有角色）
        InitializeTrackedCharacters();
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
            
            // 开始角色状态轮询
            StartCharacterStatePolling();
            
            _logger.LogInformation("ClientGameStateService initialized successfully with character state polling");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ClientGameStateService");
            // 即使SignalR失败，也要启动轮询机制
            StartPolling();
            StartCharacterStatePolling();
        }
    }

    /// <summary>
    /// 建立SignalR连接
    /// </summary>
    private async Task InitializeSignalRConnection()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_serverConfig.CurrentServerUrl}/gamehub")
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
    /// 开始角色状态轮询
    /// </summary>
    private void StartCharacterStatePolling()
    {
        if (!_characterStatePollingEnabled) return;

        _characterStatePollingTimer = new System.Threading.Timer(async _ =>
        {
            if (_isDisposed || !_characterStatePollingEnabled) return;

            try
            {
                await PollCharacterStates();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during character state polling");
            }
        }, null, TimeSpan.Zero, _characterStatePollingInterval);

        _logger.LogDebug("Character state polling started with {Count} tracked characters", 
            _trackedCharacterIds.Count);
    }

    /// <summary>
    /// 轮询角色状态
    /// </summary>
    private async Task PollCharacterStates()
    {
        if (!_trackedCharacterIds.Any()) return;

        try
        {
            var request = new CharacterStatesRequest
            {
                CharacterIds = _trackedCharacterIds,
                IncludeOfflineCharacters = false,
                LastUpdateAfter = _lastCharacterStateUpdate
            };

            var response = await _characterStateApi.GetCharacterStatesAsync(request);
            
            if (response.Success && response.Data != null)
            {
                var updatedStates = new List<CharacterStateDto>();
                
                foreach (var characterState in response.Data.Characters)
                {
                    var previousState = _characterStates.GetValueOrDefault(characterState.CharacterId);
                    _characterStates[characterState.CharacterId] = characterState;
                    
                    // 检查状态是否有显著变化
                    if (HasSignificantStateChange(previousState, characterState))
                    {
                        OnCharacterStateChanged?.Invoke(characterState);
                    }
                    
                    updatedStates.Add(characterState);
                }

                if (updatedStates.Any())
                {
                    _lastCharacterStateUpdate = response.Data.ServerTimestamp;
                    OnCharacterStatesUpdated?.Invoke(updatedStates);
                    
                    _logger.LogDebug("Updated {Count} character states", updatedStates.Count);
                }
            }
            else if (!response.Success)
            {
                _logger.LogWarning("Failed to poll character states: {Message}", response.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling character states");
        }
    }

    /// <summary>
    /// 检查角色状态是否有显著变化
    /// </summary>
    private bool HasSignificantStateChange(CharacterStateDto? previous, CharacterStateDto current)
    {
        if (previous == null) return true;

        // 检查动作状态变化
        if (previous.CurrentAction.ActionType != current.CurrentAction.ActionType ||
            previous.CurrentAction.ActionTarget != current.CurrentAction.ActionTarget)
        {
            return true;
        }

        // 检查生命值变化（超过5%）
        if (previous.MaxHealth > 0)
        {
            var healthChangePercent = Math.Abs((double)(current.Health - previous.Health) / previous.MaxHealth);
            if (healthChangePercent >= 0.05) return true;
        }

        // 检查动作进度变化（超过10%）
        if (Math.Abs(current.CurrentAction.Progress - previous.CurrentAction.Progress) >= 0.1)
        {
            return true;
        }

        // 检查在线状态变化
        if (previous.IsOnline != current.IsOnline) return true;

        return false;
    }

    /// <summary>
    /// 初始化跟踪的角色列表
    /// </summary>
    private void InitializeTrackedCharacters()
    {
        try
        {
            // 获取当前用户的所有角色ID
            // 这里需要根据实际的用户系统来实现
            // 暂时使用当前激活角色
            if (_gameState.ActiveCharacter != null)
            {
                _trackedCharacterIds.Add(_gameState.ActiveCharacter.Id);
                _logger.LogDebug("Added active character {CharacterId} to tracking list", 
                    _gameState.ActiveCharacter.Id);
            }

            // TODO: 添加用户的其他角色
            // var userCharacters = await _userService.GetUserCharactersAsync();
            // _trackedCharacterIds.AddRange(userCharacters.Select(c => c.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing tracked characters");
        }
    }

    /// <summary>
    /// 添加要跟踪的角色
    /// </summary>
    public void AddTrackedCharacter(string characterId)
    {
        if (string.IsNullOrEmpty(characterId) || _trackedCharacterIds.Contains(characterId))
            return;

        _trackedCharacterIds.Add(characterId);
        _logger.LogDebug("Added character {CharacterId} to tracking list", characterId);
    }

    /// <summary>
    /// 移除跟踪的角色
    /// </summary>
    public void RemoveTrackedCharacter(string characterId)
    {
        if (_trackedCharacterIds.Remove(characterId))
        {
            _characterStates.Remove(characterId);
            _logger.LogDebug("Removed character {CharacterId} from tracking list", characterId);
        }
    }

    /// <summary>
    /// 获取指定角色的最新状态
    /// </summary>
    public CharacterStateDto? GetCharacterState(string characterId)
    {
        return _characterStates.GetValueOrDefault(characterId);
    }

    /// <summary>
    /// 手动刷新角色状态
    /// </summary>
    public async Task RefreshCharacterStatesAsync()
    {
        try
        {
            await PollCharacterStates();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing character states");
        }
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
        _characterStatePollingTimer?.Dispose();
        
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        _logger.LogInformation("ClientGameStateService disposed");
    }
}