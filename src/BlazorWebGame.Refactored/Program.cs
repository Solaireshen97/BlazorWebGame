using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorWebGame.Refactored;
using Fluxor;
using BlazorWebGame.Refactored.Application.Interfaces;
using BlazorWebGame.Refactored.Infrastructure.Services;
using BlazorWebGame.Refactored.Infrastructure.SignalR;
using BlazorWebGame.Refactored.Infrastructure.Http;
using BlazorWebGame.Refactored.Infrastructure.Cache;
using BlazorWebGame.Refactored.Infrastructure.Persistence;
using BlazorWebGame.Refactored.Application.Behaviors;
using BlazorWebGame.Refactored.Presentation.State;
using BlazorWebGame.Refactored.Utils;
using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Serilog;
using Serilog.Core;
using MediatR;
using FluentValidation;
using Polly;
using Polly.Extensions.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 配置日志记录
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.BrowserConsole()
    .CreateLogger();

builder.Logging.AddSerilog(Log.Logger);

// 配置HTTP客户端（带Polly重试策略）
var serverBaseUrl = builder.Configuration.GetValue<string>("ServerBaseUrl") ?? "http://localhost:5239";
builder.Services.AddHttpClient("ServerApi", client =>
{
    client.BaseAddress = new Uri(serverBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler((services, request) => 
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    return RetryPolicies.GetCombinedPolicy(logger);
});

builder.Services.AddScoped(sp => 
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return httpClientFactory.CreateClient("ServerApi");
});

// 配置Fluxor状态管理
builder.Services.AddFluxor(options =>
{
    options.ScanAssemblies(typeof(Program).Assembly);
    // Redux DevTools is not available in WebAssembly by default
});

// 注册LocalStorage
builder.Services.AddBlazoredLocalStorage();

// 注册内存缓存
builder.Services.AddMemoryCache();

// 注册MediatR with Pipeline Behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// 注册FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// 注册基础设施服务
builder.Services.AddScoped<BlazorWebGame.Refactored.Application.Interfaces.ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IDataPersistenceService, IndexedDbRepository>();
builder.Services.AddScoped<ICacheService, MultiLevelCacheService>();
builder.Services.AddScoped<IHttpClientService, GameApiClient>();
builder.Services.AddScoped<ISignalRService, GameHubClient>();

// 注册应用服务
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IBattleService, BattleService>();
builder.Services.AddScoped<ITimeSyncService, TimeSyncService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();

// 注册示例数据服务（开发调试用）
builder.Services.AddScoped<SampleDataService>();

// 配置游戏常量和选项 - 简化配置
// builder.Services.Configure<GameOptions>(options => { });
// builder.Services.Configure<SignalROptions>(options => { });

var app = builder.Build();

// 初始化数据持久化服务
var persistenceService = app.Services.GetRequiredService<IDataPersistenceService>();
await persistenceService.InitializeAsync();

// 初始化认证服务
var authService = app.Services.GetRequiredService<IAuthService>();
if (authService is AuthService auth)
{
    await auth.InitializeAsync();
}

// 初始化SignalR连接
var signalRService = app.Services.GetRequiredService<ISignalRService>();
if (signalRService is GameHubClient hubClient)
{
    try
    {
        await hubClient.StartAsync();
        Log.Information("SignalR connection established successfully");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to establish SignalR connection - continuing in offline mode");
    }
}

// 分发应用程序初始化事件
var dispatcher = app.Services.GetRequiredService<IDispatcher>();
dispatcher.Dispatch(new InitializeApplicationAction());

Log.Information("BlazorWebGame.Refactored application started");
Log.Information("Server API base URL: {ServerBaseUrl}", serverBaseUrl);
Log.Information("Architecture: Clean Architecture with CQRS, SignalR, and Multi-level Caching");

await app.RunAsync();
