using BlazorWebGame.Server.Hubs;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Server.Security;
using BlazorWebGame.Server.Middleware;
using BlazorWebGame.Server;
using BlazorWebGame.Server.Configuration;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Server.Data.Repositories;
using BlazorWebGame.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 配置选项系统
builder.Services.Configure<GameServerOptions>(
    builder.Configuration.GetSection(GameServerOptions.SectionName));
builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<MonitoringOptions>(
    builder.Configuration.GetSection(MonitoringOptions.SectionName));

// 验证配置
builder.Services.AddOptions<GameServerOptions>()
    .Bind(builder.Configuration.GetSection(GameServerOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 配置 Serilog 日志
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/blazorwebgame-.log", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加 SignalR
builder.Services.AddSignalR();

// 配置JWT身份验证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? 
                    throw new InvalidOperationException("JWT Key not configured"))),
            ClockSkew = TimeSpan.FromMinutes(1) // 允许1分钟的时钟偏差
        };

        // 配置SignalR的JWT验证
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/gamehub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// 配置CORS with security considerations
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>();
        var allowedOrigins = securityOptions?.Cors?.AllowedOrigins ?? 
            new[] { "https://localhost:7051", "http://localhost:5190" };
            
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 配置速率限制选项
var rateLimitOptions = new RateLimitOptions();
var securityConfig = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>();

if (securityConfig?.RateLimit != null)
{
    rateLimitOptions.IpRateLimit = new BlazorWebGame.Server.Middleware.RateLimitRule
    {
        MaxRequests = securityConfig.RateLimit.IpRateLimit.MaxRequests,
        TimeWindow = TimeSpan.FromMinutes(securityConfig.RateLimit.IpRateLimit.TimeWindowMinutes)
    };

    rateLimitOptions.UserRateLimit = new BlazorWebGame.Server.Middleware.RateLimitRule
    {
        MaxRequests = securityConfig.RateLimit.UserRateLimit.MaxRequests,
        TimeWindow = TimeSpan.FromMinutes(securityConfig.RateLimit.UserRateLimit.TimeWindowMinutes)
    };
}
else
{
    // 默认值
    rateLimitOptions.IpRateLimit = new BlazorWebGame.Server.Middleware.RateLimitRule
    {
        MaxRequests = 100,
        TimeWindow = TimeSpan.FromMinutes(1)
    };

    rateLimitOptions.UserRateLimit = new BlazorWebGame.Server.Middleware.RateLimitRule
    {
        MaxRequests = 200,
        TimeWindow = TimeSpan.FromMinutes(1)
    };
}

builder.Services.AddSingleton(rateLimitOptions);

// 配置数据库上下文
var gameServerOptions = builder.Configuration.GetSection(GameServerOptions.SectionName).Get<GameServerOptions>();
var databaseProvider = gameServerOptions?.DatabaseProvider ?? "InMemory";

switch (databaseProvider.ToLowerInvariant())
{
    case "sqlserver":
        builder.Services.AddDbContext<GameDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure()));
        break;
    case "localdb":
        builder.Services.AddDbContext<GameDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure()));
        break;
    case "inmemory":
    default:
        builder.Services.AddDbContext<GameDbContext>(options =>
            options.UseInMemoryDatabase("BlazorWebGameDb"));
        break;
}

// 注册仓储模式
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddScoped<IBattleRecordRepository, BattleRecordRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();
builder.Services.AddScoped<IQuestRepository, QuestRepository>();
builder.Services.AddScoped<IOfflineDataRepository, OfflineDataRepository>();
builder.Services.AddScoped<IGameEventRepository, GameEventRepository>();

// 注册统一服务
builder.Services.AddSingleton<ErrorHandlingService>();
builder.Services.AddSingleton<PerformanceMonitoringService>();
builder.Services.AddSingleton<ServerOptimizationService>();

// 添加健康检查
builder.Services.AddHealthChecks()
    .AddCheck<GameHealthCheckService>("game-health");

// 注册安全服务
builder.Services.AddSingleton<GameAuthenticationService>();
builder.Services.AddSingleton<DemoUserService>();

