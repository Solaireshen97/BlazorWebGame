using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Server.Data;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 数据库连接服务 - 管理连接池和连接健康状态
/// </summary>
public class DatabaseConnectionService : IDisposable
{
    private readonly IDbContextFactory<GameDbContext> _contextFactory;
    private readonly ILogger<DatabaseConnectionService> _logger;
    
    // 连接池统计
    private readonly ConcurrentDictionary<string, int> _connectionStats = new();
    private readonly Timer _healthCheckTimer;
    private volatile bool _isHealthy = true;
    
    public bool IsHealthy => _isHealthy;
    
    public DatabaseConnectionService(
        IDbContextFactory<GameDbContext> contextFactory,
        ILogger<DatabaseConnectionService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        
        // 每30秒检查数据库健康状态
        _healthCheckTimer = new Timer(CheckDatabaseHealth, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        
        _logger.LogInformation("DatabaseConnectionService initialized");
    }

    /// <summary>
    /// 创建数据库上下文
    /// </summary>
    public async Task<GameDbContext> CreateContextAsync()
    {
        try
        {
            var context = await _contextFactory.CreateDbContextAsync();
            IncrementStat("contexts_created");
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database context");
            IncrementStat("context_creation_errors");
            throw;
        }
    }

    /// <summary>
    /// 执行数据库操作，自动管理上下文生命周期
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<GameDbContext, Task<T>> operation)
    {
        using var context = await CreateContextAsync();
        try
        {
            var result = await operation(context);
            IncrementStat("operations_completed");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database operation failed");
            IncrementStat("operation_errors");
            throw;
        }
    }

    /// <summary>
    /// 执行数据库操作（无返回值）
    /// </summary>
    public async Task ExecuteAsync(Func<GameDbContext, Task> operation)
    {
        using var context = await CreateContextAsync();
        try
        {
            await operation(context);
            IncrementStat("operations_completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database operation failed");
            IncrementStat("operation_errors");
            throw;
        }
    }

    /// <summary>
    /// 执行事务操作
    /// </summary>
    public async Task<T> ExecuteTransactionAsync<T>(Func<GameDbContext, Task<T>> operation)
    {
        using var context = await CreateContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();
        
        try
        {
            var result = await operation(context);
            await transaction.CommitAsync();
            IncrementStat("transactions_completed");
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Database transaction failed and was rolled back");
            IncrementStat("transaction_errors");
            throw;
        }
    }

    /// <summary>
    /// 执行事务操作（无返回值）
    /// </summary>
    public async Task ExecuteTransactionAsync(Func<GameDbContext, Task> operation)
    {
        using var context = await CreateContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();
        
        try
        {
            await operation(context);
            await transaction.CommitAsync();
            IncrementStat("transactions_completed");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Database transaction failed and was rolled back");
            IncrementStat("transaction_errors");
            throw;
        }
    }

    /// <summary>
    /// 检查数据库健康状态
    /// </summary>
    private async void CheckDatabaseHealth(object? state)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var canConnect = await context.Database.CanConnectAsync();
            
            if (canConnect != _isHealthy)
            {
                _isHealthy = canConnect;
                _logger.LogWarning("Database health status changed: {IsHealthy}", _isHealthy);
            }
            
