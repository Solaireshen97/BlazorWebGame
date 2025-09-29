using BlazorWebGame.Server.Hubs;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Server.Security;
using BlazorWebGame.Server.Middleware;
using BlazorWebGame.Server;
using BlazorWebGame.Server.Configuration;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Shared.Models;
using BlazorWebGame.Shared.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;
using Microsoft.Extensions.Options;

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
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BlazorWebGame 数据存储服务 API",
        Version = "v1.0.0",
        Description = "BlazorWebGame 项目的数据存储服务API，支持玩家数据、队伍管理、战斗记录等功能",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "BlazorWebGame Team",
            Email = "support@blazorwebgame.com"
        }
    });

    // 添加 JWT 认证到 Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // 包含 XML 注释文件（如果存在）
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

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
        TimeWindow = securityConfig.RateLimit.IpRateLimit.TimeWindow
    };

    rateLimitOptions.UserRateLimit = new BlazorWebGame.Server.Middleware.RateLimitRule
    {
        MaxRequests = securityConfig.RateLimit.UserRateLimit.MaxRequests,
        TimeWindow = securityConfig.RateLimit.UserRateLimit.TimeWindow
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

// 注册统一服务
builder.Services.AddSingleton<ErrorHandlingService>();
builder.Services.AddSingleton<BlazorWebGame.Server.Services.PerformanceMonitoringService>();
builder.Services.AddSingleton<ServerOptimizationService>();

// 添加健康检查
builder.Services.AddHealthChecks()
    .AddCheck<GameHealthCheckService>("game-health")
    .AddCheck<DataStorageHealthCheck>("data-storage");

// 注册安全服务
builder.Services.AddSingleton<GameAuthenticationService>();
builder.Services.AddSingleton<DemoUserService>();

// 注册共享事件管理器
builder.Services.AddSingleton<BlazorWebGame.Shared.Events.GameEventManager>();

// 注册事件持久化服务（开发环境使用内存实现）
builder.Services.AddSingleton<BlazorWebGame.Shared.Events.IRedisEventPersistence, BlazorWebGame.Shared.Events.InMemoryEventPersistence>();

// 注册统一事件系统
builder.Services.AddSingleton<UnifiedEventService>();

// 注册服务定位器（单例模式）
builder.Services.AddSingleton<ServerServiceLocator>();

// 配置统一的数据存储系统
builder.Services.AddUnifiedDataStorage(builder.Configuration, builder.Environment);

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

// 初始化统一数据存储系统
var unifiedStorageOptions = builder.Configuration.GetSection("UnifiedDataStorage")
    .Get<BlazorWebGame.Server.Configuration.UnifiedDataStorageOptions>() ?? new BlazorWebGame.Server.Configuration.UnifiedDataStorageOptions();

if (unifiedStorageOptions.StorageType.ToLower() is "sqlite")
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        await app.Services.InitializeUnifiedDataStorageAsync(logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize unified data storage system");
    }
}

// 初始化服务定位器
ServerServiceLocator.Initialize(app.Services);

// 在开发环境中运行战斗系统测试（基于配置）
if (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var gameServerOptions = app.Services.GetRequiredService<IOptions<GameServerOptions>>().Value;
    
    if (gameServerOptions.EnableDevelopmentTests)
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

        // 运行SQLite数据存储测试（如果启用了SQLite）
        if (unifiedStorageOptions.StorageType.ToLower() is "sqlite")
        {
            try
            {
                await BlazorWebGame.Server.Tests.SqliteDataStorageServiceTests.RunBasicTests(app.Services, logger);
                
                // 清理测试数据
                await BlazorWebGame.Server.Tests.SqliteDataStorageServiceTests.CleanupTestData(app.Services, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SQLite data storage test failed");
            }
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
