using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Server.Tests;

namespace BlazorWebGame;

/// <summary>
/// 验证数据存储架构实现的简单测试程序
/// </summary>
public class TestValidation
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 BlazorWebGame 数据存储架构验证");
        Console.WriteLine("=" + new string('=', 50));

        // 创建主机构建器
        var hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // 配置内存数据库用于测试
                services.AddDbContext<ConsolidatedGameDbContext>(options =>
                    options.UseInMemoryDatabase("TestDatabase"));

                // 注册服务
                services.AddScoped<IUserService, UserService>();
                services.AddScoped<IDatabaseCharacterService, DatabaseCharacterService>();
                
                // 配置日志
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });

        using var host = hostBuilder.Build();
        await host.StartAsync();

        var serviceProvider = host.Services;
        var logger = serviceProvider.GetRequiredService<ILogger<TestValidation>>();

        try
        {
            // 确保数据库创建
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ConsolidatedGameDbContext>();
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("✓ 数据库初始化完成");
            }

            // 运行快速验证
            await TestRunner.RunQuickValidation(serviceProvider, logger);

            // 运行完整测试（如果有足够时间）
            if (args.Contains("--full"))
            {
                await TestRunner.RunAllDataStorageTests(serviceProvider, logger);
            }
            else
            {
                // 只运行角色管理测试
                await TestRunner.RunCharacterManagementTests(serviceProvider, logger);
            }

            Console.WriteLine();
            Console.WriteLine("🎉 验证完成！数据存储架构实现成功");
            Console.WriteLine("📚 详细文档请查看: BlazorWebGame数据存储架构完整实现文档.md");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ 验证过程中发生错误");
            Console.WriteLine($"错误: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}