using Microsoft.Extensions.Logging;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using BlazorWebGame.Rebuild.Services.Data;

namespace BlazorWebGame.Rebuild.Tests;

/// <summary>
/// 数据存储服务的简单集成测试
/// </summary>
public static class DataStorageServiceTests
{
    public static async Task RunBasicTests(ILogger logger)
    {
        logger.LogInformation("Starting DataStorageService basic tests...");
        
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var serviceLogger = loggerFactory.CreateLogger<DataStorageService>();
        var dataStorage = new DataStorageService(serviceLogger);
        
        try
        {
            // 测试1: 玩家数据管理
            await TestPlayerDataManagement(dataStorage, logger);
            
            // 测试2: 队伍数据管理
            await TestTeamDataManagement(dataStorage, logger);
            
            // 测试3: 动作目标管理
            await TestActionTargetManagement(dataStorage, logger);
            
            // 测试4: 战斗记录管理
            await TestBattleRecordManagement(dataStorage, logger);
            
            // 测试5: 系统统计和健康检查
            await TestSystemStatsAndHealth(dataStorage, logger);
            
            logger.LogInformation("All DataStorageService tests passed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DataStorageService tests failed");
            throw;
        }
    }
    
    private static async Task TestPlayerDataManagement(IDataStorageService dataStorage, ILogger logger)
    {
        logger.LogInformation("Testing player data management...");
        
        // 创建测试玩家
        var testPlayer = new PlayerStorageDto
        {
            Id = "test-player-001",
            Name = "TestPlayer",
            Level = 5,
            Experience = 1500,
            Health = 150,
            MaxHealth = 200,
            Gold = 500,
            SelectedBattleProfession = "Warrior",
            CurrentAction = "Idle",
            IsOnline = true
        };
        
        // 保存玩家
        var saveResult = await dataStorage.SavePlayerAsync(testPlayer);
        if (!saveResult.Success)
        {
            throw new Exception($"Failed to save player: {saveResult.Message}");
        }
        
        // 获取玩家
        var retrievedPlayer = await dataStorage.GetPlayerAsync(testPlayer.Id);
        if (retrievedPlayer == null)
        {
            throw new Exception("Failed to retrieve saved player");
        }
        
        // 验证数据
        if (retrievedPlayer.Name != testPlayer.Name || retrievedPlayer.Level != testPlayer.Level)
        {
            throw new Exception("Retrieved player data doesn't match saved data");
        }
        
        // 获取在线玩家列表
        var onlinePlayersResult = await dataStorage.GetOnlinePlayersAsync();
        if (!onlinePlayersResult.Success || onlinePlayersResult.Data?.Count == 0)
        {
            throw new Exception("Failed to get online players list");
        }
        
        logger.LogInformation("Player data management test passed");
    }
    
    private static async Task TestTeamDataManagement(IDataStorageService dataStorage, ILogger logger)
    {
        logger.LogInformation("Testing team data management...");
        
        // 创建测试队伍
        var testTeam = new TeamStorageDto
        {
            Id = "test-team-001",
            Name = "TestTeam",
            CaptainId = "test-player-001",
            MemberIds = new List<string> { "test-player-001", "test-player-002" },
            MaxMembers = 5,
            Status = "Active"
        };
        
        // 保存队伍
        var saveResult = await dataStorage.SaveTeamAsync(testTeam);
        if (!saveResult.Success)
        {
            throw new Exception($"Failed to save team: {saveResult.Message}");
        }
        
        // 通过队长获取队伍
        var retrievedTeam = await dataStorage.GetTeamByCaptainAsync(testTeam.CaptainId);
        if (retrievedTeam == null)
        {
            throw new Exception("Failed to retrieve team by captain");
        }
        
        // 验证数据
        if (retrievedTeam.Name != testTeam.Name || retrievedTeam.MemberIds.Count != testTeam.MemberIds.Count)
        {
            throw new Exception("Retrieved team data doesn't match saved data");
        }
        
        logger.LogInformation("Team data management test passed");
    }
    
