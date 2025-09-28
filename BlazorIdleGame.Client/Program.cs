using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorIdleGame.Client;
using Fluxor;
using BlazorIdleGame.Client.Services.Core;
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
builder.Services.AddScoped<IGameCommunicationService, GameCommunicationService>();
builder.Services.AddScoped<IGameSyncService, GameSyncService>();

// ������־���𣨰������ԣ�
builder.Logging.SetMinimumLevel(LogLevel.Debug);

await builder.Build().RunAsync();