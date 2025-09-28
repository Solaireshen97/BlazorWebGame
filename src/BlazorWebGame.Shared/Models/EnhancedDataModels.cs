using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 增强的实体基类 - 支持审计跟踪、软删除和版本控制
/// </summary>
public abstract class EnhancedBaseEntity
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(50)]
    public string? CreatedBy { get; set; }
    
    [MaxLength(50)]
    public string? UpdatedBy { get; set; }

    // 软删除支持
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    
    [MaxLength(50)]
    public string? DeletedBy { get; set; }

    // 版本控制 - 用于乐观锁
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // 数据版本 - 用于迁移管理
    public int DataVersion { get; set; } = 1;

    /// <summary>
    /// 标记为已删除（软删除）
    /// </summary>
    public virtual void MarkAsDeleted(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = deletedBy;
    }

    /// <summary>
    /// 恢复已删除的记录
    /// </summary>
    public virtual void Restore(string? restoredBy = null)
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = restoredBy;
    }

    /// <summary>
    /// 更新时间戳
    /// </summary>
    public virtual void Touch(string? updatedBy = null)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}

/// <summary>
/// 增强的玩家实体
/// </summary>
[Table("Players")]
public class EnhancedPlayerEntity : EnhancedBaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int Gold { get; set; } = 0;

    [MaxLength(50)]
    public string SelectedBattleProfession { get; set; } = "Warrior";

    [MaxLength(50)]
    public string CurrentAction { get; set; } = "Idle";

    [MaxLength(50)]
    public string? CurrentActionTargetId { get; set; }

    public Guid? PartyId { get; set; }
    public bool IsOnline { get; set; } = true;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

    // 服务器端状态
    [MaxLength(50)]
    public string? ServerRegion { get; set; }
    
    public DateTime? LastSyncAt { get; set; }
    public int SyncVersion { get; set; } = 0;

    // JSON序列化的复杂属性
    [Column(TypeName = "TEXT")]
    public string AttributesJson { get; set; } = "{}";

    [Column(TypeName = "TEXT")]
    public string InventoryJson { get; set; } = "[]";

    [Column(TypeName = "TEXT")]
    public string SkillsJson { get; set; } = "[]";

    [Column(TypeName = "TEXT")]
    public string EquipmentJson { get; set; } = "{}";

    [Column(TypeName = "TEXT")]
    public string SettingsJson { get; set; } = "{}";

    // 导航属性
    public virtual ICollection<EnhancedActionTargetEntity> ActionTargets { get; set; } = new List<EnhancedActionTargetEntity>();
    public virtual ICollection<EnhancedOfflineDataEntity> OfflineData { get; set; } = new List<EnhancedOfflineDataEntity>();

    // 业务方法
    public Dictionary<string, object> GetAttributes()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(AttributesJson) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    public void SetAttributes(Dictionary<string, object> attributes)
    {
        AttributesJson = JsonSerializer.Serialize(attributes);
        Touch();
    }

    public List<object> GetInventory()
    {
        try
        {
            return JsonSerializer.Deserialize<List<object>>(InventoryJson) ?? new List<object>();
        }
        catch
        {
            return new List<object>();
        }
    }

    public void SetInventory(List<object> inventory)
    {
        InventoryJson = JsonSerializer.Serialize(inventory);
        Touch();
    }

    public List<string> GetSkills()
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(SkillsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public void SetSkills(List<string> skills)
    {
        SkillsJson = JsonSerializer.Serialize(skills);
        Touch();
    }

    public Dictionary<string, string> GetEquipment()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(EquipmentJson) ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    public void SetEquipment(Dictionary<string, string> equipment)
    {
        EquipmentJson = JsonSerializer.Serialize(equipment);
        Touch();
    }

    /// <summary>
    /// 更新在线状态
    /// </summary>
    public void UpdateOnlineStatus(bool isOnline)
    {
        IsOnline = isOnline;
        LastActiveAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// 标记为已同步
    /// </summary>
    public void MarkSynced()
    {
        LastSyncAt = DateTime.UtcNow;
        SyncVersion++;
        Touch();
    }
}

/// <summary>
/// 增强的队伍实体
/// </summary>
[Table("Teams")]
public class EnhancedTeamEntity : EnhancedBaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CaptainId { get; set; } = string.Empty;

    public int MaxMembers { get; set; } = 5;

    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Disbanded, InBattle

    [MaxLength(50)]
    public string? CurrentBattleId { get; set; }

    public DateTime LastBattleAt { get; set; } = DateTime.UtcNow;

    // 队伍设置
    public bool IsPublic { get; set; } = true;
    public bool AutoAcceptMembers { get; set; } = false;
    
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "TEXT")]
    public string MemberIdsJson { get; set; } = "[]";

    [Column(TypeName = "TEXT")]
    public string TeamSettingsJson { get; set; } = "{}";

    // 业务方法
    public List<string> GetMemberIds()
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(MemberIdsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public void SetMemberIds(List<string> memberIds)
    {
        MemberIdsJson = JsonSerializer.Serialize(memberIds);
        Touch();
    }

    public Dictionary<string, object> GetTeamSettings()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(TeamSettingsJson) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    public void SetTeamSettings(Dictionary<string, object> settings)
    {
        TeamSettingsJson = JsonSerializer.Serialize(settings);
        Touch();
    }

    /// <summary>
    /// 添加成员
    /// </summary>
    public bool AddMember(string playerId)
    {
        var members = GetMemberIds();
        if (members.Contains(playerId) || members.Count >= MaxMembers)
            return false;

        members.Add(playerId);
        SetMemberIds(members);
        return true;
    }

    /// <summary>
    /// 移除成员
    /// </summary>
    public bool RemoveMember(string playerId)
    {
        var members = GetMemberIds();
        if (members.Remove(playerId))
        {
            SetMemberIds(members);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 检查是否为队长
    /// </summary>
    public bool IsCaptain(string playerId) => CaptainId == playerId;

    /// <summary>
    /// 检查是否为成员
    /// </summary>
    public bool IsMember(string playerId) => GetMemberIds().Contains(playerId) || IsCaptain(playerId);
}

/// <summary>
/// 增强的动作目标实体
/// </summary>
[Table("ActionTargets")]
public class EnhancedActionTargetEntity : EnhancedBaseEntity
{
    [Required]
    [MaxLength(50)]
    public string PlayerId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string TargetType { get; set; } = string.Empty; // Enemy, GatheringNode, Recipe, Quest

    [MaxLength(50)]
    public string TargetId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string TargetName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty; // Combat, Gathering, Crafting, Quest

    [Range(0.0, 1.0)]
    public double Progress { get; set; } = 0.0;

    public double Duration { get; set; } = 0.0;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; } = false;

    // 优先级和自动化设置
    public int Priority { get; set; } = 0;
    public bool IsAutomatic { get; set; } = false;
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;

    [Column(TypeName = "TEXT")]
    public string ProgressDataJson { get; set; } = "{}";

    [Column(TypeName = "TEXT")]
    public string RequirementsJson { get; set; } = "{}";

    [Column(TypeName = "TEXT")]
    public string RewardsJson { get; set; } = "{}";

    // 导航属性
    [ForeignKey(nameof(PlayerId))]
    public virtual EnhancedPlayerEntity Player { get; set; } = null!;

    // 业务方法
    public Dictionary<string, object> GetProgressData()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(ProgressDataJson) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    public void SetProgressData(Dictionary<string, object> data)
    {
        ProgressDataJson = JsonSerializer.Serialize(data);
        Touch();
    }

    public Dictionary<string, object> GetRequirements()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(RequirementsJson) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    public void SetRequirements(Dictionary<string, object> requirements)
    {
        RequirementsJson = JsonSerializer.Serialize(requirements);
        Touch();
    }

    public Dictionary<string, object> GetRewards()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(RewardsJson) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    public void SetRewards(Dictionary<string, object> rewards)
    {
        RewardsJson = JsonSerializer.Serialize(rewards);
        Touch();
    }

    /// <summary>
    /// 更新进度
    /// </summary>
    public void UpdateProgress(double newProgress, Dictionary<string, object>? progressData = null)
    {
        Progress = Math.Clamp(newProgress, 0.0, 1.0);
        if (progressData != null)
        {
            SetProgressData(progressData);
        }
        Touch();

        if (Progress >= 1.0 && !IsCompleted)
        {
            Complete();
        }
    }

    /// <summary>
    /// 完成动作
    /// </summary>
    public void Complete()
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        Progress = 1.0;
        Touch();
    }

    /// <summary>
    /// 重试动作
    /// </summary>
    public bool Retry()
    {
        if (RetryCount >= MaxRetries)
            return false;

        RetryCount++;
        Progress = 0.0;
        IsCompleted = false;
        CompletedAt = null;
        StartedAt = DateTime.UtcNow;
        Touch();
        return true;
    }
}

