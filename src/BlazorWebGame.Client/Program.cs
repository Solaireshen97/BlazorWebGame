using BlazorWebGame;
using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Models;
using BlazorWebGame.Services;
using BlazorWebGame.Utils;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Collections.Generic;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 配置HTTP客户端，指向服务器
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7000") });

// 添加新的API服务
builder.Services.AddScoped<GameApiService>();
builder.Services.AddScoped<ClientGameStateService>();
builder.Services.AddScoped<OfflineService>();

// 保留共享的玩家列表
var sharedPlayerList = new List<Player>();

// 注册存储系统
builder.Services.AddSingleton<GameStorage>();

// 注册业务逻辑系统组件
builder.Services.AddSingleton<InventoryService>();
builder.Services.AddSingleton<PartyService>(sp => new PartyService(sharedPlayerList));
builder.Services.AddSingleton<CombatService>(sp => {
    var inventoryService = sp.GetRequiredService<InventoryService>();
    return new CombatService(inventoryService, sharedPlayerList);
});
builder.Services.AddSingleton<ProfessionService>(sp => {
    var inventoryService = sp.GetRequiredService<InventoryService>();
    var questService = sp.GetRequiredService<QuestService>();
    return new ProfessionService(inventoryService, questService);
});
builder.Services.AddSingleton<QuestService>(sp => {
    var inventoryService = sp.GetRequiredService<InventoryService>();
    return new QuestService(inventoryService);
});

// 注册角色服务
builder.Services.AddSingleton<CharacterService>(sp => {
    var gameStorage = sp.GetRequiredService<GameStorage>();
    var combatService = sp.GetRequiredService<CombatService>();
    return new CharacterService(gameStorage, combatService);
});

// 保留原有的GameStateService（可能需要逐步迁移）
builder.Services.AddSingleton<GameStateService>();

await builder.Build().RunAsync();