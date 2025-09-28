using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BlazorWebGame.Server.Configuration;

/// <summary>
/// 增强的数据存储配置选项
/// </summary>
public class EnhancedDataStorageOptions
{
    public const string SectionName = "EnhancedDataStorage";

    /// <summary>
    /// 数据存储实现类型
    /// Unified - 新的统一存储实现（推荐）
    /// Optimized - 原有的优化实现
    /// InMemory - 内存实现（测试用）
    /// </summary>
    public string ImplementationType { get; set; } = "Unified";

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=gamedata.db;Cache=Shared";

    /// <summary>
    /// 启用迁移自动应用
    /// </summary>
    public bool EnableAutoMigration { get; set; } = true;

    /// <summary>
    /// 启用数据库优化
    /// </summary>
    public bool EnableDatabaseOptimization { get; set; } = true;

    /// <summary>
    /// 缓存配置
    /// </summary>
    public CacheConfiguration Cache { get; set; } = new();

    /// <summary>
    /// 批量操作配置
    /// </summary>
    public BatchOperationConfiguration BatchOperations { get; set; } = new();

    /// <summary>
    /// 维护配置
    /// </summary>
    public MaintenanceConfiguration Maintenance { get; set; } = new();

    /// <summary>
    /// 性能监控配置
    /// </summary>
    public PerformanceMonitoringConfiguration Performance { get; set; } = new();

    /// <summary>
    /// 备份配置
    /// </summary>
    public BackupConfiguration Backup { get; set; } = new();
}

/// <summary>
/// 缓存配置
/// </summary>
public class CacheConfiguration
{
    /// <summary>
    /// 启用缓存
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 缓存大小限制（项目数量）
    /// </summary>
    public int SizeLimit { get; set; } = 10000;

    /// <summary>
    /// 缓存压缩比例
    /// </summary>
    public double CompactionPercentage { get; set; } = 0.25;

    /// <summary>
    /// 默认缓存过期时间（分钟）
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// 高优先级缓存过期时间（分钟）
    /// </summary>
    public int HighPriorityExpirationMinutes { get; set; } = 120;

    /// <summary>
    /// 滑动过期时间（分钟）
    /// </summary>
    public int SlidingExpirationMinutes { get; set; } = 10;
}

/// <summary>
/// 批量操作配置
/// </summary>
public class BatchOperationConfiguration
{
    /// <summary>
    /// 启用批量写入
    /// </summary>
    public bool EnableBatchWrites { get; set; } = true;

    /// <summary>
    /// 批量写入间隔（秒）
    /// </summary>
    public int BatchWriteIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// 批量写入最大项目数
    /// </summary>
    public int BatchWriteMaxItems { get; set; } = 100;

    /// <summary>
    /// 批量写入超时时间（秒）
    /// </summary>
    public int BatchWriteTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// 维护配置
/// </summary>
public class MaintenanceConfiguration
{
    /// <summary>
    /// 启用自动维护
    /// </summary>
    public bool EnableAutoMaintenance { get; set; } = true;

    /// <summary>
    /// 维护间隔（小时）
    /// </summary>
    public int MaintenanceIntervalHours { get; set; } = 24;

    /// <summary>
    /// 数据保留天数
    /// </summary>
    public int DataRetentionDays { get; set; } = 90;

    /// <summary>
    /// 软删除数据保留天数
    /// </summary>
    public int SoftDeleteRetentionDays { get; set; } = 30;

    /// <summary>
    /// 启用VACUUM操作（SQLite）
    /// </summary>
    public bool EnableVacuum { get; set; } = true;

    /// <summary>
    /// 启用ANALYZE操作（SQLite）
    /// </summary>
    public bool EnableAnalyze { get; set; } = true;
}

/// <summary>
/// 性能监控配置
/// </summary>
public class PerformanceMonitoringConfiguration
{
    /// <summary>
    /// 启用性能监控
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 统计数据保留天数
    /// </summary>
    public int StatsRetentionDays { get; set; } = 7;

