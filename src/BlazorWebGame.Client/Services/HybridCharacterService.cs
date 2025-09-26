using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Models;
using BlazorWebGame.Services;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Events;
using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorWebGame.Client.Services
{
    /// <summary>
    /// 混合角色服务 - 可以使用本地逻辑或服务端API
    /// </summary>
    public class HybridCharacterService : IAsyncDisposable
    {
        private readonly CharacterService _localCharacterService;
        private readonly ServerCharacterApiService _serverApiService;
        private readonly GameApiService _gameApiService;
        private readonly ServerConfigurationService _serverConfig;
        private readonly ILogger<HybridCharacterService> _logger;
        private HubConnection? _hubConnection;
        private bool _useServerMode = false;

        /// <summary>
        /// 所有角色列表（本地模式兼容）
        /// </summary>
        public List<Player> AllCharacters => _localCharacterService.AllCharacters;
        
        /// <summary>
        /// 当前激活角色（本地模式兼容）
        /// </summary>
        public Player? ActiveCharacter => _localCharacterService.ActiveCharacter;
        
        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action? OnStateChanged;

        /// <summary>
        /// 是否启用服务器模式
        /// </summary>
        public bool UseServerMode 
        { 
            get => _useServerMode; 
            set
            {
                if (_useServerMode != value)
                {
                    _useServerMode = value;
                    _logger.LogInformation($"Character service switched to {(value ? "server" : "local")} mode");
                }
            }
        }

        public HybridCharacterService(
            CharacterService localCharacterService,
            ServerCharacterApiService serverApiService,
            GameApiService gameApiService,
            ServerConfigurationService serverConfig,
            ILogger<HybridCharacterService> logger)
        {
            _localCharacterService = localCharacterService;
            _serverApiService = serverApiService;
            _gameApiService = gameApiService;
            _serverConfig = serverConfig;
            _logger = logger;

            // 转发本地服务的状态变化事件
            _localCharacterService.OnStateChanged += () => OnStateChanged?.Invoke();
        }

        /// <summary>
        /// 初始化服务
        /// </summary>
        public async Task InitializeAsync()
        {
            // 尝试连接到服务器
            try
            {
                var isServerAvailable = await _gameApiService.IsServerAvailableAsync();
                if (isServerAvailable)
                {
                    UseServerMode = true;
                    await InitializeServerConnectionAsync();
                    await SyncWithServerAsync();
                }
                else
                {
                    UseServerMode = false;
                    await _localCharacterService.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to server, falling back to local mode");
                UseServerMode = false;
                await _localCharacterService.InitializeAsync();
            }
        }

        /// <summary>
        /// 设置激活角色
        /// </summary>
        public async Task<bool> SetActiveCharacterAsync(string characterId)
        {
            if (UseServerMode)
            {
                // 在服务器模式下，我们需要同时更新本地状态和通知服务器
                var localSuccess = _localCharacterService.SetActiveCharacter(characterId);
                if (localSuccess && _hubConnection != null)
                {
                    try
                    {
                        await _hubConnection.SendAsync("JoinCharacterUpdates", characterId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to join character updates for {CharacterId}", characterId);
                    }
                }
                return localSuccess;
            }
            else
            {
                return _localCharacterService.SetActiveCharacter(characterId);
            }
        }

        /// <summary>
        /// 创建新角色
        /// </summary>
        public async Task<bool> CreateCharacterAsync(string name)
        {
            if (UseServerMode)
            {
                try
                {
                    var request = new CreateCharacterRequest { Name = name };
                    var response = await _serverApiService.CreateCharacterAsync(request);
                    
                    if (response.Success && response.Data != null)
                    {
                        // 创建对应的本地Player对象
                        var localPlayer = new Player
                        {
                            Id = response.Data.Id,
                            Name = response.Data.Name,
                            Health = response.Data.Health,
                            MaxHealth = response.Data.MaxHealth,
                            Gold = response.Data.Gold,
                            IsDead = response.Data.IsDead
                        };
                        
                        _localCharacterService.AllCharacters.Add(localPlayer);
                        
                        if (_localCharacterService.ActiveCharacter == null)
                        {
                            await SetActiveCharacterAsync(localPlayer.Id);
                        }
                        
                        OnStateChanged?.Invoke();
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create character on server");
                    return false;
                }
            }
            else
            {
                _localCharacterService.CreateCharacter(name);
                return true;
            }
        }

        /// <summary>
        /// 添加战斗经验值
        /// </summary>
        public async Task AddBattleXPAsync(Player player, BattleProfession profession, int amount)
        {
            if (UseServerMode)
            {
                try
                {
                    var request = new AddExperienceRequest
                    {
                        ProfessionType = "Battle",
                        Profession = profession.ToString(),
                        Amount = amount
                    };
                    
                    var response = await _serverApiService.AddExperienceAsync(player.Id, request);
                    if (response.Success)
                    {
                        // 同时更新本地状态以保持同步
                        _localCharacterService.AddBattleXP(player, profession, amount);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to add battle XP on server: {Message}", response.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add battle XP via server API");
                    // 回退到本地处理
                    _localCharacterService.AddBattleXP(player, profession, amount);
                }
            }
            else
            {
                _localCharacterService.AddBattleXP(player, profession, amount);
            }
        }

        /// <summary>
        /// 添加采集经验值
        /// </summary>
        public async Task AddGatheringXPAsync(Player player, GatheringProfession profession, int amount)
        {
            if (UseServerMode)
            {
                try
                {
                    var request = new AddExperienceRequest
                    {
                        ProfessionType = "Gathering",
                        Profession = profession.ToString(),
                        Amount = amount
                    };
                    
                    var response = await _serverApiService.AddExperienceAsync(player.Id, request);
                    if (response.Success)
                    {
                        _localCharacterService.AddGatheringXP(player, profession, amount);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to add gathering XP on server: {Message}", response.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add gathering XP via server API");
                    _localCharacterService.AddGatheringXP(player, profession, amount);
                }
            }
            else
            {
                _localCharacterService.AddGatheringXP(player, profession, amount);
            }
        }

        /// <summary>
        /// 初始化与服务器的连接
        /// </summary>
        private async Task InitializeServerConnectionAsync()
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{_serverConfig.CurrentServerUrl}/gamehub")
                    .Build();

                // 订阅服务器事件
                _hubConnection.On<object>("GameEvent", OnServerGameEvent);
                _hubConnection.On("RefreshState", OnServerRefreshState);

                await _hubConnection.StartAsync();
                _logger.LogInformation("Connected to game hub");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to game hub");
                _hubConnection = null;
            }
        }

        /// <summary>
        /// 与服务器同步角色数据
        /// </summary>
        private async Task SyncWithServerAsync()
        {
            try
            {
                var response = await _serverApiService.GetCharactersAsync();
                if (response.Success && response.Data != null)
                {
                    // 清除本地角色并从服务器重新加载
                    _localCharacterService.AllCharacters.Clear();
                    
                    foreach (var serverChar in response.Data)
                    {
                        var localPlayer = new Player
                        {
                            Id = serverChar.Id,
                            Name = serverChar.Name,
                            Health = serverChar.Health,
                            MaxHealth = serverChar.MaxHealth,
                            Gold = serverChar.Gold,
                            IsDead = serverChar.IsDead
                        };
                        
                        _localCharacterService.AllCharacters.Add(localPlayer);
                    }

                    if (_localCharacterService.AllCharacters.Any() && _localCharacterService.ActiveCharacter == null)
                    {
                        await SetActiveCharacterAsync(_localCharacterService.AllCharacters.First().Id);
                    }

                    _logger.LogInformation($"Synced {response.Data.Count} characters from server");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync with server");
            }
        }

        /// <summary>
        /// 处理服务器游戏事件
        /// </summary>
        private void OnServerGameEvent(object eventData)
        {
            _logger.LogDebug("Received game event from server: {EventData}", eventData);
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// 处理服务器刷新状态通知
        /// </summary>
        private void OnServerRefreshState()
        {
            _logger.LogDebug("Received refresh state from server");
            OnStateChanged?.Invoke();
        }

        // 转发本地服务的方法
        public void UpdateBuffs(Player character, double elapsedSeconds) => _localCharacterService.UpdateBuffs(character, elapsedSeconds);
        public void InitializePlayerState(Player character) => _localCharacterService.InitializePlayerState(character);
        public int GetLevel(Player player, BattleProfession profession) => _localCharacterService.GetLevel(player, profession);
        public int GetLevel(Player player, GatheringProfession profession) => _localCharacterService.GetLevel(player, profession);
        public int GetLevel(Player player, ProductionProfession profession) => _localCharacterService.GetLevel(player, profession);

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}