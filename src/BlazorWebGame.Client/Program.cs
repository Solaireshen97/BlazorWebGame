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
using BlazorWebGame.Shared.Interfaces;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 配置日志服务 - 对于Blazor WebAssembly，默认已经配置了控制台日志
builder.Logging.SetMinimumLevel(LogLevel.Information);

// 添加统一的服务器配置服务
builder.Services.AddSingleton<ServerConfigurationService>();
builder.Services.AddSingleton<ConfigurableHttpClientFactory>();

// 配置动态 HttpClient 工厂，支持运行时更改服务器地址
builder.Services.AddSingleton<HttpClient>(sp =>
{
    var factory = sp.GetRequiredService<ConfigurableHttpClientFactory>();
    return factory.GetHttpClient();
});

// 添加新的组织化API服务 - 暂时不使用接口，避免类型冲突
// 直接注册实现类，保持功能完整性
builder.Services.AddSingleton<BattleApiService>();
builder.Services.AddSingleton<CharacterApiService>();
builder.Services.AddSingleton<PartyApiService>();
builder.Services.AddSingleton<InventoryApiService>();
builder.Services.AddSingleton<EquipmentApiService>();
builder.Services.AddSingleton<ProductionApiServiceNew>();
builder.Services.AddSingleton<QuestApiService>();
builder.Services.AddSingleton<AuthApiService>();
builder.Services.AddSingleton<OfflineSettlementApiService>();
builder.Services.AddSingleton<MonitoringApiService>();

// 注册统一的API客户端（暂时使用构造器直接注入实现类）
builder.Services.AddSingleton<GameApiClient>(sp => new GameApiClient(
    sp.GetRequiredService<BattleApiService>(),
    sp.GetRequiredService<CharacterApiService>(),
    sp.GetRequiredService<PartyApiService>(),
    sp.GetRequiredService<InventoryApiService>(),
    sp.GetRequiredService<EquipmentApiService>(),
    sp.GetRequiredService<ProductionApiServiceNew>(),
    sp.GetRequiredService<QuestApiService>(),
    sp.GetRequiredService<AuthApiService>(),
    sp.GetRequiredService<OfflineSettlementApiService>(),
    sp.GetRequiredService<MonitoringApiService>()
));

// 保持向后兼容的GameApiService
builder.Services.AddSingleton<GameApiService>();

// 其他现有服务
builder.Services.AddSingleton<ClientGameStateService>();
builder.Services.AddSingleton<ClientPartyService>();
builder.Services.AddSingleton<ProductionApiService>(); // 保留原有的生产API服务，向后兼容
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