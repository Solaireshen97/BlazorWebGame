using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.Models;
using System.Text.Json;

namespace BlazorWebGame.Server.Tests;

/// <summary>
/// 用户服务测试 - 测试用户注册、登录和数据存储功能
/// </summary>
public static class UserServiceTests
{
    /// <summary>
    /// 运行用户服务基础测试
    /// </summary>
    public static async Task RunBasicUserServiceTests(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("开始运行用户服务基础测试...");

        try
        {
            await TestUserRegistration(serviceProvider, logger);
            await TestUserLogin(serviceProvider, logger);
            await TestUserManagement(serviceProvider, logger);
            
            logger.LogInformation("✅ 用户服务基础测试完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ 用户服务测试失败");
            throw;
        }
    }

    /// <summary>
    /// 测试用户注册功能
    /// </summary>
    private static async Task TestUserRegistration(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🧪 测试用户注册功能...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidatedGameDbContext>();
        var userService = new UserService(context, scope.ServiceProvider.GetRequiredService<ILogger<UserService>>());

        // 清理测试数据
        await CleanupTestUsers(context, logger);

        // 测试1: 创建新用户
        var testUsername = "testuser_" + Guid.NewGuid().ToString("N")[..8];
        var testEmail = $"{testUsername}@test.com";
        var testPassword = "testpass123";

        var user = await userService.CreateUserAsync(testUsername, testEmail, testPassword);
        
        if (user == null || string.IsNullOrEmpty(user.Id))
        {
            throw new Exception("用户创建失败");
        }

        logger.LogInformation("✅ 用户创建成功: {Username} (ID: {UserId})", user.Username, user.Id);

        // 测试2: 验证用户数据
        var createdUser = await userService.GetByIdAsync(user.Id);
        if (createdUser == null || createdUser.Username != testUsername)
        {
            throw new Exception("用户数据验证失败");
        }

        // 测试3: 验证密码
        var validatedUser = await userService.ValidateUserAsync(testUsername, testPassword);
        if (validatedUser == null || validatedUser.Id != user.Id)
        {
            throw new Exception("密码验证失败");
        }

        // 测试4: 验证用户名唯一性
        var isUsernameAvailable = await userService.IsUsernameAvailableAsync(testUsername);
        if (isUsernameAvailable)
        {
            throw new Exception("用户名唯一性检查失败");
        }

        logger.LogInformation("✅ 用户注册功能测试通过");
    }

    /// <summary>
    /// 测试用户登录功能
    /// </summary>
    private static async Task TestUserLogin(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🧪 测试用户登录功能...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidatedGameDbContext>();
        var userService = new UserService(context, scope.ServiceProvider.GetRequiredService<ILogger<UserService>>());

        // 创建测试用户
        var testUsername = "logintest_" + Guid.NewGuid().ToString("N")[..8];
        var testEmail = $"{testUsername}@test.com";
        var testPassword = "loginpass123";

        var user = await userService.CreateUserAsync(testUsername, testEmail, testPassword, new List<string> { "Player", "Tester" });

        // 测试1: 正确凭据登录
        var validatedUser = await userService.ValidateUserAsync(testUsername, testPassword);
        if (validatedUser == null || validatedUser.Id != user.Id)
        {
            throw new Exception("正确凭据登录失败");
        }

        // 测试2: 错误密码登录
        var invalidUser = await userService.ValidateUserAsync(testUsername, "wrongpassword");
        if (invalidUser != null)
        {
            throw new Exception("错误密码应该登录失败");
        }

        // 测试3: 不存在的用户登录
        var nonExistentUser = await userService.ValidateUserAsync("nonexistent", testPassword);
        if (nonExistentUser != null)
        {
            throw new Exception("不存在的用户应该登录失败");
        }

        // 测试4: 更新最后登录时间
        var originalLoginTime = validatedUser.LastLoginAt;
        await Task.Delay(100); // 确保时间差异
        await userService.UpdateLastLoginAsync(user.Id);

        var updatedUser = await userService.GetByIdAsync(user.Id);
        if (updatedUser?.LastLoginAt <= originalLoginTime)
        {
            throw new Exception("最后登录时间更新失败");
        }

        logger.LogInformation("✅ 用户登录功能测试通过");
    }

