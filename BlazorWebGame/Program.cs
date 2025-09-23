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
// ע��InventoryService
builder.Services.AddSingleton<InventoryService>();

// ע��PartyService
builder.Services.AddSingleton<PartyService>(sp => {
    return new PartyService(new List<Player>());
});

// ע��CombatService
builder.Services.AddSingleton<CombatService>(sp => {
    var inventoryService = sp.GetRequiredService<InventoryService>();
    return new CombatService(inventoryService, new List<Player>());
});

builder.Services.AddSingleton<QuestService>();
builder.Services.AddSingleton<GameStateService>();
        
await builder.Build().RunAsync();