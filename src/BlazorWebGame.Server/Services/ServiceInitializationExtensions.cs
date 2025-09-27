namespace BlazorWebGame.Server.Services;

/// <summary>
/// 服务初始化扩展方法 - 用于配置服务间的依赖关系和缓存集成
/// </summary>
public static class ServiceInitializationExtensions
{
    /// <summary>
    /// 初始化服务集成和缓存
    /// </summary>
    public static async Task InitializeServicesAsync(this IServiceProvider services, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting service initialization...");

            // 获取核心服务
            var gameEngine = services.GetRequiredService<GameEngineService>();
            var productionService = services.GetRequiredService<ServerProductionService>();
            
            // 尝试获取 Redis 缓存服务（可能不可用）
            var redisCache = services.GetService<RedisGameCacheService>();
            
            if (redisCache != null)
            {
                // 集成 Redis 缓存到游戏引擎
                gameEngine.SetRedisCache(redisCache);
                logger.LogInformation("Redis cache integrated with GameEngineService");

                // 预热缓存
                await gameEngine.WarmupBattleCacheAsync();
                await productionService.WarmupCacheAsync();
                
                logger.LogInformation("Cache warmup completed");
            }
            else
            {
                logger.LogWarning("Redis cache service not available, running without distributed cache");
            }

            logger.LogInformation("Service initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during service initialization");
            throw;
        }
    }

    /// <summary>
    /// 获取服务健康状态
    /// </summary>
    public static async Task<Dictionary<string, object>> GetServiceHealthAsync(this IServiceProvider services)
    {
        var healthStatus = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow,
            ["services"] = new Dictionary<string, object>()
        };

        var servicesHealth = (Dictionary<string, object>)healthStatus["services"];

        try
        {
            // 检查游戏引擎状态
            var gameEngine = services.GetRequiredService<GameEngineService>();
            var cacheStats = await gameEngine.GetCacheStatsAsync();
            
            servicesHealth["gameEngine"] = new
            {
                status = "healthy",
                activeBattles = cacheStats.TotalActiveBattles,
                redisEnabled = cacheStats.RedisEnabled,
                cacheError = cacheStats.HasError ? cacheStats.ErrorMessage : null
            };
        }
        catch (Exception ex)
        {
            servicesHealth["gameEngine"] = new
            {
                status = "unhealthy",
                error = ex.Message
            };
        }

        try
        {
            // 检查生产服务状态
            var productionService = services.GetRequiredService<ServerProductionService>();
            var productionStats = await productionService.GetSystemStatsAsync();
            
            servicesHealth["production"] = new
            {
                status = "healthy",
                activeGathering = productionStats.ActiveGatheringCount,
                activeCrafting = productionStats.ActiveCraftingCount,
                totalNodes = productionStats.TotalNodes,
                totalRecipes = productionStats.TotalRecipes
            };
        }
        catch (Exception ex)
        {
            servicesHealth["production"] = new
            {
                status = "unhealthy",
                error = ex.Message
            };
        }

        try
        {
            // 检查 Redis 缓存状态
            var redisCache = services.GetService<RedisGameCacheService>();
            if (redisCache != null)
            {
                var cacheStats = await redisCache.GetCacheStatisticsAsync();
                servicesHealth["redis"] = new
                {
                    status = "healthy",
                    totalKeys = cacheStats.TotalKeys,
                    memoryUsage = cacheStats.MemoryUsage,
                    keyCounts = cacheStats.KeyCounts
                };
            }
            else
            {
                servicesHealth["redis"] = new
                {
                    status = "unavailable",
                    reason = "Redis service not configured"
                };
            }
        }
        catch (Exception ex)
        {
            servicesHealth["redis"] = new
            {
                status = "unhealthy",
                error = ex.Message
            };
        }

        return healthStatus;
    }

    /// <summary>
    /// 执行服务清理
    /// </summary>
    public static async Task CleanupServicesAsync(this IServiceProvider services, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting service cleanup...");

            // 清理游戏引擎缓存
            var gameEngine = services.GetService<GameEngineService>();
            if (gameEngine != null)
            {
                await gameEngine.CleanupCompletedBattleCacheAsync();
                logger.LogDebug("GameEngine cache cleaned up");
            }

            // 清理 Redis 过期键（如果可用）
            var redisCache = services.GetService<RedisGameCacheService>();
            if (redisCache != null)
            {
                // 清理超过1小时的战斗缓存
                await redisCache.RemoveByPatternAsync("battle:*");
                logger.LogDebug("Redis expired keys cleaned up");
            }

            logger.LogInformation("Service cleanup completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during service cleanup");
        }
    }

    /// <summary>
    /// 获取服务性能指标
    /// </summary>
    public static async Task<Dictionary<string, object>> GetServiceMetricsAsync(this IServiceProvider services)
    {
        var metrics = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow,
            ["metrics"] = new Dictionary<string, object>()
        };

        var serviceMetrics = (Dictionary<string, object>)metrics["metrics"];

        try
        {
            // 游戏引擎指标
            var gameEngine = services.GetRequiredService<GameEngineService>();
            var engineStats = await gameEngine.GetCacheStatsAsync();
            
            serviceMetrics["gameEngine"] = new
            {
                activeBattles = engineStats.TotalActiveBattles,
                serverContexts = engineStats.TotalServerContexts,
                cachedBattles = engineStats.CachedBattles,
                cacheMemoryUsage = engineStats.CacheMemoryUsage
            };

            // 生产系统指标
            var productionService = services.GetRequiredService<ServerProductionService>();
            var productionStats = await productionService.GetSystemStatsAsync();
            
            serviceMetrics["production"] = new
            {
                activeGathering = productionStats.ActiveGatheringCount,
                activeCrafting = productionStats.ActiveCraftingCount,
                nodesPerProfession = productionStats.NodesPerProfession
            };

            // Redis 指标
            var redisCache = services.GetService<RedisGameCacheService>();
            if (redisCache != null)
            {
                var cacheStats = await redisCache.GetCacheStatisticsAsync();
                serviceMetrics["redis"] = cacheStats;
            }

        }
        catch (Exception ex)
        {
            serviceMetrics["error"] = ex.Message;
        }

        return metrics;
    }
}