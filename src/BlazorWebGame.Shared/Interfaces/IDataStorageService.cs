using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 数据存储服务接口 - 支持离线战斗游戏的数据管理
/// </summary>
public interface IDataStorageService
{
    #region 玩家数据管理
    
    /// <summary>
    /// 获取玩家数据
    /// </summary>
    Task<PlayerStorageDto?> GetPlayerAsync(string playerId);
    
    /// <summary>
    /// 创建或更新玩家数据
    /// </summary>
    Task<ApiResponse<PlayerStorageDto>> SavePlayerAsync(PlayerStorageDto player);
    
    /// <summary>
    /// 删除玩家数据
    /// </summary>
    Task<ApiResponse<bool>> DeletePlayerAsync(string playerId);
    
    /// <summary>
    /// 获取所有在线玩家
    /// </summary>
    Task<ApiResponse<List<PlayerStorageDto>>> GetOnlinePlayersAsync();
    
    /// <summary>
    /// 批量保存玩家数据
    /// </summary>
    Task<BatchOperationResponseDto<PlayerStorageDto>> SavePlayersAsync(List<PlayerStorageDto> players);
    
    #endregion

    #region 队伍数据管理
    
    /// <summary>
    /// 获取队伍数据
    /// </summary>
    Task<TeamStorageDto?> GetTeamAsync(string teamId);
    
    /// <summary>
    /// 根据队长ID获取队伍
    /// </summary>
    Task<TeamStorageDto?> GetTeamByCaptainAsync(string captainId);
    
    /// <summary>
    /// 根据玩家ID获取其所在队伍
    /// </summary>
    Task<TeamStorageDto?> GetTeamByPlayerAsync(string playerId);
    
    /// <summary>
    /// 创建或更新队伍数据
    /// </summary>
    Task<ApiResponse<TeamStorageDto>> SaveTeamAsync(TeamStorageDto team);
    
    /// <summary>
    /// 删除队伍数据
    /// </summary>
    Task<ApiResponse<bool>> DeleteTeamAsync(string teamId);
    
    /// <summary>
    /// 获取活跃队伍列表
    /// </summary>
    Task<ApiResponse<List<TeamStorageDto>>> GetActiveTeamsAsync();
    
    #endregion

    #region 动作目标管理
    
    /// <summary>
    /// 获取玩家当前动作目标
    /// </summary>
    Task<ActionTargetStorageDto?> GetCurrentActionTargetAsync(string playerId);
    
    /// <summary>
    /// 保存动作目标数据
    /// </summary>
    Task<ApiResponse<ActionTargetStorageDto>> SaveActionTargetAsync(ActionTargetStorageDto actionTarget);
    
    /// <summary>
    /// 完成动作目标
    /// </summary>
    Task<ApiResponse<bool>> CompleteActionTargetAsync(string actionTargetId);
    
    /// <summary>
    /// 取消动作目标
    /// </summary>
    Task<ApiResponse<bool>> CancelActionTargetAsync(string playerId);
    
    /// <summary>
    /// 获取玩家历史动作目标
    /// </summary>
    Task<ApiResponse<List<ActionTargetStorageDto>>> GetPlayerActionHistoryAsync(string playerId, int limit = 50);
    
    #endregion

    #region 战斗记录管理
    
    /// <summary>
    /// 获取战斗记录
    /// </summary>
    Task<BattleRecordStorageDto?> GetBattleRecordAsync(string battleId);
    
    /// <summary>
    /// 保存战斗记录
    /// </summary>
    Task<ApiResponse<BattleRecordStorageDto>> SaveBattleRecordAsync(BattleRecordStorageDto battleRecord);
    
    /// <summary>
    /// 结束战斗记录
    /// </summary>
    Task<ApiResponse<bool>> EndBattleRecordAsync(string battleId, string status, Dictionary<string, object> results);
    
    /// <summary>
    /// 获取玩家战斗历史
    /// </summary>
    Task<ApiResponse<List<BattleRecordStorageDto>>> GetPlayerBattleHistoryAsync(string playerId, DataStorageQueryDto query);
    
    /// <summary>
    /// 获取队伍战斗历史
    /// </summary>
    Task<ApiResponse<List<BattleRecordStorageDto>>> GetTeamBattleHistoryAsync(string teamId, DataStorageQueryDto query);
    
    /// <summary>
    /// 获取进行中的战斗记录
    /// </summary>
    Task<ApiResponse<List<BattleRecordStorageDto>>> GetActiveBattleRecordsAsync();
    
    #endregion

    #region 离线数据管理
    
    /// <summary>
    /// 保存离线数据
    /// </summary>
    Task<ApiResponse<OfflineDataStorageDto>> SaveOfflineDataAsync(OfflineDataStorageDto offlineData);
    
    /// <summary>
    /// 获取未同步的离线数据
    /// </summary>
    Task<ApiResponse<List<OfflineDataStorageDto>>> GetUnsyncedOfflineDataAsync(string playerId);
    
    /// <summary>
    /// 标记离线数据为已同步
    /// </summary>
    Task<ApiResponse<bool>> MarkOfflineDataSyncedAsync(List<string> offlineDataIds);
    
    /// <summary>
    /// 清理已同步的旧离线数据
    /// </summary>
    Task<ApiResponse<int>> CleanupSyncedOfflineDataAsync(DateTime olderThan);
    
    #endregion

    #region 数据查询和统计
    
    /// <summary>
    /// 搜索玩家
    /// </summary>
    Task<ApiResponse<List<PlayerStorageDto>>> SearchPlayersAsync(string searchTerm, int limit = 20);
    
    /// <summary>
    /// 获取数据存储统计信息
    /// </summary>
    Task<ApiResponse<Dictionary<string, object>>> GetStorageStatsAsync();
    
    /// <summary>
    /// 数据健康检查
    /// </summary>
    Task<ApiResponse<Dictionary<string, object>>> HealthCheckAsync();
    
    #endregion

    #region 数据同步和备份
    
    /// <summary>
    /// 导出玩家数据
    /// </summary>
    Task<ApiResponse<Dictionary<string, object>>> ExportPlayerDataAsync(string playerId);
    
    /// <summary>
    /// 导入玩家数据
    /// </summary>
    Task<ApiResponse<bool>> ImportPlayerDataAsync(string playerId, Dictionary<string, object> data);
    
    /// <summary>
    /// 数据备份
    /// </summary>
    Task<ApiResponse<string>> BackupDataAsync();
    
    /// <summary>
    /// 数据清理 - 删除过期数据
    /// </summary>
    Task<ApiResponse<int>> CleanupExpiredDataAsync(TimeSpan olderThan);
    
    #endregion
}