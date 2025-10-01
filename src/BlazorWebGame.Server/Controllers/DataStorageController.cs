using Microsoft.AspNetCore.Mvc;
using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Shared.DTOs;
using System.ComponentModel.DataAnnotations;

namespace BlazorWebGame.Server.Controllers;

/// <summary>
/// 数据存储服务API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DataStorageController : ControllerBase
{
    private readonly IDataStorageService _dataStorageService;
    private readonly ILogger<DataStorageController> _logger;

    public DataStorageController(IDataStorageService dataStorageService, ILogger<DataStorageController> logger)
    {
        _dataStorageService = dataStorageService;
        _logger = logger;
    }

    #region 玩家数据管理

    /// <summary>
    /// 获取玩家数据
    /// </summary>
    [HttpGet("players/{playerId}")]
    public async Task<ActionResult<PlayerStorageDto>> GetPlayer([Required] string playerId)
    {
        var player = await _dataStorageService.GetPlayerAsync(playerId);
        if (player == null)
        {
            return NotFound($"玩家 {playerId} 不存在");
        }
        return Ok(player);
    }

    /// <summary>
    /// 保存玩家数据
    /// </summary>
    [HttpPost("players")]
    public async Task<ActionResult<ApiResponse<PlayerStorageDto>>> SavePlayer([FromBody] PlayerStorageDto player)
    {
        var result = await _dataStorageService.SavePlayerAsync(player);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// 删除玩家数据
    /// </summary>
    [HttpDelete("players/{playerId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePlayer([Required] string playerId)
    {
        var result = await _dataStorageService.DeletePlayerAsync(playerId);
        return Ok(result);
    }

    /// <summary>
    /// 获取在线玩家列表
    /// </summary>
    [HttpGet("players/online")]
    public async Task<ActionResult<ApiResponse<List<PlayerStorageDto>>>> GetOnlinePlayers()
    {
        var result = await _dataStorageService.GetOnlinePlayersAsync();
        return Ok(result);
    }

    /// <summary>
    /// 批量保存玩家数据
    /// </summary>
    [HttpPost("players/batch")]
    public async Task<ActionResult<BatchOperationResponseDto<PlayerStorageDto>>> SavePlayers([FromBody] List<PlayerStorageDto> players)
    {
        var result = await _dataStorageService.SavePlayersAsync(players);
        return Ok(result);
    }

    /// <summary>
    /// 搜索玩家
    /// </summary>
    [HttpGet("players/search")]
    public async Task<ActionResult<ApiResponse<List<PlayerStorageDto>>>> SearchPlayers([FromQuery] string searchTerm, [FromQuery] int limit = 20)
    {
        var result = await _dataStorageService.SearchPlayersAsync(searchTerm, limit);
        return Ok(result);
    }

    #endregion

    #region 队伍数据管理

    /// <summary>
    /// 获取队伍数据
    /// </summary>
    [HttpGet("teams/{teamId}")]
    public async Task<ActionResult<TeamStorageDto>> GetTeam([Required] string teamId)
    {
        var team = await _dataStorageService.GetTeamAsync(teamId);
        if (team == null)
        {
            return NotFound($"队伍 {teamId} 不存在");
        }
        return Ok(team);
    }

    /// <summary>
    /// 根据队长ID获取队伍
    /// </summary>
    [HttpGet("teams/captain/{captainId}")]
    public async Task<ActionResult<TeamStorageDto>> GetTeamByCaptain([Required] string captainId)
    {
        var team = await _dataStorageService.GetTeamByCaptainAsync(captainId);
        if (team == null)
        {
            return NotFound($"队长 {captainId} 没有队伍");
        }
        return Ok(team);
    }

    /// <summary>
    /// 根据玩家ID获取其所在队伍
    /// </summary>
    [HttpGet("teams/player/{playerId}")]
    public async Task<ActionResult<TeamStorageDto>> GetTeamByPlayer([Required] string playerId)
    {
        var team = await _dataStorageService.GetTeamByPlayerAsync(playerId);
        if (team == null)
        {
            return NotFound($"玩家 {playerId} 没有队伍");
        }
        return Ok(team);
    }

    /// <summary>
    /// 保存队伍数据
    /// </summary>
    [HttpPost("teams")]
    public async Task<ActionResult<ApiResponse<TeamStorageDto>>> SaveTeam([FromBody] TeamStorageDto team)
    {
        var result = await _dataStorageService.SaveTeamAsync(team);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// 删除队伍数据
    /// </summary>
    [HttpDelete("teams/{teamId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTeam([Required] string teamId)
    {
        var result = await _dataStorageService.DeleteTeamAsync(teamId);
        return Ok(result);
    }

    /// <summary>
    /// 获取活跃队伍列表
    /// </summary>
    [HttpGet("teams/active")]
    public async Task<ActionResult<ApiResponse<List<TeamStorageDto>>>> GetActiveTeams()
    {
        var result = await _dataStorageService.GetActiveTeamsAsync();
        return Ok(result);
    }

    #endregion

    #region 动作目标管理

    /// <summary>
    /// 获取玩家当前动作目标
    /// </summary>
    [HttpGet("action-targets/current/{playerId}")]
    public async Task<ActionResult<ActionTargetStorageDto>> GetCurrentActionTarget([Required] string playerId)
    {
        var actionTarget = await _dataStorageService.GetCurrentActionTargetAsync(playerId);
        if (actionTarget == null)
        {
            return NotFound($"玩家 {playerId} 没有当前动作目标");
        }
        return Ok(actionTarget);
    }

    /// <summary>
    /// 保存动作目标数据
    /// </summary>
    [HttpPost("action-targets")]
    public async Task<ActionResult<ApiResponse<ActionTargetStorageDto>>> SaveActionTarget([FromBody] ActionTargetStorageDto actionTarget)
    {
        var result = await _dataStorageService.SaveActionTargetAsync(actionTarget);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// 完成动作目标
    /// </summary>
    [HttpPost("action-targets/{actionTargetId}/complete")]
    public async Task<ActionResult<ApiResponse<bool>>> CompleteActionTarget([Required] string actionTargetId)
    {
        var result = await _dataStorageService.CompleteActionTargetAsync(actionTargetId);
        return Ok(result);
    }

    /// <summary>
    /// 取消动作目标
    /// </summary>
    [HttpDelete("action-targets/current/{playerId}")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelActionTarget([Required] string playerId)
    {
        var result = await _dataStorageService.CancelActionTargetAsync(playerId);
        return Ok(result);
    }

    /// <summary>
    /// 获取玩家历史动作目标
    /// </summary>
    [HttpGet("action-targets/history/{playerId}")]
    public async Task<ActionResult<ApiResponse<List<ActionTargetStorageDto>>>> GetPlayerActionHistory([Required] string playerId, [FromQuery] int limit = 50)
    {
        var result = await _dataStorageService.GetPlayerActionHistoryAsync(playerId, limit);
        return Ok(result);
    }

    #endregion

    #region 战斗记录管理

    /// <summary>
    /// 获取战斗记录
    /// </summary>
    [HttpGet("battle-records/{battleId}")]
    public async Task<ActionResult<BattleRecordStorageDto>> GetBattleRecord([Required] string battleId)
    {
        var battleRecord = await _dataStorageService.GetBattleRecordAsync(battleId);
        if (battleRecord == null)
        {
            return NotFound($"战斗记录 {battleId} 不存在");
        }
        return Ok(battleRecord);
    }

    /// <summary>
    /// 保存战斗记录
    /// </summary>
    [HttpPost("battle-records")]
    public async Task<ActionResult<ApiResponse<BattleRecordStorageDto>>> SaveBattleRecord([FromBody] BattleRecordStorageDto battleRecord)
    {
        var result = await _dataStorageService.SaveBattleRecordAsync(battleRecord);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// 结束战斗记录
    /// </summary>
    [HttpPost("battle-records/{battleId}/end")]
    public async Task<ActionResult<ApiResponse<bool>>> EndBattleRecord([Required] string battleId, [FromBody] EndBattleRecordRequest request)
    {
        var result = await _dataStorageService.EndBattleRecordAsync(battleId, request.Status, request.Results);
        return Ok(result);
    }

    /// <summary>
    /// 获取玩家战斗历史
    /// </summary>
    [HttpGet("battle-records/player/{playerId}")]
    public async Task<ActionResult<ApiResponse<List<BattleRecordStorageDto>>>> GetPlayerBattleHistory([Required] string playerId, [FromQuery] DataStorageQueryDto query)
    {
        var result = await _dataStorageService.GetPlayerBattleHistoryAsync(playerId, query);
        return Ok(result);
    }

    /// <summary>
    /// 获取队伍战斗历史
    /// </summary>
    [HttpGet("battle-records/team/{teamId}")]
    public async Task<ActionResult<ApiResponse<List<BattleRecordStorageDto>>>> GetTeamBattleHistory([Required] string teamId, [FromQuery] DataStorageQueryDto query)
    {
        var result = await _dataStorageService.GetTeamBattleHistoryAsync(teamId, query);
        return Ok(result);
    }

    /// <summary>
    /// 获取进行中的战斗记录
    /// </summary>
    [HttpGet("battle-records/active")]
    public async Task<ActionResult<ApiResponse<List<BattleRecordStorageDto>>>> GetActiveBattleRecords()
    {
        var result = await _dataStorageService.GetActiveBattleRecordsAsync();
        return Ok(result);
    }

    #endregion

    #region 离线数据管理

    /// <summary>
    /// 保存离线数据
    /// </summary>
    [HttpPost("offline-data")]
    public async Task<ActionResult<ApiResponse<OfflineDataStorageDto>>> SaveOfflineData([FromBody] OfflineDataStorageDto offlineData)
    {
        var result = await _dataStorageService.SaveOfflineDataAsync(offlineData);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// 获取未同步的离线数据
    /// </summary>
    [HttpGet("offline-data/unsynced/{playerId}")]
    public async Task<ActionResult<ApiResponse<List<OfflineDataStorageDto>>>> GetUnsyncedOfflineData([Required] string playerId)
    {
        var result = await _dataStorageService.GetUnsyncedOfflineDataAsync(playerId);
        return Ok(result);
    }

    /// <summary>
    /// 标记离线数据为已同步
    /// </summary>
    [HttpPost("offline-data/mark-synced")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkOfflineDataSynced([FromBody] List<string> offlineDataIds)
    {
        var result = await _dataStorageService.MarkOfflineDataSyncedAsync(offlineDataIds);
        return Ok(result);
    }

    /// <summary>
    /// 清理已同步的旧离线数据
    /// </summary>
    [HttpDelete("offline-data/cleanup")]
    public async Task<ActionResult<ApiResponse<int>>> CleanupSyncedOfflineData([FromQuery] DateTime olderThan)
    {
        var result = await _dataStorageService.CleanupSyncedOfflineDataAsync(olderThan);
        return Ok(result);
    }

    #endregion

    #region 数据查询和统计

    /// <summary>
    /// 获取数据存储统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object>>>> GetStorageStats()
    {
        var result = await _dataStorageService.GetStorageStatsAsync();
        return Ok(result);
    }

    /// <summary>
    /// 数据健康检查
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object>>>> HealthCheck()
    {
        var result = await _dataStorageService.HealthCheckAsync();
        return Ok(result);
    }

    #endregion

    #region 数据同步和备份

    /// <summary>
    /// 导出玩家数据
    /// </summary>
    [HttpGet("export/player/{playerId}")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object>>>> ExportPlayerData([Required] string playerId)
    {
        var result = await _dataStorageService.ExportPlayerDataAsync(playerId);
        return Ok(result);
    }

    /// <summary>
    /// 导入玩家数据
    /// </summary>
    [HttpPost("import/player/{playerId}")]
    public async Task<ActionResult<ApiResponse<bool>>> ImportPlayerData([Required] string playerId, [FromBody] Dictionary<string, object> data)
    {
        var result = await _dataStorageService.ImportPlayerDataAsync(playerId, data);
        return Ok(result);
    }

    /// <summary>
    /// 数据备份
    /// </summary>
    [HttpPost("backup")]
    public async Task<ActionResult<ApiResponse<string>>> BackupData()
    {
        var result = await _dataStorageService.BackupDataAsync();
        return Ok(result);
    }

    /// <summary>
    /// 数据清理 - 删除过期数据
    /// </summary>
    [HttpDelete("cleanup")]
    public async Task<ActionResult<ApiResponse<int>>> CleanupExpiredData([FromQuery] int olderThanDays = 30)
    {
        var olderThan = TimeSpan.FromDays(olderThanDays);
        var result = await _dataStorageService.CleanupExpiredDataAsync(olderThan);
        return Ok(result);
    }

    #endregion
}

/// <summary>
/// 结束战斗记录请求
/// </summary>
public class EndBattleRecordRequest
{
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> Results { get; set; } = new();
}