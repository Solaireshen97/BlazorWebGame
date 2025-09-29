using BlazorWebGame.Server.Services;
using BlazorWebGame.Server.Security;
using BlazorWebGame.Server.Validation;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BlazorWebGame.Server.Controllers;

/// <summary>
/// 战斗控制器，处理所有战斗相关的API请求
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BattleController : ControllerBase
{
    private readonly GameEngineService _gameEngine;
    private readonly ServerPartyService _partyService;
    private readonly GameAuthenticationService _authService;
    private readonly DemoUserService _userService;
    private readonly ILogger<BattleController> _logger;

    public BattleController(
        GameEngineService gameEngine, 
        ServerPartyService partyService,
        GameAuthenticationService authService,
        DemoUserService userService,
        ILogger<BattleController> logger)
    {
        _gameEngine = gameEngine;
        _partyService = partyService;
        _authService = authService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 开始新战斗
    /// </summary>
    [HttpPost("start")]
    [ValidateResourceOwnership("CharacterId", ResourceType.Character)]
    [ValidateGameState(GameStateValidationType.BattleCanStart)]
    public ActionResult<ApiResponse<BattleStateDto>> StartBattle(StartBattleRequest request)
    {
        var userId = _authService.GetUserId(User);
        var clientIp = GetClientIpAddress();
        
        try
        {
            // 安全校验：验证请求数据
            if (string.IsNullOrEmpty(request.CharacterId) || string.IsNullOrEmpty(request.EnemyId))
            {
                _logger.LogWarning("Invalid battle start request from user {UserId} at {ClientIp}: missing required fields", 
                    userId, clientIp);
                
                return BadRequest(new ApiResponse<BattleStateDto>
                {
                    Success = false,
                    Message = "CharacterId and EnemyId are required",
                    Timestamp = DateTime.UtcNow
                });
            }

            // 验证角色归属权
            if (!_userService.UserHasCharacter(userId ?? "", request.CharacterId))
            {
                _logger.LogWarning("Unauthorized battle start attempt: User {UserId} does not own character {CharacterId}", 
                    userId, request.CharacterId);
                
                return Forbid("You don't own this character");
            }

            // 如果指定了PartyId，验证组队权限
            if (!string.IsNullOrEmpty(request.PartyId) && Guid.TryParse(request.PartyId, out var partyGuid))
            {
                // 检查角色是否可以发起组队战斗（通常只有队长可以）
                if (!_partyService.CanStartPartyBattle(request.CharacterId))
                {
                    _logger.LogWarning("Party battle start denied: Character {CharacterId} cannot start party battles", 
                        request.CharacterId);
                    
                    return BadRequest(new ApiResponse<BattleStateDto>
                    {
                        Success = false,
                        Message = "Only party leader can start party battles, or character is not in the specified party",
                        Timestamp = DateTime.UtcNow
                    });
                }

                _logger.LogInformation("Starting party battle for party {PartyId} by user {UserId} with character {CharacterId}", 
                    partyGuid, userId, request.CharacterId);
            }

            var battleState = _gameEngine.StartBattle(request);
            
            _logger.LogInformation("Battle {BattleId} started successfully by user {UserId} with character {CharacterId} from {ClientIp}", 
                battleState.BattleId, userId, request.CharacterId, clientIp);
            
            return Ok(new ApiResponse<BattleStateDto>
            {
                Success = true,
                Data = battleState,
                Message = "Battle started successfully",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid battle start request from user {UserId}: {Error}", userId, ex.Message);
            return BadRequest(new ApiResponse<BattleStateDto>
            {
                Success = false,
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Battle start failed for user {UserId}: {Error}", userId, ex.Message);
            return Conflict(new ApiResponse<BattleStateDto>
            {
                Success = false,
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting battle for user {UserId} with character {CharacterId}", 
                userId, request.CharacterId);
            
            return StatusCode(500, new ApiResponse<BattleStateDto>
            {
                Success = false,
                Message = "Internal server error",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 获取战斗状态
    /// </summary>
    [HttpGet("state/{battleId}")]
    [ValidateResourceOwnership("battleId", ResourceType.Battle)]
    public ActionResult<ApiResponse<BattleStateDto>> GetBattleState(Guid battleId)
    {
        var userId = _authService.GetUserId(User);
        
        try
        {
            var battleState = _gameEngine.GetBattleState(battleId);
            
            if (battleState == null)
            {
                _logger.LogWarning("Battle state request for non-existent battle {BattleId} by user {UserId}", 
                    battleId, userId);
                
                return NotFound(new ApiResponse<BattleStateDto>
                {
                    Success = false,
                    Message = "Battle not found",
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(new ApiResponse<BattleStateDto>
            {
                Success = true,
                Data = battleState,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting battle state for {BattleId} by user {UserId}", battleId, userId);
            return StatusCode(500, new ApiResponse<BattleStateDto>
            {
                Success = false,
                Message = "Internal server error",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 执行战斗动作
    /// </summary>
    [HttpPost("action")]
    [ValidateResourceOwnership("PlayerId", ResourceType.Character)]
    [ValidateGameState(GameStateValidationType.BattleIsActive)]
    public ActionResult<ApiResponse<bool>> ExecuteBattleAction(BattleActionRequest request)
    {
        var userId = _authService.GetUserId(User);
        
        try
        {
            // 验证请求数据
            if (request.BattleId == Guid.Empty || string.IsNullOrEmpty(request.PlayerId))
            {
                _logger.LogWarning("Invalid battle action request from user {UserId}: missing required fields", userId);
                
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "BattleId and PlayerId are required",
                    Timestamp = DateTime.UtcNow
                });
            }

            // 验证玩家归属权
            if (!_userService.UserHasCharacter(userId ?? "", request.PlayerId))
            {
                _logger.LogWarning("Unauthorized battle action attempt: User {UserId} does not own character {PlayerId}", 
                    userId, request.PlayerId);
                
                return Forbid("You don't own this character");
            }

            var success = _gameEngine.ExecuteBattleAction(request);
            
            if (success)
            {
                _logger.LogDebug("Battle action executed successfully for user {UserId} in battle {BattleId}", 
                    userId, request.BattleId);
                
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Battle action executed successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("Battle action failed for user {UserId} in battle {BattleId}", 
                    userId, request.BattleId);
                
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to execute battle action",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing battle action for user {UserId} in battle {BattleId}", 
                userId, request.BattleId);
            
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = "Internal server error",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 停止战斗
    /// </summary>
    [HttpPost("stop/{battleId}")]
    [ValidateResourceOwnership("battleId", ResourceType.Battle)]
    public ActionResult<ApiResponse<bool>> StopBattle(Guid battleId)
    {
        var userId = _authService.GetUserId(User);
        
        try
        {
            var success = _gameEngine.StopBattle(battleId);
            
            if (success)
            {
                _logger.LogInformation("Battle {BattleId} stopped by user {UserId}", battleId, userId);
            }
            else
            {
                _logger.LogWarning("Failed to stop battle {BattleId} by user {UserId}: battle not found", 
                    battleId, userId);
            }
            
            return Ok(new ApiResponse<bool>
            {
                Success = success,
                Data = success,
                Message = success ? "Battle stopped successfully" : "Battle not found",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping battle {BattleId} by user {UserId}", battleId, userId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Internal server error",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 获取用户的活跃战斗列表
    /// </summary>
    [HttpGet("active")]
    public ActionResult<ApiResponse<List<BattleStateDto>>> GetActiveBattles()
    {
        var userId = _authService.GetUserId(User);
        
        try
        {
            var allBattles = _gameEngine.GetAllBattleUpdates();
            
            // 只返回用户参与的战斗
            var userBattles = allBattles.Where(b => 
                b.PartyMemberIds.Contains(userId ?? "") || 
                b.CharacterId == userId).ToList();

            _logger.LogDebug("Retrieved {Count} active battles for user {UserId}", userBattles.Count, userId);

            return Ok(new ApiResponse<List<BattleStateDto>>
            {
                Success = true,
                Data = userBattles,
                Message = $"Retrieved {userBattles.Count} active battles",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active battles for user {UserId}", userId);
            return StatusCode(500, new ApiResponse<List<BattleStateDto>>
            {
                Success = false,
                Message = "Internal server error",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string GetClientIpAddress()
    {
        var xForwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}