    /// <summary>
    /// 慢查询阈值（毫秒）
    /// </summary>
    public int SlowQueryThresholdMs { get; set; } = 1000;

    /// <summary>
    /// 启用详细日志记录
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}

/// <summary>
/// 备份配置
/// </summary>
public class BackupConfiguration
{
    /// <summary>
    /// 启用自动备份
    /// </summary>
    public bool EnableAutoBackup { get; set; } = true;

    /// <summary>
    /// 备份间隔（小时）
    /// </summary>
    public int BackupIntervalHours { get; set; } = 168; // 每周

    /// <summary>
    /// 备份保留数量
    /// </summary>
    public int BackupRetentionCount { get; set; } = 4;

    /// <summary>
    /// 备份目录
    /// </summary>
    public string BackupDirectory { get; set; } = "backups";

    /// <summary>
    /// 启用压缩备份
    /// </summary>
    public bool EnableCompression { get; set; } = true;
}

/// <summary>
/// 增强数据存储配置扩展方法
/// </summary>
public static class EnhancedDataStorageConfigurationExtensions
{
    /// <summary>
    /// 添加增强的数据存储服务
    /// </summary>
    public static IServiceCollection AddEnhancedDataStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // 绑定配置选项
        services.Configure<EnhancedDataStorageOptions>(
            configuration.GetSection(EnhancedDataStorageOptions.SectionName));

        var options = configuration.GetSection(EnhancedDataStorageOptions.SectionName)
            .Get<EnhancedDataStorageOptions>() ?? new EnhancedDataStorageOptions();

        // 配置数据库上下文
        ConfigureDbContext(services, options, environment);

        // 配置缓存
        ConfigureCache(services, options);

        // 注册数据存储服务
        RegisterDataStorageServices(services, options, environment);

        // 注册后台服务
        RegisterBackgroundServices(services, options);

        // 注册健康检查
        RegisterHealthChecks(services, options);

        return services;
    }