/// <summary>
/// 增强的战斗记录实体
/// </summary>
[Table("BattleRecords")]
public class EnhancedBattleRecordEntity : EnhancedBaseEntity
{
    [Required]
    [MaxLength(50)]
    public string BattleId { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(50)]
    public string BattleType { get; set; } = "Normal"; // Normal, Dungeon, PvP, Boss

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "InProgress"; // InProgress, Victory, Defeat, Abandoned

    public Guid? PartyId { get; set; }

    [MaxLength(50)]
    public string? DungeonId { get; set; }

    public int WaveNumber { get; set; } = 0;
    public int Duration { get; set; } = 0; // in seconds

    // 战斗统计
    public int TotalDamageDealt { get; set; } = 0;
    public int TotalDamageTaken { get; set; } = 0;
    public int TotalHealing { get; set; } = 0;
    public int EnemiesDefeated { get; set; } = 0;

    [Column(TypeName = "TEXT")]
    public string ParticipantsJson { get; set; } = "[]"; // Player IDs

    [Column(TypeName = "TEXT")]
    public string EnemiesJson { get; set; } = "[]"; // Enemy data

    [Column(TypeName = "TEXT")]
    public string ActionsJson { get; set; } = "[]"; // Battle actions/log

    [Column(TypeName = "TEXT")]
    public string ResultsJson { get; set; } = "{}"; // Rewards, XP, etc.

