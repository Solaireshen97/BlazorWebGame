using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Models;
using BlazorWebGame.Services;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorWebGame.Client.Services;

/// <summary>
/// 混合生产服务 - 支持逐步从客户端迁移到服务端
/// </summary>
public class HybridProductionService : IAsyncDisposable
{
    private readonly ProductionApiService _productionApi;
    private readonly ProfessionService _legacyProfessionService;
    private readonly GameStateService _gameState;
    private readonly ILogger<HybridProductionService> _logger;
    
    // SignalR 连接用于接收服务端生产事件
    private HubConnection? _hubConnection;
    
    // 配置标志 - 是否使用服务端生产系统
    private bool _useServerProduction = true; // 默认使用服务端
    
    // 当前采集状态
    private GatheringStateDto? _currentGatheringState;
    
    /// <summary>
    /// 状态变更事件
    /// </summary>
    public event Action? OnStateChanged;
    
    /// <summary>
    /// 采集完成事件
    /// </summary>
    public event Action<GatheringResultDto>? OnGatheringCompleted;

    public HybridProductionService(
        ProductionApiService productionApi,
        ProfessionService legacyProfessionService,
        GameStateService gameState,
        ILogger<HybridProductionService> logger)
    {
        _productionApi = productionApi;
        _legacyProfessionService = legacyProfessionService;
        _gameState = gameState;
        _logger = logger;
        
        // 订阅旧服务的状态变更事件
        _legacyProfessionService.OnStateChanged += () => OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 初始化服务，设置 SignalR 连接
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_useServerProduction)
        {
            await InitializeSignalRConnection();
        }
    }

    /// <summary>
    /// 初始化 SignalR 连接以接收服务端生产事件
    /// </summary>
    private async Task InitializeSignalRConnection()
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7000/gamehub")
                .WithAutomaticReconnect()
                .Build();

            // 监听采集开始事件
            _hubConnection.On<GatheringStateDto>("GatheringStarted", HandleGatheringStarted);
            
            // 监听采集进度更新事件
            _hubConnection.On<GatheringStateDto>("GatheringProgress", HandleGatheringProgress);
            
            // 监听采集完成事件
            _hubConnection.On<GatheringResultDto>("GatheringCompleted", HandleGatheringCompleted);
            
            // 监听采集停止事件
            _hubConnection.On("GatheringStopped", HandleGatheringStopped);

            await _hubConnection.StartAsync();
            _logger.LogInformation("Production SignalR connection established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize production SignalR connection");
            // 如果 SignalR 连接失败，可以回退到客户端模式
            _useServerProduction = false;
        }
    }

    /// <summary>
    /// 获取可用的采集节点
    /// </summary>
    public async Task<List<GatheringNode>> GetAvailableNodesAsync(BlazorWebGame.Shared.DTOs.GatheringProfession profession)
    {
        if (_useServerProduction)
        {
            try
            {
                var serverNodes = await _productionApi.GetAvailableNodesAsync(profession.ToString());
                return serverNodes.Select(ConvertFromDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get nodes from server, falling back to client");
                _useServerProduction = false;
            }
        }
        
        // 回退到客户端逻辑
        return GatheringData.GetNodesByProfession(profession);
    }

    /// <summary>
    /// 开始采集
    /// </summary>
    public async Task<bool> StartGatheringAsync(string characterId, GatheringNode node)
    {
        if (_useServerProduction)
        {
            try
            {
                var (success, message, state) = await _productionApi.StartGatheringAsync(characterId, node.Id);
                
                if (success && state != null)
                {
                    _currentGatheringState = state;
                    _logger.LogInformation("Started server-side gathering: {Message}", message);
                    OnStateChanged?.Invoke();
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to start server-side gathering: {Message}", message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting server-side gathering, falling back to client");
                _useServerProduction = false;
            }
        }
        
        // 回退到客户端逻辑
        var character = _gameState.ActiveCharacter;
        if (character != null)
        {
            _legacyProfessionService.StartGathering(character, node);
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 停止采集
    /// </summary>
    public async Task<bool> StopGatheringAsync(string characterId)
    {
        if (_useServerProduction && _currentGatheringState != null)
        {
            try
            {
                var (success, message) = await _productionApi.StopGatheringAsync(characterId);
                
                if (success)
                {
                    _currentGatheringState = null;
                    _logger.LogInformation("Stopped server-side gathering: {Message}", message);
                    OnStateChanged?.Invoke();
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to stop server-side gathering: {Message}", message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping server-side gathering");
                return false;
            }
        }
        
        // 回退到客户端逻辑
        var character = _gameState.ActiveCharacter;
        if (character != null)
        {
            _legacyProfessionService.StopCurrentAction(character);
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 获取当前采集状态
    /// </summary>
    public GatheringStateDto? GetCurrentGatheringState()
    {
        return _currentGatheringState;
    }

    /// <summary>
    /// 检查玩家是否正在采集
    /// </summary>
    public bool IsGathering()
    {
        if (_useServerProduction)
        {
            return _currentGatheringState?.IsGathering ?? false;
        }
        
        // 检查客户端状态
        var character = _gameState.ActiveCharacter;
        return character?.CurrentGatheringNode != null;
    }

    // SignalR 事件处理器

    private void HandleGatheringStarted(GatheringStateDto state)
    {
        _currentGatheringState = state;
        _logger.LogInformation("Gathering started: {NodeId}", state.CurrentNodeId);
        OnStateChanged?.Invoke();
    }

    private void HandleGatheringProgress(GatheringStateDto state)
    {
        _currentGatheringState = state;
        OnStateChanged?.Invoke();
    }

    private void HandleGatheringCompleted(GatheringResultDto result)
    {
        _currentGatheringState = null;
        _logger.LogInformation("Gathering completed: {ItemId} x{Quantity}", result.ItemId, result.Quantity);
        
        // 触发采集完成事件
        OnGatheringCompleted?.Invoke(result);
        OnStateChanged?.Invoke();
    }

    private void HandleGatheringStopped()
    {
        _currentGatheringState = null;
        _logger.LogInformation("Gathering stopped by server");
        OnStateChanged?.Invoke();
    }

    // 工具方法

    /// <summary>
    /// 将服务端 DTO 转换为客户端模型
    /// </summary>
    private GatheringNode ConvertFromDto(GatheringNodeDto dto)
    {
        return new GatheringNode
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            GatheringTimeSeconds = dto.GatheringTimeSeconds,
            ResultingItemId = dto.ResultingItemId,
            ResultingItemQuantity = dto.ResultingItemQuantity,
            XpReward = dto.XpReward,
            RequiredProfession = Enum.Parse<GatheringProfession>(dto.RequiredProfession),
            RequiredLevel = dto.RequiredLevel,
            RequiredMonsterId = dto.RequiredMonsterId
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