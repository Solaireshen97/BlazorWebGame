using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Configuration;

/// <summary>
/// 统一数据存储配置选项
/// </summary>
public class UnifiedDataStorageOptions
{
    public const string SectionName = "UnifiedDataStorage";
    
    /// <summary>
    /// 数据存储类型: InMemory, SQLite, PostgreSQL, SqlServer
    /// </summary>
    public string StorageType { get; set; } = "SQLite";
    
    /// <summary>
    /// 是否启用缓存
    /// </summary>
    public bool EnableCaching { get; set; } = true;
    
    /// <summary>
    /// 缓存过期时间（分钟）
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 30;
    
    /// <summary>
    /// 高优先级缓存过期时间（小时）
    /// </summary>
    public int HighPriorityCacheExpirationHours { get; set; } = 2;
    
    /// <summary>
    /// 是否启用批量操作
    /// </summary>
    public bool EnableBatchOperations { get; set; } = true;
    
    /// <summary>
    /// 批量操作大小
    /// </summary>
    public int BatchSize { get; set; } = 100;
    
    /// <summary>
    /// 是否启用性能监控
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;
    
    /// <summary>
    /// 是否启用事务支持
    /// </summary>
    public bool EnableTransactionSupport { get; set; } = true;
    
    /// <summary>
    /// 数据库连接超时时间（秒）
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 命令超时时间（秒）
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 是否启用数据库自动迁移
    /// </summary>
    public bool EnableAutoMigration { get; set; } = true;
    
    /// <summary>
    /// 是否启用数据库健康检查
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;
}

/// <summary>
/// SQLite优化选项
/// </summary>
public class SqliteOptimizationOptions
{
    public const string SectionName = "SqliteOptimization";
    
    /// <summary>
    /// 是否启用WAL模式
    /// </summary>
    public bool EnableWALMode { get; set; } = true;
    
    /// <summary>
    /// 缓存大小（页数）
    /// </summary>
    public int CacheSize { get; set; } = 10000;
    
    /// <summary>
    /// 是否启用内存映射
    /// </summary>
    public bool EnableMemoryMapping { get; set; } = true;
    
    /// <summary>
    /// 内存映射大小（字节）
    /// </summary>
    public long MemoryMapSize { get; set; } = 268435456; // 256MB
    
    /// <summary>
    /// 同步模式
    /// </summary>
    public string SynchronousMode { get; set; } = "NORMAL";
    
    /// <summary>
    /// 临时存储模式
    /// </summary>
    public string TempStore { get; set; } = "MEMORY";
    
    /// <summary>
    /// 是否启用优化器
    /// </summary>
    public bool EnableOptimizer { get; set; } = true;
    
    /// <summary>
    /// 分析限制
    /// </summary>
    public int AnalysisLimit { get; set; } = 1000;
}

/// <summary>
/// 统一数据存储配置扩展方法
/// </summary>
public static class UnifiedDataStorageConfigurationExtensions
{
    /// <summary>
    /// 添加统一数据存储服务
    /// </summary>
    public static IServiceCollection AddUnifiedDataStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // 绑定配置选项
        services.Configure<UnifiedDataStorageOptions>(
            configuration.GetSection(UnifiedDataStorageOptions.SectionName));
        services.Configure<SqliteOptimizationOptions>(
            configuration.GetSection(SqliteOptimizationOptions.SectionName));

        var storageOptions = configuration.GetSection(UnifiedDataStorageOptions.SectionName)
            .Get<UnifiedDataStorageOptions>() ?? new UnifiedDataStorageOptions();
        
        var sqliteOptions = configuration.GetSection(SqliteOptimizationOptions.SectionName)
            .Get<SqliteOptimizationOptions>() ?? new SqliteOptimizationOptions();

        // 配置数据库上下文工厂
        ConfigureDbContext(services, configuration, environment, storageOptions, sqliteOptions);

        // 配置缓存
        if (storageOptions.EnableCaching)
        {
            ConfigureMemoryCache(services, storageOptions);
        }

        // 注册仓储服务
        services.AddScoped<IGameRepository, UnifiedGameRepository>();
        services.AddScoped<IAdvancedGameRepository, UnifiedGameRepository>();

        // 注册健康检查
        if (storageOptions.EnableHealthChecks)
        {
            services.AddHealthChecks()
                .AddCheck<UnifiedDataStorageHealthCheck>("unified-data-storage");
        }

        // 注册后台维护服务
        services.AddHostedService<UnifiedDataStorageMaintenanceService>();