    private static void ConfigureDbContext(
        IServiceCollection services,
        EnhancedDataStorageOptions options,
        IWebHostEnvironment environment)
    {
        services.AddDbContextFactory<EnhancedGameDbContext>(dbOptions =>
        {
            dbOptions.UseSqlite(options.ConnectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
            });

            // 开发环境特殊配置
            if (environment.IsDevelopment())
            {
                dbOptions.EnableDetailedErrors();
                if (options.Performance.EnableDetailedLogging)
                {
                    dbOptions.EnableSensitiveDataLogging();
                }
            }

            // 性能优化配置
            if (options.EnableDatabaseOptimization)
            {
                dbOptions.EnableServiceProviderCaching();
                dbOptions.EnableThreadSafetyChecks(false); // 生产环境禁用线程安全检查以提高性能
            }
        });
    }

    private static void ConfigureCache(
        IServiceCollection services,
        EnhancedDataStorageOptions options)
    {
        if (options.Cache.Enabled)
        {
            services.AddMemoryCache(cacheOptions =>
            {
                cacheOptions.SizeLimit = options.Cache.SizeLimit;
                cacheOptions.CompactionPercentage = options.Cache.CompactionPercentage;
            });
        }
        else
        {
            // 注册空的缓存实现
            services.AddSingleton<IMemoryCache, Microsoft.Extensions.Caching.Memory.NullMemoryCache>();
        }
    }

    private static void RegisterDataStorageServices(
        IServiceCollection services,
        EnhancedDataStorageOptions options,
        IWebHostEnvironment environment)
    {
        // 注册批量操作配置
        services.Configure<DataStorageOptions>(config =>
        {
            config.EnableBatchWrites = options.BatchOperations.EnableBatchWrites;
            config.BatchWriteIntervalSeconds = options.BatchOperations.BatchWriteIntervalSeconds;
            config.BatchWriteMaxItems = options.BatchOperations.BatchWriteMaxItems;
        });

        switch (options.ImplementationType.ToLower())
        {
            case "unified":
                services.AddSingleton<IUnifiedDataStorageService, UnifiedDataStorageService>();
                services.AddSingleton<IDataStorageService>(provider => 
                    provider.GetRequiredService<IUnifiedDataStorageService>());
                break;

            case "optimized":
                services.AddSingleton<IDataStorageService, OptimizedDataStorageService>();
                break;

            case "inmemory":
                services.AddSingleton<IDataStorageService, DataStorageService>();
                break;

            default:
                throw new ArgumentException($"未知的数据存储实现类型: {options.ImplementationType}");
        }
    }

    private static void RegisterBackgroundServices(
        IServiceCollection services,
        EnhancedDataStorageOptions options)
    {
        if (options.Maintenance.EnableAutoMaintenance)
        {
            services.AddHostedService<EnhancedDatabaseMaintenanceService>();
        }

        if (options.Backup.EnableAutoBackup)
        {
            services.AddHostedService<DatabaseBackupService>();
        }

        if (options.Performance.Enabled)
        {
            services.AddHostedService<PerformanceMonitoringService>();
        }
    }

    private static void RegisterHealthChecks(
        IServiceCollection services,
        EnhancedDataStorageOptions options)
    {
        services.AddHealthChecks()
            .AddCheck<EnhancedDataStorageHealthCheck>("enhanced_data_storage")
            .AddDbContextCheck<EnhancedGameDbContext>("enhanced_database");
    }

    /// <summary>
    /// 初始化增强数据存储系统
    /// </summary>
    public static async Task InitializeEnhancedDataStorageAsync(
        this IServiceProvider serviceProvider,
        ILogger logger)
    {
        try
        {
            logger.LogInformation("Initializing enhanced data storage system...");

            var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<EnhancedDataStorageOptions>>()?.Value
                ?? new EnhancedDataStorageOptions();

            // 初始化数据库
            await InitializeDatabaseAsync(serviceProvider, options, logger);

            // 验证服务健康状态
            await VerifyServiceHealthAsync(serviceProvider, logger);

            logger.LogInformation("Enhanced data storage system initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize enhanced data storage system");
            throw;
        }
    }

    private static async Task InitializeDatabaseAsync(
        IServiceProvider serviceProvider,
        EnhancedDataStorageOptions options,
        ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EnhancedGameDbContext>>();
        
        using var context = await contextFactory.CreateDbContextAsync();

        if (options.EnableAutoMigration)
        {
            logger.LogInformation("Applying database migrations...");
            await context.InitializeAsync();
        }
        else
        {
            logger.LogInformation("Ensuring database exists...");
            await context.Database.EnsureCreatedAsync();
        }

        // 获取数据库统计信息
        var stats = await context.GetDatabaseStatsAsync();
        logger.LogInformation("Database initialization completed. Stats: {@Stats}", stats);
    }

    private static async Task VerifyServiceHealthAsync(
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        try
        {
            var healthCheckService = serviceProvider.GetService<HealthCheckService>();
            if (healthCheckService != null)
            {
                var healthReport = await healthCheckService.CheckHealthAsync();
                
                if (healthReport.Status == HealthStatus.Healthy)
                {
                    logger.LogInformation("All health checks passed");
                }
                else
                {
                    logger.LogWarning("Some health checks failed: {Status}", healthReport.Status);
                    foreach (var entry in healthReport.Entries.Where(e => e.Value.Status != HealthStatus.Healthy))
                    {
                        logger.LogWarning("Health check '{HealthCheck}' failed: {Description}", 
                            entry.Key, entry.Value.Description);
                    }
                }
            }

            // 验证数据存储服务
            var dataStorageService = serviceProvider.GetService<IDataStorageService>();
            if (dataStorageService != null)
            {
                var healthCheck = await dataStorageService.HealthCheckAsync();
                if (healthCheck.Success)
                {
                    logger.LogInformation("Data storage service health check passed");
                }
                else
                {
                    logger.LogWarning("Data storage service health check failed: {Message}", healthCheck.Message);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify service health");
        }
    }
}

/// <summary>
/// 增强数据存储健康检查
/// </summary>
public class EnhancedDataStorageHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<EnhancedGameDbContext> _contextFactory;
    private readonly IDataStorageService _dataStorageService;
    private readonly ILogger<EnhancedDataStorageHealthCheck> _logger;

    public EnhancedDataStorageHealthCheck(
        IDbContextFactory<EnhancedGameDbContext> contextFactory,
        IDataStorageService dataStorageService,
        ILogger<EnhancedDataStorageHealthCheck> logger)
    {
        _contextFactory = contextFactory;
        _dataStorageService = dataStorageService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();
            var issues = new List<string>();

            // 检查数据库连接
            try
            {
                using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                data["DatabaseConnected"] = canConnect;
                
                if (!canConnect)
                {
                    issues.Add("无法连接到数据库");
                }
                else
                {
                    // 获取数据库统计信息
                    var stats = await dbContext.GetDatabaseStatsAsync();
                    data["DatabaseStats"] = stats;
                }
            }
            catch (Exception ex)
            {
                issues.Add($"数据库连接检查失败: {ex.Message}");
                data["DatabaseError"] = ex.Message;
            }

            // 检查数据存储服务
            try
            {
                var serviceHealthCheck = await _dataStorageService.HealthCheckAsync();
                data["DataStorageServiceHealthy"] = serviceHealthCheck.Success;
                
                if (!serviceHealthCheck.Success)
                {
                    issues.Add($"数据存储服务不健康: {serviceHealthCheck.Message}");
                }
                
                if (serviceHealthCheck.Data != null)
                {
                    data["DataStorageServiceData"] = serviceHealthCheck.Data;
                }
            }
            catch (Exception ex)
            {
                issues.Add($"数据存储服务检查失败: {ex.Message}");
                data["DataStorageServiceError"] = ex.Message;
            }

            // 检查存储统计信息
            try
            {
                var storageStats = await _dataStorageService.GetStorageStatsAsync();
                if (storageStats.Success && storageStats.Data != null)
                {
                    data["StorageStats"] = storageStats.Data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get storage stats during health check");
            }

            data["CheckedAt"] = DateTime.UtcNow;
            data["Issues"] = issues;

            if (issues.Count == 0)
            {
                return HealthCheckResult.Healthy("增强数据存储系统健康", data);
            }
            else
            {
                return HealthCheckResult.Unhealthy("增强数据存储系统存在问题", data: data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enhanced data storage health check failed");
            return HealthCheckResult.Unhealthy("增强数据存储健康检查失败", ex);
        }
    }
}

/// <summary>
/// 增强数据库维护后台服务
/// </summary>
public class EnhancedDatabaseMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EnhancedDatabaseMaintenanceService> _logger;
    private readonly EnhancedDataStorageOptions _options;

    public EnhancedDatabaseMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<EnhancedDatabaseMaintenanceService> logger,
        Microsoft.Extensions.Options.IOptions<EnhancedDataStorageOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Enhanced database maintenance service started");

        // 等待应用启动完成
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceTasks(stoppingToken);
                
                var delay = TimeSpan.FromHours(_options.Maintenance.MaintenanceIntervalHours);
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced database maintenance task failed");
                
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

        _logger.LogInformation("Enhanced database maintenance service stopped");
    }

    private async Task PerformMaintenanceTasks(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting enhanced database maintenance tasks");

        using var scope = _serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EnhancedGameDbContext>>();
        
        using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        try
        {
            var results = await context.PerformMaintenanceAsync();
            
            _logger.LogInformation("Enhanced database maintenance completed successfully: {@Results}", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enhanced database maintenance tasks failed");
            throw;
        }
    }
}

/// <summary>
/// 数据库备份后台服务
/// </summary>
public class DatabaseBackupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly EnhancedDataStorageOptions _options;

    public DatabaseBackupService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseBackupService> logger,
        Microsoft.Extensions.Options.IOptions<EnhancedDataStorageOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database backup service started");

        // 等待应用启动完成
        await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformBackup(stoppingToken);
                
                var delay = TimeSpan.FromHours(_options.Backup.BackupIntervalHours);
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database backup task failed");
                
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

        _logger.LogInformation("Database backup service stopped");
    }

    private async Task PerformBackup(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting automatic database backup");

        using var scope = _serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EnhancedGameDbContext>>();
        
        using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupPath = await context.CreateBackupAsync(_options.Backup.BackupDirectory);
            
            _logger.LogInformation("Database backup created successfully: {BackupPath}", backupPath);

            // 清理旧备份
            await CleanupOldBackups();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database backup failed");
            throw;
        }
    }

    private async Task CleanupOldBackups()
    {
        try
        {
            var backupDir = new DirectoryInfo(_options.Backup.BackupDirectory);
            if (!backupDir.Exists) return;

            var backupFiles = backupDir.GetFiles("gamedata_backup_*.db")
                .OrderByDescending(f => f.CreationTime)
                .Skip(_options.Backup.BackupRetentionCount)
                .ToList();

            foreach (var file in backupFiles)
            {
                file.Delete();
                _logger.LogDebug("Deleted old backup: {FileName}", file.Name);
            }

            if (backupFiles.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old backups", backupFiles.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old backups");
        }
    }
}

