using BlazorWebGame.Refactored.Application.Interfaces;
using Blazored.LocalStorage;

namespace BlazorWebGame.Refactored.Infrastructure.Services;

/// <summary>
/// 本地存储服务实现
/// </summary>
public class LocalStorageService : Application.Interfaces.ILocalStorageService
{
    private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(Blazored.LocalStorage.ILocalStorageService localStorage, ILogger<LocalStorageService> logger)
    {
        _localStorage = localStorage;
        _logger = logger;
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        try
        {
            return await _localStorage.GetItemAsync<T>(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item from local storage: {Key}", key);
            return default;
        }
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        try
        {
            await _localStorage.SetItemAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting item in local storage: {Key}", key);
        }
    }

    public async Task RemoveItemAsync(string key)
    {
        try
        {
            await _localStorage.RemoveItemAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from local storage: {Key}", key);
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await _localStorage.ClearAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing local storage");
        }
    }

    public async Task<bool> ContainKeyAsync(string key)
    {
        try
        {
            return await _localStorage.ContainKeyAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key exists in local storage: {Key}", key);
            return false;
        }
    }
}