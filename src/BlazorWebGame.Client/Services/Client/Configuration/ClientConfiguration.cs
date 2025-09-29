namespace BlazorWebGame.Client.Services.Client.Configuration;

/// <summary>
/// Client configuration settings for feature flags and server integration
/// </summary>
public class ClientConfiguration
{
    /// <summary>
    /// Server base URL
    /// </summary>
    public string ServerUrl { get; set; } = "https://localhost:7000";

    /// <summary>
    /// Feature flags for gradual server migration
    /// </summary>
    public FeatureFlags Features { get; set; } = new();

    /// <summary>
    /// API timeout settings
    /// </summary>
    public TimeoutSettings Timeouts { get; set; } = new();

    /// <summary>
    /// Offline mode configuration
    /// </summary>
    public OfflineModeSettings OfflineMode { get; set; } = new();
}

/// <summary>
/// Feature flags for enabling/disabling server-side functionality
/// </summary>
public class FeatureFlags
{
    /// <summary>
    /// Use server-side battle system (default: false for gradual migration)
    /// </summary>
    public bool UseServerBattle { get; set; } = false;

    /// <summary>
    /// Use server-side inventory system (default: false for gradual migration)
    /// </summary>
    public bool UseServerInventory { get; set; } = false;

    /// <summary>
    /// Use server-side character system (default: false for gradual migration)
    /// </summary>
    public bool UseServerCharacter { get; set; } = false;

    /// <summary>
    /// Use server-side quest system (default: false for gradual migration)
    /// </summary>
    public bool UseServerQuest { get; set; } = false;

    /// <summary>
    /// Use server-side party system (default: false for gradual migration)
    /// </summary>
    public bool UseServerParty { get; set; } = false;

    /// <summary>
    /// Enable offline mode functionality
    /// </summary>
    public bool EnableOfflineMode { get; set; } = true;
}

/// <summary>
/// API timeout configuration
/// </summary>
public class TimeoutSettings
{
    /// <summary>
    /// Standard API call timeout in seconds
    /// </summary>
    public int ApiTimeout { get; set; } = 30;

    /// <summary>
    /// Long-running operation timeout in seconds
    /// </summary>
    public int LongOperationTimeout { get; set; } = 120;

    /// <summary>
    /// SignalR connection timeout in seconds
    /// </summary>
    public int SignalRTimeout { get; set; } = 60;
}

/// <summary>
/// Offline mode configuration
/// </summary>
public class OfflineModeSettings
{
    /// <summary>
    /// Enable automatic offline mode when server is unavailable
    /// </summary>
    public bool AutoEnterOfflineMode { get; set; } = true;

    /// <summary>
    /// Maximum number of offline actions to queue
    /// </summary>
    public int MaxOfflineActions { get; set; } = 1000;

    /// <summary>
    /// Offline data sync interval in minutes
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 5;
}