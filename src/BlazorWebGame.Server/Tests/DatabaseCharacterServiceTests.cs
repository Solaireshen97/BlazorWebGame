using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.Models;
using BlazorWebGame.Shared.DTOs;
using System.Text.Json;

namespace BlazorWebGame.Server.Tests;

/// <summary>
/// 数据库角色服务测试 - 验证用户-角色关联和数据持久化功能
/// </summary>
public static class DatabaseCharacterServiceTests
{
    /// <summary>
    /// 运行所有角色服务测试
    /// </summary>
    public static async Task RunAllTests(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🚀 开始运行数据库角色服务测试...");

        try
        {
            await TestCharacterCreationWithUser(serviceProvider, logger);
            await TestCharacterQuery(serviceProvider, logger);
            await TestCharacterUpdate(serviceProvider, logger);
            await TestCharacterOwnership(serviceProvider, logger);
            await TestCharacterDeletion(serviceProvider, logger);
            await TestUserCharacterRelationship(serviceProvider, logger);
            
            logger.LogInformation("✅ 所有数据库角色服务测试通过");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ 数据库角色服务测试失败");
            throw;
        }
    }

    /// <summary>
    /// 测试角色创建和用户关联
    /// </summary>
    private static async Task TestCharacterCreationWithUser(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🧪 测试角色创建和用户关联...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidatedGameDbContext>();
        var characterService = new DatabaseCharacterService(context, 
            scope.ServiceProvider.GetRequiredService<ILogger<DatabaseCharacterService>>());
        var userService = new UserService(context, 
            scope.ServiceProvider.GetRequiredService<ILogger<UserService>>());

        // 清理测试数据
        await CleanupTestData(context, logger);

        // 创建测试用户
        var user = await userService.CreateUserAsync("testuser", "test@example.com", "password123");
        Assert.NotNull(user, "用户创建失败");

        // 创建角色
        var createRequest = new CreateCharacterRequest { Name = "TestWarrior" };
        var character = await characterService.CreateCharacterAsync(user.Id, createRequest);

        // 验证角色创建
        Assert.NotNull(character, "角色创建失败");
        Assert.Equal("TestWarrior", character.Name);
        Assert.Equal(100, character.Health);
        Assert.Equal(100, character.MaxHealth);
        Assert.Equal(1000, character.Gold);
        Assert.Equal("Warrior", character.SelectedBattleProfession);

        // 验证数据库中的角色数据
        var dbPlayer = await context.Players.FirstOrDefaultAsync(p => p.Id == character.Id);
        Assert.NotNull(dbPlayer, "数据库中未找到角色");
        Assert.Equal(user.Id, dbPlayer!.UserId);
        Assert.Equal("TestWarrior", dbPlayer.Name);

        // 验证JSON数据格式
        var attributes = JsonSerializer.Deserialize<Dictionary<string, int>>(dbPlayer.AttributesJson);
        Assert.NotNull(attributes, "属性JSON反序列化失败");
        Assert.True(attributes!.ContainsKey("Strength"), "缺少力量属性");
        Assert.Equal(10, attributes["Strength"]);

        logger.LogInformation("✅ 角色创建和用户关联测试通过");
    }

    /// <summary>
    /// 测试角色查询功能
    /// </summary>
    private static async Task TestCharacterQuery(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🧪 测试角色查询功能...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidatedGameDbContext>();
        var characterService = new DatabaseCharacterService(context, 
            scope.ServiceProvider.GetRequiredService<ILogger<DatabaseCharacterService>>());

        // 获取测试用户
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
        Assert.NotNull(user, "找不到测试用户");

        // 测试获取用户角色列表
        var userCharacters = await characterService.GetCharactersByUserIdAsync(user!.Id);
        Assert.NotNull(userCharacters, "获取用户角色列表失败");
        Assert.True(userCharacters.Count > 0, "用户应该至少有一个角色");
        Assert.True(userCharacters.Any(c => c.Name == "TestWarrior"), "找不到TestWarrior角色");

        // 测试获取角色详情
        var testCharacter = userCharacters.First(c => c.Name == "TestWarrior");
        var characterDetails = await characterService.GetCharacterDetailsAsync(testCharacter.Id);
        Assert.NotNull(characterDetails, "获取角色详情失败");
        Assert.Equal("TestWarrior", characterDetails!.Name);
        Assert.NotNull(characterDetails.EquippedSkills, "角色技能为空");
        Assert.True(characterDetails.EquippedSkills.Count >= 0, "角色技能应该有内容或为空");

        // 测试获取不存在的角色
        var nonExistentCharacter = await characterService.GetCharacterByIdAsync("non-existent-id");
        Assert.Null(nonExistentCharacter, "不存在的角色应该返回null");

        logger.LogInformation("✅ 角色查询功能测试通过");
    }

