using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebGame.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CharacterController : ControllerBase
    {
        private readonly ServerCharacterService _characterService;
        private readonly ILogger<CharacterController> _logger;

        public CharacterController(ServerCharacterService characterService, ILogger<CharacterController> logger)
        {
            _characterService = characterService;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有角色
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CharacterDto>>>> GetCharacters()
        {
            try
            {
                var characters = await _characterService.GetCharactersAsync();
                return Ok(new ApiResponse<List<CharacterDto>>
                {
                    Success = true,
                    Data = characters,
                    Message = "角色列表获取成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get characters");
                return StatusCode(500, new ApiResponse<List<CharacterDto>>
                {
                    Success = false,
                    Message = "获取角色列表失败"
                });
            }
        }

        /// <summary>
        /// 获取角色详细信息
        /// </summary>
        [HttpGet("{characterId}")]
        public async Task<ActionResult<ApiResponse<CharacterDetailsDto>>> GetCharacterDetails(string characterId)
        {
            try
            {
                var character = await _characterService.GetCharacterDetailsAsync(characterId);
                if (character == null)
                {
                    return NotFound(new ApiResponse<CharacterDetailsDto>
                    {
                        Success = false,
                        Message = "角色不存在"
                    });
                }

                return Ok(new ApiResponse<CharacterDetailsDto>
                {
                    Success = true,
                    Data = character,
                    Message = "角色详情获取成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get character details for {CharacterId}", characterId);
                return StatusCode(500, new ApiResponse<CharacterDetailsDto>
                {
                    Success = false,
                    Message = "获取角色详情失败"
                });
            }
        }

        /// <summary>
        /// 创建新角色
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CharacterDto>>> CreateCharacter(CreateCharacterRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new ApiResponse<CharacterDto>
                    {
                        Success = false,
                        Message = "角色名称不能为空"
                    });
                }

                var character = await _characterService.CreateCharacterAsync(request);
                return Ok(new ApiResponse<CharacterDto>
                {
                    Success = true,
                    Data = character,
                    Message = "角色创建成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create character");
                return StatusCode(500, new ApiResponse<CharacterDto>
                {
                    Success = false,
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
                        Success = false,
                        Message = "角色不存在或专业类型无效"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "经验值添加成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add experience for character {CharacterId}", characterId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
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
                        Success = false,
                        Message = "角色不存在"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "角色状态更新成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update character status for {CharacterId}", characterId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "更新角色状态失败"
                });
            }
        }
    }
}