    [Column(TypeName = "TEXT")]
    public string BattleSettingsJson { get; set; } = "{}";

    // 业务方法
    public List<string> GetParticipants()
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(ParticipantsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public void SetParticipants(List<string> participants)
    {
        ParticipantsJson = JsonSerializer.Serialize(participants);
        Touch();
    }

    public List<object> GetEnemies()
    {
        try
        {
            return JsonSerializer.Deserialize<List<object>>(EnemiesJson) ?? new List<object>();
        }
        catch
        {
            return new List<object>();
        }
    }

    public void SetEnemies(List<object> enemies)
    {
        EnemiesJson = JsonSerializer.Serialize(enemies);
        Touch();
    }

    public List<object> GetActions()
    {
        try
        {
            return JsonSerializer.Deserialize<List<object>>(ActionsJson) ?? new List<object>();
        }
        catch
        {
            return new List<object>();
        }
    }

    public void SetActions(List<object> actions)
    {
        ActionsJson = JsonSerializer.Serialize(actions);
        Touch();
    }

    public Dictionary<string, object> GetResults()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(ResultsJson) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    public void SetResults(Dictionary<string, object> results)
    {
        ResultsJson = JsonSerializer.Serialize(results);
        Touch();
    }

    /// <summary>
    /// 结束战斗
    /// </summary>
    public void EndBattle(string status, Dictionary<string, object>? results = null)
    {
        Status = status;
        EndedAt = DateTime.UtcNow;
        if (results != null)
        {
            SetResults(results);
        }
        
        if (StartedAt != DateTime.MinValue)
        {
            Duration = (int)(DateTime.UtcNow - StartedAt).TotalSeconds;
        }
        
        Touch();
    }

    /// <summary>
    /// 添加战斗动作
    /// </summary>
    public void AddAction(object action)
    {
        var actions = GetActions();
        actions.Add(action);
        SetActions(actions);
    }

    /// <summary>
    /// 更新战斗统计
    /// </summary>
    public void UpdateStats(int damageDealt = 0, int damageTaken = 0, int healing = 0, int enemiesDefeated = 0)
    {
        TotalDamageDealt += damageDealt;
        TotalDamageTaken += damageTaken;
        TotalHealing += healing;
        EnemiesDefeated += enemiesDefeated;
        Touch();
    }
}

/// <summary>
/// 增强的离线数据实体
/// </summary>
[Table("OfflineData")]
public class EnhancedOfflineDataEntity : EnhancedBaseEntity
{
    [Required]
    [MaxLength(50)]
    public string PlayerId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string DataType { get; set; } = string.Empty; // PlayerProgress, BattleState, TeamState

