using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Configuration;

/// <summary>
/// 数据存储配置选项
/// </summary>
public class DataStorageOptions
{
    public const string SectionName = "DataStorage";
    
    /// <summary>
    /// 数据存储类型: InMemory, SQLite, Optimized
    /// </summary>
    public string StorageType { get; set; } = "Optimized";
    
    /// <summary>
    /// 自动保存间隔（秒）
    /// </summary>
    public int AutoSaveIntervalSeconds { get; set; } = 300;
    
    /// <summary>
    /// 数据库清理间隔（小时）
    /// </summary>
    public int DatabaseCleanupIntervalHours { get; set; } = 24;
    
    /// <summary>
    /// 数据保留天数
    /// </summary>
    public int DataRetentionDays { get; set; } = 30;
    
    /// <summary>
    /// 是否启用批量写入
    /// </summary>
    public bool EnableBatchWrites { get; set; } = true;
    
    /// <summary>
    /// 批量写入间隔（秒）
    /// </summary>
    public int BatchWriteIntervalSeconds { get; set; } = 5;
    
    /// <summary>
    /// 批量写入最大数量
    /// </summary>
    public int BatchWriteMaxItems { get; set; } = 100;
}

/// <summary>
/// 数据库优化选项
/// </summary>
public class DatabaseOptimizationOptions
{
    public const string SectionName = "DatabaseOptimization";
    
    /// <summary>
    /// 是否启用WAL模式
    /// </summary>
    public bool EnableWALMode { get; set; } = true;
    
    /// <summary>
    /// 缓存大小（KB）
    /// </summary>
    public int CacheSizeKB { get; set; } = 10240;
    
    /// <summary>
    /// 是否启用内存映射
    /// </summary>
    public bool EnableMemoryMapping { get; set; } = true;
    
    /// <summary>
    /// 内存映射大小（MB）
    /// </summary>
    public int MemoryMapSizeMB { get; set; } = 256;
    
    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 是否启用详细错误信息
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;
    
    /// <summary>
    /// 是否启用敏感数据日志
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;
}

/// <summary>
/// 数据存储配置扩展方法
/// </summary>
public static class DataStorageConfigurationExtensions
{
    /// <summary>
    /// 添加优化的数据存储服务
    /// </summary>
    public static IServiceCollection AddOptimizedDataStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // 绑定配置选项
        services.Configure<DataStorageOptions>(
            configuration.GetSection(DataStorageOptions.SectionName));
        services.Configure<DatabaseOptimizationOptions>(
            configuration.GetSection(DatabaseOptimizationOptions.SectionName));

        var dataStorageOptions = configuration.GetSection(DataStorageOptions.SectionName)
            .Get<DataStorageOptions>() ?? new DataStorageOptions();
        
        var dbOptimizationOptions = configuration.GetSection(DatabaseOptimizationOptions.SectionName)
            .Get<DatabaseOptimizationOptions>() ?? new DatabaseOptimizationOptions();

        // 配置数据库上下文工厂
        services.AddDbContextFactory<GameDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=gamedata.db;Cache=Shared";
            
            // 应用SQLite配置
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(dbOptimizationOptions.ConnectionTimeoutSeconds);
            });

            // 开发环境特殊配置
            if (environment.IsDevelopment())
            {
                if (dbOptimizationOptions.EnableDetailedErrors)
                {
                    options.EnableDetailedErrors();
                }
                
                if (dbOptimizationOptions.EnableSensitiveDataLogging)
                {
                    options.EnableSensitiveDataLogging();
                }
            }
        });

        // 同时注册OptimizedGameDbContext工厂（用于高级功能）
        services.AddDbContextFactory<OptimizedGameDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=gamedata.db;Cache=Shared";
            
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(dbOptimizationOptions.ConnectionTimeoutSeconds);
            });

            if (environment.IsDevelopment())
            {
                if (dbOptimizationOptions.EnableDetailedErrors)
                    options.EnableDetailedErrors();
                if (dbOptimizationOptions.EnableSensitiveDataLogging)
                    options.EnableSensitiveDataLogging();
            }
        });

        // 添加内存缓存（如果选择了优化存储）
        if (dataStorageOptions.StorageType.ToLower() == "optimized")
        {
            services.AddMemoryCache(cacheOptions =>
            {
                cacheOptions.SizeLimit = 1000; // 限制缓存项数量
                cacheOptions.CompactionPercentage = 0.25; // 压缩比例
            });
        }

        // 注册数据存储服务
        switch (dataStorageOptions.StorageType.ToLower())
        {
            case "sqlite":
                // 注意：SQLite服务需要Scoped生命周期，但当前架构使用Singleton
                // 这里我们回退到内存存储以避免依赖注入问题
                services.AddSingleton<IDataStorageService, DataStorageService>();
                break;
                
            case "optimized":
                // 推荐的优化实现：使用混合架构
                services.AddSingleton<IDataStorageService, OptimizedDataStorageService>();
                break;
                
            case "inmemory":
            default:
                // 默认内存存储
                services.AddSingleton<IDataStorageService, DataStorageService>();
                break;
        }

        // 注册数据库连接服务
        services.AddSingleton<DatabaseConnectionService>();
        
        // 注册数据存储集成服务（保持向后兼容）
        services.AddSingleton<DataStorageIntegrationService>();

        // 注册后台维护服务
        services.AddHostedService<DatabaseMaintenanceService>();

        return services;
    }

    /// <summary>
    /// 初始化数据存储系统
    /// </summary>
    public static async Task InitializeDataStorageAsync(
        this IServiceProvider serviceProvider,
        ILogger logger)
    {
        try
        {
            var dataStorageOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<DataStorageOptions>>()?.Value
                ?? new DataStorageOptions();

            logger.LogInformation("Initializing data storage system with type: {StorageType}", 
                dataStorageOptions.StorageType);

            // 初始化数据库（如果使用SQLite或优化存储）
            if (dataStorageOptions.StorageType.ToLower() is "sqlite" or "optimized")
            {
                using var scope = serviceProvider.CreateScope();
                
                // 使用常规GameDbContext进行初始化
                var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GameDbContext>>();
                using var context = await contextFactory.CreateDbContextAsync();
                
                await context.Database.EnsureCreatedAsync();
                
                // 如果有优化上下文，也进行初始化
                var optimizedContextFactory = scope.ServiceProvider.GetService<IDbContextFactory<OptimizedGameDbContext>>();
                if (optimizedContextFactory != null)
                {
                    using var optimizedContext = await optimizedContextFactory.CreateDbContextAsync();
                    await optimizedContext.EnsureDatabaseOptimizedAsync();
                }

                logger.LogInformation("Database initialization completed successfully");
            }

            // 验证数据存储服务
            var dataStorageService = serviceProvider.GetRequiredService<IDataStorageService>();
            var healthCheck = await dataStorageService.HealthCheckAsync();
            
            if (healthCheck.Success)
            {
                logger.LogInformation("Data storage health check passed: {Message}", healthCheck.Message);
            }
            else
            {
                logger.LogWarning("Data storage health check failed: {Message}", healthCheck.Message);
            }

            // 获取并记录统计信息
            var stats = await dataStorageService.GetStorageStatsAsync();
            if (stats.Success && stats.Data != null)
            {
                logger.LogInformation("Data storage statistics: {@Stats}", stats.Data);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize data storage system");
            throw;
        }
    }
}

