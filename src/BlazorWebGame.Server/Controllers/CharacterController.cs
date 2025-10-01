using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BlazorWebGame.Server.Services.Character;
using BlazorWebGame.Server.Services.Users;

namespace BlazorWebGame.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CharacterController : ControllerBase
    {
        private readonly ServerCharacterService _characterService;
        private readonly UserService _userService;
        private readonly ILogger<CharacterController> _logger;

        public CharacterController(
            ServerCharacterService characterService, 
            UserService userService,
            ILogger<CharacterController> logger)
        {
            _characterService = characterService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前用户的角色列表
        /// </summary>
        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<CharacterDto>>>> GetMyCharacters()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<List<CharacterDto>>
                    {
                        IsSuccess = false,
                        Message = "用户未认证"
                    });
                }

                var characters = await _characterService.GetUserCharactersAsync(userId);
                return Ok(new ApiResponse<List<CharacterDto>>
                {
                    IsSuccess = true,
                    Data = characters,
                    Message = "用户角色列表获取成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user characters");
                return StatusCode(500, new ApiResponse<List<CharacterDto>>
                {
                    IsSuccess = false,
                    Message = "获取用户角色列表失败"
                });
            }
        }

        /// <summary>
        /// 获取所有角色（管理员使用）
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CharacterDto>>>> GetCharacters()
        {
            try
            {
                var characters = await _characterService.GetCharactersAsync();
                return Ok(new ApiResponse<List<CharacterDto>>
                {
                    IsSuccess = true,
                    Data = characters,
                    Message = "角色列表获取成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get characters");
                return StatusCode(500, new ApiResponse<List<CharacterDto>>
                {
                    IsSuccess = false,
                    Message = "获取角色列表失败"
                });
            }
        }

        /// <summary>
        /// 获取角色详细信息
        /// </summary>
        [HttpGet("{characterId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterDetailsDto>>> GetCharacterDetails(string characterId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<CharacterDetailsDto>
                    {
                        IsSuccess = false,
                        Message = "用户未认证"
                    });
                }

                // 验证用户是否拥有该角色
                var ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return Forbid();
                }

                var character = await _characterService.GetCharacterDetailsAsync(characterId);
                if (character == null)
                {
                    return NotFound(new ApiResponse<CharacterDetailsDto>
                    {
                        IsSuccess = false,
                        Message = "角色不存在"
                    });
                }

                return Ok(new ApiResponse<CharacterDetailsDto>
                {
                    IsSuccess = true,
                    Data = character,
                    Message = "角色详情获取成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get character details for {CharacterId}", characterId);
                return StatusCode(500, new ApiResponse<CharacterDetailsDto>
                {
                    IsSuccess = false,
                    Message = "获取角色详情失败"
                });
            }
        }

        /// <summary>
        /// 创建新角色
        /// </summary>
        [HttpPost("create")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterDto>>> CreateCharacter(CreateCharacterRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new ApiResponse<CharacterDto>
                    {
                        IsSuccess = false,
                        Message = "角色名称不能为空"
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<CharacterDto>
                    {
                        IsSuccess = false,
                        Message = "用户未认证"
                    });
                }

                var character = await _characterService.CreateCharacterAsync(request, userId);
                return Ok(new ApiResponse<CharacterDto>
                {
                    IsSuccess = true,
                    Data = character,
                    Message = "角色创建成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create character");
                return StatusCode(500, new ApiResponse<CharacterDto>
                {
                    IsSuccess = false,
                    Message = "创建角色失败"
                });
            }
        }

        /// <summary>
        /// 添加经验值
        /// </summary>
        [HttpPost("{characterId}/experience")]
        public async Task<ActionResult<ApiResponse<bool>>> AddExperience(string characterId, AddExperienceRequest request)
        {
            try
            {
                request.CharacterId = characterId;
                var success = await _characterService.AddExperienceAsync(request);
                
                if (!success)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        IsSuccess = false,
                        Message = "角色不存在或专业类型无效"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "经验值添加成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add experience for character {CharacterId}", characterId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    IsSuccess = false,
                    Message = "添加经验值失败"
                });
            }
        }

        /// <summary>
        /// 更新角色状态
        /// </summary>
        [HttpPut("{characterId}/status")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateCharacterStatus(string characterId, UpdateCharacterStatusRequest request)
        {
            try
            {
                request.CharacterId = characterId;
                var success = await _characterService.UpdateCharacterStatusAsync(request);
                
                if (!success)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        IsSuccess = false,
                        Message = "角色不存在"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "角色状态更新成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update character status for {CharacterId}", characterId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    IsSuccess = false,
                    Message = "更新角色状态失败"
                });
            }
        }

        /// <summary>
        /// 更新角色数据（离线同步专用）
        /// </summary>
        [HttpPut("{characterId}/update")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateCharacter(string characterId, CharacterUpdateRequest request)
        {
            try
            {
                request.CharacterId = characterId;
                
                // 使用现有的UpdateCharacterStatusAsync方法来处理更新
                var statusRequest = new UpdateCharacterStatusRequest
                {
                    CharacterId = characterId,
                    Action = "OfflineSync",
                    Data = request.Updates
                };
                
                var success = await _characterService.UpdateCharacterStatusAsync(statusRequest);
                
                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        IsSuccess = false,
                        Message = "角色不存在"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { Updated = true },
                    Message = "角色数据更新成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update character for offline sync {CharacterId}", characterId);
                return StatusCode(500, new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = "更新角色数据失败"
                });
            }
        }
    }
}