    public DateTime SyncedAt { get; set; } = DateTime.MinValue;
    public bool IsSynced { get; set; } = false;
    public int SyncVersion { get; set; } = 1;

    // 优先级和重试机制
    public int Priority { get; set; } = 0; // 0 = Normal, 1 = High, 2 = Critical
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public DateTime? NextRetryAt { get; set; }

    // 数据校验
    [MaxLength(100)]
    public string? DataHash { get; set; }
    
    public long DataSize { get; set; } = 0;

    [Column(TypeName = "TEXT")]
    public string DataJson { get; set; } = "{}";

    [Column(TypeName = "TEXT")]
    public string MetadataJson { get; set; } = "{}";

    // 导航属性
    [ForeignKey(nameof(PlayerId))]
    public virtual EnhancedPlayerEntity Player { get; set; } = null!;

    // 业务方法
    public Dictionary<string, object> GetData()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(DataJson) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    public void SetData(Dictionary<string, object> data)
    {
        DataJson = JsonSerializer.Serialize(data);
        DataSize = DataJson.Length;
        DataHash = ComputeHash(DataJson);
        Touch();
    }

    public Dictionary<string, object> GetMetadata()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(MetadataJson) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    public void SetMetadata(Dictionary<string, object> metadata)
    {
        MetadataJson = JsonSerializer.Serialize(metadata);
        Touch();
    }

    /// <summary>
    /// 标记为已同步
    /// </summary>
    public void MarkSynced()
    {
        IsSynced = true;
        SyncedAt = DateTime.UtcNow;
        SyncVersion++;
        RetryCount = 0;
        NextRetryAt = null;
        Touch();
    }

    /// <summary>
    /// 标记同步失败，计划重试
    /// </summary>
    public bool MarkSyncFailed()
    {
        RetryCount++;
        if (RetryCount >= MaxRetries)
        {
            return false; // 超过最大重试次数
        }

        // 指数退避重试策略
        var delayMinutes = Math.Pow(2, RetryCount - 1) * 5; // 5分钟, 10分钟, 20分钟...
        NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
        Touch();
        return true;
    }

    /// <summary>
    /// 检查是否可以重试
    /// </summary>
    public bool CanRetry()
    {
        return RetryCount < MaxRetries && 
               (NextRetryAt == null || DateTime.UtcNow >= NextRetryAt);
    }

    /// <summary>
    /// 验证数据完整性
    /// </summary>
    public bool ValidateData()
    {
        if (string.IsNullOrEmpty(DataHash))
            return true; // 没有哈希值，跳过验证

        var currentHash = ComputeHash(DataJson);
        return currentHash == DataHash;
    }

    private static string ComputeHash(string data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}