// 注册共享事件管理器
builder.Services.AddSingleton<BlazorWebGame.Shared.Events.GameEventManager>();

// 注册统一事件系统
builder.Services.AddSingleton<UnifiedEventService>();

// 注册服务定位器（单例模式）
builder.Services.AddSingleton<ServerServiceLocator>();

// 注册数据存储服务（临时禁用有问题的实现）
builder.Services.AddSingleton<BlazorWebGame.Shared.Interfaces.IDataStorageService, DataStorageService>();
builder.Services.AddSingleton<DataStorageIntegrationService>();

// 添加内存缓存支持
builder.Services.AddMemoryCache();

// 配置 Redis 分布式缓存
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
try
{
    // 添加 StackExchange.Redis 连接
    builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
    {
        var configuration = ConfigurationOptions.Parse(redisConnectionString);
        configuration.AbortOnConnectFail = false; // 允许在连接失败时继续运行
        configuration.ConnectTimeout = 5000; // 5秒连接超时
        configuration.SyncTimeout = 5000; // 5秒同步超时
        return ConnectionMultiplexer.Connect(configuration);
    });

    // 添加分布式缓存
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "BlazorWebGame";
    });

    // 注册 Redis 游戏缓存服务
    builder.Services.AddSingleton<RedisGameCacheService>();
    
    // 注册 Redis 事件持久化服务（替换内存实现）
    builder.Services.AddSingleton<BlazorWebGame.Shared.Events.IRedisEventPersistence, RedisEventPersistenceService>();

    builder.Services.GetRequiredService<ILogger<IServiceCollection>>()?.LogInformation("Redis services configured with connection: {ConnectionString}", redisConnectionString.Split('@').LastOrDefault());
}
catch (Exception ex)
{
    // Redis 连接失败时的降级处理
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();
    logger.LogWarning(ex, "Failed to configure Redis, falling back to in-memory implementations");
    
    // 使用内存实现作为后备
    builder.Services.AddSingleton<BlazorWebGame.Shared.Events.IRedisEventPersistence, BlazorWebGame.Shared.Events.InMemoryEventPersistence>();
}

// 注册离线结算服务
builder.Services.AddSingleton<OfflineSettlementService>();

// 注册增强的离线结算系统
builder.Services.AddSingleton<OfflineActivityManager>();
builder.Services.AddSingleton<EnhancedOfflineSettlementService>();

// 注册新的玩家服务系统
builder.Services.AddSingleton<ServerPlayerAttributeService>();
builder.Services.AddSingleton<ServerPlayerProfessionService>();
builder.Services.AddSingleton<ServerPlayerUtilityService>();

// 注册游戏服务
builder.Services.AddSingleton<ServerSkillSystem>();
builder.Services.AddSingleton<ServerLootService>();
builder.Services.AddSingleton<ServerCombatEngine>();
builder.Services.AddSingleton<ServerCharacterCombatService>();  // 新增角色战斗服务
builder.Services.AddSingleton<ServerPartyService>();
builder.Services.AddSingleton<ServerBattleFlowService>();
builder.Services.AddSingleton<ServerProductionService>();
builder.Services.AddSingleton<ServerInventoryService>();
builder.Services.AddSingleton<ServerQuestService>();
builder.Services.AddSingleton<ServerShopService>();  // 添加商店服务
builder.Services.AddSingleton<ServerReputationService>();  // 添加声望服务
builder.Services.AddSingleton<ServerEquipmentService>();  // 添加装备服务
builder.Services.AddSingleton<ServerEquipmentGenerator>(); // 新增装备生成器
builder.Services.AddSingleton<GameEngineService>();
builder.Services.AddSingleton<ServerCharacterService>();
builder.Services.AddSingleton<ServerEventService>();

// 注册事件驱动的服务系统
builder.Services.AddSingleton<EventDrivenBattleEngine>();
builder.Services.AddSingleton<EventDrivenProfessionService>();

// 注册角色状态管理服务
builder.Services.AddSingleton<CharacterStateService>();

