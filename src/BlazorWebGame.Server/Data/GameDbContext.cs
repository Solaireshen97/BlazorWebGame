using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Shared.Models;

namespace BlazorWebGame.Server.Data;

/// <summary>
/// SQLite数据库上下文 - 游戏数据存储
/// </summary>
public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    // 数据表定义
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<PlayerEntity> Players { get; set; }
    public DbSet<TeamEntity> Teams { get; set; }
    public DbSet<ActionTargetEntity> ActionTargets { get; set; }
    public DbSet<BattleRecordEntity> BattleRecords { get; set; }
    public DbSet<OfflineDataEntity> OfflineData { get; set; }
    public DbSet<UserCharacterEntity> UserCharacters { get; set; }
    public DbSet<CharacterEntity> Characters { get; set; } = null!;

    // 增强版战斗实体表
    public DbSet<EnhancedBattleEntity> EnhancedBattles { get; set; } = null!;
    public DbSet<EnhancedBattleParticipantEntity> EnhancedBattleParticipants { get; set; } = null!;
    public DbSet<EnhancedBattleEventEntity> EnhancedBattleEvents { get; set; } = null!;
    public DbSet<EnhancedBattleResultEntity> EnhancedBattleResults { get; set; } = null!;
    public DbSet<EnhancedBattleSystemConfigEntity> EnhancedBattleSystemConfigs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置用户实体
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Salt).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastLoginIp).HasMaxLength(45); // IPv6 max length

            // 新增字段配置
            entity.Property(e => e.DisplayName).HasMaxLength(100).HasDefaultValue(string.Empty);
            entity.Property(e => e.Avatar).HasMaxLength(255).HasDefaultValue(string.Empty);
            entity.Property(e => e.LastPasswordChange); // 允许null的DateTime类型

            // JSON字段
            entity.Property(e => e.RolesJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.ProfileJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.LoginHistoryJson).HasColumnType("TEXT").HasDefaultValue("[]"); // 新增JSON字段
            entity.Property(e => e.CharacterIdsJson).HasColumnType("TEXT").HasDefaultValue("[]"); // 新增JSON字段

            // 索引
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.LastLoginAt);
            entity.HasIndex(e => e.DisplayName); // 为显示名称添加索引，提高查询性能
        });

        // 配置角色实体
        modelBuilder.Entity<CharacterEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProfessionId).HasMaxLength(20);
            entity.Property(e => e.CurrentRegionId).HasMaxLength(100);

            // 基本信息索引
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsOnline);
            entity.HasIndex(e => e.LastActiveAt);
            entity.HasIndex(e => e.PartyId);

            // JSON字段配置
            entity.Property(e => e.BattleProfessionsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.GatheringProfessionsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.ProductionProfessionsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.ReputationsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.InventoryJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.EquipmentJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.OfflineRecordJson).HasColumnType("TEXT").HasDefaultValue("{}");

            // 新增字段配置
            entity.Property(e => e.LearnedSkillsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.EquippedSkillsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.ActiveQuestsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.CompletedQuestsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.QuestProgressJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.GeneralConsumableSlotsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.CombatConsumableSlotsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.ActivitySlotsJson).HasColumnType("TEXT").HasDefaultValue("[]");
        });

        // 配置增强版战斗实体
        modelBuilder.Entity<EnhancedBattleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.BattleType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PartyId).HasMaxLength(100);
            entity.Property(e => e.DungeonId).HasMaxLength(100);
            entity.Property(e => e.RegionId).HasMaxLength(100);
            entity.Property(e => e.BattleModeType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.DifficultyLevel).HasMaxLength(20).IsRequired();
            entity.Property(e => e.EnvironmentType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CurrentActorId).HasMaxLength(100);
            entity.Property(e => e.InviteCode).HasMaxLength(20);

            // JSON字段
            entity.Property(e => e.BattleRulesJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.WeatherEffectsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.TerrainEffectsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.ParticipantsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.EventsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.StateJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.ResultJson).HasColumnType("TEXT");

            // 索引
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.PartyId);
            entity.HasIndex(e => e.DungeonId);
            entity.HasIndex(e => e.IsPrivate);
        });

        modelBuilder.Entity<EnhancedBattleParticipantEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.BattleId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ParticipantType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SourceId).HasMaxLength(100);

            // JSON字段
            entity.Property(e => e.CombatStatsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.SkillsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.BuffsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.DebuffsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.EquipmentEffectsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.CooldownsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.ResourcesJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.AIStrategyJson).HasColumnType("TEXT");

            // 索引
            entity.HasIndex(e => e.BattleId);
            entity.HasIndex(e => e.SourceId);
            entity.HasIndex(e => e.IsAlive);
            entity.HasIndex(e => new { e.BattleId, e.Team });
        });

        modelBuilder.Entity<EnhancedBattleEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.BattleId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(30).IsRequired();
            entity.Property(e => e.ActorId).HasMaxLength(100);
            entity.Property(e => e.TargetId).HasMaxLength(100);
            entity.Property(e => e.SkillId).HasMaxLength(50);
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.DamageType).HasMaxLength(20);

            // JSON字段
            entity.Property(e => e.TargetsJson).HasColumnType("TEXT");
            entity.Property(e => e.EffectsAppliedJson).HasColumnType("TEXT");
            entity.Property(e => e.EventDetailsJson).HasColumnType("TEXT");
            entity.Property(e => e.VisualEffectsJson).HasColumnType("TEXT");
            entity.Property(e => e.AnimationData).HasColumnType("TEXT");

            // 索引
            entity.HasIndex(e => e.BattleId);
            entity.HasIndex(e => new { e.BattleId, e.TurnNumber });
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.ActorId);
        });

        modelBuilder.Entity<EnhancedBattleResultEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.BattleId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Outcome).HasMaxLength(20).IsRequired();
            entity.Property(e => e.WinningTeam).HasMaxLength(20);
            entity.Property(e => e.MVPId).HasMaxLength(100);

            // JSON字段
            entity.Property(e => e.SurvivorIdsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.RewardsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.ExperienceDistributionJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.ItemDropsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.SpecialRewardsJson).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.BattleStatisticsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.PlayerPerformanceJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.StoryProgressionJson).HasColumnType("TEXT");
            entity.Property(e => e.UnlockedContentJson).HasColumnType("TEXT");

            // 索引
            entity.HasIndex(e => e.BattleId).IsUnique();
            entity.HasIndex(e => e.Outcome);
            entity.HasIndex(e => e.CompletedAt);
        });

        modelBuilder.Entity<EnhancedBattleSystemConfigEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.ConfigType).HasMaxLength(30).IsRequired();
            entity.Property(e => e.BattleTypeReference).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            // JSON字段
            entity.Property(e => e.TurnMechanicsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.AIMechanicsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.DamageFormulaJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.HealingFormulaJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.StatusEffectRulesJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.BattleBalanceSettingsJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.DifficultyScalingJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.RewardCalculationRulesJson).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.SpecialMechanicsJson).HasColumnType("TEXT").HasDefaultValue("{}");

            // 索引
            entity.HasIndex(e => e.ConfigType);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Version);
            entity.HasIndex(e => new { e.ConfigType, e.IsActive });
        });

        // 配置玩家实体
        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SelectedBattleProfession).HasMaxLength(20);
            entity.Property(e => e.CurrentAction).HasMaxLength(20);
            entity.Property(e => e.CurrentActionTargetId).HasMaxLength(100);

            // JSON字段
            entity.Property(e => e.AttributesJson).HasColumnType("TEXT");
            entity.Property(e => e.InventoryJson).HasColumnType("TEXT");
            entity.Property(e => e.SkillsJson).HasColumnType("TEXT");
            entity.Property(e => e.EquipmentJson).HasColumnType("TEXT");

            // 索引
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsOnline);
            entity.HasIndex(e => e.LastActiveAt);
        });

        // 配置队伍实体
        modelBuilder.Entity<TeamEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CaptainId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.CurrentBattleId).HasMaxLength(100);

            // JSON字段
            entity.Property(e => e.MemberIdsJson).HasColumnType("TEXT");

            // 索引
            entity.HasIndex(e => e.CaptainId);
            entity.HasIndex(e => e.Status);
        });

        // 配置动作目标实体
        modelBuilder.Entity<ActionTargetEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.PlayerId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TargetType).HasMaxLength(50);
            entity.Property(e => e.TargetId).HasMaxLength(100);
            entity.Property(e => e.TargetName).HasMaxLength(100);
            entity.Property(e => e.ActionType).HasMaxLength(50);

            // JSON字段
            entity.Property(e => e.ProgressDataJson).HasColumnType("TEXT");

            // 索引
            entity.HasIndex(e => e.PlayerId);
            entity.HasIndex(e => e.IsCompleted);
            entity.HasIndex(e => e.StartedAt);
        });

        // 配置战斗记录实体
        modelBuilder.Entity<BattleRecordEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.BattleId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.BattleType).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.DungeonId).HasMaxLength(100);

            // JSON字段
            entity.Property(e => e.ParticipantsJson).HasColumnType("TEXT");
            entity.Property(e => e.EnemiesJson).HasColumnType("TEXT");
            entity.Property(e => e.ActionsJson).HasColumnType("TEXT");
            entity.Property(e => e.ResultsJson).HasColumnType("TEXT");

            // 索引
            entity.HasIndex(e => e.BattleId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.PartyId);
        });

        // 配置离线数据实体
        modelBuilder.Entity<OfflineDataEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.PlayerId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DataType).HasMaxLength(50);

            // JSON字段
            entity.Property(e => e.DataJson).HasColumnType("TEXT");

            // 索引
            entity.HasIndex(e => e.PlayerId);
            entity.HasIndex(e => e.IsSynced);
            entity.HasIndex(e => e.CreatedAt);
        });

        // 配置用户角色关联实体
        modelBuilder.Entity<UserCharacterEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CharacterId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CharacterName).HasMaxLength(50);

            // 索引
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CharacterId);
            entity.HasIndex(e => new { e.UserId, e.CharacterId }).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsDefault);
        });
    }
}