/// <summary>
/// 性能监控后台服务
/// </summary>
public class PerformanceMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly EnhancedDataStorageOptions _options;

    public PerformanceMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<PerformanceMonitoringService> logger,
        Microsoft.Extensions.Options.IOptions<EnhancedDataStorageOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Performance monitoring service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectPerformanceMetrics(stoppingToken);
                
                // 每5分钟收集一次性能指标
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Performance monitoring task failed");
                
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Performance monitoring service stopped");
    }

    private async Task CollectPerformanceMetrics(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            // 获取统一数据存储服务的性能指标
            var unifiedService = scope.ServiceProvider.GetService<IUnifiedDataStorageService>();
            if (unifiedService != null)
            {
                var perfStats = await unifiedService.GetPerformanceStatsAsync(cancellationToken);
                if (perfStats.Success && perfStats.Data != null)
                {
                    LogPerformanceMetrics(perfStats.Data);
                }
            }
            else
            {
                // 如果没有统一服务，尝试获取基础服务的统计信息
                var dataStorageService = scope.ServiceProvider.GetService<IDataStorageService>();
                if (dataStorageService != null)
                {
                    var stats = await dataStorageService.GetStorageStatsAsync();
                    if (stats.Success && stats.Data != null)
                    {
                        _logger.LogInformation("Storage statistics: {@Stats}", stats.Data);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect performance metrics");
        }
    }

    private void LogPerformanceMetrics(StoragePerformanceStats stats)
    {
        // 记录关键性能指标
        var totalOperations = stats.OperationCounts.Values.Sum();
        var averageResponseTime = stats.AverageResponseTimes.Values.DefaultIfEmpty(0).Average();

        _logger.LogInformation("Performance Metrics - Total Operations: {TotalOps}, Avg Response Time: {AvgTime}ms, Pending: {Pending}", 
            totalOperations, 
            averageResponseTime, 
            stats.PendingOperations);

        // 检查慢操作
        var slowOperations = stats.AverageResponseTimes
            .Where(kv => kv.Value > _options.Performance.SlowQueryThresholdMs)
            .ToList();

        if (slowOperations.Any())
        {
            _logger.LogWarning("Slow operations detected: {@SlowOperations}", slowOperations);
        }

        // 详细日志记录（如果启用）
        if (_options.Performance.EnableDetailedLogging)
        {
            _logger.LogDebug("Detailed performance stats: {@Stats}", stats);
        }
    }
}