using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Character;
using BlazorWebGame.Shared.Events;
using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlazorWebGame.Server.Services.Character
{
    /// <summary>
    /// 服务端角色管理服务 - 增强版
    /// 提供角色花名册管理、角色创建/删除/切换、属性分配等功能
    /// </summary>
    public class ServerCharacterManagementService
    {
        private readonly ILogger<ServerCharacterManagementService> _logger;
        private readonly GameEventManager _eventManager;
        private readonly IDataStorageService _dataStorage;
        private readonly ServerCharacterService _characterService;
        private readonly ServerPlayerAttributeService _playerAttributeService;
        
        // 用户角色花名册缓存
        private readonly ConcurrentDictionary<string, RosterDto> _userRosters = new();
        
        // 角色名称黑名单
        private readonly HashSet<string> _forbiddenNames = new()
        {
            "admin", "administrator", "system", "游戏管理员", "gm", "gamemaster"
        };
        
        // 角色槽位配置
        private const int DEFAULT_MAX_SLOTS = 8;
        private const int DEFAULT_UNLOCKED_SLOTS = 3;

        public ServerCharacterManagementService(
            ILogger<ServerCharacterManagementService> logger,
            GameEventManager eventManager,
            IDataStorageService dataStorage,
            ServerCharacterService characterService,
            ServerPlayerAttributeService playerAttributeService)
        {
            _logger = logger;
            _eventManager = eventManager;
            _dataStorage = dataStorage;
            _characterService = characterService;
            _playerAttributeService = playerAttributeService;
        }

        #region 角色花名册管理

        /// <summary>
        /// 获取用户的角色花名册
        /// </summary>
        public async Task<RosterDto?> GetRosterAsync(string userId)
        {
            try
            {
                // 尝试从缓存获取
                if (_userRosters.TryGetValue(userId, out var cachedRoster))
                {
                    return cachedRoster;
                }

                // 从数据库加载用户角色
                var userCharactersResult = await _dataStorage.GetUserCharactersAsync(userId);
                if (!userCharactersResult.Success)
                {
                    _logger.LogWarning($"Failed to get user characters: {userCharactersResult.Message}");
                    return CreateEmptyRoster(userId);
                }

                var userCharacters = userCharactersResult.Data ?? new List<UserCharacterStorageDto>();
                
                // 构建花名册
                var roster = new RosterDto
                {
                    UserId = userId,
                    MaxSlots = DEFAULT_MAX_SLOTS,
                    UnlockedSlots = DEFAULT_UNLOCKED_SLOTS,
                    Slots = new List<CharacterSlotDto>()
                };

                // 初始化所有槽位
                for (int i = 0; i < DEFAULT_MAX_SLOTS; i++)
                {
                    var slot = new CharacterSlotDto
                    {
                        SlotIndex = i,
                        State = i < DEFAULT_UNLOCKED_SLOTS ? "Unlocked" : "Locked"
                    };

                    if (i >= DEFAULT_UNLOCKED_SLOTS)
                    {
                        slot.UnlockCondition = GetSlotUnlockCondition(i);
                    }

                    roster.Slots.Add(slot);
                }

                // 填充角色数据
                foreach (var userChar in userCharacters.Where(uc => uc.IsActive))
                {
                    var characterDetails = await _characterService.GetCharacterDetailsAsync(userChar.CharacterId);
                    if (characterDetails != null)
                    {
                        // 找到第一个空槽位
                        var emptySlot = roster.Slots.FirstOrDefault(s => s.State == "Unlocked" && s.Character == null);
                        if (emptySlot != null)
                        {
                            emptySlot.State = "Occupied";
                            emptySlot.Character = new CharacterSummaryDto
                            {
                                Id = characterDetails.Id,
                                Name = characterDetails.Name,
                                Level = GetCharacterLevel(characterDetails),
                                ProfessionName = characterDetails.SelectedBattleProfession ?? "Warrior",
                                ProfessionIcon = GetProfessionIcon(characterDetails.SelectedBattleProfession),
                                IsOnline = false,
                                LastActiveAt = userChar.LastPlayedAt
                            };
                            emptySlot.LastPlayedAt = userChar.LastPlayedAt;

                            if (userChar.IsDefault)
                            {
                                roster.ActiveCharacterId = characterDetails.Id;
                            }
                        }
                    }
                }

                // 缓存花名册
                _userRosters[userId] = roster;

                return roster;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting roster for user {userId}");
                return CreateEmptyRoster(userId);
            }
        }

        /// <summary>
        /// 解锁角色槽位
        /// </summary>
        public async Task<ApiResponse<bool>> UnlockSlotAsync(string userId, int slotIndex)
        {
            try
            {
                if (slotIndex < 0 || slotIndex >= DEFAULT_MAX_SLOTS)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "无效的槽位索引"
                    };
                }

                var roster = await GetRosterAsync(userId);
                if (roster == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "未找到角色花名册"
                    };
                }

                var slot = roster.Slots.FirstOrDefault(s => s.SlotIndex == slotIndex);
                if (slot == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "未找到指定槽位"
                    };
                }

                if (slot.State != "Locked")
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "该槽位已解锁"
                    };
                }

                // TODO: 检查解锁条件（例如：等级、金币等）
                // 这里简化处理，直接解锁
                slot.State = "Unlocked";
                slot.UnlockCondition = null;
                roster.UnlockedSlots++;

                // 更新缓存
                _userRosters[userId] = roster;

                // 触发槽位解锁事件
                _eventManager.Raise(new GameEventArgs(GameEventType.GenericStateChanged, userId, 
                    new { Action = "SlotUnlocked", SlotIndex = slotIndex }));

                _logger.LogInformation($"User {userId} unlocked slot {slotIndex}");

                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "槽位解锁成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unlocking slot {slotIndex} for user {userId}");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "解锁槽位失败"
                };
            }
        }

        #endregion

        #region 角色创建和管理

        /// <summary>
        /// 创建新角色
        /// </summary>
        public async Task<ApiResponse<CharacterFullDto>> CreateCharacterAsync(string userId, CreateCharacterRequestDto request)
        {
            try
            {
                // 验证角色名称
                var nameValidation = ValidateCharacterName(request.Name);
                if (!nameValidation.IsValid)
                {
                    return new ApiResponse<CharacterFullDto>
                    {
                        Success = false,
                        Message = nameValidation.Reason ?? "角色名称无效"
                    };
                }

                // 检查花名册是否有空槽位
                var roster = await GetRosterAsync(userId);
                if (roster == null)
                {
                    return new ApiResponse<CharacterFullDto>
                    {
                        Success = false,
                        Message = "获取角色花名册失败"
                    };
                }

                var availableSlot = roster.Slots.FirstOrDefault(s => s.State == "Unlocked" && s.Character == null);
                if (availableSlot == null)
                {
                    return new ApiResponse<CharacterFullDto>
                    {
                        Success = false,
                        Message = "没有可用的角色槽位"
                    };
                }

                // 创建角色
                var createRequest = new CreateCharacterRequest
                {
                    Name = request.Name
                };

                var character = await _characterService.CreateCharacterAsync(createRequest, userId);
                
                // 获取角色详细信息
                var characterDetails = await _characterService.GetCharacterDetailsAsync(character.Id);
                if (characterDetails == null)
                {
                    return new ApiResponse<CharacterFullDto>
                    {
                        Success = false,
                        Message = "创建角色成功但获取详细信息失败"
                    };
                }

                // 转换为CharacterFullDto
                var characterFullDto = ConvertToCharacterFullDto(characterDetails);

                // 如果这是用户的第一个角色，设置为默认角色
                var userCharacters = await _dataStorage.GetUserCharactersAsync(userId);
                if (userCharacters.Success && userCharacters.Data?.Count == 1)
                {
                    await _dataStorage.SetDefaultCharacterAsync(userId, character.Id);
                    roster.ActiveCharacterId = character.Id;
                }

                // 更新花名册缓存
                availableSlot.State = "Occupied";
                availableSlot.Character = new CharacterSummaryDto
                {
                    Id = character.Id,
                    Name = character.Name,
                    Level = 1,
                    ProfessionName = characterDetails.SelectedBattleProfession ?? "Warrior",
                    ProfessionIcon = GetProfessionIcon(characterDetails.SelectedBattleProfession),
                    IsOnline = false,
                    LastActiveAt = DateTime.UtcNow
                };
                availableSlot.LastPlayedAt = DateTime.UtcNow;

                _userRosters[userId] = roster;

                // 触发角色创建事件
                _eventManager.Raise(new GameEventArgs(GameEventType.CharacterCreated, character.Id, 
                    new { UserId = userId, CharacterName = character.Name }));

                _logger.LogInformation($"User {userId} created character {character.Name} (ID: {character.Id})");

                return new ApiResponse<CharacterFullDto>
                {
                    Success = true,
                    Data = characterFullDto,
                    Message = "角色创建成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating character for user {userId}");
                return new ApiResponse<CharacterFullDto>
                {
                    Success = false,
                    Message = "创建角色失败"
                };
            }
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteCharacterAsync(string userId, string characterId)
        {
            try
            {
                // 验证用户拥有该角色
                var ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "无权删除该角色"
                    };
                }

                // 从数据库删除用户角色关联
                var deleteResult = await _dataStorage.DeleteUserCharacterAsync(userId, characterId);
                if (!deleteResult.Success)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = deleteResult.Message
                    };
                }

                // 更新花名册缓存
                var roster = await GetRosterAsync(userId);
                if (roster != null)
                {
                    var slot = roster.Slots.FirstOrDefault(s => s.Character?.Id == characterId);
                    if (slot != null)
                    {
                        slot.State = "Unlocked";
                        slot.Character = null;
                        slot.LastPlayedAt = null;
                    }

                    if (roster.ActiveCharacterId == characterId)
                    {
                        // 如果删除的是当前活跃角色，选择另一个角色作为活跃角色
                        var nextCharacter = roster.Slots.FirstOrDefault(s => s.Character != null && s.Character.Id != characterId);
                        roster.ActiveCharacterId = nextCharacter?.Character?.Id;
                        
                        if (roster.ActiveCharacterId != null)
                        {
                            await _dataStorage.SetDefaultCharacterAsync(userId, roster.ActiveCharacterId);
                        }
                    }

                    _userRosters[userId] = roster;
                }

                // 触发角色删除事件
                _eventManager.Raise(new GameEventArgs(GameEventType.GenericStateChanged, characterId, 
                    new { Action = "CharacterDeleted", UserId = userId }));

                _logger.LogInformation($"User {userId} deleted character {characterId}");

                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "角色删除成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting character {characterId} for user {userId}");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "删除角色失败"
                };
            }
        }

        /// <summary>
        /// 切换活跃角色
        /// </summary>
        public async Task<ApiResponse<CharacterFullDto>> SwitchCharacterAsync(string userId, string characterId)
        {
            try
            {
                // 验证用户拥有该角色
                var ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return new ApiResponse<CharacterFullDto>
                    {
                        Success = false,
                        Message = "无权切换到该角色"
                    };
                }

                // 设置为默认角色
                var setDefaultResult = await _dataStorage.SetDefaultCharacterAsync(userId, characterId);
                if (!setDefaultResult.Success)
                {
                    return new ApiResponse<CharacterFullDto>
                    {
                        Success = false,
                        Message = setDefaultResult.Message
                    };
                }

                // 更新花名册缓存
                var roster = await GetRosterAsync(userId);
                if (roster != null)
                {
                    roster.ActiveCharacterId = characterId;
                    _userRosters[userId] = roster;
                }

                // 获取角色详细信息
                var characterDetails = await _characterService.GetCharacterDetailsAsync(characterId);
                if (characterDetails == null)
                {
                    return new ApiResponse<CharacterFullDto>
                    {
                        Success = false,
                        Message = "未找到角色详细信息"
                    };
                }

                var characterFullDto = ConvertToCharacterFullDto(characterDetails);

                // 触发角色切换事件
                _eventManager.Raise(new GameEventArgs(GameEventType.ActiveCharacterChanged, characterId, 
                    new { UserId = userId }));

                _logger.LogInformation($"User {userId} switched to character {characterId}");

                return new ApiResponse<CharacterFullDto>
                {
                    Success = true,
                    Data = characterFullDto,
                    Message = "角色切换成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error switching character for user {userId} to {characterId}");
                return new ApiResponse<CharacterFullDto>
                {
                    Success = false,
                    Message = "切换角色失败"
                };
            }
        }

        /// <summary>
        /// 获取角色详细信息
        /// </summary>
        public async Task<ApiResponse<CharacterFullDto>> GetCharacterDetailsAsync(string userId, string characterId)
        {
            try
            {
                // 验证用户拥有该角色
                var ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return new ApiResponse<CharacterFullDto>
                    {
                        Success = false,
                        Message = "无权访问该角色"
                    };
                }

                var characterDetails = await _characterService.GetCharacterDetailsAsync(characterId);
                if (characterDetails == null)
                {
                    return new ApiResponse<CharacterFullDto>
                    {
                        Success = false,
                        Message = "未找到角色"
                    };
                }

                var characterFullDto = ConvertToCharacterFullDto(characterDetails);

                return new ApiResponse<CharacterFullDto>
                {
                    Success = true,
                    Data = characterFullDto,
                    Message = "获取角色详细信息成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting character details for {characterId}");
                return new ApiResponse<CharacterFullDto>
                {
                    Success = false,
                    Message = "获取角色详细信息失败"
                };
            }
        }

        #endregion

        #region 角色名称验证

        /// <summary>
        /// 验证角色名称
        /// </summary>
        public ValidateCharacterNameResult ValidateCharacterName(string name)
        {
            // 检查是否为空
            if (string.IsNullOrWhiteSpace(name))
            {
                return new ValidateCharacterNameResult
                {
                    IsValid = false,
                    Reason = "角色名称不能为空"
                };
            }

            // 检查长度
            if (name.Length < 2)
            {
                return new ValidateCharacterNameResult
                {
                    IsValid = false,
                    Reason = "角色名称至少需要2个字符"
                };
            }

            if (name.Length > 16)
            {
                return new ValidateCharacterNameResult
                {
                    IsValid = false,
                    Reason = "角色名称不能超过16个字符"
                };
            }

            // 检查是否包含非法字符
            if (!Regex.IsMatch(name, @"^[\u4e00-\u9fa5a-zA-Z0-9_]+$"))
            {
                return new ValidateCharacterNameResult
                {
                    IsValid = false,
                    Reason = "角色名称只能包含中文、英文、数字和下划线"
                };
            }

            // 检查是否在黑名单中
            if (_forbiddenNames.Contains(name.ToLower()))
            {
                return new ValidateCharacterNameResult
                {
                    IsValid = false,
                    Reason = "该名称不可用"
                };
            }

            // 检查是否包含敏感词
            if (ContainsSensitiveWords(name))
            {
                return new ValidateCharacterNameResult
                {
                    IsValid = false,
                    Reason = "角色名称包含敏感词汇"
                };
            }

            return new ValidateCharacterNameResult
            {
                IsValid = true
            };
        }

        #endregion

        #region 属性分配

        /// <summary>
        /// 分配属性点
        /// </summary>
        public async Task<ApiResponse<CharacterAttributesDto>> AllocateAttributePointsAsync(
            string userId, 
            string characterId, 
            AllocateAttributePointsRequest request)
        {
            try
            {
                // 验证用户拥有该角色
                var ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return new ApiResponse<CharacterAttributesDto>
                    {
                        Success = false,
                        Message = "无权操作该角色"
                    };
                }

                var characterDetails = await _characterService.GetCharacterDetailsAsync(characterId);
                if (characterDetails == null)
                {
                    return new ApiResponse<CharacterAttributesDto>
                    {
                        Success = false,
                        Message = "未找到角色"
                    };
                }

                // 验证属性点总数
                var totalPoints = request.Points.Values.Sum();
                if (totalPoints <= 0)
                {
                    return new ApiResponse<CharacterAttributesDto>
                    {
                        Success = false,
                        Message = "至少需要分配1个属性点"
                    };
                }

                // TODO: 检查角色是否有足够的可用属性点
                // 这需要在CharacterDetailsDto中添加AvailableAttributePoints字段

                // 应用属性点分配
                var attributes = characterDetails.BaseAttributes;
                if (attributes == null)
                {
                    return new ApiResponse<CharacterAttributesDto>
                    {
                        Success = false,
                        Message = "角色属性未初始化"
                    };
                }

                foreach (var kvp in request.Points)
                {
                    var attributeName = kvp.Key.ToLower();
                    var points = kvp.Value;

                    if (points < 0)
                    {
                        return new ApiResponse<CharacterAttributesDto>
                        {
                            Success = false,
                            Message = $"属性点数不能为负数: {attributeName}"
                        };
                    }

                    switch (attributeName)
                    {
                        case "strength":
                        case "力量":
                            attributes.Strength += points;
                            break;
                        case "agility":
                        case "敏捷":
                            attributes.Agility += points;
                            break;
                        case "intellect":
                        case "智力":
                            attributes.Intellect += points;
                            break;
                        case "spirit":
                        case "精神":
                            attributes.Spirit += points;
                            break;
                        case "stamina":
                        case "耐力":
                            attributes.Stamina += points;
                            break;
                        default:
                            return new ApiResponse<CharacterAttributesDto>
                            {
                                Success = false,
                                Message = $"未知的属性类型: {attributeName}"
                            };
                    }
                }

                // 更新角色属性
                _playerAttributeService.UpdateBaseAttributes(characterDetails);
                characterDetails.MaxHealth = _playerAttributeService.GetTotalMaxHealth(characterDetails);
                characterDetails.LastUpdated = DateTime.UtcNow;

                // 构建返回的属性DTO
                var totalAttributes = _playerAttributeService.GetTotalAttributes(characterDetails);
                var attributesDto = new CharacterAttributesDto
                {
                    Strength = totalAttributes.Strength,
                    Agility = totalAttributes.Agility,
                    Intellect = totalAttributes.Intellect,
                    Spirit = totalAttributes.Spirit,
                    Stamina = totalAttributes.Stamina,
                    AvailablePoints = 0, // TODO: 计算剩余可用点数
                    AttackPower = _playerAttributeService.GetTotalAttackPower(characterDetails),
                    Armor = totalAttributes.Strength * 2.0, // 简化计算
                    CriticalChance = totalAttributes.Agility * 0.1,
                    AttackSpeed = 1.0 + (totalAttributes.Agility * 0.01)
                };

                // 触发属性变更事件
                _eventManager.Raise(new GameEventArgs(GameEventType.CharacterStatChanged, characterId, 
                    new { UserId = userId, AttributesAllocated = request.Points }));

                _logger.LogInformation($"User {userId} allocated attribute points for character {characterId}");

                return new ApiResponse<CharacterAttributesDto>
                {
                    Success = true,
                    Data = attributesDto,
                    Message = "属性点分配成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error allocating attribute points for character {characterId}");
                return new ApiResponse<CharacterAttributesDto>
                {
                    Success = false,
                    Message = "分配属性点失败"
                };
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建空的角色花名册
        /// </summary>
        private RosterDto CreateEmptyRoster(string userId)
        {
            var roster = new RosterDto
            {
                UserId = userId,
                MaxSlots = DEFAULT_MAX_SLOTS,
                UnlockedSlots = DEFAULT_UNLOCKED_SLOTS,
                Slots = new List<CharacterSlotDto>()
            };

            for (int i = 0; i < DEFAULT_MAX_SLOTS; i++)
            {
                var slot = new CharacterSlotDto
                {
                    SlotIndex = i,
                    State = i < DEFAULT_UNLOCKED_SLOTS ? "Unlocked" : "Locked"
                };

                if (i >= DEFAULT_UNLOCKED_SLOTS)
                {
                    slot.UnlockCondition = GetSlotUnlockCondition(i);
                }

                roster.Slots.Add(slot);
            }

            return roster;
        }

        /// <summary>
        /// 获取槽位解锁条件描述
        /// </summary>
        private string GetSlotUnlockCondition(int slotIndex)
        {
            return slotIndex switch
            {
                3 => "角色等级达到10级",
                4 => "角色等级达到20级",
                5 => "角色等级达到30级",
                6 => "完成特定任务或消耗1000金币",
                7 => "完成特定任务或消耗2000金币",
                _ => "暂未开放"
            };
        }

        /// <summary>
        /// 获取角色等级
        /// </summary>
        private int GetCharacterLevel(CharacterDetailsDto character)
        {
            // 使用战斗职业等级作为主等级
            if (!string.IsNullOrEmpty(character.SelectedBattleProfession) &&
                character.BattleProfessionXP.TryGetValue(character.SelectedBattleProfession, out var xp))
            {
                return Math.Max(1, xp / 1000 + 1);
            }
            return 1;
        }

        /// <summary>
        /// 获取职业图标
        /// </summary>
        private string GetProfessionIcon(string? profession)
        {
            return profession?.ToLower() switch
            {
                "warrior" => "⚔️",
                "mage" => "🔮",
                "archer" => "🏹",
                "paladin" => "🛡️",
                _ => "⚔️"
            };
        }

        /// <summary>
        /// 将CharacterDetailsDto转换为CharacterFullDto
        /// </summary>
        private CharacterFullDto ConvertToCharacterFullDto(CharacterDetailsDto details)
        {
            var level = GetCharacterLevel(details);
            var totalAttributes = _playerAttributeService.GetTotalAttributes(details);

            return new CharacterFullDto
            {
                Id = details.Id,
                Name = details.Name,
                Level = level,
                Experience = 0, // TODO: 从XP计算
                ExperienceToNextLevel = 1000, // TODO: 根据等级计算
                Gold = details.Gold,
                Vitals = new CharacterVitalsDto
                {
                    Health = details.Health,
                    MaxHealth = details.MaxHealth,
                    Mana = 0, // TODO: 添加法力值支持
                    MaxMana = 100,
                    HealthRegen = 0.1,
                    ManaRegen = 0.05
                },
                Attributes = new CharacterAttributesDto
                {
                    Strength = totalAttributes.Strength,
                    Agility = totalAttributes.Agility,
                    Intellect = totalAttributes.Intellect,
                    Spirit = totalAttributes.Spirit,
                    Stamina = totalAttributes.Stamina,
                    AvailablePoints = 0,
                    AttackPower = _playerAttributeService.GetTotalAttackPower(details),
                    Armor = totalAttributes.Strength * 2.0,
                    CriticalChance = totalAttributes.Agility * 0.1,
                    AttackSpeed = 1.0 + (totalAttributes.Agility * 0.01)
                },
                Profession = new ProfessionInfoDto
                {
                    Id = details.SelectedBattleProfession ?? "Warrior",
                    Name = details.SelectedBattleProfession ?? "战士",
                    Icon = GetProfessionIcon(details.SelectedBattleProfession),
                    Level = level,
                    Experience = 0,
                    ExperienceToNextLevel = 1000
                },
                Equipment = new Shared.DTOs.Character.EquipmentDto
                {
                    TotalGearScore = 0
                },
                Skills = new SkillSystemDto
                {
                    ActiveSkills = new List<SkillSlotDto>(),
                    PassiveSkills = new List<SkillSlotDto>(),
                    AvailableSkillPoints = 0
                },
                Reputations = new Dictionary<string, Shared.DTOs.Character.ReputationDto>(),
                Statistics = new CharacterStatisticsDto(),
                CreatedAt = DateTime.UtcNow,
                LastActiveAt = details.LastUpdated
            };
        }

        /// <summary>
        /// 检查是否包含敏感词
        /// </summary>
        private bool ContainsSensitiveWords(string name)
        {
            // 简化实现，实际应该使用更完善的敏感词过滤系统
            var sensitiveWords = new[] { "fuck", "shit", "damn", "操", "妈", "傻逼" };
            return sensitiveWords.Any(word => name.ToLower().Contains(word));
        }

        #endregion
    }
}
