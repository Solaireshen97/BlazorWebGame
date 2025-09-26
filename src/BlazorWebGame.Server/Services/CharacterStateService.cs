using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Events;
using BlazorWebGame.Shared.Models;
using System.Collections.Concurrent;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 统一的角色状态管理服务
/// 负责跟踪所有角色的动作状态、进度和详细信息，支持高效的批量查询和轮询
/// </summary>
public class CharacterStateService : IDisposable
{
    private readonly ILogger<CharacterStateService> _logger;
    private readonly ServerCharacterService _characterService;
    private readonly EventDrivenProfessionService _professionService;
    private readonly GameEngineService _battleService;
    private readonly UnifiedEventService _eventService;

    // 角色状态缓存 - 使用ConcurrentDictionary支持高并发访问
    private readonly ConcurrentDictionary<string, CharacterStateInfo> _characterStates = new();
    
    // 状态更新队列 - 异步处理状态变更
    private readonly ConcurrentQueue<CharacterStateUpdate> _stateUpdateQueue = new();
    
    // 缓存配置
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
    
    // 后台更新任务
    private readonly Timer _updateTimer;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed = false;

    // 性能统计
    private long _totalStateQueries = 0;
    private long _totalStateUpdates = 0;
    private readonly object _statsLock = new();

    public CharacterStateService(
        ILogger<CharacterStateService> logger,
        ServerCharacterService characterService,
        EventDrivenProfessionService professionService,
        GameEngineService battleService,
        UnifiedEventService eventService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _characterService = characterService ?? throw new ArgumentNullException(nameof(characterService));
        _professionService = professionService ?? throw new ArgumentNullException(nameof(professionService));
        _battleService = battleService ?? throw new ArgumentNullException(nameof(battleService));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

        _cancellationTokenSource = new CancellationTokenSource();
        
        // 启动定期状态更新任务
        _updateTimer = new Timer(ProcessStateUpdates, null, _updateInterval, _updateInterval);
        
        // 注册事件处理器
        RegisterEventHandlers();
        
        _logger.LogInformation("CharacterStateService initialized with update interval: {Interval}ms", 
            _updateInterval.TotalMilliseconds);
    }

    /// <summary>
    /// 注册相关事件处理器
    /// </summary>
    private void RegisterEventHandlers()
    {
        try
        {
            // 注册角色动作状态变更事件处理器
            _eventService.RegisterHandler(GameEventTypes.PLAYER_ACTION_STARTED, 
                new ActionStartedHandler(this, _logger));
            _eventService.RegisterHandler(GameEventTypes.PLAYER_ACTION_COMPLETED, 
                new ActionCompletedHandler(this, _logger));
            _eventService.RegisterHandler(GameEventTypes.GATHERING_STARTED, 
                new GatheringStartedHandler(this, _logger));
            _eventService.RegisterHandler(GameEventTypes.CRAFTING_STARTED, 
                new CraftingStartedHandler(this, _logger));
            _eventService.RegisterHandler(GameEventTypes.BATTLE_STARTED, 
                new BattleStartedHandler(this, _logger));
            
            _logger.LogDebug("Character state event handlers registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register character state event handlers");
        }
    }

