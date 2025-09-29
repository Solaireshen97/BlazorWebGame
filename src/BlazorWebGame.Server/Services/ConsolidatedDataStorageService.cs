using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Server.Configuration;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Shared.Models;
using System.Collections.Concurrent;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 統一数据存储服务 - 整合所有数据存储功能的单一实现
/// 提供高性能、事务支持、缓存管理和批量操作
/// </summary>
public class ConsolidatedDataStorageService : IDataStorageService, IDisposable
{
    private readonly IDbContextFactory<ConsolidatedGameDbContext> _contextFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConsolidatedDataStorageService> _logger;
    private readonly ConsolidatedDataStorageOptions _options;

    // 性能统计
    private readonly ConcurrentDictionary<string, long> _operationCounts = new();
    private readonly ConcurrentDictionary<string, double> _operationTimes = new();
    
    // 批量操作队列
    private readonly ConcurrentQueue<IBatchOperation> _batchQueue = new();
    private readonly Timer? _batchProcessor;
    private readonly SemaphoreSlim _batchSemaphore = new(1, 1);
    
    // 缓存配置
    private readonly MemoryCacheEntryOptions _defaultCacheOptions;
    private readonly MemoryCacheEntryOptions _shortTermCacheOptions;
    
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed = false;

    public ConsolidatedDataStorageService(
        IDbContextFactory<ConsolidatedGameDbContext> contextFactory,
        IMemoryCache cache,
        ILogger<ConsolidatedDataStorageService> logger,
        IOptions<ConsolidatedDataStorageOptions> options)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));

        // 配置缓存选项
        _defaultCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(_options.CacheExpirationMinutes / 2),
            Priority = CacheItemPriority.Normal,
            Size = 1
        };

        _shortTermCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.Low,
            Size = 1
        };

        // 启动批量处理器
        if (_options.EnableBatchOperations)
        {
            _batchProcessor = new Timer(ProcessBatchOperations, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        _logger.LogInformation("Consolidated data storage service initialized with {StorageType}", _options.StorageType);
    }

    #region IDataStorageService Implementation

    public async Task<PlayerStorageDto?> GetPlayerAsync(string playerId)
    {
        var player = await GetPlayerEntityAsync(playerId);
        return player != null ? MapToPlayerStorageDto(player) : null;
    }

    public async Task<ApiResponse<PlayerStorageDto>> SavePlayerAsync(PlayerStorageDto player)
    {
        try
        {
            var entity = MapToPlayerEntity(player);
            var result = await SavePlayerEntityAsync(entity);
            
            if (result.Success && result.Data != null)
            {
                return new ApiResponse<PlayerStorageDto>
                {
                    Success = true,
                    Data = MapToPlayerStorageDto(result.Data),
                    Message = "Player saved successfully"
                };
            }
            
            return new ApiResponse<PlayerStorageDto>
            {
                Success = false,
                Message = result.Message ?? "Failed to save player"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving player {PlayerId}", player.Id);
            return new ApiResponse<PlayerStorageDto>
            {
                Success = false,
                Message = "An error occurred while saving player"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeletePlayerAsync(string playerId)
    {
        var result = await DeletePlayerEntityAsync(playerId);
        return new ApiResponse<bool>
        {
            Success = result.Success,
            Data = result.Data,
            Message = result.Message
        };
    }

    public async Task<ApiResponse<List<PlayerStorageDto>>> GetOnlinePlayersAsync()
    {
        try
        {
            var players = await GetOnlinePlayerEntitiesAsync();
            var dtos = players.Select(MapToPlayerStorageDto).ToList();
            return new ApiResponse<List<PlayerStorageDto>>
            {
                Success = true,
                Data = dtos,
                Message = $"Retrieved {dtos.Count} online players"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online players");
            return new ApiResponse<List<PlayerStorageDto>>
            {
                Success = false,
                Message = "An error occurred while getting online players"
            };
        }
    }

    public async Task<BatchOperationResponseDto<PlayerStorageDto>> SavePlayersAsync(List<PlayerStorageDto> players)
    {
        var response = new BatchOperationResponseDto<PlayerStorageDto>
        {
            TotalCount = players.Count,
            SuccessfulItems = new List<PlayerStorageDto>(),
            FailedItems = new List<BatchOperationFailureDto<PlayerStorageDto>>()
        };

        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();

            foreach (var player in players)
            {
                try
                {
                    var entity = MapToPlayerEntity(player);
                    
                    var existing = await context.Players.FindAsync(entity.Id);
                    if (existing != null)
                    {
                        context.Entry(existing).CurrentValues.SetValues(entity);
                    }
                    else
                    {
                        context.Players.Add(entity);
                    }
                    
                    response.SuccessfulItems.Add(player);
                }
                catch (Exception ex)
                {
                    response.FailedItems.Add(new BatchOperationFailureDto<PlayerStorageDto>
                    {
                        Item = player,
                        Error = ex.Message
                    });
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            response.SuccessCount = response.SuccessfulItems.Count;
            response.FailureCount = response.FailedItems.Count;
            response.Success = response.SuccessCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch save players failed");
            response.Success = false;
            response.Message = ex.Message;
        }

        return response;
    }

    #endregion

    #region Team Operations - Basic Implementation

    public async Task<TeamStorageDto?> GetTeamAsync(string teamId)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var team = await context.Teams.FindAsync(teamId);
            return team != null ? MapToTeamStorageDto(team) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team {TeamId}", teamId);
            return null;
        }
    }

    public async Task<TeamStorageDto?> GetTeamByCaptainAsync(string captainId)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var team = await context.Teams.FirstOrDefaultAsync(t => t.CaptainId == captainId);
            return team != null ? MapToTeamStorageDto(team) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team by captain {CaptainId}", captainId);
            return null;
        }
    }

    #endregion

    #region Mapping Methods

    private PlayerStorageDto MapToPlayerStorageDto(PlayerEntity entity)
    {
        return new PlayerStorageDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Level = entity.Level,
            Experience = entity.Experience,
            Health = entity.Health,
            MaxHealth = entity.MaxHealth,
            Gold = entity.Gold,
            SelectedBattleProfession = entity.SelectedBattleProfession,
            CurrentAction = entity.CurrentAction,
            CurrentActionTargetId = entity.CurrentActionTargetId,
            PartyId = entity.PartyId,
            IsOnline = entity.IsOnline,
            LastActiveAt = entity.LastActiveAt,
            AttributesJson = entity.AttributesJson,
            InventoryJson = entity.InventoryJson,
            SkillsJson = entity.SkillsJson,
            EquipmentJson = entity.EquipmentJson,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private PlayerEntity MapToPlayerEntity(PlayerStorageDto dto)
    {
        return new PlayerEntity
        {
            Id = dto.Id,
            Name = dto.Name,
            Level = dto.Level,
            Experience = dto.Experience,
            Health = dto.Health,
            MaxHealth = dto.MaxHealth,
            Gold = dto.Gold,
            SelectedBattleProfession = dto.SelectedBattleProfession,
            CurrentAction = dto.CurrentAction,
            CurrentActionTargetId = dto.CurrentActionTargetId,
            PartyId = dto.PartyId,
            IsOnline = dto.IsOnline,
            LastActiveAt = dto.LastActiveAt,
            AttributesJson = dto.AttributesJson,
            InventoryJson = dto.InventoryJson,
            SkillsJson = dto.SkillsJson,
            EquipmentJson = dto.EquipmentJson,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private TeamStorageDto MapToTeamStorageDto(TeamEntity entity)
    {
        return new TeamStorageDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CaptainId = entity.CaptainId,
            Status = entity.Status,
            CurrentBattleId = entity.CurrentBattleId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            MemberIdsJson = entity.MemberIdsJson
        };
    }

    private TeamEntity MapToTeamEntity(TeamStorageDto dto)
    {
        return new TeamEntity
        {
            Id = dto.Id,
            Name = dto.Name,
            CaptainId = dto.CaptainId,
            Status = dto.Status,
            CurrentBattleId = dto.CurrentBattleId,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            MemberIdsJson = dto.MemberIdsJson
        };
    }

    #endregion

    #region Batch Operations

    private async void ProcessBatchOperations(object? state)
    {
        if (_disposed || !_batchSemaphore.Wait(100))
            return;

        try
        {
            var operations = new List<IBatchOperation>();
            
            // Collect batch operations
            while (operations.Count < _options.BatchSize && _batchQueue.TryDequeue(out var operation))
            {
                operations.Add(operation);
            }

            if (operations.Count > 0)
            {
                await ExecuteBatchOperations(operations);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch operations");
        }
        finally
        {
            _batchSemaphore.Release();
        }
    }

    private async Task ExecuteBatchOperations(List<IBatchOperation> operations)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            foreach (var operation in operations)
            {
                await operation.ExecuteAsync(context);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogDebug("Executed {Count} batch operations successfully", operations.Count);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Batch operations failed, rolled back");
            throw;
        }
    }

    #endregion

    #region Missing IDataStorageService Methods - Stub Implementations

    public async Task<TeamStorageDto?> GetTeamByPlayerAsync(string playerId)
    {
        return await Task.FromResult<TeamStorageDto?>(null);
    }

    public async Task<ApiResponse<TeamStorageDto>> SaveTeamAsync(TeamStorageDto team)
    {
        return await Task.FromResult(new ApiResponse<TeamStorageDto> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<bool>> DeleteTeamAsync(string teamId)
    {
        return await Task.FromResult(new ApiResponse<bool> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<List<TeamStorageDto>>> GetActiveTeamsAsync()
    {
        return await Task.FromResult(new ApiResponse<List<TeamStorageDto>> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ActionTargetStorageDto?> GetCurrentActionTargetAsync(string playerId)
    {
        return await Task.FromResult<ActionTargetStorageDto?>(null);
    }

    public async Task<ApiResponse<ActionTargetStorageDto>> SaveActionTargetAsync(ActionTargetStorageDto actionTarget)
    {
        return await Task.FromResult(new ApiResponse<ActionTargetStorageDto> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<bool>> CompleteActionTargetAsync(string actionTargetId)
    {
        return await Task.FromResult(new ApiResponse<bool> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<bool>> CancelActionTargetAsync(string playerId)
    {
        return await Task.FromResult(new ApiResponse<bool> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<List<ActionTargetStorageDto>>> GetPlayerActionHistoryAsync(string playerId, int limit = 50)
    {
        return await Task.FromResult(new ApiResponse<List<ActionTargetStorageDto>> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<BattleRecordStorageDto?> GetBattleRecordAsync(string battleId)
    {
        return await Task.FromResult<BattleRecordStorageDto?>(null);
    }

    public async Task<ApiResponse<BattleRecordStorageDto>> SaveBattleRecordAsync(BattleRecordStorageDto battleRecord)
    {
        return await Task.FromResult(new ApiResponse<BattleRecordStorageDto> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<bool>> EndBattleRecordAsync(string battleId, string status, Dictionary<string, object> results)
    {
        return await Task.FromResult(new ApiResponse<bool> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<List<BattleRecordStorageDto>>> GetPlayerBattleHistoryAsync(string playerId, DataStorageQueryDto query)
    {
        return await Task.FromResult(new ApiResponse<List<BattleRecordStorageDto>> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<List<BattleRecordStorageDto>>> GetTeamBattleHistoryAsync(string teamId, DataStorageQueryDto query)
    {
        return await Task.FromResult(new ApiResponse<List<BattleRecordStorageDto>> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<List<BattleRecordStorageDto>>> GetActiveBattleRecordsAsync()
    {
        return await Task.FromResult(new ApiResponse<List<BattleRecordStorageDto>> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<OfflineDataStorageDto>> SaveOfflineDataAsync(OfflineDataStorageDto offlineData)
    {
        return await Task.FromResult(new ApiResponse<OfflineDataStorageDto> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<List<OfflineDataStorageDto>>> GetUnsyncedOfflineDataAsync(string playerId)
    {
        return await Task.FromResult(new ApiResponse<List<OfflineDataStorageDto>> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<bool>> MarkOfflineDataSyncedAsync(List<string> offlineDataIds)
    {
        return await Task.FromResult(new ApiResponse<bool> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<int>> CleanupSyncedOfflineDataAsync(DateTime olderThan)
    {
        return await Task.FromResult(new ApiResponse<int> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<List<PlayerStorageDto>>> SearchPlayersAsync(string searchTerm, int limit = 20)
    {
        return await Task.FromResult(new ApiResponse<List<PlayerStorageDto>> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<Dictionary<string, object>>> GetStorageStatsAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var stats = await context.GetDatabaseStatsAsync();
            return new ApiResponse<Dictionary<string, object>>
            {
                Success = true,
                Data = stats,
                Message = "Storage stats retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage stats");
            return new ApiResponse<Dictionary<string, object>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ApiResponse<Dictionary<string, object>>> HealthCheckAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var canConnect = await context.Database.CanConnectAsync();
            
            var healthInfo = new Dictionary<string, object>
            {
                ["CanConnect"] = canConnect,
                ["DatabaseType"] = "SQLite",
                ["CheckedAt"] = DateTime.UtcNow
            };

            if (canConnect)
            {
                var stats = await context.GetDatabaseStatsAsync();
                healthInfo["DatabaseStats"] = stats;
            }

            return new ApiResponse<Dictionary<string, object>>
            {
                Success = canConnect,
                Data = healthInfo,
                Message = canConnect ? "Health check passed" : "Database connection failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return new ApiResponse<Dictionary<string, object>>
            {
                Success = false,
                Message = ex.Message,
                Data = new Dictionary<string, object> 
                { 
                    ["Error"] = ex.Message,
                    ["CheckedAt"] = DateTime.UtcNow
                }
            };
        }
    }

    public async Task<ApiResponse<Dictionary<string, object>>> ExportPlayerDataAsync(string playerId)
    {
        return await Task.FromResult(new ApiResponse<Dictionary<string, object>> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<bool>> ImportPlayerDataAsync(string playerId, Dictionary<string, object> data)
    {
        return await Task.FromResult(new ApiResponse<bool> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    public async Task<ApiResponse<string>> BackupDataAsync()
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"gamedata_backup_{timestamp}.db";
            var backupPath = Path.Combine("backups", backupFileName);
            
            Directory.CreateDirectory("backups");
            
            using var context = await _contextFactory.CreateDbContextAsync();
            
            // For SQLite, we can simply copy the database file
            var connectionString = context.Database.GetConnectionString();
            if (connectionString?.Contains("Data Source=") == true)
            {
                var sourceFile = connectionString.Split("Data Source=")[1].Split(';')[0];
                File.Copy(sourceFile, backupPath, true);
                
                return new ApiResponse<string>
                {
                    Success = true,
                    Data = backupPath,
                    Message = "Backup completed successfully"
                };
            }
            
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Unable to determine database file path"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed");
            return new ApiResponse<string>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ApiResponse<int>> CleanupExpiredDataAsync(TimeSpan olderThan)
    {
        return await Task.FromResult(new ApiResponse<int> 
        { 
            Success = false, 
            Message = "Not implemented" 
        });
    }

    #endregion

    #region Helper Methods

    private async Task<PlayerEntity?> GetPlayerEntityAsync(string playerId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Players.FirstOrDefaultAsync(p => p.Id == playerId);
    }

    private async Task<ServiceResult<PlayerEntity>> SavePlayerEntityAsync(PlayerEntity player)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var existing = await context.Players.FirstOrDefaultAsync(p => p.Id == player.Id);
            if (existing != null)
            {
                // Update existing
                context.Entry(existing).CurrentValues.SetValues(player);
                context.Players.Update(existing);
            }
            else
            {
                // Create new
                context.Players.Add(player);
            }

            await context.SaveChangesAsync();
            
            return new ServiceResult<PlayerEntity>
            {
                Success = true,
                Data = existing ?? player,
                Message = "Player saved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving player entity {PlayerId}", player.Id);
            return new ServiceResult<PlayerEntity>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    private async Task<List<PlayerEntity>> GetOnlinePlayerEntitiesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Players.Where(p => p.IsOnline).ToListAsync();
    }

    private async Task<ApiResponse<bool>> DeletePlayerEntityAsync(string playerId)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var player = await context.Players.FirstOrDefaultAsync(p => p.Id == playerId);
            
            if (player == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Player not found"
                };
            }

            context.Players.Remove(player);
            await context.SaveChangesAsync();

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Player deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting player {PlayerId}", playerId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        if (!_disposed)
        {
            _batchProcessor?.Dispose();
            _batchSemaphore?.Dispose();
            _cancellationTokenSource?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}

/// <summary>
/// Interface for batch operations
/// </summary>
public interface IBatchOperation
{
    Task ExecuteAsync(ConsolidatedGameDbContext context);
}
