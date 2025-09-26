using System.ComponentModel.DataAnnotations;
using System.Linq;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebGame.Server.Controllers;

/// <summary>
/// 离线结算控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OfflineSettlementController : ControllerBase
{
    private readonly OfflineSettlementService _offlineSettlementService;
    private readonly ILogger<OfflineSettlementController> _logger;

    public OfflineSettlementController(
        OfflineSettlementService offlineSettlementService,
        ILogger<OfflineSettlementController> logger)
    {
        _offlineSettlementService = offlineSettlementService;
        _logger = logger;
    }

    /// <summary>
    /// 安全地截取ID用于日志记录，防止日志注入攻击
    /// </summary>
    private static string SafeLogId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "[empty]";
        
        // 只保留字母数字和连字符，并截取前8位
        var sanitized = new string(id.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return sanitized.Substring(0, Math.Min(8, sanitized.Length)) + (sanitized.Length > 8 ? "..." : "");
    }

    /// <summary>
    /// 处理单个玩家的离线结算
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <returns>离线结算结果</returns>
    [HttpPost("player/{playerId}")]
    public async Task<ActionResult<ApiResponse<OfflineSettlementResultDto>>> ProcessPlayerOfflineSettlement(
        [Required] string playerId)
    {
        try
        {
            var result = await _offlineSettlementService.ProcessPlayerOfflineSettlementAsync(playerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理玩家 {PlayerId} 离线结算时发生错误", SafeLogId(playerId));
            return StatusCode(500, new ApiResponse<OfflineSettlementResultDto>
            {
                Success = false,
                Message = "内部服务器错误"
            });
        }
    }

    /// <summary>
    /// 处理队伍的离线结算
    /// </summary>
    /// <param name="teamId">队伍ID</param>
    /// <returns>队伍离线结算结果列表</returns>
    [HttpPost("team/{teamId}")]
    public async Task<ActionResult<ApiResponse<List<OfflineSettlementResultDto>>>> ProcessTeamOfflineSettlement(
        [Required] string teamId)
    {
        try
        {
            var result = await _offlineSettlementService.ProcessTeamOfflineSettlementAsync(teamId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理队伍 {TeamId} 离线结算时发生错误", SafeLogId(teamId));
            return StatusCode(500, new ApiResponse<List<OfflineSettlementResultDto>>
            {
                Success = false,
                Message = "内部服务器错误"
            });
        }
    }

    /// <summary>
    /// 批量处理多个玩家的离线结算
    /// </summary>
    /// <param name="request">批量结算请求</param>
    /// <returns>批量结算结果</returns>
    [HttpPost("batch")]
    public async Task<ActionResult<BatchOperationResponseDto<OfflineSettlementResultDto>>> ProcessBatchOfflineSettlement(
        [FromBody] BatchOfflineSettlementRequestDto request)
    {
        try
        {
            if (request.PlayerIds == null || !request.PlayerIds.Any())
            {
                return BadRequest(new BatchOperationResponseDto<OfflineSettlementResultDto>
                {
                    TotalProcessed = 0,
                    SuccessCount = 0,
                    ErrorCount = 1,
                    Errors = new List<string> { "玩家ID列表不能为空" }
                });
            }

            var result = await _offlineSettlementService.ProcessBatchOfflineSettlementAsync(request.PlayerIds);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量处理离线结算时发生错误");
            return StatusCode(500, new BatchOperationResponseDto<OfflineSettlementResultDto>
            {
                TotalProcessed = request.PlayerIds?.Count ?? 0,
                SuccessCount = 0,
                ErrorCount = 1,
                Errors = new List<string> { "内部服务器错误" }
            });
        }
    }

    /// <summary>
    /// 获取玩家的离线时间信息
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <returns>离线时间信息</returns>
    [HttpGet("player/{playerId}/offline-info")]
    public async Task<ActionResult<ApiResponse<PlayerOfflineInfoDto>>> GetPlayerOfflineInfo(
        [Required] string playerId)
    {
        try
        {
            // 这里可以实现获取玩家离线信息的逻辑
            // 暂时返回基本信息
            return Ok(new ApiResponse<PlayerOfflineInfoDto>
            {
                Success = true,
                Message = "获取离线信息成功",
                Data = new PlayerOfflineInfoDto
                {
                    PlayerId = playerId,
                    IsOffline = true, // 这里应该根据实际情况判断
                    OfflineTime = TimeSpan.FromHours(1), // 示例数据
                    EstimatedRewards = new OfflineRewardDto
                    {
                        Type = "Estimated",
                        Description = "预估奖励",
                        Experience = 50,
                        Gold = 10
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取玩家 {PlayerId} 离线信息时发生错误", SafeLogId(playerId));
            return StatusCode(500, new ApiResponse<PlayerOfflineInfoDto>
            {
                Success = false,
                Message = "内部服务器错误"
            });
        }
    }

    /// <summary>
    /// 获取服务器离线结算统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<OfflineSettlementStatisticsDto>>> GetOfflineSettlementStatistics()
    {
        try
        {
            // 这里可以实现获取离线结算统计信息的逻辑
            return Ok(new ApiResponse<OfflineSettlementStatisticsDto>
            {
                Success = true,
                Message = "获取统计信息成功",
                Data = new OfflineSettlementStatisticsDto
                {
                    TotalSettlements = 0,
                    TotalPlayersProcessed = 0,
                    AverageOfflineTime = TimeSpan.Zero,
                    TotalExperienceDistributed = 0,
                    TotalGoldDistributed = 0,
                    LastStatisticsUpdate = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取离线结算统计信息时发生错误");
            return StatusCode(500, new ApiResponse<OfflineSettlementStatisticsDto>
            {
                Success = false,
                Message = "内部服务器错误"
            });
        }
    }

    /// <summary>
    /// 同步离线战斗进度
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="request">离线战斗进度数据</param>
    /// <returns>同步结果</returns>
    [HttpPost("player/{playerId}/battle-progress")]
    public async Task<ActionResult<ApiResponse<object>>> SyncOfflineBattleProgress(
        [Required] string playerId, 
        [FromBody] OfflineBattleProgressSyncRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "请求数据不能为空"
                });
            }

            request.PlayerId = playerId;

            // 记录离线战斗进度同步
            _logger.LogInformation("同步玩家 {PlayerId} 的离线战斗进度: {BattleCount} 场战斗, {Experience} 经验, {Gold} 金币", 
                SafeLogId(playerId), 
                request.EstimatedBattles, 
                request.EstimatedExperience, 
                request.EstimatedGold);

            // 在这里可以添加实际的离线战斗进度处理逻辑
            // 例如：验证进度合理性、应用奖励、更新角色状态等

            var result = new
            {
                PlayerId = playerId,
                ProcessedBattles = request.EstimatedBattles,
                ExperienceAwarded = request.EstimatedExperience,
                GoldAwarded = request.EstimatedGold,
                SyncTime = DateTime.UtcNow
            };

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = result,
                Message = "离线战斗进度同步成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步玩家 {PlayerId} 离线战斗进度时发生错误", SafeLogId(playerId));
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "内部服务器错误"
            });
        }
    }
}

/// <summary>
/// 玩家离线信息传输对象
/// </summary>
public class PlayerOfflineInfoDto
{
    public string PlayerId { get; set; } = string.Empty;
    public bool IsOffline { get; set; }
    public TimeSpan OfflineTime { get; set; }
    public OfflineRewardDto EstimatedRewards { get; set; } = new();
    public DateTime LastActiveTime { get; set; }
}

/// <summary>
/// 离线结算统计信息传输对象
/// </summary>
public class OfflineSettlementStatisticsDto
{
    public long TotalSettlements { get; set; }
    public long TotalPlayersProcessed { get; set; }
    public TimeSpan AverageOfflineTime { get; set; }
    public long TotalExperienceDistributed { get; set; }
    public long TotalGoldDistributed { get; set; }
    public DateTime LastStatisticsUpdate { get; set; }
}