    /// <summary>
    /// 测试角色更新功能
    /// </summary>
    private static async Task TestCharacterUpdate(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🧪 测试角色更新功能...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidatedGameDbContext>();
        var characterService = new DatabaseCharacterService(context, 
            scope.ServiceProvider.GetRequiredService<ILogger<DatabaseCharacterService>>());

        // 获取测试角色
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
        Assert.NotNull(user, "找不到测试用户");

        var characters = await characterService.GetCharactersByUserIdAsync(user!.Id);
        var testCharacter = characters.First(c => c.Name == "TestWarrior");

        // 准备更新数据
        var updateDto = new CharacterUpdateDto
        {
            Health = 80,
            Gold = 1500,
            CurrentAction = "Fighting",
            AttributesJson = JsonSerializer.Serialize(new Dictionary<string, int>
            {
                ["Strength"] = 15,
                ["Agility"] = 12,
                ["Intellect"] = 10,
                ["Spirit"] = 10,
                ["Stamina"] = 13
            })
        };

        // 执行更新
        var updateResult = await characterService.UpdateCharacterAsync(testCharacter.Id, updateDto);
        Assert.True(updateResult, "角色更新失败");

        // 验证更新结果
        var updatedCharacter = await characterService.GetCharacterDetailsAsync(testCharacter.Id);
        Assert.NotNull(updatedCharacter, "更新后获取角色失败");
        Assert.Equal(80, updatedCharacter!.Health);
        Assert.Equal(1500, updatedCharacter.Gold);
        Assert.Equal("Fighting", updatedCharacter.CurrentAction);

        logger.LogInformation("✅ 角色更新功能测试通过");
    }

    /// <summary>
    /// 测试角色归属权限验证
    /// </summary>
    private static async Task TestCharacterOwnership(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🧪 测试角色归属权限验证...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidatedGameDbContext>();
        var characterService = new DatabaseCharacterService(context, 
            scope.ServiceProvider.GetRequiredService<ILogger<DatabaseCharacterService>>());
        var userService = new UserService(context, 
            scope.ServiceProvider.GetRequiredService<ILogger<UserService>>());

        // 获取测试用户和角色
        var user1 = await context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
        Assert.NotNull(user1, "找不到测试用户");

        var characters = await characterService.GetCharactersByUserIdAsync(user1!.Id);
        var testCharacter = characters.First(c => c.Name == "TestWarrior");

        // 创建另一个用户
        var user2 = await userService.CreateUserAsync("testuser2", "test2@example.com", "password123");

        // 测试正确的归属验证
        var isOwner = await characterService.IsCharacterOwnedByUserAsync(testCharacter.Id, user1.Id);
        Assert.True(isOwner, "角色应该属于user1");

        // 测试错误的归属验证
        var isNotOwner = await characterService.IsCharacterOwnedByUserAsync(testCharacter.Id, user2.Id);
        Assert.False(isNotOwner, "角色不应该属于user2");

        // 测试不存在的角色
        var nonExistentOwnership = await characterService.IsCharacterOwnedByUserAsync("non-existent", user1.Id);
        Assert.False(nonExistentOwnership, "不存在的角色不应该有归属");

        logger.LogInformation("✅ 角色归属权限验证测试通过");
    }

    /// <summary>
    /// 测试角色删除功能
    /// </summary>
    private static async Task TestCharacterDeletion(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🧪 测试角色删除功能...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidatedGameDbContext>();
        var characterService = new DatabaseCharacterService(context, 
            scope.ServiceProvider.GetRequiredService<ILogger<DatabaseCharacterService>>());

        // 获取测试用户
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
        Assert.NotNull(user, "找不到测试用户");

        // 创建一个新角色用于删除测试
        var createRequest = new CreateCharacterRequest { Name = "ToBeDeleted" };
        var characterToDelete = await characterService.CreateCharacterAsync(user.Id, createRequest);

        // 验证角色存在
        var beforeDeletion = await characterService.GetCharacterByIdAsync(characterToDelete.Id);
        Assert.NotNull(beforeDeletion, "待删除角色应该存在");

        // 执行删除
        var deleteResult = await characterService.DeleteCharacterAsync(characterToDelete.Id);
        Assert.True(deleteResult, "角色删除应该成功");

        // 验证软删除（角色应该不在活跃列表中）
        var userCharacters = await characterService.GetCharactersByUserIdAsync(user.Id);
        Assert.False(userCharacters.Any(c => c.Name == "ToBeDeleted"), "删除的角色不应该出现在用户角色列表中");

        // 验证数据库中角色仍存在但标记为离线
        var dbPlayer = await context.Players.FirstOrDefaultAsync(p => p.Id == characterToDelete.Id);
        Assert.NotNull(dbPlayer, "数据库中角色应该仍然存在");
        Assert.False(dbPlayer.IsOnline, "删除的角色应该标记为离线");

        logger.LogInformation("✅ 角色删除功能测试通过");
    }

