using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorWebGame.Server.Services.Activities;
using BlazorWebGame.Server.Services.Data;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Tests;

/// <summary>
/// 离线结算服务测试
/// </summary>
public static class OfflineSettlementServiceTests
{
    /// <summary>
    /// 运行基础测试
    /// </summary>
    public static async Task RunBasicTests(ILogger logger)
    {
        logger.LogInformation("开始运行离线结算服务测试...");

        try
        {
            // 这里应该使用实际的 DataStorageService 实例
            // 为了测试，我们创建一个 mock 或者使用现有的服务
            
            logger.LogInformation("创建测试用的数据存储服务...");
            var dataStorageService = new DataStorageService(
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<DataStorageService>()
            );
            
            logger.LogInformation("创建离线结算服务...");
            var offlineSettlementService = new OfflineSettlementService(
                dataStorageService,
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<OfflineSettlementService>()
            );

            // 测试1: 创建测试玩家
            await TestCreateTestPlayer(dataStorageService, logger);
            
            // 测试2: 测试玩家离线结算
            await TestPlayerOfflineSettlement(offlineSettlementService, logger);
            
            // 测试3: 测试队伍离线结算
            await TestTeamOfflineSettlement(dataStorageService, offlineSettlementService, logger);
            
            // 测试4: 测试批量离线结算
            await TestBatchOfflineSettlement(offlineSettlementService, logger);

            logger.LogInformation("所有离线结算服务测试通过！");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "离线结算服务测试失败");
            throw;
        }
    }

    /// <summary>
    /// 测试创建测试玩家
    /// </summary>
    private static async Task TestCreateTestPlayer(IDataStorageService dataStorageService, ILogger logger)
    {
        logger.LogInformation("测试创建测试玩家...");

        var testPlayer = new PlayerStorageDto
        {
            Id = "test-player-001",
            Name = "测试玩家1",
            Level = 5,
            Experience = 450,
            Health = 120,
            MaxHealth = 120,
            Gold = 100,
            SelectedBattleProfession = "Warrior",
            CurrentAction = "Combat",
            IsOnline = false,
            LastActiveAt = DateTime.UtcNow.AddHours(-2) // 2小时前离线
        };

        var result = await dataStorageService.SavePlayerAsync(testPlayer);
        if (!result.IsSuccess)
        {
            throw new Exception($"创建测试玩家失败: {result.Message}");
        }

        logger.LogInformation("测试玩家创建成功: {PlayerId}", testPlayer.Id);
    }

    /// <summary>
    /// 测试玩家离线结算
    /// </summary>
    private static async Task TestPlayerOfflineSettlement(OfflineSettlementService service, ILogger logger)
    {
        logger.LogInformation("测试玩家离线结算...");

        var result = await service.ProcessPlayerOfflineSettlementAsync("test-player-001");
        
        if (!result.IsSuccess)
        {
            throw new Exception($"玩家离线结算失败: {result.Message}");
        }

        if (result.Data == null)
        {
            throw new Exception("离线结算结果数据为空");
        }

        logger.LogInformation("玩家离线结算成功:");
        logger.LogInformation("  - 离线时间: {OfflineTime}", result.Data.OfflineTime);
        logger.LogInformation("  - 获得经验: {Experience}", result.Data.TotalExperience);
        logger.LogInformation("  - 获得金币: {Gold}", result.Data.TotalGold);
        logger.LogInformation("  - 战斗次数: {BattleCount}", result.Data.BattleResults.Count);
        logger.LogInformation("  - 奖励数量: {RewardCount}", result.Data.Rewards.Count);

        // 验证结果合理性
        if (result.Data.TotalExperience <= 0)
        {
            throw new Exception("离线结算经验值应该大于0");
        }

        if (result.Data.TotalGold <= 0)
        {
            throw new Exception("离线结算金币应该大于0");
        }
    }

    /// <summary>
    /// 测试队伍离线结算
    /// </summary>
    private static async Task TestTeamOfflineSettlement(
        IDataStorageService dataStorageService, 
        OfflineSettlementService service, 
        ILogger logger)
    {
        logger.LogInformation("测试队伍离线结算...");

        // 创建第二个测试玩家
        var testPlayer2 = new PlayerStorageDto
        {
            Id = "test-player-002",
            Name = "测试玩家2",
            Level = 3,
            Experience = 250,
            Health = 100,
            MaxHealth = 100,
            Gold = 50,
            SelectedBattleProfession = "Mage",
            CurrentAction = "Gathering",
            IsOnline = false,
            LastActiveAt = DateTime.UtcNow.AddHours(-1) // 1小时前离线
        };

        await dataStorageService.SavePlayerAsync(testPlayer2);

        // 创建测试队伍
        var testTeam = new TeamStorageDto
        {
            Id = "test-team-001",
            Name = "测试队伍",
            CaptainId = "test-player-001",
            MemberIds = new List<string> { "test-player-001", "test-player-002" },
            Status = "Active"
        };

        await dataStorageService.SaveTeamAsync(testTeam);

        // 执行队伍离线结算
        var result = await service.ProcessTeamOfflineSettlementAsync("test-team-001");
        
        if (!result.IsSuccess)
        {
            throw new Exception($"队伍离线结算失败: {result.Message}");
        }

        if (result.Data == null || result.Data.Count == 0)
        {
            throw new Exception("队伍离线结算结果数据为空");
        }

        logger.LogInformation("队伍离线结算成功:");
        logger.LogInformation("  - 处理成员数量: {MemberCount}", result.Data.Count);
        
        foreach (var memberResult in result.Data)
        {
            logger.LogInformation("  - 成员 {PlayerId}: 经验 {Exp}, 金币 {Gold}", 
                memberResult.PlayerId, memberResult.TotalExperience, memberResult.TotalGold);
        }
    }

    /// <summary>
    /// 测试批量离线结算
    /// </summary>
    private static async Task TestBatchOfflineSettlement(OfflineSettlementService service, ILogger logger)
    {
        logger.LogInformation("测试批量离线结算...");

        var playerIds = new List<string> { "test-player-001", "test-player-002" };
        var result = await service.ProcessBatchOfflineSettlementAsync(playerIds);

        logger.LogInformation("批量离线结算完成:");
        logger.LogInformation("  - 总处理数量: {Total}", result.TotalProcessed);
        logger.LogInformation("  - 成功数量: {Success}", result.SuccessCount);
        logger.LogInformation("  - 失败数量: {Error}", result.ErrorCount);

        if (result.ErrorCount > 0)
        {
            logger.LogWarning("批量结算存在错误:");
            foreach (var error in result.Errors)
            {
                logger.LogWarning("  - {Error}", error);
            }
        }

        if (result.SuccessCount == 0)
        {
            throw new Exception("批量离线结算没有成功处理任何玩家");
        }
    }
}