using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorWebGame.Refactored;
using BlazorWebGame.Refactored.Infrastructure.Events.Core;
using BlazorWebGame.Refactored.Application;
using BlazorWebGame.Refactored.Application.Systems;
using BlazorWebGame.Refactored.Application.Services;
using BlazorWebGame.Refactored.Infrastructure.Persistence;
using BlazorWebGame.Refactored.Application.Interfaces;
using BlazorWebGame.Refactored.Infrastructure.Services;
using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Serilog;
using BlazorWebGame.Refactored.Domain.Entities;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 配置日志记录
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.BrowserConsole()
    .CreateLogger();

builder.Logging.AddSerilog(Log.Logger);

// ========== 配置选项 ==========
builder.Services.Configure<GameEngineOptions>(
    builder.Configuration.GetSection("GameEngine"));

// ========== 基础设施 ==========
builder.Services.AddScoped<IEventBus, EventBus>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddBlazoredLocalStorage();

// ========== 存储层 ==========
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddScoped<IDataPersistenceService, IndexedDbRepository>();

// ========== 游戏系统 ==========
builder.Services.AddScoped<IGameSystem, ActivitySystem>();

// ========== 应用服务 ==========
builder.Services.AddScoped<IGameStateManager, GameStateManager>();
builder.Services.AddScoped<GameEngine>();

var app = builder.Build();

// 初始化游戏
await InitializeGameAsync(app.Services);

Log.Information("BlazorWebGame.Refactored application started with Event-Driven Architecture");
Log.Information("Architecture: Event-Driven with GameEngine and Game Systems");

await app.RunAsync();

static async Task InitializeGameAsync(IServiceProvider services)
{
    try
    {
        using var scope = services.CreateScope();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var stateManager = scope.ServiceProvider.GetRequiredService<IGameStateManager>();
        var gameEngine = scope.ServiceProvider.GetRequiredService<GameEngine>();
        var persistenceService = scope.ServiceProvider.GetRequiredService<IDataPersistenceService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // 初始化数据持久化服务
        await persistenceService.InitializeAsync();
        
        // 启动游戏引擎
        await gameEngine.StartAsync();
        
        // 创建默认用户ID（使用固定的GUID）
        var playerId = "550e8400-e29b-41d4-a716-446655440000"; // 固定的测试用户GUID
        
        // 检查是否已有角色，如果没有则创建一个
        var characters = await stateManager.GetUserCharactersAsync(playerId);
        if (!characters.Any())
        {
            await stateManager.CreateCharacterAsync("默认角色", playerId);
            characters = await stateManager.GetUserCharactersAsync(playerId);
        }
        
        // 加载第一个角色
        if (characters.Any())
        {
            await stateManager.LoadCharacterAsync(characters.First().Id.ToString());
        }
        
        // 发布游戏就绪事件
        await eventBus.PublishAsync(new BlazorWebGame.Refactored.Domain.Events.GameReadyEvent());
        
        logger.LogInformation("Game initialized successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize game");
    }
}
