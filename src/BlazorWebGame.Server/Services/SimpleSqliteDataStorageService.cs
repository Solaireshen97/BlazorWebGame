using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Shared.Models;
using BlazorWebGame.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 简化的SQLite数据存储服务实现 - 基于Entity Framework Core，使用DbContextFactory
/// </summary>
public class SimpleSqliteDataStorageService : IDataStorageService
{
    private readonly IDbContextFactory<GameDbContext> _contextFactory;
    private readonly ILogger<SimpleSqliteDataStorageService> _logger;

    public SimpleSqliteDataStorageService(IDbContextFactory<GameDbContext> contextFactory, ILogger<SimpleSqliteDataStorageService> logger)
    {
        _contextFactory = contextFactory;
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

    #region 玩家数据管理

    public async Task<PlayerStorageDto?> GetPlayerAsync(string playerId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var player = await context.Players.FindAsync(playerId);
            return player != null ? MapToDto(player) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get player with ID: {SafePlayerId}", SafeLogId(playerId));
            return null;
        }
    }

    public async Task<ApiResponse<PlayerStorageDto>> SavePlayerAsync(PlayerStorageDto player)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var entity = MapToEntity(player);
            entity.UpdatedAt = DateTime.UtcNow;
            
            var existing = await context.Players.FindAsync(player.Id);
            if (existing != null)
            {
                // 更新现有玩家
                context.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                // 添加新玩家
                await context.Players.AddAsync(entity);
            }
            
            await context.SaveChangesAsync();
            
            _logger.LogDebug("Player saved successfully with ID: {SafePlayerId}", SafeLogId(player.Id));
            
