using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace BlazorWebGame.Client.Services.Client.Configuration;

/// <summary>
/// Service to load and manage client configuration
/// </summary>
public class ConfigurationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ConfigurationService> _logger;
    private ClientConfiguration? _configuration;
    private bool _isLoaded = false;

    public ConfigurationService(HttpClient httpClient, ILogger<ConfigurationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Load configuration from appsettings.json
    /// </summary>
    public async Task<ClientConfiguration> LoadConfigurationAsync()
    {
        if (_isLoaded && _configuration != null)
        {
            return _configuration;
        }

        try
        {
            _logger.LogInformation("Loading client configuration from appsettings.json");
            
            // Try to load from appsettings.json
            var configData = await _httpClient.GetFromJsonAsync<Dictionary<string, object>>("appsettings.json");
            
            if (configData != null)
            {
                _configuration = ParseConfiguration(configData);
                _logger.LogInformation("Configuration loaded successfully from appsettings.json");
            }
            else
            {
                _logger.LogWarning("Failed to load configuration, using defaults");
                _configuration = new ClientConfiguration();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration, using defaults");
            _configuration = new ClientConfiguration();
        }

        _isLoaded = true;
        return _configuration;
    }

    /// <summary>
    /// Get current configuration (loads if not already loaded)
    /// </summary>
    public async Task<ClientConfiguration> GetConfigurationAsync()
    {
        if (!_isLoaded || _configuration == null)
        {
            return await LoadConfigurationAsync();
        }
        return _configuration;
    }

    /// <summary>
    /// Update configuration and optionally persist changes
    /// </summary>
    public void UpdateConfiguration(Action<ClientConfiguration> updateAction)
    {
        if (_configuration != null)
        {
            updateAction(_configuration);
            _logger.LogInformation("Configuration updated");
        }
    }

    private ClientConfiguration ParseConfiguration(Dictionary<string, object> configData)
    {
        var config = new ClientConfiguration();

        // Parse ServerUrl
        if (configData.TryGetValue("ServerUrl", out var serverUrl))
        {
            config.ServerUrl = serverUrl.ToString() ?? config.ServerUrl;
        }

        // Parse Features
        if (configData.TryGetValue("Features", out var featuresObj) && featuresObj is System.Text.Json.JsonElement featuresElement)
        {
            config.Features = ParseFeatureFlags(featuresElement);
        }

        // Parse Timeouts
        if (configData.TryGetValue("Timeouts", out var timeoutsObj) && timeoutsObj is System.Text.Json.JsonElement timeoutsElement)
        {
            config.Timeouts = ParseTimeoutSettings(timeoutsElement);
        }

        // Parse OfflineMode
        if (configData.TryGetValue("OfflineMode", out var offlineModeObj) && offlineModeObj is System.Text.Json.JsonElement offlineModeElement)
        {
            config.OfflineMode = ParseOfflineModeSettings(offlineModeElement);
        }

        return config;
    }

    private FeatureFlags ParseFeatureFlags(System.Text.Json.JsonElement element)
    {
        var features = new FeatureFlags();

        if (element.TryGetProperty("UseServerBattle", out var useServerBattle))
            features.UseServerBattle = useServerBattle.GetBoolean();

        if (element.TryGetProperty("UseServerInventory", out var useServerInventory))
            features.UseServerInventory = useServerInventory.GetBoolean();

        if (element.TryGetProperty("UseServerCharacter", out var useServerCharacter))
            features.UseServerCharacter = useServerCharacter.GetBoolean();

        if (element.TryGetProperty("UseServerQuest", out var useServerQuest))
            features.UseServerQuest = useServerQuest.GetBoolean();

        if (element.TryGetProperty("UseServerParty", out var useServerParty))
            features.UseServerParty = useServerParty.GetBoolean();

        if (element.TryGetProperty("EnableOfflineMode", out var enableOfflineMode))
            features.EnableOfflineMode = enableOfflineMode.GetBoolean();

        return features;
    }

    private TimeoutSettings ParseTimeoutSettings(System.Text.Json.JsonElement element)
    {
        var timeouts = new TimeoutSettings();

        if (element.TryGetProperty("ApiTimeout", out var apiTimeout))
            timeouts.ApiTimeout = apiTimeout.GetInt32();

        if (element.TryGetProperty("LongOperationTimeout", out var longOperationTimeout))
            timeouts.LongOperationTimeout = longOperationTimeout.GetInt32();

        if (element.TryGetProperty("SignalRTimeout", out var signalRTimeout))
            timeouts.SignalRTimeout = signalRTimeout.GetInt32();

        return timeouts;
    }

    private OfflineModeSettings ParseOfflineModeSettings(System.Text.Json.JsonElement element)
    {
        var offlineMode = new OfflineModeSettings();

        if (element.TryGetProperty("AutoEnterOfflineMode", out var autoEnterOfflineMode))
            offlineMode.AutoEnterOfflineMode = autoEnterOfflineMode.GetBoolean();

        if (element.TryGetProperty("MaxOfflineActions", out var maxOfflineActions))
            offlineMode.MaxOfflineActions = maxOfflineActions.GetInt32();

        if (element.TryGetProperty("SyncIntervalMinutes", out var syncIntervalMinutes))
            offlineMode.SyncIntervalMinutes = syncIntervalMinutes.GetInt32();

        return offlineMode;
    }
}