using BlazorWebGame.Rebuild.Services.Party;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebGame.Rebuild.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PartyController : ControllerBase
{
    private readonly ServerPartyService _partyService;
    private readonly ILogger<PartyController> _logger;

    public PartyController(ServerPartyService partyService, ILogger<PartyController> logger)
    {
        _partyService = partyService;
        _logger = logger;
    }

    /// <summary>
    /// 创建新组队
    /// </summary>
    [HttpPost("create")]
    public ActionResult<ApiResponse<PartyDto>> CreateParty(CreatePartyRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId))
            {
                return BadRequest(new ApiResponse<PartyDto>
                {
                    Success = false,
                    Message = "CharacterId is required"
                });
            }

            var party = _partyService.CreateParty(request.CharacterId);
            
            if (party == null)
            {
                return BadRequest(new ApiResponse<PartyDto>
                {
                    Success = false,
                    Message = "Character is already in a party or failed to create party"
                });
            }

            _logger.LogInformation("Party created by character {CharacterId}", request.CharacterId);

            return Ok(new ApiResponse<PartyDto>
            {
                Success = true,
                Data = party,
                Message = "Party created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating party for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<PartyDto>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// 加入组队
    /// </summary>
    [HttpPost("join")]
    public ActionResult<ApiResponse<bool>> JoinParty(JoinPartyRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "CharacterId is required"
                });
            }

            var success = _partyService.JoinParty(request.CharacterId, request.PartyId);

            if (success)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Successfully joined party"
                });
            }
            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to join party - party may be full or character already in a party"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining party for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// 离开组队
    /// </summary>
    [HttpPost("leave")]
    public ActionResult<ApiResponse<bool>> LeaveParty(LeavePartyRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "CharacterId is required"
                });
            }

            var success = _partyService.LeaveParty(request.CharacterId);

            return Ok(new ApiResponse<bool>
            {
                Success = success,
                Data = success,
                Message = success ? "Successfully left party" : "Character is not in any party"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving party for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// 获取角色的组队信息
    /// </summary>
    [HttpGet("character/{characterId}")]
    public ActionResult<ApiResponse<PartyDto>> GetPartyForCharacter(string characterId)
    {
        try
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return BadRequest(new ApiResponse<PartyDto>
                {
                    Success = false,
                    Message = "CharacterId is required"
                });
            }

            var party = _partyService.GetPartyForCharacter(characterId);

            if (party == null)
            {
                return Ok(new ApiResponse<PartyDto>
                {
                    Success = true,
                    Data = null,
                    Message = "Character is not in any party"
                });
            }

            return Ok(new ApiResponse<PartyDto>
            {
                Success = true,
                Data = party
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting party for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<PartyDto>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// 获取所有组队列表
    /// </summary>
    [HttpGet("all")]
    public ActionResult<ApiResponse<List<PartyDto>>> GetAllParties()
    {
        try
        {
            var parties = _partyService.GetAllParties();

            return Ok(new ApiResponse<List<PartyDto>>
            {
                Success = true,
                Data = parties
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all parties");
            return StatusCode(500, new ApiResponse<List<PartyDto>>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// 根据ID获取组队信息
    /// </summary>
    [HttpGet("{partyId}")]
    public ActionResult<ApiResponse<PartyDto>> GetParty(Guid partyId)
    {
        try
        {
            var party = _partyService.GetParty(partyId);

            if (party == null)
            {
                return NotFound(new ApiResponse<PartyDto>
                {
                    Success = false,
                    Message = "Party not found"
                });
            }

            return Ok(new ApiResponse<PartyDto>
            {
                Success = true,
                Data = party
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting party {PartyId}", partyId);
            return StatusCode(500, new ApiResponse<PartyDto>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// 更新队伍进度（离线同步专用）
    /// </summary>
    [HttpPut("{partyId}/progress")]
    public ActionResult<ApiResponse<object>> UpdateTeamProgress(string partyId, TeamProgressUpdateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(partyId))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "PartyId is required"
                });
            }

            request.PartyId = partyId;

            // 查找对应的组队
            if (!Guid.TryParse(partyId, out var partyGuid))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid PartyId format"
                });
            }

            var party = _partyService.GetParty(partyGuid);
            if (party == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Party not found"
                });
            }

            // 在这里可以添加更复杂的队伍进度更新逻辑
            // 目前简单记录日志并返回成功
            _logger.LogInformation("Team progress updated for party {PartyId} with {DataCount} progress items", 
                partyId, request.ProgressData?.Count ?? 0);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new 
                { 
                    Updated = true, 
                    PartyId = partyId,
                    UpdatedAt = DateTime.UtcNow
                },
                Message = "队伍进度更新成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating team progress for party {PartyId}", partyId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "更新队伍进度失败"
            });
        }
    }
}