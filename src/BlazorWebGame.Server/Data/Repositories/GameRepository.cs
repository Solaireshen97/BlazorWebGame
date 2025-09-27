using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BlazorWebGame.Server.Data.Repositories;

/// <summary>
/// 通用游戏数据仓储实现
/// </summary>
public class GameRepository<T> : IGameRepository<T> where T : class
{
    protected readonly GameDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<GameRepository<T>> _logger;

    public GameRepository(GameDbContext context, ILogger<GameRepository<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    public virtual async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public virtual async Task<IEnumerable<T>> CreateBatchAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        _dbSet.AddRange(entityList);
        await _context.SaveChangesAsync(cancellationToken);
        return entityList;
    }

    public virtual async Task<IEnumerable<T>> UpdateBatchAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        _dbSet.UpdateRange(entityList);
        await _context.SaveChangesAsync(cancellationToken);
        return entityList;
    }

    public virtual async Task<int> DeleteBatchAsync(IEnumerable<object> ids, CancellationToken cancellationToken = default)
    {
        var entities = new List<T>();
        foreach (var id in ids)
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity != null)
                entities.Add(entity);
        }

        if (entities.Count == 0)
            return 0;

        _dbSet.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
        return entities.Count;
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        return predicate == null 
            ? await _dbSet.CountAsync(cancellationToken)
            : await _dbSet.CountAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }
}

/// <summary>
/// 玩家数据仓储实现
/// </summary>
public class PlayerRepository : GameRepository<PlayerDbEntity>, IPlayerRepository
{
    public PlayerRepository(GameDbContext context, ILogger<PlayerRepository> logger) 
        : base(context, logger) { }

    public async Task<PlayerDbEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Username == username, cancellationToken);
    }

    public async Task<PlayerDbEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<PlayerDbEntity>> GetActivePlayersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.IsActive).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PlayerDbEntity>> GetRecentLoginsAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.LastLoginAt >= since).ToListAsync(cancellationToken);
    }
}

/// <summary>
/// 角色数据仓储实现
/// </summary>
public class CharacterRepository : GameRepository<CharacterDbEntity>, ICharacterRepository
{
    public CharacterRepository(GameDbContext context, ILogger<CharacterRepository> logger) 
        : base(context, logger) { }

    public async Task<IEnumerable<CharacterDbEntity>> GetByPlayerIdAsync(string playerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.PlayerId == playerId).ToListAsync(cancellationToken);
    }

    public async Task<CharacterDbEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<CharacterDbEntity>> GetActiveCharactersAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.LastActiveAt >= since).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CharacterDbEntity>> GetByLevelRangeAsync(int minLevel, int maxLevel, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.Level >= minLevel && c.Level <= maxLevel).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CharacterDbEntity>> GetTopByExperienceAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _dbSet.OrderByDescending(c => c.Experience).Take(count).ToListAsync(cancellationToken);
    }
}

/// <summary>
/// 战斗记录数据仓储实现
/// </summary>
public class BattleRecordRepository : GameRepository<BattleRecordDbEntity>, IBattleRecordRepository
{
    public BattleRecordRepository(GameDbContext context, ILogger<BattleRecordRepository> logger) 
        : base(context, logger) { }

    public async Task<IEnumerable<BattleRecordDbEntity>> GetByCharacterIdAsync(string characterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(b => b.CharacterId == characterId).OrderByDescending(b => b.StartTime).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BattleRecordDbEntity>> GetRecentBattlesAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(b => b.StartTime >= since).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BattleRecordDbEntity>> GetBattlesByResultAsync(string result, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(b => b.Result == result).ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetBattleStatsAsync(string characterId, CancellationToken cancellationToken = default)
    {
        var battles = await _dbSet.Where(b => b.CharacterId == characterId).ToListAsync(cancellationToken);
        
        return battles.GroupBy(b => b.Result ?? "Unknown")
                     .ToDictionary(g => g.Key, g => g.Count());
    }
}

/// <summary>
/// 队伍数据仓储实现
/// </summary>
public class TeamRepository : GameRepository<TeamDbEntity>, ITeamRepository
{
    public TeamRepository(GameDbContext context, ILogger<TeamRepository> logger) 
        : base(context, logger) { }

    public async Task<TeamDbEntity?> GetByCaptainIdAsync(string captainId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.CaptainId == captainId && t.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<TeamDbEntity>> GetActiveTeamsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.IsActive).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TeamDbEntity>> GetPublicTeamsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.IsActive && t.IsPublic).ToListAsync(cancellationToken);
    }

    public async Task<TeamDbEntity?> GetByMemberIdAsync(string memberId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.IsActive && t.MemberIdsJson.Contains(memberId), cancellationToken);
    }
}

