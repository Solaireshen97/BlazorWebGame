using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorWebGame.Refactored;
using Fluxor;
using BlazorWebGame.Refactored.Application.Interfaces;
using BlazorWebGame.Refactored.Infrastructure.Services;
using BlazorWebGame.Refactored.Presentation.State;
using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Serilog;
using Serilog.Core;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 配置日志记录
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.BrowserConsole()
    .CreateLogger();

builder.Logging.AddSerilog(Log.Logger);

// 配置HTTP客户端
var serverBaseUrl = builder.Configuration.GetValue<string>("ServerBaseUrl") ?? "https://localhost:7000";
builder.Services.AddHttpClient("ServerApi", client =>
{
    client.BaseAddress = new Uri(serverBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
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
    options.UseReduxDevTools();
});

// 注册LocalStorage
builder.Services.AddBlazoredLocalStorage();

// 注册MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// 注册应用服务
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IBattleService, BattleService>();
builder.Services.AddScoped<ISignalRService, SignalRService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();
builder.Services.AddScoped<ITimeSyncService, TimeSyncService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();

// 注册Polly重试策略
builder.Services.AddHttpClient<IHttpClientService, HttpClientService>()
    .AddPolicyHandler(RetryPolicyFactory.GetRetryPolicy())
    .AddPolicyHandler(RetryPolicyFactory.GetCircuitBreakerPolicy());

// 配置游戏常量和选项
builder.Services.Configure<GameOptions>(builder.Configuration.GetSection("Game"));
builder.Services.Configure<SignalROptions>(builder.Configuration.GetSection("SignalR"));

var app = builder.Build();

// 初始化认证服务
var authService = app.Services.GetRequiredService<IAuthService>();
if (authService is AuthService auth)
{
    await auth.InitializeAsync();
}

// 初始化Fluxor
var store = app.Services.GetRequiredService<IStore>();
store.InitializeAsync();

// 分发应用程序初始化事件
var dispatcher = app.Services.GetRequiredService<IDispatcher>();
dispatcher.Dispatch(new InitializeApplicationAction());

Log.Information("BlazorWebGame.Refactored application started");
Log.Information("Server API base URL: {ServerBaseUrl}", serverBaseUrl);

await app.RunAsync();
