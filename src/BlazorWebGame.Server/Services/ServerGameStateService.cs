using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Models;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 服务端游戏状态管理服务 - 替代客户端的GameStateService
/// </summary>
public class ServerGameStateService
{
    private readonly ServerCharacterService _characterService;
    private readonly ServerPlayerAttributeService _playerAttributeService;
    private readonly ServerInventoryService _inventoryService;
    private readonly ServerPartyService _partyService;
    private readonly ServerQuestService _questService;
    private readonly ServerProductionService _productionService;
    private readonly ILogger<ServerGameStateService> _logger;
    
    // 存储角色的自动化状态
    private readonly Dictionary<string, AutomationStateDto> _automationStates = new();
    
    // 存储角色的实时状态更新计数器
    private readonly Dictionary<string, long> _updateTicks = new();
    private long _globalTick = 0;

    public ServerGameStateService(
        ServerCharacterService characterService,
        ServerPlayerAttributeService playerAttributeService,
        ServerInventoryService inventoryService,
        ServerPartyService partyService,
        ServerQuestService questService,
        ServerProductionService productionService,
        ILogger<ServerGameStateService> logger)
    {
        _characterService = characterService;
        _playerAttributeService = playerAttributeService;
        _inventoryService = inventoryService;
        _partyService = partyService;
        _questService = questService;
        _productionService = productionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取角色的完整游戏状态
    /// </summary>
    public async Task<GameStateDto?> GetGameStateAsync(string characterId)
    {
        try
        {
            var character = await _characterService.GetCharacterAsync(characterId);
            if (character == null) return null;

            var inventory = _inventoryService.GetCharacterInventory(characterId);
            var party = _partyService.GetPartyForCharacter(characterId);
            var quests = await _questService.GetCharacterQuestsAsync(characterId);
            var gatheringState = _productionService.GetGatheringState(characterId);
            var craftingState = _productionService.GetCraftingState(characterId);
            var automationState = GetAutomationState(characterId);

            return new GameStateDto
            {
                CharacterId = characterId,
                Character = character,
                Inventory = inventory,
                Party = party,
                Quests = quests,
                GatheringState = gatheringState,
                CraftingState = craftingState,
                AutomationState = automationState,
                LastUpdateTick = _updateTicks.GetValueOrDefault(characterId, 0)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game state for character {CharacterId}", characterId);
            return null;
        }
    }

    /// <summary>
    /// 更新角色的动作状态
    /// </summary>
    public async Task<ApiResponse<PlayerActionStateDto>> UpdatePlayerActionAsync(UpdatePlayerActionRequest request)
    {
        try
        {
            var character = await _characterService.GetCharacterAsync(request.CharacterId);
            if (character == null)
            {
                return new ApiResponse<PlayerActionStateDto>
                {
                    Success = false,
                    Message = "角色不存在"
                };
            }

            // 更新角色动作状态
            character.CurrentAction = request.ActionState;
            
            // 根据动作状态设置相应的冷却时间和状态
            switch (request.ActionState)
            {
                case PlayerActionState.Idle:
                    character.CurrentGatheringNode = null;
                    character.CurrentRecipe = null;
                    character.GatheringCooldown = 0;
                    character.CraftingCooldown = 0;
                    break;
                    
                case PlayerActionState.Reviving:
                    character.ReviveCooldown = 2.0; // 2秒复活时间
                    break;
            }

            await _characterService.UpdateCharacterAsync(character);
            IncrementUpdateTick(request.CharacterId);

            return new ApiResponse<PlayerActionStateDto>
            {
                Success = true,
                Message = "更新角色动作状态成功",
                Data = new PlayerActionStateDto
                {
                    CharacterId = request.CharacterId,
                    ActionState = character.CurrentAction,
                    GatheringCooldown = character.GatheringCooldown,
                    CraftingCooldown = character.CraftingCooldown,
                    ReviveCooldown = character.ReviveCooldown
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating player action for character {CharacterId}", request.CharacterId);
            return new ApiResponse<PlayerActionStateDto>
            {
                Success = false,
                Message = "更新角色动作状态时发生错误"
            };
        }
    }

    /// <summary>
    /// 设置角色的自动化操作
    /// </summary>
    public async Task<ApiResponse<AutomationStateDto>> SetAutomationAsync(SetAutomationRequest request)
    {
        try
        {
            var character = await _characterService.GetCharacterAsync(request.CharacterId);
            if (character == null)
            {
                return new ApiResponse<AutomationStateDto>
                {
                    Success = false,
                    Message = "角色不存在"
                };
            }

            var automationState = new AutomationStateDto
            {
                CharacterId = request.CharacterId,
                IsAutoBattleEnabled = request.IsAutoBattleEnabled,
                IsAutoGatheringEnabled = request.IsAutoGatheringEnabled,
                IsAutoCraftingEnabled = request.IsAutoCraftingEnabled,
                AutoBattleSettings = request.AutoBattleSettings,
                AutoGatheringSettings = request.AutoGatheringSettings,
                AutoCraftingSettings = request.AutoCraftingSettings
            };

            _automationStates[request.CharacterId] = automationState;
            IncrementUpdateTick(request.CharacterId);

            _logger.LogInformation("Set automation for character {CharacterId}: Battle={AutoBattle}, Gathering={AutoGathering}, Crafting={AutoCrafting}",
                request.CharacterId, request.IsAutoBattleEnabled, request.IsAutoGatheringEnabled, request.IsAutoCraftingEnabled);

            return new ApiResponse<AutomationStateDto>
            {
                Success = true,
                Message = "设置自动化操作成功",
                Data = automationState
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting automation for character {CharacterId}", request.CharacterId);
            return new ApiResponse<AutomationStateDto>
            {
                Success = false,
                Message = "设置自动化操作时发生错误"
            };
        }
    }

    /// <summary>
    /// 获取角色的自动化状态
    /// </summary>
    public async Task<AutomationStateDto> GetAutomationStateAsync(string characterId)
    {
        return GetAutomationState(characterId);
    }

    /// <summary>
    /// 处理角色复活
    /// </summary>
    public async Task<ApiResponse<CharacterStatusDto>> ReviveCharacterAsync(ReviveCharacterRequest request)
    {
        try
        {
            var character = await _characterService.GetCharacterAsync(request.CharacterId);
            if (character == null)
            {
                return new ApiResponse<CharacterStatusDto>
                {
                    Success = false,
                    Message = "角色不存在"
                };
            }

            if (character.Health > 0)
            {
                return new ApiResponse<CharacterStatusDto>
                {
                    Success = false,
                    Message = "角色未死亡，无需复活"
                };
            }

            // 复活角色
            character.Health = character.GetTotalMaxHealth() * 0.25; // 复活后恢复25%血量
            character.CurrentAction = PlayerActionState.Idle;
            character.ReviveCooldown = 0;

            await _characterService.UpdateCharacterAsync(character);
            IncrementUpdateTick(request.CharacterId);

            _logger.LogInformation("Character {CharacterId} revived with {Health} health", request.CharacterId, character.Health);

            return new ApiResponse<CharacterStatusDto>
            {
                Success = true,
                Message = "角色复活成功",
                Data = new CharacterStatusDto
                {
                    CharacterId = request.CharacterId,
                    Health = character.Health,
                    MaxHealth = character.GetTotalMaxHealth(),
                    Mana = character.Mana,
                    MaxMana = character.GetTotalMaxMana(),
                    ActionState = character.CurrentAction
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviving character {CharacterId}", request.CharacterId);
            return new ApiResponse<CharacterStatusDto>
            {
                Success = false,
                Message = "角色复活时发生错误"
            };
        }
    }

    /// <summary>
    /// 获取实时状态更新
    /// </summary>
    public async Task<GameStateUpdateDto> GetUpdatesAsync(string characterId, long lastUpdateTick)
    {
        try
        {
            var currentTick = _updateTicks.GetValueOrDefault(characterId, 0);
            
            if (currentTick <= lastUpdateTick)
            {
                // 没有更新
                return new GameStateUpdateDto
                {
                    CharacterId = characterId,
                    UpdateTick = currentTick,
                    HasUpdates = false
                };
            }

            // 获取更新后的状态
            var character = await _characterService.GetCharacterAsync(characterId);
            var gatheringState = _productionService.GetGatheringState(characterId);
            var craftingState = _productionService.GetCraftingState(characterId);
            var automationState = GetAutomationState(characterId);

            return new GameStateUpdateDto
            {
                CharacterId = characterId,
                UpdateTick = currentTick,
                HasUpdates = true,
                Character = character,
                GatheringState = gatheringState,
                CraftingState = craftingState,
                AutomationState = automationState
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting updates for character {CharacterId}", characterId);
            return new GameStateUpdateDto
            {
                CharacterId = characterId,
                UpdateTick = lastUpdateTick,
                HasUpdates = false
            };
        }
    }

    /// <summary>
    /// 重置角色状态到空闲
    /// </summary>
    public async Task<ApiResponse<string>> ResetCharacterStateAsync(ResetCharacterStateRequest request)
    {
        try
        {
            var character = await _characterService.GetCharacterAsync(request.CharacterId);
            if (character == null)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "角色不存在"
                };
            }

            // 重置角色状态
            character.CurrentAction = PlayerActionState.Idle;
            character.CurrentGatheringNode = null;
            character.CurrentRecipe = null;
            character.GatheringCooldown = 0;
            character.CraftingCooldown = 0;
            character.ReviveCooldown = 0;

            // 停止所有生产活动
            await _productionService.StopAllProductionAsync(new StopAllProductionRequest { CharacterId = request.CharacterId });

            // 清除自动化状态
            _automationStates.Remove(request.CharacterId);

            await _characterService.UpdateCharacterAsync(character);
            IncrementUpdateTick(request.CharacterId);

            _logger.LogInformation("Reset character state for {CharacterId}", request.CharacterId);

            return new ApiResponse<string>
            {
                Success = true,
                Message = "重置角色状态成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting character state for {CharacterId}", request.CharacterId);
            return new ApiResponse<string>
            {
                Success = false,
                Message = "重置角色状态时发生错误"
            };
        }
    }

    /// <summary>
    /// 获取自动化状态（内部方法）
    /// </summary>
    private AutomationStateDto GetAutomationState(string characterId)
    {
        return _automationStates.GetValueOrDefault(characterId, new AutomationStateDto
        {
            CharacterId = characterId,
            IsAutoBattleEnabled = false,
            IsAutoGatheringEnabled = false,
            IsAutoCraftingEnabled = false
        });
    }

    /// <summary>
    /// 增加角色的更新计数器
    /// </summary>
    private void IncrementUpdateTick(string characterId)
    {
        _globalTick++;
        _updateTicks[characterId] = _globalTick;
    }

    /// <summary>
    /// 游戏主循环处理 - 由GameLoopService调用
    /// </summary>
    public async Task ProcessGameTickAsync(double deltaTime)
    {
        try
        {
            // 处理所有活跃角色的状态更新
            var activeCharacters = await _characterService.GetActiveCharactersAsync();
            
            foreach (var character in activeCharacters)
            {
                await ProcessCharacterTick(character, deltaTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing game tick");
        }
    }

    /// <summary>
    /// 处理单个角色的游戏循环
    /// </summary>
    private async Task ProcessCharacterTick(CharacterDto character, double deltaTime)
    {
        try
        {
            bool hasUpdates = false;

            // 处理复活冷却
            if (character.ReviveCooldown > 0)
            {
                character.ReviveCooldown -= deltaTime;
                if (character.ReviveCooldown <= 0)
                {
                    character.ReviveCooldown = 0;
                    character.CurrentAction = PlayerActionState.Idle;
                    hasUpdates = true;
                }
            }

            // 处理法力回复
            if (character.Mana < character.GetTotalMaxMana())
            {
                character.Mana = Math.Min(character.GetTotalMaxMana(), character.Mana + deltaTime * 2); // 每秒回复2点法力
                hasUpdates = true;
            }

            // 处理自动化操作
            var automationState = GetAutomationState(character.CharacterId);
            if (automationState.IsAutoBattleEnabled || automationState.IsAutoGatheringEnabled || automationState.IsAutoCraftingEnabled)
            {
                await ProcessAutomation(character, automationState, deltaTime);
                hasUpdates = true;
            }

            // 如果有更新，保存角色状态并增加更新计数器
            if (hasUpdates)
            {
                await _characterService.UpdateCharacterAsync(character);
                IncrementUpdateTick(character.CharacterId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing character tick for {CharacterId}", character.CharacterId);
        }
    }

    /// <summary>
    /// 处理自动化操作
    /// </summary>
    private async Task ProcessAutomation(CharacterDto character, AutomationStateDto automationState, double deltaTime)
    {
        // 这里可以实现自动战斗、自动采集、自动制作的逻辑
        // 为了简化，暂时只记录日志
        if (automationState.IsAutoBattleEnabled && character.CurrentAction == PlayerActionState.Idle)
        {
            _logger.LogDebug("Auto battle enabled for character {CharacterId}", character.CharacterId);
        }

        if (automationState.IsAutoGatheringEnabled && character.CurrentAction == PlayerActionState.Idle)
        {
            _logger.LogDebug("Auto gathering enabled for character {CharacterId}", character.CharacterId);
        }

        if (automationState.IsAutoCraftingEnabled && character.CurrentAction == PlayerActionState.Idle)
        {
            _logger.LogDebug("Auto crafting enabled for character {CharacterId}", character.CharacterId);
        }
    }
}