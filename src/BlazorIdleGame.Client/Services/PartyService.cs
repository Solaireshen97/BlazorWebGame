using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using BlazorIdleGame.Client.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace BlazorIdleGame.Client.Services
{
    public interface IPartyService
    {
        PartyInfo? CurrentParty { get; }
        List<PartyInfo> AvailableParties { get; }
        List<PartyInvite> PendingInvites { get; }
        
        event EventHandler<PartyInfo>? PartyUpdated;
        event EventHandler<PartyInvite>? InviteReceived;
        event EventHandler<string>? MemberJoined;
        event EventHandler<string>? MemberLeft;
        
        Task InitializeAsync();
        Task<bool> CreatePartyAsync(string name, int maxMembers = 5);
        Task<bool> JoinPartyAsync(string partyId);
        Task<bool> LeavePartyAsync();
        Task<bool> InvitePlayerAsync(string playerId);
        Task<bool> KickMemberAsync(string memberId);
        Task<bool> PromoteToLeaderAsync(string memberId);
        Task<bool> SetReadyStatusAsync(bool isReady);
        Task<List<PartyInfo>> GetAvailablePartiesAsync();
        void UpdatePartyState(PartyInfo party);
        void Dispose();
    }
    
    public class PartyService : IPartyService, IDisposable
    {
        private readonly HttpClient _http;
        private readonly ILogger<PartyService> _logger;
        private HubConnection? _hubConnection;
        
        private PartyInfo? _currentParty;
        private List<PartyInfo> _availableParties = new();
        private List<PartyInvite> _pendingInvites = new();
        
        public PartyInfo? CurrentParty => _currentParty;
        public List<PartyInfo> AvailableParties => _availableParties;
        public List<PartyInvite> PendingInvites => _pendingInvites;
        
        public event EventHandler<PartyInfo>? PartyUpdated;
        public event EventHandler<PartyInvite>? InviteReceived;
        public event EventHandler<string>? MemberJoined;
        public event EventHandler<string>? MemberLeft;
        
        public PartyService(HttpClient http, ILogger<PartyService> logger)
        {
            _http = http;
            _logger = logger;
        }
        
        public async Task InitializeAsync()
        {
            try
            {
                // 尝试建立SignalR连接（用于实时组队通知）
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{_http.BaseAddress}partyHub")
                    .WithAutomaticReconnect()
                    .Build();
                
                // 注册SignalR事件处理
                _hubConnection.On<PartyInfo>("PartyUpdated", party =>
                {
                    _currentParty = party;
                    PartyUpdated?.Invoke(this, party);
                });
                
                _hubConnection.On<PartyInvite>("InviteReceived", invite =>
                {
                    _pendingInvites.Add(invite);
                    InviteReceived?.Invoke(this, invite);
                });
                
                _hubConnection.On<string, string>("MemberJoined", (partyId, memberName) =>
                {
                    if (_currentParty?.Id == partyId)
                    {
                        MemberJoined?.Invoke(this, memberName);
                    }
                });
                
                _hubConnection.On<string, string>("MemberLeft", (partyId, memberName) =>
                {
                    if (_currentParty?.Id == partyId)
                    {
                        MemberLeft?.Invoke(this, memberName);
                    }
                });
                
                await _hubConnection.StartAsync();
                _logger.LogInformation("SignalR连接成功");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR连接失败，使用HTTP轮询模式");
                // SignalR失败不影响游戏，降级到纯HTTP模式
            }
        }
        
        public async Task<bool> CreatePartyAsync(string name, int maxMembers = 5)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/party/create", new
                {
                    Name = name,
                    MaxMembers = maxMembers
                });
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<PartyInfo>>();
                    if (result?.Success == true && result.Data != null)
                    {
                        _currentParty = result.Data;
                        PartyUpdated?.Invoke(this, _currentParty);
                        
                        // 加入SignalR组
                        if (_hubConnection?.State == HubConnectionState.Connected)
                        {
                            await _hubConnection.InvokeAsync("JoinPartyGroup", _currentParty.Id);
                        }
                        
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建队伍失败");
                return false;
            }
        }
        
        public async Task<bool> JoinPartyAsync(string partyId)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/party/join", new
                {
                    PartyId = partyId
                });
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<PartyInfo>>();
                    if (result?.Success == true && result.Data != null)
                    {
                        _currentParty = result.Data;
                        PartyUpdated?.Invoke(this, _currentParty);
                        
                        // 加入SignalR组
                        if (_hubConnection?.State == HubConnectionState.Connected)
                        {
                            await _hubConnection.InvokeAsync("JoinPartyGroup", partyId);
                        }
                        
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加入队伍失败");
                return false;
            }
        }
        
        public async Task<bool> LeavePartyAsync()
        {
            if (_currentParty == null) return false;
            
            try
            {
                var response = await _http.PostAsync($"api/party/leave/{_currentParty.Id}", null);
                
                if (response.IsSuccessStatusCode)
                {
                    // 离开SignalR组
                    if (_hubConnection?.State == HubConnectionState.Connected)
                    {
                        await _hubConnection.InvokeAsync("LeavePartyGroup", _currentParty.Id);
                    }
                    
                    _currentParty = null;
                    PartyUpdated?.Invoke(this, null!);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "离开队伍失败");
                return false;
            }
        }
        
        public async Task<bool> InvitePlayerAsync(string playerId)
        {
            if (_currentParty == null) return false;
            
            try
            {
                var response = await _http.PostAsJsonAsync("api/party/invite", new
                {
                    PartyId = _currentParty.Id,
                    PlayerId = playerId
                });
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "邀请玩家失败");
                return false;
            }
        }
        
        public async Task<bool> KickMemberAsync(string memberId)
        {
            if (_currentParty == null) return false;
            
            try
            {
                var response = await _http.PostAsJsonAsync("api/party/kick", new
                {
                    PartyId = _currentParty.Id,
                    MemberId = memberId
                });
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "踢出成员失败");
                return false;
            }
        }
        
        public async Task<bool> PromoteToLeaderAsync(string memberId)
        {
            if (_currentParty == null) return false;
            
            try
            {
                var response = await _http.PostAsJsonAsync("api/party/promote", new
                {
                    PartyId = _currentParty.Id,
                    MemberId = memberId
                });
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "转让队长失败");
                return false;
            }
        }
        
        public async Task<bool> SetReadyStatusAsync(bool isReady)
        {
            if (_currentParty == null) return false;
            
            try
            {
                var response = await _http.PostAsJsonAsync("api/party/ready", new
                {
                    PartyId = _currentParty.Id,
                    IsReady = isReady
                });
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置准备状态失败");
                return false;
            }
        }
        
        public async Task<List<PartyInfo>> GetAvailablePartiesAsync()
        {
            try
            {
                var response = await _http.GetFromJsonAsync<ApiResponse<List<PartyInfo>>>("api/party/available");
                if (response?.Success == true && response.Data != null)
                {
                    _availableParties = response.Data;
                    return _availableParties;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用队伍列表失败");
            }
            
            return new List<PartyInfo>();
        }
        
        public void UpdatePartyState(PartyInfo party)
        {
            _currentParty = party;
            PartyUpdated?.Invoke(this, party);
        }
        
        public void Dispose()
        {
            _hubConnection?.DisposeAsync().GetAwaiter().GetResult();
        }
    }
}