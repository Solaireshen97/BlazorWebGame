using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BlazorWebGame.Server.Data;

/// <summary>
/// 数据库迁移助手 - 处理数据库架构迁移和数据转换
/// </summary>
public static class DatabaseMigrationHelper
{
    /// <summary>
    /// 执行数据库迁移
    /// </summary>
    public static async Task<bool> MigrateDatabaseAsync(
        UnifiedGameDbContext context, 
        ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting database migration");

            // 确保数据库存在
            await context.Database.EnsureCreatedAsync();

            // 检查是否需要数据迁移
            if (await RequiresDataMigrationAsync(context))
            {
                await PerformDataMigrationAsync(context, logger);
            }

            // 应用SQLite优化
            if (context.Database.IsSqlite())
            {
                await context.EnsureDatabaseOptimizedAsync();
            }

            logger.LogInformation("Database migration completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed");
            return false;
        }
    }

    /// <summary>
    /// 检查是否需要数据迁移
    /// </summary>
    private static async Task<bool> RequiresDataMigrationAsync(UnifiedGameDbContext context)
    {
        try
        {
            // 尝试查询新字段来检测是否需要迁移
            await context.Database.ExecuteSqlRawAsync("SELECT COUNT(*) FROM Players WHERE 1=0");
            return false; // 如果查询成功，说明表结构是最新的
        }
        catch
        {
            return true; // 如果查询失败，可能需要迁移
        }
    }

    /// <summary>
    /// 执行数据迁移
    /// </summary>
    private static async Task PerformDataMigrationAsync(UnifiedGameDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("Performing data migration tasks");

            // 迁移任务1: 确保所有JSON字段有默认值
            await EnsureJsonFieldDefaultsAsync(context, logger);

            // 迁移任务2: 更新索引结构
            await UpdateIndexStructureAsync(context, logger);

            // 迁移任务3: 清理孤立数据
            await CleanOrphanedDataAsync(context, logger);

            logger.LogInformation("Data migration tasks completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data migration tasks failed");
            throw;
        }
    }

    /// <summary>
    /// 确保JSON字段有默认值
    /// </summary>
    private static async Task EnsureJsonFieldDefaultsAsync(UnifiedGameDbContext context, ILogger logger)
    {
        try
        {
            // 更新Players表的JSON字段
            await context.Database.ExecuteSqlRawAsync(@"
                UPDATE Players 
                SET AttributesJson = COALESCE(NULLIF(AttributesJson, ''), '{}'),
                    InventoryJson = COALESCE(NULLIF(InventoryJson, ''), '[]'),
                    SkillsJson = COALESCE(NULLIF(SkillsJson, ''), '[]'),
                    EquipmentJson = COALESCE(NULLIF(EquipmentJson, ''), '{}')
                WHERE AttributesJson IS NULL OR AttributesJson = '' OR
                      InventoryJson IS NULL OR InventoryJson = '' OR
                      SkillsJson IS NULL OR SkillsJson = '' OR
                      EquipmentJson IS NULL OR EquipmentJson = ''
            ");

            // 更新Teams表的JSON字段
            await context.Database.ExecuteSqlRawAsync(@"
                UPDATE Teams 
                SET MemberIdsJson = COALESCE(NULLIF(MemberIdsJson, ''), '[]')
                WHERE MemberIdsJson IS NULL OR MemberIdsJson = ''
            ");

            // 更新ActionTargets表的JSON字段
            await context.Database.ExecuteSqlRawAsync(@"
                UPDATE ActionTargets 
                SET ProgressDataJson = COALESCE(NULLIF(ProgressDataJson, ''), '{}')
                WHERE ProgressDataJson IS NULL OR ProgressDataJson = ''
            ");

            // 更新BattleRecords表的JSON字段
            await context.Database.ExecuteSqlRawAsync(@"
                UPDATE BattleRecords 
                SET ParticipantsJson = COALESCE(NULLIF(ParticipantsJson, ''), '[]'),
                    EnemiesJson = COALESCE(NULLIF(EnemiesJson, ''), '[]'),
                    ActionsJson = COALESCE(NULLIF(ActionsJson, ''), '[]'),
                    ResultsJson = COALESCE(NULLIF(ResultsJson, ''), '{}')
                WHERE ParticipantsJson IS NULL OR ParticipantsJson = '' OR
                      EnemiesJson IS NULL OR EnemiesJson = '' OR
                      ActionsJson IS NULL OR ActionsJson = '' OR
                      ResultsJson IS NULL OR ResultsJson = ''
            ");

            // 更新OfflineData表的JSON字段
            await context.Database.ExecuteSqlRawAsync(@"
                UPDATE OfflineData 
                SET DataJson = COALESCE(NULLIF(DataJson, ''), '{}')
                WHERE DataJson IS NULL OR DataJson = ''
            ");

            logger.LogInformation("JSON field defaults updated successfully");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update JSON field defaults");
        }
    }

    /// <summary>
    /// 更新索引结构
    /// </summary>
    private static async Task UpdateIndexStructureAsync(UnifiedGameDbContext context, ILogger logger)
    {
        try
        {
            if (context.Database.IsSqlite())
            {
                // 重建分析统计以优化查询计划
                await context.Database.ExecuteSqlRawAsync("ANALYZE");
                logger.LogInformation("Database analysis completed");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update index structure");
        }
    }

    /// <summary>
    /// 清理孤立数据
    /// </summary>
    private static async Task CleanOrphanedDataAsync(UnifiedGameDbContext context, ILogger logger)
    {
        try
        {
            var cleanupCount = 0;

            // 清理没有对应玩家的ActionTargets
            cleanupCount += await context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ActionTargets 
                WHERE PlayerId NOT IN (SELECT Id FROM Players)
            ");

            // 清理没有对应玩家的OfflineData
            cleanupCount += await context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM OfflineData 
                WHERE PlayerId NOT IN (SELECT Id FROM Players)
            ");

            // 清理已完成超过30天的ActionTargets
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd HH:mm:ss");
            cleanupCount += await context.Database.ExecuteSqlRawAsync($@"
                DELETE FROM ActionTargets 
                WHERE IsCompleted = 1 AND CompletedAt < '{thirtyDaysAgo}'
            ");

            // 清理已同步超过7天的OfflineData
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd HH:mm:ss");
            cleanupCount += await context.Database.ExecuteSqlRawAsync($@"
                DELETE FROM OfflineData 
                WHERE IsSynced = 1 AND SyncedAt < '{sevenDaysAgo}'
            ");

            if (cleanupCount > 0)
            {
                logger.LogInformation("Cleaned up {Count} orphaned records", cleanupCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to clean orphaned data");
        }
    }

    /// <summary>
    /// 初始化数据库，确保数据库和表存在
    /// </summary>
    public static async Task InitializeDatabaseAsync(GameDbContext context, ILogger logger)
    {
        try
        {
            // 确保数据库文件存在
            await context.Database.EnsureCreatedAsync();
            
            logger.LogInformation("Database initialized successfully");
            
            // 检查数据库健康状态
            var canConnect = await context.Database.CanConnectAsync();
            if (canConnect)
            {
                logger.LogInformation("Database connection test successful");
                
                // 记录表的存在状态
                var tableExistenceInfo = await GetTableExistenceInfoAsync(context);
                foreach (var info in tableExistenceInfo)
                {
                    logger.LogDebug("Table {TableName}: {Status}", info.Key, info.Value ? "EXISTS" : "MISSING");
                }
            }
            else
            {
                logger.LogWarning("Database connection test failed");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    /// <summary>
    /// 导出数据库数据到JSON（备份用途）
    /// </summary>
    public static async Task<string> ExportDatabaseToJsonAsync(
        UnifiedGameDbContext context, 
        ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting database export to JSON");

            var exportData = new
            {
                ExportedAt = DateTime.UtcNow,
                Players = await context.Players.ToListAsync(),
                Teams = await context.Teams.ToListAsync(),
                ActionTargets = await context.ActionTargets.ToListAsync(),
                BattleRecords = await context.BattleRecords.ToListAsync(),
                OfflineData = await context.OfflineData.ToListAsync()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonData = JsonSerializer.Serialize(exportData, options);
            
            logger.LogInformation("Database export completed. Size: {Size} characters", jsonData.Length);
            return jsonData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export database to JSON");
            throw;
        }
    }

    /// <summary>
    /// 从JSON导入数据库数据（恢复用途）
    /// </summary>
    public static async Task<bool> ImportDatabaseFromJsonAsync(
        UnifiedGameDbContext context, 
        string jsonData, 
        ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting database import from JSON");

            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                using var document = JsonDocument.Parse(jsonData);
                var root = document.RootElement;

                // 清空现有数据
                await context.Database.ExecuteSqlRawAsync("DELETE FROM OfflineData");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM BattleRecords");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM ActionTargets");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM Teams");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM Players");

                // 导入数据
                if (root.TryGetProperty("players", out var playersElement))
                {
                    var players = JsonSerializer.Deserialize<List<BlazorWebGame.Shared.Models.PlayerEntity>>(
                        playersElement.GetRawText(), options);
                    if (players?.Any() == true)
                    {
                        context.Players.AddRange(players);
                    }
                }

                if (root.TryGetProperty("teams", out var teamsElement))
                {
                    var teams = JsonSerializer.Deserialize<List<BlazorWebGame.Shared.Models.TeamEntity>>(
                        teamsElement.GetRawText(), options);
                    if (teams?.Any() == true)
                    {
                        context.Teams.AddRange(teams);
                    }
                }

                if (root.TryGetProperty("actionTargets", out var actionTargetsElement))
                {
                    var actionTargets = JsonSerializer.Deserialize<List<BlazorWebGame.Shared.Models.ActionTargetEntity>>(
                        actionTargetsElement.GetRawText(), options);
                    if (actionTargets?.Any() == true)
                    {
                        context.ActionTargets.AddRange(actionTargets);
                    }
                }

                if (root.TryGetProperty("battleRecords", out var battleRecordsElement))
                {
                    var battleRecords = JsonSerializer.Deserialize<List<BlazorWebGame.Shared.Models.BattleRecordEntity>>(
                        battleRecordsElement.GetRawText(), options);
                    if (battleRecords?.Any() == true)
                    {
                        context.BattleRecords.AddRange(battleRecords);
                    }
                }

                if (root.TryGetProperty("offlineData", out var offlineDataElement))
                {
                    var offlineData = JsonSerializer.Deserialize<List<BlazorWebGame.Shared.Models.OfflineDataEntity>>(
                        offlineDataElement.GetRawText(), options);
                    if (offlineData?.Any() == true)
                    {
                        context.OfflineData.AddRange(offlineData);
                    }
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                logger.LogInformation("Database import completed successfully");
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import database from JSON");
            return false;
        }
    }

    /// <summary>
    /// 验证数据库完整性
    /// </summary>
    public static async Task<Dictionary<string, object>> ValidateDatabaseIntegrityAsync(
        UnifiedGameDbContext context, 
        ILogger logger)
    {
        var results = new Dictionary<string, object>();

        try
        {
            logger.LogInformation("Starting database integrity validation");

            // 基本统计
            results["PlayerCount"] = await context.Players.CountAsync();
            results["TeamCount"] = await context.Teams.CountAsync();
            results["ActionTargetCount"] = await context.ActionTargets.CountAsync();
            results["BattleRecordCount"] = await context.BattleRecords.CountAsync();
            results["OfflineDataCount"] = await context.OfflineData.CountAsync();

            // 数据完整性检查
            var orphanedActionTargets = await context.ActionTargets
                .Where(at => !context.Players.Any(p => p.Id == at.PlayerId))
                .CountAsync();
            results["OrphanedActionTargets"] = orphanedActionTargets;

            var orphanedOfflineData = await context.OfflineData
                .Where(od => !context.Players.Any(p => p.Id == od.PlayerId))
                .CountAsync();
            results["OrphanedOfflineData"] = orphanedOfflineData;

            // JSON字段验证
            var invalidJsonFields = 0;
            var players = await context.Players.ToListAsync();
            foreach (var player in players)
            {
                if (!IsValidJson(player.AttributesJson) ||
                    !IsValidJson(player.InventoryJson) ||
                    !IsValidJson(player.SkillsJson) ||
                    !IsValidJson(player.EquipmentJson))
                {
                    invalidJsonFields++;
                }
            }
            results["InvalidJsonFieldCount"] = invalidJsonFields;

            // 数据一致性检查
            var inconsistentTeams = await context.Teams
                .Where(t => t.Status == "Active" && t.CaptainId != null)
                .Where(t => !context.Players.Any(p => p.Id == t.CaptainId))
                .CountAsync();
            results["InconsistentTeams"] = inconsistentTeams;

            results["ValidationTimestamp"] = DateTime.UtcNow;
            results["ValidationStatus"] = "Completed";

            logger.LogInformation("Database integrity validation completed: {@Results}", results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database integrity validation failed");
            results["ValidationStatus"] = "Failed";
            results["ValidationError"] = ex.Message;
        }

        return results;
    }

    private static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取数据库表存在信息
    /// </summary>
    private static async Task<Dictionary<string, bool>> GetTableExistenceInfoAsync(GameDbContext context)
    {
        var tableInfo = new Dictionary<string, bool>();
        
        try
        {
            // 检查各个表是否存在
            tableInfo["Players"] = await TableExistsAsync(context, "Players");
            tableInfo["Teams"] = await TableExistsAsync(context, "Teams");
            tableInfo["ActionTargets"] = await TableExistsAsync(context, "ActionTargets");
            tableInfo["BattleRecords"] = await TableExistsAsync(context, "BattleRecords");
            tableInfo["OfflineData"] = await TableExistsAsync(context, "OfflineData");
        }
        catch (Exception ex)
        {
            // 如果检查表存在性失败，记录错误但不影响整体流程
            Console.WriteLine($"Error checking table existence: {ex.Message}");
        }
        
        return tableInfo;
    }

    /// <summary>
    /// 检查指定表是否存在
    /// </summary>
    private static async Task<bool> TableExistsAsync(GameDbContext context, string tableName)
    {
        try
        {
            var sql = "SELECT name FROM sqlite_master WHERE type='table' AND name = @tableName";
            var result = await context.Database.SqlQueryRaw<string>(sql, tableName).ToListAsync();
            return result.Any();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取数据库统计信息
    /// </summary>
    public static async Task<Dictionary<string, object>> GetDatabaseStatsAsync(GameDbContext context, ILogger logger)
    {
        var stats = new Dictionary<string, object>();
        
        try
        {
            stats["DatabasePath"] = context.Database.GetConnectionString();
            stats["PlayersCount"] = await context.Players.CountAsync();
            stats["TeamsCount"] = await context.Teams.CountAsync();
            stats["ActionTargetsCount"] = await context.ActionTargets.CountAsync();
            stats["BattleRecordsCount"] = await context.BattleRecords.CountAsync();
            stats["OfflineDataCount"] = await context.OfflineData.CountAsync();
            stats["LastChecked"] = DateTime.UtcNow;
            
            logger.LogInformation("Database stats collected successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to collect database stats");
            stats["Error"] = ex.Message;
        }
        
        return stats;
    }

    /// <summary>
    /// 备份数据库（简单的文件复制备份）
    /// </summary>
    public static async Task<string> BackupDatabaseAsync(GameDbContext context, ILogger logger)
    {
        try
        {
            var connectionString = context.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string is null or empty");
            }

            // 解析连接字符串获取数据库文件路径
            var dbPath = ExtractDbPathFromConnectionString(connectionString);
            if (string.IsNullOrEmpty(dbPath))
            {
                throw new InvalidOperationException("Could not extract database path from connection string");
            }

            // 生成备份文件名
            var backupFileName = $"gamedata_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db";
            var backupPath = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", backupFileName);

            // 复制数据库文件
            if (File.Exists(dbPath))
            {
                await Task.Run(() => File.Copy(dbPath, backupPath, true));
                logger.LogInformation("Database backed up to: {BackupPath}", backupPath);
                return backupPath;
            }
            else
            {
                throw new FileNotFoundException($"Database file not found: {dbPath}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to backup database");
            throw;
        }
    }

    /// <summary>
    /// 从连接字符串中提取数据库文件路径
    /// </summary>
    private static string? ExtractDbPathFromConnectionString(string connectionString)
    {
        // 简单的连接字符串解析，查找 Data Source 参数
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2 && 
                keyValue[0].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase))
            {
                return keyValue[1].Trim();
            }
        }
        return null;
    }

    /// <summary>
    /// 清理旧的备份文件（保留最近的N个备份）
    /// </summary>
    public static async Task CleanupOldBackupsAsync(string backupDirectory, int keepCount = 5, ILogger? logger = null)
    {
        try
        {
            if (!Directory.Exists(backupDirectory))
            {
                return;
            }

            var backupFiles = Directory.GetFiles(backupDirectory, "gamedata_backup_*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Skip(keepCount)
                .ToList();

            foreach (var file in backupFiles)
            {
                await Task.Run(() => file.Delete());
                logger?.LogInformation("Deleted old backup: {BackupFile}", file.Name);
            }

            if (backupFiles.Any())
            {
                logger?.LogInformation("Cleaned up {Count} old backup files", backupFiles.Count);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to cleanup old backups");
        }
    }
}