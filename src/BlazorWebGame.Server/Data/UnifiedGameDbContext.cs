using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Shared.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Data;

/// <summary>
/// 统一的游戏数据库上下文 - 集成最佳实践和性能优化
/// </summary>
public class UnifiedGameDbContext : DbContext
{
    private readonly ILogger<UnifiedGameDbContext>? _logger;

    public UnifiedGameDbContext(DbContextOptions<UnifiedGameDbContext> options) : base(options)
    {
    }

    public UnifiedGameDbContext(DbContextOptions<UnifiedGameDbContext> options, ILogger<UnifiedGameDbContext> logger) 
        : base(options)
    {
        _logger = logger;
    }

    // 数据库表集合
    public DbSet<PlayerEntity> Players { get; set; }
    public DbSet<TeamEntity> Teams { get; set; }
    public DbSet<ActionTargetEntity> ActionTargets { get; set; }
    public DbSet<BattleRecordEntity> BattleRecords { get; set; }
    public DbSet<OfflineDataEntity> OfflineData { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // SQLite性能优化配置
        if (optionsBuilder.IsConfigured && Database.IsSqlite())
        {
            optionsBuilder.UseSqlite(options => 
            {
                options.CommandTimeout(30); // 30秒超时
            });
            
            // 开发环境启用详细日志
            if (_logger != null)
            {
                optionsBuilder.LogTo(message => _logger.LogDebug(message), LogLevel.Debug);
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurePlayerEntity(modelBuilder);
        ConfigureTeamEntity(modelBuilder);
        ConfigureActionTargetEntity(modelBuilder);
        ConfigureBattleRecordEntity(modelBuilder);
        ConfigureOfflineDataEntity(modelBuilder);
        
        // 创建性能优化索引
        CreatePerformanceIndexes(modelBuilder);
        
        // 配置级联删除和关系
        ConfigureEntityRelationships(modelBuilder);
    }

    private void ConfigurePlayerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SelectedBattleProfession).HasMaxLength(50).HasDefaultValue("Warrior");
            entity.Property(e => e.CurrentAction).HasMaxLength(50).HasDefaultValue("Idle");
            entity.Property(e => e.CurrentActionTargetId).HasMaxLength(50);
            
            // 约束和默认值
            entity.Property(e => e.Level).HasDefaultValue(1);
            entity.Property(e => e.Experience).HasDefaultValue(0);
            entity.Property(e => e.Health).HasDefaultValue(100);
            entity.Property(e => e.MaxHealth).HasDefaultValue(100);
            entity.Property(e => e.Gold).HasDefaultValue(0);
            entity.Property(e => e.IsOnline).HasDefaultValue(true);
            
            // JSON 字段配置
            entity.Property(e => e.AttributesJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.InventoryJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.SkillsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.EquipmentJson).HasColumnType("TEXT").HasDefaultValue("{}");
            
            // 基础索引
            entity.HasIndex(e => e.Name).HasDatabaseName("IX_Players_Name");
            entity.HasIndex(e => e.IsOnline).HasDatabaseName("IX_Players_IsOnline");
            entity.HasIndex(e => e.LastActiveAt).HasDatabaseName("IX_Players_LastActiveAt");
            entity.HasIndex(e => e.PartyId).HasDatabaseName("IX_Players_PartyId");
            entity.HasIndex(e => e.Level).HasDatabaseName("IX_Players_Level");
            entity.HasIndex(e => e.SelectedBattleProfession).HasDatabaseName("IX_Players_Profession");
            
            // 复合索引
            entity.HasIndex(e => new { e.IsOnline, e.LastActiveAt }).HasDatabaseName("IX_Players_OnlineActivity");
            entity.HasIndex(e => new { e.PartyId, e.IsOnline }).HasDatabaseName("IX_Players_PartyOnline");
            entity.HasIndex(e => new { e.Level, e.Experience }).HasDatabaseName("IX_Players_LevelExp");
        });
    }

    private void ConfigureTeamEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeamEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CaptainId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Active");
            entity.Property(e => e.CurrentBattleId).HasMaxLength(50);
            entity.Property(e => e.MaxMembers).HasDefaultValue(5);
            
            // JSON 字段配置
            entity.Property(e => e.MemberIdsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            
            // 索引
            entity.HasIndex(e => e.CaptainId).HasDatabaseName("IX_Teams_CaptainId");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_Teams_Status");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Teams_CreatedAt");
            entity.HasIndex(e => e.CurrentBattleId).HasDatabaseName("IX_Teams_CurrentBattle");
            
            // 复合索引
            entity.HasIndex(e => new { e.Status, e.CreatedAt }).HasDatabaseName("IX_Teams_StatusCreated");
            entity.HasIndex(e => new { e.CaptainId, e.Status }).HasDatabaseName("IX_Teams_CaptainStatus");
        });
    }

    private void ConfigureActionTargetEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActionTargetEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PlayerId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TargetType).HasMaxLength(50);
            entity.Property(e => e.TargetId).HasMaxLength(50);
            entity.Property(e => e.TargetName).HasMaxLength(200);
            entity.Property(e => e.ActionType).HasMaxLength(50);
            entity.Property(e => e.Progress).HasDefaultValue(0.0);
            entity.Property(e => e.Duration).HasDefaultValue(0.0);
            entity.Property(e => e.IsCompleted).HasDefaultValue(false);
            
            // JSON 字段配置
            entity.Property(e => e.ProgressDataJson).HasColumnType("TEXT").HasDefaultValue("{}");
            
            // 索引
            entity.HasIndex(e => e.PlayerId).HasDatabaseName("IX_ActionTargets_PlayerId");
            entity.HasIndex(e => e.IsCompleted).HasDatabaseName("IX_ActionTargets_IsCompleted");
            entity.HasIndex(e => e.StartedAt).HasDatabaseName("IX_ActionTargets_StartedAt");
            entity.HasIndex(e => e.CompletedAt).HasDatabaseName("IX_ActionTargets_CompletedAt");
            entity.HasIndex(e => e.TargetType).HasDatabaseName("IX_ActionTargets_TargetType");
            entity.HasIndex(e => e.ActionType).HasDatabaseName("IX_ActionTargets_ActionType");
            
            // 复合索引
            entity.HasIndex(e => new { e.PlayerId, e.IsCompleted }).HasDatabaseName("IX_ActionTargets_PlayerCompleted");
            entity.HasIndex(e => new { e.PlayerId, e.StartedAt }).HasDatabaseName("IX_ActionTargets_PlayerStarted");
            entity.HasIndex(e => new { e.IsCompleted, e.CompletedAt }).HasDatabaseName("IX_ActionTargets_CompletedAt");
            entity.HasIndex(e => new { e.TargetType, e.ActionType }).HasDatabaseName("IX_ActionTargets_TypeAction");
        });
    }

    private void ConfigureBattleRecordEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BattleRecordEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50).IsRequired();
            entity.Property(e => e.BattleId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.BattleType).HasMaxLength(50).HasDefaultValue("Normal");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("InProgress");
            entity.Property(e => e.DungeonId).HasMaxLength(50);
            entity.Property(e => e.WaveNumber).HasDefaultValue(0);
            entity.Property(e => e.Duration).HasDefaultValue(0);
            
            // JSON 字段配置
            entity.Property(e => e.ParticipantsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.EnemiesJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.ActionsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.ResultsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            
            // 索引
            entity.HasIndex(e => e.BattleId).IsUnique().HasDatabaseName("IX_BattleRecords_BattleId");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_BattleRecords_Status");
            entity.HasIndex(e => e.StartedAt).HasDatabaseName("IX_BattleRecords_StartedAt");
            entity.HasIndex(e => e.EndedAt).HasDatabaseName("IX_BattleRecords_EndedAt");
            entity.HasIndex(e => e.PartyId).HasDatabaseName("IX_BattleRecords_PartyId");
            entity.HasIndex(e => e.BattleType).HasDatabaseName("IX_BattleRecords_BattleType");
            entity.HasIndex(e => e.DungeonId).HasDatabaseName("IX_BattleRecords_DungeonId");
            
            // 复合索引
            entity.HasIndex(e => new { e.Status, e.StartedAt }).HasDatabaseName("IX_BattleRecords_StatusStarted");
            entity.HasIndex(e => new { e.PartyId, e.StartedAt }).HasDatabaseName("IX_BattleRecords_PartyStarted");
            entity.HasIndex(e => new { e.BattleType, e.Status }).HasDatabaseName("IX_BattleRecords_TypeStatus");
            entity.HasIndex(e => new { e.StartedAt, e.EndedAt, e.Duration }).HasDatabaseName("IX_BattleRecords_TimeRange");
        });
    }

    private void ConfigureOfflineDataEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OfflineDataEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PlayerId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.IsSynced).HasDefaultValue(false);
            
            // JSON 字段配置
            entity.Property(e => e.DataJson).HasColumnType("TEXT").HasDefaultValue("{}");
            
            // 索引
            entity.HasIndex(e => e.PlayerId).HasDatabaseName("IX_OfflineData_PlayerId");
            entity.HasIndex(e => e.IsSynced).HasDatabaseName("IX_OfflineData_IsSynced");
            entity.HasIndex(e => e.DataType).HasDatabaseName("IX_OfflineData_DataType");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_OfflineData_CreatedAt");
            entity.HasIndex(e => e.SyncedAt).HasDatabaseName("IX_OfflineData_SyncedAt");
            
            // 复合索引
            entity.HasIndex(e => new { e.PlayerId, e.IsSynced }).HasDatabaseName("IX_OfflineData_PlayerSynced");
            entity.HasIndex(e => new { e.IsSynced, e.SyncedAt }).HasDatabaseName("IX_OfflineData_SyncedAt");
            entity.HasIndex(e => new { e.DataType, e.IsSynced }).HasDatabaseName("IX_OfflineData_TypeSynced");
        });
    }

    private void CreatePerformanceIndexes(ModelBuilder modelBuilder)
    {
        // 时间范围查询优化
        modelBuilder.Entity<PlayerEntity>()
            .HasIndex(e => new { e.CreatedAt, e.UpdatedAt })
            .HasDatabaseName("IX_Players_TimeRange");
            
        // 活动统计查询
        modelBuilder.Entity<PlayerEntity>()
            .HasIndex(e => new { e.IsOnline, e.Level, e.LastActiveAt })
            .HasDatabaseName("IX_Players_ActivityStats");
            
        // 战斗历史查询
        modelBuilder.Entity<BattleRecordEntity>()
            .HasIndex(e => new { e.Status, e.BattleType, e.StartedAt })
            .HasDatabaseName("IX_BattleRecords_HistoryQuery");
    }

    private void ConfigureEntityRelationships(ModelBuilder modelBuilder)
    {
        // 配置软删除而不是级联删除以保持数据完整性
        // 在实际应用中，我们通常不希望完全删除关联数据
        
        // Player -> ActionTargets (一对多)
        modelBuilder.Entity<ActionTargetEntity>()
            .HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(e => e.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Team -> BattleRecords (一对多)
        modelBuilder.Entity<BattleRecordEntity>()
            .HasOne<TeamEntity>()
            .WithMany()
            .HasForeignKey(e => e.PartyId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Player -> OfflineData (一对多)
        modelBuilder.Entity<OfflineDataEntity>()
            .HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(e => e.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// 确保数据库创建和优化
    /// </summary>
    public async Task EnsureDatabaseOptimizedAsync()
    {
        try
        {
            await Database.EnsureCreatedAsync();
            
            // SQLite特定的性能优化设置
            if (Database.IsSqlite())
            {
                await ApplySqliteOptimizationsAsync();
            }
            
            _logger?.LogInformation("Unified database optimization completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to optimize unified database");
            throw;
        }
    }

    private async Task ApplySqliteOptimizationsAsync()
    {
        var optimizations = new[]
        {
            "PRAGMA journal_mode = WAL",           // 使用WAL模式提高并发性能
            "PRAGMA synchronous = NORMAL",         // 平衡性能和数据安全
            "PRAGMA cache_size = 10000",          // 增加缓存大小（10MB）
            "PRAGMA temp_store = MEMORY",          // 将临时表存储在内存中
            "PRAGMA mmap_size = 268435456",       // 启用内存映射（256MB）
            "PRAGMA optimize",                     // 优化查询计划
            "PRAGMA analysis_limit = 1000"        // 设置分析限制
        };

        foreach (var pragma in optimizations)
        {
            try
            {
                await Database.ExecuteSqlRawAsync(pragma);
                _logger?.LogDebug("Applied SQLite optimization: {Pragma}", pragma);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to apply SQLite optimization: {Pragma}", pragma);
            }
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
            stats["Players"] = await Players.CountAsync();
            stats["Teams"] = await Teams.CountAsync();
            stats["ActionTargets"] = await ActionTargets.CountAsync();
            stats["BattleRecords"] = await BattleRecords.CountAsync();
            stats["OfflineData"] = await OfflineData.CountAsync();
            
            // 活动统计
            stats["OnlinePlayers"] = await Players.CountAsync(p => p.IsOnline);
            stats["ActiveTeams"] = await Teams.CountAsync(t => t.Status == "Active");
            stats["ActiveActionTargets"] = await ActionTargets.CountAsync(at => !at.IsCompleted);
            stats["ActiveBattles"] = await BattleRecords.CountAsync(br => br.Status == "InProgress");
            stats["UnsyncedOfflineData"] = await OfflineData.CountAsync(od => !od.IsSynced);
            
            // 时间统计
            var now = DateTime.UtcNow;
            var oneDayAgo = now.AddDays(-1);
            var oneWeekAgo = now.AddDays(-7);
            
            stats["PlayersCreatedToday"] = await Players.CountAsync(p => p.CreatedAt >= oneDayAgo);
            stats["PlayersCreatedThisWeek"] = await Players.CountAsync(p => p.CreatedAt >= oneWeekAgo);
            stats["BattlesStartedToday"] = await BattleRecords.CountAsync(br => br.StartedAt >= oneDayAgo);
            stats["BattlesStartedThisWeek"] = await BattleRecords.CountAsync(br => br.StartedAt >= oneWeekAgo);
            
            stats["LastUpdated"] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get database stats");
            stats["Error"] = ex.Message;
        }
        
        return stats;
    }

    /// <summary>
    /// 重建索引以优化性能
    /// </summary>
    public async Task RebuildIndexesAsync()
    {
        try
        {
            if (Database.IsSqlite())
            {
                await Database.ExecuteSqlRawAsync("REINDEX");
                await Database.ExecuteSqlRawAsync("ANALYZE");
                _logger?.LogInformation("Database indexes rebuilt successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to rebuild database indexes");
            throw;
        }
    }
}