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
// ע��PartyService��GameStateService֮ǰ
builder.Services.AddSingleton<PartyService>(sp => {
    // PartyService��ҪAllCharacters���ã���������GameStateService��ʼ������е�
    // ����ʹ��һ�����б��ʼ����GameStateService����InitializeAsync��������ȷ������
    return new PartyService(new List<Player>());
});
builder.Services.AddSingleton<GameStateService>();
builder.Services.AddSingleton<QuestService>();

await builder.Build().RunAsync();