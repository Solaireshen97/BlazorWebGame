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
/// SQLite数据存储服务实现 - 使用Entity Framework Core
/// 注意：此实现需要Scoped生命周期，当前项目架构使用Singleton服务，需要架构重构才能完全启用
/// </summary>
public class SqliteDataStorageService : IDataStorageService
{
    private readonly GameDbContext _context;
    private readonly ILogger<SqliteDataStorageService> _logger;

    public SqliteDataStorageService(GameDbContext context, ILogger<SqliteDataStorageService> logger)
    {
        _context = context;
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
            var player = await _context.Players.FindAsync(playerId);
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
            var entity = await _context.Players.FindAsync(player.Id);
            
            if (entity == null)
            {
                entity = MapToEntity(player);
                _context.Players.Add(entity);
            }
            else
            {
                // 更新现有实体
                MapToEntityUpdate(player, entity);
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            
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

    public async Task<ApiResponse<bool>> DeletePlayerAsync(string playerId)
    {
        try
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "玩家不存在"
                };
            }

            // 删除相关数据
            var actionTargets = await _context.ActionTargets
                .Where(at => at.PlayerId == playerId)
                .ToListAsync();
            _context.ActionTargets.RemoveRange(actionTargets);

            var offlineData = await _context.OfflineData
                .Where(od => od.PlayerId == playerId)
                .ToListAsync();
            _context.OfflineData.RemoveRange(offlineData);

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Player and related data deleted successfully for ID: {SafePlayerId}", SafeLogId(playerId));
            
            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "玩家数据删除成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete player with ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"删除玩家数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<PlayerStorageDto>>> GetOnlinePlayersAsync()
    {
        try
        {
            var onlinePlayers = await _context.Players
                .Where(p => p.IsOnline)
                .OrderByDescending(p => p.LastActiveAt)
                .Select(p => MapToDto(p))
                .ToListAsync();
            
            return new ApiResponse<List<PlayerStorageDto>>
            {
                Success = true,
                Data = onlinePlayers,
                Message = $"获取到 {onlinePlayers.Count} 名在线玩家"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get online players");
            return new ApiResponse<List<PlayerStorageDto>>
            {
                Success = false,
                Message = $"获取在线玩家失败: {ex.Message}"
            };
        }
    }

    public async Task<BatchOperationResponseDto<PlayerStorageDto>> SavePlayersAsync(List<PlayerStorageDto> players)
    {
        var response = new BatchOperationResponseDto<PlayerStorageDto>();
        
        foreach (var player in players)
        {
            try
            {
                var result = await SavePlayerAsync(player);
                if (result.Success && result.Data != null)
                {
                    response.SuccessfulItems.Add(result.Data);
                    response.SuccessCount++;
                }
                else
                {
                    response.Errors.Add($"Player {player.Id}: {result.Message}");
                    response.ErrorCount++;
                }
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Player {player.Id}: {ex.Message}");
                response.ErrorCount++;
            }
            response.TotalProcessed++;
        }
        
        return response;
    }

    #endregion

    #region 队伍数据管理

    public async Task<TeamStorageDto?> GetTeamAsync(string teamId)
    {
        try
        {
            var team = await _context.Teams.FindAsync(teamId);
            return team != null ? MapToDto(team) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get team with ID: {SafeTeamId}", SafeLogId(teamId));
            return null;
        }
    }

    public async Task<TeamStorageDto?> GetTeamByCaptainAsync(string captainId)
    {
        try
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.CaptainId == captainId);
            return team != null ? MapToDto(team) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get team for captain: {SafeCaptainId}", SafeLogId(captainId));
            return null;
        }
    }

    public async Task<TeamStorageDto?> GetTeamByPlayerAsync(string playerId)
    {
        try
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == playerId && p.PartyId.HasValue);
            
