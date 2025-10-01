using BlazorWebGame.Server.Services.Reputation;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace BlazorWebGame.Server.Controllers;

/// <summary>
/// 声望系统控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReputationController : ControllerBase
{
    private readonly ServerReputationService _reputationService;
    private readonly ILogger<ReputationController> _logger;

    public ReputationController(ServerReputationService reputationService, ILogger<ReputationController> logger)
    {
        _reputationService = reputationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取角色的声望信息
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <returns>声望信息</returns>
    [HttpGet("{characterId}")]
    public async Task<ActionResult<ApiResponse<ReputationDto>>> GetReputation([Required] string characterId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return BadRequest(new ApiResponse<ReputationDto>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var reputation = await _reputationService.GetReputationAsync(characterId);
            
            return Ok(new ApiResponse<ReputationDto>
            {
                Success = true,
                Data = reputation,
                Message = "获取声望信息成功"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid character ID: {CharacterId}", characterId);
            return NotFound(new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = "获取声望信息时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取指定阵营的详细声望信息
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="factionName">阵营名称</param>
    /// <returns>详细声望信息</returns>
    [HttpGet("{characterId}/faction/{factionName}")]
    public async Task<ActionResult<ApiResponse<ReputationDetailDto>>> GetReputationDetail(
        [Required] string characterId, 
        [Required] string factionName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(factionName))
            {
                return BadRequest(new ApiResponse<ReputationDetailDto>
                {
                    Success = false,
                    Message = "角色ID和阵营名称不能为空"
                });
            }

            var detail = await _reputationService.GetReputationDetailAsync(characterId, factionName);
            
            return Ok(new ApiResponse<ReputationDetailDto>
            {
                Success = true,
                Data = detail,
                Message = "获取声望详情成功"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters: CharacterId={CharacterId}, FactionName={FactionName}", characterId, factionName);
            return NotFound(new ApiResponse<ReputationDetailDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation detail for character {CharacterId}, faction {FactionName}", characterId, factionName);
            return StatusCode(500, new ApiResponse<ReputationDetailDto>
            {
                Success = false,
                Message = "获取声望详情时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取所有阵营的详细声望信息
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <returns>所有阵营的详细声望信息列表</returns>
    [HttpGet("{characterId}/details")]
    public async Task<ActionResult<ApiResponse<System.Collections.Generic.List<ReputationDetailDto>>>> GetAllReputationDetails([Required] string characterId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return BadRequest(new ApiResponse<System.Collections.Generic.List<ReputationDetailDto>>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var details = await _reputationService.GetAllReputationDetailsAsync(characterId);
            
            return Ok(new ApiResponse<System.Collections.Generic.List<ReputationDetailDto>>
            {
                Success = true,
                Data = details,
                Message = "获取所有声望详情成功"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid character ID: {CharacterId}", characterId);
            return NotFound(new ApiResponse<System.Collections.Generic.List<ReputationDetailDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all reputation details for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<System.Collections.Generic.List<ReputationDetailDto>>
            {
                Success = false,
                Message = "获取声望详情时发生错误"
            });
        }
    }

    /// <summary>
    /// 更新角色声望
    /// </summary>
    /// <param name="request">更新声望请求</param>
    /// <returns>更新后的声望信息</returns>
    [HttpPost("update")]
    public async Task<ActionResult<ApiResponse<ReputationDto>>> UpdateReputation([FromBody] UpdateReputationRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse<ReputationDto>
                {
                    Success = false,
                    Message = "请求数据不能为空"
                });
            }

            if (string.IsNullOrWhiteSpace(request.CharacterId) || string.IsNullOrWhiteSpace(request.FactionName))
            {
                return BadRequest(new ApiResponse<ReputationDto>
                {
                    Success = false,
                    Message = "角色ID和阵营名称不能为空"
                });
            }

            var updatedReputation = await _reputationService.UpdateReputationAsync(request);
            
            return Ok(new ApiResponse<ReputationDto>
            {
                Success = true,
                Data = updatedReputation,
                Message = $"成功更新 {request.FactionName} 声望 {request.Amount:+#;-#;0} 点"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid update request: {CharacterId}, {FactionName}", request?.CharacterId, request?.FactionName);
            return NotFound(new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reputation for character {CharacterId}", request?.CharacterId);
            return StatusCode(500, new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = "更新声望时发生错误"
            });
        }
    }

    /// <summary>
    /// 批量更新角色声望
    /// </summary>
    /// <param name="request">批量更新声望请求</param>
    /// <returns>更新后的声望信息</returns>
    [HttpPost("batch-update")]
    public async Task<ActionResult<ApiResponse<ReputationDto>>> BatchUpdateReputation([FromBody] BatchUpdateReputationRequest request)
    {
        try
        {
            if (request == null || request.Changes == null || request.Changes.Count == 0)
            {
                return BadRequest(new ApiResponse<ReputationDto>
                {
                    Success = false,
                    Message = "请求数据或变更列表不能为空"
                });
            }

            if (string.IsNullOrWhiteSpace(request.CharacterId))
            {
                return BadRequest(new ApiResponse<ReputationDto>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var updatedReputation = await _reputationService.BatchUpdateReputationAsync(request);
            
            return Ok(new ApiResponse<ReputationDto>
            {
                Success = true,
                Data = updatedReputation,
                Message = $"成功批量更新 {request.Changes.Count} 个阵营的声望"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid batch update request: {CharacterId}", request?.CharacterId);
            return NotFound(new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch updating reputation for character {CharacterId}", request?.CharacterId);
            return StatusCode(500, new ApiResponse<ReputationDto>
            {
                Success = false,
                Message = "批量更新声望时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取声望奖励信息
    /// </summary>
    /// <param name="request">声望奖励查询请求</param>
    /// <returns>声望奖励信息列表</returns>
    [HttpPost("rewards")]
    public async Task<ActionResult<ApiResponse<System.Collections.Generic.List<ReputationRewardDto>>>> GetReputationRewards([FromBody] ReputationRewardsRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CharacterId))
            {
                return BadRequest(new ApiResponse<System.Collections.Generic.List<ReputationRewardDto>>
                {
                    Success = false,
                    Message = "请求数据或角色ID不能为空"
                });
            }

            var rewards = await _reputationService.GetReputationRewardsAsync(request);
            
            return Ok(new ApiResponse<System.Collections.Generic.List<ReputationRewardDto>>
            {
                Success = true,
                Data = rewards,
                Message = "获取声望奖励信息成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation rewards for character {CharacterId}", request?.CharacterId);
            return StatusCode(500, new ApiResponse<System.Collections.Generic.List<ReputationRewardDto>>
            {
                Success = false,
                Message = "获取声望奖励时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取角色可获得的声望奖励
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <returns>可获得的奖励列表</returns>
    [HttpGet("{characterId}/available-rewards")]
    public async Task<ActionResult<ApiResponse<System.Collections.Generic.List<ReputationRewardDto>>>> GetAvailableRewards([Required] string characterId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return BadRequest(new ApiResponse<System.Collections.Generic.List<ReputationRewardDto>>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var rewards = await _reputationService.GetAvailableRewardsAsync(characterId);
            
            return Ok(new ApiResponse<System.Collections.Generic.List<ReputationRewardDto>>
            {
                Success = true,
                Data = rewards,
                Message = "获取可用奖励信息成功"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid character ID: {CharacterId}", characterId);
            return NotFound(new ApiResponse<System.Collections.Generic.List<ReputationRewardDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available rewards for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<System.Collections.Generic.List<ReputationRewardDto>>
            {
                Success = false,
                Message = "获取可用奖励时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取声望统计信息
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <returns>声望统计信息</returns>
    [HttpGet("{characterId}/stats")]
    public async Task<ActionResult<ApiResponse<ReputationStatsDto>>> GetReputationStats([Required] string characterId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return BadRequest(new ApiResponse<ReputationStatsDto>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var stats = await _reputationService.GetReputationStatsAsync(characterId);
            
            return Ok(new ApiResponse<ReputationStatsDto>
            {
                Success = true,
                Data = stats,
                Message = "获取声望统计信息成功"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid character ID: {CharacterId}", characterId);
            return NotFound(new ApiResponse<ReputationStatsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation stats for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<ReputationStatsDto>
            {
                Success = false,
                Message = "获取声望统计时发生错误"
            });
        }
    }
}