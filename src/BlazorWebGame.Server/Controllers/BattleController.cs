using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebGame.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BattleController : ControllerBase
{
    private readonly GameEngineService _gameEngine;
    private readonly ServerPartyService _partyService;
    private readonly ILogger<BattleController> _logger;

    public BattleController(GameEngineService gameEngine, ServerPartyService partyService, ILogger<BattleController> logger)
    {
        _gameEngine = gameEngine;
        _partyService = partyService;
        _logger = logger;
    }

    /// <summary>
    /// 开始新战斗
    /// </summary>
    [HttpPost("start")]
    public ActionResult<ApiResponse<BattleStateDto>> StartBattle(StartBattleRequest request)
    {
        try
        {
            // 简单验证请求数据
            if (string.IsNullOrEmpty(request.CharacterId) || string.IsNullOrEmpty(request.EnemyId))
            {
                return BadRequest(new ApiResponse<BattleStateDto>
                {
                    Success = false,
                    Message = "CharacterId and EnemyId are required"
                });
            }

            // 如果指定了PartyId，验证组队权限
            if (!string.IsNullOrEmpty(request.PartyId) && Guid.TryParse(request.PartyId, out var partyGuid))
            {
                // 检查角色是否可以发起组队战斗（通常只有队长可以）
                if (!_partyService.CanStartPartyBattle(request.CharacterId))
                {
                    return BadRequest(new ApiResponse<BattleStateDto>
                    {
                        Success = false,
                        Message = "Only party leader can start party battles, or character is not in the specified party"
                    });
                }

                _logger.LogInformation("Starting party battle for party {PartyId} by character {CharacterId}", 
                    partyGuid, request.CharacterId);
            }

            var battleState = _gameEngine.StartBattle(request);
            
            _logger.LogInformation("Battle started for character {CharacterId}", request.CharacterId);
            
            return Ok(new ApiResponse<BattleStateDto>
            {
                Success = true,
                Data = battleState,
                Message = "Battle started successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting battle for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<BattleStateDto>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// 获取战斗状态
    /// </summary>
    [HttpGet("state/{battleId}")]
    public ActionResult<ApiResponse<BattleStateDto>> GetBattleState(Guid battleId)
    {
        try
        {
            var battleState = _gameEngine.GetBattleState(battleId);
            
            if (battleState == null)
            {
                return NotFound(new ApiResponse<BattleStateDto>
                {
                    Success = false,
                    Message = "Battle not found"
                });
            }

            return Ok(new ApiResponse<BattleStateDto>
            {
                Success = true,
                Data = battleState
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting battle state for {BattleId}", battleId);
            return StatusCode(500, new ApiResponse<BattleStateDto>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// 执行战斗动作
    /// </summary>
    [HttpPost("action")]
    public ActionResult<ApiResponse<bool>> ExecuteBattleAction(BattleActionRequest request)
    {
        try
        {
            var success = _gameEngine.ExecuteBattleAction(request);
            
            if (success)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Battle action executed successfully"
                });
            }
            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to execute battle action"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing battle action for battle {BattleId}", request.BattleId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// 停止战斗
    /// </summary>
    [HttpPost("stop/{battleId}")]
    public ActionResult<ApiResponse<bool>> StopBattle(Guid battleId)
    {
        try
        {
            var success = _gameEngine.StopBattle(battleId);
            
            return Ok(new ApiResponse<bool>
            {
                Success = success,
                Data = success,
                Message = success ? "Battle stopped successfully" : "Battle not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping battle {BattleId}", battleId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }
}