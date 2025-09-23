using BlazorWebGame;
using BlazorWebGame.Models;
using BlazorWebGame.Services;
using BlazorWebGame.Utils;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<GameStorage>();
// 注册PartyService在GameStateService之前
builder.Services.AddSingleton<PartyService>(sp => {
    // PartyService需要AllCharacters引用，但这是在GameStateService初始化后才有的
    // 我们使用一个空列表初始化，GameStateService会在InitializeAsync中设置正确的引用
    return new PartyService(new List<Player>());
});
builder.Services.AddSingleton<GameStateService>();
builder.Services.AddSingleton<QuestService>();

await builder.Build().RunAsync();