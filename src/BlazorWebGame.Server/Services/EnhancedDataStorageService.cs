using BlazorWebGame.Server.Data;
using BlazorWebGame.Server.Data.Repositories;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 增强的数据存储服务 - 集成数据库和缓存
/// </summary>
public class EnhancedDataStorageService : IDataStorageService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IBattleRecordRepository _battleRecordRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IInventoryItemRepository _inventoryRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IOfflineDataRepository _offlineDataRepository;
    private readonly IGameEventRepository _gameEventRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EnhancedDataStorageService> _logger;

    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public EnhancedDataStorageService(
        IPlayerRepository playerRepository,
        ICharacterRepository characterRepository,
        IBattleRecordRepository battleRecordRepository,
        ITeamRepository teamRepository,
        IInventoryItemRepository inventoryRepository,
        IEquipmentRepository equipmentRepository,
        IQuestRepository questRepository,
        IOfflineDataRepository offlineDataRepository,
        IGameEventRepository gameEventRepository,
        IMemoryCache cache,
        ILogger<EnhancedDataStorageService> logger)
    {
        _playerRepository = playerRepository;
        _characterRepository = characterRepository;
        _battleRecordRepository = battleRecordRepository;
        _teamRepository = teamRepository;
        _inventoryRepository = inventoryRepository;
        _equipmentRepository = equipmentRepository;
        _questRepository = questRepository;
        _offlineDataRepository = offlineDataRepository;
        _gameEventRepository = gameEventRepository;
        _cache = cache;
        _logger = logger;
    }

    #region Player Operations

    public async Task<PlayerEntity?> GetPlayerAsync(string playerId)
    {
        var cacheKey = $"player_{playerId}";
        
        if (_cache.TryGetValue(cacheKey, out PlayerEntity? cachedPlayer))
        {
            return cachedPlayer;
        }

        var dbPlayer = await _playerRepository.GetByIdAsync(playerId);
        if (dbPlayer == null) return null;

        var player = MapToPlayerEntity(dbPlayer);
        _cache.Set(cacheKey, player, _cacheExpiration);
        
        return player;
    }

    public async Task<PlayerEntity> SavePlayerAsync(PlayerEntity player)
    {
        var dbPlayer = await _playerRepository.GetByIdAsync(player.Id);
        
        if (dbPlayer == null)
        {
            dbPlayer = MapToPlayerDbEntity(player);
            await _playerRepository.CreateAsync(dbPlayer);
        }
        else
        {
            UpdatePlayerDbEntity(dbPlayer, player);
            await _playerRepository.UpdateAsync(dbPlayer);
        }

        // Update cache
        var cacheKey = $"player_{player.Id}";
        _cache.Set(cacheKey, player, _cacheExpiration);

        _logger.LogInformation("Saved player {PlayerId}", player.Id);
        return player;
    }

    public async Task<List<PlayerEntity>> GetAllPlayersAsync()
    {
        var dbPlayers = await _playerRepository.GetAllAsync();
        return dbPlayers.Select(MapToPlayerEntity).ToList();
    }

    public async Task<bool> DeletePlayerAsync(string playerId)
    {
        var result = await _playerRepository.DeleteAsync(playerId);
        if (result)
        {
            var cacheKey = $"player_{playerId}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Deleted player {PlayerId}", playerId);
        }
        return result;
    }

    #endregion

    #region Team Operations

    public async Task<TeamEntity?> GetTeamAsync(string teamId)
    {
        var cacheKey = $"team_{teamId}";
        
        if (_cache.TryGetValue(cacheKey, out TeamEntity? cachedTeam))
        {
            return cachedTeam;
        }

        var dbTeam = await _teamRepository.GetByIdAsync(teamId);
        if (dbTeam == null) return null;

        var team = MapToTeamEntity(dbTeam);
        _cache.Set(cacheKey, team, _cacheExpiration);
        
        return team;
    }

    public async Task<TeamEntity> SaveTeamAsync(TeamEntity team)
    {
        var dbTeam = await _teamRepository.GetByIdAsync(team.Id);
        
        if (dbTeam == null)
        {
            dbTeam = MapToTeamDbEntity(team);
            await _teamRepository.CreateAsync(dbTeam);
        }
        else
        {
            UpdateTeamDbEntity(dbTeam, team);
            await _teamRepository.UpdateAsync(dbTeam);
        }

        // Update cache
        var cacheKey = $"team_{team.Id}";
        _cache.Set(cacheKey, team, _cacheExpiration);

        _logger.LogInformation("Saved team {TeamId}", team.Id);
        return team;
    }

    public async Task<List<TeamEntity>> GetAllTeamsAsync()
    {
        var dbTeams = await _teamRepository.GetActiveTeamsAsync();
        return dbTeams.Select(MapToTeamEntity).ToList();
    }

    public async Task<bool> DeleteTeamAsync(string teamId)
    {
        var result = await _teamRepository.DeleteAsync(teamId);
        if (result)
        {
            var cacheKey = $"team_{teamId}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Deleted team {TeamId}", teamId);
        }
        return result;
    }

    #endregion

    #region Battle Record Operations

    public async Task<BattleRecordEntity?> GetBattleRecordAsync(string recordId)
    {
        if (Guid.TryParse(recordId, out var guid))
        {
            var dbRecord = await _battleRecordRepository.GetByIdAsync(guid);
            return dbRecord != null ? MapToBattleRecordEntity(dbRecord) : null;
        }
        return null;
    }

    public async Task<BattleRecordEntity> SaveBattleRecordAsync(BattleRecordEntity record)
    {
        var dbRecord = MapToBattleRecordDbEntity(record);
        await _battleRecordRepository.CreateAsync(dbRecord);
        
        _logger.LogInformation("Saved battle record {RecordId} for character {CharacterId}", 
            record.Id, record.CharacterId);
        return record;
    }

    public async Task<List<BattleRecordEntity>> GetBattleRecordsByCharacterAsync(string characterId)
    {
        var dbRecords = await _battleRecordRepository.GetByCharacterIdAsync(characterId);
        return dbRecords.Select(MapToBattleRecordEntity).ToList();
    }

    #endregion

    #region Offline Data Operations

    public async Task<OfflineDataEntity?> GetOfflineDataAsync(string dataId)
    {
        if (Guid.TryParse(dataId, out var guid))
        {
            var dbData = await _offlineDataRepository.GetByIdAsync(guid);
            return dbData != null ? MapToOfflineDataEntity(dbData) : null;
        }
        return null;
    }

    public async Task<OfflineDataEntity> SaveOfflineDataAsync(OfflineDataEntity data)
    {
        var dbData = MapToOfflineDataDbEntity(data);
        await _offlineDataRepository.CreateAsync(dbData);
        
        _logger.LogInformation("Saved offline data {DataId} for character {CharacterId}", 
            data.Id, data.CharacterId);
        return data;
    }

    public async Task<List<OfflineDataEntity>> GetOfflineDataByCharacterAsync(string characterId)
    {
        var dbData = await _offlineDataRepository.GetByCharacterIdAsync(characterId);
        return dbData.Select(MapToOfflineDataEntity).ToList();
    }

    #endregion

    #region Mapping Methods

    private static PlayerEntity MapToPlayerEntity(PlayerDbEntity dbPlayer)
    {
        return new PlayerEntity
        {
            Id = dbPlayer.Id,
            Name = dbPlayer.Username,
            TeamId = null, // This would need to be determined separately
            Metadata = dbPlayer.MetadataJson,
            LastUpdated = dbPlayer.LastLoginAt ?? dbPlayer.CreatedAt
        };
    }

    private static PlayerDbEntity MapToPlayerDbEntity(PlayerEntity player)
    {
        return new PlayerDbEntity
        {
            Id = player.Id,
            Username = player.Name,
            Email = null, // Would need additional mapping
            PasswordHash = null, // Would need additional mapping
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = player.LastUpdated,
            IsActive = true,
            MetadataJson = player.Metadata
        };
    }

    private static void UpdatePlayerDbEntity(PlayerDbEntity dbPlayer, PlayerEntity player)
    {
        dbPlayer.Username = player.Name;
        dbPlayer.LastLoginAt = player.LastUpdated;
        dbPlayer.MetadataJson = player.Metadata;
    }

    private static TeamEntity MapToTeamEntity(TeamDbEntity dbTeam)
    {
        return new TeamEntity
        {
            Id = dbTeam.Id,
            Name = dbTeam.Name ?? "Unknown Team",
            CaptainId = dbTeam.CaptainId,
            MemberIds = dbTeam.MemberIdsJson,
            IsActive = dbTeam.IsActive,
            CreatedAt = dbTeam.CreatedAt,
            MaxMembers = dbTeam.MaxMembers
        };
    }

    private static TeamDbEntity MapToTeamDbEntity(TeamEntity team)
    {
        return new TeamDbEntity
        {
            Id = team.Id,
            Name = team.Name,
            CaptainId = team.CaptainId,
            Description = null,
            MaxMembers = team.MaxMembers,
            IsPublic = true,
            IsActive = team.IsActive,
            CreatedAt = team.CreatedAt,
            MemberIdsJson = team.MemberIds
        };
    }

    private static void UpdateTeamDbEntity(TeamDbEntity dbTeam, TeamEntity team)
    {
        dbTeam.Name = team.Name;
        dbTeam.CaptainId = team.CaptainId;
        dbTeam.IsActive = team.IsActive;
        dbTeam.MemberIdsJson = team.MemberIds;
        dbTeam.MaxMembers = team.MaxMembers;
    }

    private static BattleRecordEntity MapToBattleRecordEntity(BattleRecordDbEntity dbRecord)
    {
        return new BattleRecordEntity
        {
            Id = dbRecord.Id.ToString(),
            CharacterId = dbRecord.CharacterId,
            EnemyId = dbRecord.EnemyId ?? "Unknown",
            BattleType = dbRecord.BattleType ?? "Unknown",
            StartTime = dbRecord.StartTime,
            EndTime = dbRecord.EndTime,
            Duration = dbRecord.Duration,
            Result = dbRecord.Result ?? "Unknown",
            Rewards = dbRecord.RewardsJson
        };
    }

    private static BattleRecordDbEntity MapToBattleRecordDbEntity(BattleRecordEntity record)
    {
        return new BattleRecordDbEntity
        {
            Id = Guid.TryParse(record.Id, out var guid) ? guid : Guid.NewGuid(),
            CharacterId = record.CharacterId,
            EnemyId = record.EnemyId,
            BattleType = record.BattleType,
            StartTime = record.StartTime,
            EndTime = record.EndTime,
            Duration = record.Duration,
            Result = record.Result,
            RewardsJson = record.Rewards
        };
    }

    private static OfflineDataEntity MapToOfflineDataEntity(OfflineDataDbEntity dbData)
    {
        return new OfflineDataEntity
        {
            Id = dbData.Id.ToString(),
            CharacterId = dbData.CharacterId,
            ActivityType = dbData.ActivityType ?? "Unknown",
            Status = dbData.Status,
            StartTime = dbData.StartTime,
            EndTime = dbData.EndTime,
            Duration = dbData.Duration,
            Efficiency = dbData.Efficiency,
            ActivityData = dbData.ActivityDataJson,
            Rewards = dbData.RewardsJson,
            CreatedAt = dbData.CreatedAt,
            ProcessedAt = dbData.ProcessedAt
        };
    }

    private static OfflineDataDbEntity MapToOfflineDataDbEntity(OfflineDataEntity data)
    {
        return new OfflineDataDbEntity
        {
            Id = Guid.TryParse(data.Id, out var guid) ? guid : Guid.NewGuid(),
            CharacterId = data.CharacterId,
            ActivityType = data.ActivityType,
            Status = data.Status,
            StartTime = data.StartTime,
            EndTime = data.EndTime,
            Duration = data.Duration,
            Efficiency = data.Efficiency,
            CreatedAt = data.CreatedAt,
            ProcessedAt = data.ProcessedAt,
            ActivityDataJson = data.ActivityData,
            RewardsJson = data.Rewards
        };
    }

    #endregion

    #region ActionTarget Operations - Placeholder methods

    public async Task<ActionTargetEntity?> GetActionTargetAsync(string targetId)
    {
        // These would need additional implementation based on game requirements
        await Task.Delay(1);
        return null;
    }

    public async Task<ActionTargetEntity> SaveActionTargetAsync(ActionTargetEntity target)
    {
        await Task.Delay(1);
        return target;
    }

    public async Task<List<ActionTargetEntity>> GetActionTargetsByPlayerAsync(string playerId)
    {
        await Task.Delay(1);
        return new List<ActionTargetEntity>();
    }

    public async Task<bool> DeleteActionTargetAsync(string targetId)
    {
        await Task.Delay(1);
        return true;
    }

    public async Task<List<ActionTargetEntity>> GetAllActionTargetsAsync()
    {
        await Task.Delay(1);
        return new List<ActionTargetEntity>();
    }

    #endregion
}