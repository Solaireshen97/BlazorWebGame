using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using BlazorWebGame.Server.Hubs;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// GameEngineService 缓存优化扩展 - 集成 Redis 缓存功能
/// </summary>
public partial class GameEngineService
{
    private RedisGameCacheService? _redisCache;

    /// <summary>
    /// 设置 Redis 缓存服务（依赖注入后调用）
    /// </summary>
    public void SetRedisCache(RedisGameCacheService redisCache)
    {
        _redisCache = redisCache;
        _logger.LogInformation("Redis cache service integrated with GameEngineService");
    }

    /// <summary>
    /// 异步获取战斗状态（带缓存）
    /// </summary>
    public async Task<BattleStateDto?> GetBattleStateAsync(Guid battleId)
    {
        try
        {
            // 首先尝试从缓存获取
            BattleStateDto? cachedBattle = null;
            if (_redisCache != null)
            {
                cachedBattle = await _redisCache.GetBattleStateAsync(battleId);
                if (cachedBattle != null)
                {
                    _logger.LogDebug("Battle {BattleId} loaded from Redis cache", battleId);
                    return cachedBattle;
                }
            }

            // 缓存未命中，从内存获取
            var battle = GetBattleState(battleId);
            
            // 更新缓存
            if (battle != null && _redisCache != null)
            {
                await _redisCache.SetBattleStateAsync(battleId, battle);
                _logger.LogDebug("Battle {BattleId} cached to Redis", battleId);
            }

            return battle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting battle state {BattleId} with cache", battleId);
            // 降级到同步方法
            return GetBattleState(battleId);
        }
    }

    /// <summary>
    /// 异步开始战斗（带缓存）
    /// </summary>
    public async Task<BattleStateDto> StartBattleAsync(StartBattleRequest request)
    {
        var battle = StartBattle(request);
        
        // 缓存新创建的战斗状态
        if (_redisCache != null)
        {
            try
            {
                await _redisCache.SetBattleStateAsync(battle.BattleId, battle);
                _logger.LogDebug("New battle {BattleId} cached to Redis", battle.BattleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching new battle {BattleId}", battle.BattleId);
            }
        }

        return battle;
    }

    /// <summary>
    /// 异步停止战斗（清理缓存）
    /// </summary>
    public async Task<bool> StopBattleAsync(Guid battleId)
    {
        var success = StopBattle(battleId);
        
        // 清理缓存
        if (success && _redisCache != null)
        {
            try
            {
                await _redisCache.RemoveBattleStateAsync(battleId);
                _logger.LogDebug("Battle {BattleId} removed from Redis cache", battleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing battle {BattleId} from cache", battleId);
            }
        }

        return success;
    }

    /// <summary>
    /// 批量更新战斗状态缓存
    /// </summary>
    public async Task UpdateBattleCacheAsync()
    {
        if (_redisCache == null) return;

        try
        {
            var activeBattles = GetAllBattleUpdates();
            var updateTasks = activeBattles.Select(async battle =>
            {
                try
                {
                    await _redisCache.SetBattleStateAsync(battle.BattleId, battle);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating cache for battle {BattleId}", battle.BattleId);
                }
            });

            await Task.WhenAll(updateTasks);
            _logger.LogDebug("Updated {Count} battle states in Redis cache", activeBattles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch battle cache update");
        }
    }

    /// <summary>
    /// 获取战斗状态并更新缓存（内部使用）
    /// </summary>
    private async Task<BattleStateDto?> RefreshBattleCacheAsync(Guid battleId)
    {
        var battle = GetBattleState(battleId);
        
        if (battle != null && _redisCache != null)
        {
            try
            {
                await _redisCache.SetBattleStateAsync(battleId, battle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing cache for battle {BattleId}", battleId);
            }
        }

        return battle;
    }

    /// <summary>
    /// 清理已完成战斗的缓存
    /// </summary>
    public async Task CleanupCompletedBattleCacheAsync()
    {
        if (_redisCache == null) return;

        try
        {
            var completedBattles = _activeBattles.Values
                .Where(b => !b.IsActive || b.Status == BattleStatus.Completed)
                .ToList();

            var cleanupTasks = completedBattles.Select(async battle =>
            {
                try
                {
                    await _redisCache.RemoveBattleStateAsync(battle.BattleId);
                    _logger.LogDebug("Cleaned up cache for completed battle {BattleId}", battle.BattleId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up cache for battle {BattleId}", battle.BattleId);
                }
            });

            await Task.WhenAll(cleanupTasks);
            _logger.LogInformation("Cleaned up {Count} completed battles from cache", completedBattles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in battle cache cleanup");
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public async Task<GameEngineCacheStats> GetCacheStatsAsync()
    {
        if (_redisCache == null)
        {
            return new GameEngineCacheStats
            {
                RedisEnabled = false,
                Timestamp = DateTime.UtcNow,
                TotalActiveBattles = _activeBattles.Count,
                TotalServerContexts = _serverBattleContexts.Count
            };
        }

        try
        {
            var cacheStats = await _redisCache.GetCacheStatisticsAsync();
            
            return new GameEngineCacheStats
            {
                RedisEnabled = true,
                Timestamp = DateTime.UtcNow,
                TotalActiveBattles = _activeBattles.Count,
                TotalServerContexts = _serverBattleContexts.Count,
                CachedBattles = cacheStats.KeyCounts.GetValueOrDefault("battles", 0),
                TotalCacheKeys = (int)cacheStats.TotalKeys,
                CacheMemoryUsage = cacheStats.MemoryUsage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new GameEngineCacheStats
            {
                RedisEnabled = true,
                HasError = true,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow,
                TotalActiveBattles = _activeBattles.Count,
                TotalServerContexts = _serverBattleContexts.Count
            };
        }
    }

    /// <summary>
    /// 预热缓存 - 将活跃战斗加载到缓存中
    /// </summary>
    public async Task WarmupBattleCacheAsync()
    {
        if (_redisCache == null)
        {
            _logger.LogInformation("Redis cache not available, skipping warmup");
            return;
        }

        try
        {
            _logger.LogInformation("Starting battle cache warmup");
            
            var activeBattles = _activeBattles.Values.Where(b => b.IsActive).ToList();
            var warmupTasks = activeBattles.Select(async battle =>
            {
                try
                {
                    await _redisCache.SetBattleStateAsync(battle.BattleId, battle);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error warming up cache for battle {BattleId}", battle.BattleId);
                }
            });

            await Task.WhenAll(warmupTasks);
            _logger.LogInformation("Battle cache warmup completed: {Count} battles cached", activeBattles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during battle cache warmup");
        }
    }

    /// <summary>
    /// 异步处理战斗Tick时同时更新缓存
    /// </summary>
    public async Task ProcessBattleTickWithCacheAsync(double deltaTime)
    {
        // 首先执行正常的战斗处理
        await ProcessBattleTickAsync(deltaTime);
        
        // 然后异步更新缓存（不阻塞游戏循环）
        _ = Task.Run(async () =>
        {
            try
            {
                await UpdateBattleCacheAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating battle cache during tick");
            }
        });
    }
}

/// <summary>
/// 游戏引擎缓存统计信息
/// </summary>
public class GameEngineCacheStats
{
    public bool RedisEnabled { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
    public int TotalActiveBattles { get; set; }
    public int TotalServerContexts { get; set; }
    public int CachedBattles { get; set; }
    public int TotalCacheKeys { get; set; }
    public long CacheMemoryUsage { get; set; }
}