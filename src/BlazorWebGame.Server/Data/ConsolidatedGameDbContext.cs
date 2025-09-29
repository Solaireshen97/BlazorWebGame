using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BlazorWebGame.Shared.Models;
using System.Text.Json;

namespace BlazorWebGame.Server.Data;

/// <summary>
/// 统一优化的游戏数据库上下文 - 集成所有最佳实践
/// 整合了之前多个DbContext的功能，提供高性能和完整功能
/// </summary>
public class ConsolidatedGameDbContext : DbContext
{
    private readonly ILogger<ConsolidatedGameDbContext>? _logger;

    public ConsolidatedGameDbContext(DbContextOptions<ConsolidatedGameDbContext> options) : base(options)
    {
    }

    public ConsolidatedGameDbContext(DbContextOptions<ConsolidatedGameDbContext> options, ILogger<ConsolidatedGameDbContext> logger) 
        : base(options)
    {
        _logger = logger;
    }

    // 数据库表集合
    public DbSet<PlayerEntity> Players { get; set; } = null!;
    public DbSet<TeamEntity> Teams { get; set; } = null!;
    public DbSet<ActionTargetEntity> ActionTargets { get; set; } = null!;
    public DbSet<BattleRecordEntity> BattleRecords { get; set; } = null!;
    public DbSet<OfflineDataEntity> OfflineData { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // SQLite性能优化配置
        if (optionsBuilder.IsConfigured && Database.IsSqlite())
        {
            // 应用SQLite优化pragma设置
            ApplySqliteOptimizations();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置玩家实体
        ConfigurePlayerEntity(modelBuilder);
        
        // 配置队伍实体
        ConfigureTeamEntity(modelBuilder);
        
        // 配置动作目标实体
        ConfigureActionTargetEntity(modelBuilder);
        
        // 配置战斗记录实体
        ConfigureBattleRecordEntity(modelBuilder);
        
        // 配置离线数据实体
        ConfigureOfflineDataEntity(modelBuilder);

        // 配置性能优化索引
        ConfigurePerformanceIndexes(modelBuilder);

        _logger?.LogDebug("Consolidated database model configuration completed");
    }

    private void ConfigurePlayerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            // 主键和基本属性
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SelectedBattleProfession).HasMaxLength(50);
            entity.Property(e => e.CurrentAction).HasMaxLength(50);
            entity.Property(e => e.CurrentActionTargetId).HasMaxLength(50);
            
            // JSON 字段配置
            entity.Property(e => e.AttributesJson)
                .HasColumnType("TEXT")
                .HasDefaultValue("{}");
            entity.Property(e => e.InventoryJson)
                .HasColumnType("TEXT")
                .HasDefaultValue("[]");
            entity.Property(e => e.SkillsJson)
                .HasColumnType("TEXT")
                .HasDefaultValue("[]");
            entity.Property(e => e.EquipmentJson)
                .HasColumnType("TEXT")
                .HasDefaultValue("{}");
            