    private static async Task TestActionTargetManagement(IDataStorageService dataStorage, ILogger logger)
    {
        logger.LogInformation("Testing action target management...");
        
        // 创建测试动作目标
        var testActionTarget = new ActionTargetStorageDto
        {
            Id = "test-action-001",
            PlayerId = "test-player-001",
            TargetType = "Enemy",
            TargetId = "goblin-001",
            TargetName = "Forest Goblin",
            ActionType = "Combat",
            Progress = 0.0,
            Duration = 30.0,
            IsCompleted = false
        };
        
        // 保存动作目标
        var saveResult = await dataStorage.SaveActionTargetAsync(testActionTarget);
        if (!saveResult.Success)
        {
            throw new Exception($"Failed to save action target: {saveResult.Message}");
        }
        
        // 获取当前动作目标
        var retrievedActionTarget = await dataStorage.GetCurrentActionTargetAsync(testActionTarget.PlayerId);
        if (retrievedActionTarget == null)
        {
            throw new Exception("Failed to retrieve current action target");
        }
        
        // 完成动作目标
        var completeResult = await dataStorage.CompleteActionTargetAsync(testActionTarget.Id);
        if (!completeResult.Success)
        {
            throw new Exception($"Failed to complete action target: {completeResult.Message}");
        }
        
        logger.LogInformation("Action target management test passed");
    }
    
    private static async Task TestBattleRecordManagement(IDataStorageService dataStorage, ILogger logger)
    {
        logger.LogInformation("Testing battle record management...");
        
        // 创建测试战斗记录
        var testBattleRecord = new BattleRecordStorageDto
        {
            Id = "test-battle-001",
            BattleId = "battle-001",
            BattleType = "Normal",
            Status = "InProgress",
            Participants = new List<string> { "test-player-001" },
            Enemies = new List<object> { new { Name = "Forest Goblin", Health = 50 } },
            Actions = new List<object>(),
            Results = new Dictionary<string, object>()
        };
        
        // 保存战斗记录
        var saveResult = await dataStorage.SaveBattleRecordAsync(testBattleRecord);
        if (!saveResult.Success)
        {
            throw new Exception($"Failed to save battle record: {saveResult.Message}");
        }
        
        // 获取战斗记录
        var retrievedBattleRecord = await dataStorage.GetBattleRecordAsync(testBattleRecord.BattleId);
        if (retrievedBattleRecord == null)
        {
            throw new Exception("Failed to retrieve battle record");
        }
        
        // 结束战斗记录
        var results = new Dictionary<string, object> 
        { 
            ["victory"] = true, 
            ["xpGained"] = 100 
        };
        var endResult = await dataStorage.EndBattleRecordAsync(testBattleRecord.BattleId, "Victory", results);
        if (!endResult.Success)
        {
            throw new Exception($"Failed to end battle record: {endResult.Message}");
        }
        
        logger.LogInformation("Battle record management test passed");
    }
    
    private static async Task TestSystemStatsAndHealth(IDataStorageService dataStorage, ILogger logger)
    {
        logger.LogInformation("Testing system stats and health check...");
        
        // 获取存储统计信息
        var statsResult = await dataStorage.GetStorageStatsAsync();
        if (!statsResult.Success || statsResult.Data == null)
        {
            throw new Exception($"Failed to get storage stats: {statsResult.Message}");
        }
        
        // 验证统计信息包含预期的键
        var expectedKeys = new[] { "TotalPlayers", "OnlinePlayers", "TotalTeams", "ActiveTeams" };
        foreach (var key in expectedKeys)
        {
            if (!statsResult.Data.ContainsKey(key))
            {
                throw new Exception($"Storage stats missing expected key: {key}");
            }
        }
        
        // 健康检查
        var healthResult = await dataStorage.HealthCheckAsync();
        if (!healthResult.Success || healthResult.Data == null)
        {
            throw new Exception($"Health check failed: {healthResult.Message}");
        }
        
        // 验证健康检查返回状态
        if (!healthResult.Data.ContainsKey("Status") || 
            healthResult.Data["Status"].ToString() != "Healthy")
        {
            throw new Exception("Health check didn't return healthy status");
        }
        
        logger.LogInformation("System stats and health check test passed");
    }
}