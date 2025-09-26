using BlazorWebGame.Server.Hubs;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Server;
using BlazorWebGame.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加 SignalR
builder.Services.AddSignalR();

// 修改 CORS 支持
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "https://localhost:7051",  // 客户端 HTTPS
                "http://localhost:5190")   // 客户端 HTTP
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 注册共享事件管理器
builder.Services.AddSingleton<BlazorWebGame.Shared.Events.GameEventManager>();

// 注册服务定位器（单例模式）
builder.Services.AddSingleton<ServerServiceLocator>();

// 注册数据存储服务
builder.Services.AddSingleton<BlazorWebGame.Shared.Interfaces.IDataStorageService, DataStorageService>();
builder.Services.AddSingleton<DataStorageIntegrationService>();

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
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

// 配置 SignalR Hub
app.MapHub<GameHub>("/gamehub");

app.Run();
