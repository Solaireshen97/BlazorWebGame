using BlazorWebGame.Server.Services.Profession;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Events;
using BlazorWebGame.Shared.Interfaces;
using System.Collections.Concurrent;

namespace BlazorWebGame.Server.Services.Character
{
    /// <summary>
    /// 服务端角色管理服务
    /// </summary>
    public class ServerCharacterService
    {
        private readonly ConcurrentDictionary<string, CharacterDetailsDto> _characters = new();
        private readonly GameEventManager _eventManager;
        private readonly ILogger<ServerCharacterService> _logger;
        private readonly ServerPlayerAttributeService _playerAttributeService;
        private readonly ServerPlayerProfessionService _playerProfessionService;
        private readonly ServerPlayerUtilityService _playerUtilityService;
        private readonly IDataStorageService _dataStorage;

        public ServerCharacterService(
            GameEventManager eventManager, 
            ILogger<ServerCharacterService> logger,
            ServerPlayerAttributeService playerAttributeService,
            ServerPlayerProfessionService playerProfessionService,
            ServerPlayerUtilityService playerUtilityService,
            IDataStorageService dataStorage)
        {
            _eventManager = eventManager;
            _logger = logger;
            _playerAttributeService = playerAttributeService;
            _playerProfessionService = playerProfessionService;
            _playerUtilityService = playerUtilityService;
            _dataStorage = dataStorage;
            
            // 初始化一些测试角色
            InitializeTestCharacters();
        }

        /// <summary>
        /// 获取角色列表
        /// </summary>
        public async Task<List<CharacterDto>> GetCharactersAsync()
        {
            await Task.Delay(1); // 模拟异步操作
            return _characters.Values
                .Select(c => new CharacterDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Health = c.Health,
                    MaxHealth = c.MaxHealth,
                    Gold = c.Gold,
                    IsDead = c.IsDead,
                    RevivalTimeRemaining = c.RevivalTimeRemaining,
                    CurrentAction = c.CurrentAction,
                    SelectedBattleProfession = c.SelectedBattleProfession,
                    LastUpdated = c.LastUpdated
                })
                .ToList();
        }

        /// <summary>
        /// 获取角色详细信息
        /// </summary>
        public async Task<CharacterDetailsDto?> GetCharacterDetailsAsync(string characterId)
        {
            await Task.Delay(1); // 模拟异步操作
            return _characters.TryGetValue(characterId, out var character) ? character : null;
        }

        /// <summary>
        /// 创建新角色
        /// </summary>
        public async Task<CharacterDto> CreateCharacterAsync(CreateCharacterRequest request, string? userId = null)
        {
            var characterId = Guid.NewGuid().ToString();
            var character = new CharacterDetailsDto
            {
                Id = characterId,
                Name = request.Name,
                Health = 100,
                MaxHealth = 100,
                Gold = 10000,
                IsDead = false,
                RevivalTimeRemaining = 0,
                CurrentAction = "Idle",
                SelectedBattleProfession = "Warrior",
                LastUpdated = DateTime.UtcNow
            };

            // 初始化专业经验值
            InitializeCharacterProfessions(character);
            
            // 使用新的玩家服务初始化角色
            _playerUtilityService.InitializeCollections(character);
            _playerAttributeService.InitializePlayerAttributes(character);

            _characters.TryAdd(characterId, character);

            // 如果提供了用户ID，创建用户角色关联
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    // 检查用户是否已有默认角色
                    var userCharacters = await _dataStorage.GetUserCharactersAsync(userId);
                    var isFirstCharacter = !userCharacters.Success || userCharacters.Data?.Count == 0;
                    
                    var relationResult = await _dataStorage.CreateUserCharacterAsync(
                        userId, characterId, request.Name, isFirstCharacter);
                    
                    if (relationResult.Success)
                    {
                        _logger.LogInformation($"Created user-character relationship: User {userId} -> Character {characterId} ({request.Name})");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to create user-character relationship: {relationResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error creating user-character relationship for User {userId} -> Character {characterId}");
                }
            }

            // 触发角色创建事件
            _eventManager.Raise(new GameEventArgs(GameEventType.CharacterCreated, characterId));

            _logger.LogInformation($"Created character {request.Name} with ID {characterId}" + 
                (userId != null ? $" for user {userId}" : ""));

            await Task.Delay(1); // 模拟异步操作
            return new CharacterDto
            {
                Id = character.Id,
                Name = character.Name,
                Health = character.Health,
                MaxHealth = character.MaxHealth,
                Gold = character.Gold,
                IsDead = character.IsDead,
                RevivalTimeRemaining = character.RevivalTimeRemaining,
                CurrentAction = character.CurrentAction,
                SelectedBattleProfession = character.SelectedBattleProfession,
                LastUpdated = character.LastUpdated
            };
        }

