using Microsoft.Extensions.Logging;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using System;
using System.Threading.Tasks;

namespace BlazorWebGame.Server.Tests;

/// <summary>
/// 用户服务集成测试
/// </summary>
public static class UserServiceTests
{
    public static async Task RunBasicTests(ILogger logger)
    {
        logger.LogInformation("Starting UserService basic tests...");
        
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dataStorageLogger = loggerFactory.CreateLogger<DataStorageService>();
        var userServiceLogger = loggerFactory.CreateLogger<UserService>();
        
        var dataStorage = new DataStorageService(dataStorageLogger);
        var userService = new UserService(dataStorage, userServiceLogger);
        
        try
        {
            // 测试1: 用户注册
            await TestUserRegistration(userService, logger);
            
            // 测试2: 用户登录
            await TestUserLogin(userService, logger);
            
            // 测试3: 密码验证
            await TestPasswordValidation(userService, logger);
            
            // 测试4: 用户角色验证
            await TestUserRoles(userService, logger);
            
            logger.LogInformation("All UserService tests passed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UserService tests failed");
            throw;
        }
    }

    private static async Task TestUserRegistration(UserService userService, ILogger logger)
    {
        logger.LogInformation("Testing user registration...");
        
        // 测试成功注册
        var registrationResult = await userService.RegisterUserAsync("testuser", "password123", "test@example.com");
        if (!registrationResult.Success)
        {
            throw new Exception($"User registration failed: {registrationResult.Message}");
        }
        
        var user = registrationResult.Data!;
        if (user.Username != "testuser" || user.Email != "test@example.com")
        {
            throw new Exception("User registration data mismatch");
        }
        
        // 测试重复用户名注册
        var duplicateResult = await userService.RegisterUserAsync("testuser", "password123", "test2@example.com");
        if (duplicateResult.Success)
        {
            throw new Exception("Duplicate username registration should have failed");
        }
        
        logger.LogInformation("✓ User registration test passed");
    }

    private static async Task TestUserLogin(UserService userService, ILogger logger)
    {
        logger.LogInformation("Testing user login...");
        
        // 测试正确登录
        var user = await userService.ValidateUserAsync("testuser", "password123");
        if (user == null)
        {
            throw new Exception("Valid user login failed");
        }
        
        if (user.Username != "testuser")
        {
            throw new Exception("Login returned wrong user");
        }
        
        // 测试错误密码
        var invalidUser = await userService.ValidateUserAsync("testuser", "wrongpassword");
        if (invalidUser != null)
        {
            throw new Exception("Invalid password login should have failed");
        }
        
        // 测试不存在的用户
        var nonExistentUser = await userService.ValidateUserAsync("nonexistent", "password123");
        if (nonExistentUser != null)
        {
            throw new Exception("Non-existent user login should have failed");
        }
        
        logger.LogInformation("✓ User login test passed");
    }

    private static async Task TestPasswordValidation(UserService userService, ILogger logger)
    {
        logger.LogInformation("Testing password validation...");
        
        // 获取用户
        var user = await userService.ValidateUserAsync("testuser", "password123");
        if (user == null)
        {
            throw new Exception("Unable to get test user for password validation");
        }
        
        // 测试正确密码
        var validPassword = await userService.ValidateUserAsync(user.Username, "password123");
        if (validPassword == null)
        {
            throw new Exception("Valid password validation failed");
        }
        
        // 测试错误密码
        var invalidPassword = await userService.ValidateUserAsync(user.Username, "wrongpassword");
        if (invalidPassword != null)
        {
            throw new Exception("Invalid password should have failed validation");
        }
        
        logger.LogInformation("✓ Password validation test passed");
    }

    private static async Task TestUserRoles(UserService userService, ILogger logger)
    {
        logger.LogInformation("Testing user roles...");
        
        // 获取用户
        var user = await userService.ValidateUserAsync("testuser", "password123");
        if (user == null)
        {
            throw new Exception("Unable to get test user for role testing");
        }
        
        // 检查默认角色
        var hasPlayerRole = await userService.UserHasRoleAsync(user.Id, "Player");
        if (!hasPlayerRole)
        {
            throw new Exception("User should have default Player role");
        }
        
        // 检查不存在的角色
        var hasAdminRole = await userService.UserHasRoleAsync(user.Id, "Admin");
        if (hasAdminRole)
        {
            throw new Exception("User should not have Admin role");
        }
        
        logger.LogInformation("✓ User roles test passed");
    }
}