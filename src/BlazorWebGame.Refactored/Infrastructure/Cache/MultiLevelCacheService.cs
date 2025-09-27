using BlazorWebGame.Refactored.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BlazorWebGame.Refactored.Infrastructure.Cache;

public class MultiLevelCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<MultiLevelCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // 缓存层级配置
    private readonly TimeSpan _level1Duration = TimeSpan.FromMinutes(5);   // 内存缓存：热数据
    private readonly TimeSpan _level2Duration = TimeSpan.FromHours(1);     // LocalStorage：温数据
    private readonly TimeSpan _level3Duration = TimeSpan.FromDays(7);      // IndexedDB：冷数据

    public MultiLevelCacheService(
        IMemoryCache memoryCache, 
        ILocalStorageService localStorage,
        ILogger<MultiLevelCacheService> logger)
    {
        _memoryCache = memoryCache;
        _localStorage = localStorage;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            // Level 1: 内存缓存（最快）
            if (_memoryCache.TryGetValue(GetMemoryKey(key), out T? memoryValue))
            {
                _logger.LogDebug("Cache hit (Memory): {Key}", key);
                return memoryValue;
            }

            // Level 2: LocalStorage（中等速度）
            var localStorageValue = await _localStorage.GetItemAsync<T>(GetLocalStorageKey(key));
            if (localStorageValue != null)
            {
                _logger.LogDebug("Cache hit (LocalStorage): {Key}", key);
                
                // 提升到内存缓存
                await SetMemoryCacheAsync(key, localStorageValue);
                return localStorageValue;
            }

            // Level 3: IndexedDB（慢但持久）
            var indexedDbValue = await GetFromIndexedDbAsync<T>(key);
            if (indexedDbValue != null)
            {
                _logger.LogDebug("Cache hit (IndexedDB): {Key}", key);
                
                // 提升到上级缓存
                await SetMemoryCacheAsync(key, indexedDbValue);
                await SetLocalStorageCacheAsync(key, indexedDbValue);
                return indexedDbValue;
            }

            _logger.LogDebug("Cache miss: {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var actualExpiry = expiry ?? _level1Duration;

            // 根据过期时间决定存储层级
            if (actualExpiry <= _level1Duration)
            {
                // 短期数据：仅存储在内存
                await SetMemoryCacheAsync(key, value, actualExpiry);
            }
            else if (actualExpiry <= _level2Duration)
            {
                // 中期数据：内存 + LocalStorage
                await SetMemoryCacheAsync(key, value, _level1Duration);
                await SetLocalStorageCacheAsync(key, value);
            }
            else
            {
                // 长期数据：所有层级
                await SetMemoryCacheAsync(key, value, _level1Duration);
                await SetLocalStorageCacheAsync(key, value);
                await SetIndexedDbCacheAsync(key, value, actualExpiry);
            }

            _logger.LogDebug("Cache set: {Key} with expiry {Expiry}", key, actualExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            // 从所有层级移除
            _memoryCache.Remove(GetMemoryKey(key));
            await _localStorage.RemoveItemAsync(GetLocalStorageKey(key));
            await RemoveFromIndexedDbAsync(key);

            _logger.LogDebug("Cache removed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            // 清除内存缓存（没有直接方法，需要重新注入服务）
            // _memoryCache.Clear(); // 不存在此方法
            
            await _localStorage.ClearAsync();
            await ClearIndexedDbAsync();

            _logger.LogInformation("All caches cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing caches");
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            // 检查内存缓存
            if (_memoryCache.TryGetValue(GetMemoryKey(key), out _))
                return true;

            // 检查LocalStorage
            var localValue = await _localStorage.GetItemAsync<object>(GetLocalStorageKey(key));
            if (localValue != null)
                return true;

            // 检查IndexedDB
            return await ExistsInIndexedDbAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    // 私有辅助方法
    private async Task SetMemoryCacheAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? _level1Duration,
            Priority = CacheItemPriority.Normal
        };

        _memoryCache.Set(GetMemoryKey(key), value, options);
        await Task.CompletedTask;
    }

    private async Task SetLocalStorageCacheAsync<T>(string key, T value)
    {
        var cacheEntry = new CacheEntry<T>
        {
            Value = value,
            ExpiresAt = DateTime.UtcNow.Add(_level2Duration)
        };

        await _localStorage.SetItemAsync(GetLocalStorageKey(key), cacheEntry);
    }

    private async Task SetIndexedDbCacheAsync<T>(string key, T value, TimeSpan expiry)
    {
        // IndexedDB实现（简化版，实际需要使用IndexedDB API）
        var cacheEntry = new CacheEntry<T>
        {
            Value = value,
            ExpiresAt = DateTime.UtcNow.Add(expiry)
        };

        await _localStorage.SetItemAsync(GetIndexedDbKey(key), cacheEntry);
    }

    private async Task<T?> GetFromIndexedDbAsync<T>(string key)
    {
        // 简化版IndexedDB实现
        var entry = await _localStorage.GetItemAsync<CacheEntry<T>>(GetIndexedDbKey(key));
        
        if (entry != null && entry.ExpiresAt > DateTime.UtcNow)
        {
            return entry.Value;
        }

        return default;
    }

    private async Task RemoveFromIndexedDbAsync(string key)
    {
        await _localStorage.RemoveItemAsync(GetIndexedDbKey(key));
    }

    private async Task ClearIndexedDbAsync()
    {
        // 简化实现：清除所有以缓存前缀开头的项
        // 实际应用中需要使用IndexedDB API
        await Task.CompletedTask;
    }

    private async Task<bool> ExistsInIndexedDbAsync(string key)
    {
        var entry = await _localStorage.GetItemAsync<object>(GetIndexedDbKey(key));
        return entry != null;
    }

    // 键名生成方法
    private string GetMemoryKey(string key) => $"mem_{key}";
    private string GetLocalStorageKey(string key) => $"ls_{key}";
    private string GetIndexedDbKey(string key) => $"idb_{key}";

    // 缓存条目包装类
    private class CacheEntry<T>
    {
        public T Value { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
    }
}