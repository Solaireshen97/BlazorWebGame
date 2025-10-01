using BlazorIdleGame.Client;
using BlazorIdleGame.Client.Services.Activity;
using BlazorIdleGame.Client.Services.Auth;
using BlazorIdleGame.Client.Services.Battle;
using BlazorIdleGame.Client.Services.Character;
using BlazorIdleGame.Client.Services.Core;
using BlazorIdleGame.Client.Services.Skill;
using BlazorIdleGame.Client.Services.Time;
using Fluxor;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ��������
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.HostEnvironment.Environment}.json", optional: true, reloadOnChange: true);

// ע�����÷���
builder.Services.AddScoped<ApiConfigService, ApiConfigService>();

// ���� HTTP �ͻ��� - ʹ�����÷���
builder.Services.AddScoped(sp => {
    var apiConfig = sp.GetRequiredService<ApiConfigService>();
    return new HttpClient { BaseAddress = new Uri(apiConfig.ApiBaseUrl) };
});

// ���� Fluxor
builder.Services.AddFluxor(options => options
    .ScanAssemblies(typeof(Program).Assembly)
    .UseReduxDevTools());

// ע����������
// ʱ�����
builder.Services.AddSingleton<IGameTimeService, GameTimeService>();
// �����
builder.Services.AddScoped<IActivityService, ActivityService>();
// ս������
builder.Services.AddScoped<IBattleService, BattleService>();
builder.Services.AddScoped<IGameCommunicationService, GameCommunicationService>();
builder.Services.AddScoped<IGameSyncService, GameSyncService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEnhancedCharacterService, EnhancedCharacterService>();
// ���ܷ���
builder.Services.AddScoped<ISkillService, SkillService>();

// ������־���𣨰������ԣ�
builder.Logging.SetMinimumLevel(LogLevel.Debug);

await builder.Build().RunAsync();