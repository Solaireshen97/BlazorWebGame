using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorIdleGame.Client.Services.Core;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Character;
using Microsoft.Extensions.Logging;

namespace BlazorIdleGame.Client.Services.Character
{
    public class EnhancedCharacterService : IEnhancedCharacterService
    {
        private readonly IGameCommunicationService _communication;
        private readonly ILogger<EnhancedCharacterService> _logger;

        private RosterDto? _currentRoster;
        private CharacterFullDto? _activeCharacter;
        private readonly Dictionary<string, CharacterFullDto> _characterCache = new();

        public RosterDto? CurrentRoster => _currentRoster;
        public CharacterFullDto? ActiveCharacter => _activeCharacter;

        public event EventHandler<RosterDto>? RosterUpdated;
        public event EventHandler<CharacterFullDto>? ActiveCharacterUpdated;
        public event EventHandler<OfflineProgressDto>? OfflineProgressReceived;
        public event EventHandler<CharacterSlotDto>? SlotUnlocked;

        public EnhancedCharacterService(
            IGameCommunicationService communication,
            ILogger<EnhancedCharacterService> logger)
        {
            _communication = communication;
            _logger = logger;
        }

        public async Task<RosterDto?> GetRosterAsync()
        {
            try
            {
                var response = await _communication.GetAsync<ApiResponse<RosterDto>>(
                    "api/character/roster");

                if (response?.Success == true && response.Data != null)
                {
                    _currentRoster = response.Data;
                    RosterUpdated?.Invoke(this, response.Data);

                    // 自动加载活跃角色
                    if (!string.IsNullOrEmpty(response.Data.ActiveCharacterId))
                    {
                        await GetCharacterDetailsAsync(response.Data.ActiveCharacterId);
                    }

                    return response.Data;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色花名册失败");
                return null;
            }
        }

        public async Task<bool> UnlockSlotAsync(int slotIndex)
        {
            try
            {
                var response = await _communication.PostAsync<object, ApiResponse<CharacterSlotDto>>(
                    $"api/character/roster/unlock/{slotIndex}", new { });

                if (response?.Success == true && response.Data != null)
                {
                    _logger.LogInformation("成功解锁槽位 {SlotIndex}", slotIndex);
                    SlotUnlocked?.Invoke(this, response.Data);

                    // 刷新花名册
                    await GetRosterAsync();
                    return true;
                }

                _logger.LogWarning("解锁槽位失败: {Message}", response?.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解锁槽位失败");
                return false;
            }
        }

        public async Task<CharacterFullDto?> CreateCharacterAsync(CreateCharacterRequestDto request)
        {
            try
            {
                // 先验证名称
                var validation = await ValidateNameAsync(request.Name);
                if (!validation.IsValid)
                {
                    _logger.LogWarning("角色名称无效: {Reason}", validation.Reason);
                    return null;
                }

                var response = await _communication.PostAsync<CreateCharacterRequestDto, ApiResponse<CharacterFullDto>>(
                    "api/character/create", request);

                if (response?.Success == true && response.Data != null)
                {
                    _logger.LogInformation("成功创建角色: {Name}", request.Name);

                    // 缓存角色信息
                    _characterCache[response.Data.Id] = response.Data;

                    // 如果这是第一个角色，自动设为活跃角色
                    if (_currentRoster?.Slots.Count(s => s.Character != null) == 1)
                    {
                        _activeCharacter = response.Data;
                        ActiveCharacterUpdated?.Invoke(this, response.Data);
                    }

                    // 刷新花名册
                    await GetRosterAsync();

                    return response.Data;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建角色失败");
                return null;
            }
        }

        public async Task<ValidateCharacterNameResult> ValidateNameAsync(string name)
        {
            try
            {
                var request = new ValidateCharacterNameRequest { Name = name };
                var response = await _communication.PostAsync<ValidateCharacterNameRequest, ApiResponse<ValidateCharacterNameResult>>(
                    "api/character/validate-name", request);

                if (response?.Success == true && response.Data != null)
                {
                    return response.Data;
                }

                return new ValidateCharacterNameResult
                {
                    IsValid = false,
                    Reason = "验证失败"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证角色名称失败");
                return new ValidateCharacterNameResult
                {
                    IsValid = false,
                    Reason = ex.Message
                };
            }
        }

        public async Task<bool> DeleteCharacterAsync(string characterId)
        {
            try
            {
                var response = await _communication.PostAsync<object, ApiResponse<bool>>(
                    $"api/character/{characterId}/delete", new { });

                if (response?.Success == true)
                {
                    _logger.LogInformation("成功删除角色: {CharacterId}", characterId);

                    // 清除缓存
                    _characterCache.Remove(characterId);

                    // 如果删除的是活跃角色，清空活跃角色
                    if (_activeCharacter?.Id == characterId)
                    {
                        _activeCharacter = null;
                        ActiveCharacterUpdated?.Invoke(this, null!);
                    }

                    // 刷新花名册
                    await GetRosterAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除角色失败");
                return false;
            }
        }

        public async Task<bool> SwitchCharacterAsync(string characterId)
        {
            try
            {
                var request = new SwitchCharacterRequest { CharacterId = characterId };
                var response = await _communication.PostAsync<SwitchCharacterRequest, ApiResponse<CharacterFullDto>>(
                    "api/character/switch", request);

                if (response?.Success == true && response.Data != null)
                {
                    _logger.LogInformation("成功切换角色: {CharacterId}", characterId);

                    // 检查离线收益
                    var offlineProgress = await GetOfflineProgressAsync(characterId);
                    if (offlineProgress != null)
                    {
                        OfflineProgressReceived?.Invoke(this, offlineProgress);
                    }

                    // 更新活跃角色
                    _activeCharacter = response.Data;
                    _characterCache[characterId] = response.Data;
                    ActiveCharacterUpdated?.Invoke(this, response.Data);

                    // 更新花名册的活跃角色ID
                    if (_currentRoster != null)
                    {
                        _currentRoster.ActiveCharacterId = characterId;
                        RosterUpdated?.Invoke(this, _currentRoster);
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换角色失败");
                return false;
            }
        }

        public async Task<CharacterFullDto?> GetCharacterDetailsAsync(string characterId)
        {
            try
            {
                // 先检查缓存
                if (_characterCache.ContainsKey(characterId))
                {
                    var cached = _characterCache[characterId];
                    // 如果缓存数据不超过30秒，直接返回
                    if ((DateTime.UtcNow - cached.LastActiveAt).TotalSeconds < 30)
                    {
                        return cached;
                    }
                }

                var response = await _communication.GetAsync<ApiResponse<CharacterFullDto>>(
                    $"api/character/{characterId}/details");

                if (response?.Success == true && response.Data != null)
                {
                    // 更新缓存
                    _characterCache[characterId] = response.Data;

                    // 如果是活跃角色，更新活跃角色信息
                    if (_activeCharacter?.Id == characterId)
                    {
                        _activeCharacter = response.Data;
                        ActiveCharacterUpdated?.Invoke(this, response.Data);
                    }

                    return response.Data;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色详情失败");
                return null;
            }
        }

        public async Task<CharacterFullDto?> RefreshActiveCharacterAsync()
        {
            if (_activeCharacter == null)
                return null;

            return await GetCharacterDetailsAsync(_activeCharacter.Id);
        }

        public async Task<bool> AllocateAttributePointsAsync(Dictionary<string, int> points)
        {
            if (_activeCharacter == null)
            {
                _logger.LogWarning("没有活跃角色，无法分配属性点");
                return false;
            }

            try
            {
                var request = new AllocateAttributePointsRequest { Points = points };
                var response = await _communication.PostAsync<AllocateAttributePointsRequest, ApiResponse<CharacterAttributesDto>>(
                    $"api/character/{_activeCharacter.Id}/attributes/allocate", request);

                if (response?.Success == true && response.Data != null)
                {
                    _logger.LogInformation("成功分配属性点");

                    // 更新本地属性
                    _activeCharacter.Attributes = response.Data;
                    ActiveCharacterUpdated?.Invoke(this, _activeCharacter);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分配属性点失败");
                return false;
            }
        }

        public async Task<bool> ResetAttributesAsync()
        {
            if (_activeCharacter == null)
                return false;

            try
            {
                var response = await _communication.PostAsync<object, ApiResponse<CharacterAttributesDto>>(
                    $"api/character/{_activeCharacter.Id}/attributes/reset", new { });

                if (response?.Success == true && response.Data != null)
                {
                    _logger.LogInformation("成功重置属性点");

                    _activeCharacter.Attributes = response.Data;
                    ActiveCharacterUpdated?.Invoke(this, _activeCharacter);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置属性点失败");
                return false;
            }
        }

        public async Task<OfflineProgressDto?> GetOfflineProgressAsync(string characterId)
        {
            try
            {
                var response = await _communication.GetAsync<ApiResponse<OfflineProgressDto>>(
                    $"api/character/{characterId}/offline-progress");

                if (response?.Success == true && response.Data != null)
                {
                    return response.Data;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "获取离线进度失败（可能没有离线收益）");
                return null;
            }
        }
    }
}