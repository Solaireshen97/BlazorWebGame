using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BlazorWebGame.Server.Data;

/// <summary>
/// 游戏数据库上下文 - 支持多种数据库提供程序
/// </summary>
public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    // 核心游戏实体
    public DbSet<PlayerDbEntity> Players { get; set; }
    public DbSet<CharacterDbEntity> Characters { get; set; }
    public DbSet<BattleRecordDbEntity> BattleRecords { get; set; }
    public DbSet<TeamDbEntity> Teams { get; set; }
    public DbSet<InventoryItemDbEntity> InventoryItems { get; set; }
    public DbSet<EquipmentDbEntity> Equipment { get; set; }
    public DbSet<QuestDbEntity> Quests { get; set; }
    public DbSet<OfflineDataDbEntity> OfflineData { get; set; }
    public DbSet<GameEventDbEntity> GameEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置实体映射
        ConfigurePlayerEntity(modelBuilder);
        ConfigureCharacterEntity(modelBuilder);
        ConfigureBattleRecordEntity(modelBuilder);
        ConfigureTeamEntity(modelBuilder);
        ConfigureInventoryItemEntity(modelBuilder);
        ConfigureEquipmentEntity(modelBuilder);
        ConfigureQuestEntity(modelBuilder);
        ConfigureOfflineDataEntity(modelBuilder);
        ConfigureGameEventEntity(modelBuilder);
    }

    private static void ConfigurePlayerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerDbEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            
            // 索引优化
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.LastLoginAt);
            
            // JSON 属性配置
            entity.Property(e => e.MetadataJson)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new()
                );
        });
    }

    private static void ConfigureCharacterEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CharacterDbEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.PlayerId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CharacterClass).HasMaxLength(50);
            
            // 外键关系
            entity.HasOne<PlayerDbEntity>()
                .WithMany()
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // 索引优化
            entity.HasIndex(e => e.PlayerId);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.LastActiveAt);
            
            // JSON 属性配置
            entity.Property(e => e.AttributesJson)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions?)null) ?? new()
                );
                
            entity.Property(e => e.SkillsJson)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new()
                );
        });
    }

    private static void ConfigureBattleRecordEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BattleRecordDbEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CharacterId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EnemyId).HasMaxLength(100);
            entity.Property(e => e.BattleType).HasMaxLength(50);
            entity.Property(e => e.Result).HasMaxLength(20);
            
            // 外键关系
            entity.HasOne<CharacterDbEntity>()
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // 索引优化
            entity.HasIndex(e => e.CharacterId);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.Result);
            
            // JSON 属性配置
            entity.Property(e => e.RewardsJson)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new()
                );
        });
    }

    private static void ConfigureTeamEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeamDbEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.CaptainId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            
            // 外键关系
            entity.HasOne<CharacterDbEntity>()
                .WithMany()
                .HasForeignKey(e => e.CaptainId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // 索引优化
            entity.HasIndex(e => e.CaptainId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsActive);
            
            // JSON 属性配置
            entity.Property(e => e.MemberIdsJson)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new()
                );
        });
    }

    private static void ConfigureInventoryItemEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItemDbEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CharacterId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ItemId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ItemName).HasMaxLength(200);
            entity.Property(e => e.ItemType).HasMaxLength(50);
            entity.Property(e => e.Rarity).HasMaxLength(20);
            
            // 外键关系
            entity.HasOne<CharacterDbEntity>()
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // 索引优化
            entity.HasIndex(e => e.CharacterId);
            entity.HasIndex(e => e.ItemId);
            entity.HasIndex(e => e.ItemType);
            entity.HasIndex(e => new { e.CharacterId, e.ItemId });
            
            // JSON 属性配置
            entity.Property(e => e.PropertiesJson)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new()
                );
        });
    }

    private static void ConfigureEquipmentEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EquipmentDbEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CharacterId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Slot).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ItemId).HasMaxLength(100);
            
            // 外键关系
            entity.HasOne<CharacterDbEntity>()
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // 索引优化
            entity.HasIndex(e => e.CharacterId);
            entity.HasIndex(e => new { e.CharacterId, e.Slot }).IsUnique();
        });
    }

    private static void ConfigureQuestEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuestDbEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CharacterId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.QuestId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.QuestName).HasMaxLength(300);
            entity.Property(e => e.Status).HasMaxLength(20);
            
            // 外键关系
            entity.HasOne<CharacterDbEntity>()
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // 索引优化
            entity.HasIndex(e => e.CharacterId);
            entity.HasIndex(e => e.QuestId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.CharacterId, e.QuestId });
            
            // JSON 属性配置
            entity.Property(e => e.ProgressJson)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new()
                );
        });
    }

    private static void ConfigureOfflineDataEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OfflineDataDbEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CharacterId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ActivityType).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);
            
            // 外键关系
            entity.HasOne<CharacterDbEntity>()
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // 索引优化
            entity.HasIndex(e => e.CharacterId);
            entity.HasIndex(e => e.ActivityType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.EndTime);
            
            // JSON 属性配置
            entity.Property(e => e.ActivityDataJson)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new()
                );
                
            entity.Property(e => e.RewardsJson)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new()
                );
        });
    }

    private static void ConfigureGameEventEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameEventDbEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.ActorId).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);
            
            // 索引优化
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.ActorId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Priority);
            
            // JSON 属性配置
            entity.Property(e => e.EventDataJson)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new()
                );
        });
    }
}