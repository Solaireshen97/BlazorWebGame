using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;

namespace BlazorWebGame.Server.Tests;

/// <summary>
/// SQLite数据存储服务测试类
/// </summary>
public static class SqliteDataStorageServiceTests
{
    /// <summary>
    /// 运行SQLite数据存储服务的基本测试
    /// </summary>
    public static async Task RunBasicTests(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("开始运行 SQLite 数据存储服务测试...");

        try
        {
            // 获取服务实例
            var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var storageService = new SqliteDataStorageService(context, 
                scope.ServiceProvider.GetRequiredService<ILogger<SqliteDataStorageService>>());

            // 测试数据库连接
            await TestDatabaseConnection(context, logger);

            // 测试玩家数据操作
            await TestPlayerOperations(storageService, logger);

            // 测试队伍数据操作
            await TestTeamOperations(storageService, logger);

            // 测试动作目标操作
            await TestActionTargetOperations(storageService, logger);

            // 测试统计信息
            await TestStatistics(storageService, logger);

            logger.LogInformation("✅ SQLite 数据存储服务测试全部通过!");

            scope.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ SQLite 数据存储服务测试失败");
            throw;
        }
    }

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    private static async Task TestDatabaseConnection(GameDbContext context, ILogger logger)
    {
        logger.LogInformation("测试数据库连接...");

        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            throw new Exception("无法连接到数据库");
        }

