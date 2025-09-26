using BlazorWebGame;
using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Client.Services;
using BlazorWebGame.Models;
using BlazorWebGame.Services;
using BlazorWebGame.Services.PlayerServices;
using BlazorWebGame.Utils;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 配置日志服务 - 对于Blazor WebAssembly，默认已经配置了控制台日志
builder.Logging.SetMinimumLevel(LogLevel.Information);

// 配置HTTP客户端，指向服务器（与服务器实际运行端口匹配）
builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7000") });

// 添加新的API服务
builder.Services.AddSingleton<GameApiService>();
builder.Services.AddSingleton<ClientGameStateService>();
builder.Services.AddSingleton<ClientPartyService>();
builder.Services.AddSingleton<ProductionApiService>();
builder.Services.AddSingleton<HybridProductionService>();
builder.Services.AddSingleton<OfflineService>();

// 添加新的库存和任务API服务
builder.Services.AddSingleton<ClientInventoryApiService>();
builder.Services.AddSingleton<ClientQuestApiService>();
builder.Services.AddSingleton<ClientEquipmentApiService>();
builder.Services.AddSingleton<HybridInventoryService>();
builder.Services.AddSingleton<HybridQuestService>();

// 添加服务端集成服务
builder.Services.AddSingleton<ServerCharacterApiService>();
builder.Services.AddSingleton<HybridCharacterService>();
builder.Services.AddSingleton<HybridEventService>();
builder.Services.AddSingleton<ServerPlayerApiService>();

// 添加服务端API测试服务
builder.Services.AddSingleton<ServerApiTestService>();

// 保留共享的玩家列表
var sharedPlayerList = new List<Player>();

// 注册存储系统
builder.Services.AddSingleton<GameStorage>();

// 注册新的Player服务
builder.Services.AddSingleton<IPlayerAttributeService, PlayerAttributeService>();
builder.Services.AddSingleton<IPlayerProfessionService, PlayerProfessionService>();
builder.Services.AddSingleton<IPlayerUtilityService, PlayerUtilityService>();

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

// 注册角色服务（现在使用新的Player服务）
builder.Services.AddSingleton<CharacterService>(sp => {
    var gameStorage = sp.GetRequiredService<GameStorage>();
    var combatService = sp.GetRequiredService<CombatService>();
    var playerAttributeService = sp.GetRequiredService<IPlayerAttributeService>();
    var playerProfessionService = sp.GetRequiredService<IPlayerProfessionService>();
    var playerUtilityService = sp.GetRequiredService<IPlayerUtilityService>();
    return new CharacterService(gameStorage, combatService, playerAttributeService, playerProfessionService, playerUtilityService);
});

// 保留原有的GameStateService（可能需要逐步迁移）
builder.Services.AddSingleton<GameStateService>();

await builder.Build().RunAsync();