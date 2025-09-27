using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorWebGame.Server.Data;

/// <summary>
/// 玩家数据库实体
/// </summary>
public class PlayerDbEntity
{
    [Key]
    public string Id { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Email { get; set; }
    
    [MaxLength(500)]
    public string? PasswordHash { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // JSON 存储的元数据 (设置、偏好等)
    public Dictionary<string, object> MetadataJson { get; set; } = new();
}

/// <summary>
/// 角色数据库实体
/// </summary>
public class CharacterDbEntity
{
    [Key]
    public string Id { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string PlayerId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? CharacterClass { get; set; }
    
    public int Level { get; set; } = 1;
    public long Experience { get; set; } = 0;
    public long Gold { get; set; } = 0;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int Mana { get; set; } = 50;
    public int MaxMana { get; set; } = 50;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public bool IsDead { get; set; } = false;
    
    // JSON 存储的复杂数据
    public Dictionary<string, int> AttributesJson { get; set; } = new();
    public List<string> SkillsJson { get; set; } = new();
}

/// <summary>
/// 战斗记录数据库实体
/// </summary>
public class BattleRecordDbEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string CharacterId { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? EnemyId { get; set; }
    
    [MaxLength(50)]
    public string? BattleType { get; set; }
    
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    
    [MaxLength(20)]
    public string? Result { get; set; } // "Victory", "Defeat", "Timeout"
    
    // JSON 存储的奖励数据
    public Dictionary<string, object> RewardsJson { get; set; } = new();
}

/// <summary>
/// 队伍数据库实体
/// </summary>
public class TeamDbEntity
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Name { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string CaptainId { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public int MaxMembers { get; set; } = 5;
    public bool IsPublic { get; set; } = true;
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DisbandedAt { get; set; }
    
    // JSON 存储的成员ID列表
    public List<string> MemberIdsJson { get; set; } = new();
}

/// <summary>
/// 背包物品数据库实体
/// </summary>
public class InventoryItemDbEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string CharacterId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ItemId { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? ItemName { get; set; }
    
    [MaxLength(50)]
    public string? ItemType { get; set; }
    
    [MaxLength(20)]
    public string? Rarity { get; set; }
    
    public int Quantity { get; set; } = 1;
    public int SlotPosition { get; set; } = 0;
    public bool IsStackable { get; set; } = true;
    
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
    
    // JSON 存储的物品属性
    public Dictionary<string, object> PropertiesJson { get; set; } = new();
}

/// <summary>
/// 装备数据库实体
/// </summary>
public class EquipmentDbEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string CharacterId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Slot { get; set; } = string.Empty; // "MainHand", "OffHand", "Helmet", etc.
    
    [MaxLength(100)]
    public string? ItemId { get; set; }
    
    public DateTime EquippedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 任务数据库实体
/// </summary>
public class QuestDbEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string CharacterId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string QuestId { get; set; } = string.Empty;
    
    [MaxLength(300)]
    public string? QuestName { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // "Active", "Completed", "Failed"
    
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    // JSON 存储的任务进度数据
    public Dictionary<string, object> ProgressJson { get; set; } = new();
}

/// <summary>
/// 离线数据数据库实体
/// </summary>
public class OfflineDataDbEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string CharacterId { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? ActivityType { get; set; } // "Mining", "Crafting", "Battle", etc.
    
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // "Pending", "Processed", "Expired"
    
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public double Efficiency { get; set; } = 1.0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    
    // JSON 存储的活动数据和奖励
    public Dictionary<string, object> ActivityDataJson { get; set; } = new();
    public Dictionary<string, object> RewardsJson { get; set; } = new();
}

/// <summary>
/// 游戏事件数据库实体
/// </summary>
public class GameEventDbEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? EntityId { get; set; }
    
    [MaxLength(50)]
    public string? ActorId { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // "Pending", "Processing", "Completed", "Failed"
    
    public int Priority { get; set; } = 0;
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    
    // JSON 存储的事件数据
    public Dictionary<string, object> EventDataJson { get; set; } = new();
}