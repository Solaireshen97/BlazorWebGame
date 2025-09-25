using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebGame.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BattleController : ControllerBase
{
    private readonly GameEngineService _gameEngine;
    private readonly ILogger<BattleController> _logger;

    public BattleController(GameEngineService gameEngine, ILogger<BattleController> logger)
    {
        _gameEngine = gameEngine;
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