/// <summary>
/// 数据库维护后台服务
/// </summary>
public class DatabaseMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMaintenanceService> _logger;
    private readonly DataStorageOptions _options;

    public DatabaseMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMaintenanceService> logger,
        Microsoft.Extensions.Options.IOptions<DataStorageOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database maintenance service started");

        // 等待应用启动完成
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceTasks();
                
                // 等待下一次维护周期
                var delay = TimeSpan.FromHours(_options.DatabaseCleanupIntervalHours);
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 正常取消，退出循环
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database maintenance task failed");
                
                // 出错后等待1小时再重试
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Database maintenance service stopped");
    }

    private async Task PerformMaintenanceTasks()
    {
        _logger.LogInformation("Starting database maintenance tasks");

        using var scope = _serviceProvider.CreateScope();
        var connectionService = scope.ServiceProvider.GetService<DatabaseConnectionService>();
        
        if (connectionService != null)
        {
            try
            {
                // 1. 数据库清理
                var retentionPeriod = TimeSpan.FromDays(_options.DataRetentionDays);
                await connectionService.CleanupDatabaseAsync(retentionPeriod);
                _logger.LogInformation("Database cleanup completed");

                // 2. 数据库优化（每周执行一次）
                if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
                {
                    await connectionService.OptimizeDatabaseAsync();
                    _logger.LogInformation("Database optimization completed");
                }

                // 3. 备份（如果需要的话）
                if (ShouldCreateBackup())
                {
                    var backupPath = await connectionService.BackupDatabaseAsync("backups");
                    _logger.LogInformation("Database backup created: {BackupPath}", backupPath);
                }

                // 4. 记录维护统计
                var stats = connectionService.GetConnectionStats();
                _logger.LogInformation("Database maintenance completed. Stats: {@Stats}", stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database maintenance tasks failed");
                throw;
            }
        }
        else
        {
            _logger.LogWarning("DatabaseConnectionService not available, skipping maintenance tasks");
        }
    }

    private bool ShouldCreateBackup()
    {
        // 简单的备份策略：每天凌晨2点
        var now = DateTime.Now;
        return now.Hour == 2 && now.Minute < 60; // 在维护周期内
    }
}

/// <summary>
/// 数据存储健康检查
/// </summary>
public class DataStorageHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IDataStorageService _dataStorageService;
    private readonly DatabaseConnectionService? _connectionService;

    public DataStorageHealthCheck(
        IDataStorageService dataStorageService,
        DatabaseConnectionService? connectionService = null)
    {
        _dataStorageService = dataStorageService;
        _connectionService = connectionService;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查数据存储服务健康状态
            var healthCheck = await _dataStorageService.HealthCheckAsync();
            
            var data = new Dictionary<string, object>
            {
                ["StorageHealthy"] = healthCheck.Success,
                ["StorageMessage"] = healthCheck.Message ?? "Unknown"
            };

            // 如果有数据库连接服务，也检查其健康状态
            if (_connectionService != null)
            {
                data["DatabaseHealthy"] = _connectionService.IsHealthy;
                data["ConnectionStats"] = _connectionService.GetConnectionStats();
            }

            // 获取存储统计信息
            var stats = await _dataStorageService.GetStorageStatsAsync();
            if (stats.Success && stats.Data != null)
            {
                data["StorageStats"] = stats.Data;
            }

            if (healthCheck.Success && (_connectionService == null || _connectionService.IsHealthy))
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                    "Data storage system is healthy", data);
            }
            else
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "Data storage system is unhealthy", data: data);
            }
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "Data storage health check failed", ex);
        }
    }
}