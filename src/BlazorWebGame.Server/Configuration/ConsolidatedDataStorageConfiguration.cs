using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BlazorWebGame.Server.Configuration;

/// <summary>
/// 统一数据存储配置选项 - 整合所有配置选项
/// </summary>
public class ConsolidatedDataStorageOptions
{
    public const string SectionName = "ConsolidatedDataStorage";
    
    /// <summary>
    /// 数据存储类型: SQLite, PostgreSQL, SqlServer, InMemory
    /// </summary>
    public string StorageType { get; set; } = "SQLite";
    
    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=gamedata.db;Cache=Shared;Journal Mode=WAL";
    
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
    /// 批量操作间隔（秒）
    /// </summary>
    public int BatchIntervalSeconds { get; set; } = 5;
    
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
    
    /// <summary>
    /// 是否启用自动备份
    /// </summary>
    public bool EnableAutoBackup { get; set; } = false;
    
    /// <summary>
    /// 自动备份间隔（小时）
    /// </summary>
    public int AutoBackupIntervalHours { get; set; } = 24;
    
    /// <summary>
    /// 备份保留天数
    /// </summary>
    public int BackupRetentionDays { get; set; } = 7;
    
    /// <summary>
    /// SQLite优化配置
    /// </summary>
    public SqliteOptimizationOptions SqliteOptimization { get; set; } = new();
}

/// <summary>
/// 统一数据存储配置扩展方法
/// </summary>
public static class ConsolidatedDataStorageConfigurationExtensions
{
    /// <summary>
    /// 添加统一数据存储服务
    /// </summary>
    public static IServiceCollection AddConsolidatedDataStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // 绑定配置选项
        services.Configure<ConsolidatedDataStorageOptions>(
            configuration.GetSection(ConsolidatedDataStorageOptions.SectionName));

        var storageOptions = configuration.GetSection(ConsolidatedDataStorageOptions.SectionName)
            .Get<ConsolidatedDataStorageOptions>() ?? new ConsolidatedDataStorageOptions();

        // 如果没有配置节，尝试从旧的配置中读取
        if (configuration.GetSection(ConsolidatedDataStorageOptions.SectionName).GetChildren().Any() == false)
        {
            storageOptions = MigrateFromLegacyConfiguration(configuration);
            services.Configure<ConsolidatedDataStorageOptions>(opts =>
            {
                configuration.GetSection(ConsolidatedDataStorageOptions.SectionName).Bind(opts);
                if (opts.ConnectionString == "Data Source=gamedata.db;Cache=Shared;Journal Mode=WAL")
                {
                    opts.ConnectionString = configuration.GetConnectionString("DefaultConnection") 
                        ?? "Data Source=gamedata.db;Cache=Shared;Journal Mode=WAL";
                }
            });
        }

        // 配置数据库上下文工厂
        ConfigureDbContextFactory(services, storageOptions, environment);

        // 配置缓存
        if (storageOptions.EnableCaching)
        {
            ConfigureMemoryCache(services, storageOptions);
        }

        // 注册统一数据存储服务
        services.AddScoped<ConsolidatedDataStorageService>();
        
        // 注册接口实现
        services.AddScoped<IDataStorageService>(provider => provider.GetRequiredService<ConsolidatedDataStorageService>());

        // 注册健康检查
        if (storageOptions.EnableHealthChecks)
        {
            services.AddHealthChecks()
                .AddCheck<ConsolidatedDataStorageHealthCheck>("consolidated-data-storage");
        }

        // 注册后台维护服务
        services.AddHostedService<ConsolidatedDataStorageMaintenanceService>();

