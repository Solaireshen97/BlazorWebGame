using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Character;
using BlazorWebGame.Server.Services.Character;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BlazorWebGame.Server.Controllers
{
    /// <summary>
    /// 角色管理控制器 - 提供角色花名册和增强角色管理功能
    /// </summary>
    [ApiController]
    [Route("api/character-management")]
    [Authorize]
    public class CharacterManagementController : ControllerBase
    {
        private readonly ServerCharacterManagementService _characterManagementService;
        private readonly ILogger<CharacterManagementController> _logger;

        public CharacterManagementController(
            ServerCharacterManagementService characterManagementService,
            ILogger<CharacterManagementController> logger)
        {
            _characterManagementService = characterManagementService;
            _logger = logger;
        }

        #region 角色花名册管理

        /// <summary>
        /// 获取当前用户的角色花名册
        /// </summary>
        [HttpGet("roster")]
        public async Task<ActionResult<ApiResponse<RosterDto>>> GetRoster()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<RosterDto>
                    {
                        Success = false,
                        Message = "用户未认证"
                    });
                }

                var roster = await _characterManagementService.GetRosterAsync(userId);
                if (roster == null)
                {
                    return NotFound(new ApiResponse<RosterDto>
                    {
                        Success = false,
                        Message = "未找到角色花名册"
                    });
                }

                return Ok(new ApiResponse<RosterDto>
                {
                    Success = true,
                    Data = roster,
                    Message = "获取角色花名册成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roster");
                return StatusCode(500, new ApiResponse<RosterDto>
                {
                    Success = false,
                    Message = "获取角色花名册失败"
                });
            }
        }

        /// <summary>
        /// 解锁角色槽位
        /// </summary>
        [HttpPost("roster/slots/{slotIndex}/unlock")]
        public async Task<ActionResult<ApiResponse<bool>>> UnlockSlot(int slotIndex)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "用户未认证"
                    });
                }

                var result = await _characterManagementService.UnlockSlotAsync(userId, slotIndex);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unlocking slot {slotIndex}");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "解锁槽位失败"
                });
            }
        }

        #endregion

        #region 角色创建和管理

        /// <summary>
        /// 创建新角色
        /// </summary>
        [HttpPost("characters")]
        public async Task<ActionResult<ApiResponse<CharacterFullDto>>> CreateCharacter([FromBody] CreateCharacterRequestDto request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<CharacterFullDto>
                    {
                        Success = false,
                        Message = "用户未认证"
                    });
                }

                var result = await _characterManagementService.CreateCharacterAsync(userId, request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating character");
                return StatusCode(500, new ApiResponse<CharacterFullDto>
                {
                    Success = false,
                    Message = "创建角色失败"
                });
            }
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        [HttpDelete("characters/{characterId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCharacter(string characterId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "用户未认证"
                    });
                }

                var result = await _characterManagementService.DeleteCharacterAsync(userId, characterId);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting character {characterId}");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "删除角色失败"
                });
            }
        }

        /// <summary>
        /// 切换活跃角色
        /// </summary>
        [HttpPost("characters/{characterId}/switch")]
        public async Task<ActionResult<ApiResponse<CharacterFullDto>>> SwitchCharacter(string characterId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<CharacterFullDto>
                    {
                        Success = false,
                        Message = "用户未认证"
                    });
                }

                var result = await _characterManagementService.SwitchCharacterAsync(userId, characterId);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error switching to character {characterId}");
                return StatusCode(500, new ApiResponse<CharacterFullDto>
                {
                    Success = false,
                    Message = "切换角色失败"
                });
            }
        }

        /// <summary>
        /// 获取角色详细信息
        /// </summary>
        [HttpGet("characters/{characterId}")]
        public async Task<ActionResult<ApiResponse<CharacterFullDto>>> GetCharacterDetails(string characterId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<CharacterFullDto>
                    {
                        Success = false,
                        Message = "用户未认证"
                    });
                }

                var result = await _characterManagementService.GetCharacterDetailsAsync(userId, characterId);
                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting character details {characterId}");
                return StatusCode(500, new ApiResponse<CharacterFullDto>
                {
                    Success = false,
                    Message = "获取角色详细信息失败"
                });
            }
        }

        #endregion

        #region 角色名称验证

        /// <summary>
        /// 验证角色名称是否可用
        /// </summary>
        [HttpPost("validate-name")]
        public ActionResult<ApiResponse<ValidateCharacterNameResult>> ValidateName([FromBody] ValidateCharacterNameRequest request)
        {
            try
            {
                var result = _characterManagementService.ValidateCharacterName(request.Name);
                
                return Ok(new ApiResponse<ValidateCharacterNameResult>
                {
                    Success = result.IsValid,
                    Data = result,
                    Message = result.IsValid ? "角色名称可用" : result.Reason ?? "角色名称无效"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating character name");
                return StatusCode(500, new ApiResponse<ValidateCharacterNameResult>
                {
                    Success = false,
                    Message = "验证角色名称失败"
                });
            }
        }

        #endregion

        #region 属性分配

        /// <summary>
        /// 分配角色属性点
        /// </summary>
        [HttpPost("characters/{characterId}/attributes/allocate")]
        public async Task<ActionResult<ApiResponse<CharacterAttributesDto>>> AllocateAttributePoints(
            string characterId,
            [FromBody] AllocateAttributePointsRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<CharacterAttributesDto>
                    {
                        Success = false,
                        Message = "用户未认证"
                    });
                }

                var result = await _characterManagementService.AllocateAttributePointsAsync(userId, characterId, request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error allocating attribute points for character {characterId}");
                return StatusCode(500, new ApiResponse<CharacterAttributesDto>
                {
                    Success = false,
                    Message = "分配属性点失败"
                });
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        #endregion
    }
}
