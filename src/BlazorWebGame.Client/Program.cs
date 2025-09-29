using BlazorWebGame;
using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Client.Services;
using BlazorWebGame.Client.Services.Client;
using BlazorWebGame.Client.Services.Client.Configuration;
using BlazorWebGame.Client.Services.Client.Facades;
using BlazorWebGame.Client.Services.Client.Stubs;
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

// 添加新的客户端架构服务
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddSingleton<ClientConfiguration>(sp =>
{
    var configService = sp.GetRequiredService<ConfigurationService>();
    // This will be initialized asynchronously in the main method
    return new ClientConfiguration();
});
builder.Services.AddSingleton<ClientServiceManager>();
builder.Services.AddSingleton<GameServiceFacade>();

// 添加服务器端功能存根 (Empty implementations for server functionality)
builder.Services.AddSingleton<ServerGameStateStub>();
builder.Services.AddSingleton<ServerInventoryStub>();
builder.Services.AddSingleton<ServerCombatStub>();

// 添加统一的服务器配置服务 (保持向后兼容)
builder.Services.AddSingleton<ServerConfigurationService>();
builder.Services.AddSingleton<ConfigurableHttpClientFactory>();

// 配置动态 HttpClient 工厂，支持运行时更改服务器地址
builder.Services.AddSingleton<HttpClient>(sp =>
{
    var factory = sp.GetRequiredService<ConfigurableHttpClientFactory>();
    return factory.GetHttpClient();
});

// === API Service Layer (Organized by functionality) ===
// 注册核心API服务 - 统一管理所有服务器通信
builder.Services.AddSingleton<BattleApiService>();
builder.Services.AddSingleton<CharacterApiService>();
builder.Services.AddSingleton<CharacterStateApiService>();
builder.Services.AddSingleton<PartyApiService>();
builder.Services.AddSingleton<InventoryApiService>();
builder.Services.AddSingleton<EquipmentApiService>();
builder.Services.AddSingleton<ProductionApiServiceNew>();
builder.Services.AddSingleton<QuestApiService>();
builder.Services.AddSingleton<ShopApiService>();
builder.Services.AddSingleton<ReputationApiService>();
builder.Services.AddSingleton<AuthApiService>();
builder.Services.AddSingleton<OfflineSettlementApiService>();
builder.Services.AddSingleton<EnhancedOfflineSettlementApiService>();
builder.Services.AddSingleton<MonitoringApiService>();

// 注册统一的API客户端
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

// 保持向后兼容的旧API服务
builder.Services.AddSingleton<GameApiService>();

// === Client State Management Services ===
// 注册新的客户端状态管理服务
builder.Services.AddSingleton<ClientGameStateService>();
builder.Services.AddSingleton<ClientPartyService>();
builder.Services.AddSingleton<OfflineService>();

// 注册混合服务 (在迁移期间提供服务器/本地切换)
builder.Services.AddSingleton<ClientInventoryApiService>();
builder.Services.AddSingleton<ClientQuestApiService>();
builder.Services.AddSingleton<ClientEquipmentApiService>();
builder.Services.AddSingleton<HybridInventoryService>();
builder.Services.AddSingleton<HybridQuestService>();
builder.Services.AddSingleton<HybridProductionService>();
builder.Services.AddSingleton<HybridCharacterService>();
builder.Services.AddSingleton<HybridEventService>();
builder.Services.AddSingleton<HybridShopService>();

// 服务端集成服务
builder.Services.AddSingleton<ServerCharacterApiService>();
builder.Services.AddSingleton<ServerPlayerApiService>();
builder.Services.AddSingleton<ServerApiTestService>();

// 生产API服务 (向后兼容)
builder.Services.AddSingleton<ProductionApiService>();

// === Legacy Local Services (将逐步迁移到服务器) ===
// 保留共享的玩家列表 (将被服务器状态替代)
var sharedPlayerList = new List<Player>();

// 注册存储系统 (将被服务器数据库替代)
builder.Services.AddSingleton<GameStorage>();

// 注册新的Player接口服务 (现代化的服务设计)
builder.Services.AddSingleton<IPlayerAttributeService, PlayerAttributeService>();
builder.Services.AddSingleton<IPlayerProfessionService, PlayerProfessionService>();
builder.Services.AddSingleton<IPlayerUtilityService, PlayerUtilityService>();

// 注册业务逻辑系统组件 (将被服务器逻辑替代)
builder.Services.AddSingleton<InventoryService>(); // 已标记为过时，仅保留UI状态
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

// 保留原有的GameStateService（标记为需要迁移）
builder.Services.AddSingleton<GameStateService>();

// Build the application
var app = builder.Build();

// Initialize configuration asynchronously
var configService = app.Services.GetRequiredService<ConfigurationService>();
var configuration = await configService.LoadConfigurationAsync();

// Update the registered configuration instance
var configInstance = app.Services.GetRequiredService<ClientConfiguration>();
configInstance.ServerUrl = configuration.ServerUrl;
configInstance.Features = configuration.Features;
configInstance.Timeouts = configuration.Timeouts;
configInstance.OfflineMode = configuration.OfflineMode;

// Initialize the client service manager
var serviceManager = app.Services.GetRequiredService<ClientServiceManager>();

// Register all the new architecture services with the manager
serviceManager.RegisterService(app.Services.GetRequiredService<ServerGameStateStub>());
serviceManager.RegisterService(app.Services.GetRequiredService<ServerInventoryStub>());
serviceManager.RegisterService(app.Services.GetRequiredService<ServerCombatStub>());

// Initialize all services
await serviceManager.InitializeAsync();

await app.RunAsync();