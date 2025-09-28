using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Data;

/// <summary>
/// 数据库迁移助手 - 用于管理SQLite数据库的创建和更新
/// </summary>
public static class DatabaseMigrationHelper
{
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