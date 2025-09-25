using BlazorWebGame.Server.Hubs;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Server;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加 SignalR
builder.Services.AddSignalR();

// 修改 CORS 支持
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "https://localhost:7051",  // 客户端 HTTPS
                "http://localhost:5190")   // 客户端 HTTP
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 注册共享事件管理器
builder.Services.AddSingleton<BlazorWebGame.Shared.Events.GameEventManager>();

// 注册游戏服务
builder.Services.AddSingleton<ServerSkillSystem>();
builder.Services.AddSingleton<ServerLootService>();
builder.Services.AddSingleton<ServerCombatEngine>();
builder.Services.AddSingleton<ServerPartyService>();
builder.Services.AddSingleton<GameEngineService>();
builder.Services.AddSingleton<ServerCharacterService>();
builder.Services.AddSingleton<ServerEventService>();
builder.Services.AddHostedService<GameLoopService>();

var app = builder.Build();

// 在开发环境中运行战斗系统测试
if (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    
    // 运行战斗系统测试
    try
    {
        TestBattleSystem.RunBattleTest(app.Services, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Battle system test failed");
    }
    
    // 运行组队系统测试
    try
    {
        TestPartySystem.RunPartyTest(logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Party system test failed");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

// 配置 SignalR Hub
app.MapHub<GameHub>("/gamehub");

app.Run();
