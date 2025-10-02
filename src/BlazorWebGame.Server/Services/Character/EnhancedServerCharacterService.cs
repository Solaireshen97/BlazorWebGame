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
    /// ��ǿ�����˽�ɫ�������� - ʹ������ģ��
    /// </summary>
    public class EnhancedServerCharacterService
    {
        // ʹ���ڴ滺��+���ڲ���
        private readonly ConcurrentDictionary<string, Tuple<BlazorWebGame.Shared.Models.Character, DateTime>> _characterCache = new();

        // �������ʱ�䣨����30���Ӳ���Ծ�ʹ��ڴ����Ƴ���
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
            
            // ��ʼ�����Խ�ɫ
            InitializeTestCharacters();

            // ���Ӷ�ʱ���񣬶����������ڵĽ�ɫ����
            StartCacheCleanupTask();
        }

        // ��ȡ��ɫ - ������ڻ�����������ݿ����
        private async Task<BlazorWebGame.Shared.Models.Character?> GetOrLoadCharacterAsync(string characterId)
        {
            // ��黺��
            if (_characterCache.TryGetValue(characterId, out var cachedData))
            {
                // ����������ʱ��
                _characterCache[characterId] = new Tuple<BlazorWebGame.Shared.Models.Character, DateTime>(
                    cachedData.Item1, DateTime.UtcNow);
                return cachedData.Item1;
            }

            // �����ݿ����
            var response = await _dataStorage.GetCharacterByIdAsync(characterId);
            if (!response.IsSuccess || response.Data == null)
            {
                return null;
            }

            // ת��Ϊ����ģ��
            var character = ConvertToDomainModel(response.Data);

            // ���ӵ�����
            _characterCache[characterId] = new Tuple<BlazorWebGame.Shared.Models.Character, DateTime>(
                character, DateTime.UtcNow);

            return character;
        }

        // �����ɫ�����ݿ�
        private async Task SaveCharacterToStorageAsync(BlazorWebGame.Shared.Models.Character character)
        {
            // ʹ��CharacterMapperת��ΪDTO
            var dto = BlazorWebGame.Shared.Mappers.CharacterMapper.ToDto(character);
            // �������ݴ洢���񱣴��ɫ
            await _dataStorage.SaveCharacterAsync(dto);
        }


        // ������������
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
                        _logger.LogError(ex, "������ɫ����ʱ����");
                    }

                    // ÿ5����ִ��һ������
                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            });
        }

        // �������ڻ���
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
                _logger.LogInformation("�Ѵ��ڴ����Ƴ���ʱ��δ��Ľ�ɫ: {CharacterId}", key);
            }
        }

        // ��ʽ���ڴ����Ƴ���ɫ
        public void RemoveCharacterFromCache(string characterId)
        {
            _characterCache.TryRemove(characterId, out _);
        }

        // �û�����ʱ���Ƴ����û������н�ɫ����
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
        /// ��ȡ�û��Ľ�ɫ������
        /// </summary>
        /// <summary>
        /// ��ȡ�û��Ľ�ɫ������
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

                // Ԥ�������н�ɫ���ݵ��ڴ���
                foreach (var userChar in activeUserCharacters)
                {
                    // ʹ��GetOrLoadCharacterAsyncȷ����ɫ���ݱ����ص��ڴ���
                    var character = await GetOrLoadCharacterAsync(userChar.CharacterId);
                    if (character != null)
                    {
                        // ȷ����ɫҲ���ӵ�_characters�ֵ���(GetOrLoadCharacterAsync�Ѿ����ӵ�_characterCache)
                        _characters.TryAdd(character.Id, character);
                    }
                }

                // ����ѽ�����λ
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

                // ���ʣ���λ
                for (int i = roster.UnlockedSlots; i < roster.MaxSlots; i++)
                {
                    roster.Slots.Add(new CharacterSlotDto
                    {
                        SlotIndex = i,
                        State = "Locked",
                        UnlockCondition = i < 3 ? "������ֽ̳�" : $"��ɫ�ﵽ{i * 10}��",
                        Character = null
                    });
                }

                return roster;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"��ȡ�û�������ʧ�ܣ�{userId}");
                return CreateEmptyRoster(userId);
            }
        }

        /// <summary>
        /// ������ɫ��λ
        /// </summary>
        public async Task<ApiResponse<CharacterSlotDto>> UnlockSlotAsync(string userId, int slotIndex)
        {
            try
            {
                // ��ȡ�û���ǰ������
                var roster = await GetUserRosterAsync(userId);
                
                // ����λ�Ƿ��ѽ���
                if (slotIndex < 0 || slotIndex >= roster.MaxSlots)
                {
                    return ApiResponse<CharacterSlotDto>.Failure("��Ч�Ĳ�λ����");
                }
                
                if (slotIndex < roster.UnlockedSlots)
                {
                    return ApiResponse<CharacterSlotDto>.Failure("�ò�λ�ѽ���");
                }

                // ����������
                bool canUnlock = false;
                
                // ģ������������
                if (slotIndex < 3)
                {
                    // ǰ������λֻ��������ֽ̳�
                    canUnlock = true;
                }
                else
                {
                    // ������λ��Ҫ��ɫ�ﵽһ���ȼ�
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
                    return ApiResponse<CharacterSlotDto>.Failure($"δ�������������{roster.Slots[slotIndex].UnlockCondition}");
                }

                // �������ݿ��еĲ�λ״̬
                await _dataStorage.UnlockCharacterSlotAsync(userId, slotIndex);

                // �����µĲ�λ��Ϣ
                var newSlot = new CharacterSlotDto
                {
                    SlotIndex = slotIndex,
                    State = "Unlocked",
                    Character = null
                };

                _logger.LogInformation($"�û� {userId} �����˲�λ {slotIndex}");

                return ApiResponse<CharacterSlotDto>.Success(newSlot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"������λʧ�ܣ��û� {userId}, ��λ {slotIndex}");
                return ApiResponse<CharacterSlotDto>.Failure("������λʱ��������");
            }
        }

        /// <summary>
        /// ������ɫ
        /// </summary>
        public async Task<ApiResponse<CharacterFullDto>> CreateCharacterAsync(string userId, CreateCharacterRequestDto request)
        {
            try
            {
                // ��֤����
                var nameValidation = ValidateCharacterName(request.Name);
                if (!nameValidation.IsValid)
                {
                    return ApiResponse<CharacterFullDto>.Failure(nameValidation.Reason!);
                }

                // ��֤��λ�Ƿ����
                var roster = await GetUserRosterAsync(userId);
                if (request.SlotIndex < 0 || request.SlotIndex >= roster.UnlockedSlots)
                {
                    return ApiResponse<CharacterFullDto>.Failure("��Ч�Ĳ�λ����");
                }

                if (roster.Slots[request.SlotIndex].State == "Occupied")
                {
                    return ApiResponse<CharacterFullDto>.Failure("�ò�λ�ѱ�ռ��");
                }

                // �����½�ɫ
                var character = new BlazorWebGame.Shared.Models.Character(request.Name);

                // ����ְҵ
                var profession = !string.IsNullOrEmpty(request.StartingProfessionId)
                    ? request.StartingProfessionId
                    : "Warrior";

                character.Professions.SelectBattleProfession(profession);

                // ���浽�ڴ�
                _characters.TryAdd(character.Id, character);
                var characterDto = ConvertToStorageDto(character);

                // �����ɫ�����ݿ�
                await _dataStorage.SaveCharacterAsync(characterDto);

                // �����û�-��ɫ����
                var isFirstCharacter = roster.Slots.All(s => s.Character == null);
                var relationResult = await _dataStorage.CreateUserCharacterAsync(
                    userId, character.Id, request.Name, isFirstCharacter, request.SlotIndex);

                if (!relationResult.IsSuccess)
                {
                    _characters.TryRemove(character.Id, out _);
                    return ApiResponse<CharacterFullDto>.Failure($"������ɫ����ʧ�ܣ�{relationResult.Message}");
                }

                // ������ɫ�����¼�
                _eventManager.Raise(new GameEventArgs(GameEventType.CharacterCreated, character.Id));

                _logger.LogInformation($"�û� {userId} �����˽�ɫ {request.Name}��ID��{character.Id}");

                // ����ɫת��ΪDTO
                var characterFullDto = ConvertToFullDto(character);
                return ApiResponse<CharacterFullDto>.Success(characterFullDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"������ɫʧ�ܣ�{request.Name}");
                return ApiResponse<CharacterFullDto>.Failure("������ɫʱ��������");
            }
        }

        /// <summary>
        /// ��֤��ɫ����
        /// </summary>
        public ValidateCharacterNameResult ValidateCharacterName(string name)
        {
            // ������Ƴ���
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 20)
            {
                return new ValidateCharacterNameResult 
                { 
                    IsValid = false, 
                    Reason = "���Ƴ���ӦΪ2-20���ַ�" 
                };
            }

            // ��������ַ�
            if (name.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
            {
                return new ValidateCharacterNameResult 
                { 
                    IsValid = false, 
                    Reason = "����ֻ�ܰ�����ĸ�����ֺͿո�" 
                };
            }

            // ����ظ�����
            if (_characters.Values.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                return new ValidateCharacterNameResult 
                { 
                    IsValid = false, 
                    Reason = "�������ѱ�ʹ��" 
                };
            }

            return new ValidateCharacterNameResult { IsValid = true };
        }

        /// <summary>
        /// ��ȡ��ɫ����
        /// </summary>
        public async Task<ApiResponse<CharacterFullDto>> GetCharacterDetails(string characterId)
        {
            try
            {
                var character = await GetOrLoadCharacterAsync(characterId);
                if (character == null)
                {
                    return ApiResponse<CharacterFullDto>.Failure("��ɫ������");
                }

                return ApiResponse<CharacterFullDto>.Success(ConvertToFullDto(character));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ��ɫ����ʧ�� {CharacterId}", characterId);
                return ApiResponse<CharacterFullDto>.Failure($"��ȡ��ɫ����ʧ�ܣ�{ex.Message}");
            }
        }

        /// <summary>
        /// �л���ɫ
        /// </summary>
        public async Task<ApiResponse<CharacterFullDto>> SwitchCharacterAsync(string userId, string characterId)
        {
            try
            {
                // ��֤�û��Ƿ�ӵ�иý�ɫ
                bool ownsCharacter = await _dataStorage.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return ApiResponse<CharacterFullDto>.Failure("�û���ӵ�иý�ɫ");
                }

                // ��ȡ��ɫ
                if (!_characters.TryGetValue(characterId, out var character))
                {
                    return ApiResponse<CharacterFullDto>.Failure("��ɫ������");
                }

                // ���ý�ɫΪ��Ծ״̬
                character.GoOnline();
                
                // ����Ĭ�Ͻ�ɫ
                await _dataStorage.SetDefaultCharacterAsync(userId, characterId);

                //// �������߽���
                //var offlineProgress = await CalculateOfflineProgressAsync(character);
                //if (offlineProgress != null)
                //{
                //    // Ӧ�����߽���
                //    ApplyOfflineProgress(character, offlineProgress);

                //    // �������߽����¼�
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
                _logger.LogError(ex, $"�л���ɫʧ�ܣ�{characterId}");
                return ApiResponse<CharacterFullDto>.Failure("�л���ɫʱ��������");
            }
        }

        /// <summary>
        /// ����û��Ƿ�ӵ�н�ɫ
        /// </summary>
        public async Task<bool> UserOwnsCharacterAsync(string userId, string characterId)
        {
            try
            {
                return await _dataStorage.UserOwnsCharacterAsync(userId, characterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "����û���ɫ����Ȩʧ�� {UserId} -> {CharacterId}", userId, characterId);
                return false;
            }
        }

        /// <summary>
        /// ɾ����ɫ
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteCharacterAsync(string userId, string characterId)
        {
            try
            {
                // ��֤�û��Ƿ�ӵ�иý�ɫ
                bool ownsCharacter = await _dataStorage.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return ApiResponse<bool>.Failure("�û���ӵ�иý�ɫ");
                }

                // ��ȡ�û����н�ɫ
                var userCharactersResult = await _dataStorage.GetUserCharactersAsync(userId);
                if (!userCharactersResult.IsSuccess || userCharactersResult.Data == null)
                {
                    return ApiResponse<bool>.Failure("��ȡ�û���ɫʧ��");
                }

                // ����Ƿ���Ψһ��ɫ
                if (userCharactersResult.Data.Count(uc => uc.IsActive) == 1)
                {
                    return ApiResponse<bool>.Failure("�޷�ɾ��Ψһ��ɫ");
                }

                // �����ݿ���ɾ����ɫ����
                var result = await _dataStorage.DeleteUserCharacterAsync(userId, characterId);
                if (!result.IsSuccess)
                {
                    return ApiResponse<bool>.Failure($"ɾ����ɫʧ�ܣ�{result.Message}");
                }

                // ���ڴ���ɾ����ɫ
                _characters.TryRemove(characterId, out var character);

                // ������ɫɾ���¼�
                _eventManager.Raise(new GameEventArgs(GameEventType.CharacterDeleted, characterId));

                _logger.LogInformation($"�û� {userId} ɾ���˽�ɫ {characterId}");
                return ApiResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ɾ����ɫʧ�ܣ�{characterId}");
                return ApiResponse<bool>.Failure("ɾ����ɫʱ��������");
            }
        }

        /// <summary>
        /// �������Ե�
        /// </summary>
        public async Task<ApiResponse<CharacterAttributesDto>> AllocateAttributePointsAsync(string characterId, AllocateAttributePointsRequest request)
        {
            try
            {
                var character = await GetOrLoadCharacterAsync(characterId);
                if (character == null)
                {
                    return ApiResponse<CharacterAttributesDto>.Failure("��ɫ������");
                }

                // ��֤�ܵ����Ƿ񳬹����õ���
                var totalPoints = request.Points.Values.Sum();
                if (totalPoints > character.Attributes.AttributePoints)
                {
                    return ApiResponse<CharacterAttributesDto>.Failure("��������Ե㳬�����õ���");
                }

                // �������Ե�
                foreach (var kvp in request.Points)
                {
                    if (kvp.Value <= 0) continue;

                    bool success = character.Attributes.AllocateAttribute(kvp.Key, kvp.Value);
                    if (!success)
                    {
                        return ApiResponse<CharacterAttributesDto>.Failure($"�������Ե�ʧ�ܣ�{kvp.Key}");
                    }
                }

                // ��������ֵ�ͷ���ֵ����
                character.Vitals.RecalculateMaxValues(character.Attributes);

                // ������º�Ľ�ɫ���ݵ����ݿ�
                await SaveCharacterToStorageAsync(character);

                // ת��ΪDTO
                var attributesDto = new CharacterAttributesDto
                {
                    Strength = character.Attributes.Strength,
                    Agility = character.Attributes.Agility,
                    Intellect = character.Attributes.Intellect,
                    Spirit = character.Attributes.Spirit,
                    Stamina = character.Attributes.Stamina,
                    AvailablePoints = character.Attributes.AttributePoints,

                    // ��������
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
                _logger.LogError(ex, "�������Ե�ʧ�� {CharacterId}", characterId);
                return ApiResponse<CharacterAttributesDto>.Failure($"�������Ե�ʧ�ܣ�{ex.Message}");
            }
        }

        /// <summary>
        /// �������Ե�
        /// </summary>
        public async Task<ApiResponse<CharacterAttributesDto>> ResetAttributesAsync(string characterId)
        {
            try
            {
                var character = await GetOrLoadCharacterAsync(characterId);
                if (character == null)
                {
                    return ApiResponse<CharacterAttributesDto>.Failure("��ɫ������");
                }

                // ��������
                character.Attributes.ResetAttributes();

                // ��������ֵ�ͷ���ֵ����
                character.Vitals.RecalculateMaxValues(character.Attributes);

                // ������º�Ľ�ɫ���ݵ����ݿ�
                await SaveCharacterToStorageAsync(character);

                // ת��ΪDTO
                var attributesDto = new CharacterAttributesDto
                {
                    Strength = character.Attributes.Strength,
                    Agility = character.Attributes.Agility,
                    Intellect = character.Attributes.Intellect,
                    Spirit = character.Attributes.Spirit,
                    Stamina = character.Attributes.Stamina,
                    AvailablePoints = character.Attributes.AttributePoints,

                    // ��������
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
                _logger.LogError(ex, "�������Ե�ʧ�� {CharacterId}", characterId);
                return ApiResponse<CharacterAttributesDto>.Failure($"�������Ե�ʧ�ܣ�{ex.Message}");
            }
        }

        /// <summary>
        /// ��ȡ���߽���
        /// </summary>
        public async Task<ApiResponse<OfflineProgressDto>> GetOfflineProgressAsync(string characterId)
        {
            if (!_characters.TryGetValue(characterId, out var character))
            {
                return ApiResponse<OfflineProgressDto>.Failure("��ɫ������");
            }

            var offlineProgress = await CalculateOfflineProgressAsync(character);
            if (offlineProgress == null)
            {
                return ApiResponse<OfflineProgressDto>.Failure("û�����߽���");
            }

            return ApiResponse<OfflineProgressDto>.Success(offlineProgress);
        }

        /// <summary>
        /// ��ȡCharacter����ģ�ͣ���ڽ�ս��ͳ����Ҫֱ�Ӳ�������ģ�͵ĳ����
        /// </summary>
        public async Task<BlazorWebGame.Shared.Models.Character?> GetCharacterDomainModelAsync(string characterId)
        {
            return await GetOrLoadCharacterAsync(characterId);
        }

        /// <summary>
        /// ����Character����ģ�ͣ���ڽ�ս����󱣴���ɫ״̬��
        /// </summary>
        public async Task<bool> SaveCharacterDomainModelAsync(BlazorWebGame.Shared.Models.Character character)
        {
            try
            {
                if (character == null)
                    return false;

                // ���»������Ľ�ɫ����
                _characterCache[character.Id] = new Tuple<BlazorWebGame.Shared.Models.Character, DateTime>(
                    character, DateTime.UtcNow);
                
                // �����浽�洢
                await SaveCharacterToStorageAsync(character);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�����ɫ����ģ��ʧ�� {CharacterId}", character?.Id);
                return false;
            }
        }

        #region ˽�и�������

        /// <summary>
        /// ���㹥����
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
        /// ���㷨��ǿ��
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
        /// ���㱩����
        /// </summary>
        private double CalculateCritChance(BlazorWebGame.Shared.Models.Character character)
        {
            double baseChance = 5.0; // ����������5%
            return baseChance + character.Attributes.Agility * 0.1;
        }

        /// <summary>
        /// ���㱩���˺�
        /// </summary>
        private double CalculateCritDamage(BlazorWebGame.Shared.Models.Character character)
        {
            double baseDamage = 150.0; // ���������˺�150%
            return baseDamage + character.Attributes.Strength * 0.2;
        }

        /// <summary>
        /// ���㻤��ֵ
        /// </summary>
        private double CalculateArmor(BlazorWebGame.Shared.Models.Character character)
        {
            return character.Attributes.Stamina * 1.5 + character.Level * 2;
        }

        /// <summary>
        /// ����ħ������
        /// </summary>
        private double CalculateMagicResistance(BlazorWebGame.Shared.Models.Character character)
        {
            return character.Attributes.Spirit * 1.5 + character.Level;
        }

        /// <summary>
        /// �������߽���
        /// </summary>
        private async Task<OfflineProgressDto?> CalculateOfflineProgressAsync(BlazorWebGame.Shared.Models.Character character)
        {
            return null;
        }

        /// <summary>
        /// Ӧ�����߽���
        /// </summary>
        private void ApplyOfflineProgress(BlazorWebGame.Shared.Models.Character character, OfflineProgressDto progress)
        {
            //// Ӧ�þ���ֵ
            //if (progress.ExperienceGained > 0)
            //{
            //    character.GainExperience(progress.ExperienceGained);
            //}

            //// Ӧ�ý��
            //if (progress.GoldGained > 0)
            //{
            //    character.GainGold(progress.GoldGained);
            //}

            //// Ӧ����Դ
            //foreach (var resource in progress.ResourcesGained)
            //{
            //    // ����Ӧ��������Դ����ɫ����
            //    character.Inventory.AddItem(resource.Key, resource.Value);
            //}

            //// Ӧ����Ʒ
            //foreach (var item in progress.LootedItems)
            //{
            //    character.Inventory.AddItem(item);
            //}

            //// ������߼�¼
            //character.LastOfflineRecord = null;

            //// ���½�ɫ��Ծʱ��
            //character.UpdateActivity();
        }

        /// <summary>
        /// �����ջ�����
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

            // ����һ�������Ĳ�λ
            roster.Slots.Add(new CharacterSlotDto
            {
                SlotIndex = 0,
                State = "Unlocked",
                Character = null,
                UnlockCondition = null
            });

            // ���������Ĳ�λ
            for (int i = 1; i < roster.MaxSlots; i++)
            {
                roster.Slots.Add(new CharacterSlotDto
                {
                    SlotIndex = i,
                    State = "Locked",
                    Character = null,
                    UnlockCondition = i < 3 ? "������ֽ̳�" : $"��ɫ�ﵽ{i * 10}��"
                });
            }

            return roster;
        }

        // ���洢DTOת��Ϊ����ģ��
        private BlazorWebGame.Shared.Models.Character ConvertToDomainModel(CharacterStorageDto dto)
        {
            // ʹ��CharacterMapper��ToCharacter����
            return BlazorWebGame.Shared.Mappers.CharacterMapper.ToCharacter(dto);
        }

        // ������ģ��ת��Ϊ�洢DTO
        private CharacterStorageDto ConvertToStorageDto(BlazorWebGame.Shared.Models.Character character)
        {
            // ʹ��CharacterMapper��ToDto����
            return BlazorWebGame.Shared.Mappers.CharacterMapper.ToDto(character);
        }

        /// <summary>
        /// ������ģ��ת��ΪDTO
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
                
                // ����ֵ
                Vitals = new CharacterVitalsDto
                {
                    Health = character.Vitals.Health,
                    MaxHealth = character.Vitals.MaxHealth,
                    Mana = character.Vitals.Mana,
                    MaxMana = character.Vitals.MaxMana,
                    HealthRegen = character.Attributes.Spirit * 0.1,
                    ManaRegen = character.Attributes.Spirit * 0.2
                },
                
                // ����
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
                
                // ְҵ��Ϣ
                Profession = new ProfessionInfoDto
                {
                    Id = character.Professions.SelectedBattleProfession,
                    Name = character.Professions.SelectedBattleProfession,
                    Description = GetProfessionDescription(character.Professions.SelectedBattleProfession),
                    Icon = $"images/professions/{character.Professions.SelectedBattleProfession.ToLower()}.png",
                    Level = character.Professions.GetProfessionLevel("Battle", character.Professions.SelectedBattleProfession),
                    Experience = 0, // ��Ҫ��character.Professions.BattleProfessions��ȡ
                    ExperienceToNextLevel = 1000, // ͬ��
                    Specializations = new List<string>(),
                    ActiveSpecialization = null
                },
                
                // װ��
                Equipment = new EquipmentDto
                {
                    TotalGearScore = 0,
                    ActiveSetBonuses = new List<string>()
                },
                
                // ����
                Skills = new SkillSystemDto
                {
                    ActiveSkills = new List<SkillSlotDto>(),
                    PassiveSkills = new List<SkillSlotDto>(),
                    AvailableSkillPoints = 0
                },
                
                // ��ǰ����
                CurrentRegionId = character.CurrentRegionId,
                CurrentRegionName = GetRegionName(character.CurrentRegionId),
                
                // ����
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
                
                // ͳ��
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
        /// ��ȡְҵ����
        /// </summary>
        private string GetProfessionDescription(string professionName)
        {
            return professionName switch
            {
                "Warrior" => "���͵�սʿ���ó���ս��������ͳ����˺���",
                "Mage" => "ǿ��ķ�ʦ������Ԫ���������ó�Զ��ħ�������",
                "Archer" => "���ݵĹ����֣��ó�Զ��������������ܡ�",
                "Paladin" => "��ʥ����ʿ������������������������",
                _ => "δְ֪ҵ"
            };
        }

        /// <summary>
        /// ��ȡ��������
        /// </summary>
        private string GetRegionName(string? regionId)
        {
            if (string.IsNullOrEmpty(regionId))
                return "��";
                
            // ģ����������ӳ��
            return regionId switch
            {
                "start_village" => "���ִ�",
                "forest_1" => "�İ�ɭ��",
                "mine_1" => "��ʯ��",
                "city_1" => "��¹��",
                _ => regionId
            };
        }

        /// <summary>
        /// ��ȡ��Ӫ����
        /// </summary>
        private string GetFactionName(string factionId)
        {
            // ģ����Ӫ����ӳ��
            return factionId switch
            {
                "villagers" => "��������",
                "merchants" => "�����л�",
                "warriors" => "սʿ����",
                "mages" => "��ʦѧԺ",
                _ => factionId
            };
        }

        /// <summary>
        /// ��ȡ�����ȼ�
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
        /// ��ʼ�����Խ�ɫ
        /// </summary>
        private void InitializeTestCharacters()
        {
            // �������Խ�ɫ1
            var testCharacter1 = new BlazorWebGame.Shared.Models.Character("������");
            testCharacter1.GainGold(10000);
            testCharacter1.SetCurrentRegion("start_village");
            testCharacter1.GainReputation("villagers", 3500);
            testCharacter1.GainReputation("merchants", 1500);
            _characters.TryAdd(testCharacter1.Id, testCharacter1);

            // �������Խ�ɫ2
            var testCharacter2 = new BlazorWebGame.Shared.Models.Character("����߯��˹");
            testCharacter2.Professions.SelectBattleProfession("Archer");
            testCharacter2.GainGold(5000);
            testCharacter2.GainExperience(1200); // Ӧ�û�����
            testCharacter2.SetCurrentRegion("forest_1");
            testCharacter2.GainReputation("villagers", 2000);
            testCharacter2.Attributes.AllocateAttribute("agility", 5);
            testCharacter2.Attributes.AllocateAttribute("stamina", 3);
            _characters.TryAdd(testCharacter2.Id, testCharacter2);

            _logger.LogInformation("��ʼ����2�����Խ�ɫ");
        }

        #endregion
    }
}