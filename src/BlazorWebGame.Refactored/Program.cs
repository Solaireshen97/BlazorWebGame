using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorWebGame.Refactored;
using BlazorWebGame.Refactored.Infrastructure.Events.Core;
using BlazorWebGame.Refactored.Application;
using BlazorWebGame.Refactored.Application.Systems;
using BlazorWebGame.Refactored.Application.Services;
using BlazorWebGame.Refactored.Infrastructure.Persistence;
using BlazorWebGame.Refactored.Application.Interfaces;
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
builder.Services.AddSingleton<IEventBus, EventBus>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// ========== 存储层 ==========
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddScoped<IDataPersistenceService, IndexedDbRepository>();

// ========== 游戏系统 ==========
builder.Services.AddSingleton<IGameSystem, ActivitySystem>();

// ========== 应用服务 ==========
builder.Services.AddSingleton<IGameStateManager, GameStateManager>();
builder.Services.AddSingleton<GameEngine>();

var app = builder.Build();

// 初始化游戏
await InitializeGameAsync(app.Services);

Log.Information("BlazorWebGame.Refactored application started with Event-Driven Architecture");
Log.Information("Architecture: Event-Driven with GameEngine and Game Systems");

await app.RunAsync();

static async Task InitializeGameAsync(IServiceProvider services)
{
    var eventBus = services.GetRequiredService<IEventBus>();
    var stateManager = services.GetRequiredService<IGameStateManager>();
    var gameEngine = services.GetRequiredService<GameEngine>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // 启动游戏引擎
        await gameEngine.StartAsync();
        
        // 创建默认用户ID（简化版本）
        var playerId = "default-player";
        
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
        logger.LogError(ex, "Failed to initialize game");
    }
}