    /// <summary>
    /// 测试用户管理功能
    /// </summary>
    private static async Task TestUserManagement(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🧪 测试用户管理功能...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidatedGameDbContext>();
        var userService = new UserService(context, scope.ServiceProvider.GetRequiredService<ILogger<UserService>>());

        // 创建测试用户
        var testUsername = "mgmttest_" + Guid.NewGuid().ToString("N")[..8];
        var testEmail = $"{testUsername}@test.com";
        var testPassword = "mgmtpass123";

        var user = await userService.CreateUserAsync(testUsername, testEmail, testPassword);

        // 测试1: 更新刷新令牌
        var refreshToken = Guid.NewGuid().ToString();
        var expiryTime = DateTime.UtcNow.AddDays(7);
        
        var tokenUpdateResult = await userService.UpdateRefreshTokenAsync(user.Id, refreshToken, expiryTime);
        if (!tokenUpdateResult)
        {
            throw new Exception("刷新令牌更新失败");
        }

        var updatedUser = await userService.GetByIdAsync(user.Id);
        if (updatedUser?.RefreshToken != refreshToken || updatedUser.RefreshTokenExpiryTime != expiryTime)
        {
            throw new Exception("刷新令牌验证失败");
        }

        // 测试2: 停用用户
        var deactivateResult = await userService.DeactivateUserAsync(user.Id);
        if (!deactivateResult)
        {
            throw new Exception("用户停用失败");
        }

        var deactivatedUser = await userService.GetByIdAsync(user.Id);
        if (deactivatedUser != null) // 应该返回null因为用户被停用
        {
            throw new Exception("停用用户应该无法获取");
        }

        // 测试3: 邮箱查询
        var testEmailUser = "emailtest_" + Guid.NewGuid().ToString("N")[..8];
        var testEmailAddr = $"{testEmailUser}@email.test";
        await userService.CreateUserAsync(testEmailUser, testEmailAddr, "emailpass123");

        var userByEmail = await userService.GetByEmailAsync(testEmailAddr);
        if (userByEmail == null || userByEmail.Username != testEmailUser)
        {
            throw new Exception("邮箱查询用户失败");
        }

        logger.LogInformation("✅ 用户管理功能测试通过");
    }

    /// <summary>
    /// 清理测试数据
    /// </summary>
    public static async Task CleanupTestUsers(ConsolidatedGameDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("🧹 清理测试用户数据...");

            // 删除测试用户（用户名包含test的用户）
            var testUsers = await context.Users
                .Where(u => u.Username.Contains("test") || u.Email.Contains("test"))
                .ToListAsync();

            if (testUsers.Any())
            {
                context.Users.RemoveRange(testUsers);
                await context.SaveChangesAsync();
                logger.LogInformation("✅ 已清理 {Count} 个测试用户", testUsers.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "清理测试数据时发生警告");
        }
    }

    /// <summary>
    /// 运行性能基准测试
    /// </summary>
    public static async Task RunPerformanceBenchmark(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🏃‍♀️ 开始用户服务性能基准测试...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidatedGameDbContext>();
        var userService = new UserService(context, scope.ServiceProvider.GetRequiredService<ILogger<UserService>>());

        // 清理测试数据
        await CleanupTestUsers(context, logger);

        const int testUserCount = 100;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 批量创建用户测试
            logger.LogInformation("创建 {Count} 个测试用户...", testUserCount);
            var createTasks = new List<Task<UserEntity>>();

            for (int i = 0; i < testUserCount; i++)
            {
                var username = $"perftest_{i:D3}_{Guid.NewGuid():N}"[..20];
                var email = $"{username}@perf.test";
                var password = $"perfpass{i}";

                createTasks.Add(userService.CreateUserAsync(username, email, password));
            }

            var users = await Task.WhenAll(createTasks);
            var createTime = stopwatch.ElapsedMilliseconds;
            logger.LogInformation("✅ 创建 {Count} 用户耗时: {Time}ms (平均 {AvgTime:F2}ms/用户)", 
                testUserCount, createTime, (double)createTime / testUserCount);

            // 批量查询测试
            stopwatch.Restart();
            var queryTasks = users.Select(u => userService.GetByIdAsync(u.Id)).ToArray();
            await Task.WhenAll(queryTasks);
            var queryTime = stopwatch.ElapsedMilliseconds;
            logger.LogInformation("✅ 查询 {Count} 用户耗时: {Time}ms (平均 {AvgTime:F2}ms/用户)", 
                testUserCount, queryTime, (double)queryTime / testUserCount);

            // 批量验证测试
            stopwatch.Restart();
            var validateTasks = users.Select((u, i) => 
                userService.ValidateUserAsync(u.Username, $"perfpass{i}")).ToArray();
            await Task.WhenAll(validateTasks);
            var validateTime = stopwatch.ElapsedMilliseconds;
            logger.LogInformation("✅ 验证 {Count} 用户耗时: {Time}ms (平均 {AvgTime:F2}ms/用户)", 
                testUserCount, validateTime, (double)validateTime / testUserCount);

            logger.LogInformation("🏆 性能基准测试完成");
        }
        finally
        {
            // 清理测试数据
            await CleanupTestUsers(context, logger);
            stopwatch.Stop();
        }
    }
}