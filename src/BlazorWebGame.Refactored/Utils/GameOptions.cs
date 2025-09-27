using Polly;
using Polly.Extensions.Http;

namespace BlazorWebGame.Refactored.Utils;

/// <summary>
/// 游戏配置选项
/// </summary>
public class GameOptions
{
    public const string SectionName = "Game";

    public string ServerBaseUrl { get; set; } = "https://localhost:7000";
    public int MaxCharactersPerAccount { get; set; } = 5;
    public int MaxActiveActivitiesPerCharacter { get; set; } = 3;
    public TimeSpan DefaultPollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan BattlePollingInterval { get; set; } = TimeSpan.FromSeconds(3);
    public TimeSpan FastPollingInterval { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan CharacterCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan StaticDataCacheExpiration { get; set; } = TimeSpan.FromHours(1);
    public int MaxNotifications { get; set; } = 20;
    public int ActivityListPageSize { get; set; } = 10;
    public bool EnablePerformanceMonitoring { get; set; } = true;
    public bool EnableDebugMode { get; set; } = false;
}

/// <summary>
/// SignalR配置选项
/// </summary>
public class SignalROptions
{
    public const string SectionName = "SignalR";

    public string HubUrl { get; set; } = "/gamehub";
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxReconnectAttempts { get; set; } = 5;
    public bool EnableAutomaticReconnect { get; set; } = true;
    public TimeSpan HandshakeTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan ServerTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// 缓存配置选项
/// </summary>
public class CacheOptions
{
    public const string SectionName = "Cache";

    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(15);
    public long MaxCacheSize { get; set; } = 50 * 1024 * 1024; // 50MB
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
    public double MemoryPressureThreshold { get; set; } = 0.8; // 80%
    public bool EnableCompression { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
}

/// <summary>
/// HTTP重试策略工厂
/// </summary>
public static class RetryPolicyFactory
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.Values.ContainsKey("Logger") ? 
                        context.Values["Logger"] as ILogger : null;
                    logger?.LogWarning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    // 可以在这里记录断路器打开的日志
                },
                onReset: () =>
                {
                    // 可以在这里记录断路器重置的日志
                });
    }
}

/// <summary>
/// 游戏常量定义
/// </summary>
public static class GameConstants
{
    // 角色限制
    public const int MaxCharactersPerAccount = 5;
    public const int MaxActiveActivitiesPerCharacter = 3;
    
    // 轮询间隔
    public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan BattlePollingInterval = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan FastPollingInterval = TimeSpan.FromSeconds(1);
    
    // 缓存配置
    public static readonly TimeSpan CharacterCacheExpiration = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan StaticDataCacheExpiration = TimeSpan.FromHours(1);
    
    // SignalR配置
    public static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    public static readonly int MaxReconnectAttempts = 5;
    
    // UI配置
    public const int MaxNotifications = 20;
    public const int ActivityListPageSize = 10;
    
    // 网络配置
    public static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan LongPollingTimeout = TimeSpan.FromMinutes(2);
    
    // 性能配置
    public const int MaxConcurrentRequests = 10;
    public const long MaxMemoryUsage = 100 * 1024 * 1024; // 100MB
    
    // 游戏逻辑常量
    public const int BaseExperiencePerLevel = 100;
    public const double ExperienceMultiplier = 1.5;
    public const int MaxLevel = 100;
    public const int StartingGold = 1000;
    public const int InventorySlots = 50;
    
    // 活动配置
    public const int MaxBattleDurationMinutes = 30;
    public const int MaxGatheringDurationMinutes = 60;
    public const int MaxCraftingDurationHours = 4;
    
    // 本地存储键名
    public static class LocalStorageKeys
    {
        public const string AccessToken = "access_token";
        public const string RefreshToken = "refresh_token";
        public const string UserId = "user_id";
        public const string Username = "username";
        public const string TokenExpiresAt = "token_expires_at";
        public const string CurrentCharacterId = "current_character_id";
        public const string GameSettings = "game_settings";
        public const string UIPreferences = "ui_preferences";
        public const string CachedData = "cached_data";
    }
    
    // API端点
    public static class ApiEndpoints
    {
        public const string Auth = "/api/auth";
        public const string Characters = "/api/character";
        public const string Battles = "/api/battle";
        public const string Activities = "/api/activity";
        public const string Inventory = "/api/inventory";
        public const string Equipment = "/api/equipment";
        public const string Quests = "/api/quest";
        public const string Shop = "/api/shop";
        public const string Production = "/api/production";
        public const string DataStorage = "/api/datastorage";
        public const string Monitoring = "/api/monitoring";
    }
    
    // SignalR组名
    public static class SignalRGroups
    {
        public static string Character(Guid characterId) => $"character-{characterId}";
        public static string Battle(Guid battleId) => $"battle-{battleId}";
        public static string Party(string partyId) => $"party-{partyId}";
        public static string Global => "global";
        public static string User(string userId) => $"user-{userId}";
    }
    
    // 事件类型
    public static class EventTypes
    {
        public const string CharacterUpdate = "CharacterUpdate";
        public const string BattleUpdate = "BattleUpdate";
        public const string ActivityUpdate = "ActivityUpdate";
        public const string Notification = "Notification";
        public const string SystemMessage = "SystemMessage";
        public const string PartyUpdate = "PartyUpdate";
        public const string InventoryUpdate = "InventoryUpdate";
    }
}