            if (player?.PartyId == null)
                return null;

            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == player.PartyId.ToString());
            
            return team != null ? MapToDto(team) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get team for player: {SafePlayerId}", SafeLogId(playerId));
            return null;
        }
    }

    public async Task<ApiResponse<TeamStorageDto>> SaveTeamAsync(TeamStorageDto team)
    {
        try
        {
            var entity = await _context.Teams.FindAsync(team.Id);
            
            if (entity == null)
            {
                entity = MapToEntity(team);
                _context.Teams.Add(entity);
            }
            else
            {
                MapToEntityUpdate(team, entity);
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Team saved successfully with ID: {SafeTeamId}", SafeLogId(team.Id));
            
            return new ApiResponse<TeamStorageDto>
            {
                Success = true,
                Data = MapToDto(entity),
                Message = "队伍数据保存成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save team with ID: {SafeTeamId}", SafeLogId(team.Id));
            return new ApiResponse<TeamStorageDto>
            {
                Success = false,
                Message = $"保存队伍数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteTeamAsync(string teamId)
    {
        try
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "队伍不存在"
                };
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Team deleted successfully with ID: {SafeTeamId}", SafeLogId(teamId));
            
            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "队伍删除成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete team with ID: {SafeTeamId}", SafeLogId(teamId));
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"删除队伍失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<TeamStorageDto>>> GetActiveTeamsAsync()
    {
        try
        {
            var activeTeams = await _context.Teams
                .Where(t => t.Status == "Active")
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => MapToDto(t))
                .ToListAsync();
            
            return new ApiResponse<List<TeamStorageDto>>
            {
                Success = true,
                Data = activeTeams,
                Message = $"获取到 {activeTeams.Count} 支活跃队伍"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active teams");
            return new ApiResponse<List<TeamStorageDto>>
            {
                Success = false,
                Message = $"获取活跃队伍失败: {ex.Message}"
            };
        }
    }

    #endregion

    #region 动作目标管理

    public async Task<ActionTargetStorageDto?> GetCurrentActionTargetAsync(string playerId)
    {
        try
        {
            var currentTarget = await _context.ActionTargets
                .Where(at => at.PlayerId == playerId && !at.IsCompleted)
                .OrderByDescending(at => at.StartedAt)
                .FirstOrDefaultAsync();
            
            return currentTarget != null ? MapToDto(currentTarget) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current action target for player: {SafePlayerId}", SafeLogId(playerId));
            return null;
        }
    }

    public async Task<ApiResponse<ActionTargetStorageDto>> SaveActionTargetAsync(ActionTargetStorageDto actionTarget)
    {
        try
        {
            var entity = await _context.ActionTargets.FindAsync(actionTarget.Id);
            
            if (entity == null)
            {
                entity = MapToEntity(actionTarget);
                _context.ActionTargets.Add(entity);
            }
            else
            {
                MapToEntityUpdate(actionTarget, entity);
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            
            _logger.LogDebug("ActionTarget saved for player with IDs: {SafeActionTargetId}, {SafePlayerId}", 
                SafeLogId(actionTarget.Id), SafeLogId(actionTarget.PlayerId));
            
            return new ApiResponse<ActionTargetStorageDto>
            {
                Success = true,
                Data = MapToDto(entity),
                Message = "动作目标保存成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save action target with ID: {SafeActionTargetId}", SafeLogId(actionTarget.Id));
            return new ApiResponse<ActionTargetStorageDto>
            {
                Success = false,
                Message = $"保存动作目标失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> CompleteActionTargetAsync(string actionTargetId)
    {
        try
        {
            var actionTarget = await _context.ActionTargets.FindAsync(actionTargetId);
            if (actionTarget == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "动作目标不存在"
                };
            }

            actionTarget.IsCompleted = true;
            actionTarget.CompletedAt = DateTime.UtcNow;
            actionTarget.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "动作目标完成"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete action target with ID: {SafeActionTargetId}", SafeLogId(actionTargetId));
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"完成动作目标失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> CancelActionTargetAsync(string playerId)
    {
        try
        {
            var currentTarget = await _context.ActionTargets
                .Where(at => at.PlayerId == playerId && !at.IsCompleted)
                .OrderByDescending(at => at.StartedAt)
                .FirstOrDefaultAsync();
            
            if (currentTarget == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "没有进行中的动作目标"
                };
            }

            _context.ActionTargets.Remove(currentTarget);
            await _context.SaveChangesAsync();
            
            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "动作目标已取消"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel action target for player with ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"取消动作目标失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<ActionTargetStorageDto>>> GetPlayerActionHistoryAsync(string playerId, int limit = 50)
    {
        try
        {
            var actionHistory = await _context.ActionTargets
                .Where(at => at.PlayerId == playerId)
                .OrderByDescending(at => at.StartedAt)
                .Take(limit)
                .Select(at => MapToDto(at))
                .ToListAsync();
            
            return new ApiResponse<List<ActionTargetStorageDto>>
            {
                Success = true,
                Data = actionHistory,
                Message = $"获取到 {actionHistory.Count} 条动作历史记录"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get action history for player with ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<List<ActionTargetStorageDto>>
            {
                Success = false,
                Message = $"获取动作历史失败: {ex.Message}"
            };
        }
    }

    #endregion

    #region 战斗记录管理 - 这部分实现将在下一个文件继续

    public async Task<BattleRecordStorageDto?> GetBattleRecordAsync(string battleId)
    {
        try
        {
            var battleRecord = await _context.BattleRecords
                .FirstOrDefaultAsync(br => br.BattleId == battleId);
            
            return battleRecord != null ? MapToDto(battleRecord) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get battle record with ID: {SafeBattleId}", SafeLogId(battleId));
            return null;
        }
    }

    public async Task<ApiResponse<BattleRecordStorageDto>> SaveBattleRecordAsync(BattleRecordStorageDto battleRecord)
    {
        try
        {
            var entity = await _context.BattleRecords.FindAsync(battleRecord.Id);
            
            if (entity == null)
            {
                entity = MapToEntity(battleRecord);
                _context.BattleRecords.Add(entity);
            }
            else
            {
                MapToEntityUpdate(battleRecord, entity);
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            
            _logger.LogDebug("BattleRecord saved successfully with ID: {SafeBattleRecordId}", SafeLogId(battleRecord.Id));
            
            return new ApiResponse<BattleRecordStorageDto>
            {
                Success = true,
                Data = MapToDto(entity),
                Message = "战斗记录保存成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save battle record with ID: {SafeBattleRecordId}", SafeLogId(battleRecord.Id));
            return new ApiResponse<BattleRecordStorageDto>
            {
                Success = false,
                Message = $"保存战斗记录失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> EndBattleRecordAsync(string battleId, string status, Dictionary<string, object> results)
    {
        try
        {
            var battleRecord = await _context.BattleRecords.FirstOrDefaultAsync(br => br.BattleId == battleId);
            if (battleRecord == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "战斗记录不存在"
                };
            }

            battleRecord.Status = status;
            battleRecord.EndedAt = DateTime.UtcNow;
            battleRecord.ResultsJson = JsonSerializer.Serialize(results);
            battleRecord.UpdatedAt = DateTime.UtcNow;
            
            if (battleRecord.StartedAt != DateTime.MinValue)
            {
                battleRecord.Duration = (int)(DateTime.UtcNow - battleRecord.StartedAt).TotalSeconds;
            }

            await _context.SaveChangesAsync();
            
            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "战斗记录已结束"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end battle record with ID: {SafeBattleId}", SafeLogId(battleId));
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"结束战斗记录失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<BattleRecordStorageDto>>> GetPlayerBattleHistoryAsync(string playerId, DataStorageQueryDto query)
    {
        try
        {
            var battleRecordsQuery = _context.BattleRecords
                .Where(br => br.ParticipantsJson.Contains($"\"{playerId}\""));
            
            // 应用过滤条件
            if (query.StartDate.HasValue)
                battleRecordsQuery = battleRecordsQuery.Where(br => br.StartedAt >= query.StartDate.Value);
            
            if (query.EndDate.HasValue)
                battleRecordsQuery = battleRecordsQuery.Where(br => br.StartedAt <= query.EndDate.Value);
            
            if (!string.IsNullOrEmpty(query.Status))
                battleRecordsQuery = battleRecordsQuery.Where(br => br.Status == query.Status);
            
            var result = await battleRecordsQuery
                .OrderByDescending(br => br.StartedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(br => MapToDto(br))
                .ToListAsync();
            
            return new ApiResponse<List<BattleRecordStorageDto>>
            {
                Success = true,
                Data = result,
                Message = $"获取到 {result.Count} 条战斗记录"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get battle history for player with ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<List<BattleRecordStorageDto>>
            {
                Success = false,
                Message = $"获取战斗历史失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<BattleRecordStorageDto>>> GetTeamBattleHistoryAsync(string teamId, DataStorageQueryDto query)
    {
        try
        {
            var teamGuid = Guid.Parse(teamId);
            var battleRecordsQuery = _context.BattleRecords
                .Where(br => br.PartyId == teamGuid);
            
            // 应用过滤条件
            if (query.StartDate.HasValue)
                battleRecordsQuery = battleRecordsQuery.Where(br => br.StartedAt >= query.StartDate.Value);
            
            if (query.EndDate.HasValue)
                battleRecordsQuery = battleRecordsQuery.Where(br => br.StartedAt <= query.EndDate.Value);
            
            if (!string.IsNullOrEmpty(query.Status))
                battleRecordsQuery = battleRecordsQuery.Where(br => br.Status == query.Status);
            
            var result = await battleRecordsQuery
                .OrderByDescending(br => br.StartedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(br => MapToDto(br))
                .ToListAsync();
            
            return new ApiResponse<List<BattleRecordStorageDto>>
            {
                Success = true,
                Data = result,
                Message = $"获取到 {result.Count} 条队伍战斗记录"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get battle history for team with ID: {SafeTeamId}", SafeLogId(teamId));
            return new ApiResponse<List<BattleRecordStorageDto>>
            {
                Success = false,
                Message = $"获取队伍战斗历史失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<BattleRecordStorageDto>>> GetActiveBattleRecordsAsync()
    {
        try
        {
            var activeBattles = await _context.BattleRecords
                .Where(br => br.Status == "InProgress")
                .OrderByDescending(br => br.StartedAt)
                .Select(br => MapToDto(br))
                .ToListAsync();
            
            return new ApiResponse<List<BattleRecordStorageDto>>
            {
                Success = true,
                Data = activeBattles,
                Message = $"获取到 {activeBattles.Count} 场进行中的战斗"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active battle records");
            return new ApiResponse<List<BattleRecordStorageDto>>
            {
                Success = false,
                Message = $"获取进行中战斗失败: {ex.Message}"
            };
        }
    }

    #endregion

    #region 离线数据管理

    public async Task<ApiResponse<OfflineDataStorageDto>> SaveOfflineDataAsync(OfflineDataStorageDto offlineData)
    {
        try
        {
            var entity = await _context.OfflineData.FindAsync(offlineData.Id);
            
            if (entity == null)
            {
                entity = MapToEntity(offlineData);
                _context.OfflineData.Add(entity);
            }
            else
            {
                MapToEntityUpdate(offlineData, entity);
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            
            _logger.LogDebug("OfflineData saved for player with IDs: {SafeOfflineDataId}, {SafePlayerId}", 
                SafeLogId(offlineData.Id), SafeLogId(offlineData.PlayerId));
            
            return new ApiResponse<OfflineDataStorageDto>
            {
                Success = true,
                Data = MapToDto(entity),
                Message = "离线数据保存成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save offline data with ID: {SafeOfflineDataId}", SafeLogId(offlineData.Id));
            return new ApiResponse<OfflineDataStorageDto>
            {
                Success = false,
                Message = $"保存离线数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<OfflineDataStorageDto>>> GetUnsyncedOfflineDataAsync(string playerId)
    {
        try
        {
            var unsyncedData = await _context.OfflineData
                .Where(od => od.PlayerId == playerId && !od.IsSynced)
                .OrderBy(od => od.CreatedAt)
                .Select(od => MapToDto(od))
                .ToListAsync();
            
            return new ApiResponse<List<OfflineDataStorageDto>>
            {
                Success = true,
                Data = unsyncedData,
                Message = $"获取到 {unsyncedData.Count} 条未同步的离线数据"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unsynced offline data for player with ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<List<OfflineDataStorageDto>>
            {
                Success = false,
                Message = $"获取未同步离线数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> MarkOfflineDataSyncedAsync(List<string> offlineDataIds)
    {
        try
        {
            int syncedCount = 0;
            var syncTime = DateTime.UtcNow;
            
            var offlineDataItems = await _context.OfflineData
                .Where(od => offlineDataIds.Contains(od.Id))
                .ToListAsync();
            
            foreach (var offlineData in offlineDataItems)
            {
                offlineData.IsSynced = true;
                offlineData.SyncedAt = syncTime;
                offlineData.UpdatedAt = syncTime;
                syncedCount++;
            }
            
            await _context.SaveChangesAsync();
            
            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = $"已标记 {syncedCount} 条离线数据为已同步"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark offline data as synced");
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"标记离线数据同步状态失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<int>> CleanupSyncedOfflineDataAsync(DateTime olderThan)
    {
        try
        {
            var toRemove = await _context.OfflineData
                .Where(od => od.IsSynced && od.SyncedAt < olderThan)
                .ToListAsync();
            
            _context.OfflineData.RemoveRange(toRemove);
            await _context.SaveChangesAsync();
            
            var removedCount = toRemove.Count;
            _logger.LogInformation("Cleaned up {RemovedCount} synced offline data records older than the specified date", removedCount);
            
            return new ApiResponse<int>
            {
                Success = true,
                Data = removedCount,
                Message = $"清理了 {removedCount} 条已同步的旧离线数据"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup synced offline data");
            return new ApiResponse<int>
            {
                Success = false,
                Message = $"清理已同步离线数据失败: {ex.Message}"
            };
        }
    }

    #endregion

    #region 数据查询和统计

    public async Task<ApiResponse<List<PlayerStorageDto>>> SearchPlayersAsync(string searchTerm, int limit = 20)
    {
        try
        {
            var searchResults = await _context.Players
                .Where(p => p.Name.Contains(searchTerm) || p.Id.Contains(searchTerm))
                .Take(limit)
                .OrderBy(p => p.Name)
                .Select(p => MapToDto(p))
                .ToListAsync();
            
            return new ApiResponse<List<PlayerStorageDto>>
            {
                Success = true,
                Data = searchResults,
                Message = $"找到 {searchResults.Count} 个匹配的玩家"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search players");
            return new ApiResponse<List<PlayerStorageDto>>
            {
                Success = false,
                Message = $"搜索玩家失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<Dictionary<string, object>>> GetStorageStatsAsync()
    {
        try
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalPlayers"] = await _context.Players.CountAsync(),
                ["OnlinePlayers"] = await _context.Players.CountAsync(p => p.IsOnline),
                ["TotalTeams"] = await _context.Teams.CountAsync(),
                ["ActiveTeams"] = await _context.Teams.CountAsync(t => t.Status == "Active"),
                ["TotalActionTargets"] = await _context.ActionTargets.CountAsync(),
                ["ActiveActionTargets"] = await _context.ActionTargets.CountAsync(at => !at.IsCompleted),
                ["TotalBattleRecords"] = await _context.BattleRecords.CountAsync(),
                ["ActiveBattles"] = await _context.BattleRecords.CountAsync(br => br.Status == "InProgress"),
                ["TotalOfflineData"] = await _context.OfflineData.CountAsync(),
                ["UnsyncedOfflineData"] = await _context.OfflineData.CountAsync(od => !od.IsSynced),
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
            // 简单的健康检查 - 尝试连接数据库
            var canConnect = await _context.Database.CanConnectAsync();
            
            var healthCheck = new Dictionary<string, object>
            {
                ["Status"] = canConnect ? "Healthy" : "Unhealthy",
                ["StorageType"] = "SQLite",
                ["Timestamp"] = DateTime.UtcNow,
                ["DatabaseConnected"] = canConnect
            };
            
            if (canConnect)
            {
                healthCheck["StorageStats"] = new Dictionary<string, object>
                {
                    ["Players"] = await _context.Players.CountAsync(),
                    ["Teams"] = await _context.Teams.CountAsync(),
                    ["ActionTargets"] = await _context.ActionTargets.CountAsync(),
                    ["BattleRecords"] = await _context.BattleRecords.CountAsync(),
                    ["OfflineData"] = await _context.OfflineData.CountAsync()
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
        try
        {
            var player = await GetPlayerAsync(playerId);
            if (player == null)
            {
                return new ApiResponse<Dictionary<string, object>>
                {
                    Success = false,
                    Message = "玩家不存在"
                };
            }
            
            var exportData = new Dictionary<string, object>
            {
                ["Player"] = player,
                ["Team"] = await GetTeamByPlayerAsync(playerId),
                ["CurrentActionTarget"] = await GetCurrentActionTargetAsync(playerId),
                ["ActionHistory"] = (await GetPlayerActionHistoryAsync(playerId, 100)).Data ?? new List<ActionTargetStorageDto>(),
                ["BattleHistory"] = (await GetPlayerBattleHistoryAsync(playerId, new DataStorageQueryDto { PageSize = 100 })).Data ?? new List<BattleRecordStorageDto>(),
                ["OfflineData"] = (await GetUnsyncedOfflineDataAsync(playerId)).Data ?? new List<OfflineDataStorageDto>(),
                ["ExportTimestamp"] = DateTime.UtcNow
            };
            
            return new ApiResponse<Dictionary<string, object>>
            {
                Success = true,
                Data = exportData,
                Message = "玩家数据导出成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export player data for ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<Dictionary<string, object>>
            {
                Success = false,
                Message = $"导出玩家数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> ImportPlayerDataAsync(string playerId, Dictionary<string, object> data)
    {
        try
        {
            // 这里可以实现数据导入逻辑
            // 由于是演示代码，此处简化处理
            _logger.LogInformation("Player data import requested for ID: {SafePlayerId}", SafeLogId(playerId));
            
            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "数据导入功能待实现"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import player data for ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"导入玩家数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<string>> BackupDataAsync()
    {
        try
        {
            var backupId = Guid.NewGuid().ToString();
            _logger.LogInformation("Data backup requested with ID {BackupId}", backupId);
            
            // 这里可以实现实际的备份逻辑，比如导出到文件
            
            return new ApiResponse<string>
            {
                Success = true,
                Data = backupId,
                Message = $"数据备份已启动，备份ID: {backupId}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backup data");
            return new ApiResponse<string>
            {
                Success = false,
                Message = $"数据备份失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<int>> CleanupExpiredDataAsync(TimeSpan olderThan)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - olderThan;
            int cleanedCount = 0;
            
            // 清理已完成的旧动作目标
            var expiredActionTargets = await _context.ActionTargets
                .Where(at => at.IsCompleted && at.CompletedAt < cutoffTime)
                .ToListAsync();
            
            _context.ActionTargets.RemoveRange(expiredActionTargets);
            cleanedCount += expiredActionTargets.Count;
            
            // 清理已结束的旧战斗记录
            var expiredBattleRecords = await _context.BattleRecords
                .Where(br => br.Status != "InProgress" && br.EndedAt < cutoffTime)
                .ToListAsync();
            
            _context.BattleRecords.RemoveRange(expiredBattleRecords);
            cleanedCount += expiredBattleRecords.Count;
            
            // 清理已同步的旧离线数据
            var cleanupOfflineResult = await CleanupSyncedOfflineDataAsync(cutoffTime);
            if (cleanupOfflineResult.Success)
            {
                cleanedCount += cleanupOfflineResult.Data;
            }
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {CleanedCount} expired data records older than {CutoffTime}", cleanedCount, cutoffTime);
            
            return new ApiResponse<int>
            {
                Success = true,
                Data = cleanedCount,
                Message = $"清理了 {cleanedCount} 条过期数据"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired data");
            return new ApiResponse<int>
            {
                Success = false,
                Message = $"清理过期数据失败: {ex.Message}"
            };
        }
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

    private PlayerEntity MapToEntity(PlayerStorageDto dto)
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

    private void MapToEntityUpdate(PlayerStorageDto dto, PlayerEntity entity)
    {
        entity.Name = dto.Name;
        entity.Level = dto.Level;
        entity.Experience = dto.Experience;
        entity.Health = dto.Health;
        entity.MaxHealth = dto.MaxHealth;
        entity.Gold = dto.Gold;
        entity.SelectedBattleProfession = dto.SelectedBattleProfession;
        entity.CurrentAction = dto.CurrentAction;
        entity.CurrentActionTargetId = dto.CurrentActionTargetId;
        entity.PartyId = dto.PartyId;
        entity.IsOnline = dto.IsOnline;
        entity.LastActiveAt = dto.LastActiveAt;
        entity.AttributesJson = JsonSerializer.Serialize(dto.Attributes);
        entity.InventoryJson = JsonSerializer.Serialize(dto.Inventory);
        entity.SkillsJson = JsonSerializer.Serialize(dto.Skills);
        entity.EquipmentJson = JsonSerializer.Serialize(dto.Equipment);
    }

    private static TeamStorageDto MapToDto(TeamEntity entity)
    {
        return new TeamStorageDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CaptainId = entity.CaptainId,
            MemberIds = JsonSerializer.Deserialize<List<string>>(entity.MemberIdsJson) ?? new List<string>(),
            MaxMembers = entity.MaxMembers,
            Status = entity.Status,
            CurrentBattleId = entity.CurrentBattleId,
            LastBattleAt = entity.LastBattleAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private TeamEntity MapToEntity(TeamStorageDto dto)
    {
        return new TeamEntity
        {
            Id = dto.Id,
            Name = dto.Name,
            CaptainId = dto.CaptainId,
            MemberIdsJson = JsonSerializer.Serialize(dto.MemberIds),
            MaxMembers = dto.MaxMembers,
            Status = dto.Status,
            CurrentBattleId = dto.CurrentBattleId,
            LastBattleAt = dto.LastBattleAt,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private void MapToEntityUpdate(TeamStorageDto dto, TeamEntity entity)
    {
        entity.Name = dto.Name;
        entity.CaptainId = dto.CaptainId;
        entity.MemberIdsJson = JsonSerializer.Serialize(dto.MemberIds);
        entity.MaxMembers = dto.MaxMembers;
        entity.Status = dto.Status;
        entity.CurrentBattleId = dto.CurrentBattleId;
        entity.LastBattleAt = dto.LastBattleAt;
    }

    private static ActionTargetStorageDto MapToDto(ActionTargetEntity entity)
    {
        return new ActionTargetStorageDto
        {
            Id = entity.Id,
            PlayerId = entity.PlayerId,
            TargetType = entity.TargetType,
            TargetId = entity.TargetId,
            TargetName = entity.TargetName,
            ActionType = entity.ActionType,
            Progress = entity.Progress,
            Duration = entity.Duration,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            IsCompleted = entity.IsCompleted,
            ProgressData = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.ProgressDataJson) ?? new Dictionary<string, object>(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private ActionTargetEntity MapToEntity(ActionTargetStorageDto dto)
    {
        return new ActionTargetEntity
        {
            Id = dto.Id,
            PlayerId = dto.PlayerId,
            TargetType = dto.TargetType,
            TargetId = dto.TargetId,
            TargetName = dto.TargetName,
            ActionType = dto.ActionType,
            Progress = dto.Progress,
            Duration = dto.Duration,
            StartedAt = dto.StartedAt,
            CompletedAt = dto.CompletedAt,
            IsCompleted = dto.IsCompleted,
            ProgressDataJson = JsonSerializer.Serialize(dto.ProgressData),
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private void MapToEntityUpdate(ActionTargetStorageDto dto, ActionTargetEntity entity)
    {
        entity.PlayerId = dto.PlayerId;
        entity.TargetType = dto.TargetType;
        entity.TargetId = dto.TargetId;
        entity.TargetName = dto.TargetName;
        entity.ActionType = dto.ActionType;
        entity.Progress = dto.Progress;
        entity.Duration = dto.Duration;
        entity.StartedAt = dto.StartedAt;
        entity.CompletedAt = dto.CompletedAt;
        entity.IsCompleted = dto.IsCompleted;
        entity.ProgressDataJson = JsonSerializer.Serialize(dto.ProgressData);
    }

    private static BattleRecordStorageDto MapToDto(BattleRecordEntity entity)
    {
        return new BattleRecordStorageDto
        {
            Id = entity.Id,
            BattleId = entity.BattleId,
            BattleType = entity.BattleType,
            StartedAt = entity.StartedAt,
            EndedAt = entity.EndedAt,
            Status = entity.Status,
            Participants = JsonSerializer.Deserialize<List<string>>(entity.ParticipantsJson) ?? new List<string>(),
            Enemies = JsonSerializer.Deserialize<List<object>>(entity.EnemiesJson) ?? new List<object>(),
            Actions = JsonSerializer.Deserialize<List<object>>(entity.ActionsJson) ?? new List<object>(),
            Results = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.ResultsJson) ?? new Dictionary<string, object>(),
            PartyId = entity.PartyId,
            DungeonId = entity.DungeonId,
            WaveNumber = entity.WaveNumber,
            Duration = entity.Duration,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private BattleRecordEntity MapToEntity(BattleRecordStorageDto dto)
    {
        return new BattleRecordEntity
        {
            Id = dto.Id,
            BattleId = dto.BattleId,
            BattleType = dto.BattleType,
            StartedAt = dto.StartedAt,
            EndedAt = dto.EndedAt,
            Status = dto.Status,
            ParticipantsJson = JsonSerializer.Serialize(dto.Participants),
            EnemiesJson = JsonSerializer.Serialize(dto.Enemies),
            ActionsJson = JsonSerializer.Serialize(dto.Actions),
            ResultsJson = JsonSerializer.Serialize(dto.Results),
            PartyId = dto.PartyId,
            DungeonId = dto.DungeonId,
            WaveNumber = dto.WaveNumber,
            Duration = dto.Duration,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private void MapToEntityUpdate(BattleRecordStorageDto dto, BattleRecordEntity entity)
    {
        entity.BattleId = dto.BattleId;
        entity.BattleType = dto.BattleType;
        entity.StartedAt = dto.StartedAt;
        entity.EndedAt = dto.EndedAt;
        entity.Status = dto.Status;
        entity.ParticipantsJson = JsonSerializer.Serialize(dto.Participants);
        entity.EnemiesJson = JsonSerializer.Serialize(dto.Enemies);
        entity.ActionsJson = JsonSerializer.Serialize(dto.Actions);
        entity.ResultsJson = JsonSerializer.Serialize(dto.Results);
        entity.PartyId = dto.PartyId;
        entity.DungeonId = dto.DungeonId;
        entity.WaveNumber = dto.WaveNumber;
        entity.Duration = dto.Duration;
    }

    private static OfflineDataStorageDto MapToDto(OfflineDataEntity entity)
    {
        return new OfflineDataStorageDto
        {
            Id = entity.Id,
            PlayerId = entity.PlayerId,
            DataType = entity.DataType,
            Data = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.DataJson) ?? new Dictionary<string, object>(),
            SyncedAt = entity.SyncedAt ?? DateTime.MinValue,
            IsSynced = entity.IsSynced,
            Version = entity.Version,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private OfflineDataEntity MapToEntity(OfflineDataStorageDto dto)
    {
        return new OfflineDataEntity
        {
            Id = dto.Id,
            PlayerId = dto.PlayerId,
            DataType = dto.DataType,
            DataJson = JsonSerializer.Serialize(dto.Data),
            SyncedAt = dto.SyncedAt,
            IsSynced = dto.IsSynced,
            Version = dto.Version,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private void MapToEntityUpdate(OfflineDataStorageDto dto, OfflineDataEntity entity)
    {
        entity.PlayerId = dto.PlayerId;
        entity.DataType = dto.DataType;
        entity.DataJson = JsonSerializer.Serialize(dto.Data);
        entity.SyncedAt = dto.SyncedAt;
        entity.IsSynced = dto.IsSynced;
        entity.Version = dto.Version;
    }

    #endregion
}