using BlazorWebGame.Client.Services.Client.Abstractions;
using BlazorWebGame.Client.Services.Client.Configuration;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Client.Services.Client.Facades;

/// <summary>
/// Facade that routes calls between local services and server APIs based on feature flags
/// </summary>
public class GameServiceFacade
{
    private readonly ILogger<GameServiceFacade> _logger;
    private readonly ClientConfiguration _configuration;

    public GameServiceFacade(
        ILogger<GameServiceFacade> logger,
        ClientConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Route service call based on feature flags
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="serverCall">Server-side implementation</param>
    /// <param name="localCall">Local implementation (fallback)</param>
    /// <param name="featureEnabled">Feature flag check</param>
    /// <returns>Result from appropriate implementation</returns>
    public async Task<TResult> RouteCall<TResult>(
        Func<Task<TResult>> serverCall,
        Func<Task<TResult>> localCall,
        bool featureEnabled)
    {
        try
        {
            if (featureEnabled)
            {
                _logger.LogDebug("Routing to server implementation");
                return await serverCall();
            }
            else
            {
                _logger.LogDebug("Routing to local implementation");
                return await localCall();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in service call, attempting fallback");
            
            // If server call failed and we were using server, try local fallback
            if (featureEnabled)
            {
                _logger.LogWarning("Server call failed, falling back to local implementation");
                return await localCall();
            }
            
            throw;
        }
    }

    /// <summary>
    /// Route service call with no return value
    /// </summary>
    /// <param name="serverCall">Server-side implementation</param>
    /// <param name="localCall">Local implementation (fallback)</param>
    /// <param name="featureEnabled">Feature flag check</param>
    public async Task RouteCall(
        Func<Task> serverCall,
        Func<Task> localCall,
        bool featureEnabled)
    {
        try
        {
            if (featureEnabled)
            {
                _logger.LogDebug("Routing to server implementation");
                await serverCall();
            }
            else
            {
                _logger.LogDebug("Routing to local implementation");
                await localCall();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in service call, attempting fallback");
            
            // If server call failed and we were using server, try local fallback
            if (featureEnabled)
            {
                _logger.LogWarning("Server call failed, falling back to local implementation");
                await localCall();
            }
            else
            {
                throw;
            }
        }
    }
}