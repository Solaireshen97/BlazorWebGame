using BlazorWebGame.Server.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Runtime;

namespace BlazorWebGame.Server.Services.System;

/// <summary>
/// 服务器优化服务，提供自动优化和缓存管理
/// </summary>
public class ServerOptimizationService : BackgroundService
{
    private readonly ILogger<ServerOptimizationService> _logger;
    private readonly GameServerOptions _options;
    private readonly PerformanceMonitoringService _performanceService;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly Timer _cleanupTimer;
    private readonly Timer _optimizationTimer;

    public ServerOptimizationService(
        ILogger<ServerOptimizationService> logger,
        IOptions<GameServerOptions> options,
        PerformanceMonitoringService performanceService)
    {
        _logger = logger;
        _options = options.Value;
        _performanceService = performanceService;
        
        // 每5分钟清理过期缓存
        _cleanupTimer = new Timer(CleanupExpiredCache, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        
        // 每10分钟执行优化检查
        _optimizationTimer = new Timer(PerformOptimizations, null, 
            TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Server optimization service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 每小时执行一次深度优化
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                
                if (!stoppingToken.IsCancellationRequested)
                {
                    await PerformDeepOptimization();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in optimization service background task");
            }
        }
    }

    /// <summary>
    /// 获取缓存数据
    /// </summary>
    public T? GetCached<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            entry.LastAccessed = DateTime.UtcNow;
            entry.AccessCount++;
            return entry.Value as T;
        }
        
        return null;
    }

    /// <summary>
    /// 设置缓存数据
    /// </summary>
    public void SetCache<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var actualExpiration = expiration ?? TimeSpan.FromMinutes(30);
        
        _cache.AddOrUpdate(key, 
            new CacheEntry
            {
                Value = value,
                ExpiresAt = DateTime.UtcNow.Add(actualExpiration),
                LastAccessed = DateTime.UtcNow,
                AccessCount = 1
            },
            (k, old) =>
            {
                old.Value = value;
                old.ExpiresAt = DateTime.UtcNow.Add(actualExpiration);
                old.LastAccessed = DateTime.UtcNow;
                return old;
            });
    }

    /// <summary>
    /// 移除缓存项
    /// </summary>
    public bool RemoveFromCache(string key)
    {
        return _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public CacheStatistics GetCacheStatistics()
    {
        var entries = _cache.Values.ToList();
        var now = DateTime.UtcNow;
        
        return new CacheStatistics
        {
            TotalEntries = entries.Count,
            ExpiredEntries = entries.Count(e => e.IsExpired),
            TotalAccessCount = entries.Sum(e => e.AccessCount),
            AverageAccessCount = entries.Any() ? entries.Average(e => e.AccessCount) : 0,
            OldestEntry = entries.Any() ? now - entries.Min(e => e.LastAccessed) : TimeSpan.Zero,
            NewestEntry = entries.Any() ? now - entries.Max(e => e.LastAccessed) : TimeSpan.Zero
        };
    }

    /// <summary>
    /// 清理过期缓存
    /// </summary>
    private void CleanupExpiredCache(object? state)
    {
        try
        {
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
    }

    /// <summary>
    /// 执行优化操作
    /// </summary>
    private void PerformOptimizations(object? state)
    {
        try
        {
            var systemMetrics = _performanceService.GetSystemMetrics();
            
            // 如果内存使用超过阈值，执行垃圾回收
            if (systemMetrics.MemoryUsageMB > 1024) // 1GB 阈值
            {
                _logger.LogInformation("Memory usage is high ({MemoryMB}MB), performing garbage collection", 
                    systemMetrics.MemoryUsageMB);
                
                GC.Collect(1, GCCollectionMode.Optimized);
                
                // 记录垃圾回收后的内存使用
                var newMetrics = _performanceService.GetSystemMetrics();
                _logger.LogInformation("Garbage collection completed. Memory reduced from {OldMB}MB to {NewMB}MB", 
                    systemMetrics.MemoryUsageMB, newMetrics.MemoryUsageMB);
            }

            // 清理不常用的缓存项
            CleanupUnusedCache();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during performance optimizations");
        }
    }

    /// <summary>
    /// 清理不常使用的缓存项
    /// </summary>
    private void CleanupUnusedCache()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-2); // 2小时未访问
        var unusedKeys = _cache
            .Where(kvp => kvp.Value.LastAccessed < cutoffTime && kvp.Value.AccessCount < 5)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in unusedKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (unusedKeys.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} unused cache entries", unusedKeys.Count);
        }
    }

    /// <summary>
    /// 执行深度优化
    /// </summary>
    private async Task PerformDeepOptimization()
    {
        try
        {
            _logger.LogInformation("Starting deep optimization routine");

            // 执行完整垃圾回收
            var beforeMemory = GC.GetTotalMemory(false);
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var afterMemory = GC.GetTotalMemory(false);
            var memoryFreed = (beforeMemory - afterMemory) / 1024 / 1024;
            
            if (memoryFreed > 0)
            {
                _logger.LogInformation("Deep optimization freed {MemoryMB}MB of memory", memoryFreed);
            }

            // 压缩大对象堆
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();

            // 清理所有过期缓存
            CleanupExpiredCache(null);

            _logger.LogInformation("Deep optimization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during deep optimization");
        }
    }

    public override void Dispose()
    {
        _cleanupTimer?.Dispose();
        _optimizationTimer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// 缓存条目
/// </summary>
public class CacheEntry
{
    public object? Value { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime LastAccessed { get; set; }
    public long AccessCount { get; set; }
    
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

/// <summary>
/// 缓存统计信息
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public long TotalAccessCount { get; set; }
    public double AverageAccessCount { get; set; }
    public TimeSpan OldestEntry { get; set; }
    public TimeSpan NewestEntry { get; set; }
}
