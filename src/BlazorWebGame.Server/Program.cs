using BlazorWebGame.Server.Hubs;
using BlazorWebGame.Server.Services;

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

// 注册游戏服务
builder.Services.AddSingleton<GameEngineService>();
builder.Services.AddHostedService<GameLoopService>();

var app = builder.Build();

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
