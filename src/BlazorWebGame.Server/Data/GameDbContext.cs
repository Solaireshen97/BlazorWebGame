using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Shared.Models;
using System.Text.Json;

namespace BlazorWebGame.Server.Data;

/// <summary>
/// 游戏数据库上下文 - SQLite数据库
/// </summary>
public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    // 数据库表集合
    public DbSet<PlayerEntity> Players { get; set; }
    public DbSet<TeamEntity> Teams { get; set; }
    public DbSet<ActionTargetEntity> ActionTargets { get; set; }
    public DbSet<BattleRecordEntity> BattleRecords { get; set; }
    public DbSet<OfflineDataEntity> OfflineData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置玩家实体
        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SelectedBattleProfession).HasMaxLength(50);
            entity.Property(e => e.CurrentAction).HasMaxLength(50);
            entity.Property(e => e.CurrentActionTargetId).HasMaxLength(50);
            
            // JSON 字段配置
            entity.Property(e => e.AttributesJson).HasColumnType("TEXT");
            entity.Property(e => e.InventoryJson).HasColumnType("TEXT");
            entity.Property(e => e.SkillsJson).HasColumnType("TEXT");
            entity.Property(e => e.EquipmentJson).HasColumnType("TEXT");
            
            // 索引
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsOnline);
            entity.HasIndex(e => e.LastActiveAt);
            entity.HasIndex(e => e.PartyId);
        });

        // 配置队伍实体
        modelBuilder.Entity<TeamEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CaptainId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.CurrentBattleId).HasMaxLength(50);
            
            // JSON 字段配置
            entity.Property(e => e.MemberIdsJson).HasColumnType("TEXT");
            
            // 索引
            entity.HasIndex(e => e.CaptainId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // 配置动作目标实体
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
            entity.Property(e => e.ProgressDataJson).HasColumnType("TEXT");
            
            // 索引
            entity.HasIndex(e => e.PlayerId);
            entity.HasIndex(e => e.IsCompleted);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => new { e.PlayerId, e.IsCompleted });
        });

        // 配置战斗记录实体
        modelBuilder.Entity<BattleRecordEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.BattleId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.BattleType).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.DungeonId).HasMaxLength(50);
            
            // JSON 字段配置
            entity.Property(e => e.ParticipantsJson).HasColumnType("TEXT");
            entity.Property(e => e.EnemiesJson).HasColumnType("TEXT");
            entity.Property(e => e.ActionsJson).HasColumnType("TEXT");
            entity.Property(e => e.ResultsJson).HasColumnType("TEXT");
            
            // 索引
            entity.HasIndex(e => e.BattleId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.PartyId);
            entity.HasIndex(e => e.BattleType);
        });

        // 配置离线数据实体
        modelBuilder.Entity<OfflineDataEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.PlayerId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DataType).HasMaxLength(50);
            
            // JSON 字段配置
            entity.Property(e => e.DataJson).HasColumnType("TEXT");
            
            // 索引
            entity.HasIndex(e => e.PlayerId);
            entity.HasIndex(e => e.IsSynced);
            entity.HasIndex(e => e.DataType);
            entity.HasIndex(e => new { e.PlayerId, e.IsSynced });
        });
    }

    /// <summary>
    /// 确保数据库创建和种子数据
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync()
    {
        await Database.EnsureCreatedAsync();
        
        // 可以在这里添加种子数据逻辑
        // await SeedDataAsync();
    }

    /// <summary>
    /// 种子数据方法（可选）
    /// </summary>
    private async Task SeedDataAsync()
    {
        // 示例：如果没有数据，添加一些初始数据
        if (!await Players.AnyAsync())
        {
            // 添加种子数据的逻辑
        }
    }
}