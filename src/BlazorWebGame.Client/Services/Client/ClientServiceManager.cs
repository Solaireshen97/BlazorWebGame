using BlazorWebGame.Client.Services.Client.Abstractions;
using BlazorWebGame.Client.Services.Client.Configuration;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Client.Services.Client;

/// <summary>
/// Central service manager for coordinating client services
/// </summary>
public class ClientServiceManager : IAsyncDisposable
{
    private readonly ILogger<ClientServiceManager> _logger;
    private readonly ClientConfiguration _configuration;
    private readonly List<IClientGameService> _services = new();
    private bool _isInitialized = false;
    private bool _isDisposed = false;

    public ClientServiceManager(
        ILogger<ClientServiceManager> logger,
        ClientConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Register a service with the manager
    /// </summary>
    public void RegisterService(IClientGameService service)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("Cannot register services after initialization");
        }

        _services.Add(service);
        service.OnServiceStateChanged += HandleServiceStateChanged;
    }

    /// <summary>
    /// Initialize all registered services
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        _logger.LogInformation("Initializing {ServiceCount} client services", _services.Count);

        var initTasks = _services.Select(async service =>
        {
            try
            {
                await service.InitializeAsync();
                _logger.LogDebug("Initialized service: {ServiceType}", service.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize service: {ServiceType}", service.GetType().Name);
                throw;
            }
        });

        await Task.WhenAll(initTasks);
        _isInitialized = true;
        
        _logger.LogInformation("All client services initialized successfully");
    }

    /// <summary>
    /// Enter offline mode for all offline-capable services
    /// </summary>
    public async Task EnterOfflineModeAsync()
    {
        _logger.LogWarning("Entering offline mode");

        var offlineServices = _services.OfType<IOfflineCapableService>().ToList();
        var offlineTasks = offlineServices.Select(async service =>
        {
            try
            {
                await service.EnterOfflineModeAsync();
                _logger.LogDebug("Service entered offline mode: {ServiceType}", service.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enter offline mode for service: {ServiceType}", service.GetType().Name);
            }
        });

        await Task.WhenAll(offlineTasks);
        _logger.LogInformation("Offline mode activated for {Count} services", offlineServices.Count);
    }

    /// <summary>
    /// Exit offline mode for all offline-capable services
    /// </summary>
    public async Task ExitOfflineModeAsync()
    {
        _logger.LogInformation("Exiting offline mode");

        var offlineServices = _services.OfType<IOfflineCapableService>().ToList();
        var onlineTasks = offlineServices.Select(async service =>
        {
            try
            {
                await service.ExitOfflineModeAsync();
                _logger.LogDebug("Service exited offline mode: {ServiceType}", service.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to exit offline mode for service: {ServiceType}", service.GetType().Name);
            }
        });

        await Task.WhenAll(onlineTasks);
        _logger.LogInformation("Online mode restored for {Count} services", offlineServices.Count);
    }

    /// <summary>
    /// Update feature flags for all feature-flagged services
    /// </summary>
    public async Task UpdateFeatureFlagsAsync()
    {
        _logger.LogInformation("Updating feature flags for services");

        var featureFlaggedServices = _services.OfType<IFeatureFlaggedService>().ToList();
        
        foreach (var service in featureFlaggedServices)
        {
            try
            {
                // This would need specific logic for each service type
                // For now, just call the update method
                await service.UpdateFeatureFlagAsync(service.IsEnabled);
                _logger.LogDebug("Updated feature flags for service: {ServiceType}", service.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update feature flags for service: {ServiceType}", service.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Get all ready services
    /// </summary>
    public IEnumerable<IClientGameService> GetReadyServices()
    {
        return _services.Where(s => s.IsReady);
    }

    /// <summary>
    /// Get services by type
    /// </summary>
    public IEnumerable<T> GetServices<T>() where T : class, IClientGameService
    {
        return _services.OfType<T>();
    }

    /// <summary>
    /// Check if all services are ready
    /// </summary>
    public bool AreAllServicesReady => _services.All(s => s.IsReady);

    private void HandleServiceStateChanged(bool isReady)
    {
        _logger.LogDebug("Service state changed. Ready services: {ReadyCount}/{TotalCount}", 
            GetReadyServices().Count(), _services.Count);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _logger.LogInformation("Disposing client service manager");

        foreach (var service in _services)
        {
            service.OnServiceStateChanged -= HandleServiceStateChanged;
            
            if (service is IAsyncDisposable asyncDisposable)
            {
                try
                {
                    await asyncDisposable.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing service: {ServiceType}", service.GetType().Name);
                }
            }
            else if (service is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing service: {ServiceType}", service.GetType().Name);
                }
            }
        }

        _services.Clear();
        _isDisposed = true;
        
        _logger.LogInformation("Client service manager disposed");
    }
}