        return services;
    }

    private static ConsolidatedDataStorageOptions MigrateFromLegacyConfiguration(IConfiguration configuration)
    {
        var options = new ConsolidatedDataStorageOptions();

        // 尝试从UnifiedDataStorage配置读取
        var unifiedConfig = configuration.GetSection("UnifiedDataStorage");
        if (unifiedConfig.Exists())
        {
            unifiedConfig.Bind(options);
        }

        // 应用连接字符串
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            options.ConnectionString = connectionString;
        }

        return options;
    }

    private static void ConfigureDbContextFactory(
        IServiceCollection services,
        ConsolidatedDataStorageOptions storageOptions,
        IWebHostEnvironment environment)
    {
        services.AddDbContextFactory<ConsolidatedGameDbContext>(options =>
        {
            switch (storageOptions.StorageType.ToLower())
            {
                case "sqlite":
                default:
                    ConfigureSqlite(options, storageOptions, environment);
                    break;

                case "postgresql":
                    ConfigurePostgreSQL(options, storageOptions, environment);
                    break;

                case "sqlserver":
                    ConfigureSqlServer(options, storageOptions, environment);
                    break;

                case "inmemory":
                    ConfigureInMemory(options, storageOptions, environment);
                    break;
            }
        });
    }

    private static void ConfigureSqlite(
        DbContextOptionsBuilder options,
        ConsolidatedDataStorageOptions storageOptions,
        IWebHostEnvironment environment)
    {
        options.UseSqlite(storageOptions.ConnectionString, sqliteOptions =>
        {
            sqliteOptions.CommandTimeout(storageOptions.CommandTimeoutSeconds);
        });

        ConfigureCommonOptions(options, storageOptions, environment);
    }

    private static void ConfigurePostgreSQL(
        DbContextOptionsBuilder options,
        ConsolidatedDataStorageOptions storageOptions,
        IWebHostEnvironment environment)
    {
        // PostgreSQL support - requires Npgsql.EntityFrameworkCore.PostgreSQL package
        throw new NotSupportedException("PostgreSQL support requires additional packages. Please install Npgsql.EntityFrameworkCore.PostgreSQL and uncomment the configuration code.");
        
        /*
        options.UseNpgsql(storageOptions.ConnectionString, npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(storageOptions.CommandTimeoutSeconds);
        });

        ConfigureCommonOptions(options, storageOptions, environment);
        */
    }

    private static void ConfigureSqlServer(
        DbContextOptionsBuilder options,
        ConsolidatedDataStorageOptions storageOptions,
        IWebHostEnvironment environment)
    {
        // SQL Server support - requires Microsoft.EntityFrameworkCore.SqlServer package
        throw new NotSupportedException("SQL Server support requires additional packages. Please install Microsoft.EntityFrameworkCore.SqlServer and uncomment the configuration code.");
        
        /*
        options.UseSqlServer(storageOptions.ConnectionString, sqlServerOptions =>
        {
            sqlServerOptions.CommandTimeout(storageOptions.CommandTimeoutSeconds);
        });

        ConfigureCommonOptions(options, storageOptions, environment);
        */
    }

    private static void ConfigureInMemory(
        DbContextOptionsBuilder options,
        ConsolidatedDataStorageOptions storageOptions,
        IWebHostEnvironment environment)
    {
        // In-Memory database support - requires Microsoft.EntityFrameworkCore.InMemory package
        throw new NotSupportedException("In-Memory database support requires additional packages. Please install Microsoft.EntityFrameworkCore.InMemory and uncomment the configuration code.");
        
        /*
        options.UseInMemoryDatabase("BlazorWebGameDb");
        ConfigureCommonOptions(options, storageOptions, environment);
        */
    }

    private static void ConfigureCommonOptions(
        DbContextOptionsBuilder options,
        ConsolidatedDataStorageOptions storageOptions,
        IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        }

        if (storageOptions.EnablePerformanceMonitoring)
        {
            // 可以添加性能监控相关的配置
            options.EnableServiceProviderCaching();
            options.EnableSensitiveDataLogging(environment.IsDevelopment());
        }
    }

    private static void ConfigureMemoryCache(
        IServiceCollection services,
        ConsolidatedDataStorageOptions storageOptions)
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
    public static async Task InitializeConsolidatedDataStorageAsync(
        this IServiceProvider serviceProvider,
        ILogger logger)
    {
        try
        {
            var storageOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<ConsolidatedDataStorageOptions>>()?.Value
                ?? new ConsolidatedDataStorageOptions();

            logger.LogInformation("Initializing consolidated data storage system with type: {StorageType}", 
                storageOptions.StorageType);

            using var scope = serviceProvider.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ConsolidatedGameDbContext>>();
            
            using var context = await contextFactory.CreateDbContextAsync();

            // 确保数据库创建和优化
            if (storageOptions.EnableAutoMigration)
            {
                await context.EnsureDatabaseOptimizedAsync();
            }

            // 验证服务健康状态
            var dataStorageService = scope.ServiceProvider.GetRequiredService<BlazorWebGame.Shared.Interfaces.IAdvancedGameRepository>();
            var healthCheck = await dataStorageService.HealthCheckAsync();
            
            if (healthCheck.Success)
            {
                logger.LogInformation("Consolidated data storage health check passed");
            }
            else
            {
                logger.LogWarning("Consolidated data storage health check failed: {Message}", healthCheck.Message);
            }

            // 获取并记录统计信息
            var stats = await dataStorageService.GetDatabaseStatsAsync();
            if (stats.Success && stats.Data != null)
            {
                logger.LogInformation("Consolidated data storage statistics: {@Stats}", stats.Data);
            }

            logger.LogInformation("Consolidated data storage system initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize consolidated data storage system");
            throw;
        }
    }
}

