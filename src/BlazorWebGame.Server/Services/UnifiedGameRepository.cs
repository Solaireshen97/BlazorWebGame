using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Shared.Models;
using BlazorWebGame.Server.Data;
using System.Collections.Concurrent;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 统一游戏数据仓储实现 - 结合缓存和数据库持久化
/// </summary>
public class UnifiedGameRepository : IAdvancedGameRepository
{
    private readonly IDbContextFactory<UnifiedGameDbContext> _contextFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UnifiedGameRepository> _logger;
    
    // 缓存配置
    private readonly MemoryCacheEntryOptions _defaultCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10),
        Priority = CacheItemPriority.Normal
    };

    private readonly MemoryCacheEntryOptions _highPriorityCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
        SlidingExpiration = TimeSpan.FromMinutes(30),
        Priority = CacheItemPriority.High
    };

    // 性能统计
    private readonly ConcurrentDictionary<string, int> _operationCounts = new();
    private readonly ConcurrentDictionary<string, long> _operationTimes = new();

    public UnifiedGameRepository(
        IDbContextFactory<UnifiedGameDbContext> contextFactory, 
        IMemoryCache cache,
        ILogger<UnifiedGameRepository> logger)
    {
        _contextFactory = contextFactory;
        _cache = cache;
        _logger = logger;
    }

    #region Player Operations

    public async Task<ServiceResult<PlayerEntity>> GetPlayerAsync(string playerId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetPlayerAsync), async () =>
        {
            // 尝试从缓存获取
            var cacheKey = $"player:{playerId}";
            if (_cache.TryGetValue(cacheKey, out PlayerEntity? cachedPlayer) && cachedPlayer != null)
            {
                return ServiceResult<PlayerEntity>.Success(cachedPlayer);
            }

            using var context = await _contextFactory.CreateDbContextAsync();
            var player = await context.Players.FindAsync(playerId);
            
            if (player == null)
            {
                return ServiceResult<PlayerEntity>.Failure($"Player with ID {playerId} not found");
            }

            // 缓存结果
            _cache.Set(cacheKey, player, _defaultCacheOptions);
            
            return ServiceResult<PlayerEntity>.Success(player);
        });
    }

    public async Task<ServiceResult<PlayerEntity>> CreatePlayerAsync(PlayerEntity player)
    {
        return await ExecuteWithMetricsAsync(nameof(CreatePlayerAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            // 检查名称唯一性
            var existingPlayer = await context.Players.FirstOrDefaultAsync(p => p.Name == player.Name);
            if (existingPlayer != null)
            {
                return ServiceResult<PlayerEntity>.Failure($"Player with name '{player.Name}' already exists");
            }

            player.Id = string.IsNullOrEmpty(player.Id) ? Guid.NewGuid().ToString() : player.Id;
            player.CreatedAt = DateTime.UtcNow;
            player.UpdatedAt = DateTime.UtcNow;

            context.Players.Add(player);
            await context.SaveChangesAsync();

            // 更新缓存
            var cacheKey = $"player:{player.Id}";
            _cache.Set(cacheKey, player, _defaultCacheOptions);
            
            return ServiceResult<PlayerEntity>.Success(player);
        });
    }

    public async Task<ServiceResult<PlayerEntity>> UpdatePlayerAsync(PlayerEntity player)
    {
        return await ExecuteWithMetricsAsync(nameof(UpdatePlayerAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var existingPlayer = await context.Players.FindAsync(player.Id);
            if (existingPlayer == null)
            {
                return ServiceResult<PlayerEntity>.Failure($"Player with ID {player.Id} not found");
            }

            // 更新字段
            existingPlayer.Name = player.Name;
            existingPlayer.Level = player.Level;
            existingPlayer.Experience = player.Experience;
            existingPlayer.Health = player.Health;
            existingPlayer.MaxHealth = player.MaxHealth;
            existingPlayer.Gold = player.Gold;
            existingPlayer.SelectedBattleProfession = player.SelectedBattleProfession;
            existingPlayer.CurrentAction = player.CurrentAction;
            existingPlayer.CurrentActionTargetId = player.CurrentActionTargetId;
            existingPlayer.PartyId = player.PartyId;
            existingPlayer.IsOnline = player.IsOnline;
            existingPlayer.LastActiveAt = player.LastActiveAt;
            existingPlayer.AttributesJson = player.AttributesJson;
            existingPlayer.InventoryJson = player.InventoryJson;
            existingPlayer.SkillsJson = player.SkillsJson;
            existingPlayer.EquipmentJson = player.EquipmentJson;
            existingPlayer.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            // 更新缓存
            var cacheKey = $"player:{player.Id}";
            _cache.Set(cacheKey, existingPlayer, _defaultCacheOptions);
            
            return ServiceResult<PlayerEntity>.Success(existingPlayer);
        });
    }

    public async Task<ServiceResult<bool>> DeletePlayerAsync(string playerId)
    {
        return await ExecuteWithMetricsAsync(nameof(DeletePlayerAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var player = await context.Players.FindAsync(playerId);
            if (player == null)
            {
                return ServiceResult<bool>.Failure($"Player with ID {playerId} not found");
            }

            context.Players.Remove(player);
            await context.SaveChangesAsync();

            // 清除缓存
            var cacheKey = $"player:{playerId}";
            _cache.Remove(cacheKey);
            
            return ServiceResult<bool>.Success(true);
        });
    }

    public async Task<ServiceResult<List<PlayerEntity>>> GetPlayersAsync(int page = 1, int pageSize = 50)
    {
        return await ExecuteWithMetricsAsync(nameof(GetPlayersAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var players = await context.Players
                .OrderByDescending(p => p.LastActiveAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return ServiceResult<List<PlayerEntity>>.Success(players);
        });
    }

    public async Task<ServiceResult<List<PlayerEntity>>> GetOnlinePlayersAsync()
    {
        return await ExecuteWithMetricsAsync(nameof(GetOnlinePlayersAsync), async () =>
        {
            var cacheKey = "players:online";
            if (_cache.TryGetValue(cacheKey, out List<PlayerEntity>? cachedPlayers) && cachedPlayers != null)
            {
                return ServiceResult<List<PlayerEntity>>.Success(cachedPlayers);
            }

            using var context = await _contextFactory.CreateDbContextAsync();
            
            var players = await context.Players
                .Where(p => p.IsOnline)
                .OrderByDescending(p => p.LastActiveAt)
                .ToListAsync();

            // 短期缓存在线玩家列表
            _cache.Set(cacheKey, players, TimeSpan.FromMinutes(1));
            
            return ServiceResult<List<PlayerEntity>>.Success(players);
        });
    }

    public async Task<ServiceResult<List<PlayerEntity>>> GetPlayersByTeamAsync(string teamId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetPlayersByTeamAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            // 先获取团队信息
            var team = await context.Teams.FindAsync(teamId);
            if (team == null)
            {
                return ServiceResult<List<PlayerEntity>>.Failure($"Team with ID {teamId} not found");
            }

            var players = await context.Players
                .Where(p => p.PartyId.ToString() == teamId)
                .ToListAsync();
            
            return ServiceResult<List<PlayerEntity>>.Success(players);
        });
    }

    public async Task<ServiceResult<PlayerEntity>> GetPlayerByNameAsync(string playerName)
    {
        return await ExecuteWithMetricsAsync(nameof(GetPlayerByNameAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var player = await context.Players
                .FirstOrDefaultAsync(p => p.Name == playerName);
            
            if (player == null)
            {
                return ServiceResult<PlayerEntity>.Failure($"Player with name '{playerName}' not found");
            }
            
            return ServiceResult<PlayerEntity>.Success(player);
        });
    }

    #endregion

    #region Team Operations

    public async Task<ServiceResult<TeamEntity>> GetTeamAsync(string teamId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetTeamAsync), async () =>
        {
            var cacheKey = $"team:{teamId}";
            if (_cache.TryGetValue(cacheKey, out TeamEntity? cachedTeam) && cachedTeam != null)
            {
                return ServiceResult<TeamEntity>.Success(cachedTeam);
            }

            using var context = await _contextFactory.CreateDbContextAsync();
            var team = await context.Teams.FindAsync(teamId);
            
            if (team == null)
            {
                return ServiceResult<TeamEntity>.Failure($"Team with ID {teamId} not found");
            }

            _cache.Set(cacheKey, team, _defaultCacheOptions);
            
            return ServiceResult<TeamEntity>.Success(team);
        });
    }

    public async Task<ServiceResult<TeamEntity>> CreateTeamAsync(TeamEntity team)
    {
        return await ExecuteWithMetricsAsync(nameof(CreateTeamAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            team.Id = string.IsNullOrEmpty(team.Id) ? Guid.NewGuid().ToString() : team.Id;
            team.CreatedAt = DateTime.UtcNow;
            team.UpdatedAt = DateTime.UtcNow;

            context.Teams.Add(team);
            await context.SaveChangesAsync();

            var cacheKey = $"team:{team.Id}";
            _cache.Set(cacheKey, team, _defaultCacheOptions);
            
            return ServiceResult<TeamEntity>.Success(team);
        });
    }

    public async Task<ServiceResult<TeamEntity>> UpdateTeamAsync(TeamEntity team)
    {
        return await ExecuteWithMetricsAsync(nameof(UpdateTeamAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var existingTeam = await context.Teams.FindAsync(team.Id);
            if (existingTeam == null)
            {
                return ServiceResult<TeamEntity>.Failure($"Team with ID {team.Id} not found");
            }

            existingTeam.Name = team.Name;
            existingTeam.CaptainId = team.CaptainId;
            existingTeam.MaxMembers = team.MaxMembers;
            existingTeam.Status = team.Status;
            existingTeam.MemberIdsJson = team.MemberIdsJson;
            existingTeam.CurrentBattleId = team.CurrentBattleId;
            existingTeam.LastBattleAt = team.LastBattleAt;
            existingTeam.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            var cacheKey = $"team:{team.Id}";
            _cache.Set(cacheKey, existingTeam, _defaultCacheOptions);
            
            return ServiceResult<TeamEntity>.Success(existingTeam);
        });
    }

    public async Task<ServiceResult<bool>> DeleteTeamAsync(string teamId)
    {
        return await ExecuteWithMetricsAsync(nameof(DeleteTeamAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var team = await context.Teams.FindAsync(teamId);
            if (team == null)
            {
                return ServiceResult<bool>.Failure($"Team with ID {teamId} not found");
            }

            context.Teams.Remove(team);
            await context.SaveChangesAsync();

            var cacheKey = $"team:{teamId}";
            _cache.Remove(cacheKey);
            
            return ServiceResult<bool>.Success(true);
        });
    }

    public async Task<ServiceResult<List<TeamEntity>>> GetTeamsAsync(int page = 1, int pageSize = 50)
    {
        return await ExecuteWithMetricsAsync(nameof(GetTeamsAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var teams = await context.Teams
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return ServiceResult<List<TeamEntity>>.Success(teams);
        });
    }

    public async Task<ServiceResult<List<TeamEntity>>> GetActiveTeamsAsync()
    {
        return await ExecuteWithMetricsAsync(nameof(GetActiveTeamsAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var teams = await context.Teams
                .Where(t => t.Status == "Active")
                .OrderByDescending(t => t.LastBattleAt)
                .ToListAsync();
            
            return ServiceResult<List<TeamEntity>>.Success(teams);
        });
    }

    public async Task<ServiceResult<TeamEntity>> GetTeamByCaptainAsync(string captainId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetTeamByCaptainAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var team = await context.Teams
                .FirstOrDefaultAsync(t => t.CaptainId == captainId);
            
            if (team == null)
            {
                return ServiceResult<TeamEntity>.Failure($"Team with captain ID {captainId} not found");
            }
            
            return ServiceResult<TeamEntity>.Success(team);
        });
    }

    #endregion

    #region Action Target Operations

    public async Task<ServiceResult<ActionTargetEntity>> GetActionTargetAsync(string actionTargetId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetActionTargetAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var actionTarget = await context.ActionTargets.FindAsync(actionTargetId);
            
            if (actionTarget == null)
            {
                return ServiceResult<ActionTargetEntity>.Failure($"Action target with ID {actionTargetId} not found");
            }
            
            return ServiceResult<ActionTargetEntity>.Success(actionTarget);
        });
    }

    public async Task<ServiceResult<ActionTargetEntity>> CreateActionTargetAsync(ActionTargetEntity actionTarget)
    {
        return await ExecuteWithMetricsAsync(nameof(CreateActionTargetAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            actionTarget.Id = string.IsNullOrEmpty(actionTarget.Id) ? Guid.NewGuid().ToString() : actionTarget.Id;
            actionTarget.CreatedAt = DateTime.UtcNow;
            actionTarget.UpdatedAt = DateTime.UtcNow;

            context.ActionTargets.Add(actionTarget);
            await context.SaveChangesAsync();
            
            return ServiceResult<ActionTargetEntity>.Success(actionTarget);
        });
    }

    public async Task<ServiceResult<ActionTargetEntity>> UpdateActionTargetAsync(ActionTargetEntity actionTarget)
    {
        return await ExecuteWithMetricsAsync(nameof(UpdateActionTargetAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var existing = await context.ActionTargets.FindAsync(actionTarget.Id);
            if (existing == null)
            {
                return ServiceResult<ActionTargetEntity>.Failure($"Action target with ID {actionTarget.Id} not found");
            }

            existing.TargetType = actionTarget.TargetType;
            existing.TargetId = actionTarget.TargetId;
            existing.TargetName = actionTarget.TargetName;
            existing.ActionType = actionTarget.ActionType;
            existing.Progress = actionTarget.Progress;
            existing.Duration = actionTarget.Duration;
            existing.CompletedAt = actionTarget.CompletedAt;
            existing.IsCompleted = actionTarget.IsCompleted;
            existing.ProgressDataJson = actionTarget.ProgressDataJson;
            existing.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            
            return ServiceResult<ActionTargetEntity>.Success(existing);
        });
    }

    public async Task<ServiceResult<bool>> DeleteActionTargetAsync(string actionTargetId)
    {
        return await ExecuteWithMetricsAsync(nameof(DeleteActionTargetAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var actionTarget = await context.ActionTargets.FindAsync(actionTargetId);
            if (actionTarget == null)
            {
                return ServiceResult<bool>.Failure($"Action target with ID {actionTargetId} not found");
            }

            context.ActionTargets.Remove(actionTarget);
            await context.SaveChangesAsync();
            
            return ServiceResult<bool>.Success(true);
        });
    }

    public async Task<ServiceResult<List<ActionTargetEntity>>> GetActionTargetsByPlayerAsync(string playerId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetActionTargetsByPlayerAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var actionTargets = await context.ActionTargets
                .Where(at => at.PlayerId == playerId)
                .OrderByDescending(at => at.StartedAt)
                .ToListAsync();
            
            return ServiceResult<List<ActionTargetEntity>>.Success(actionTargets);
        });
    }

    public async Task<ServiceResult<List<ActionTargetEntity>>> GetActiveActionTargetsAsync()
    {
        return await ExecuteWithMetricsAsync(nameof(GetActiveActionTargetsAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var actionTargets = await context.ActionTargets
                .Where(at => !at.IsCompleted)
                .OrderBy(at => at.StartedAt)
                .ToListAsync();
            
            return ServiceResult<List<ActionTargetEntity>>.Success(actionTargets);
        });
    }

    public async Task<ServiceResult<ActionTargetEntity>> GetCurrentActionTargetAsync(string playerId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetCurrentActionTargetAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var actionTarget = await context.ActionTargets
                .Where(at => at.PlayerId == playerId && !at.IsCompleted)
                .OrderByDescending(at => at.StartedAt)
                .FirstOrDefaultAsync();
            
            if (actionTarget == null)
            {
                return ServiceResult<ActionTargetEntity>.Failure($"No active action target found for player {playerId}");
            }
            
            return ServiceResult<ActionTargetEntity>.Success(actionTarget);
        });
    }

    #endregion

    #region Battle Record Operations

    public async Task<ServiceResult<BattleRecordEntity>> GetBattleRecordAsync(string battleRecordId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetBattleRecordAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var battleRecord = await context.BattleRecords.FindAsync(battleRecordId);
            
            if (battleRecord == null)
            {
                return ServiceResult<BattleRecordEntity>.Failure($"Battle record with ID {battleRecordId} not found");
            }
            
            return ServiceResult<BattleRecordEntity>.Success(battleRecord);
        });
    }

    public async Task<ServiceResult<BattleRecordEntity>> CreateBattleRecordAsync(BattleRecordEntity battleRecord)
    {
        return await ExecuteWithMetricsAsync(nameof(CreateBattleRecordAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            battleRecord.Id = string.IsNullOrEmpty(battleRecord.Id) ? Guid.NewGuid().ToString() : battleRecord.Id;
            battleRecord.CreatedAt = DateTime.UtcNow;
            battleRecord.UpdatedAt = DateTime.UtcNow;

            context.BattleRecords.Add(battleRecord);
            await context.SaveChangesAsync();
            
            return ServiceResult<BattleRecordEntity>.Success(battleRecord);
        });
    }

    public async Task<ServiceResult<BattleRecordEntity>> UpdateBattleRecordAsync(BattleRecordEntity battleRecord)
    {
        return await ExecuteWithMetricsAsync(nameof(UpdateBattleRecordAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var existing = await context.BattleRecords.FindAsync(battleRecord.Id);
            if (existing == null)
            {
                return ServiceResult<BattleRecordEntity>.Failure($"Battle record with ID {battleRecord.Id} not found");
            }

            existing.BattleType = battleRecord.BattleType;
            existing.EndedAt = battleRecord.EndedAt;
            existing.Status = battleRecord.Status;
            existing.ParticipantsJson = battleRecord.ParticipantsJson;
            existing.EnemiesJson = battleRecord.EnemiesJson;
            existing.ActionsJson = battleRecord.ActionsJson;
            existing.ResultsJson = battleRecord.ResultsJson;
            existing.WaveNumber = battleRecord.WaveNumber;
            existing.Duration = battleRecord.Duration;
            existing.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            
            return ServiceResult<BattleRecordEntity>.Success(existing);
        });
    }

    public async Task<ServiceResult<bool>> DeleteBattleRecordAsync(string battleRecordId)
    {
        return await ExecuteWithMetricsAsync(nameof(DeleteBattleRecordAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var battleRecord = await context.BattleRecords.FindAsync(battleRecordId);
            if (battleRecord == null)
            {
                return ServiceResult<bool>.Failure($"Battle record with ID {battleRecordId} not found");
            }

            context.BattleRecords.Remove(battleRecord);
            await context.SaveChangesAsync();
            
            return ServiceResult<bool>.Success(true);
        });
    }

    public async Task<ServiceResult<List<BattleRecordEntity>>> GetBattleRecordsAsync(int page = 1, int pageSize = 50)
    {
        return await ExecuteWithMetricsAsync(nameof(GetBattleRecordsAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var battleRecords = await context.BattleRecords
                .OrderByDescending(br => br.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return ServiceResult<List<BattleRecordEntity>>.Success(battleRecords);
        });
    }

    public async Task<ServiceResult<List<BattleRecordEntity>>> GetBattleRecordsByPlayerAsync(string playerId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetBattleRecordsByPlayerAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var battleRecords = await context.BattleRecords
                .Where(br => br.ParticipantsJson.Contains(playerId))
                .OrderByDescending(br => br.StartedAt)
                .ToListAsync();
            
            return ServiceResult<List<BattleRecordEntity>>.Success(battleRecords);
        });
    }

    public async Task<ServiceResult<List<BattleRecordEntity>>> GetBattleRecordsByTeamAsync(string teamId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetBattleRecordsByTeamAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var battleRecords = await context.BattleRecords
                .Where(br => br.PartyId.ToString() == teamId)
                .OrderByDescending(br => br.StartedAt)
                .ToListAsync();
            
            return ServiceResult<List<BattleRecordEntity>>.Success(battleRecords);
        });
    }

    public async Task<ServiceResult<List<BattleRecordEntity>>> GetActiveBattlesAsync()
    {
        return await ExecuteWithMetricsAsync(nameof(GetActiveBattlesAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var battleRecords = await context.BattleRecords
                .Where(br => br.Status == "InProgress")
                .OrderBy(br => br.StartedAt)
                .ToListAsync();
            
            return ServiceResult<List<BattleRecordEntity>>.Success(battleRecords);
        });
    }

    public async Task<ServiceResult<BattleRecordEntity>> GetBattleRecordByBattleIdAsync(string battleId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetBattleRecordByBattleIdAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var battleRecord = await context.BattleRecords
                .FirstOrDefaultAsync(br => br.BattleId == battleId);
            
            if (battleRecord == null)
            {
                return ServiceResult<BattleRecordEntity>.Failure($"Battle record with battle ID {battleId} not found");
            }
            
            return ServiceResult<BattleRecordEntity>.Success(battleRecord);
        });
    }

    #endregion

    #region Offline Data Operations

    public async Task<ServiceResult<OfflineDataEntity>> GetOfflineDataAsync(string offlineDataId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetOfflineDataAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var offlineData = await context.OfflineData.FindAsync(offlineDataId);
            
            if (offlineData == null)
            {
                return ServiceResult<OfflineDataEntity>.Failure($"Offline data with ID {offlineDataId} not found");
            }
            
            return ServiceResult<OfflineDataEntity>.Success(offlineData);
        });
    }

    public async Task<ServiceResult<OfflineDataEntity>> CreateOfflineDataAsync(OfflineDataEntity offlineData)
    {
        return await ExecuteWithMetricsAsync(nameof(CreateOfflineDataAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            offlineData.Id = string.IsNullOrEmpty(offlineData.Id) ? Guid.NewGuid().ToString() : offlineData.Id;
            offlineData.CreatedAt = DateTime.UtcNow;
            offlineData.UpdatedAt = DateTime.UtcNow;

            context.OfflineData.Add(offlineData);
            await context.SaveChangesAsync();
            
            return ServiceResult<OfflineDataEntity>.Success(offlineData);
        });
    }

    public async Task<ServiceResult<OfflineDataEntity>> UpdateOfflineDataAsync(OfflineDataEntity offlineData)
    {
        return await ExecuteWithMetricsAsync(nameof(UpdateOfflineDataAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var existing = await context.OfflineData.FindAsync(offlineData.Id);
            if (existing == null)
            {
                return ServiceResult<OfflineDataEntity>.Failure($"Offline data with ID {offlineData.Id} not found");
            }

            existing.DataType = offlineData.DataType;
            existing.DataJson = offlineData.DataJson;
            existing.IsSynced = offlineData.IsSynced;
            existing.SyncedAt = offlineData.SyncedAt;
            existing.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            
            return ServiceResult<OfflineDataEntity>.Success(existing);
        });
    }

    public async Task<ServiceResult<bool>> DeleteOfflineDataAsync(string offlineDataId)
    {
        return await ExecuteWithMetricsAsync(nameof(DeleteOfflineDataAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var offlineData = await context.OfflineData.FindAsync(offlineDataId);
            if (offlineData == null)
            {
                return ServiceResult<bool>.Failure($"Offline data with ID {offlineDataId} not found");
            }

            context.OfflineData.Remove(offlineData);
            await context.SaveChangesAsync();
            
            return ServiceResult<bool>.Success(true);
        });
    }

    public async Task<ServiceResult<List<OfflineDataEntity>>> GetOfflineDataByPlayerAsync(string playerId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetOfflineDataByPlayerAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var offlineData = await context.OfflineData
                .Where(od => od.PlayerId == playerId)
                .OrderByDescending(od => od.CreatedAt)
                .ToListAsync();
            
            return ServiceResult<List<OfflineDataEntity>>.Success(offlineData);
        });
    }

    public async Task<ServiceResult<List<OfflineDataEntity>>> GetUnsyncedOfflineDataAsync(string playerId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetUnsyncedOfflineDataAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var unsyncedData = await context.OfflineData
                .Where(od => od.PlayerId == playerId && !od.IsSynced)
                .OrderBy(od => od.CreatedAt)
                .ToListAsync();
            
            return ServiceResult<List<OfflineDataEntity>>.Success(unsyncedData);
        });
    }

    public async Task<ServiceResult<bool>> MarkOfflineDataAsSyncedAsync(string offlineDataId)
    {
        return await ExecuteWithMetricsAsync(nameof(MarkOfflineDataAsSyncedAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var offlineData = await context.OfflineData.FindAsync(offlineDataId);
            if (offlineData == null)
            {
                return ServiceResult<bool>.Failure($"Offline data with ID {offlineDataId} not found");
            }

            offlineData.IsSynced = true;
            offlineData.SyncedAt = DateTime.UtcNow;
            offlineData.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            
            return ServiceResult<bool>.Success(true);
        });
    }

    #endregion

    #region Batch Operations

    public async Task<ServiceResult<bool>> SaveChangesAsync()
    {
        return await ExecuteWithMetricsAsync(nameof(SaveChangesAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            await context.SaveChangesAsync();
            return ServiceResult<bool>.Success(true);
        });
    }

    public async Task<ServiceResult<List<T>>> BatchCreateAsync<T>(List<T> entities) where T : BaseEntity
    {
        return await ExecuteWithMetricsAsync($"BatchCreate{typeof(T).Name}", async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            foreach (var entity in entities)
            {
                entity.Id = string.IsNullOrEmpty(entity.Id) ? Guid.NewGuid().ToString() : entity.Id;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            context.Set<T>().AddRange(entities);
            await context.SaveChangesAsync();
            
            return ServiceResult<List<T>>.Success(entities);
        });
    }

    public async Task<ServiceResult<List<T>>> BatchUpdateAsync<T>(List<T> entities) where T : BaseEntity
    {
        return await ExecuteWithMetricsAsync($"BatchUpdate{typeof(T).Name}", async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            foreach (var entity in entities)
            {
                entity.UpdatedAt = DateTime.UtcNow;
                context.Set<T>().Update(entity);
            }

            await context.SaveChangesAsync();
            
            return ServiceResult<List<T>>.Success(entities);
        });
    }

    public async Task<ServiceResult<bool>> BatchDeleteAsync<T>(List<string> entityIds) where T : BaseEntity
    {
        return await ExecuteWithMetricsAsync($"BatchDelete{typeof(T).Name}", async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var entities = await context.Set<T>()
                .Where(e => entityIds.Contains(e.Id))
                .ToListAsync();

            context.Set<T>().RemoveRange(entities);
            await context.SaveChangesAsync();
            
            return ServiceResult<bool>.Success(true);
        });
    }

    #endregion

    #region Statistics and Health

    public async Task<ServiceResult<Dictionary<string, object>>> GetDatabaseStatsAsync()
    {
        return await ExecuteWithMetricsAsync(nameof(GetDatabaseStatsAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var stats = await context.GetDatabaseStatsAsync();
            
            // 添加仓储层统计
            stats["RepositoryStats"] = new Dictionary<string, object>
            {
                ["OperationCounts"] = _operationCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ["OperationTimes"] = _operationTimes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ["CacheStats"] = GetCacheStats()
            };
            
            return ServiceResult<Dictionary<string, object>>.Success(stats);
        });
    }

    public async Task<ServiceResult<bool>> HealthCheckAsync()
    {
        return await ExecuteWithMetricsAsync(nameof(HealthCheckAsync), async () =>
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                await context.Database.ExecuteSqlRawAsync("SELECT 1");
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return ServiceResult<bool>.Failure($"Health check failed: {ex.Message}");
            }
        });
    }

    public async Task<ServiceResult<bool>> OptimizeDatabaseAsync()
    {
        return await ExecuteWithMetricsAsync(nameof(OptimizeDatabaseAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            await context.RebuildIndexesAsync();
            return ServiceResult<bool>.Success(true);
        });
    }

    #endregion

    #region Transaction Support

    public async Task<ServiceResult<T>> ExecuteInTransactionAsync<T>(Func<Task<ServiceResult<T>>> operation)
    {
        return await ExecuteWithMetricsAsync(nameof(ExecuteInTransactionAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();
            
            try
            {
                var result = await operation();
                if (result.Success)
                {
                    await transaction.CommitAsync();
                }
                else
                {
                    await transaction.RollbackAsync();
                }
                
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction failed");
                return ServiceResult<T>.Failure($"Transaction failed: {ex.Message}");
            }
        });
    }

    #endregion

    #region Advanced Query Operations

    public async Task<ServiceResult<List<PlayerEntity>>> SearchPlayersAsync(string searchTerm, int page = 1, int pageSize = 50)
    {
        return await ExecuteWithMetricsAsync(nameof(SearchPlayersAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var players = await context.Players
                .Where(p => p.Name.Contains(searchTerm))
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return ServiceResult<List<PlayerEntity>>.Success(players);
        });
    }

    public async Task<ServiceResult<List<TeamEntity>>> SearchTeamsAsync(string searchTerm, int page = 1, int pageSize = 50)
    {
        return await ExecuteWithMetricsAsync(nameof(SearchTeamsAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var teams = await context.Teams
                .Where(t => t.Name.Contains(searchTerm))
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return ServiceResult<List<TeamEntity>>.Success(teams);
        });
    }

    public async Task<ServiceResult<List<BattleRecordEntity>>> GetBattleHistoryAsync(DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 50)
    {
        return await ExecuteWithMetricsAsync(nameof(GetBattleHistoryAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var battleRecords = await context.BattleRecords
                .Where(br => br.StartedAt >= fromDate && br.StartedAt <= toDate)
                .OrderByDescending(br => br.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return ServiceResult<List<BattleRecordEntity>>.Success(battleRecords);
        });
    }

    public async Task<ServiceResult<Dictionary<string, int>>> GetPlayerStatisticsAsync(string playerId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetPlayerStatisticsAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var stats = new Dictionary<string, int>
            {
                ["TotalBattles"] = await context.BattleRecords
                    .CountAsync(br => br.ParticipantsJson.Contains(playerId)),
                ["CompletedActions"] = await context.ActionTargets
                    .CountAsync(at => at.PlayerId == playerId && at.IsCompleted),
                ["ActiveActions"] = await context.ActionTargets
                    .CountAsync(at => at.PlayerId == playerId && !at.IsCompleted)
            };
            
            return ServiceResult<Dictionary<string, int>>.Success(stats);
        });
    }

    public async Task<ServiceResult<Dictionary<string, int>>> GetTeamStatisticsAsync(string teamId)
    {
        return await ExecuteWithMetricsAsync(nameof(GetTeamStatisticsAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var stats = new Dictionary<string, int>
            {
                ["TotalBattles"] = await context.BattleRecords
                    .CountAsync(br => br.PartyId.ToString() == teamId),
                ["MemberCount"] = await context.Players
                    .CountAsync(p => p.PartyId.ToString() == teamId),
                ["ActiveBattles"] = await context.BattleRecords
                    .CountAsync(br => br.PartyId.ToString() == teamId && br.Status == "InProgress")
            };
            
            return ServiceResult<Dictionary<string, int>>.Success(stats);
        });
    }

    #endregion

    #region Cache Operations

    public async Task<ServiceResult<T>> GetFromCacheAsync<T>(string key) where T : class
    {
        return await Task.Run(() =>
        {
            if (_cache.TryGetValue(key, out T? value) && value != null)
            {
                return ServiceResult<T>.Success(value);
            }
            
            return ServiceResult<T>.Failure($"Key '{key}' not found in cache");
        });
    }

    public async Task<ServiceResult<bool>> SetCacheAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        return await Task.Run(() =>
        {
            var options = expiration.HasValue 
                ? new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration.Value }
                : _defaultCacheOptions;
                
            _cache.Set(key, value, options);
            return ServiceResult<bool>.Success(true);
        });
    }

    public async Task<ServiceResult<bool>> RemoveFromCacheAsync(string key)
    {
        return await Task.Run(() =>
        {
            _cache.Remove(key);
            return ServiceResult<bool>.Success(true);
        });
    }

    public async Task<ServiceResult<bool>> ClearCacheAsync(string pattern = "*")
    {
        return await Task.Run(() =>
        {
            // 注意：MemoryCache没有直接的模式清除功能
            // 在生产环境中，可以考虑使用Redis等支持模式匹配的缓存
            if (pattern == "*")
            {
                if (_cache is MemoryCache memoryCache)
                {
                    memoryCache.Clear();
                }
            }
            
            return ServiceResult<bool>.Success(true);
        });
    }

    #endregion

    #region Bulk Operations

    public async Task<ServiceResult<bool>> BulkInsertAsync<T>(IEnumerable<T> entities) where T : BaseEntity
    {
        return await ExecuteWithMetricsAsync($"BulkInsert{typeof(T).Name}", async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var entityList = entities.ToList();
            foreach (var entity in entityList)
            {
                entity.Id = string.IsNullOrEmpty(entity.Id) ? Guid.NewGuid().ToString() : entity.Id;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await context.Set<T>().AddRangeAsync(entityList);
            await context.SaveChangesAsync();
            
            return ServiceResult<bool>.Success(true);
        });
    }

    public async Task<ServiceResult<bool>> BulkUpdateAsync<T>(IEnumerable<T> entities) where T : BaseEntity
    {
        return await ExecuteWithMetricsAsync($"BulkUpdate{typeof(T).Name}", async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var entityList = entities.ToList();
            foreach (var entity in entityList)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }

            context.Set<T>().UpdateRange(entityList);
            await context.SaveChangesAsync();
            
            return ServiceResult<bool>.Success(true);
        });
    }

    public async Task<ServiceResult<bool>> BulkDeleteAsync<T>(IEnumerable<string> entityIds) where T : BaseEntity
    {
        return await ExecuteWithMetricsAsync($"BulkDelete{typeof(T).Name}", async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var idList = entityIds.ToList();
            var entities = await context.Set<T>()
                .Where(e => idList.Contains(e.Id))
                .ToListAsync();

            context.Set<T>().RemoveRange(entities);
            await context.SaveChangesAsync();
            
            return ServiceResult<bool>.Success(true);
        });
    }

    #endregion

    #region Database Maintenance

    public async Task<ServiceResult<bool>> CompactDatabaseAsync()
    {
        return await ExecuteWithMetricsAsync(nameof(CompactDatabaseAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            if (context.Database.IsSqlite())
            {
                await context.Database.ExecuteSqlRawAsync("VACUUM");
                await context.Database.ExecuteSqlRawAsync("ANALYZE");
            }
            
            return ServiceResult<bool>.Success(true);
        });
    }

    public async Task<ServiceResult<bool>> RebuildIndexesAsync()
    {
        return await ExecuteWithMetricsAsync(nameof(RebuildIndexesAsync), async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            await context.RebuildIndexesAsync();
            return ServiceResult<bool>.Success(true);
        });
    }

    public async Task<ServiceResult<string>> BackupDatabaseAsync(string backupPath)
    {
        return await ExecuteWithMetricsAsync(nameof(BackupDatabaseAsync), async () =>
        {
            // 实现数据库备份逻辑
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"gamedb_backup_{timestamp}.db";
            var fullPath = Path.Combine(backupPath, fileName);
            
            // 确保备份目录存在
            Directory.CreateDirectory(backupPath);
            
            using var context = await _contextFactory.CreateDbContextAsync();
            if (context.Database.IsSqlite())
            {
                var connectionString = context.Database.GetConnectionString();
                if (connectionString != null)
                {
                    var sourceFile = connectionString.Split('=')[1].Split(';')[0];
                    if (File.Exists(sourceFile))
                    {
                        File.Copy(sourceFile, fullPath, true);
                        return ServiceResult<string>.Success(fullPath);
                    }
                }
            }
            
            return ServiceResult<string>.Failure("Failed to create database backup");
        });
    }

    public async Task<ServiceResult<bool>> RestoreDatabaseAsync(string backupPath)
    {
        return await ExecuteWithMetricsAsync(nameof(RestoreDatabaseAsync), async () =>
        {
            if (!File.Exists(backupPath))
            {
                return ServiceResult<bool>.Failure($"Backup file not found: {backupPath}");
            }
            
            using var context = await _contextFactory.CreateDbContextAsync();
            if (context.Database.IsSqlite())
            {
                var connectionString = context.Database.GetConnectionString();
                if (connectionString != null)
                {
                    var targetFile = connectionString.Split('=')[1].Split(';')[0];
                    File.Copy(backupPath, targetFile, true);
                    return ServiceResult<bool>.Success(true);
                }
            }
            
            return ServiceResult<bool>.Failure("Failed to restore database from backup");
        });
    }

    #endregion

    #region Private Helper Methods

    private async Task<ServiceResult<T>> ExecuteWithMetricsAsync<T>(string operationName, Func<Task<ServiceResult<T>>> operation)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _operationCounts.AddOrUpdate(operationName, 1, (key, value) => value + 1);
            
            var result = await operation();
            
            stopwatch.Stop();
            _operationTimes.AddOrUpdate(operationName, stopwatch.ElapsedMilliseconds, 
                (key, value) => (value + stopwatch.ElapsedMilliseconds) / 2); // 简单平均
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Operation {OperationName} failed after {ElapsedMs} ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            
            return ServiceResult<T>.Failure($"Operation {operationName} failed: {ex.Message}");
        }
    }

    private Dictionary<string, object> GetCacheStats()
    {
        // MemoryCache没有直接获取统计信息的方法
        // 这里提供基本信息
        return new Dictionary<string, object>
        {
            ["CacheType"] = "MemoryCache",
            ["DefaultExpirationMinutes"] = _defaultCacheOptions.AbsoluteExpirationRelativeToNow?.TotalMinutes ?? 0,
            ["HighPriorityExpirationHours"] = _highPriorityCacheOptions.AbsoluteExpirationRelativeToNow?.TotalHours ?? 0
        };
    }

    #endregion
}