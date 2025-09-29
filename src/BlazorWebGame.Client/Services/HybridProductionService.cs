using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Models;
using BlazorWebGame.Services;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorWebGame.Client.Services;

/// <summary>
/// 纯服务端生产服务 - 移除混合模式，只支持服务端处理
/// </summary>
public class HybridProductionService : IAsyncDisposable
{
    private readonly ProductionApiService _productionApi;
    private readonly ILogger<HybridProductionService> _logger;
    
    // SignalR 连接用于接收服务端生产事件
    private HubConnection? _hubConnection;
    
    // 移除客户端回退逻辑 - 只使用服务端
    // private readonly ProfessionService _legacyProfessionService; // 已移除
    // private readonly GameStateService _gameState; // 已移除
    // private bool _useServerProduction = true; // 已移除，始终使用服务端
    
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
        ILogger<HybridProductionService> logger)
    {
        _productionApi = productionApi;
        _logger = logger;
        
        // 移除旧服务订阅 - 不再支持客户端回退
        // _legacyProfessionService.OnStateChanged += () => OnStateChanged?.Invoke(); // 已移除
    }

    /// <summary>
    /// 初始化服务，设置 SignalR 连接
    /// </summary>
    public async Task InitializeAsync()
    {
        // 始终使用服务端生产 - 移除条件判断
        await InitializeSignalRConnection();
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
            // 移除客户端回退逻辑 - 纯在线游戏必须有服务端连接
            throw new InvalidOperationException("无法连接到服务端，游戏无法继续", ex);
        }
    }

    /// <summary>
    /// 获取可用的采集节点 - 纯服务端
    /// </summary>
    public async Task<List<GatheringNode>> GetAvailableNodesAsync(GatheringProfession profession)
    {
        try
        {
            var serverNodes = await _productionApi.GetAvailableNodesAsync(profession.ToString());
            return serverNodes.Select(ConvertFromDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get nodes from server");
            // 移除客户端回退 - 纯在线游戏
            throw new InvalidOperationException("无法从服务端获取采集节点", ex);
        }
    }

    /// <summary>
    /// 开始采集 - 纯服务端
    /// </summary>
    public async Task<bool> StartGatheringAsync(string characterId, GatheringNode node)
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
            _logger.LogError(ex, "Error starting server-side gathering");
            // 移除客户端回退 - 纯在线游戏
            throw new InvalidOperationException("无法开始服务端采集", ex);
        }
    }

    /// <summary>
    /// 停止采集 - 纯服务端
    /// </summary>
    public async Task<bool> StopGatheringAsync(string characterId)
    {
        if (_currentGatheringState != null)
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
        
        // 没有活跃的采集状态
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
    /// 检查玩家是否正在采集 - 纯服务端状态
    /// </summary>
    public bool IsGathering()
    {
        // 只检查服务端状态 - 移除客户端回退
        return _currentGatheringState?.IsGathering ?? false;
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