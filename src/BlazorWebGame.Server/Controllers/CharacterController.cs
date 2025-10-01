using BlazorWebGame.Server.Services.Character;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Character;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlazorWebGame.Server.Controllers
{
    [ApiController]
    [Route("api/character")]
    public class EnhancedCharacterController : ControllerBase
    {
        private readonly EnhancedServerCharacterService _characterService;
        private readonly ILogger<EnhancedCharacterController> _logger;

        public EnhancedCharacterController(
            EnhancedServerCharacterService characterService,
            ILogger<EnhancedCharacterController> logger)
        {
            _characterService = characterService;
            _logger = logger;
        }

        /// <summary>
        /// 获取角色花名册
        /// </summary>
        [HttpGet("roster")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<RosterDto>>> GetRoster()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<RosterDto>.Failure("用户未认证"));
                }

                var roster = await _characterService.GetUserRosterAsync(userId);
                return Ok(ApiResponse<RosterDto>.Success(roster, "获取角色花名册成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色花名册失败");
                return StatusCode(500, ApiResponse<RosterDto>.Failure("获取角色花名册失败"));
            }
        }

        /// <summary>
        /// 解锁角色槽位
        /// </summary>
        [HttpPost("roster/unlock/{slotIndex}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterSlotDto>>> UnlockSlot(int slotIndex)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CharacterSlotDto>.Failure("用户未认证"));
                }

                var result = await _characterService.UnlockSlotAsync(userId, slotIndex);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解锁角色槽位失败 {SlotIndex}", slotIndex);
                return StatusCode(500, ApiResponse<CharacterSlotDto>.Failure("解锁角色槽位失败"));
            }
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        [HttpPost("create")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterFullDto>>> CreateCharacter(CreateCharacterRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CharacterFullDto>.Failure("用户未认证"));
                }

                var result = await _characterService.CreateCharacterAsync(userId, request);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建角色失败 {Name}", request.Name);
                return StatusCode(500, ApiResponse<CharacterFullDto>.Failure("创建角色失败"));
            }
        }

        /// <summary>
        /// 验证角色名称
        /// </summary>
        [HttpPost("validate-name")]
        public ActionResult<ApiResponse<ValidateCharacterNameResult>> ValidateName(ValidateCharacterNameRequest request)
        {
            try
            {
                var result = _characterService.ValidateCharacterName(request.Name);
                return Ok(ApiResponse<ValidateCharacterNameResult>.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证角色名称失败 {Name}", request.Name);
                return StatusCode(500, ApiResponse<ValidateCharacterNameResult>.Failure("验证角色名称失败"));
            }
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        [HttpPost("{characterId}/delete")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCharacter(string characterId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<bool>.Failure("用户未认证"));
                }

                var result = await _characterService.DeleteCharacterAsync(userId, characterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除角色失败 {CharacterId}", characterId);
                return StatusCode(500, ApiResponse<bool>.Failure("删除角色失败"));
            }
        }

        /// <summary>
        /// 切换角色
        /// </summary>
        [HttpPost("switch")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterFullDto>>> SwitchCharacter(SwitchCharacterRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CharacterFullDto>.Failure("用户未认证"));
                }

                var result = await _characterService.SwitchCharacterAsync(userId, request.CharacterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换角色失败 {CharacterId}", request.CharacterId);
                return StatusCode(500, ApiResponse<CharacterFullDto>.Failure("切换角色失败"));
            }
        }

        /// <summary>
        /// 获取角色详情
        /// </summary>
        [HttpGet("{characterId}/details")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterFullDto>>> GetCharacterDetails(string characterId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CharacterFullDto>.Failure("用户未认证"));
                }

                // 验证用户是否拥有该角色
                bool ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return Forbid();
                }

                var result = _characterService.GetCharacterDetails(characterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色详情失败 {CharacterId}", characterId);
                return StatusCode(500, ApiResponse<CharacterFullDto>.Failure("获取角色详情失败"));
            }
        }

        /// <summary>
        /// 分配属性点
        /// </summary>
        [HttpPost("{characterId}/attributes/allocate")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterAttributesDto>>> AllocateAttributePoints(
            string characterId, AllocateAttributePointsRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CharacterAttributesDto>.Failure("用户未认证"));
                }

                // 验证用户是否拥有该角色
                bool ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return Forbid();
                }

                var result = _characterService.AllocateAttributePointsAsync(characterId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分配属性点失败 {CharacterId}", characterId);
                return StatusCode(500, ApiResponse<CharacterAttributesDto>.Failure("分配属性点失败"));
            }
        }

        /// <summary>
        /// 重置属性点
        /// </summary>
        [HttpPost("{characterId}/attributes/reset")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterAttributesDto>>> ResetAttributes(string characterId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CharacterAttributesDto>.Failure("用户未认证"));
                }

                // 验证用户是否拥有该角色
                bool ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return Forbid();
                }

                var result = _characterService.ResetAttributesAsync(characterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置属性点失败 {CharacterId}", characterId);
                return StatusCode(500, ApiResponse<CharacterAttributesDto>.Failure("重置属性点失败"));
            }
        }

        /// <summary>
        /// 获取离线进度
        /// </summary>
        [HttpGet("{characterId}/offline-progress")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<OfflineProgressDto>>> GetOfflineProgress(string characterId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<OfflineProgressDto>.Failure("用户未认证"));
                }

                // 验证用户是否拥有该角色
                bool ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return Forbid();
                }

                var result = await _characterService.GetOfflineProgressAsync(characterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取离线进度失败 {CharacterId}", characterId);
                return StatusCode(500, ApiResponse<OfflineProgressDto>.Failure("获取离线进度失败"));
            }
        }
    }

    /// <summary>
    /// 角色名称验证请求
    /// </summary>
    public class ValidateCharacterNameRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 切换角色请求
    /// </summary>
    public class SwitchCharacterRequest
    {
        public string CharacterId { get; set; } = string.Empty;
    }
}