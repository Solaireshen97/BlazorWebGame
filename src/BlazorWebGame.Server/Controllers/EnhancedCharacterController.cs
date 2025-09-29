using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; 
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Server.Services;
using System.Security.Claims;

namespace BlazorWebGame.Server.Controllers;

/// <summary>
/// 增强的角色控制器 - 支持用户认证和数据库持久化
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // 需要身份验证
public class EnhancedCharacterController : ControllerBase
{
    private readonly IDatabaseCharacterService _characterService;
    private readonly IUserService _userService;
    private readonly ILogger<EnhancedCharacterController> _logger;

    public EnhancedCharacterController(
        IDatabaseCharacterService characterService,
        IUserService userService,
        ILogger<EnhancedCharacterController> logger)
    {
        _characterService = characterService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户的所有角色
    /// </summary>
    [HttpGet("my-characters")]
    public async Task<ActionResult<ApiResponse<List<CharacterDto>>>> GetMyCharacters()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ApiResponse<List<CharacterDto>>
                {
                    Success = false,
                    Message = "用户未认证"
                });

            var characters = await _characterService.GetCharactersByUserIdAsync(userId);
            return Ok(new ApiResponse<List<CharacterDto>>
            {
                Success = true,
                Data = characters,
                Message = $"成功获取 {characters.Count} 个角色"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters for current user");
            return StatusCode(500, new ApiResponse<List<CharacterDto>>
            {
                Success = false,
                Message = "获取角色列表失败"
            });
        }
    }

    /// <summary>
    /// 获取所有角色（管理员功能）
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ApiResponse<List<CharacterDto>>>> GetAllCharacters()
    {
        try
        {
            var characters = await _characterService.GetAllCharactersAsync();
            return Ok(new ApiResponse<List<CharacterDto>>
            {
                Success = true,
                Data = characters,
                Message = $"成功获取 {characters.Count} 个角色"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all characters");
            return StatusCode(500, new ApiResponse<List<CharacterDto>>
            {
                Success = false,
                Message = "获取角色列表失败"
            });
        }
    }

    /// <summary>
    /// 获取指定角色的详细信息
    /// </summary>
    [HttpGet("{characterId}")]
    public async Task<ActionResult<ApiResponse<CharacterDetailsDto>>> GetCharacterDetails(string characterId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 验证角色归属
            if (!await _characterService.IsCharacterOwnedByUserAsync(characterId, userId))
            {
                return Forbid(new ApiResponse<CharacterDetailsDto>
                {
                    Success = false,
                    Message = "您没有权限访问该角色"
                }.ToString());
            }

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
            _logger.LogError(ex, "Failed to get character details for {CharacterId}", SafeLogId(characterId));
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
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 验证输入
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new ApiResponse<CharacterDto>
                {
                    Success = false,
                    Message = "角色名称不能为空"
                });
            }

            if (request.Name.Length < 2 || request.Name.Length > 20)
            {
                return BadRequest(new ApiResponse<CharacterDto>
                {
                    Success = false,
                    Message = "角色名称长度必须在2-20个字符之间"
                });
            }

            // 检查用户是否已达到角色数量限制
            var existingCharacters = await _characterService.GetCharactersByUserIdAsync(userId);
            if (existingCharacters.Count >= 10) // 假设限制10个角色
            {
                return BadRequest(new ApiResponse<CharacterDto>
                {
                    Success = false,
                    Message = "您已达到角色数量上限（10个）"
                });
            }

            var character = await _characterService.CreateCharacterAsync(userId, request);
            return Ok(new ApiResponse<CharacterDto>
            {
                Success = true,
                Data = character,
                Message = "角色创建成功"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid character creation request");
            return BadRequest(new ApiResponse<CharacterDto>
            {
                Success = false,
                Message = ex.Message
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
    /// 更新角色信息
    /// </summary>
    [HttpPut("{characterId}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateCharacter(string characterId, CharacterUpdateDto updates)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 验证角色归属
            if (!await _characterService.IsCharacterOwnedByUserAsync(characterId, userId))
            {
                return Forbid();
            }

            var success = await _characterService.UpdateCharacterAsync(characterId, updates);
            if (!success)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "角色不存在或更新失败"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "角色更新成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update character {CharacterId}", SafeLogId(characterId));
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "更新角色失败"
            });
        }
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    [HttpDelete("{characterId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCharacter(string characterId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 验证角色归属
            if (!await _characterService.IsCharacterOwnedByUserAsync(characterId, userId))
            {
                return Forbid();
            }

            var success = await _characterService.DeleteCharacterAsync(characterId);
            if (!success)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "角色不存在或删除失败"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "角色删除成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete character {CharacterId}", SafeLogId(characterId));
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "删除角色失败"
            });
        }
    }

    /// <summary>
    /// 验证角色归属（管理员功能）
    /// </summary>
    [HttpGet("{characterId}/owner")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ApiResponse<string>>> GetCharacterOwner(string characterId)
    {
        try
        {
            var character = await _characterService.GetCharacterDetailsAsync(characterId);
            if (character == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "角色不存在"
                });
            }

            // Note: Would need to add UserId to CharacterDetailsDto or fetch from PlayerEntity directly
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Data = "owner-user-id-placeholder", // TODO: Implement proper owner lookup
                Message = "角色归属查询成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character owner for {CharacterId}", SafeLogId(characterId));
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "查询角色归属失败"
            });
        }
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
               ?? User.FindFirst("sub")?.Value
               ?? User.FindFirst("userId")?.Value;
    }

    /// <summary>
    /// 安全地记录ID用于日志
    /// </summary>
    private static string SafeLogId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "[empty]";
        
        var sanitized = new string(id.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return sanitized.Length > 8 ? sanitized.Substring(0, 8) + "..." : sanitized;
    }
}