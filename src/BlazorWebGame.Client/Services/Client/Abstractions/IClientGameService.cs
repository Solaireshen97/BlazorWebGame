namespace BlazorWebGame.Client.Services.Client.Abstractions;

/// <summary>
/// Base interface for all client game services
/// </summary>
public interface IClientGameService
{
    /// <summary>
    /// Initialize the service
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Check if service is ready
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Service state changed event
    /// </summary>
    event Action<bool>? OnServiceStateChanged;
}

/// <summary>
/// Interface for services that can operate offline
/// </summary>
public interface IOfflineCapableService : IClientGameService
{
    /// <summary>
    /// Enter offline mode
    /// </summary>
    Task EnterOfflineModeAsync();

    /// <summary>
    /// Exit offline mode and sync with server
    /// </summary>
    Task ExitOfflineModeAsync();

    /// <summary>
    /// Check if currently in offline mode
    /// </summary>
    bool IsOfflineMode { get; }
}

/// <summary>
/// Interface for services that can be disabled via feature flags
/// </summary>
public interface IFeatureFlaggedService : IClientGameService
{
    /// <summary>
    /// Check if service is enabled via feature flags
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Update service based on feature flag changes
    /// </summary>
    Task UpdateFeatureFlagAsync(bool enabled);
}