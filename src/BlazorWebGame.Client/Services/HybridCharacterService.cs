using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Models;
using BlazorWebGame.Services;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services
{
    /// <summary>
    /// 简化角色服务 - 现在只使用服务器API，保留本地UI状态缓存
    /// </summary>
    public class HybridCharacterService : IAsyncDisposable
    {
        private readonly CharacterService _localCharacterService; // 仅用于UI状态管理
        private readonly ServerCharacterApiService _serverApiService;
        private readonly GameApiService _gameApiService;
        private readonly ILogger<HybridCharacterService> _logger;

        /// <summary>
        /// 所有角色列表（本地缓存用于UI展示）
        /// </summary>
        public List<Player> AllCharacters => _localCharacterService.AllCharacters;
        
        /// <summary>
        /// 当前激活角色（本地缓存用于UI展示）
        /// </summary>
        public Player? ActiveCharacter => _localCharacterService.ActiveCharacter;
        
        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action? OnStateChanged;

        public HybridCharacterService(
            CharacterService localCharacterService,
            ServerCharacterApiService serverApiService,
            GameApiService gameApiService,
            ILogger<HybridCharacterService> logger)
        {
            _localCharacterService = localCharacterService;
            _serverApiService = serverApiService;
            _gameApiService = gameApiService;
            _logger = logger;

            // 转发本地服务的状态变化事件
            _localCharacterService.OnStateChanged += () => OnStateChanged?.Invoke();
        }

        /// <summary>
        /// 初始化服务 - 现在只使用服务器模式
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // 尝试从服务器同步角色数据
                await SyncWithServerAsync();
                _logger.LogInformation("Character service initialized in server mode");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize server connection, working with local cache only");
                // 即使服务器连接失败，也保持本地UI状态可用
                await _localCharacterService.InitializeAsync();
            }
        }

        /// <summary>
        /// 设置激活角色 - 现在只通过服务器API
        /// </summary>
        public async Task<bool> SetActiveCharacterAsync(string characterId)
        {
            try
            {
                var localSuccess = _localCharacterService.SetActiveCharacter(characterId);
                if (localSuccess)
                {
                    _logger.LogInformation("Character {CharacterId} set as active", characterId);
                }
                return localSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set active character {CharacterId}", characterId);
                return false;
            }
        }

        /// <summary>
        /// 创建新角色 - 现在只通过服务器API
        /// </summary>
        public async Task<bool> CreateCharacterAsync(string name)
        {
            try
            {
                var request = new CreateCharacterRequest { Name = name };
                var response = await _serverApiService.CreateCharacterAsync(request);
                
                if (response.Success && response.Data != null)
                {
                    // 创建对应的本地Player对象用于UI展示
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

        /// <summary>
        /// 添加战斗经验值 - 现在由服务器自动处理，不需要客户端调用
        /// </summary>
        [Obsolete("经验值更新应由服务器自动处理，客户端不应直接调用")]
        public async Task AddBattleXPAsync(Player player, BattleProfession profession, int amount)
        {
            // 经验值更新现在完全由服务器处理，客户端不应直接调用
            _logger.LogWarning("AddBattleXPAsync called - this should be handled by server automatically");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 添加采集经验值 - 现在由服务器自动处理，不需要客户端调用
        /// </summary>
        [Obsolete("经验值更新应由服务器自动处理，客户端不应直接调用")]
        public async Task AddGatheringXPAsync(Player player, GatheringProfession profession, int amount)
        {
            // 经验值更新现在完全由服务器处理，客户端不应直接调用
            _logger.LogWarning("AddGatheringXPAsync called - this should be handled by server automatically");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 从服务器同步角色数据
        /// </summary>
        private async Task SyncWithServerAsync()
        {
            try
            {
                var response = await _serverApiService.GetCharactersAsync();
                if (response.Success && response.Data != null)
                {
                    // 清除本地角色列表并用服务器数据重新填充
                    _localCharacterService.AllCharacters.Clear();
                    
                    foreach (var serverCharacter in response.Data)
                    {
                        var localPlayer = new Player
                        {
                            Id = serverCharacter.Id,
                            Name = serverCharacter.Name,
                            Health = serverCharacter.Health,
                            MaxHealth = serverCharacter.MaxHealth,
                            Gold = serverCharacter.Gold,
                            IsDead = serverCharacter.IsDead
                        };
                        
                        _localCharacterService.AllCharacters.Add(localPlayer);
                    }

                    if (_localCharacterService.AllCharacters.Any() && _localCharacterService.ActiveCharacter == null)
                    {
                        await SetActiveCharacterAsync(_localCharacterService.AllCharacters.First().Id);
                    }

                    _logger.LogInformation($"Synced {response.Data.Count} characters from server");
                    OnStateChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync with server");
            }
        }

        // 转发本地服务的方法（用于UI兼容性）
        public void UpdateBuffs(Player character, double elapsedSeconds) => _localCharacterService.UpdateBuffs(character, elapsedSeconds);
        public void InitializePlayerState(Player character) => _localCharacterService.InitializePlayerState(character);
        public int GetLevel(Player player, BattleProfession profession) => _localCharacterService.GetLevel(player, profession);
        public int GetLevel(Player player, GatheringProfession profession) => _localCharacterService.GetLevel(player, profession);
        public int GetLevel(Player player, ProductionProfession profession) => _localCharacterService.GetLevel(player, profession);

        public async ValueTask DisposeAsync()
        {
            // 清理资源
            await Task.CompletedTask;
        }
    }
}
