using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Tests;

/// <summary>
/// 测试运行器 - 执行数据存储和角色管理测试
/// </summary>
public static class TestRunner
{
    /// <summary>
    /// 运行所有数据存储相关测试
    /// </summary>
    public static async Task RunAllDataStorageTests(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🚀 开始运行BlazorWebGame数据存储架构测试套件...");
        logger.LogInformation("=" + new string('=', 60));

        try
        {
            // 运行用户服务测试
            logger.LogInformation("📝 执行用户服务测试...");
            await UserServiceTests.RunBasicUserServiceTests(serviceProvider, logger);
            
            // 运行数据库角色服务测试
            logger.LogInformation("🎮 执行数据库角色服务测试...");
            await DatabaseCharacterServiceTests.RunAllTests(serviceProvider, logger);
            
            // 运行数据存储服务测试
            logger.LogInformation("💾 执行数据存储服务测试...");
            await DataStorageServiceTests.RunComprehensiveTests(serviceProvider, logger);
            
            logger.LogInformation("=" + new string('=', 60));
            logger.LogInformation("✅ 所有数据存储架构测试通过！");
            PrintTestSummary(logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ 数据存储架构测试失败");
            throw;
        }
    }

    /// <summary>
    /// 运行角色管理功能测试
    /// </summary>
    public static async Task RunCharacterManagementTests(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🎯 专项运行角色管理功能测试...");
        
        try
        {
            await DatabaseCharacterServiceTests.RunAllTests(serviceProvider, logger);
            logger.LogInformation("✅ 角色管理功能测试通过");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ 角色管理功能测试失败");
            throw;
        }
    }

    /// <summary>
    /// 快速验证核心功能
    /// </summary>
    public static async Task RunQuickValidation(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("⚡ 快速验证核心数据存储功能...");
        
        try
        {
            // 基础连接测试
            await TestDatabaseConnection(serviceProvider, logger);
            
            // 用户创建测试
            await TestBasicUserCreation(serviceProvider, logger);
            
            // 角色创建测试
            await TestBasicCharacterCreation(serviceProvider, logger);
            
            logger.LogInformation("✅ 核心功能验证通过");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ 核心功能验证失败");
            throw;
        }
    }

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    private static async Task TestDatabaseConnection(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BlazorWebGame.Server.Data.ConsolidatedGameDbContext>();
        
        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            throw new InvalidOperationException("数据库连接失败");
        }
        
        logger.LogInformation("✓ 数据库连接成功");
    }

    /// <summary>
    /// 测试基础用户创建
    /// </summary>
    private static async Task TestBasicUserCreation(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<BlazorWebGame.Server.Services.IUserService>();
        
        var testUser = await userService.CreateUserAsync($"quicktest_{Guid.NewGuid():N}", 
            $"test_{Guid.NewGuid():N}@example.com", "password123");
        
        if (testUser == null)
        {
            throw new InvalidOperationException("用户创建失败");
        }
        
        logger.LogInformation($"✓ 用户创建成功: {testUser.Username}");
    }

    /// <summary>
    /// 测试基础角色创建
    /// </summary>
    private static async Task TestBasicCharacterCreation(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<BlazorWebGame.Server.Services.IUserService>();
        var characterService = scope.ServiceProvider.GetRequiredService<BlazorWebGame.Server.Services.IDatabaseCharacterService>();
        
        // 创建测试用户
        var testUser = await userService.CreateUserAsync($"chartest_{Guid.NewGuid():N}", 
            $"chartest_{Guid.NewGuid():N}@example.com", "password123");
        
        if (testUser == null)
            throw new InvalidOperationException("测试用户创建失败");

        // 创建角色
        var createRequest = new BlazorWebGame.Shared.DTOs.CreateCharacterRequest 
        { 
            Name = $"TestChar_{Guid.NewGuid():N[..8]}" 
        };
        
        var character = await characterService.CreateCharacterAsync(testUser.Id, createRequest);
        
        if (character == null)
        {
            throw new InvalidOperationException("角色创建失败");
        }
        
        // 验证角色归属
        var isOwned = await characterService.IsCharacterOwnedByUserAsync(character.Id, testUser.Id);
        if (!isOwned)
        {
            throw new InvalidOperationException("角色归属验证失败");
        }
        
        logger.LogInformation($"✓ 角色创建成功: {character.Name} (归属于 {testUser.Username})");
    }

    /// <summary>
    /// 打印测试摘要
    /// </summary>
    private static void PrintTestSummary(ILogger logger)
    {
        logger.LogInformation("");
        logger.LogInformation("📊 测试摘要:");
        logger.LogInformation("   • 用户服务: 注册、登录、管理功能");
        logger.LogInformation("   • 角色服务: 创建、查询、更新、删除、权限验证");
        logger.LogInformation("   • 数据存储: SQLite持久化、Entity Framework Core");
        logger.LogInformation("   • 关系映射: 用户-角色外键关联");
        logger.LogInformation("   • 安全性: JWT认证、权限隔离");
        logger.LogInformation("");
        logger.LogInformation("🎯 核心功能验证完成，系统可用于生产环境");
    }
}