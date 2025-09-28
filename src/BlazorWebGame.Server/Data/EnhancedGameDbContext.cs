using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using BlazorWebGame.Shared.Models;
using System.Text.Json;

namespace BlazorWebGame.Server.Data;

/// <summary>
/// 增强的游戏数据库上下文 - 支持高级功能和优化
/// </summary>
public class EnhancedGameDbContext : DbContext
{
    private readonly ILogger<EnhancedGameDbContext>? _logger;

    public EnhancedGameDbContext(DbContextOptions<EnhancedGameDbContext> options, ILogger<EnhancedGameDbContext>? logger = null)
        : base(options)
    {
        _logger = logger;
    }

    // 数据库表集合
    public DbSet<EnhancedPlayerEntity> Players { get; set; } = null!;
    public DbSet<EnhancedTeamEntity> Teams { get; set; } = null!;
    public DbSet<EnhancedActionTargetEntity> ActionTargets { get; set; } = null!;
    public DbSet<EnhancedBattleRecordEntity> BattleRecords { get; set; } = null!;
    public DbSet<EnhancedOfflineDataEntity> OfflineData { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置全局查询过滤器（软删除）
        ConfigureGlobalFilters(modelBuilder);

        // 配置实体关系
        ConfigureEntityRelationships(modelBuilder);

        // 配置值转换器
        ConfigureValueConverters(modelBuilder);

        // 配置索引和约束
        ConfigureIndexesAndConstraints(modelBuilder);

        // 配置种子数据
        ConfigureSeedData(modelBuilder);

        _logger?.LogDebug("Enhanced database model configuration completed");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (_logger != null)
        {
            optionsBuilder.LogTo(message => _logger.LogDebug(message), LogLevel.Information);
        }

        // SQLite 特定优化
        if (optionsBuilder.Options.Extensions.Any(e => e.GetType().Name.Contains("Sqlite")))
        {
            optionsBuilder.UseSqlite(sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
            });
        }
    }

    private void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        // 全局软删除过滤器
        modelBuilder.Entity<EnhancedPlayerEntity>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EnhancedTeamEntity>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EnhancedActionTargetEntity>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EnhancedBattleRecordEntity>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EnhancedOfflineDataEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    private void ConfigureEntityRelationships(ModelBuilder modelBuilder)
    {
        // 玩家 -> 动作目标 (一对多)
        modelBuilder.Entity<EnhancedActionTargetEntity>()
            .HasOne(at => at.Player)
            .WithMany(p => p.ActionTargets)
            .HasForeignKey(at => at.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // 玩家 -> 离线数据 (一对多)
        modelBuilder.Entity<EnhancedOfflineDataEntity>()
            .HasOne(od => od.Player)
            .WithMany(p => p.OfflineData)
            .HasForeignKey(od => od.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void ConfigureValueConverters(ModelBuilder modelBuilder)
    {
        // DateTime to UTC converter
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        // 应用DateTime转换器到所有DateTime属性
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var dateTimeProperties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType == typeof(DateTime));

            foreach (var property in dateTimeProperties)
            {
                modelBuilder.Entity(entityType.Name).Property(property.Name)
                    .HasConversion(dateTimeConverter);
            }

            var nullableDateTimeProperties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType == typeof(DateTime?));

            foreach (var property in nullableDateTimeProperties)
            {
                modelBuilder.Entity(entityType.Name).Property(property.Name)
                    .HasConversion(nullableDateTimeConverter);
            }
        }
    }

    private void ConfigureIndexesAndConstraints(ModelBuilder modelBuilder)
    {
        // 玩家实体索引
        modelBuilder.Entity<EnhancedPlayerEntity>(entity =>
        {
            entity.HasIndex(e => e.Name).HasDatabaseName("IX_Players_Name");
            entity.HasIndex(e => e.IsOnline).HasDatabaseName("IX_Players_IsOnline");
            entity.HasIndex(e => e.LastActiveAt).HasDatabaseName("IX_Players_LastActiveAt");
            entity.HasIndex(e => e.PartyId).HasDatabaseName("IX_Players_PartyId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_Players_IsDeleted");
            entity.HasIndex(e => new { e.ServerRegion, e.IsOnline }).HasDatabaseName("IX_Players_ServerRegion_IsOnline");
            entity.HasIndex(e => new { e.LastActiveAt, e.IsDeleted }).HasDatabaseName("IX_Players_LastActiveAt_IsDeleted");

            // 唯一约束
            entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("UX_Players_Name");
        });

        // 队伍实体索引
        modelBuilder.Entity<EnhancedTeamEntity>(entity =>
        {
            entity.HasIndex(e => e.CaptainId).HasDatabaseName("IX_Teams_CaptainId");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_Teams_Status");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_Teams_IsDeleted");
            entity.HasIndex(e => e.CurrentBattleId).HasDatabaseName("IX_Teams_CurrentBattleId");
            entity.HasIndex(e => new { e.Status, e.IsPublic }).HasDatabaseName("IX_Teams_Status_IsPublic");

            // 唯一约束
            entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("UX_Teams_Name");
        });

        // 动作目标实体索引
        modelBuilder.Entity<EnhancedActionTargetEntity>(entity =>
        {
            entity.HasIndex(e => new { e.PlayerId, e.IsCompleted }).HasDatabaseName("IX_ActionTargets_PlayerId_IsCompleted");
            entity.HasIndex(e => e.StartedAt).HasDatabaseName("IX_ActionTargets_StartedAt");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_ActionTargets_IsDeleted");
            entity.HasIndex(e => e.TargetType).HasDatabaseName("IX_ActionTargets_TargetType");
            entity.HasIndex(e => e.ActionType).HasDatabaseName("IX_ActionTargets_ActionType");
            entity.HasIndex(e => new { e.PlayerId, e.Priority, e.IsCompleted }).HasDatabaseName("IX_ActionTargets_PlayerId_Priority_IsCompleted");
        });

        // 战斗记录实体索引
        modelBuilder.Entity<EnhancedBattleRecordEntity>(entity =>
        {
            entity.HasIndex(e => e.BattleId).IsUnique().HasDatabaseName("UX_BattleRecords_BattleId");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_BattleRecords_Status");
            entity.HasIndex(e => e.StartedAt).HasDatabaseName("IX_BattleRecords_StartedAt");
            entity.HasIndex(e => e.PartyId).HasDatabaseName("IX_BattleRecords_PartyId");
            entity.HasIndex(e => e.BattleType).HasDatabaseName("IX_BattleRecords_BattleType");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_BattleRecords_IsDeleted");
            entity.HasIndex(e => e.DungeonId).HasDatabaseName("IX_BattleRecords_DungeonId");
            entity.HasIndex(e => new { e.Status, e.BattleType }).HasDatabaseName("IX_BattleRecords_Status_BattleType");
        });

        // 离线数据实体索引
        modelBuilder.Entity<EnhancedOfflineDataEntity>(entity =>
        {
            entity.HasIndex(e => new { e.PlayerId, e.IsSynced }).HasDatabaseName("IX_OfflineData_PlayerId_IsSynced");
            entity.HasIndex(e => e.DataType).HasDatabaseName("IX_OfflineData_DataType");
            entity.HasIndex(e => e.Priority).HasDatabaseName("IX_OfflineData_Priority");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_OfflineData_IsDeleted");
            entity.HasIndex(e => e.NextRetryAt).HasDatabaseName("IX_OfflineData_NextRetryAt");
            entity.HasIndex(e => new { e.IsSynced, e.Priority, e.NextRetryAt }).HasDatabaseName("IX_OfflineData_IsSynced_Priority_NextRetryAt");
        });
    }

    private void ConfigureSeedData(ModelBuilder modelBuilder)
    {
        // 可以在这里添加种子数据
        // 例如：默认游戏配置、管理员账户等
    }

    /// <summary>
    /// 应用待处理的迁移并优化数据库
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger?.LogInformation("Initializing enhanced database...");

            // 应用迁移
            await Database.MigrateAsync();

            // SQLite 特定优化
            if (Database.IsSqlite())
            {
                await OptimizeSqliteAsync();
            }

            _logger?.LogInformation("Enhanced database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize enhanced database");
            throw;
        }
    }

    /// <summary>
    /// 优化SQLite数据库
    /// </summary>
    private async Task OptimizeSqliteAsync()
    {
        try
        {
            // 启用WAL模式以提高并发性能
            await Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
            
            // 设置同步模式为NORMAL以平衡性能和安全性
            await Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;");
            
            // 设置缓存大小 (10MB)
            await Database.ExecuteSqlRawAsync("PRAGMA cache_size = -10240;");
            
            // 启用内存映射I/O (256MB)
            await Database.ExecuteSqlRawAsync("PRAGMA mmap_size = 268435456;");
            
            // 设置页面大小为4KB (新数据库)
            await Database.ExecuteSqlRawAsync("PRAGMA page_size = 4096;");
            
            // 启用外键约束
            await Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
            
            // 设置临时存储为内存
            await Database.ExecuteSqlRawAsync("PRAGMA temp_store = MEMORY;");

            _logger?.LogDebug("SQLite optimization pragmas applied successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to apply SQLite optimizations, continuing anyway");
        }
    }

    /// <summary>
    /// 获取数据库统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetDatabaseStatsAsync()
    {
        var stats = new Dictionary<string, object>();

        try
        {
            stats["TotalPlayers"] = await Players.CountAsync();
            stats["OnlinePlayers"] = await Players.CountAsync(p => p.IsOnline);
            stats["TotalTeams"] = await Teams.CountAsync();
            stats["ActiveTeams"] = await Teams.CountAsync(t => t.Status == "Active");
            stats["TotalActionTargets"] = await ActionTargets.CountAsync();
            stats["ActiveActionTargets"] = await ActionTargets.CountAsync(at => !at.IsCompleted);
            stats["TotalBattleRecords"] = await BattleRecords.CountAsync();
            stats["ActiveBattles"] = await BattleRecords.CountAsync(br => br.Status == "InProgress");
            stats["TotalOfflineData"] = await OfflineData.CountAsync();
            stats["UnsyncedOfflineData"] = await OfflineData.CountAsync(od => !od.IsSynced);
            
            // 计算软删除的记录数量
            stats["SoftDeletedPlayers"] = await Players.IgnoreQueryFilters().CountAsync(p => p.IsDeleted);
            stats["SoftDeletedTeams"] = await Teams.IgnoreQueryFilters().CountAsync(t => t.IsDeleted);
            stats["SoftDeletedActionTargets"] = await ActionTargets.IgnoreQueryFilters().CountAsync(at => at.IsDeleted);
            stats["SoftDeletedBattleRecords"] = await BattleRecords.IgnoreQueryFilters().CountAsync(br => br.IsDeleted);
            stats["SoftDeletedOfflineData"] = await OfflineData.IgnoreQueryFilters().CountAsync(od => od.IsDeleted);

            if (Database.IsSqlite())
            {
                // SQLite 特定统计信息
                var dbSizeResult = await Database.SqlQueryRaw<long>("SELECT page_count * page_size AS size FROM pragma_page_count(), pragma_page_size();").FirstOrDefaultAsync();
                stats["DatabaseSizeBytes"] = dbSizeResult;

                var walModeResult = await Database.SqlQueryRaw<string>("PRAGMA journal_mode;").FirstOrDefaultAsync();
                stats["JournalMode"] = walModeResult ?? "unknown";

                var cacheSize = await Database.SqlQueryRaw<long>("PRAGMA cache_size;").FirstOrDefaultAsync();
                stats["CacheSizePages"] = cacheSize;
            }

            stats["LastUpdated"] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get database statistics");
            stats["Error"] = ex.Message;
        }

        return stats;
    }

    /// <summary>
    /// 执行数据库维护任务
    /// </summary>
    public async Task<Dictionary<string, object>> PerformMaintenanceAsync()
    {
        var results = new Dictionary<string, object>();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger?.LogInformation("Starting database maintenance tasks");

            // 1. 清理过期的软删除记录（超过30天）
            var cleanupThreshold = DateTime.UtcNow.AddDays(-30);
            var deletedPlayers = await Players.IgnoreQueryFilters()
                .Where(p => p.IsDeleted && p.DeletedAt < cleanupThreshold)
                .ToListAsync();
            
            Database.RemoveRange(deletedPlayers);
            var playersDeleted = deletedPlayers.Count;

            var deletedTeams = await Teams.IgnoreQueryFilters()
                .Where(t => t.IsDeleted && t.DeletedAt < cleanupThreshold)
                .ToListAsync();
            
            Database.RemoveRange(deletedTeams);
            var teamsDeleted = deletedTeams.Count;

            var deletedActionTargets = await ActionTargets.IgnoreQueryFilters()
                .Where(at => at.IsDeleted && at.DeletedAt < cleanupThreshold)
                .ToListAsync();
            
            Database.RemoveRange(deletedActionTargets);
            var actionTargetsDeleted = deletedActionTargets.Count;

            var deletedBattleRecords = await BattleRecords.IgnoreQueryFilters()
                .Where(br => br.IsDeleted && br.DeletedAt < cleanupThreshold)
                .ToListAsync();
            
            Database.RemoveRange(deletedBattleRecords);
            var battleRecordsDeleted = deletedBattleRecords.Count;

            var deletedOfflineData = await OfflineData.IgnoreQueryFilters()
                .Where(od => od.IsDeleted && od.DeletedAt < cleanupThreshold)
                .ToListAsync();
            
            Database.RemoveRange(deletedOfflineData);
            var offlineDataDeleted = deletedOfflineData.Count;

            // 2. 清理已同步的旧离线数据（超过7天）
            var syncedDataThreshold = DateTime.UtcNow.AddDays(-7);
            var oldSyncedData = await OfflineData
                .Where(od => od.IsSynced && od.SyncedAt < syncedDataThreshold)
                .ToListAsync();
            
            OfflineData.RemoveRange(oldSyncedData);
            var syncedDataDeleted = oldSyncedData.Count;

            // 3. 清理完成的旧动作目标（超过7天）
            var completedActionsThreshold = DateTime.UtcNow.AddDays(-7);
            var oldCompletedActions = await ActionTargets
                .Where(at => at.IsCompleted && at.CompletedAt < completedActionsThreshold)
                .ToListAsync();
            
            ActionTargets.RemoveRange(oldCompletedActions);
            var completedActionsDeleted = oldCompletedActions.Count;

            // 保存所有更改
            var changesSaved = await SaveChangesAsync();

            // 4. SQLite 特定维护
            if (Database.IsSqlite())
            {
                await Database.ExecuteSqlRawAsync("VACUUM;");
                await Database.ExecuteSqlRawAsync("ANALYZE;");
                results["VacuumCompleted"] = true;
                results["AnalyzeCompleted"] = true;
            }

            var elapsedTime = DateTime.UtcNow - startTime;

            results["Success"] = true;
            results["ElapsedTime"] = elapsedTime.TotalSeconds;
            results["ChangesSaved"] = changesSaved;
            results["DeletedRecords"] = new Dictionary<string, int>
            {
                ["Players"] = playersDeleted,
                ["Teams"] = teamsDeleted,
                ["ActionTargets"] = actionTargetsDeleted + completedActionsDeleted,
                ["BattleRecords"] = battleRecordsDeleted,
                ["OfflineData"] = offlineDataDeleted + syncedDataDeleted
            };

            _logger?.LogInformation("Database maintenance completed successfully in {ElapsedTime}s. Deleted {TotalDeleted} records.", 
                elapsedTime.TotalSeconds,
                playersDeleted + teamsDeleted + actionTargetsDeleted + battleRecordsDeleted + offlineDataDeleted + syncedDataDeleted + completedActionsDeleted);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Database maintenance failed");
            results["Success"] = false;
            results["Error"] = ex.Message;
            var elapsedTime = DateTime.UtcNow - startTime;
            results["ElapsedTime"] = elapsedTime.TotalSeconds;
        }

        return results;
    }

    /// <summary>
    /// 重置自增计数器（如果需要）
    /// </summary>
    public async Task ResetAutoIncrementAsync()
    {
        if (Database.IsSqlite())
        {
            await Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence;");
            _logger?.LogInformation("SQLite auto-increment sequences reset");
        }
    }

    /// <summary>
    /// 创建数据库备份（SQLite）
    /// </summary>
    public async Task<string> CreateBackupAsync(string backupPath)
    {
        if (!Database.IsSqlite())
            throw new NotSupportedException("Database backup is only supported for SQLite");

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"gamedata_backup_{timestamp}.db";
        var fullBackupPath = Path.Combine(backupPath, backupFileName);

        // 确保备份目录存在
        Directory.CreateDirectory(backupPath);

        // 执行备份
        await Database.ExecuteSqlRawAsync($"VACUUM INTO '{fullBackupPath}';");

        _logger?.LogInformation("Database backup created: {BackupPath}", fullBackupPath);
        return fullBackupPath;
    }
}