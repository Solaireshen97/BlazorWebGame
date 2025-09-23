using BlazorWebGame;
using BlazorWebGame.Models;
using BlazorWebGame.Services;
using BlazorWebGame.Utils;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Collections.Generic;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<GameStorage>();
// 注册InventoryService
builder.Services.AddSingleton<InventoryService>();
// 注册PartyService在GameStateService之前
builder.Services.AddSingleton<PartyService>(sp => {
    // PartyService需要AllCharacters引用，但这是在GameStateService初始化后才有的
    return new PartyService(new List<Player>());
});
builder.Services.AddSingleton<GameStateService>();
builder.Services.AddSingleton<QuestService>();

await builder.Build().RunAsync();