using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebGame.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController : ControllerBase
    {
        private readonly ServerCharacterService _characterService;
        private readonly ServerPlayerAttributeService _playerAttributeService;
        private readonly ServerPlayerProfessionService _playerProfessionService;
        private readonly ServerPlayerUtilityService _playerUtilityService;
        private readonly ILogger<PlayerController> _logger;

        public PlayerController(
            ServerCharacterService characterService,
            ServerPlayerAttributeService playerAttributeService,
            ServerPlayerProfessionService playerProfessionService,
            ServerPlayerUtilityService playerUtilityService,
            ILogger<PlayerController> logger)
        {
            _characterService = characterService;
            _playerAttributeService = playerAttributeService;
            _playerProfessionService = playerProfessionService;
            _playerUtilityService = playerUtilityService;
            _logger = logger;
        }

        /// <summary>
        /// 获取角色的总属性值
        /// </summary>
        [HttpGet("{characterId}/attributes")]
        public async Task<IActionResult> GetPlayerAttributes(string characterId)
        {
            try
            {
                var character = await _characterService.GetCharacterDetailsAsync(characterId);
                if (character == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                var attributes = _playerAttributeService.GetTotalAttributes(character);
                return Ok(new { success = true, data = attributes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting player attributes for character {CharacterId}", characterId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 获取角色的攻击力
        /// </summary>
        [HttpGet("{characterId}/attack-power")]
        public async Task<IActionResult> GetPlayerAttackPower(string characterId)
        {
            try
            {
                var character = await _characterService.GetCharacterDetailsAsync(characterId);
                if (character == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                var attackPower = _playerAttributeService.GetTotalAttackPower(character);
                return Ok(new { success = true, data = attackPower });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attack power for character {CharacterId}", characterId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 获取角色的最大生命值
        /// </summary>
        [HttpGet("{characterId}/max-health")]
        public async Task<IActionResult> GetPlayerMaxHealth(string characterId)
        {
            try
            {
                var character = await _characterService.GetCharacterDetailsAsync(characterId);
                if (character == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                var maxHealth = _playerAttributeService.GetTotalMaxHealth(character);
                return Ok(new { success = true, data = maxHealth });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting max health for character {CharacterId}", characterId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 获取角色专业等级
        /// </summary>
        [HttpGet("{characterId}/profession/{professionType}/{profession}/level")]
        public async Task<IActionResult> GetProfessionLevel(string characterId, string professionType, string profession)
        {
            try
            {
                var level = _characterService.GetCharacterProfessionLevel(characterId, professionType, profession);
                return Ok(new { success = true, data = level });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profession level for character {CharacterId}, profession {Profession}", 
                    characterId, profession);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 获取角色专业进度
        /// </summary>
        [HttpGet("{characterId}/profession/{professionType}/{profession}/progress")]
        public async Task<IActionResult> GetProfessionProgress(string characterId, string professionType, string profession)
        {
            try
            {
                var character = await _characterService.GetCharacterDetailsAsync(characterId);
                if (character == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                double progress = professionType.ToLower() switch
                {
                    "battle" => _playerProfessionService.GetLevelProgress(character,
                        Enum.Parse<BattleProfession>(profession)),
                    "gathering" => _playerProfessionService.GetLevelProgress(character,
                        Enum.Parse<GatheringProfession>(profession)),
                    "production" => _playerProfessionService.GetLevelProgress(character,
                        Enum.Parse<ProductionProfession>(profession)),
                    _ => 0.0
                };

                return Ok(new { success = true, data = progress });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profession progress for character {CharacterId}, profession {Profession}", 
                    characterId, profession);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 获取角色的采集速度加成
        /// </summary>
        [HttpGet("{characterId}/gathering-speed-bonus")]
        public async Task<IActionResult> GetGatheringSpeedBonus(string characterId)
        {
            try
            {
                var character = await _characterService.GetCharacterDetailsAsync(characterId);
                if (character == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                var bonus = _playerProfessionService.GetTotalGatheringSpeedBonus(character);
                return Ok(new { success = true, data = bonus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gathering speed bonus for character {CharacterId}", characterId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 获取角色的额外战利品概率
        /// </summary>
        [HttpGet("{characterId}/extra-loot-chance")]
        public async Task<IActionResult> GetExtraLootChance(string characterId)
        {
            try
            {
                var character = await _characterService.GetCharacterDetailsAsync(characterId);
                if (character == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                var chance = _playerProfessionService.GetTotalExtraLootChance(character);
                return Ok(new { success = true, data = chance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting extra loot chance for character {CharacterId}", characterId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 检查角色背包中是否有指定物品
        /// </summary>
        [HttpGet("{characterId}/inventory/has-item/{itemId}")]
        public async Task<IActionResult> HasItemInInventory(string characterId, string itemId)
        {
            try
            {
                var character = await _characterService.GetCharacterDetailsAsync(characterId);
                if (character == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                var hasItem = _playerUtilityService.HasItemInInventory(character, itemId);
                return Ok(new { success = true, data = hasItem });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking inventory for character {CharacterId}, item {ItemId}", 
                    characterId, itemId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 获取角色背包中指定物品的数量
        /// </summary>
        [HttpGet("{characterId}/inventory/item-quantity/{itemId}")]
        public async Task<IActionResult> GetItemQuantity(string characterId, string itemId)
        {
            try
            {
                var character = await _characterService.GetCharacterDetailsAsync(characterId);
                if (character == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                var quantity = _playerUtilityService.GetItemQuantity(character, itemId);
                return Ok(new { success = true, data = quantity });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item quantity for character {CharacterId}, item {ItemId}", 
                    characterId, itemId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 检查角色是否满足等级要求
        /// </summary>
        [HttpGet("{characterId}/meets-level-requirement/{profession}/{requiredLevel}")]
        public async Task<IActionResult> MeetsLevelRequirement(string characterId, string profession, int requiredLevel)
        {
            try
            {
                var meets = _characterService.CheckLevelRequirement(characterId, profession, requiredLevel);
                return Ok(new { success = true, data = meets });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking level requirement for character {CharacterId}, profession {Profession}, level {Level}", 
                    characterId, profession, requiredLevel);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 初始化角色数据一致性
        /// </summary>
        [HttpPost("{characterId}/ensure-data-consistency")]
        public async Task<IActionResult> EnsureDataConsistency(string characterId)
        {
            try
            {
                var character = await _characterService.GetCharacterDetailsAsync(characterId);
                if (character == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                _playerUtilityService.EnsureDataConsistency(character);
                
                return Ok(new { success = true, message = "Data consistency ensured" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring data consistency for character {CharacterId}", characterId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 重新初始化角色属性
        /// </summary>
        [HttpPost("{characterId}/reinitialize-attributes")]
        public async Task<IActionResult> ReinitializeAttributes(string characterId)
        {
            try
            {
                var character = await _characterService.GetCharacterDetailsAsync(characterId);
                if (character == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                _playerAttributeService.InitializePlayerAttributes(character);
                
                return Ok(new { success = true, message = "Player attributes reinitialized" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reinitializing attributes for character {CharacterId}", characterId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}