        /// <summary>
        /// 获取用户拥有的角色列表
        /// </summary>
        public async Task<List<CharacterDto>> GetUserCharactersAsync(string userId)
        {
            try
            {
                var userCharactersResult = await _dataStorage.GetUserCharactersAsync(userId);
                if (!userCharactersResult.Success || userCharactersResult.Data == null)
                {
                    return new List<CharacterDto>();
                }

                var characters = new List<CharacterDto>();
                foreach (var userChar in userCharactersResult.Data.Where(uc => uc.IsActive))
                {
                    //var characterData = await _dataStorage.GetCharacterAsync(userChar.CharacterId);
                    if (_characters.TryGetValue(userChar.CharacterId, out var character))
                    {
                        characters.Add(new CharacterDto
                        {
                            Id = character.Id,
                            Name = character.Name,
                            Health = character.Health,
                            MaxHealth = character.MaxHealth,
                            Gold = character.Gold,
                            IsDead = character.IsDead,
                            RevivalTimeRemaining = character.RevivalTimeRemaining,
                            CurrentAction = character.CurrentAction,
                            SelectedBattleProfession = character.SelectedBattleProfession,
                            LastUpdated = character.LastUpdated
                        });
                    }
                }

                return characters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting characters for user {userId}");
                return new List<CharacterDto>();
            }
        }

