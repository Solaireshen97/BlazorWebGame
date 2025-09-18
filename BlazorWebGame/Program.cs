using BlazorWebGame;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorWebGame.Utils;
using BlazorWebGame.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// 注册游戏存储和状态服务为单例
builder.Services.AddSingleton<GameStorage>();
builder.Services.AddSingleton<GameStateService>();

await builder.Build().RunAsync();
