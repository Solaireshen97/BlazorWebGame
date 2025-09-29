using BlazorIdleGame.Client;
using BlazorIdleGame.Client.Services.Auth;
using BlazorIdleGame.Client.Services.Core;
using Fluxor;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using BlazorIdleGame.Client.Services.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 载入配置
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.HostEnvironment.Environment}.json", optional: true, reloadOnChange: true);

// 注册配置服务
builder.Services.AddScoped<ApiConfigService, ApiConfigService>();

// 配置 HTTP 客户端 - 使用配置服务
builder.Services.AddScoped(sp => {
    var apiConfig = sp.GetRequiredService<ApiConfigService>();
    return new HttpClient { BaseAddress = new Uri(apiConfig.ApiBaseUrl) };
});

// 配置 Fluxor
builder.Services.AddFluxor(options => options
    .ScanAssemblies(typeof(Program).Assembly)
    .UseReduxDevTools());

// 注册其他服务
builder.Services.AddScoped<IGameCommunicationService, GameCommunicationService>();
builder.Services.AddScoped<IGameSyncService, GameSyncService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 设置日志级别（帮助调试）
builder.Logging.SetMinimumLevel(LogLevel.Debug);

await builder.Build().RunAsync();