        /// <summary>
        /// 验证用户是否拥有指定角色
        /// </summary>
        public async Task<bool> UserOwnsCharacterAsync(string userId, string characterId)
        {
            try
            {
                return await _dataStorage.UserOwnsCharacterAsync(userId, characterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying character ownership: User {userId}, Character {characterId}");
                return false;
            }
        }

        /// <summary>
        /// 添加经验值
        /// </summary>
        public async Task<bool> AddExperienceAsync(AddExperienceRequest request)
        {
            if (!_characters.TryGetValue(request.CharacterId, out var character))
                return false;

            (bool leveledUp, int oldLevel, int newLevel) = request.ProfessionType.ToLower() switch
            {
                "battle" => _playerProfessionService.AddBattleXP(character, 
                    Enum.Parse<Models.BattleProfession>(request.Profession), request.Amount),
                "gathering" => _playerProfessionService.AddGatheringXP(character,
                    Enum.Parse<Models.GatheringProfession>(request.Profession), request.Amount),
                "production" => _playerProfessionService.AddProductionXP(character,
                    Enum.Parse<Models.ProductionProfession>(request.Profession), request.Amount),
                _ => (false, 0, 0)
            };

            character.LastUpdated = DateTime.UtcNow;

            // 检查是否升级
            if (leveledUp)
            {
                // 如果是战斗专业升级，需要更新属性
                if (request.ProfessionType.ToLower() == "battle" && request.Profession == character.SelectedBattleProfession)
                {
                    _playerAttributeService.UpdateBaseAttributes(character);
                    character.MaxHealth = _playerAttributeService.GetTotalMaxHealth(character);
                    character.Health = character.MaxHealth; // 升级时恢复满血
                }

                _eventManager.Raise(new GameEventArgs(GameEventType.LevelUp, request.CharacterId, 
                    new { request.ProfessionType, request.Profession, OldLevel = oldLevel, NewLevel = newLevel }));
                
                _logger.LogInformation($"Character {character.Name} leveled up in {request.Profession} from {oldLevel} to {newLevel}");
            }

            // 触发经验获得事件
            _eventManager.Raise(new GameEventArgs(GameEventType.ExperienceGained, request.CharacterId, 
                new { request.ProfessionType, request.Profession, request.Amount }));

            await Task.Delay(1); // 模拟异步操作
            return true;
        }

        /// <summary>
        /// 更新角色状态
        /// </summary>
        public async Task<bool> UpdateCharacterStatusAsync(UpdateCharacterStatusRequest request)
        {
            if (!_characters.TryGetValue(request.CharacterId, out var character))
                return false;

            character.CurrentAction = request.Action;
            character.LastUpdated = DateTime.UtcNow;

            // 根据传入的数据更新角色状态
            foreach (var kvp in request.Data)
            {
                switch (kvp.Key.ToLower())
                {
                    case "health":
                        if (kvp.Value is int health) character.Health = health;
                        break;
                    case "gold":
                        if (kvp.Value is int gold) character.Gold = gold;
                        break;
                    case "isdead":
                        if (kvp.Value is bool isDead) character.IsDead = isDead;
                        break;
                    case "revivaltimeremaining":
                        if (kvp.Value is double revivalTime) character.RevivalTimeRemaining = revivalTime;
                        break;
                }
            }

            // 触发状态变化事件
            _eventManager.Raise(new GameEventArgs(GameEventType.CharacterStatChanged, request.CharacterId));

            await Task.Delay(1); // 模拟异步操作
            return true;
        }

        /// <summary>
        /// 获取角色的总属性值
        /// </summary>
        public AttributeSetDto? GetCharacterTotalAttributes(string characterId)
        {
            if (_characters.TryGetValue(characterId, out var character))
            {
                return _playerAttributeService.GetTotalAttributes(character);
            }
            return null;
        }

        /// <summary>
        /// 获取角色的攻击力
        /// </summary>
        public int GetCharacterAttackPower(string characterId)
        {
            if (_characters.TryGetValue(characterId, out var character))
            {
                return _playerAttributeService.GetTotalAttackPower(character);
            }
            return 0;
        }

        /// <summary>
        /// 获取角色专业等级
        /// </summary>
        public int GetCharacterProfessionLevel(string characterId, string professionType, string profession)
        {
            if (!_characters.TryGetValue(characterId, out var character))
                return 1;

            return professionType.ToLower() switch
            {
                "battle" => _playerProfessionService.GetLevel(character, 
                    Enum.Parse<Models.BattleProfession>(profession)),
                "gathering" => _playerProfessionService.GetLevel(character,
                    Enum.Parse<Models.GatheringProfession>(profession)),
                "production" => _playerProfessionService.GetLevel(character,
                    Enum.Parse<Models.ProductionProfession>(profession)),
                _ => 1
            };
        }

        /// <summary>
        /// 检查角色是否满足等级要求
        /// </summary>
        public bool CheckLevelRequirement(string characterId, string profession, int requiredLevel)
        {
            if (!_characters.TryGetValue(characterId, out var character))
                return false;

            var battleProfession = Enum.Parse<Models.BattleProfession>(profession);
            return _playerUtilityService.MeetsLevelRequirement(character, battleProfession, requiredLevel);
        }

        /// <summary>
        /// 计算等级（简化版经验值计算）
        /// </summary>
        private int GetLevel(CharacterDetailsDto character, string professionType, string profession)
        {
            int xp = 0;
            switch (professionType.ToLower())
            {
                case "battle":
                    character.BattleProfessionXP.TryGetValue(profession, out xp);
                    break;
                case "gathering":
                    character.GatheringProfessionXP.TryGetValue(profession, out xp);
                    break;
                case "production":
                    character.ProductionProfessionXP.TryGetValue(profession, out xp);
                    break;
            }

            // 简单的等级计算：每1000经验值一级
            return Math.Max(1, xp / 1000 + 1);
        }

        /// <summary>
        /// 初始化角色专业
        /// </summary>
        private void InitializeCharacterProfessions(CharacterDetailsDto character)
        {
            // 战斗专业
            character.BattleProfessionXP["Warrior"] = 0;
            character.BattleProfessionXP["Mage"] = 0;
            character.BattleProfessionXP["Archer"] = 0;
            character.BattleProfessionXP["Paladin"] = 0;

            // 采集专业
            character.GatheringProfessionXP["Mining"] = 0;
            character.GatheringProfessionXP["Herbalism"] = 0;
            character.GatheringProfessionXP["Fishing"] = 0;

            // 生产专业
            character.ProductionProfessionXP["Cooking"] = 0;
            character.ProductionProfessionXP["Alchemy"] = 0;
            character.ProductionProfessionXP["Blacksmithing"] = 0;
            character.ProductionProfessionXP["Jewelcrafting"] = 0;
            character.ProductionProfessionXP["Leatherworking"] = 0;
            character.ProductionProfessionXP["Tailoring"] = 0;
            character.ProductionProfessionXP["Engineering"] = 0;
        }

        /// <summary>
        /// 检查并处理升级
        /// </summary>
        public void CheckLevelUp(Shared.Models.ServerBattlePlayer player)
        {
            // 简化的升级检查逻辑
            var currentLevel = player.Level;
            var xpForNextLevel = currentLevel * 1000 + 500; // 简单的升级公式
            
            if (player.Experience >= xpForNextLevel)
            {
                player.Level++;
                player.Experience -= xpForNextLevel;
                
                // 升级时增加属性
                player.MaxHealth += 10;
                player.Health = player.MaxHealth; // 升级时恢复满血
                player.MaxMana += 5;
                player.Mana = player.MaxMana;
                
                // 增加基础属性
                player.Strength += 2;
                player.Agility += 2;
                player.Intellect += 2;
                player.Spirit += 2;
                player.Stamina += 2;
                
                _logger.LogInformation("Player {PlayerId} leveled up to level {Level}", player.Id, player.Level);
            }
        }

        /// <summary>
        /// 初始化测试角色
        /// </summary>
        private void InitializeTestCharacters()
        {
            var testCharacter1 = new CharacterDetailsDto
            {
                Id = "test-character-1",
                Name = "测试者",
                Health = 100,
                MaxHealth = 100,
                Gold = 10000,
                IsDead = false,
                RevivalTimeRemaining = 0,
                CurrentAction = "Idle",
                SelectedBattleProfession = "Warrior",
                LastUpdated = DateTime.UtcNow
            };
            InitializeCharacterProfessions(testCharacter1);
            _playerUtilityService.InitializeCollections(testCharacter1);
            _playerAttributeService.InitializePlayerAttributes(testCharacter1);
            _characters.TryAdd(testCharacter1.Id, testCharacter1);

            var testCharacter2 = new CharacterDetailsDto
            {
                Id = "test-character-2",
                Name = "阿尔忒弥斯",
                Health = 100,
                MaxHealth = 100,
                Gold = 50,
                IsDead = false,
                RevivalTimeRemaining = 0,
                CurrentAction = "Idle",
                SelectedBattleProfession = "Archer",
                LastUpdated = DateTime.UtcNow
            };
            InitializeCharacterProfessions(testCharacter2);
            _playerUtilityService.InitializeCollections(testCharacter2);
            _playerAttributeService.InitializePlayerAttributes(testCharacter2);
            _characters.TryAdd(testCharacter2.Id, testCharacter2);

            _logger.LogInformation("Initialized test characters with player services");
        }
    }
}