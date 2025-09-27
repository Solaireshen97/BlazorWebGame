using Microsoft.Extensions.Caching.Memory;
using BlazorWebGame.Refactored.Domain.ValueObjects;
using BlazorWebGame.Refactored.Application.Interfaces;

namespace BlazorWebGame.Refactored.Infrastructure.Services;

/// <summary>
/// 带缓存的数据服务
/// </summary>
public class CachedDataService : ICachedDataService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedDataService> _logger;
    
    public CachedDataService(
        IMemoryCache cache,
        ILogger<CachedDataService> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null) where T : class
    {
        if (_cache.TryGetValue<T>(key, out var cached))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cached;
        }
        
        _logger.LogDebug("Cache miss for key: {Key}", key);
        
        try
        {
            var data = await factory();
            
            if (data != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(1),
                    Priority = CacheItemPriority.Normal
                };
                
                _cache.Set(key, data, cacheOptions);
                _logger.LogDebug("Data cached for key: {Key}", key);
            }
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cached data for key: {Key}", key);
            return null;
        }
    }
    
    public async Task<List<BlazorWebGame.Refactored.Domain.ValueObjects.Recipe>> GetRecipesAsync(string profession)
    {
        return await GetOrCreateAsync(
            $"recipes_{profession}",
            async () => await LoadRecipesAsync(profession),
            TimeSpan.FromHours(1)
        ) ?? new List<BlazorWebGame.Refactored.Domain.ValueObjects.Recipe>();
    }
    
    public async Task<List<BlazorWebGame.Refactored.Domain.ValueObjects.Recipe>> GetRecipesByLevelAsync(string profession, int level)
    {
        return await GetOrCreateAsync(
            $"recipes_{profession}_{level}",
            async () => await LoadRecipesByLevelAsync(profession, level),
            TimeSpan.FromMinutes(30)
        ) ?? new List<BlazorWebGame.Refactored.Domain.ValueObjects.Recipe>();
    }
    
    public async Task<Dictionary<string, int>> GetMaterialPricesAsync()
    {
        return await GetOrCreateAsync(
            "material_prices",
            async () => await LoadMaterialPricesAsync(),
            TimeSpan.FromMinutes(15)
        ) ?? new Dictionary<string, int>();
    }
    
    public async Task<List<ItemReward>> GetLootTableAsync(string source)
    {
        return await GetOrCreateAsync(
            $"loot_table_{source}",
            async () => await LoadLootTableAsync(source),
            TimeSpan.FromHours(2)
        ) ?? new List<ItemReward>();
    }
    
    public void InvalidateCache(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Cache invalidated for key: {Key}", key);
    }
    
    public void InvalidateCacheByPattern(string pattern)
    {
        // 注意：MemoryCache 没有内置的模式匹配删除功能
        // 这里需要一个更复杂的实现来跟踪所有的键
        _logger.LogWarning("Pattern invalidation not fully implemented for pattern: {Pattern}", pattern);
    }
    
    public void ClearCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0); // 压缩缓存，移除所有过期项
        }
        _logger.LogInformation("Cache cleared");
    }
    
    // 多级缓存支持
    public async Task<T?> GetOrCreateWithTieredCacheAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? memoryExpiration = null,
        TimeSpan? persistentExpiration = null) where T : class
    {
        // 首先检查内存缓存
        if (_cache.TryGetValue<T>(key, out var memoryData))
        {
            _logger.LogDebug("Memory cache hit for key: {Key}", key);
            return memoryData;
        }
        
        // 然后检查持久化缓存（这里可以是 localStorage 或 IndexedDB）
        var persistentData = await GetFromPersistentCacheAsync<T>(key);
        if (persistentData != null)
        {
            _logger.LogDebug("Persistent cache hit for key: {Key}", key);
            
            // 将数据放回内存缓存
            var memoryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = memoryExpiration ?? TimeSpan.FromMinutes(5),
                SlidingExpiration = TimeSpan.FromMinutes(1),
                Priority = CacheItemPriority.Normal
            };
            _cache.Set(key, persistentData, memoryOptions);
            
            return persistentData;
        }
        
        // 最后从工厂方法获取数据
        _logger.LogDebug("All cache miss for key: {Key}, loading from source", key);
        
        try
        {
            var data = await factory();
            
            if (data != null)
            {
                // 设置内存缓存
                var memoryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = memoryExpiration ?? TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(1),
                    Priority = CacheItemPriority.Normal
                };
                _cache.Set(key, data, memoryOptions);
                
                // 设置持久化缓存
                await SetPersistentCacheAsync(key, data, persistentExpiration ?? TimeSpan.FromHours(24));
                
                _logger.LogDebug("Data cached in both tiers for key: {Key}", key);
            }
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tiered cached data for key: {Key}", key);
            return null;
        }
    }
    
    // 缓存统计
    public CacheStatistics GetCacheStatistics()
    {
        // 这里需要一个自定义的 MemoryCache 包装器来跟踪统计信息
        return new CacheStatistics
        {
            HitCount = 0, // 需要实现统计跟踪
            MissCount = 0,
            TotalRequests = 0,
            CacheSize = 0
        };
    }
    
    private async Task<List<BlazorWebGame.Refactored.Domain.ValueObjects.Recipe>> LoadRecipesAsync(string profession)
    {
        // 模拟加载配方数据
        await Task.Delay(100);
        
        return new List<BlazorWebGame.Refactored.Domain.ValueObjects.Recipe>
        {
            new BlazorWebGame.Refactored.Domain.ValueObjects.Recipe
            {
                RecipeId = $"{profession}_basic",
                Name = $"基础{profession}配方",
                RequiredMaterials = new List<MaterialRequirement>
                {
                    new("basic_material", 5)
                },
                OutputItem = new ItemReward($"{profession}_item", 1),
                CraftTime = 60,
                ExperienceReward = new BigNumber(50),
                RequiredLevel = 1
            }
        };
    }
    
    private async Task<List<BlazorWebGame.Refactored.Domain.ValueObjects.Recipe>> LoadRecipesByLevelAsync(string profession, int level)
    {
        var allRecipes = await LoadRecipesAsync(profession);
        return allRecipes.Where(r => r.RequiredLevel <= level).ToList();
    }
    
    private async Task<Dictionary<string, int>> LoadMaterialPricesAsync()
    {
        // 模拟加载材料价格
        await Task.Delay(50);
        
        return new Dictionary<string, int>
        {
            ["wood"] = 10,
            ["stone"] = 15,
            ["iron_ore"] = 25,
            ["cloth"] = 20,
            ["leather"] = 30
        };
    }
    
    private async Task<List<ItemReward>> LoadLootTableAsync(string source)
    {
        // 模拟加载掉落表
        await Task.Delay(75);
        
        return new List<ItemReward>
        {
            new("common_drop", 1, ItemRarity.Common),
            new("rare_drop", 1, ItemRarity.Rare)
        };
    }
    
    private async Task<T?> GetFromPersistentCacheAsync<T>(string key) where T : class
    {
        // 这里应该从持久化存储（如 localStorage）获取数据
        // 目前返回 null，表示没有持久化缓存
        await Task.CompletedTask;
        return null;
    }
    
    private async Task SetPersistentCacheAsync<T>(string key, T data, TimeSpan expiration) where T : class
    {
        // 这里应该将数据保存到持久化存储
        await Task.CompletedTask;
    }
}

/// <summary>
/// 缓存数据服务接口
/// </summary>
public interface ICachedDataService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;
    Task<List<BlazorWebGame.Refactored.Domain.ValueObjects.Recipe>> GetRecipesAsync(string profession);
    Task<List<BlazorWebGame.Refactored.Domain.ValueObjects.Recipe>> GetRecipesByLevelAsync(string profession, int level);
    Task<Dictionary<string, int>> GetMaterialPricesAsync();
    Task<List<ItemReward>> GetLootTableAsync(string source);
    void InvalidateCache(string key);
    void InvalidateCacheByPattern(string pattern);
    void ClearCache();
    Task<T?> GetOrCreateWithTieredCacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan? memoryExpiration = null, TimeSpan? persistentExpiration = null) where T : class;
    CacheStatistics GetCacheStatistics();
}

/// <summary>
/// 缓存统计信息
/// </summary>
public class CacheStatistics
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public long TotalRequests { get; set; }
    public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
    public long CacheSize { get; set; }
}