/// <summary>
/// 背包物品数据仓储实现
/// </summary>
public class InventoryItemRepository : GameRepository<InventoryItemDbEntity>, IInventoryItemRepository
{
    public InventoryItemRepository(GameDbContext context, ILogger<InventoryItemRepository> logger) 
        : base(context, logger) { }

    public async Task<IEnumerable<InventoryItemDbEntity>> GetByCharacterIdAsync(string characterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.CharacterId == characterId).OrderBy(i => i.SlotPosition).ToListAsync(cancellationToken);
    }

    public async Task<InventoryItemDbEntity?> GetByCharacterAndItemAsync(string characterId, string itemId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(i => i.CharacterId == characterId && i.ItemId == itemId, cancellationToken);
    }

    public async Task<IEnumerable<InventoryItemDbEntity>> GetByItemTypeAsync(string characterId, string itemType, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.CharacterId == characterId && i.ItemType == itemType).ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalQuantityAsync(string characterId, string itemId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.CharacterId == characterId && i.ItemId == itemId)
                          .SumAsync(i => i.Quantity, cancellationToken);
    }
}

/// <summary>
/// 装备数据仓储实现
/// </summary>
public class EquipmentRepository : GameRepository<EquipmentDbEntity>, IEquipmentRepository
{
    public EquipmentRepository(GameDbContext context, ILogger<EquipmentRepository> logger) 
        : base(context, logger) { }

    public async Task<IEnumerable<EquipmentDbEntity>> GetByCharacterIdAsync(string characterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.CharacterId == characterId).ToListAsync(cancellationToken);
    }

    public async Task<EquipmentDbEntity?> GetByCharacterAndSlotAsync(string characterId, string slot, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.CharacterId == characterId && e.Slot == slot, cancellationToken);
    }

    public async Task<Dictionary<string, string?>> GetEquipmentSlotsAsync(string characterId, CancellationToken cancellationToken = default)
    {
        var equipment = await _dbSet.Where(e => e.CharacterId == characterId).ToListAsync(cancellationToken);
        return equipment.ToDictionary(e => e.Slot, e => e.ItemId);
    }
}

/// <summary>
/// 任务数据仓储实现
/// </summary>
public class QuestRepository : GameRepository<QuestDbEntity>, IQuestRepository
{
    public QuestRepository(GameDbContext context, ILogger<QuestRepository> logger) 
        : base(context, logger) { }

    public async Task<IEnumerable<QuestDbEntity>> GetByCharacterIdAsync(string characterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(q => q.CharacterId == characterId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<QuestDbEntity>> GetActiveQuestsAsync(string characterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(q => q.CharacterId == characterId && q.Status == "Active").ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<QuestDbEntity>> GetCompletedQuestsAsync(string characterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(q => q.CharacterId == characterId && q.Status == "Completed").ToListAsync(cancellationToken);
    }

    public async Task<QuestDbEntity?> GetByCharacterAndQuestAsync(string characterId, string questId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(q => q.CharacterId == characterId && q.QuestId == questId, cancellationToken);
    }
}

/// <summary>
/// 离线数据仓储实现
/// </summary>
public class OfflineDataRepository : GameRepository<OfflineDataDbEntity>, IOfflineDataRepository
{
    public OfflineDataRepository(GameDbContext context, ILogger<OfflineDataRepository> logger) 
        : base(context, logger) { }

    public async Task<IEnumerable<OfflineDataDbEntity>> GetByCharacterIdAsync(string characterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(o => o.CharacterId == characterId).OrderByDescending(o => o.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OfflineDataDbEntity>> GetPendingActivitiesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(o => o.Status == "Pending").ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OfflineDataDbEntity>> GetByActivityTypeAsync(string activityType, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(o => o.ActivityType == activityType).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OfflineDataDbEntity>> GetExpiredActivitiesAsync(DateTime expiredBefore, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(o => o.Status == "Pending" && o.StartTime < expiredBefore).ToListAsync(cancellationToken);
    }
}

/// <summary>
/// 游戏事件数据仓储实现
/// </summary>
public class GameEventRepository : GameRepository<GameEventDbEntity>, IGameEventRepository
{
    public GameEventRepository(GameDbContext context, ILogger<GameEventRepository> logger) 
        : base(context, logger) { }

    public async Task<IEnumerable<GameEventDbEntity>> GetPendingEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.Status == "Pending")
                          .OrderBy(e => e.Priority)
                          .ThenBy(e => e.CreatedAt)
                          .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GameEventDbEntity>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.EventType == eventType).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GameEventDbEntity>> GetByEntityIdAsync(string entityId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.EntityId == entityId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GameEventDbEntity>> GetByActorIdAsync(string actorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.ActorId == actorId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GameEventDbEntity>> GetFailedEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.Status == "Failed" && e.RetryCount < e.MaxRetries).ToListAsync(cancellationToken);
    }

    public async Task<int> GetPendingEventCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(e => e.Status == "Pending", cancellationToken);
    }
}