            // 性能优化索引
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsOnline);
            entity.HasIndex(e => e.LastActiveAt);
            entity.HasIndex(e => e.PartyId);
            entity.HasIndex(e => new { e.IsOnline, e.LastActiveAt });
            entity.HasIndex(e => new { e.PartyId, e.IsOnline });
        });
    }

    private void ConfigureTeamEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeamEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CaptainId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Active");
            entity.Property(e => e.CurrentBattleId).HasMaxLength(50);
            
            // JSON 字段配置
            entity.Property(e => e.MemberIdsJson)
                .HasColumnType("TEXT")
                .HasDefaultValue("[]");
            
            // 索引
            entity.HasIndex(e => e.CaptainId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
        });
    }

    private void ConfigureActionTargetEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActionTargetEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.PlayerId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TargetType).HasMaxLength(50);
            entity.Property(e => e.TargetId).HasMaxLength(50);
            entity.Property(e => e.TargetName).HasMaxLength(200);
            entity.Property(e => e.ActionType).HasMaxLength(50);
            
            // JSON 字段配置
            entity.Property(e => e.ProgressDataJson)
                .HasColumnType("TEXT")
                .HasDefaultValue("{}");
            
            // 复合索引优化查询性能
            entity.HasIndex(e => e.PlayerId);
            entity.HasIndex(e => e.IsCompleted);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => new { e.PlayerId, e.IsCompleted });
            entity.HasIndex(e => new { e.PlayerId, e.StartedAt });
        });
    }

    private void ConfigureBattleRecordEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BattleRecordEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.BattleId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.BattleType).HasMaxLength(50).HasDefaultValue("Normal");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("InProgress");
            entity.Property(e => e.DungeonId).HasMaxLength(50);
            
            // JSON 字段配置
            entity.Property(e => e.ParticipantsJson)
                .HasColumnType("TEXT")
                .HasDefaultValue("[]");
            entity.Property(e => e.EnemiesJson)
                .HasColumnType("TEXT")
                .HasDefaultValue("[]");
            entity.Property(e => e.ActionsJson)
                .HasColumnType("TEXT")
                .HasDefaultValue("[]");
            entity.Property(e => e.ResultsJson)
                .HasColumnType("TEXT")
                .HasDefaultValue("{}");
            
            // 索引
            entity.HasIndex(e => e.BattleId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.PartyId);
            entity.HasIndex(e => e.BattleType);
            entity.HasIndex(e => new { e.Status, e.StartedAt });
            entity.HasIndex(e => new { e.PartyId, e.Status });
        });
    }

    private void ConfigureOfflineDataEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OfflineDataEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.PlayerId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DataType).HasMaxLength(50);
            
            // JSON 字段配置
            entity.Property(e => e.DataJson)
                .HasColumnType("TEXT")
                .HasDefaultValue("{}");
            
            // 索引
            entity.HasIndex(e => e.PlayerId);
            entity.HasIndex(e => e.IsSynced);
            entity.HasIndex(e => e.DataType);
            entity.HasIndex(e => new { e.PlayerId, e.IsSynced });
            entity.HasIndex(e => new { e.PlayerId, e.DataType });
        });
    }

    private void ConfigurePerformanceIndexes(ModelBuilder modelBuilder)
    {
        // 添加额外的性能优化索引
        // 这些索引基于实际使用模式来优化查询性能
        
        // 玩家查询优化
        modelBuilder.Entity<PlayerEntity>()
            .HasIndex(p => new { p.Level, p.IsOnline })
            .HasDatabaseName("IX_Players_Level_IsOnline");
            
        // 战斗记录查询优化  
        modelBuilder.Entity<BattleRecordEntity>()
            .HasIndex(b => new { b.StartedAt, b.Status, b.BattleType })
            .HasDatabaseName("IX_BattleRecords_StartedAt_Status_Type");
            
        // 离线数据查询优化
        modelBuilder.Entity<OfflineDataEntity>()
            .HasIndex(o => new { o.CreatedAt, o.IsSynced })
            .HasDatabaseName("IX_OfflineData_CreatedAt_IsSynced");
    }

    private void ApplySqliteOptimizations()
    {
        // SQLite性能优化设置将在连接时应用
        // 这些设置在UnifiedDataStorageConfiguration中处理
    }

    /// <summary>
    /// 确保数据库创建和优化
    /// </summary>
    public async Task EnsureDatabaseOptimizedAsync()
    {
        try
        {
            // 确保数据库存在
            await Database.EnsureCreatedAsync();
            
            // 应用SQLite优化设置
            if (Database.IsSqlite())
            {
                await ApplySqlitePerformanceSettingsAsync();
            }
            
            _logger?.LogInformation("Database optimization completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to optimize database");
            throw;
        }
    }

    private async Task ApplySqlitePerformanceSettingsAsync()
    {
        try
        {
            // 启用WAL模式以提高并发性能
            await Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            
            // 设置缓存大小 (10MB)
            await Database.ExecuteSqlRawAsync("PRAGMA cache_size=10000;");
            
            // 启用内存映射
            await Database.ExecuteSqlRawAsync("PRAGMA mmap_size=268435456;"); // 256MB
            
            // 设置同步模式为NORMAL以平衡性能和安全性
            await Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
            
            // 使用内存存储临时表
            await Database.ExecuteSqlRawAsync("PRAGMA temp_store=MEMORY;");
            
            // 启用查询规划器
            await Database.ExecuteSqlRawAsync("PRAGMA optimize;");
            
            _logger?.LogDebug("SQLite performance settings applied successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to apply some SQLite performance settings");
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
            // 基本表统计
            stats["PlayersCount"] = await Players.CountAsync();
            stats["TeamsCount"] = await Teams.CountAsync();
            stats["ActionTargetsCount"] = await ActionTargets.CountAsync();
            stats["BattleRecordsCount"] = await BattleRecords.CountAsync();
            stats["OfflineDataCount"] = await OfflineData.CountAsync();
            
            // 在线玩家统计
            stats["OnlinePlayersCount"] = await Players.CountAsync(p => p.IsOnline);
            
            // 活跃战斗统计
            stats["ActiveBattlesCount"] = await BattleRecords.CountAsync(b => b.Status == "InProgress");
            
            // 未同步离线数据统计
            stats["UnsyncedOfflineDataCount"] = await OfflineData.CountAsync(o => !o.IsSynced);
            
            // 数据库文件大小（仅SQLite）
            if (Database.IsSqlite())
            {
                var pageSizeResult = await Database.SqlQueryRaw<int>("PRAGMA page_size").FirstOrDefaultAsync();
                var pageCountResult = await Database.SqlQueryRaw<int>("PRAGMA page_count").FirstOrDefaultAsync();
                
                stats["DatabaseSizeBytes"] = pageSizeResult * pageCountResult;
                stats["PageSize"] = pageSizeResult;
                stats["PageCount"] = pageCountResult;
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
    public async Task PerformMaintenanceAsync()
    {
        try
        {
            if (Database.IsSqlite())
            {
                // 分析查询计划
                await Database.ExecuteSqlRawAsync("ANALYZE;");
                
                // 优化数据库
                await Database.ExecuteSqlRawAsync("PRAGMA optimize;");
                
                // 清理未使用的页面
                await Database.ExecuteSqlRawAsync("VACUUM;");
                
                _logger?.LogInformation("Database maintenance completed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Database maintenance failed");
            throw;
        }
    }
}