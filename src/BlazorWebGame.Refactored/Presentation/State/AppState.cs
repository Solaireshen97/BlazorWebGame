using System.Collections.Immutable;
using Microsoft.AspNetCore.SignalR.Client;
using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Domain.ValueObjects;
using Fluxor;

namespace BlazorWebGame.Refactored.Presentation.State;

/// <summary>
/// 全局应用状态
/// </summary>
[FeatureState]
public record AppState
{
    public AuthState Auth { get; init; } = new();
    public CharacterState Characters { get; init; } = new();
    public BattleState Battles { get; init; } = new();
    public ActivityState Activities { get; init; } = new();
    public UIState UI { get; init; } = new();
    public RealtimeState Realtime { get; init; } = new();
    public CacheState Cache { get; init; } = new();
}

/// <summary>
/// 认证状态
/// </summary>
public record AuthState
{
    public bool IsAuthenticated { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? UserId { get; init; }
    public string? Username { get; init; }
    public DateTime? TokenExpiresAt { get; init; }
    public bool IsLoading { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// 角色状态
/// </summary>
public record CharacterState
{
    public Guid? CurrentCharacterId { get; init; }
    public ImmutableDictionary<Guid, CharacterData> Characters { get; init; } = 
        ImmutableDictionary<Guid, CharacterData>.Empty;
    public ImmutableDictionary<Guid, ActivitySummary> ActiveActivities { get; init; } = 
        ImmutableDictionary<Guid, ActivitySummary>.Empty;
    public bool IsLoading { get; init; }
    public string? Error { get; init; }
    public DateTime LastUpdated { get; init; }

    // Convenience properties for UI
    public CharacterData? CurrentCharacter => 
        CurrentCharacterId.HasValue && Characters.ContainsKey(CurrentCharacterId.Value) 
            ? Characters[CurrentCharacterId.Value] 
            : null;
    
    public IEnumerable<CharacterData> AllCharacters => Characters.Values;
}

/// <summary>
/// 战斗状态
/// </summary>
public record BattleState
{
    public ImmutableDictionary<Guid, BattleData> ActiveBattles { get; init; } = 
        ImmutableDictionary<Guid, BattleData>.Empty;
    public Guid? CurrentBattleId { get; init; }
    public bool IsLoading { get; init; }
    public string? Error { get; init; }
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// 活动状态
/// </summary>
public record ActivityState
{
    public ImmutableDictionary<Guid, ActivitySummary> Activities { get; init; } = 
        ImmutableDictionary<Guid, ActivitySummary>.Empty;
    public ImmutableList<ActivityType> SupportedTypes { get; init; } = 
        ImmutableList<ActivityType>.Empty;
    public bool IsLoading { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// UI状态
/// </summary>
public record UIState
{
    public bool IsSidebarExpanded { get; init; } = true;
    public string CurrentPage { get; init; } = "Dashboard";
    public ImmutableQueue<NotificationMessage> Notifications { get; init; } = 
        ImmutableQueue<NotificationMessage>.Empty;
    public bool IsOffline { get; init; }
    public string? GlobalError { get; init; }
    public Dictionary<string, bool> LoadingStates { get; init; } = new();
}

/// <summary>
/// 实时通信状态
/// </summary>
public record RealtimeState
{
    public HubConnectionState ConnectionState { get; init; } = HubConnectionState.Disconnected;
    public ImmutableHashSet<string> JoinedGroups { get; init; } = 
        ImmutableHashSet<string>.Empty;
    public ImmutableQueue<RealtimeEvent> PendingEvents { get; init; } = 
        ImmutableQueue<RealtimeEvent>.Empty;
    public DateTime LastHeartbeat { get; init; }
    public TimeSpan ServerTimeDrift { get; init; } = TimeSpan.Zero;
    public int ReconnectAttempts { get; init; }
    public bool AutoReconnectEnabled { get; init; } = true;
}

/// <summary>
/// 缓存状态
/// </summary>
public record CacheState
{
    public ImmutableDictionary<string, CacheEntry> MemoryCache { get; init; } = 
        ImmutableDictionary<string, CacheEntry>.Empty;
    public DateTime LastCleanup { get; init; }
    public long TotalCacheSize { get; init; }
    public int HitCount { get; init; }
    public int MissCount { get; init; }
}

/// <summary>
/// 角色数据
/// </summary>
public record CharacterData
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public CharacterClass Class { get; init; }
    public int Level { get; init; }
    public BigNumber Experience { get; init; }
    public CharacterStats Stats { get; init; } = new();
    public ResourcePool Resources { get; init; } = new();
    public DateTime LastLogin { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsOnline { get; init; }
    public Vector3 Position { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// 活动摘要
/// </summary>
public record ActivitySummary
{
    public Guid Id { get; init; }
    public Guid CharacterId { get; init; }
    public ActivityType Type { get; init; }
    public ActivityDisplayState State { get; init; } = ActivityDisplayState.Active;
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public double Progress { get; init; }
    public int Priority { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool CanInterrupt { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// 战斗数据
/// </summary>
public record BattleData
{
    public Guid Id { get; init; }
    public Guid CharacterId { get; init; }
    public string EnemyId { get; init; } = string.Empty;
    public string? PartyId { get; init; }
    public BattleStatus Status { get; init; }
    public List<BattleParticipant> Players { get; init; } = new();
    public List<BattleParticipant> Enemies { get; init; } = new();
    public DateTime StartTime { get; init; }
    public DateTime LastUpdated { get; init; }
    public List<BattleAction> RecentActions { get; init; } = new();
}

/// <summary>
/// 战斗参与者
/// </summary>
public record BattleParticipant
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Health { get; init; }
    public int MaxHealth { get; init; }
    public int AttackPower { get; init; }
    public double AttacksPerSecond { get; init; }
    public bool IsPlayer { get; init; }
    public Dictionary<string, double> SkillCooldowns { get; init; } = new();
}

/// <summary>
/// 战斗动作
/// </summary>
public record BattleAction
{
    public string ActorId { get; init; } = string.Empty;
    public string TargetId { get; init; } = string.Empty;
    public BattleActionType Type { get; init; }
    public string? SkillId { get; init; }
    public int Damage { get; init; }
    public DateTime Timestamp { get; init; }
    public bool IsCritical { get; init; }
}

/// <summary>
/// 通知消息
/// </summary>
public record NotificationMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(5);
    public bool IsRead { get; init; }
    public Dictionary<string, object>? Data { get; init; }
}

/// <summary>
/// 实时事件
/// </summary>
public record RealtimeEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Type { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public Dictionary<string, object> Data { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool IsProcessed { get; init; }
}

/// <summary>
/// 缓存条目
/// </summary>
public record CacheEntry
{
    public object Data { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; init; }
    public long Size { get; init; }
    public int AccessCount { get; init; }
    public DateTime LastAccessed { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 3D坐标
/// </summary>
public record Vector3(double X = 0, double Y = 0, double Z = 0);

/// <summary>
/// 枚举定义
/// </summary>
public enum BattleStatus
{
    Pending,
    Active,
    Victory,
    Defeat,
    Cancelled
}

public enum BattleActionType
{
    Attack,
    Skill,
    Item,
    Defend,
    Flee
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    Achievement
}

/// <summary>
/// 活动状态枚举 (Presentation layer)
/// </summary>
public enum ActivityDisplayState
{
    Active,
    Paused,
    Completed,
    Cancelled,
    Failed
}