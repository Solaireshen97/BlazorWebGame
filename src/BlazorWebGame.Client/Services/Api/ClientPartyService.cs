using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 客户端组队服务，通过服务端 API 管理组队功能
/// </summary>
public class ClientPartyService : IAsyncDisposable
{
    private readonly GameApiService _gameApi;
    private readonly ILogger<ClientPartyService> _logger;
    private HubConnection? _hubConnection;
    private PartyDto? _currentParty;

    public event Action<PartyDto?>? OnPartyChanged;
    public event Action<string>? OnPartyMessage;

    public PartyDto? CurrentParty => _currentParty;

    public ClientPartyService(GameApiService gameApi, ILogger<ClientPartyService> logger)
    {
        _gameApi = gameApi;
        _logger = logger;
    }

    /// <summary>
    /// 初始化服务和 SignalR 连接
    /// </summary>
    public async Task InitializeAsync(string characterId)
    {
        await InitializeSignalRConnection();
        
        // 获取角色当前的组队状态
        await RefreshPartyStatusAsync(characterId);

        if (_hubConnection != null && _currentParty != null)
        {
            await _hubConnection.InvokeAsync("JoinCharacterUpdates", characterId);
        }
    }

    /// <summary>
    /// 初始化 SignalR 连接
    /// </summary>
    private async Task InitializeSignalRConnection()
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_gameApi.BaseUrl}/gamehub")
                .WithAutomaticReconnect()
                .Build();

            // 注册事件处理器
            _hubConnection.On<object>("PartyUpdate", OnServerPartyUpdate);

            await _hubConnection.StartAsync();
            _logger.LogInformation("SignalR connection established for party service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish SignalR connection for party service");
        }
    }

    /// <summary>
    /// 处理服务端组队更新
    /// </summary>
    private void OnServerPartyUpdate(object updateData)
    {
        try
        {
            _logger.LogDebug("Received party update from server");
            
            // 简单处理 - 实际应该解析具体的更新类型
            if (updateData is PartyDto party)
            {
                _currentParty = party;
                OnPartyChanged?.Invoke(_currentParty);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing party update from server");
        }
    }

    /// <summary>
    /// 创建组队（仅队长）
    /// </summary>
    public async Task<bool> CreatePartyAsync(string characterId)
    {
        try
        {
            var response = await _gameApi.CreatePartyAsync(characterId);
            
            if (response.Success && response.Data != null)
            {
                _currentParty = response.Data;
                OnPartyChanged?.Invoke(_currentParty);
                OnPartyMessage?.Invoke("组队创建成功！");
                
                // 加入组队的 SignalR 组
                if (_hubConnection != null)
                {
                    await _hubConnection.InvokeAsync("JoinCharacterUpdates", characterId);
                }
                
                return true;
            }
            else
            {
                OnPartyMessage?.Invoke(response.Message ?? "创建组队失败");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating party for character {CharacterId}", characterId);
            OnPartyMessage?.Invoke("创建组队时发生网络错误");
            return false;
        }
    }

    /// <summary>
    /// 加入组队
    /// </summary>
    public async Task<bool> JoinPartyAsync(string characterId, Guid partyId)
    {
        try
        {
            var response = await _gameApi.JoinPartyAsync(characterId, partyId);
            
            if (response.Success)
            {
                OnPartyMessage?.Invoke("成功加入组队！");
                await RefreshPartyStatusAsync(characterId);
                
                // 加入角色更新的 SignalR 组
                if (_hubConnection != null)
                {
                    await _hubConnection.InvokeAsync("JoinCharacterUpdates", characterId);
                }
                
                return true;
            }
            else
            {
                OnPartyMessage?.Invoke(response.Message ?? "加入组队失败");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining party for character {CharacterId}", characterId);
            OnPartyMessage?.Invoke("加入组队时发生网络错误");
            return false;
        }
    }

    /// <summary>
    /// 离开组队
    /// </summary>
    public async Task<bool> LeavePartyAsync(string characterId)
    {
        try
        {
            var response = await _gameApi.LeavePartyAsync(characterId);
            
            if (response.Success)
            {
                var wasInParty = _currentParty != null;
                _currentParty = null;
                OnPartyChanged?.Invoke(null);
                
                if (wasInParty)
                {
                    OnPartyMessage?.Invoke("已离开组队");
                }
                
                // 离开角色更新的 SignalR 组
                if (_hubConnection != null)
                {
                    await _hubConnection.InvokeAsync("LeaveCharacterUpdates", characterId);
                }
                
                return true;
            }
            else
            {
                OnPartyMessage?.Invoke(response.Message ?? "离开组队失败");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving party for character {CharacterId}", characterId);
            OnPartyMessage?.Invoke("离开组队时发生网络错误");
            return false;
        }
    }

    /// <summary>
    /// 刷新组队状态
    /// </summary>
    public async Task RefreshPartyStatusAsync(string characterId)
    {
        try
        {
            var response = await _gameApi.GetPartyForCharacterAsync(characterId);
            
            if (response.Success)
            {
                var previousParty = _currentParty;
                _currentParty = response.Data;
                
                // 只有在组队状态真正改变时才触发事件
                if ((previousParty == null && _currentParty != null) ||
                    (previousParty != null && _currentParty == null) ||
                    (previousParty?.Id != _currentParty?.Id))
                {
                    OnPartyChanged?.Invoke(_currentParty);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing party status for character {CharacterId}", characterId);
        }
    }

    /// <summary>
    /// 获取所有可用的组队
    /// </summary>
    public async Task<List<PartyDto>> GetAvailablePartiesAsync()
    {
        try
        {
            var response = await _gameApi.GetAllPartiesAsync();
            return response.Success ? (response.Data ?? new List<PartyDto>()) : new List<PartyDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available parties");
            return new List<PartyDto>();
        }
    }

    /// <summary>
    /// 检查角色是否是队长
    /// </summary>
    public bool IsLeader(string characterId)
    {
        return _currentParty?.CaptainId == characterId;
    }

    /// <summary>
    /// 检查角色是否在组队中
    /// </summary>
    public bool IsInParty(string characterId)
    {
        return _currentParty?.MemberIds.Contains(characterId) == true;
    }

    /// <summary>
    /// 获取组队成员数量
    /// </summary>
    public int GetMemberCount()
    {
        return _currentParty?.MemberIds.Count ?? 0;
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}