            return new ApiResponse<PlayerStorageDto>
            {
                Success = true,
                Data = MapToDto(entity),
                Message = "玩家数据保存成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save player with ID: {SafePlayerId}", SafeLogId(player.Id));
            return new ApiResponse<PlayerStorageDto>
            {
                Success = false,
                Message = $"保存玩家数据失败: {ex.Message}"
            };
        }
    }

    // 为了保持接口兼容性，实现所有必需的方法，但大部分暂时返回未实现错误
    public async Task<ApiResponse<bool>> DeletePlayerAsync(string playerId)
    {
        return new ApiResponse<bool> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<List<PlayerStorageDto>>> GetOnlinePlayersAsync()
    {
        return new ApiResponse<List<PlayerStorageDto>> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<BatchOperationResponseDto<PlayerStorageDto>> SavePlayersAsync(List<PlayerStorageDto> players)
    {
        return new BatchOperationResponseDto<PlayerStorageDto>();
    }

    #endregion

    #region 队伍数据管理

    public async Task<TeamStorageDto?> GetTeamAsync(string teamId)
    {
        return null;
    }

    public async Task<TeamStorageDto?> GetTeamByCaptainAsync(string captainId)
    {
        return null;
    }

    public async Task<TeamStorageDto?> GetTeamByPlayerAsync(string playerId)
    {
        return null;
    }

    public async Task<ApiResponse<TeamStorageDto>> SaveTeamAsync(TeamStorageDto team)
    {
        return new ApiResponse<TeamStorageDto> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<bool>> DeleteTeamAsync(string teamId)
    {
        return new ApiResponse<bool> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<List<TeamStorageDto>>> GetActiveTeamsAsync()
    {
        return new ApiResponse<List<TeamStorageDto>> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    #endregion

    #region 动作目标管理

    public async Task<ActionTargetStorageDto?> GetCurrentActionTargetAsync(string playerId)
    {
        return null;
    }

    public async Task<ApiResponse<ActionTargetStorageDto>> SaveActionTargetAsync(ActionTargetStorageDto actionTarget)
    {
        return new ApiResponse<ActionTargetStorageDto> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<bool>> CompleteActionTargetAsync(string actionTargetId)
    {
        return new ApiResponse<bool> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<bool>> CancelActionTargetAsync(string playerId)
    {
        return new ApiResponse<bool> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<List<ActionTargetStorageDto>>> GetPlayerActionHistoryAsync(string playerId, int limit = 50)
    {
        return new ApiResponse<List<ActionTargetStorageDto>> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    #endregion

    #region 战斗记录管理

    public async Task<BattleRecordStorageDto?> GetBattleRecordAsync(string battleId)
    {
        return null;
    }

    public async Task<ApiResponse<BattleRecordStorageDto>> SaveBattleRecordAsync(BattleRecordStorageDto battleRecord)
    {
        return new ApiResponse<BattleRecordStorageDto> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<bool>> EndBattleRecordAsync(string battleId, string status, Dictionary<string, object> results)
    {
        return new ApiResponse<bool> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<List<BattleRecordStorageDto>>> GetPlayerBattleHistoryAsync(string playerId, DataStorageQueryDto query)
    {
        return new ApiResponse<List<BattleRecordStorageDto>> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<List<BattleRecordStorageDto>>> GetTeamBattleHistoryAsync(string teamId, DataStorageQueryDto query)
    {
        return new ApiResponse<List<BattleRecordStorageDto>> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<List<BattleRecordStorageDto>>> GetActiveBattleRecordsAsync()
    {
        return new ApiResponse<List<BattleRecordStorageDto>> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    #endregion

    #region 离线数据管理

    public async Task<ApiResponse<OfflineDataStorageDto>> SaveOfflineDataAsync(OfflineDataStorageDto offlineData)
    {
        return new ApiResponse<OfflineDataStorageDto> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<List<OfflineDataStorageDto>>> GetUnsyncedOfflineDataAsync(string playerId)
    {
        return new ApiResponse<List<OfflineDataStorageDto>> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<bool>> MarkOfflineDataSyncedAsync(List<string> offlineDataIds)
    {
        return new ApiResponse<bool> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<int>> CleanupSyncedOfflineDataAsync(DateTime olderThan)
    {
        return new ApiResponse<int> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    #endregion

    #region 数据查询和统计

    public async Task<ApiResponse<List<PlayerStorageDto>>> SearchPlayersAsync(string searchTerm, int limit = 20)
    {
        return new ApiResponse<List<PlayerStorageDto>> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<Dictionary<string, object>>> GetStorageStatsAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var stats = new Dictionary<string, object>
            {
                ["TotalPlayers"] = await context.Players.CountAsync(),
                ["OnlinePlayers"] = await context.Players.CountAsync(p => p.IsOnline),
                ["TotalTeams"] = await context.Teams.CountAsync(),
                ["ActiveTeams"] = await context.Teams.CountAsync(t => t.Status == "Active"),
                ["TotalActionTargets"] = await context.ActionTargets.CountAsync(),
                ["ActiveActionTargets"] = await context.ActionTargets.CountAsync(at => !at.IsCompleted),
                ["TotalBattleRecords"] = await context.BattleRecords.CountAsync(),
                ["ActiveBattles"] = await context.BattleRecords.CountAsync(br => br.Status == "InProgress"),
                ["TotalOfflineData"] = await context.OfflineData.CountAsync(),
                ["UnsyncedOfflineData"] = await context.OfflineData.CountAsync(od => !od.IsSynced),
                ["LastUpdated"] = DateTime.UtcNow
            };
            
            return new ApiResponse<Dictionary<string, object>>
            {
                Success = true,
                Data = stats,
                Message = "存储统计信息获取成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage stats");
            return new ApiResponse<Dictionary<string, object>>
            {
                Success = false,
                Message = $"获取存储统计信息失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<Dictionary<string, object>>> HealthCheckAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            // 测试数据库连接
            var canConnect = await context.Database.CanConnectAsync();
            
            var healthCheck = new Dictionary<string, object>
            {
                ["Status"] = canConnect ? "Healthy" : "Unhealthy",
                ["StorageType"] = "SQLite",
                ["DatabaseConnection"] = canConnect,
                ["Timestamp"] = DateTime.UtcNow
            };

            if (canConnect)
            {
                healthCheck["DatabaseInfo"] = new Dictionary<string, object>
                {
                    ["Players"] = await context.Players.CountAsync(),
                    ["Teams"] = await context.Teams.CountAsync(),
                    ["ActionTargets"] = await context.ActionTargets.CountAsync(),
                    ["BattleRecords"] = await context.BattleRecords.CountAsync(),
                    ["OfflineData"] = await context.OfflineData.CountAsync()
                };
            }
            
            return new ApiResponse<Dictionary<string, object>>
            {
                Success = canConnect,
                Data = healthCheck,
                Message = canConnect ? "数据存储服务健康检查通过" : "数据库连接失败"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return new ApiResponse<Dictionary<string, object>>
            {
                Success = false,
                Data = new Dictionary<string, object>
                {
                    ["Status"] = "Unhealthy",
                    ["Error"] = ex.Message,
                    ["Timestamp"] = DateTime.UtcNow
                },
                Message = $"健康检查失败: {ex.Message}"
            };
        }
    }

    #endregion

    #region 数据同步和备份

    public async Task<ApiResponse<Dictionary<string, object>>> ExportPlayerDataAsync(string playerId)
    {
        return new ApiResponse<Dictionary<string, object>> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<bool>> ImportPlayerDataAsync(string playerId, Dictionary<string, object> data)
    {
        return new ApiResponse<bool> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<string>> BackupDataAsync()
    {
        return new ApiResponse<string> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    public async Task<ApiResponse<int>> CleanupExpiredDataAsync(TimeSpan olderThan)
    {
        return new ApiResponse<int> { Success = false, Message = "SQLite implementation: Method not implemented yet" };
    }

    #endregion

    #region 实体映射方法

    private static PlayerStorageDto MapToDto(PlayerEntity entity)
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
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Attributes = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.AttributesJson) ?? new Dictionary<string, object>(),
            Inventory = JsonSerializer.Deserialize<List<object>>(entity.InventoryJson) ?? new List<object>(),
            Skills = JsonSerializer.Deserialize<List<string>>(entity.SkillsJson) ?? new List<string>(),
            Equipment = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.EquipmentJson) ?? new Dictionary<string, string>()
        };
    }

    private static PlayerEntity MapToEntity(PlayerStorageDto dto)
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
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            AttributesJson = JsonSerializer.Serialize(dto.Attributes),
            InventoryJson = JsonSerializer.Serialize(dto.Inventory),
            SkillsJson = JsonSerializer.Serialize(dto.Skills),
            EquipmentJson = JsonSerializer.Serialize(dto.Equipment)
        };
    }

    #endregion
}
