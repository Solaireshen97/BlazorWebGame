using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BlazorWebGame.Server.Tests;

/// <summary>
/// 用户角色关联系统测试
/// </summary>
public static class UserCharacterServiceTests
{
    public static async Task RunComprehensiveTests(ILogger logger)
    {
        logger.LogInformation("Starting User-Character relationship tests...");
        
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dataStorageLogger = loggerFactory.CreateLogger<DataStorageService>();
        var userServiceLogger = loggerFactory.CreateLogger<UserService>();
        var characterServiceLogger = loggerFactory.CreateLogger<ServerCharacterService>();
        
        var dataStorage = new DataStorageService(dataStorageLogger);
        var userService = new UserService(dataStorage, userServiceLogger);
        
        // Create mock services for character service dependencies
        var eventManager = new BlazorWebGame.Shared.Events.GameEventManager();
        var playerAttributeService = new ServerPlayerAttributeService(
            loggerFactory.CreateLogger<ServerPlayerAttributeService>());
        var playerProfessionService = new ServerPlayerProfessionService(
            loggerFactory.CreateLogger<ServerPlayerProfessionService>());
        var playerUtilityService = new ServerPlayerUtilityService(
            loggerFactory.CreateLogger<ServerPlayerUtilityService>());
        
        var characterService = new ServerCharacterService(
            eventManager, 
            characterServiceLogger,
            playerAttributeService,
            playerProfessionService,
            playerUtilityService,
            dataStorage);
        
        try
        {
            // 测试1: 创建用户和角色
            await TestUserAndCharacterCreation(userService, characterService, logger);
            
            // 测试2: 角色所有权验证
            await TestCharacterOwnership(userService, characterService, dataStorage, logger);
            
            // 测试3: 用户角色列表查询
            await TestUserCharacterList(userService, characterService, dataStorage, logger);
            
            // 测试4: 默认角色设置
            await TestDefaultCharacterSetting(dataStorage, logger);
            
            // 测试5: 角色访问权限控制
            await TestCharacterAccessControl(userService, characterService, logger);
            
            logger.LogInformation("All User-Character relationship tests passed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "User-Character relationship tests failed");
            throw;
        }
    }

    private static async Task TestUserAndCharacterCreation(
        UserService userService, 
        ServerCharacterService characterService, 
        ILogger logger)
    {
        logger.LogInformation("Testing user and character creation...");
        
        // 创建用户
        var registrationResult = await userService.RegisterUserAsync("gameuser", "password123", "gameuser@example.com");
        if (!registrationResult.Success)
        {
            throw new Exception($"User registration failed: {registrationResult.Message}");
        }
        
        var user = registrationResult.Data!;
        
        // 创建角色并关联到用户
        var characterRequest = new CreateCharacterRequest { Name = "TestHero" };
        var character = await characterService.CreateCharacterAsync(characterRequest, user.Id);
        
        if (character == null || string.IsNullOrEmpty(character.Id))
        {
            throw new Exception("Character creation failed");
        }
        
        // 验证角色名称正确
        if (character.Name != "TestHero")
        {
            throw new Exception("Character name mismatch");
        }
        
        logger.LogInformation("✓ User and character creation test passed");
    }

    private static async Task TestCharacterOwnership(
        UserService userService, 
        ServerCharacterService characterService,
        DataStorageService dataStorage,
        ILogger logger)
    {
        logger.LogInformation("Testing character ownership...");
        
        // 获取已创建的用户
        var user = await userService.ValidateUserAsync("gameuser", "password123");
        if (user == null)
        {
            throw new Exception("Test user not found");
        }
        
        // 获取用户的角色
        var userCharacters = await characterService.GetUserCharactersAsync(user.Id);
        if (userCharacters.Count == 0)
        {
            throw new Exception("User should have at least one character");
        }
        
        var characterId = userCharacters[0].Id;
        
        // 测试用户拥有该角色
        var ownsCharacter = await characterService.UserOwnsCharacterAsync(user.Id, characterId);
        if (!ownsCharacter)
        {
            throw new Exception("User should own the character");
        }
        
        // 测试其他用户不拥有该角色
        var otherUserResult = await userService.RegisterUserAsync("otheruser", "password123", "other@example.com");
        if (!otherUserResult.Success)
        {
            throw new Exception($"Other user registration failed: {otherUserResult.Message}");
        }
        
        var otherUser = otherUserResult.Data!;
        var doesNotOwnCharacter = await characterService.UserOwnsCharacterAsync(otherUser.Id, characterId);
        if (doesNotOwnCharacter)
        {
            throw new Exception("Other user should not own the character");
        }
        
        logger.LogInformation("✓ Character ownership test passed");
    }

    private static async Task TestUserCharacterList(
        UserService userService,
        ServerCharacterService characterService,
        DataStorageService dataStorage,
        ILogger logger)
    {
        logger.LogInformation("Testing user character list...");
        
        // 获取用户
        var user = await userService.ValidateUserAsync("gameuser", "password123");
        if (user == null)
        {
            throw new Exception("Test user not found");
        }
        
        // 为用户创建第二个角色
        var secondCharacterRequest = new CreateCharacterRequest { Name = "SecondHero" };
        await characterService.CreateCharacterAsync(secondCharacterRequest, user.Id);
        
        // 获取用户角色列表
        var userCharacters = await characterService.GetUserCharactersAsync(user.Id);
        if (userCharacters.Count != 2)
        {
            throw new Exception($"User should have 2 characters, but has {userCharacters.Count}");
        }
        
        // 验证角色名称
        var characterNames = userCharacters.Select(c => c.Name).ToHashSet();
        if (!characterNames.Contains("TestHero") || !characterNames.Contains("SecondHero"))
        {
            throw new Exception("Character names do not match expected values");
        }
        
        logger.LogInformation("✓ User character list test passed");
    }

    private static async Task TestDefaultCharacterSetting(DataStorageService dataStorage, ILogger logger)
    {
        logger.LogInformation("Testing default character setting...");
        
        // Create a test user first for this specific test
        var testUser = new UserStorageDto
        {
            Id = "test-user-for-default",
            Username = "testdefaultuser",
            Email = "testdefault@example.com",
            IsActive = true,
            Roles = new List<string> { "Player" }
        };
        
        var userCreateResult = await dataStorage.CreateUserAsync(testUser, "password123");
        if (!userCreateResult.Success)
        {
            throw new Exception($"Failed to create test user: {userCreateResult.Message}");
        }
        
        var testUserId = userCreateResult.Data!.Id;
        
        // Create test user-character relationships
        var createResult = await dataStorage.CreateUserCharacterAsync(testUserId, "char1", "Character1", true);
        if (!createResult.Success)
        {
            throw new Exception($"Failed to create user-character relationship: {createResult.Message}");
        }
        
        var createResult2 = await dataStorage.CreateUserCharacterAsync(testUserId, "char2", "Character2", false);
        if (!createResult2.Success)
        {
            throw new Exception($"Failed to create second user-character relationship: {createResult2.Message}");
        }
        
        // 设置第二个角色为默认
        var setDefaultResult = await dataStorage.SetDefaultCharacterAsync(testUserId, "char2");
        if (!setDefaultResult.Success)
        {
            throw new Exception($"Failed to set default character: {setDefaultResult.Message}");
        }
        
        // 验证默认角色已更改
        var userCharactersResult = await dataStorage.GetUserCharactersAsync(testUserId);
        if (!userCharactersResult.Success)
        {
            throw new Exception("Failed to get user characters");
        }
        
        var userCharacters = userCharactersResult.Data!;
        var char1 = userCharacters.FirstOrDefault(c => c.CharacterId == "char1");
        var char2 = userCharacters.FirstOrDefault(c => c.CharacterId == "char2");
        
        if (char1?.IsDefault == true || char2?.IsDefault != true)
        {
            throw new Exception("Default character setting failed");
        }
        
        logger.LogInformation("✓ Default character setting test passed");
    }

    private static async Task TestCharacterAccessControl(
        UserService userService,
        ServerCharacterService characterService,
        ILogger logger)
    {
        logger.LogInformation("Testing character access control...");
        
        // 创建管理员用户
        var adminResult = await userService.RegisterUserAsync("adminuser", "admin123", "admin@example.com");
        if (!adminResult.Success)
        {
            throw new Exception($"Admin user registration failed: {adminResult.Message}");
        }
        
        // 手动给管理员添加Admin角色
        var adminUser = adminResult.Data!;
        adminUser.Roles.Add("Admin");
        
        // 获取普通用户的角色
        var regularUser = await userService.ValidateUserAsync("gameuser", "password123");
        if (regularUser == null)
        {
            throw new Exception("Regular user not found");
        }
        
        var userCharacters = await characterService.GetUserCharactersAsync(regularUser.Id);
        if (userCharacters.Count == 0)
        {
            throw new Exception("Regular user should have characters");
        }
        
        var characterId = userCharacters[0].Id;
        
        // 测试管理员可以访问任何角色（通过UserService的UserHasCharacterAsync）
        var adminCanAccess = await userService.UserHasCharacterAsync(adminUser.Id, characterId);
        if (!adminCanAccess)
        {
            throw new Exception("Admin should be able to access any character");
        }
        
        // 测试普通用户只能访问自己的角色
        var userCanAccess = await userService.UserHasCharacterAsync(regularUser.Id, characterId);
        if (!userCanAccess)
        {
            throw new Exception("User should be able to access their own character");
        }
        
        logger.LogInformation("✓ Character access control test passed");
    }
}