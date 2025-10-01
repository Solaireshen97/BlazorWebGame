using BlazorWebGame.Server.Services.Profession;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Character;
using BlazorWebGame.Shared.Events;
using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using EquipmentDto = BlazorWebGame.Shared.DTOs.Character.EquipmentDto;
using ReputationDto = BlazorWebGame.Shared.DTOs.Character.ReputationDto;

namespace BlazorWebGame.Server.Services.Character
{
    /// <summary>
    /// 增强版服务端角色管理服务 - 使用领域模型
    /// </summary>
    public class EnhancedServerCharacterService
    {
        // 使用内存缓存+过期策略
        private readonly ConcurrentDictionary<string, Tuple<BlazorWebGame.Shared.Models.Character, DateTime>> _characterCache = new();

        // 缓存过期时间（例如30分钟不活跃就从内存中移除）
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

        private readonly ConcurrentDictionary<string, BlazorWebGame.Shared.Models.Character> _characters = new();
        private readonly GameEventManager _eventManager;
        private readonly ILogger<EnhancedServerCharacterService> _logger;
        private readonly ServerPlayerAttributeService _attributeService;
        private readonly ServerPlayerProfessionService _professionService;
        private readonly ServerPlayerUtilityService _utilityService;
        private readonly IDataStorageService _dataStorage;
        private readonly GameClock _gameClock;

        public EnhancedServerCharacterService(
            GameEventManager eventManager,
            ILogger<EnhancedServerCharacterService> logger,
            ServerPlayerAttributeService attributeService,
            ServerPlayerProfessionService professionService,
            ServerPlayerUtilityService utilityService,
            IDataStorageService dataStorage,
            GameClock gameClock)
        {
            _eventManager = eventManager;
            _logger = logger;
            _attributeService = attributeService;
            _professionService = professionService;
            _utilityService = utilityService;
            _dataStorage = dataStorage;
            _gameClock = gameClock;
            
            // 初始化测试角色
            InitializeTestCharacters();

            // 添加定时任务，定期清理过期的角色缓存
            StartCacheCleanupTask();
        }

        // 获取角色 - 如果不在缓存中则从数据库加载
        private async Task<BlazorWebGame.Shared.Models.Character?> GetOrLoadCharacterAsync(string characterId)
        {
            // 检查缓存
            if (_characterCache.TryGetValue(characterId, out var cachedData))
            {
                // 更新最后访问时间
                _characterCache[characterId] = new Tuple<BlazorWebGame.Shared.Models.Character, DateTime>(
                    cachedData.Item1, DateTime.UtcNow);
                return cachedData.Item1;
            }

            // 从数据库加载
            var response = await _dataStorage.GetCharacterByIdAsync(characterId);
            if (!response.IsSuccess || response.Data == null)
            {
                return null;
            }

            // 转换为领域模型
            var character = ConvertToDomainModel(response.Data);

            // 添加到缓存
            _characterCache[characterId] = new Tuple<BlazorWebGame.Shared.Models.Character, DateTime>(
                character, DateTime.UtcNow);

            return character;
        }

        // 保存角色到数据库
        private async Task SaveCharacterToStorageAsync(BlazorWebGame.Shared.Models.Character character)
        {
            // 使用CharacterMapper转换为DTO
            var dto = BlazorWebGame.Shared.Mappers.CharacterMapper.ToDto(character);
            // 调用数据存储服务保存角色
            await _dataStorage.SaveCharacterAsync(dto);
        }


