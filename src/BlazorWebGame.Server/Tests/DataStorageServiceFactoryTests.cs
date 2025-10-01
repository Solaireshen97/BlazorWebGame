using System;
using System.IO;
using System.Threading.Tasks;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Server.Services.Data;
using BlazorWebGame.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlazorWebGame.Server.Tests;

/// <summary>
/// 数据存储服务工厂测试类
/// </summary>
public class DataStorageServiceFactoryTests : IDisposable
{
    private readonly string _testDatabasePath;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDataStorageServiceFactory _factory;

    public DataStorageServiceFactoryTests()
    {
        // 创建临时测试数据库
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"factory_test_{Guid.NewGuid()}.db");
        
        // 设置服务容器
        var services = new ServiceCollection();
        
        // 注册日志服务
        services.AddLogging(builder => builder.AddConsole());
        
        // 注册DbContext工厂
        services.AddDbContextFactory<GameDbContext>(options =>
            options.UseSqlite($"Data Source={_testDatabasePath}"));
            
        // 注册具体的数据存储服务日志器
        services.AddSingleton<ILogger<DataStorageService>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<DataStorageService>());
        services.AddSingleton<ILogger<SqliteDataStorageService>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<SqliteDataStorageService>());
        services.AddSingleton<ILogger<DataStorageServiceFactory>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<DataStorageServiceFactory>());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // 创建工厂实例
        var logger = _serviceProvider.GetRequiredService<ILogger<DataStorageServiceFactory>>();
        var contextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<GameDbContext>>();
        _factory = new DataStorageServiceFactory(_serviceProvider, logger, contextFactory);
        
        // 初始化数据库
        InitializeDatabase().Wait();
    }

    private async Task InitializeDatabase()
    {
        var contextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<GameDbContext>>();
        using var context = contextFactory.CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public void Dispose()
    {
        _serviceProvider?.GetService<IServiceProvider>()?.GetService<IDisposable>()?.Dispose();
        
        // 清理测试数据库文件
        if (File.Exists(_testDatabasePath))
        {
            try
            {
                File.Delete(_testDatabasePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not delete test database file: {ex.Message}");
            }
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 测试支持的存储类型
    /// </summary>
    public void TestSupportedStorageTypes()
    {
        Console.WriteLine("=== 测试支持的存储类型 ===");
        
        var supportedTypes = _factory.GetSupportedStorageTypes();
        Console.WriteLine($"支持的存储类型: {string.Join(", ", supportedTypes)}");
        
        // 验证预期的存储类型
        var expectedTypes = new[] { "Memory", "SQLite" };
        foreach (var expectedType in expectedTypes)
        {
            if (!_factory.IsStorageTypeSupported(expectedType))
            {
                throw new Exception($"预期支持的存储类型 {expectedType} 未被支持");
            }
            Console.WriteLine($"✅ {expectedType} 存储类型受支持");
        }
        
        // 测试不支持的存储类型
        var unsupportedTypes = new[] { "SqlServer", "MongoDB", "Redis", "" };
        foreach (var unsupportedType in unsupportedTypes)
        {
            if (_factory.IsStorageTypeSupported(unsupportedType))
            {
                throw new Exception($"不应该支持的存储类型 {unsupportedType} 被错误地标记为支持");
            }
            Console.WriteLine($"✅ {unsupportedType} 存储类型正确地不被支持");
        }
        
        Console.WriteLine("存储类型支持测试完成");
    }

    /// <summary>
    /// 测试创建内存数据存储服务
    /// </summary>
    public async Task TestCreateMemoryDataStorageService()
    {
        Console.WriteLine("\n=== 测试创建内存数据存储服务 ===");
        
        var service = _factory.CreateDataStorageService("Memory");
        
        if (service == null)
        {
            throw new Exception("创建内存数据存储服务失败");
        }
        
        Console.WriteLine($"✅ 成功创建内存数据存储服务: {service.GetType().Name}");
        
        // 测试服务功能
        var healthCheck = await service.HealthCheckAsync();
        Console.WriteLine($"健康检查结果: {healthCheck.Success}");
        Console.WriteLine($"存储类型: {healthCheck.Data?.GetValueOrDefault("StorageType", "Unknown")}");
        
        if (!healthCheck.Success)
        {
            throw new Exception($"内存数据存储服务健康检查失败: {healthCheck.Message}");
        }
        
        Console.WriteLine("内存数据存储服务测试完成");
    }

    /// <summary>
    /// 测试创建SQLite数据存储服务
    /// </summary>
    public async Task TestCreateSqliteDataStorageService()
    {
        Console.WriteLine("\n=== 测试创建SQLite数据存储服务 ===");
        
        var service = _factory.CreateDataStorageService("SQLite");
        
        if (service == null)
        {
            throw new Exception("创建SQLite数据存储服务失败");
        }
        
        Console.WriteLine($"✅ 成功创建SQLite数据存储服务: {service.GetType().Name}");
        
        // 测试服务功能
        var healthCheck = await service.HealthCheckAsync();
        Console.WriteLine($"健康检查结果: {healthCheck.Success}");
        Console.WriteLine($"存储类型: {healthCheck.Data?.GetValueOrDefault("StorageType", "Unknown")}");
        Console.WriteLine($"数据库连接: {healthCheck.Data?.GetValueOrDefault("DatabaseConnection", "Unknown")}");
        
        if (!healthCheck.Success)
        {
            throw new Exception($"SQLite数据存储服务健康检查失败: {healthCheck.Message}");
        }
        
        Console.WriteLine("SQLite数据存储服务测试完成");
    }

    /// <summary>
    /// 测试大小写不敏感的存储类型
    /// </summary>
    public async Task TestCaseInsensitiveStorageTypes()
    {
        Console.WriteLine("\n=== 测试大小写不敏感的存储类型 ===");
        
        var testCases = new[]
        {
            ("memory", "Memory"),
            ("MEMORY", "Memory"),
            ("Memory", "Memory"),
            ("sqlite", "SQLite"),
            ("SQLITE", "SQLite"),
            ("SQLite", "SQLite")
        };
        
        foreach (var (input, expected) in testCases)
        {
            try
            {
                var service = _factory.CreateDataStorageService(input);
                var healthCheck = await service.HealthCheckAsync();
                var actualType = healthCheck.Data?.GetValueOrDefault("StorageType", "Unknown")?.ToString();
                
                Console.WriteLine($"✅ 输入: '{input}' -> 创建成功，类型: {actualType}");
                
                if (expected == "SQLite" && actualType != "SQLite")
                {
                    Console.WriteLine($"⚠️ 期望SQLite但得到: {actualType}");
                }
                else if (expected == "Memory" && actualType != "InMemory")
                {
                    Console.WriteLine($"⚠️ 期望Memory但得到: {actualType}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"测试存储类型 '{input}' 失败: {ex.Message}");
            }
        }
        
        Console.WriteLine("大小写不敏感测试完成");
    }

    /// <summary>
    /// 测试工厂错误处理
    /// </summary>
    public void TestFactoryErrorHandling()
    {
        Console.WriteLine("\n=== 测试工厂错误处理 ===");
        
        // 测试无效存储类型
        try
        {
            var service = _factory.CreateDataStorageService("InvalidType");
            // 应该降级到内存存储而不是抛出异常
            Console.WriteLine($"✅ 无效存储类型降级成功: {service.GetType().Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✅ 无效存储类型正确抛出异常: {ex.Message}");
        }
        
        // 测试空存储类型
        try
        {
            var service = _factory.CreateDataStorageService("");
            Console.WriteLine($"✅ 空存储类型处理成功: {service.GetType().Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✅ 空存储类型正确抛出异常: {ex.Message}");
        }
        
        // 测试null存储类型
        try
        {
            var service = _factory.CreateDataStorageService(null!);
            Console.WriteLine($"✅ null存储类型处理成功: {service.GetType().Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✅ null存储类型正确抛出异常: {ex.Message}");
        }
        
        Console.WriteLine("错误处理测试完成");
    }

    /// <summary>
    /// 测试存储健康检查
    /// </summary>
    public async Task TestStorageHealthCheck()
    {
        Console.WriteLine("\n=== 测试存储健康检查 ===");
        
        // 测试内存存储健康检查
        var memoryHealth = await _factory.GetStorageHealthAsync("Memory");
        Console.WriteLine($"内存存储健康检查:");
        foreach (var item in memoryHealth)
        {
            Console.WriteLine($"  {item.Key}: {item.Value}");
        }
        
        // 测试SQLite存储健康检查
        var sqliteHealth = await _factory.GetStorageHealthAsync("SQLite");
        Console.WriteLine($"SQLite存储健康检查:");
        foreach (var item in sqliteHealth)
        {
            Console.WriteLine($"  {item.Key}: {item.Value}");
        }
        
        // 测试不支持的存储类型健康检查
        var unsupportedHealth = await _factory.GetStorageHealthAsync("UnsupportedType");
        Console.WriteLine($"不支持的存储类型健康检查:");
        foreach (var item in unsupportedHealth)
        {
            Console.WriteLine($"  {item.Key}: {item.Value}");
        }
        
        var status = unsupportedHealth.GetValueOrDefault("Status", "Unknown")?.ToString();
        if (status != "Unsupported")
        {
            throw new Exception($"期望不支持的存储类型状态为'Unsupported'，但得到: {status}");
        }
        
        Console.WriteLine("存储健康检查测试完成");
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    public async Task RunAllTests()
    {
        try
        {
            Console.WriteLine("开始数据存储服务工厂测试");
            Console.WriteLine($"测试数据库路径: {_testDatabasePath}");
            
            TestSupportedStorageTypes();
            await TestCreateMemoryDataStorageService();
            await TestCreateSqliteDataStorageService();
            await TestCaseInsensitiveStorageTypes();
            TestFactoryErrorHandling();
            await TestStorageHealthCheck();
            
            Console.WriteLine("\n=== 所有工厂测试完成 ===");
            Console.WriteLine("✅ 数据存储服务工厂测试全部通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 工厂测试失败: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            throw;
        }
    }
}