/// <summary>
/// 统一数据存储健康检查
/// </summary>
public class ConsolidatedDataStorageHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<ConsolidatedGameDbContext> _contextFactory;
    private readonly ILogger<ConsolidatedDataStorageHealthCheck> _logger;

    public ConsolidatedDataStorageHealthCheck(
        IDbContextFactory<ConsolidatedGameDbContext> contextFactory,
        ILogger<ConsolidatedDataStorageHealthCheck> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            // 测试数据库连接
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // 获取基本统计信息
            var stats = await dbContext.GetDatabaseStatsAsync();
            
            return HealthCheckResult.Healthy("Consolidated data storage is healthy", stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Consolidated data storage health check failed");
            return HealthCheckResult.Unhealthy("Consolidated data storage health check failed", ex);
        }
    }
}

/// <summary>
/// 统一数据存储维护后台服务
/// </summary>
public class ConsolidatedDataStorageMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsolidatedDataStorageMaintenanceService> _logger;
    private readonly ConsolidatedDataStorageOptions _options;

    public ConsolidatedDataStorageMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<ConsolidatedDataStorageMaintenanceService> logger,
        Microsoft.Extensions.Options.IOptions<ConsolidatedDataStorageOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consolidated data storage maintenance service started");

        // 等待应用启动完成
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceTasks();
                
                // 等待下一次维护周期（默认6小时）
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Consolidated data storage maintenance task failed");
                
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

        _logger.LogInformation("Consolidated data storage maintenance service stopped");
    }

    private async Task PerformMaintenanceTasks()
    {
        _logger.LogInformation("Starting consolidated data storage maintenance tasks");

        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetService<BlazorWebGame.Shared.Interfaces.IAdvancedGameRepository>();
        
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

                // 4. 自动备份
                if (_options.EnableAutoBackup && ShouldCreateBackup())
                {
                    var backupResult = await repository.BackupDatabaseAsync("backups");
                    if (backupResult.Success)
                    {
                        _logger.LogInformation("Database backup created: {BackupPath}", backupResult.Data);
                        await CleanupOldBackups();
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
            _logger.LogWarning("BlazorWebGame.Shared.Interfaces.IAdvancedGameRepository not available, skipping maintenance tasks");
        }
    }

    private bool ShouldCreateBackup()
    {
        // 基于配置的备份策略
        var now = DateTime.Now;
        var lastBackupTime = GetLastBackupTime();
        
        return lastBackupTime == null || 
               now.Subtract(lastBackupTime.Value).TotalHours >= _options.AutoBackupIntervalHours;
    }

    private DateTime? GetLastBackupTime()
    {
        try
        {
            var backupDir = "backups";
            if (!Directory.Exists(backupDir))
                return null;

            var backupFiles = Directory.GetFiles(backupDir, "gamedata_backup_*.db");
            if (backupFiles.Length == 0)
                return null;

            return backupFiles
                .Select(f => File.GetCreationTime(f))
                .Max();
        }
        catch
        {
            return null;
        }
    }

    private async Task CleanupOldBackups()
    {
        try
        {
            var backupDir = "backups";
            if (!Directory.Exists(backupDir))
                return;

            var cutoffDate = DateTime.Now.AddDays(-_options.BackupRetentionDays);
            var backupFiles = Directory.GetFiles(backupDir, "gamedata_backup_*.db");

            foreach (var file in backupFiles)
            {
                if (File.GetCreationTime(file) < cutoffDate)
                {
                    File.Delete(file);
                    _logger.LogInformation("Deleted old backup: {BackupFile}", Path.GetFileName(file));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old backups");
        }
    }
}