        // 定期清理缓存
        private void StartCacheCleanupTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        CleanupExpiredCacheEntries();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "清理角色缓存时出错");
                    }

                    // 每5分钟执行一次清理
                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            });
        }

        // 清理过期缓存
        private void CleanupExpiredCacheEntries()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _characterCache
                .Where(kvp => (now - kvp.Value.Item2) > _cacheExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _characterCache.TryRemove(key, out _);
                _logger.LogInformation("已从内存中移除长时间未活动的角色: {CharacterId}", key);
            }
        }

        // 显式从内存中移除角色
        public void RemoveCharacterFromCache(string characterId)
        {
            _characterCache.TryRemove(characterId, out _);
        }

        // 用户离线时，移除该用户的所有角色缓存
        public async Task RemoveUserCharactersFromCacheAsync(string userId)
        {
            var response = await _dataStorage.GetUserCharactersAsync(userId);
            if (response.IsSuccess && response.Data != null)
            {
                foreach (var userChar in response.Data)
                {
                    RemoveCharacterFromCache(userChar.CharacterId);
                }
            }
        }

        /// <summary>
        /// 获取用户的角色花名册
        /// </summary>
        /// <summary>
        /// 获取用户的角色花名册
        /// </summary>
        public async Task<RosterDto> GetUserRosterAsync(string userId)
        {
            try
            {
                var userCharactersResult = await _dataStorage.GetUserCharactersAsync(userId);
                if (!userCharactersResult.IsSuccess || userCharactersResult.Data == null)
                {
                    return CreateEmptyRoster(userId);
                }

                var activeUserCharacters = userCharactersResult.Data.Where(uc => uc.IsActive).ToList();
                var roster = new RosterDto
                {
                    UserId = userId,
                    MaxSlots = 8,
                    UnlockedSlots = Math.Max(1, activeUserCharacters.Count),
                    Slots = new List<CharacterSlotDto>(),
                    ActiveCharacterId = activeUserCharacters.FirstOrDefault(uc => uc.IsDefault)?.CharacterId
                };

                // 预加载所有角色数据到内存中
                foreach (var userChar in activeUserCharacters)
                {
                    // 使用GetOrLoadCharacterAsync确保角色数据被加载到内存中
                    var character = await GetOrLoadCharacterAsync(userChar.CharacterId);
                    if (character != null)
                    {
                        // 确保角色也添加到_characters字典中(GetOrLoadCharacterAsync已经添加到_characterCache)
                        _characters.TryAdd(character.Id, character);
                    }
                }

                // 填充已解锁槽位
                for (int i = 0; i < roster.UnlockedSlots; i++)
                {
                    var userChar = activeUserCharacters.FirstOrDefault(uc => uc.SlotIndex == i);
                    var slot = new CharacterSlotDto
                    {
                        SlotIndex = i,
                        State = userChar != null ? "Occupied" : "Unlocked",
                        UnlockCondition = null,
                        LastPlayedAt = null
                    };

                    if (userChar != null && _characters.TryGetValue(userChar.CharacterId, out var character))
                    {
                        slot.Character = new CharacterSummaryDto
                        {
                            Id = character.Id,
                            Name = character.Name,
                            Level = character.Level,
                            ProfessionName = character.Professions.SelectedBattleProfession,
                            ProfessionIcon = $"images/professions/{character.Professions.SelectedBattleProfession.ToLower()}.png",
                            IsOnline = character.IsOnline,
                            LastActiveAt = character.LastActiveAt
                        };
                        slot.LastPlayedAt = character.LastActiveAt;
                    }

                    roster.Slots.Add(slot);
                }

                // 填充剩余槽位
                for (int i = roster.UnlockedSlots; i < roster.MaxSlots; i++)
                {
                    roster.Slots.Add(new CharacterSlotDto
                    {
                        SlotIndex = i,
                        State = "Locked",
                        UnlockCondition = i < 3 ? "完成新手教程" : $"角色达到{i * 10}级",
                        Character = null
                    });
                }

                return roster;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取用户花名册失败：{userId}");
                return CreateEmptyRoster(userId);
            }
        }

        /// <summary>
        /// 解锁角色槽位
        /// </summary>
        public async Task<ApiResponse<CharacterSlotDto>> UnlockSlotAsync(string userId, int slotIndex)
        {
            try
            {
                // 获取用户当前花名册
                var roster = await GetUserRosterAsync(userId);
                
                // 检查槽位是否已解锁
                if (slotIndex < 0 || slotIndex >= roster.MaxSlots)
                {
                    return ApiResponse<CharacterSlotDto>.Failure("无效的槽位索引");
                }
                
                if (slotIndex < roster.UnlockedSlots)
                {
                    return ApiResponse<CharacterSlotDto>.Failure("该槽位已解锁");
                }

                // 检查解锁条件
                bool canUnlock = false;
                
                // 模拟解锁条件检查
                if (slotIndex < 3)
                {
                    // 前三个槽位只需完成新手教程
                    canUnlock = true;
                }
                else
                {
                    // 其他槽位需要角色达到一定等级
                    var requiredLevel = slotIndex * 10;
                    var userCharacters = await _dataStorage.GetUserCharactersAsync(userId);
                    if (userCharacters.IsSuccess && userCharacters.Data != null)
                    {
                        foreach (var userChar in userCharacters.Data)
                        {
                            if (_characters.TryGetValue(userChar.CharacterId, out var character) && 
                                character.Level >= requiredLevel)
                            {
                                canUnlock = true;
                                break;
                            }
                        }
                    }
                }

                if (!canUnlock)
                {
                    return ApiResponse<CharacterSlotDto>.Failure($"未满足解锁条件：{roster.Slots[slotIndex].UnlockCondition}");
                }

                // 更新数据库中的槽位状态
                await _dataStorage.UnlockCharacterSlotAsync(userId, slotIndex);

                // 创建新的槽位信息
                var newSlot = new CharacterSlotDto
                {
                    SlotIndex = slotIndex,
                    State = "Unlocked",
                    Character = null
                };

                _logger.LogInformation($"用户 {userId} 解锁了槽位 {slotIndex}");

                return ApiResponse<CharacterSlotDto>.Success(newSlot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"解锁槽位失败：用户 {userId}, 槽位 {slotIndex}");
                return ApiResponse<CharacterSlotDto>.Failure("解锁槽位时发生错误");
            }
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        public async Task<ApiResponse<CharacterFullDto>> CreateCharacterAsync(string userId, CreateCharacterRequestDto request)
        {
            try
            {
                // 验证名称
                var nameValidation = ValidateCharacterName(request.Name);
                if (!nameValidation.IsValid)
                {
                    return ApiResponse<CharacterFullDto>.Failure(nameValidation.Reason!);
                }

                // 验证槽位是否可用
                var roster = await GetUserRosterAsync(userId);
                if (request.SlotIndex < 0 || request.SlotIndex >= roster.UnlockedSlots)
                {
                    return ApiResponse<CharacterFullDto>.Failure("无效的槽位索引");
                }

                if (roster.Slots[request.SlotIndex].State == "Occupied")
                {
                    return ApiResponse<CharacterFullDto>.Failure("该槽位已被占用");
                }

                // 创建新角色
                var character = new BlazorWebGame.Shared.Models.Character(request.Name);

                // 设置职业
                var profession = !string.IsNullOrEmpty(request.StartingProfessionId)
                    ? request.StartingProfessionId
                    : "Warrior";

                character.Professions.SelectBattleProfession(profession);

                // 保存到内存
                _characters.TryAdd(character.Id, character);
                var characterDto = ConvertToStorageDto(character);

                // 保存角色到数据库
                await _dataStorage.SaveCharacterAsync(characterDto);

                // 创建用户-角色关联
                var isFirstCharacter = roster.Slots.All(s => s.Character == null);
                var relationResult = await _dataStorage.CreateUserCharacterAsync(
                    userId, character.Id, request.Name, isFirstCharacter, request.SlotIndex);

                if (!relationResult.IsSuccess)
                {
                    _characters.TryRemove(character.Id, out _);
                    return ApiResponse<CharacterFullDto>.Failure($"创建角色关联失败：{relationResult.Message}");
                }

                // 触发角色创建事件
                _eventManager.Raise(new GameEventArgs(GameEventType.CharacterCreated, character.Id));

                _logger.LogInformation($"用户 {userId} 创建了角色 {request.Name}，ID：{character.Id}");

                // 将角色转换为DTO
                var characterFullDto = ConvertToFullDto(character);
                return ApiResponse<CharacterFullDto>.Success(characterFullDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建角色失败：{request.Name}");
                return ApiResponse<CharacterFullDto>.Failure("创建角色时发生错误");
            }
        }

        /// <summary>
        /// 验证角色名称
        /// </summary>
        public ValidateCharacterNameResult ValidateCharacterName(string name)
        {
            // 检查名称长度
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 20)
            {
                return new ValidateCharacterNameResult 
                { 
                    IsValid = false, 
                    Reason = "名称长度应为2-20个字符" 
                };
            }

            // 检查特殊字符
            if (name.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
            {
                return new ValidateCharacterNameResult 
                { 
                    IsValid = false, 
                    Reason = "名称只能包含字母、数字和空格" 
                };
            }

            // 检查重复名称
            if (_characters.Values.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                return new ValidateCharacterNameResult 
                { 
                    IsValid = false, 
                    Reason = "该名称已被使用" 
                };
            }

            return new ValidateCharacterNameResult { IsValid = true };
        }

        /// <summary>
        /// 获取角色详情
        /// </summary>
        public async Task<ApiResponse<CharacterFullDto>> GetCharacterDetails(string characterId)
        {
            try
            {
                var character = await GetOrLoadCharacterAsync(characterId);
                if (character == null)
                {
                    return ApiResponse<CharacterFullDto>.Failure("角色不存在");
                }

                return ApiResponse<CharacterFullDto>.Success(ConvertToFullDto(character));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色详情失败 {CharacterId}", characterId);
                return ApiResponse<CharacterFullDto>.Failure($"获取角色详情失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 切换角色
        /// </summary>
        public async Task<ApiResponse<CharacterFullDto>> SwitchCharacterAsync(string userId, string characterId)
        {
            try
            {
                // 验证用户是否拥有该角色
                bool ownsCharacter = await _dataStorage.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return ApiResponse<CharacterFullDto>.Failure("用户不拥有该角色");
                }

                // 获取角色
                if (!_characters.TryGetValue(characterId, out var character))
                {
                    return ApiResponse<CharacterFullDto>.Failure("角色不存在");
                }

                // 设置角色为活跃状态
                character.GoOnline();
                
                // 更新默认角色
                await _dataStorage.SetDefaultCharacterAsync(userId, characterId);

                //// 计算离线进度
                //var offlineProgress = await CalculateOfflineProgressAsync(character);
                //if (offlineProgress != null)
                //{
                //    // 应用离线进度
                //    ApplyOfflineProgress(character, offlineProgress);

                //    // 触发离线进度事件
                //    _eventManager.Raise(new GameEventArgs(
                //        GameEventType.OfflineProgressCalculated, 
                //        character.Id,
                //        offlineProgress));
                //}

                var characterDto = ConvertToFullDto(character);
                return ApiResponse<CharacterFullDto>.Success(characterDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"切换角色失败：{characterId}");
                return ApiResponse<CharacterFullDto>.Failure("切换角色时发生错误");
            }
        }

        /// <summary>
        /// 检查用户是否拥有角色
        /// </summary>
        public async Task<bool> UserOwnsCharacterAsync(string userId, string characterId)
        {
            try
            {
                return await _dataStorage.UserOwnsCharacterAsync(userId, characterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查用户角色所有权失败 {UserId} -> {CharacterId}", userId, characterId);
                return false;
            }
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteCharacterAsync(string userId, string characterId)
        {
            try
            {
                // 验证用户是否拥有该角色
                bool ownsCharacter = await _dataStorage.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return ApiResponse<bool>.Failure("用户不拥有该角色");
                }

                // 获取用户所有角色
                var userCharactersResult = await _dataStorage.GetUserCharactersAsync(userId);
                if (!userCharactersResult.IsSuccess || userCharactersResult.Data == null)
                {
                    return ApiResponse<bool>.Failure("获取用户角色失败");
                }

                // 检查是否是唯一角色
                if (userCharactersResult.Data.Count(uc => uc.IsActive) == 1)
                {
                    return ApiResponse<bool>.Failure("无法删除唯一角色");
                }

                // 从数据库中删除角色关联
                var result = await _dataStorage.DeleteUserCharacterAsync(userId, characterId);
                if (!result.IsSuccess)
                {
                    return ApiResponse<bool>.Failure($"删除角色失败：{result.Message}");
                }

                // 从内存中删除角色
                _characters.TryRemove(characterId, out var character);

                // 触发角色删除事件
                _eventManager.Raise(new GameEventArgs(GameEventType.CharacterDeleted, characterId));

                _logger.LogInformation($"用户 {userId} 删除了角色 {characterId}");
                return ApiResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除角色失败：{characterId}");
                return ApiResponse<bool>.Failure("删除角色时发生错误");
            }
        }

        /// <summary>
        /// 分配属性点
        /// </summary>
        public async Task<ApiResponse<CharacterAttributesDto>> AllocateAttributePointsAsync(string characterId, AllocateAttributePointsRequest request)
        {
            try
            {
                var character = await GetOrLoadCharacterAsync(characterId);
                if (character == null)
                {
                    return ApiResponse<CharacterAttributesDto>.Failure("角色不存在");
                }

                // 验证总点数是否超过可用点数
                var totalPoints = request.Points.Values.Sum();
                if (totalPoints > character.Attributes.AttributePoints)
                {
                    return ApiResponse<CharacterAttributesDto>.Failure("分配的属性点超过可用点数");
                }

                // 分配属性点
                foreach (var kvp in request.Points)
                {
                    if (kvp.Value <= 0) continue;

                    bool success = character.Attributes.AllocateAttribute(kvp.Key, kvp.Value);
                    if (!success)
                    {
                        return ApiResponse<CharacterAttributesDto>.Failure($"分配属性点失败：{kvp.Key}");
                    }
                }

                // 更新生命值和法力值上限
                character.Vitals.RecalculateMaxValues(character.Attributes);

                // 保存更新后的角色数据到数据库
                await SaveCharacterToStorageAsync(character);

                // 转换为DTO
                var attributesDto = new CharacterAttributesDto
                {
                    Strength = character.Attributes.Strength,
                    Agility = character.Attributes.Agility,
                    Intellect = character.Attributes.Intellect,
                    Spirit = character.Attributes.Spirit,
                    Stamina = character.Attributes.Stamina,
                    AvailablePoints = character.Attributes.AttributePoints,

                    // 衍生属性
                    AttackPower = CalculateAttackPower(character),
                    SpellPower = CalculateSpellPower(character),
                    CriticalChance = CalculateCritChance(character),
                    CriticalDamage = CalculateCritDamage(character),
                    AttackSpeed = 1.0,
                    CastSpeed = 1.0,
                    Armor = CalculateArmor(character),
                    MagicResistance = CalculateMagicResistance(character)
                };

                return ApiResponse<CharacterAttributesDto>.Success(attributesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分配属性点失败 {CharacterId}", characterId);
                return ApiResponse<CharacterAttributesDto>.Failure($"分配属性点失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 重置属性点
        /// </summary>
        public async Task<ApiResponse<CharacterAttributesDto>> ResetAttributesAsync(string characterId)
        {
            try
            {
                var character = await GetOrLoadCharacterAsync(characterId);
                if (character == null)
                {
                    return ApiResponse<CharacterAttributesDto>.Failure("角色不存在");
                }

                // 重置属性
                character.Attributes.ResetAttributes();

                // 更新生命值和法力值上限
                character.Vitals.RecalculateMaxValues(character.Attributes);

                // 保存更新后的角色数据到数据库
                await SaveCharacterToStorageAsync(character);

                // 转换为DTO
                var attributesDto = new CharacterAttributesDto
                {
                    Strength = character.Attributes.Strength,
                    Agility = character.Attributes.Agility,
                    Intellect = character.Attributes.Intellect,
                    Spirit = character.Attributes.Spirit,
                    Stamina = character.Attributes.Stamina,
                    AvailablePoints = character.Attributes.AttributePoints,

                    // 衍生属性
                    AttackPower = CalculateAttackPower(character),
                    SpellPower = CalculateSpellPower(character),
                    CriticalChance = CalculateCritChance(character),
                    CriticalDamage = CalculateCritDamage(character),
                    AttackSpeed = 1.0,
                    CastSpeed = 1.0,
                    Armor = CalculateArmor(character),
                    MagicResistance = CalculateMagicResistance(character)
                };

                return ApiResponse<CharacterAttributesDto>.Success(attributesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置属性点失败 {CharacterId}", characterId);
                return ApiResponse<CharacterAttributesDto>.Failure($"重置属性点失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取离线进度
        /// </summary>
        public async Task<ApiResponse<OfflineProgressDto>> GetOfflineProgressAsync(string characterId)
        {
            if (!_characters.TryGetValue(characterId, out var character))
            {
                return ApiResponse<OfflineProgressDto>.Failure("角色不存在");
            }

            var offlineProgress = await CalculateOfflineProgressAsync(character);
            if (offlineProgress == null)
            {
                return ApiResponse<OfflineProgressDto>.Failure("没有离线进度");
            }

            return ApiResponse<OfflineProgressDto>.Success(offlineProgress);
        }

        #region 私有辅助方法

        /// <summary>
        /// 计算攻击力
        /// </summary>
        private double CalculateAttackPower(BlazorWebGame.Shared.Models.Character character)
        {
            return character.Professions.SelectedBattleProfession switch
            {
                "Warrior" => character.Attributes.Strength * 2 + character.Level * 1.5,
                "Archer" => character.Attributes.Agility * 2 + character.Level * 1.2,
                "Mage" => character.Attributes.Intellect * 0.5 + character.Level * 0.8,
                "Paladin" => character.Attributes.Strength * 1.5 + character.Attributes.Intellect * 0.5 + character.Level * 1.3,
                _ => character.Attributes.Strength + character.Level
            };
        }

        /// <summary>
        /// 计算法术强度
        /// </summary>
        private double CalculateSpellPower(BlazorWebGame.Shared.Models.Character character)
        {
            return character.Professions.SelectedBattleProfession switch
            {
                "Mage" => character.Attributes.Intellect * 2 + character.Level * 1.5,
                "Paladin" => character.Attributes.Intellect * 1.5 + character.Attributes.Spirit * 0.5 + character.Level * 1.0,
                "Warrior" => character.Attributes.Intellect * 0.3 + character.Level * 0.5,
                "Archer" => character.Attributes.Intellect * 0.5 + character.Level * 0.7,
                _ => character.Attributes.Intellect + character.Level * 0.5
            };
        }

        /// <summary>
        /// 计算暴击率
        /// </summary>
        private double CalculateCritChance(BlazorWebGame.Shared.Models.Character character)
        {
            double baseChance = 5.0; // 基础暴击率5%
            return baseChance + character.Attributes.Agility * 0.1;
        }

        /// <summary>
        /// 计算暴击伤害
        /// </summary>
        private double CalculateCritDamage(BlazorWebGame.Shared.Models.Character character)
        {
            double baseDamage = 150.0; // 基础暴击伤害150%
            return baseDamage + character.Attributes.Strength * 0.2;
        }

        /// <summary>
        /// 计算护甲值
        /// </summary>
        private double CalculateArmor(BlazorWebGame.Shared.Models.Character character)
        {
            return character.Attributes.Stamina * 1.5 + character.Level * 2;
        }

        /// <summary>
        /// 计算魔法抗性
        /// </summary>
        private double CalculateMagicResistance(BlazorWebGame.Shared.Models.Character character)
        {
            return character.Attributes.Spirit * 1.5 + character.Level;
        }

        /// <summary>
        /// 计算离线进度
        /// </summary>
        private async Task<OfflineProgressDto?> CalculateOfflineProgressAsync(BlazorWebGame.Shared.Models.Character character)
        {
            return null;
        }

        /// <summary>
        /// 应用离线进度
        /// </summary>
        private void ApplyOfflineProgress(BlazorWebGame.Shared.Models.Character character, OfflineProgressDto progress)
        {
            //// 应用经验值
            //if (progress.ExperienceGained > 0)
            //{
            //    character.GainExperience(progress.ExperienceGained);
            //}

            //// 应用金币
            //if (progress.GoldGained > 0)
            //{
            //    character.GainGold(progress.GoldGained);
            //}

            //// 应用资源
            //foreach (var resource in progress.ResourcesGained)
            //{
            //    // 这里应该添加资源到角色背包
            //    character.Inventory.AddItem(resource.Key, resource.Value);
            //}

            //// 应用物品
            //foreach (var item in progress.LootedItems)
            //{
            //    character.Inventory.AddItem(item);
            //}

            //// 清除离线记录
            //character.LastOfflineRecord = null;

            //// 更新角色活跃时间
            //character.UpdateActivity();
        }

        /// <summary>
        /// 创建空花名册
        /// </summary>
        private RosterDto CreateEmptyRoster(string userId)
        {
            var roster = new RosterDto
            {
                UserId = userId,
                MaxSlots = 8,
                UnlockedSlots = 1,
                Slots = new List<CharacterSlotDto>(),
                ActiveCharacterId = null
            };

            // 添加一个解锁的槽位
            roster.Slots.Add(new CharacterSlotDto
            {
                SlotIndex = 0,
                State = "Unlocked",
                Character = null,
                UnlockCondition = null
            });

            // 添加锁定的槽位
            for (int i = 1; i < roster.MaxSlots; i++)
            {
                roster.Slots.Add(new CharacterSlotDto
                {
                    SlotIndex = i,
                    State = "Locked",
                    Character = null,
                    UnlockCondition = i < 3 ? "完成新手教程" : $"角色达到{i * 10}级"
                });
            }

            return roster;
        }

        // 将存储DTO转换为领域模型
        private BlazorWebGame.Shared.Models.Character ConvertToDomainModel(CharacterStorageDto dto)
        {
            // 使用CharacterMapper的ToCharacter方法
            return BlazorWebGame.Shared.Mappers.CharacterMapper.ToCharacter(dto);
        }

        // 将领域模型转换为存储DTO
        private CharacterStorageDto ConvertToStorageDto(BlazorWebGame.Shared.Models.Character character)
        {
            // 使用CharacterMapper的ToDto方法
            return BlazorWebGame.Shared.Mappers.CharacterMapper.ToDto(character);
        }

        /// <summary>
        /// 将领域模型转换为DTO
        /// </summary>
        private CharacterFullDto ConvertToFullDto(BlazorWebGame.Shared.Models.Character character)
        {
            return new CharacterFullDto
            {
                Id = character.Id,
                Name = character.Name,
                Level = character.Level,
                Experience = character.Experience,
                ExperienceToNextLevel = character.GetRequiredExperienceForNextLevel(),
                Gold = character.Gold,
                
                // 生命值
                Vitals = new CharacterVitalsDto
                {
                    Health = character.Vitals.Health,
                    MaxHealth = character.Vitals.MaxHealth,
                    Mana = character.Vitals.Mana,
                    MaxMana = character.Vitals.MaxMana,
                    HealthRegen = character.Attributes.Spirit * 0.1,
                    ManaRegen = character.Attributes.Spirit * 0.2
                },
                
                // 属性
                Attributes = new CharacterAttributesDto
                {
                    Strength = character.Attributes.Strength,
                    Agility = character.Attributes.Agility,
                    Intellect = character.Attributes.Intellect,
                    Spirit = character.Attributes.Spirit,
                    Stamina = character.Attributes.Stamina,
                    AvailablePoints = character.Attributes.AttributePoints,
                    AttackPower = CalculateAttackPower(character),
                    SpellPower = CalculateSpellPower(character),
                    CriticalChance = CalculateCritChance(character),
                    CriticalDamage = CalculateCritDamage(character),
                    AttackSpeed = 1.0,
                    CastSpeed = 1.0,
                    Armor = CalculateArmor(character),
                    MagicResistance = CalculateMagicResistance(character)
                },
                
                // 职业信息
                Profession = new ProfessionInfoDto
                {
                    Id = character.Professions.SelectedBattleProfession,
                    Name = character.Professions.SelectedBattleProfession,
                    Description = GetProfessionDescription(character.Professions.SelectedBattleProfession),
                    Icon = $"images/professions/{character.Professions.SelectedBattleProfession.ToLower()}.png",
                    Level = character.Professions.GetProfessionLevel("Battle", character.Professions.SelectedBattleProfession),
                    Experience = 0, // 需要从character.Professions.BattleProfessions获取
                    ExperienceToNextLevel = 1000, // 同上
                    Specializations = new List<string>(),
                    ActiveSpecialization = null
                },
                
                // 装备
                Equipment = new EquipmentDto
                {
                    TotalGearScore = 0,
                    ActiveSetBonuses = new List<string>()
                },
                
                // 技能
                Skills = new SkillSystemDto
                {
                    ActiveSkills = new List<SkillSlotDto>(),
                    PassiveSkills = new List<SkillSlotDto>(),
                    AvailableSkillPoints = 0
                },
                
                // 当前区域
                CurrentRegionId = character.CurrentRegionId,
                CurrentRegionName = GetRegionName(character.CurrentRegionId),
                
                // 声望
                Reputations = character.Reputations.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ReputationDto
                    {
                        FactionId = kvp.Key,
                        FactionName = GetFactionName(kvp.Key),
                        Current = kvp.Value,
                        Max = 10000,
                        Level = GetReputationLevel(kvp.Value)
                    }),
                
                // 统计
                Statistics = new CharacterStatisticsDto
                {
                    TotalPlayTime = 0,
                    MonstersKilled = 0,
                    DungeonsCompleted = 0,
                    QuestsCompleted = 0,
                    Deaths = 0,
                    TotalDamageDealt = 0,
                    TotalHealingDone = 0,
                    ItemsCrafted = 0,
                    ItemsLooted = 0,
                    AchievementPoints = new Dictionary<string, int>()
                },
                
                CreatedAt = character.CreatedAt,
                LastActiveAt = character.LastActiveAt
            };
        }

        /// <summary>
        /// 获取职业描述
        /// </summary>
        private string GetProfessionDescription(string professionName)
        {
            return professionName switch
            {
                "Warrior" => "勇猛的战士，擅长近战物理输出和承受伤害。",
                "Mage" => "强大的法师，掌握元素力量，擅长远程魔法输出。",
                "Archer" => "敏捷的弓箭手，擅长远程物理输出和闪避。",
                "Paladin" => "神圣的骑士，兼具物理输出和治疗能力。",
                _ => "未知职业"
            };
        }

        /// <summary>
        /// 获取区域名称
        /// </summary>
        private string GetRegionName(string? regionId)
        {
            if (string.IsNullOrEmpty(regionId))
                return "无";
                
            // 模拟区域名称映射
            return regionId switch
            {
                "start_village" => "新手村",
                "forest_1" => "幽暗森林",
                "mine_1" => "黑石矿洞",
                "city_1" => "白鹿城",
                _ => regionId
            };
        }

        /// <summary>
        /// 获取阵营名称
        /// </summary>
        private string GetFactionName(string factionId)
        {
            // 模拟阵营名称映射
            return factionId switch
            {
                "villagers" => "村民联盟",
                "merchants" => "商人行会",
                "warriors" => "战士公会",
                "mages" => "法师学院",
                _ => factionId
            };
        }

        /// <summary>
        /// 获取声望等级
        /// </summary>
        private string GetReputationLevel(int reputation)
        {
            if (reputation < 0)
            {
                if (reputation <= -3000)
                    return "Hostile";
                return "Unfriendly";
            }
            
            if (reputation < 3000)
                return "Neutral";
            if (reputation < 6000)
                return "Friendly";
            if (reputation < 9000)
                return "Honored";
                
            return "Exalted";
        }

        /// <summary>
        /// 初始化测试角色
        /// </summary>
        private void InitializeTestCharacters()
        {
            // 创建测试角色1
            var testCharacter1 = new BlazorWebGame.Shared.Models.Character("测试者");
            testCharacter1.GainGold(10000);
            testCharacter1.SetCurrentRegion("start_village");
            testCharacter1.GainReputation("villagers", 3500);
            testCharacter1.GainReputation("merchants", 1500);
            _characters.TryAdd(testCharacter1.Id, testCharacter1);

            // 创建测试角色2
            var testCharacter2 = new BlazorWebGame.Shared.Models.Character("阿尔忒弥斯");
            testCharacter2.Professions.SelectBattleProfession("Archer");
            testCharacter2.GainGold(5000);
            testCharacter2.GainExperience(1200); // 应该会升级
            testCharacter2.SetCurrentRegion("forest_1");
            testCharacter2.GainReputation("villagers", 2000);
            testCharacter2.Attributes.AllocateAttribute("agility", 5);
            testCharacter2.Attributes.AllocateAttribute("stamina", 3);
            _characters.TryAdd(testCharacter2.Id, testCharacter2);

            _logger.LogInformation("初始化了2个测试角色");
        }

        #endregion
    }
}