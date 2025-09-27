using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Domain.ValueObjects;
using BlazorWebGame.Refactored.Presentation.State;

namespace BlazorWebGame.Refactored.Application.Interfaces;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
    Task<AuthResult> RefreshTokenAsync();
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    string? CurrentUserId { get; }
    string? CurrentUsername { get; }
    Task<bool> ValidateTokenAsync();
}

/// <summary>
/// 角色服务接口
/// </summary>
public interface ICharacterService
{
    Task<IEnumerable<CharacterData>> GetCharactersAsync();
    Task<CharacterData?> GetCharacterDetailAsync(Guid characterId);
    Task<CharacterData> CreateCharacterAsync(string name, CharacterClass characterClass);
    Task UpdateCharacterAsync(Guid characterId, CharacterUpdateData updateData);
    Task DeleteCharacterAsync(Guid characterId);
    Task<ResourcePool> GetCharacterResourcesAsync(Guid characterId);
    Task<IEnumerable<ItemReward>> GetCharacterInventoryAsync(Guid characterId);
    
    // Additional methods for CQRS
    Task<Character?> GetCharacterAsync(Guid characterId);
    Task<Character?> GetCharacterByNameAsync(string name);
    Task<Character> CreateCharacterAsync(Character character);
    Task<List<Character>> GetUserCharactersAsync(string userId);
}

/// <summary>
/// 活动服务接口
/// </summary>
public interface IActivityService
{
    Task<ActivityResult> StartActivityAsync(Guid characterId, ActivityRequest request);
    Task CancelActivityAsync(Guid activityId);
    Task<IEnumerable<ActivitySummary>> GetCharacterActivitiesAsync(Guid characterId);
    Task<ActivitySummary?> GetActivityStatusAsync(Guid activityId);
    Task<ActivityResult> CompleteActivityAsync(Guid activityId);
    Task UpdateActivityProgressAsync(Guid activityId, double progress);
    
    // Additional methods for CQRS (with different signatures to avoid conflict)
    Task<Activity> StartActivityAsync(Guid characterId, ActivityType activityType, ActivityParameters parameters);
    Task<List<Activity>> GetCharacterActivitiesListAsync(Guid characterId);
    Task<Activity?> GetActivityAsync(Guid activityId);
}

/// <summary>
/// 战斗服务接口
/// </summary>
public interface IBattleService
{
    Task<BattleData> StartBattleAsync(Guid characterId, string enemyId, string? partyId = null);
    Task<BattleData?> GetBattleStateAsync(Guid battleId);
    Task<BattleActionResult> ExecuteBattleActionAsync(Guid battleId, BattleActionRequest request);
    Task EndBattleAsync(Guid battleId);
    Task<IEnumerable<BattleData>> GetActiveBattlesAsync(Guid characterId);
}

/// <summary>
/// SignalR服务接口
/// </summary>
public interface ISignalRService
{
    Task StartAsync();
    Task StopAsync();
    bool IsConnected { get; }
    
    Task JoinGameAsync(string userId);
    Task LeaveGameAsync();
    Task SendCharacterActionAsync(Guid characterId, string action, object data);
    
    // 事件处理器
    event Func<string, Task>? OnCharacterUpdate;
    event Func<string, Task>? OnActivityUpdate;
    event Func<string, Task>? OnBattleUpdate;
    event Func<string, Task>? OnNotification;
    event Func<string, object, Task>? OnRealtimeEvent;
}

/// <summary>
/// 缓存服务接口
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task ClearAsync();
    Task CleanupExpiredEntriesAsync();
    Task<bool> ExistsAsync(string key);
    CacheStatistics GetStatistics();
}

/// <summary>
/// HTTP客户端服务接口
/// </summary>
public interface IHttpClientService
{
    Task<T?> GetAsync<T>(string endpoint);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data);
    Task<bool> DeleteAsync(string endpoint);
    
    // Game-specific API methods
    Task<List<BlazorWebGame.Refactored.Application.DTOs.CharacterDto>> GetCharactersAsync(string userId);
    Task<BlazorWebGame.Refactored.Application.DTOs.CharacterDto?> GetCharacterAsync(Guid characterId);
    Task<BlazorWebGame.Refactored.Application.DTOs.CharacterDto?> CreateCharacterAsync(BlazorWebGame.Refactored.Infrastructure.Http.CreateCharacterRequest request);
    Task<bool> DeleteCharacterAsync(Guid characterId);
    Task<List<BlazorWebGame.Refactored.Application.DTOs.ActivityDto>> GetCharacterActivitiesAsync(Guid characterId);
    Task<BlazorWebGame.Refactored.Application.DTOs.ActivityDto?> StartActivityAsync(BlazorWebGame.Refactored.Infrastructure.Http.StartActivityRequest request);
    Task<bool> CancelActivityAsync(Guid activityId);
    Task<BlazorWebGame.Refactored.Application.DTOs.ActivityDto?> GetActivityAsync(Guid activityId);
}

