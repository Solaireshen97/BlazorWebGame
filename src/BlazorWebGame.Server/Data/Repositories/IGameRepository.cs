namespace BlazorWebGame.Server.Data.Repositories;

/// <summary>
/// 通用游戏数据仓储接口
/// </summary>
public interface IGameRepository<T> where T : class
{
    // 基础 CRUD 操作
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default);
    
    // 批量操作
    Task<IEnumerable<T>> CreateBatchAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> UpdateBatchAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task<int> DeleteBatchAsync(IEnumerable<object> ids, CancellationToken cancellationToken = default);
    
    // 查询操作
    Task<IEnumerable<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(System.Linq.Expressions.Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}

/// <summary>
/// 玩家数据仓储接口
/// </summary>
public interface IPlayerRepository : IGameRepository<PlayerDbEntity>
{
    Task<PlayerDbEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<PlayerDbEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<PlayerDbEntity>> GetActivePlayersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PlayerDbEntity>> GetRecentLoginsAsync(DateTime since, CancellationToken cancellationToken = default);
}

/// <summary>
/// 角色数据仓储接口
/// </summary>
public interface ICharacterRepository : IGameRepository<CharacterDbEntity>
{
    Task<IEnumerable<CharacterDbEntity>> GetByPlayerIdAsync(string playerId, CancellationToken cancellationToken = default);
    Task<CharacterDbEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<CharacterDbEntity>> GetActiveCharactersAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<IEnumerable<CharacterDbEntity>> GetByLevelRangeAsync(int minLevel, int maxLevel, CancellationToken cancellationToken = default);
    Task<IEnumerable<CharacterDbEntity>> GetTopByExperienceAsync(int count, CancellationToken cancellationToken = default);
}

/// <summary>
/// 战斗记录数据仓储接口
/// </summary>
public interface IBattleRecordRepository : IGameRepository<BattleRecordDbEntity>
{
    Task<IEnumerable<BattleRecordDbEntity>> GetByCharacterIdAsync(string characterId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BattleRecordDbEntity>> GetRecentBattlesAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<IEnumerable<BattleRecordDbEntity>> GetBattlesByResultAsync(string result, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetBattleStatsAsync(string characterId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 队伍数据仓储接口
/// </summary>
public interface ITeamRepository : IGameRepository<TeamDbEntity>
{
    Task<TeamDbEntity?> GetByCaptainIdAsync(string captainId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TeamDbEntity>> GetActiveTeamsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TeamDbEntity>> GetPublicTeamsAsync(CancellationToken cancellationToken = default);
    Task<TeamDbEntity?> GetByMemberIdAsync(string memberId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 背包物品数据仓储接口
/// </summary>
public interface IInventoryItemRepository : IGameRepository<InventoryItemDbEntity>
{
    Task<IEnumerable<InventoryItemDbEntity>> GetByCharacterIdAsync(string characterId, CancellationToken cancellationToken = default);
    Task<InventoryItemDbEntity?> GetByCharacterAndItemAsync(string characterId, string itemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryItemDbEntity>> GetByItemTypeAsync(string characterId, string itemType, CancellationToken cancellationToken = default);
    Task<int> GetTotalQuantityAsync(string characterId, string itemId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 装备数据仓储接口
/// </summary>
public interface IEquipmentRepository : IGameRepository<EquipmentDbEntity>
{
    Task<IEnumerable<EquipmentDbEntity>> GetByCharacterIdAsync(string characterId, CancellationToken cancellationToken = default);
    Task<EquipmentDbEntity?> GetByCharacterAndSlotAsync(string characterId, string slot, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string?>> GetEquipmentSlotsAsync(string characterId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 任务数据仓储接口
/// </summary>
public interface IQuestRepository : IGameRepository<QuestDbEntity>
{
    Task<IEnumerable<QuestDbEntity>> GetByCharacterIdAsync(string characterId, CancellationToken cancellationToken = default);
    Task<IEnumerable<QuestDbEntity>> GetActiveQuestsAsync(string characterId, CancellationToken cancellationToken = default);
    Task<IEnumerable<QuestDbEntity>> GetCompletedQuestsAsync(string characterId, CancellationToken cancellationToken = default);
    Task<QuestDbEntity?> GetByCharacterAndQuestAsync(string characterId, string questId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 离线数据仓储接口
/// </summary>
public interface IOfflineDataRepository : IGameRepository<OfflineDataDbEntity>
{
    Task<IEnumerable<OfflineDataDbEntity>> GetByCharacterIdAsync(string characterId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OfflineDataDbEntity>> GetPendingActivitiesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<OfflineDataDbEntity>> GetByActivityTypeAsync(string activityType, CancellationToken cancellationToken = default);
    Task<IEnumerable<OfflineDataDbEntity>> GetExpiredActivitiesAsync(DateTime expiredBefore, CancellationToken cancellationToken = default);
}

/// <summary>
/// 游戏事件数据仓储接口
/// </summary>
public interface IGameEventRepository : IGameRepository<GameEventDbEntity>
{
    Task<IEnumerable<GameEventDbEntity>> GetPendingEventsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<GameEventDbEntity>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameEventDbEntity>> GetByEntityIdAsync(string entityId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameEventDbEntity>> GetByActorIdAsync(string actorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameEventDbEntity>> GetFailedEventsAsync(CancellationToken cancellationToken = default);
    Task<int> GetPendingEventCountAsync(CancellationToken cancellationToken = default);
}