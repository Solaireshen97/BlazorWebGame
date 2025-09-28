using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 游戏数据仓储接口 - 统一数据访问层
/// </summary>
public interface IGameRepository
{
    #region Player Operations
    Task<ServiceResult<PlayerEntity>> GetPlayerAsync(string playerId);
    Task<ServiceResult<PlayerEntity>> CreatePlayerAsync(PlayerEntity player);
    Task<ServiceResult<PlayerEntity>> UpdatePlayerAsync(PlayerEntity player);
    Task<ServiceResult<bool>> DeletePlayerAsync(string playerId);
    Task<ServiceResult<List<PlayerEntity>>> GetPlayersAsync(int page = 1, int pageSize = 50);
    Task<ServiceResult<List<PlayerEntity>>> GetOnlinePlayersAsync();
    Task<ServiceResult<List<PlayerEntity>>> GetPlayersByTeamAsync(string teamId);
    Task<ServiceResult<PlayerEntity>> GetPlayerByNameAsync(string playerName);
    #endregion

    #region Team Operations
    Task<ServiceResult<TeamEntity>> GetTeamAsync(string teamId);
    Task<ServiceResult<TeamEntity>> CreateTeamAsync(TeamEntity team);
    Task<ServiceResult<TeamEntity>> UpdateTeamAsync(TeamEntity team);
    Task<ServiceResult<bool>> DeleteTeamAsync(string teamId);
    Task<ServiceResult<List<TeamEntity>>> GetTeamsAsync(int page = 1, int pageSize = 50);
    Task<ServiceResult<List<TeamEntity>>> GetActiveTeamsAsync();
    Task<ServiceResult<TeamEntity>> GetTeamByCaptainAsync(string captainId);
    #endregion

    #region Action Target Operations
    Task<ServiceResult<ActionTargetEntity>> GetActionTargetAsync(string actionTargetId);
    Task<ServiceResult<ActionTargetEntity>> CreateActionTargetAsync(ActionTargetEntity actionTarget);
    Task<ServiceResult<ActionTargetEntity>> UpdateActionTargetAsync(ActionTargetEntity actionTarget);
    Task<ServiceResult<bool>> DeleteActionTargetAsync(string actionTargetId);
    Task<ServiceResult<List<ActionTargetEntity>>> GetActionTargetsByPlayerAsync(string playerId);
    Task<ServiceResult<List<ActionTargetEntity>>> GetActiveActionTargetsAsync();
    Task<ServiceResult<ActionTargetEntity>> GetCurrentActionTargetAsync(string playerId);
    #endregion

    #region Battle Record Operations
    Task<ServiceResult<BattleRecordEntity>> GetBattleRecordAsync(string battleRecordId);
    Task<ServiceResult<BattleRecordEntity>> CreateBattleRecordAsync(BattleRecordEntity battleRecord);
    Task<ServiceResult<BattleRecordEntity>> UpdateBattleRecordAsync(BattleRecordEntity battleRecord);
    Task<ServiceResult<bool>> DeleteBattleRecordAsync(string battleRecordId);
    Task<ServiceResult<List<BattleRecordEntity>>> GetBattleRecordsAsync(int page = 1, int pageSize = 50);
    Task<ServiceResult<List<BattleRecordEntity>>> GetBattleRecordsByPlayerAsync(string playerId);
    Task<ServiceResult<List<BattleRecordEntity>>> GetBattleRecordsByTeamAsync(string teamId);
    Task<ServiceResult<List<BattleRecordEntity>>> GetActiveBattlesAsync();
    Task<ServiceResult<BattleRecordEntity>> GetBattleRecordByBattleIdAsync(string battleId);
    #endregion

    #region Offline Data Operations
    Task<ServiceResult<OfflineDataEntity>> GetOfflineDataAsync(string offlineDataId);
    Task<ServiceResult<OfflineDataEntity>> CreateOfflineDataAsync(OfflineDataEntity offlineData);
    Task<ServiceResult<OfflineDataEntity>> UpdateOfflineDataAsync(OfflineDataEntity offlineData);
    Task<ServiceResult<bool>> DeleteOfflineDataAsync(string offlineDataId);
    Task<ServiceResult<List<OfflineDataEntity>>> GetOfflineDataByPlayerAsync(string playerId);
    Task<ServiceResult<List<OfflineDataEntity>>> GetUnsyncedOfflineDataAsync(string playerId);
    Task<ServiceResult<bool>> MarkOfflineDataAsSyncedAsync(string offlineDataId);
    #endregion

    #region Batch Operations
    Task<ServiceResult<bool>> SaveChangesAsync();
    Task<ServiceResult<List<T>>> BatchCreateAsync<T>(List<T> entities) where T : BaseEntity;
    Task<ServiceResult<List<T>>> BatchUpdateAsync<T>(List<T> entities) where T : BaseEntity;
    Task<ServiceResult<bool>> BatchDeleteAsync<T>(List<string> entityIds) where T : BaseEntity;
    #endregion

    #region Statistics and Health
    Task<ServiceResult<Dictionary<string, object>>> GetDatabaseStatsAsync();
    Task<ServiceResult<bool>> HealthCheckAsync();
    Task<ServiceResult<bool>> OptimizeDatabaseAsync();
    #endregion

    #region Transaction Support
    Task<ServiceResult<T>> ExecuteInTransactionAsync<T>(Func<Task<ServiceResult<T>>> operation);
    #endregion
}

/// <summary>
/// 扩展的游戏数据仓储接口 - 支持高级查询和缓存
/// </summary>
public interface IAdvancedGameRepository : IGameRepository
{
    #region Advanced Query Operations
    Task<ServiceResult<List<PlayerEntity>>> SearchPlayersAsync(string searchTerm, int page = 1, int pageSize = 50);
    Task<ServiceResult<List<TeamEntity>>> SearchTeamsAsync(string searchTerm, int page = 1, int pageSize = 50);
    Task<ServiceResult<List<BattleRecordEntity>>> GetBattleHistoryAsync(DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 50);
    Task<ServiceResult<Dictionary<string, int>>> GetPlayerStatisticsAsync(string playerId);
    Task<ServiceResult<Dictionary<string, int>>> GetTeamStatisticsAsync(string teamId);
    #endregion

    #region Cache Operations
    Task<ServiceResult<T>> GetFromCacheAsync<T>(string key) where T : class;
    Task<ServiceResult<bool>> SetCacheAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task<ServiceResult<bool>> RemoveFromCacheAsync(string key);
    Task<ServiceResult<bool>> ClearCacheAsync(string pattern = "*");
    #endregion

    #region Bulk Operations
    Task<ServiceResult<bool>> BulkInsertAsync<T>(IEnumerable<T> entities) where T : BaseEntity;
    Task<ServiceResult<bool>> BulkUpdateAsync<T>(IEnumerable<T> entities) where T : BaseEntity;
    Task<ServiceResult<bool>> BulkDeleteAsync<T>(IEnumerable<string> entityIds) where T : BaseEntity;
    #endregion

    #region Database Maintenance
    Task<ServiceResult<bool>> CompactDatabaseAsync();
    Task<ServiceResult<bool>> RebuildIndexesAsync();
    Task<ServiceResult<string>> BackupDatabaseAsync(string backupPath);
    Task<ServiceResult<bool>> RestoreDatabaseAsync(string backupPath);
    #endregion
}