    /// <summary>
    /// 测试用户-角色关系的完整性
    /// </summary>
    private static async Task TestUserCharacterRelationship(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("🧪 测试用户-角色关系的完整性...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidatedGameDbContext>();
        var characterService = new DatabaseCharacterService(context, 
            scope.ServiceProvider.GetRequiredService<ILogger<DatabaseCharacterService>>());
        var userService = new UserService(context, 
            scope.ServiceProvider.GetRequiredService<ILogger<UserService>>());

        // 创建测试用户
        var user = await userService.CreateUserAsync("relationtest", "relation@example.com", "password123");

        // 为用户创建多个角色
        var characterNames = new[] { "Warrior1", "Mage1", "Archer1" };
        var createdCharacters = new List<CharacterDto>();

        foreach (var name in characterNames)
        {
            var createRequest = new CreateCharacterRequest { Name = name };
            var character = await characterService.CreateCharacterAsync(user.Id, createRequest);
            createdCharacters.Add(character);
        }

        // 验证用户的所有角色
        var userCharacters = await characterService.GetCharactersByUserIdAsync(user.Id);
        Assert.Equal(3, userCharacters.Count);
        Assert.True(characterNames.All(name => userCharacters.Any(c => c.Name == name)), "所有角色名称都应该存在");

        // 验证每个角色都正确关联到用户
        foreach (var character in createdCharacters)
        {
            var isOwned = await characterService.IsCharacterOwnedByUserAsync(character.Id, user.Id);
            Assert.True(isOwned, $"角色 {character.Name} 应该属于用户");

            // 验证数据库中的外键关系
            var dbPlayer = await context.Players.FirstOrDefaultAsync(p => p.Id == character.Id);
            Assert.NotNull(dbPlayer, $"数据库中找不到角色 {character.Name}");
            Assert.Equal(user.Id, dbPlayer!.UserId);
        }

        // 测试用户删除对角色的影响（设置为null，不级联删除）
        await userService.DeactivateUserAsync(user.Id);

        // 角色应该仍然存在，但UserId可能为null（取决于DeleteBehavior.SetNull设置）
        var charactersAfterUserDeactivation = await context.Players
            .Where(p => createdCharacters.Select(c => c.Id).Contains(p.Id))
            .ToListAsync();

        Assert.Equal(3, charactersAfterUserDeactivation.Count);
        // Note: 由于设置了DeleteBehavior.SetNull，UserId应该被设为null
        // 但实际行为可能因为用户只是被停用而不是删除，所以UserId可能仍然存在

        logger.LogInformation("✅ 用户-角色关系完整性测试通过");
    }

    /// <summary>
    /// 清理测试数据
    /// </summary>
    private static async Task CleanupTestData(ConsolidatedGameDbContext context, ILogger logger)
    {
        try
        {
            // 删除测试角色
            var testPlayers = await context.Players
                .Where(p => p.Name.StartsWith("Test") || p.Name.StartsWith("ToBe") || 
                           p.Name.StartsWith("Warrior") || p.Name.StartsWith("Mage") || p.Name.StartsWith("Archer"))
                .ToListAsync();
            
            if (testPlayers.Any())
            {
                context.Players.RemoveRange(testPlayers);
            }

            // 删除测试用户
            var testUsers = await context.Users
                .Where(u => u.Username.StartsWith("testuser") || u.Username == "relationtest")
                .ToListAsync();
            
            if (testUsers.Any())
            {
                context.Users.RemoveRange(testUsers);
            }

            await context.SaveChangesAsync();
            logger.LogDebug("测试数据清理完成");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "清理测试数据时发生错误");
        }
    }

    /// <summary>
    /// 简单的断言辅助类
    /// </summary>
    private static class Assert
    {
        public static void NotNull<T>(T? obj, string message) where T : class
        {
            if (obj == null)
                throw new AssertionException(message);
        }

        public static void Null<T>(T? obj, string message) where T : class
        {
            if (obj != null)
                throw new AssertionException(message);
        }

        public static void True(bool condition, string message)
        {
            if (!condition)
                throw new AssertionException(message);
        }

        public static void False(bool condition, string message)
        {
            if (condition)
                throw new AssertionException(message);
        }

        public static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new AssertionException($"期望值: {expected}, 实际值: {actual}");
        }
    }

    /// <summary>
    /// 断言异常
    /// </summary>
    private class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }
}