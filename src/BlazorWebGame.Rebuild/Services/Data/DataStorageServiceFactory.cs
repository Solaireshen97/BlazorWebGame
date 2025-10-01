using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Rebuild.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorWebGame.Rebuild.Services.Data;

/// <summary>
/// 数据存储服务工厂实现
/// </summary>
public class DataStorageServiceFactory : IDataStorageServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataStorageServiceFactory> _logger;
    private readonly IDbContextFactory<GameDbContext>? _contextFactory;

    private static readonly HashSet<string> SupportedStorageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Memory",
        "SQLite"
    };

    public DataStorageServiceFactory(
        IServiceProvider serviceProvider, 
        ILogger<DataStorageServiceFactory> logger,
        IDbContextFactory<GameDbContext>? contextFactory = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// 创建数据存储服务实例
    /// </summary>
    /// <param name="storageType">存储类型：Memory, SQLite</param>
    /// <returns>数据存储服务实例</returns>
    public IDataStorageService CreateDataStorageService(string storageType)
    {
        if (string.IsNullOrWhiteSpace(storageType))
        {
            _logger.LogWarning("Storage type is null or empty, defaulting to Memory");
            storageType = "Memory";
        }

        if (!IsStorageTypeSupported(storageType))
        {
            _logger.LogError("Unsupported storage type: {StorageType}. Supported types: {SupportedTypes}", 
                storageType, string.Join(", ", SupportedStorageTypes));
            throw new ArgumentException($"不支持的存储类型: {storageType}", nameof(storageType));
        }

        try
        {
            return storageType.ToUpperInvariant() switch
            {
                "MEMORY" => CreateMemoryDataStorageService(),
                "SQLITE" => CreateSqliteDataStorageService(),
                _ => throw new ArgumentException($"未知的存储类型: {storageType}", nameof(storageType))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create data storage service for type: {StorageType}", storageType);
            
            // 降级到内存存储作为备选方案
            _logger.LogWarning("Falling back to Memory storage due to error creating {StorageType} storage", storageType);
            return CreateMemoryDataStorageService();
        }
    }

    /// <summary>
    /// 获取支持的存储类型列表
    /// </summary>
    /// <returns>支持的存储类型</returns>
    public IEnumerable<string> GetSupportedStorageTypes()
    {
        return SupportedStorageTypes.ToList();
    }

    /// <summary>
    /// 验证存储类型是否支持
    /// </summary>
    /// <param name="storageType">存储类型</param>
    /// <returns>是否支持</returns>
    public bool IsStorageTypeSupported(string storageType)
    {
        return !string.IsNullOrWhiteSpace(storageType) && 
               SupportedStorageTypes.Contains(storageType);
    }

    /// <summary>
    /// 创建内存数据存储服务
    /// </summary>
    private IDataStorageService CreateMemoryDataStorageService()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<DataStorageService>>();
        var service = new DataStorageService(logger);
        
        _logger.LogInformation("Created Memory data storage service");
        return service;
    }

    /// <summary>
    /// 创建SQLite数据存储服务
    /// </summary>
    private IDataStorageService CreateSqliteDataStorageService()
    {
        if (_contextFactory == null)
        {
            _logger.LogError("DbContextFactory is null, cannot create SQLite data storage service");
            throw new InvalidOperationException("DbContextFactory未配置，无法创建SQLite数据存储服务");
        }

        var logger = _serviceProvider.GetRequiredService<ILogger<SqliteDataStorageService>>();
        var service = new SqliteDataStorageService(_contextFactory, logger);
        
        _logger.LogInformation("Created SQLite data storage service");
        return service;
    }

    /// <summary>
    /// 验证SQLite数据库连接
    /// </summary>
    /// <returns>是否可以连接到数据库</returns>
    public async Task<bool> ValidateSqliteDatabaseAsync()
    {
        if (_contextFactory == null)
        {
            _logger.LogWarning("DbContextFactory is null, cannot validate SQLite database");
            return false;
        }

        try
        {
            using var context = _contextFactory.CreateDbContext();
            var canConnect = await context.Database.CanConnectAsync();
            
            if (canConnect)
            {
                // 确保数据库存在并且架构是最新的
                await context.Database.EnsureCreatedAsync();
                _logger.LogInformation("SQLite database validation successful");
            }
            else
            {
                _logger.LogWarning("Cannot connect to SQLite database");
            }
            
            return canConnect;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate SQLite database");
            return false;
        }
    }

    /// <summary>
    /// 获取数据存储服务的健康状态
    /// </summary>
    /// <param name="storageType">存储类型</param>
    /// <returns>健康状态信息</returns>
    public async Task<Dictionary<string, object>> GetStorageHealthAsync(string storageType)
    {
        var healthInfo = new Dictionary<string, object>
        {
            ["StorageType"] = storageType,
            ["Timestamp"] = DateTime.UtcNow,
            ["IsSupported"] = IsStorageTypeSupported(storageType)
        };

        if (!IsStorageTypeSupported(storageType))
        {
            healthInfo["Status"] = "Unsupported";
            healthInfo["Message"] = $"存储类型 {storageType} 不被支持";
            return healthInfo;
        }

        try
        {
            var service = CreateDataStorageService(storageType);
            var healthCheck = await service.HealthCheckAsync();
            
            healthInfo["Status"] = healthCheck.Success ? "Healthy" : "Unhealthy";
            healthInfo["ServiceHealth"] = healthCheck.Data;
            healthInfo["Message"] = healthCheck.Message;
        }
        catch (Exception ex)
        {
            healthInfo["Status"] = "Error";
            healthInfo["Error"] = ex.Message;
            healthInfo["Message"] = $"创建或检查存储服务时发生错误: {ex.Message}";
            _logger.LogError(ex, "Error getting storage health for type: {StorageType}", storageType);
        }

        return healthInfo;
    }
}