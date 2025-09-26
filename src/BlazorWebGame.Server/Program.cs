using BlazorWebGame.Server.Hubs;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Server.Security;
using BlazorWebGame.Server.Middleware;
using BlazorWebGame.Server;
using BlazorWebGame.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
        var allowedOrigins = builder.Configuration.GetSection("Security:Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "https://localhost:7051", "http://localhost:5190" };
            
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 配置速率限制选项
builder.Services.Configure<RateLimitOptions>(options =>
{
    var rateLimitConfig = builder.Configuration.GetSection("Security:RateLimit");
    
    options.IpRateLimit = new RateLimitRule
    {
        MaxRequests = rateLimitConfig.GetValue<int>("IpRateLimit:MaxRequests", 100),
        TimeWindow = TimeSpan.FromMinutes(rateLimitConfig.GetValue<int>("IpRateLimit:TimeWindowMinutes", 1))
    };
    
    options.UserRateLimit = new RateLimitRule
    {
        MaxRequests = rateLimitConfig.GetValue<int>("UserRateLimit:MaxRequests", 200),
        TimeWindow = TimeSpan.FromMinutes(rateLimitConfig.GetValue<int>("UserRateLimit:TimeWindowMinutes", 1))
    };
});

// 注册安全服务
builder.Services.AddSingleton<GameAuthenticationService>();
builder.Services.AddSingleton<DemoUserService>();

// 注册共享事件管理器
builder.Services.AddSingleton<BlazorWebGame.Shared.Events.GameEventManager>();

// 注册服务定位器（单例模式）
builder.Services.AddSingleton<ServerServiceLocator>();

// 注册数据存储服务
builder.Services.AddSingleton<BlazorWebGame.Shared.Interfaces.IDataStorageService, DataStorageService>();
builder.Services.AddSingleton<DataStorageIntegrationService>();

// 注册离线结算服务
builder.Services.AddSingleton<OfflineSettlementService>();

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
builder.Services.AddSingleton<ServerEquipmentService>();  // 添加装备服务
builder.Services.AddSingleton<ServerEquipmentGenerator>(); // 新增装备生成器
builder.Services.AddSingleton<GameEngineService>();
builder.Services.AddSingleton<ServerCharacterService>();
builder.Services.AddSingleton<ServerEventService>();

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

var app = builder.Build();

// 初始化服务定位器
ServerServiceLocator.Initialize(app.Services);

// 在开发环境中运行战斗系统测试
if (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    
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
    
    // 运行数据存储服务测试
    try
    {
        await BlazorWebGame.Server.Tests.DataStorageServiceTests.RunBasicTests(logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "DataStorageService test failed");
    }
    
    // 运行离线结算服务测试
    try
    {
        await BlazorWebGame.Server.Tests.OfflineSettlementServiceTests.RunBasicTests(logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "OfflineSettlementService test failed");
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
app.MapGet("/health", () => new { 
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
