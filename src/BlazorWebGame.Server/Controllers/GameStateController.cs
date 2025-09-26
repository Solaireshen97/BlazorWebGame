using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebGame.Server.Controllers;

/// <summary>
/// 游戏状态管理 API 控制器 - 处理全局游戏状态、角色状态和实时更新
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GameStateController : ControllerBase
{
    private readonly ServerGameStateService _gameStateService;
    private readonly ILogger<GameStateController> _logger;

    public GameStateController(ServerGameStateService gameStateService, ILogger<GameStateController> logger)
    {
        _gameStateService = gameStateService;
        _logger = logger;
    }

    /// <summary>
    /// 获取角色的完整游戏状态
    /// </summary>
    [HttpGet("{characterId}")]
    public async Task<ActionResult<ApiResponse<GameStateDto>>> GetGameState(string characterId)
    {
        try
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return BadRequest(new ApiResponse<GameStateDto>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var gameState = await _gameStateService.GetGameStateAsync(characterId);
            
            if (gameState == null)
            {
                return NotFound(new ApiResponse<GameStateDto>
                {
                    Success = false,
                    Message = "未找到角色游戏状态"
                });
            }

            return Ok(new ApiResponse<GameStateDto>
            {
                Success = true,
                Message = "获取游戏状态成功",
                Data = gameState
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game state for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<GameStateDto>
            {
                Success = false,
                Message = "获取游戏状态时发生错误"
            });
        }
    }

    /// <summary>
    /// 更新角色的动作状态
    /// </summary>
    [HttpPost("action/update")]
    public async Task<ActionResult<ApiResponse<PlayerActionStateDto>>> UpdatePlayerAction(UpdatePlayerActionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId))
            {
                return BadRequest(new ApiResponse<PlayerActionStateDto>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var result = await _gameStateService.UpdatePlayerActionAsync(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating player action for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<PlayerActionStateDto>
            {
                Success = false,
                Message = "更新角色动作状态时发生错误"
            });
        }
    }

    /// <summary>
    /// 设置角色的自动化操作
    /// </summary>
    [HttpPost("automation/set")]
    public async Task<ActionResult<ApiResponse<AutomationStateDto>>> SetAutomation(SetAutomationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId))
            {
                return BadRequest(new ApiResponse<AutomationStateDto>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var result = await _gameStateService.SetAutomationAsync(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting automation for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<AutomationStateDto>
            {
                Success = false,
                Message = "设置自动化操作时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取角色的自动化状态
    /// </summary>
    [HttpGet("automation/{characterId}")]
    public async Task<ActionResult<ApiResponse<AutomationStateDto>>> GetAutomationState(string characterId)
    {
        try
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return BadRequest(new ApiResponse<AutomationStateDto>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var automationState = await _gameStateService.GetAutomationStateAsync(characterId);
            
            return Ok(new ApiResponse<AutomationStateDto>
            {
                Success = true,
                Message = "获取自动化状态成功",
                Data = automationState
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting automation state for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<AutomationStateDto>
            {
                Success = false,
                Message = "获取自动化状态时发生错误"
            });
        }
    }

    /// <summary>
    /// 处理角色复活
    /// </summary>
    [HttpPost("revive")]
    public async Task<ActionResult<ApiResponse<CharacterStatusDto>>> ReviveCharacter(ReviveCharacterRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId))
            {
                return BadRequest(new ApiResponse<CharacterStatusDto>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var result = await _gameStateService.ReviveCharacterAsync(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviving character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<CharacterStatusDto>
            {
                Success = false,
                Message = "角色复活时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取实时状态更新（用于轮询）
    /// </summary>
    [HttpGet("updates/{characterId}")]
    public async Task<ActionResult<ApiResponse<GameStateUpdateDto>>> GetUpdates(string characterId, [FromQuery] long lastUpdateTick = 0)
    {
        try
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return BadRequest(new ApiResponse<GameStateUpdateDto>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var updates = await _gameStateService.GetUpdatesAsync(characterId, lastUpdateTick);
            
            return Ok(new ApiResponse<GameStateUpdateDto>
            {
                Success = true,
                Message = "获取状态更新成功",
                Data = updates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting updates for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<GameStateUpdateDto>
            {
                Success = false,
                Message = "获取状态更新时发生错误"
            });
        }
    }

    /// <summary>
    /// 重置角色状态到空闲
    /// </summary>
    [HttpPost("reset")]
    public async Task<ActionResult<ApiResponse<string>>> ResetCharacterState(ResetCharacterStateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var result = await _gameStateService.ResetCharacterStateAsync(request);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting character state for {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "重置角色状态时发生错误"
            });
        }
    }
}