    /// <summary>
    /// 获取单个角色的状态信息
    /// </summary>
    public async Task<CharacterStateDto?> GetCharacterStateAsync(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return null;

        Interlocked.Increment(ref _totalStateQueries);

        try
        {
            // 先从缓存获取
            if (_characterStates.TryGetValue(characterId, out var cachedState) && 
                !IsStateCacheExpired(cachedState))
            {
                return ConvertToDto(cachedState);
            }

            // 缓存未命中或已过期，从服务获取最新状态
            var characterDetails = await _characterService.GetCharacterDetailsAsync(characterId);
            if (characterDetails == null)
            {
                _logger.LogWarning("Character {CharacterId} not found", characterId);
                return null;
            }

            // 构建状态信息
            var stateInfo = await BuildCharacterStateInfo(characterDetails);
            
            // 更新缓存
            _characterStates.AddOrUpdate(characterId, stateInfo, (key, old) => stateInfo);
            
            return ConvertToDto(stateInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character state for {CharacterId}", characterId);
            return null;
        }
    }

    /// <summary>
    /// 批量获取角色状态信息
    /// </summary>
    public async Task<CharacterStatesResponse> GetCharacterStatesAsync(CharacterStatesRequest request)
    {
        var response = new CharacterStatesResponse();
        
        try
        {
            var tasks = request.CharacterIds.Select(async characterId =>
            {
                var state = await GetCharacterStateAsync(characterId);
                if (state != null && 
                    (request.IncludeOfflineCharacters || state.IsOnline) &&
                    (request.LastUpdateAfter == null || state.LastUpdated > request.LastUpdateAfter))
                {
                    return state;
                }
                return null;
            });

            var results = await Task.WhenAll(tasks);
            response.Characters = results.Where(s => s != null).Cast<CharacterStateDto>().ToList();
            response.TotalCount = response.Characters.Count;
            response.ServerTimestamp = DateTime.UtcNow;
            
            _logger.LogDebug("Retrieved {Count} character states out of {RequestedCount} requested", 
                response.Characters.Count, request.CharacterIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character states for batch request");
        }

        return response;
    }

    /// <summary>
    /// 获取所有活跃角色的状态（用于管理界面）
    /// </summary>
    public async Task<List<CharacterStateDto>> GetAllActiveCharacterStatesAsync()
    {
        try
        {
            var activeStates = _characterStates.Values
                .Where(s => s.IsOnline && !IsStateCacheExpired(s))
                .Select(ConvertToDto)
                .ToList();

            _logger.LogDebug("Retrieved {Count} active character states", activeStates.Count);
            return activeStates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all active character states");
            return new List<CharacterStateDto>();
        }
    }

    /// <summary>
    /// 更新角色动作状态
    /// </summary>
    public void UpdateCharacterActionState(string characterId, string actionType, 
        string actionTarget = "", double duration = 0, Dictionary<string, object>? actionData = null)
    {
        if (string.IsNullOrEmpty(characterId)) return;

        var update = new CharacterStateUpdate
        {
            CharacterId = characterId,
            UpdateType = StateUpdateType.ActionState,
            ActionType = actionType,
            ActionTarget = actionTarget,
            Duration = duration,
            ActionData = actionData ?? new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow
        };

        _stateUpdateQueue.Enqueue(update);
        Interlocked.Increment(ref _totalStateUpdates);
    }

    /// <summary>
    /// 更新角色位置信息
    /// </summary>
    public void UpdateCharacterLocation(string characterId, string zone, string subZone, double x, double y)
    {
        if (string.IsNullOrEmpty(characterId)) return;

        var update = new CharacterStateUpdate
        {
            CharacterId = characterId,
            UpdateType = StateUpdateType.Location,
            Zone = zone,
            SubZone = subZone,
            X = x,
            Y = y,
            Timestamp = DateTime.UtcNow
        };

        _stateUpdateQueue.Enqueue(update);
    }

    /// <summary>
    /// 更新角色生命值状态
    /// </summary>
    public void UpdateCharacterHealth(string characterId, int health, int maxHealth)
    {
        if (string.IsNullOrEmpty(characterId)) return;

        var update = new CharacterStateUpdate
        {
            CharacterId = characterId,
            UpdateType = StateUpdateType.Health,
            Health = health,
            MaxHealth = maxHealth,
            Timestamp = DateTime.UtcNow
        };

        _stateUpdateQueue.Enqueue(update);
    }

    /// <summary>
    /// 设置角色在线状态
    /// </summary>
    public void SetCharacterOnlineStatus(string characterId, bool isOnline)
    {
        if (string.IsNullOrEmpty(characterId)) return;

        var update = new CharacterStateUpdate
        {
            CharacterId = characterId,
            UpdateType = StateUpdateType.OnlineStatus,
            IsOnline = isOnline,
            Timestamp = DateTime.UtcNow
        };

        _stateUpdateQueue.Enqueue(update);
    }

    /// <summary>
    /// 构建角色状态信息
    /// </summary>
    private async Task<CharacterStateInfo> BuildCharacterStateInfo(object characterDetails)
    {
        // 这里需要根据实际的角色详情对象结构来实现
        // 暂时使用简化的实现
        var stateInfo = new CharacterStateInfo
        {
            CharacterId = ExtractCharacterId(characterDetails),
            CharacterName = ExtractCharacterName(characterDetails),
            Level = ExtractCharacterLevel(characterDetails),
            Health = ExtractCharacterHealth(characterDetails),
            MaxHealth = ExtractCharacterMaxHealth(characterDetails),
            LastUpdated = DateTime.UtcNow,
            IsOnline = true // 能获取到详情说明在线
        };

        // 检查战斗状态
        await UpdateBattleState(stateInfo);
        
        // 检查生产状态
        await UpdateProfessionState(stateInfo);

        return stateInfo;
    }

    /// <summary>
    /// 更新战斗状态
    /// </summary>
    private async Task UpdateBattleState(CharacterStateInfo stateInfo)
    {
        try
        {
            // 检查角色是否在战斗中
            var battles = _battleService.GetAllBattleUpdates();
            var currentBattle = battles.FirstOrDefault(b => 
                b.CharacterId == stateInfo.CharacterId || 
                b.PartyMemberIds.Contains(stateInfo.CharacterId));

            if (currentBattle != null && currentBattle.IsActive)
            {
                stateInfo.CurrentAction = new PlayerActionStateInfo
                {
                    ActionType = "Combat",
                    ActionTarget = currentBattle.EnemyId,
                    StartTime = currentBattle.LastUpdated,
                    ActionData = new Dictionary<string, object>
                    {
                        ["BattleId"] = currentBattle.BattleId.ToString(),
                        ["PlayerHealth"] = currentBattle.PlayerHealth,
                        ["EnemyHealth"] = currentBattle.EnemyHealth
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating battle state for character {CharacterId}", stateInfo.CharacterId);
        }
    }

    /// <summary>
    /// 更新生产状态
    /// </summary>
    private async Task UpdateProfessionState(CharacterStateInfo stateInfo)
    {
        try
        {
            // 这里需要从EventDrivenProfessionService获取角色的生产状态
            // 由于该服务的方法可能是内部的，我们需要添加相应的公共方法
            // 暂时使用简化实现
            
            // 如果角色不在战斗中，检查是否在进行生产活动
            if (stateInfo.CurrentAction.ActionType == "Idle")
            {
                // 这里应该调用profession service来获取活跃的采集/制作状态
                // var professionState = await _professionService.GetCharacterActivityStateAsync(stateInfo.CharacterId);
                // 暂时保持空闲状态
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profession state for character {CharacterId}", stateInfo.CharacterId);
        }
    }

    /// <summary>
    /// 计算战斗进度
    /// </summary>
    private double CalculateBattleProgress(BattleStateDto battle)
    {
        // 简化的进度计算：基于敌人血量损失
        if (battle.EnemyMaxHealth <= 0) return 1.0;
        
        var healthLost = battle.EnemyMaxHealth - battle.EnemyHealth;
        return Math.Max(0.0, Math.Min(1.0, (double)healthLost / battle.EnemyMaxHealth));
    }

    /// <summary>
    /// 处理状态更新队列
    /// </summary>
    private void ProcessStateUpdates(object? state)
    {
        if (_disposed) return;

        try
        {
            var processedCount = 0;
            var maxProcessCount = 100; // 每次最多处理100个更新

            while (_stateUpdateQueue.TryDequeue(out var update) && processedCount < maxProcessCount)
            {
                ApplyStateUpdate(update);
                processedCount++;
            }

            if (processedCount > 0)
            {
                _logger.LogDebug("Processed {Count} character state updates", processedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing character state updates");
        }
    }

    /// <summary>
    /// 应用状态更新
    /// </summary>
    private void ApplyStateUpdate(CharacterStateUpdate update)
    {
        try
        {
            var stateInfo = _characterStates.GetOrAdd(update.CharacterId, _ => new CharacterStateInfo
            {
                CharacterId = update.CharacterId,
                CharacterName = update.CharacterId, // 临时使用ID作为名称
                LastUpdated = DateTime.UtcNow
            });

            switch (update.UpdateType)
            {
                case StateUpdateType.ActionState:
                    stateInfo.CurrentAction = new PlayerActionStateInfo
                    {
                        ActionType = update.ActionType,
                        ActionTarget = update.ActionTarget,
                        Duration = update.Duration,
                        StartTime = update.Timestamp,
                        ActionData = update.ActionData
                    };
                    break;

                case StateUpdateType.Health:
                    stateInfo.Health = update.Health;
                    stateInfo.MaxHealth = update.MaxHealth;
                    break;

                case StateUpdateType.Location:
                    stateInfo.CurrentLocation = new LocationInfo
                    {
                        Zone = update.Zone,
                        SubZone = update.SubZone,
                        X = update.X,
                        Y = update.Y
                    };
                    break;

                case StateUpdateType.OnlineStatus:
                    stateInfo.IsOnline = update.IsOnline;
                    break;
            }

            stateInfo.LastUpdated = update.Timestamp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying state update for character {CharacterId}", update.CharacterId);
        }
    }

    /// <summary>
    /// 检查状态缓存是否过期
    /// </summary>
    private bool IsStateCacheExpired(CharacterStateInfo stateInfo)
    {
        return DateTime.UtcNow - stateInfo.LastUpdated > _cacheExpiry;
    }

    /// <summary>
    /// 转换为DTO对象
    /// </summary>
    private CharacterStateDto ConvertToDto(CharacterStateInfo stateInfo)
    {
        var actionDto = new PlayerActionStateDto
        {
            ActionType = stateInfo.CurrentAction.ActionType,
            ActionTarget = stateInfo.CurrentAction.ActionTarget,
            Progress = CalculateActionProgress(stateInfo.CurrentAction),
            Duration = stateInfo.CurrentAction.Duration,
            TimeRemaining = CalculateTimeRemaining(stateInfo.CurrentAction),
            StartTime = stateInfo.CurrentAction.StartTime,
            ActionData = stateInfo.CurrentAction.ActionData
        };

        return new CharacterStateDto
        {
            CharacterId = stateInfo.CharacterId,
            CharacterName = stateInfo.CharacterName,
            CurrentAction = actionDto,
            Level = stateInfo.Level,
            Health = stateInfo.Health,
            MaxHealth = stateInfo.MaxHealth,
            Mana = stateInfo.Mana,
            MaxMana = stateInfo.MaxMana,
            LastUpdated = stateInfo.LastUpdated,
            IsOnline = stateInfo.IsOnline,
            CurrentLocation = ConvertLocationToDto(stateInfo.CurrentLocation),
            Equipment = new EquipmentSummaryDto(), // 简化实现
            ActiveBuffs = new List<ActiveBuffDto>() // 简化实现
        };
    }

    /// <summary>
    /// 计算动作进度
    /// </summary>
    private double CalculateActionProgress(PlayerActionStateInfo actionInfo)
    {
        if (actionInfo.Duration <= 0) return 0.0;
        
        var elapsed = (DateTime.UtcNow - actionInfo.StartTime).TotalSeconds;
        return Math.Max(0.0, Math.Min(1.0, elapsed / actionInfo.Duration));
    }

    /// <summary>
    /// 计算剩余时间
    /// </summary>
    private double CalculateTimeRemaining(PlayerActionStateInfo actionInfo)
    {
        if (actionInfo.Duration <= 0) return 0.0;
        
        var elapsed = (DateTime.UtcNow - actionInfo.StartTime).TotalSeconds;
        return Math.Max(0.0, actionInfo.Duration - elapsed);
    }

    /// <summary>
    /// 转换位置信息为DTO
    /// </summary>
    private LocationDto? ConvertLocationToDto(LocationInfo? locationInfo)
    {
        if (locationInfo == null) return null;
        
        return new LocationDto
        {
            Zone = locationInfo.Zone,
            SubZone = locationInfo.SubZone,
            X = locationInfo.X,
            Y = locationInfo.Y
        };
    }

    /// <summary>
    /// 从角色详情中提取字符ID（简化实现）
    /// </summary>
    private string ExtractCharacterId(object characterDetails)
    {
        // 这里需要根据实际的角色详情对象来实现
        // 暂时返回一个随机ID
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// 从角色详情中提取角色名称（简化实现）
    /// </summary>
    private string ExtractCharacterName(object characterDetails)
    {
        return "Hero"; // 简化实现
    }

    /// <summary>
    /// 从角色详情中提取角色等级（简化实现）
    /// </summary>
    private int ExtractCharacterLevel(object characterDetails)
    {
        return 1; // 简化实现
    }

    /// <summary>
    /// 从角色详情中提取角色生命值（简化实现）
    /// </summary>
    private int ExtractCharacterHealth(object characterDetails)
    {
        return 100; // 简化实现
    }

    /// <summary>
    /// 从角色详情中提取角色最大生命值（简化实现）
    /// </summary>
    private int ExtractCharacterMaxHealth(object characterDetails)
    {
        return 100; // 简化实现
    }

    /// <summary>
    /// 获取性能统计信息
    /// </summary>
    public CharacterStateServiceStats GetStats()
    {
        lock (_statsLock)
        {
            return new CharacterStateServiceStats
            {
                TotalStateQueries = _totalStateQueries,
                TotalStateUpdates = _totalStateUpdates,
                CachedCharacterCount = _characterStates.Count,
                QueuedUpdateCount = _stateUpdateQueue.Count
            };
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _updateTimer?.Dispose();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        
        _logger.LogInformation("CharacterStateService disposed");
    }
}

#region Supporting Classes and Enums

/// <summary>
/// 角色状态信息（内部使用）
/// </summary>
internal class CharacterStateInfo
{
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public PlayerActionStateInfo CurrentAction { get; set; } = new();
    public int Level { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsOnline { get; set; }
    public LocationInfo? CurrentLocation { get; set; }
}

/// <summary>
/// 玩家动作状态信息（内部使用）
/// </summary>
internal class PlayerActionStateInfo
{
    public string ActionType { get; set; } = "Idle";
    public string ActionTarget { get; set; } = string.Empty;
    public double Duration { get; set; } = 0.0;
    public DateTime StartTime { get; set; }
    public Dictionary<string, object> ActionData { get; set; } = new();
}

/// <summary>
/// 位置信息（内部使用）
/// </summary>
internal class LocationInfo
{
    public string Zone { get; set; } = string.Empty;
    public string SubZone { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// 状态更新信息
/// </summary>
internal class CharacterStateUpdate
{
    public string CharacterId { get; set; } = string.Empty;
    public StateUpdateType UpdateType { get; set; }
    public DateTime Timestamp { get; set; }
    
    // Action state fields
    public string ActionType { get; set; } = string.Empty;
    public string ActionTarget { get; set; } = string.Empty;
    public double Duration { get; set; }
    public Dictionary<string, object> ActionData { get; set; } = new();
    
    // Health fields
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    
    // Location fields
    public string Zone { get; set; } = string.Empty;
    public string SubZone { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    
    // Online status
    public bool IsOnline { get; set; }
}

/// <summary>
/// 状态更新类型
/// </summary>
internal enum StateUpdateType
{
    ActionState,
    Health,
    Location,
    OnlineStatus
}

/// <summary>
/// 角色状态服务统计信息
/// </summary>
public class CharacterStateServiceStats
{
    public long TotalStateQueries { get; set; }
    public long TotalStateUpdates { get; set; }
    public int CachedCharacterCount { get; set; }
    public int QueuedUpdateCount { get; set; }
}

#endregion

#region Event Handlers

/// <summary>
/// 动作开始事件处理器
/// </summary>
internal class ActionStartedHandler : IUnifiedEventHandler
{
    private readonly CharacterStateService _stateService;
    private readonly ILogger _logger;

    public ActionStartedHandler(CharacterStateService stateService, ILogger logger)
    {
        _stateService = stateService;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent gameEvent)
    {
        try
        {
            // 从事件中提取角色ID和动作信息
            // 这里需要根据实际的事件数据结构来实现
            var characterId = ExtractCharacterIdFromEvent(gameEvent);
            var actionType = ExtractActionTypeFromEvent(gameEvent);
            var actionTarget = ExtractActionTargetFromEvent(gameEvent);
            var duration = ExtractDurationFromEvent(gameEvent);

            if (!string.IsNullOrEmpty(characterId))
            {
                _stateService.UpdateCharacterActionState(characterId, actionType, actionTarget, duration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling action started event");
        }
    }

    private string ExtractCharacterIdFromEvent(UnifiedEvent gameEvent)
    {
        // 实现从事件中提取角色ID的逻辑
        return gameEvent.ActorId.ToString();
    }

    private string ExtractActionTypeFromEvent(UnifiedEvent gameEvent)
    {
        // 根据事件类型返回相应的动作类型
        return gameEvent.EventType switch
        {
            GameEventTypes.GATHERING_STARTED => "Gathering",
            GameEventTypes.CRAFTING_STARTED => "Crafting",
            GameEventTypes.BATTLE_STARTED => "Combat",
            _ => "Unknown"
        };
    }

    private string ExtractActionTargetFromEvent(UnifiedEvent gameEvent)
    {
        return gameEvent.TargetId.ToString();
    }

    private double ExtractDurationFromEvent(UnifiedEvent gameEvent)
    {
        // 从事件数据中提取持续时间
        return 10.0; // 简化实现
    }
}

/// <summary>
/// 动作完成事件处理器
/// </summary>
internal class ActionCompletedHandler : IUnifiedEventHandler
{
    private readonly CharacterStateService _stateService;
    private readonly ILogger _logger;

    public ActionCompletedHandler(CharacterStateService stateService, ILogger logger)
    {
        _stateService = stateService;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent gameEvent)
    {
        try
        {
            var characterId = gameEvent.ActorId.ToString();
            _stateService.UpdateCharacterActionState(characterId, "Idle");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling action completed event");
        }
    }
}

/// <summary>
/// 采集开始事件处理器
/// </summary>
internal class GatheringStartedHandler : ActionStartedHandler
{
    public GatheringStartedHandler(CharacterStateService stateService, ILogger logger) 
        : base(stateService, logger) { }
}

/// <summary>
/// 制作开始事件处理器
/// </summary>
internal class CraftingStartedHandler : ActionStartedHandler
{
    public CraftingStartedHandler(CharacterStateService stateService, ILogger logger) 
        : base(stateService, logger) { }
}

/// <summary>
/// 战斗开始事件处理器
/// </summary>
internal class BattleStartedHandler : ActionStartedHandler
{
    public BattleStartedHandler(CharacterStateService stateService, ILogger logger) 
        : base(stateService, logger) { }
}

#endregion