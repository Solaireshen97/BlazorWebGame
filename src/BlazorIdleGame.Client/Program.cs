using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorIdleGame.Client;
using BlazorIdleGame.Client.Services;
using Fluxor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 配置 HTTP 客户端
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.Configuration["ServerUrl"] ?? "https://localhost:7001/") 
});

// 注册游戏服务
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IPartyService, PartyService>();
builder.Services.AddScoped<IBattleService, BattleService>();

// 配置 Fluxor 状态管理
builder.Services.AddFluxor(options =>
{
    options.ScanAssemblies(typeof(Program).Assembly);
#if DEBUG
    options.UseReduxDevTools();
#endif
});

// 配置日志
builder.Logging.SetMinimumLevel(LogLevel.Information);

await builder.Build().RunAsync();