// 注册战斗管理器 - 需要初始化玩家列表
builder.Services.AddSingleton<ServerBattleManager>(serviceProvider =>
{
    var allCharacters = new List<ServerBattlePlayer>(); // TODO: 从数据库或服务中获取
    return new ServerBattleManager(
        allCharacters,
        serviceProvider.GetRequiredService<ServerCombatEngine>(),
        serviceProvider.GetRequiredService<ServerBattleFlowService>(),
        serviceProvider.GetRequiredService<ServerCharacterService>(),
        serviceProvider.GetRequiredService<ServerSkillSystem>(),
        serviceProvider.GetRequiredService<ServerLootService>(),
        serviceProvider.GetRequiredService<ILogger<ServerBattleManager>>(),
        serviceProvider.GetRequiredService<IHubContext<GameHub>>()
    );
});

builder.Services.AddHostedService<GameLoopService>();
builder.Services.AddHostedService<ServerOptimizationService>();

var app = builder.Build();

// 初始化数据库
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var gameOptions = scope.ServiceProvider.GetRequiredService<IOptions<GameServerOptions>>().Value;

    if (gameOptions.EnableDatabaseMigration)
    {
        try
        {
            await DbInitializer.InitializeAsync(context, logger, gameOptions.EnableDatabaseSeeding);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize database");
            // 在开发环境中，我们继续运行，但在生产环境中可能需要停止
            if (!app.Environment.IsDevelopment())
            {
                throw;
            }
        }
    }
}

// 初始化服务定位器
ServerServiceLocator.Initialize(app.Services);

// 初始化服务集成和缓存
try
{
    await app.Services.InitializeServicesAsync(app.Services.GetRequiredService<ILogger<Program>>());
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to initialize services");
    // 在开发环境继续运行，生产环境可能需要停止
    if (!app.Environment.IsDevelopment())
    {
        throw;
    }
}

// 在开发环境中运行战斗系统测试（基于配置）
if (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();  
    var gameOptions = app.Services.GetRequiredService<IOptions<GameServerOptions>>().Value;
    
    if (gameOptions.EnableDevelopmentTests)
    {
        // 运行战斗系统测试
        try
        {
            TestBattleSystem.RunBattleTest(app.Services, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Battle system test failed");
        }
        
        // 运行组队系统测试
        try
        {
            TestPartySystem.RunPartyTest(logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Party system test failed");
        }
        
        // 运行统一事件系统测试
        try
        {
            BlazorWebGame.Server.Tests.UnifiedEventSystemTest.RunEventSystemTest(app.Services, logger);
            BlazorWebGame.Server.Tests.UnifiedEventSystemTest.RunPerformanceBenchmark(logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unified event system test failed");
        }
    }
    else
    {
        logger.LogInformation("Development tests are disabled in configuration");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 启用CORS（必须在身份验证之前）
app.UseCors();

// 安全中间件管道（顺序很重要）
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

// 身份验证和授权
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 配置 SignalR Hub（需要身份验证）
app.MapHub<GameHub>("/gamehub");

// 添加健康检查端点
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            Status = report.Status.ToString(),
            Checks = report.Entries.Select(entry => new
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Description = entry.Value.Description,
                Data = entry.Value.Data
            }),
            Duration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

// 添加简单健康检查端点
app.MapGet("/health/simple", () => new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName 
});

// 添加详细服务健康检查端点
app.MapGet("/health/services", async (IServiceProvider services) =>
{
    try
    {
        var health = await services.GetServiceHealthAsync();
        return Results.Ok(health);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Service Health Check Failed"
        );
    }
});

// 添加服务性能指标端点
app.MapGet("/metrics/services", async (IServiceProvider services) =>
{
    try
    {
        var metrics = await services.GetServiceMetricsAsync();
        return Results.Ok(metrics);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Service Metrics Failed"
        );
    }
});

// 添加API信息端点
app.MapGet("/api/info", () => new {
    Name = "BlazorWebGame Server API",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow
});

try
{
    Log.Information("Starting BlazorWebGame Server");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