        logger.LogInformation("✅ 数据库连接测试通过");
    }

    /// <summary>
    /// 测试玩家数据操作
    /// </summary>
    private static async Task TestPlayerOperations(IDataStorageService storageService, ILogger logger)
    {
        logger.LogInformation("测试玩家数据操作...");

        // 创建测试玩家数据
        var testPlayer = new PlayerStorageDto
        {
            Id = "test-player-001",
            Name = "测试玩家",
            Level = 1,
            Experience = 0,
            Health = 100,
            MaxHealth = 100,
            Gold = 100,
            SelectedBattleProfession = "Warrior",
            CurrentAction = "Idle",
            IsOnline = true,
            LastActiveAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Attributes = new Dictionary<string, object>
            {
                ["strength"] = 10,
                ["agility"] = 8,
                ["intelligence"] = 5
            },
            Inventory = new List<object>
            {
                new { id = "sword-001", name = "铁剑", quantity = 1 }
            },
            Skills = new List<string> { "基础剑术" },
            Equipment = new Dictionary<string, string>
            {
                ["weapon"] = "铁剑"
            }
        };

        // 测试保存玩家
        var saveResult = await storageService.SavePlayerAsync(testPlayer);
        if (!saveResult.Success)
        {
            throw new Exception($"保存玩家失败: {saveResult.Message}");
        }

        logger.LogInformation("✅ 玩家保存测试通过");

        // 测试获取玩家
        var retrievedPlayer = await storageService.GetPlayerAsync("test-player-001");
        if (retrievedPlayer == null)
        {
            throw new Exception("获取玩家失败");
        }

        if (retrievedPlayer.Name != testPlayer.Name)
        {
            throw new Exception($"玩家名称不匹配: 期望 {testPlayer.Name}, 实际 {retrievedPlayer.Name}");
        }

        logger.LogInformation("✅ 玩家获取测试通过");

        // 测试更新玩家
        retrievedPlayer.Level = 2;
        retrievedPlayer.Experience = 100;
        var updateResult = await storageService.SavePlayerAsync(retrievedPlayer);
        if (!updateResult.Success)
        {
            throw new Exception($"更新玩家失败: {updateResult.Message}");
        }

        var updatedPlayer = await storageService.GetPlayerAsync("test-player-001");
        if (updatedPlayer?.Level != 2)
        {
            throw new Exception("玩家等级更新失败");
        }

        logger.LogInformation("✅ 玩家更新测试通过");

        // 测试在线玩家列表
        var onlinePlayersResult = await storageService.GetOnlinePlayersAsync();
        if (!onlinePlayersResult.Success)
        {
            throw new Exception($"获取在线玩家失败: {onlinePlayersResult.Message}");
        }

        logger.LogInformation("✅ 在线玩家列表测试通过 (找到 {Count} 名在线玩家)", onlinePlayersResult.Data?.Count ?? 0);
    }

    /// <summary>
    /// 测试队伍数据操作
    /// </summary>
    private static async Task TestTeamOperations(IDataStorageService storageService, ILogger logger)
    {
        logger.LogInformation("测试队伍数据操作...");

        // 创建测试队伍数据
        var testTeam = new TeamStorageDto
        {
            Id = "test-team-001",
            Name = "测试队伍",
            CaptainId = "test-player-001",
            MemberIds = new List<string> { "test-player-001" },
            MaxMembers = 5,
            Status = "Active",
            LastBattleAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 测试保存队伍
        var saveResult = await storageService.SaveTeamAsync(testTeam);
        if (!saveResult.Success)
        {
            throw new Exception($"保存队伍失败: {saveResult.Message}");
        }

        logger.LogInformation("✅ 队伍保存测试通过");

        // 测试获取队伍
        var retrievedTeam = await storageService.GetTeamAsync("test-team-001");
        if (retrievedTeam == null)
        {
            throw new Exception("获取队伍失败");
        }

        logger.LogInformation("✅ 队伍获取测试通过");

        // 测试根据队长获取队伍
        var teamByCaptain = await storageService.GetTeamByCaptainAsync("test-player-001");
        if (teamByCaptain == null)
        {
            throw new Exception("根据队长获取队伍失败");
        }

        logger.LogInformation("✅ 根据队长获取队伍测试通过");
    }

    /// <summary>
    /// 测试动作目标操作
    /// </summary>
    private static async Task TestActionTargetOperations(IDataStorageService storageService, ILogger logger)
    {
        logger.LogInformation("测试动作目标操作...");

        // 创建测试动作目标
        var testActionTarget = new ActionTargetStorageDto
        {
            Id = "test-action-001",
            PlayerId = "test-player-001",
            TargetType = "Enemy",
            TargetId = "goblin-001",
            TargetName = "森林哥布林",
            ActionType = "Combat",
            Progress = 0.0,
            Duration = 30.0,
            StartedAt = DateTime.UtcNow,
            IsCompleted = false,
            ProgressData = new Dictionary<string, object>
            {
                ["damageDealt"] = 0,
                ["experienceGained"] = 0
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 测试保存动作目标
        var saveResult = await storageService.SaveActionTargetAsync(testActionTarget);
        if (!saveResult.Success)
        {
            throw new Exception($"保存动作目标失败: {saveResult.Message}");
        }

        logger.LogInformation("✅ 动作目标保存测试通过");

        // 测试获取当前动作目标
        var currentTarget = await storageService.GetCurrentActionTargetAsync("test-player-001");
        if (currentTarget == null)
        {
            throw new Exception("获取当前动作目标失败");
        }

        logger.LogInformation("✅ 获取当前动作目标测试通过");

        // 测试完成动作目标
        var completeResult = await storageService.CompleteActionTargetAsync("test-action-001");
        if (!completeResult.Success)
        {
            throw new Exception($"完成动作目标失败: {completeResult.Message}");
        }

        logger.LogInformation("✅ 完成动作目标测试通过");
    }

    /// <summary>
    /// 测试统计信息
    /// </summary>
    private static async Task TestStatistics(IDataStorageService storageService, ILogger logger)
    {
        logger.LogInformation("测试统计信息...");

        // 测试存储统计
        var statsResult = await storageService.GetStorageStatsAsync();
        if (!statsResult.Success || statsResult.Data == null)
        {
            throw new Exception($"获取存储统计失败: {statsResult.Message}");
        }

        logger.LogInformation("✅ 存储统计测试通过 - 统计信息: {@Stats}", statsResult.Data);

        // 测试健康检查
        var healthResult = await storageService.HealthCheckAsync();
        if (!healthResult.Success || healthResult.Data == null)
        {
            throw new Exception($"健康检查失败: {healthResult.Message}");
        }

        logger.LogInformation("✅ 健康检查测试通过 - 状态: {Status}", 
            healthResult.Data.TryGetValue("Status", out var status) ? status : "Unknown");
    }

    /// <summary>
    /// 清理测试数据
    /// </summary>
    public static async Task CleanupTestData(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("清理测试数据...");

        try
        {
            var scope = serviceProvider.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IDataStorageService>();

            // 删除测试玩家数据
            await storageService.DeletePlayerAsync("test-player-001");

            // 删除测试队伍数据
            await storageService.DeleteTeamAsync("test-team-001");

            logger.LogInformation("✅ 测试数据清理完成");

            scope.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "清理测试数据时发生错误（可能数据不存在）");
        }
    }
}