        return services;
    }

    private static void ConfigureDbContext(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        UnifiedDataStorageOptions storageOptions,
        SqliteOptimizationOptions sqliteOptions)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=gamedata.db;Cache=Shared";

        switch (storageOptions.StorageType.ToLower())
        {
            case "sqlite":
            default:
                services.AddDbContextFactory<UnifiedGameDbContext>(options =>
                {
                    options.UseSqlite(connectionString, sqliteDbOptions =>
                    {
                        sqliteDbOptions.CommandTimeout(storageOptions.CommandTimeoutSeconds);
                    });

                    ConfigureEntityFrameworkOptions(options, environment, storageOptions);
                });
                break;

            case "postgresql":
                // PostgreSQL support is not currently implemented
                // To enable PostgreSQL, add Npgsql.EntityFrameworkCore.PostgreSQL package
                // and uncomment the following code:
                /*
                services.AddDbContextFactory<UnifiedGameDbContext>(options =>
                {
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.CommandTimeout(storageOptions.CommandTimeoutSeconds);
                    });

                    ConfigureEntityFrameworkOptions(options, environment, storageOptions);
                });
                */
                throw new NotSupportedException("PostgreSQL support is not currently enabled. Please use SQLite instead.");
                // break;

            case "sqlserver":
                // SQL Server support is not currently implemented
                // To enable SQL Server, add Microsoft.EntityFrameworkCore.SqlServer package
                throw new NotSupportedException("SQL Server support is not currently enabled. Please use SQLite instead.");
                // break;

            case "inmemory":
                // In-memory database support is not currently implemented  
                // To enable In-Memory database, add Microsoft.EntityFrameworkCore.InMemory package
                throw new NotSupportedException("In-Memory database support is not currently enabled. Please use SQLite instead.");
                // break;
        }
    }

    private static void ConfigureEntityFrameworkOptions(
        DbContextOptionsBuilder options,
        IWebHostEnvironment environment,
        UnifiedDataStorageOptions storageOptions)
    {
        if (environment.IsDevelopment())
        {
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        }

        if (storageOptions.EnablePerformanceMonitoring)
        {
            // 可以添加性能监控相关的配置
        }
    }

    private static void ConfigureMemoryCache(
        IServiceCollection services,
        UnifiedDataStorageOptions storageOptions)
    {
        services.AddMemoryCache(cacheOptions =>
        {
            cacheOptions.SizeLimit = 1000; // 限制缓存项数量
            cacheOptions.CompactionPercentage = 0.25; // 压缩比例
        });
    }

    /// <summary>
    /// 初始化统一数据存储系统
    /// </summary>
    public static async Task InitializeUnifiedDataStorageAsync(
        this IServiceProvider serviceProvider,
        ILogger logger)
    {
        try
        {
            var storageOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<UnifiedDataStorageOptions>>()?.Value
                ?? new UnifiedDataStorageOptions();

            logger.LogInformation("Initializing unified data storage system with type: {StorageType}", 
                storageOptions.StorageType);

            using var scope = serviceProvider.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UnifiedGameDbContext>>();
            
            using var context = await contextFactory.CreateDbContextAsync();

            // 确保数据库创建
            if (storageOptions.EnableAutoMigration)
            {
                await context.Database.EnsureCreatedAsync();
                await context.EnsureDatabaseOptimizedAsync();
            }

            // 验证仓储服务
            var repository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
            var healthCheck = await repository.HealthCheckAsync();
            
            if (healthCheck.Success)
            {
                logger.LogInformation("Unified data storage health check passed");
            }
            else
            {
                logger.LogWarning("Unified data storage health check failed: {Message}", healthCheck.Message);
            }

            // 获取并记录统计信息
            var stats = await repository.GetDatabaseStatsAsync();
            if (stats.Success && stats.Data != null)
            {
                logger.LogInformation("Unified data storage statistics: {@Stats}", stats.Data);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize unified data storage system");
            throw;
        }
    }
}

/// <summary>
/// 统一数据存储健康检查
/// </summary>
public class UnifiedDataStorageHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IGameRepository _repository;

    public UnifiedDataStorageHealthCheck(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthCheck = await _repository.HealthCheckAsync();
            
            if (healthCheck.Success)
            {
                var stats = await _repository.GetDatabaseStatsAsync();
                var data = stats.Success && stats.Data != null ? stats.Data : new Dictionary<string, object>();
                
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                    "Unified data storage system is healthy", data);
            }
            else
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    $"Unified data storage system is unhealthy: {healthCheck.Message}");
            }
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "Unified data storage health check failed", ex);
        }
    }
}

/// <summary>
/// 统一数据存储维护后台服务
/// </summary>
public class UnifiedDataStorageMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UnifiedDataStorageMaintenanceService> _logger;
    private readonly UnifiedDataStorageOptions _options;

    public UnifiedDataStorageMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<UnifiedDataStorageMaintenanceService> logger,
        Microsoft.Extensions.Options.IOptions<UnifiedDataStorageOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Unified data storage maintenance service started");

        // 等待应用启动完成
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceTasks();
                
                // 等待下一次维护周期（每6小时）
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unified data storage maintenance task failed");
                
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

        _logger.LogInformation("Unified data storage maintenance service stopped");
    }

    private async Task PerformMaintenanceTasks()
    {
        _logger.LogInformation("Starting unified data storage maintenance tasks");

        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetService<IAdvancedGameRepository>();
        
        if (repository != null)
        {
            try
            {
                // 1. 数据库优化
                await repository.OptimizeDatabaseAsync();
                _logger.LogInformation("Database optimization completed");

                // 2. 重建索引（每周执行一次）
                if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
                {
                    await repository.RebuildIndexesAsync();
                    _logger.LogInformation("Database indexes rebuilt");
                }

                // 3. 数据库压缩（每月执行一次）
                if (DateTime.UtcNow.Day == 1)
                {
                    await repository.CompactDatabaseAsync();
                    _logger.LogInformation("Database compaction completed");
                }

                // 4. 创建备份（如果需要的话）
                if (ShouldCreateBackup())
                {
                    var backupPath = await repository.BackupDatabaseAsync("backups");
                    if (backupPath.Success)
                    {
                        _logger.LogInformation("Database backup created: {BackupPath}", backupPath.Data);
                    }
                }

                // 5. 记录维护统计
                var stats = await repository.GetDatabaseStatsAsync();
                if (stats.Success)
                {
                    _logger.LogInformation("Maintenance completed. Stats: {@Stats}", stats.Data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maintenance tasks failed");
                throw;
            }
        }
        else
        {
            _logger.LogWarning("IAdvancedGameRepository not available, skipping maintenance tasks");
        }
    }

    private bool ShouldCreateBackup()
    {
        // 简单的备份策略：每天凌晨3点
        var now = DateTime.Now;
        return now.Hour == 3 && now.Minute < 30; // 在维护周期内
    }
}