            if (canConnect)
            {
                IncrementStat("health_checks_passed");
            }
            else
            {
                IncrementStat("health_checks_failed");
            }
        }
        catch (Exception ex)
        {
            _isHealthy = false;
            _logger.LogError(ex, "Database health check failed");
            IncrementStat("health_check_errors");
        }
    }

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    public Dictionary<string, object> GetConnectionStats()
    {
        return new Dictionary<string, object>
        {
            ["IsHealthy"] = _isHealthy,
            ["Statistics"] = _connectionStats.ToDictionary(kv => kv.Key, kv => kv.Value),
            ["LastHealthCheck"] = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 优化数据库性能
    /// </summary>
    public async Task OptimizeDatabaseAsync()
    {
        try
        {
            using var context = await CreateContextAsync();
            
            // SQLite特定的优化命令
            await context.Database.ExecuteSqlRawAsync("PRAGMA optimize");
            await context.Database.ExecuteSqlRawAsync("PRAGMA analysis_limit=1000");
            await context.Database.ExecuteSqlRawAsync("ANALYZE");
            
            _logger.LogInformation("Database optimization completed");
            IncrementStat("optimizations_completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database optimization failed");
            IncrementStat("optimization_errors");
        }
    }

    /// <summary>
    /// 清理数据库（删除旧数据、重建索引等）
    /// </summary>
    public async Task CleanupDatabaseAsync(TimeSpan retentionPeriod)
    {
        try
        {
            using var context = await CreateContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();
            
            var cutoffTime = DateTime.UtcNow - retentionPeriod;
            
            // 清理旧的战斗记录
            var oldBattleRecords = await context.BattleRecords
                .Where(br => br.Status != "InProgress" && br.EndedAt < cutoffTime)
                .CountAsync();
            
            if (oldBattleRecords > 0)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM BattleRecords WHERE Status != 'InProgress' AND EndedAt < @cutoff",
                    new Microsoft.Data.Sqlite.SqliteParameter("@cutoff", cutoffTime));
            }

            // 清理旧的已完成动作目标
            var oldActionTargets = await context.ActionTargets
                .Where(at => at.IsCompleted && at.CompletedAt < cutoffTime)
                .CountAsync();
            
            if (oldActionTargets > 0)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM ActionTargets WHERE IsCompleted = 1 AND CompletedAt < @cutoff",
                    new Microsoft.Data.Sqlite.SqliteParameter("@cutoff", cutoffTime));
            }

            // 清理已同步的离线数据
            var oldOfflineData = await context.OfflineData
                .Where(od => od.IsSynced && od.SyncedAt < cutoffTime)
                .CountAsync();
            
            if (oldOfflineData > 0)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM OfflineData WHERE IsSynced = 1 AND SyncedAt < @cutoff",
                    new Microsoft.Data.Sqlite.SqliteParameter("@cutoff", cutoffTime));
            }

            // 压缩数据库
            await context.Database.ExecuteSqlRawAsync("VACUUM");
            
            await transaction.CommitAsync();
            
            var totalCleaned = oldBattleRecords + oldActionTargets + oldOfflineData;
            _logger.LogInformation("Database cleanup completed. Cleaned {TotalRecords} old records", totalCleaned);
            IncrementStat("cleanups_completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database cleanup failed");
            IncrementStat("cleanup_errors");
            throw;
        }
    }

    /// <summary>
    /// 备份数据库
    /// </summary>
    public async Task<string> BackupDatabaseAsync(string backupPath)
    {
        try
        {
            var backupFileName = $"gamedata_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db";
            var fullBackupPath = Path.Combine(backupPath, backupFileName);
            
            // 确保备份目录存在
            Directory.CreateDirectory(backupPath);
            
            using var context = await CreateContextAsync();
            
            // 对于SQLite，我们可以使用VACUUM INTO命令来创建备份
            await context.Database.ExecuteSqlRawAsync(
                $"VACUUM INTO '{fullBackupPath}'");
            
            _logger.LogInformation("Database backup created: {BackupPath}", fullBackupPath);
            IncrementStat("backups_completed");
            
            return fullBackupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database backup failed");
            IncrementStat("backup_errors");
            throw;
        }
    }

    /// <summary>
    /// 获取数据库大小信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetDatabaseSizeInfoAsync()
    {
        try
        {
            using var context = await CreateContextAsync();
            
            var sizeInfo = new Dictionary<string, object>();
            
            // 获取总页数和页大小
            var pageCountResult = await context.Database.SqlQueryRaw<int>("PRAGMA page_count").FirstOrDefaultAsync();
            var pageSizeResult = await context.Database.SqlQueryRaw<int>("PRAGMA page_size").FirstOrDefaultAsync();
            
            var totalPages = pageCountResult;
            var pageSize = pageSizeResult;
            var totalSize = totalPages * pageSize;
            
            sizeInfo["TotalPages"] = totalPages;
            sizeInfo["PageSize"] = pageSize;
            sizeInfo["TotalSizeBytes"] = totalSize;
            sizeInfo["TotalSizeMB"] = Math.Round(totalSize / 1024.0 / 1024.0, 2);
            
            // 获取各表的记录数
            sizeInfo["TableCounts"] = new Dictionary<string, object>
            {
                ["Players"] = await context.Players.CountAsync(),
                ["Teams"] = await context.Teams.CountAsync(),
                ["ActionTargets"] = await context.ActionTargets.CountAsync(),
                ["BattleRecords"] = await context.BattleRecords.CountAsync(),
                ["OfflineData"] = await context.OfflineData.CountAsync()
            };
            
            return sizeInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database size info");
            throw;
        }
    }

    private void IncrementStat(string key)
    {
        _connectionStats.AddOrUpdate(key, 1, (k, v) => v + 1);
    }

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}