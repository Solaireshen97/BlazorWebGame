using BlazorWebGame.Refactored.Application.Interfaces;
using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BlazorWebGame.Refactored.Infrastructure.Persistence;

/// <summary>
/// IndexedDB数据持久化实现
/// 注意：这是简化版实现，实际应用中需要使用JavaScript互操作调用IndexedDB API
/// </summary>
public class IndexedDbRepository : IDataPersistenceService
{
    private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
    private readonly ILogger<IndexedDbRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string DB_PREFIX = "gamedb_";

    public IndexedDbRepository(Blazored.LocalStorage.ILocalStorageService localStorage, ILogger<IndexedDbRepository> logger)
    {
        _localStorage = localStorage;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync<T>(string collection, string id) where T : class
    {
        try
        {
            var key = GenerateKey(collection, id);
            var result = await _localStorage.GetItemAsync<T>(key);
            
            if (result != null)
            {
                _logger.LogDebug("Retrieved {Type} with ID {Id} from {Collection}", typeof(T).Name, id, collection);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {Type} with ID {Id} from {Collection}", typeof(T).Name, id, collection);
            return null;
        }
    }

    public async Task<List<T>> GetAllAsync<T>(string collection) where T : class
    {
        try
        {
            // 简化实现：使用LocalStorage模拟
            // 实际应用中需要使用IndexedDB的查询功能
            var results = new List<T>();
            var keys = await GetCollectionKeysAsync(collection);
            
            foreach (var key in keys)
            {
                var item = await _localStorage.GetItemAsync<T>(key);
                if (item != null)
                {
                    results.Add(item);
                }
            }

            _logger.LogDebug("Retrieved {Count} {Type} items from {Collection}", results.Count, typeof(T).Name, collection);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all {Type} from {Collection}", typeof(T).Name, collection);
            return new List<T>();
        }
    }

    public async Task<bool> SaveAsync<T>(string collection, string id, T entity) where T : class
    {
        try
        {
            var key = GenerateKey(collection, id);
            await _localStorage.SetItemAsync(key, entity);
            
            // 更新集合索引
            await AddToCollectionIndexAsync(collection, id);
            
            _logger.LogDebug("Saved {Type} with ID {Id} to {Collection}", typeof(T).Name, id, collection);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving {Type} with ID {Id} to {Collection}", typeof(T).Name, id, collection);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string collection, string id)
    {
        try
        {
            var key = GenerateKey(collection, id);
            await _localStorage.RemoveItemAsync(key);
            
            // 从集合索引中移除
            await RemoveFromCollectionIndexAsync(collection, id);
            
            _logger.LogDebug("Deleted item with ID {Id} from {Collection}", id, collection);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item with ID {Id} from {Collection}", id, collection);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string collection, string id)
    {
        try
        {
            var key = GenerateKey(collection, id);
            var item = await _localStorage.GetItemAsync<object>(key);
            return item != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of item with ID {Id} in {Collection}", id, collection);
            return false;
        }
    }

    public async Task<List<T>> QueryAsync<T>(string collection, Func<T, bool> predicate) where T : class
    {
        try
        {
            var allItems = await GetAllAsync<T>(collection);
            var filteredItems = allItems.Where(predicate).ToList();
            
            _logger.LogDebug("Query returned {Count} {Type} items from {Collection}", filteredItems.Count, typeof(T).Name, collection);
            return filteredItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying {Type} from {Collection}", typeof(T).Name, collection);
            return new List<T>();
        }
    }

    public async Task<int> CountAsync(string collection)
    {
        try
        {
            var keys = await GetCollectionKeysAsync(collection);
            return keys.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting items in {Collection}", collection);
            return 0;
        }
    }

    public async Task ClearCollectionAsync(string collection)
    {
        try
        {
            var keys = await GetCollectionKeysAsync(collection);
            
            foreach (var key in keys)
            {
                await _localStorage.RemoveItemAsync(key);
            }

            // 清除集合索引
            await ClearCollectionIndexAsync(collection);
            
            _logger.LogInformation("Cleared all items from {Collection}", collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing {Collection}", collection);
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            // 初始化数据库结构
            _logger.LogInformation("IndexedDB repository initialized");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing IndexedDB repository");
        }
    }

    // 私有辅助方法
    private string GenerateKey(string collection, string id) => $"{DB_PREFIX}{collection}_{id}";
    private string GenerateCollectionIndexKey(string collection) => $"{DB_PREFIX}index_{collection}";

    private async Task<List<string>> GetCollectionKeysAsync(string collection)
    {
        try
        {
            var indexKey = GenerateCollectionIndexKey(collection);
            var index = await _localStorage.GetItemAsync<List<string>>(indexKey);
            return index ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private async Task AddToCollectionIndexAsync(string collection, string id)
    {
        try
        {
            var indexKey = GenerateCollectionIndexKey(collection);
            var index = await GetCollectionKeysAsync(collection);
            var key = GenerateKey(collection, id);
            
            if (!index.Contains(key))
            {
                index.Add(key);
                await _localStorage.SetItemAsync(indexKey, index);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection index for {Collection}", collection);
        }
    }

    private async Task RemoveFromCollectionIndexAsync(string collection, string id)
    {
        try
        {
            var indexKey = GenerateCollectionIndexKey(collection);
            var index = await GetCollectionKeysAsync(collection);
            var key = GenerateKey(collection, id);
            
            if (index.Remove(key))
            {
                await _localStorage.SetItemAsync(indexKey, index);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from collection index for {Collection}", collection);
        }
    }

    private async Task ClearCollectionIndexAsync(string collection)
    {
        try
        {
            var indexKey = GenerateCollectionIndexKey(collection);
            await _localStorage.RemoveItemAsync(indexKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing collection index for {Collection}", collection);
        }
    }
}

// 数据持久化服务接口
public interface IDataPersistenceService
{
    Task<T?> GetAsync<T>(string collection, string id) where T : class;
    Task<List<T>> GetAllAsync<T>(string collection) where T : class;
    Task<bool> SaveAsync<T>(string collection, string id, T entity) where T : class;
    Task<bool> DeleteAsync(string collection, string id);
    Task<bool> ExistsAsync(string collection, string id);
    Task<List<T>> QueryAsync<T>(string collection, Func<T, bool> predicate) where T : class;
    Task<int> CountAsync(string collection);
    Task ClearCollectionAsync(string collection);
    Task InitializeAsync();
}