/// <summary>
/// 本地存储服务接口
/// </summary>
public interface ILocalStorageService
{
    Task<T?> GetItemAsync<T>(string key);
    Task SetItemAsync<T>(string key, T value);
    Task RemoveItemAsync(string key);
    Task ClearAsync();
    Task<bool> ContainKeyAsync(string key);
}

/// <summary>
/// 时间同步服务接口
/// </summary>
public interface ITimeSyncService
{
    DateTime ServerNow { get; }
    TimeSpan ServerTimeDrift { get; }
    Task UpdateServerTimeAsync(DateTime serverTime, TimeSpan latency);
    double InterpolateProgress(DateTime startTime, DateTime endTime, InterpolationOptions? options = null);
}

/// <summary>
/// 通知服务接口
/// </summary>
public interface INotificationService
{
    Task ShowNotificationAsync(string title, string message, NotificationType type = NotificationType.Info);
    Task ShowNotificationAsync(NotificationMessage notification);
    Task HideNotificationAsync(Guid notificationId);
    Task ClearAllNotificationAsync();
    Task<IEnumerable<NotificationMessage>> GetNotificationsAsync();
}

/// <summary>
/// 性能监控服务接口
/// </summary>
public interface IPerformanceService
{
    void RecordMetric(string name, double value, Dictionary<string, string>? tags = null);
    IDisposable MeasureOperation(string operationName);
    PerformanceReport GetReport();
    Task<SystemHealth> GetSystemHealthAsync();
}

// ======================
// 数据传输对象
// ======================

/// <summary>
/// 认证结果
/// </summary>
public record AuthResult
{
    public bool IsSuccess { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? UserId { get; init; }
    public string? Username { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 活动结果
/// </summary>
public record ActivityServiceResult
{
    public bool IsSuccess { get; init; }
    public ActivitySummary? Activity { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 战斗动作请求
/// </summary>
public record BattleActionRequest
{
    public string ActorId { get; init; } = string.Empty;
    public string TargetId { get; init; } = string.Empty;
    public BattleActionType Type { get; init; }
    public string? SkillId { get; init; }
}

/// <summary>
/// 战斗动作结果
/// </summary>
public record BattleActionResult
{
    public bool IsSuccess { get; init; }
    public BattleAction? Action { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// API响应包装
/// </summary>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public List<string> Errors { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// API响应包装（无数据）
/// </summary>
public record ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public List<string> Errors { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 角色更新事件
/// </summary>
public record CharacterUpdateEvent
{
    public Guid CharacterId { get; init; }
    public CharacterUpdateData Data { get; init; } = new();
}

/// <summary>
/// 战斗更新事件
/// </summary>
public record BattleUpdateEvent
{
    public Guid BattleId { get; init; }
    public BattleUpdateData Data { get; init; } = new();
}

/// <summary>
/// 活动更新事件
/// </summary>
public record ActivityUpdateEvent
{
    public Guid ActivityId { get; init; }
    public double Progress { get; init; }
    public Domain.ValueObjects.ActivityState? State { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// 缓存统计信息
/// </summary>
public record CacheStatistics
{
    public int TotalEntries { get; init; }
    public long TotalSize { get; init; }
    public int HitCount { get; init; }
    public int MissCount { get; init; }
    public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
    public int TotalRequests => HitCount + MissCount;
    public DateTime LastCleanup { get; init; }
}

/// <summary>
/// 插值选项
/// </summary>
public record InterpolationOptions
{
    public bool ClampToMax { get; init; } = true;
    public Func<double, double>? EasingFunction { get; init; }
}

/// <summary>
/// 性能报告
/// </summary>
public record PerformanceReport
{
    public double AverageResponseTime { get; init; }
    public long MemoryUsage { get; init; }
    public int ActiveConnections { get; init; }
    public Dictionary<string, double> CustomMetrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 系统健康状态
/// </summary>
public record SystemHealth
{
    public bool IsHealthy { get; init; }
    public Dictionary<string, HealthCheckResult> Checks { get; init; } = new();
    public TimeSpan ResponseTime { get; init; }
}

/// <summary>
/// 健康检查结果
/// </summary>
public record HealthCheckResult
{
    public bool IsHealthy { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, object>? Data { get; init; }
}