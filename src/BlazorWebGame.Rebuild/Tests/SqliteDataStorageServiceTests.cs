using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BlazorWebGame.Rebuild.Data;
using BlazorWebGame.Rebuild.Services.Data;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlazorWebGame.Rebuild.Tests;

/// <summary>
/// SQLite数据存储服务测试类
/// </summary>
public class SqliteDataStorageServiceTests : IDisposable
{
    private readonly string _testDatabasePath;
    private readonly IDbContextFactory<GameDbContext> _contextFactory;
    private readonly ILogger<SqliteDataStorageService> _logger;
    private readonly SqliteDataStorageService _service;

    public SqliteDataStorageServiceTests()
    {
        // 创建临时测试数据库
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_gamedata_{Guid.NewGuid()}.db");
        
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlite($"Data Source={_testDatabasePath}")
            .Options;
            
        _contextFactory = new TestDbContextFactory(options);
        _logger = NullLogger<SqliteDataStorageService>.Instance;
        _service = new SqliteDataStorageService(_contextFactory, _logger);
        
        // 初始化数据库
        InitializeDatabase().Wait();
    }

    private async Task InitializeDatabase()
    {
        using var context = _contextFactory.CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public void Dispose()
    {
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
    /// 测试玩家数据操作
    /// </summary>
    public async Task TestPlayerOperations()
    {
        Console.WriteLine("=== 测试玩家数据操作 ===");
        
        // 创建测试玩家
        var testPlayer = new PlayerStorageDto
        {
            Id = "test-player-1",
            Name = "测试玩家",
            Level = 5,
            Experience = 1000,
            Health = 95,
            MaxHealth = 100,
            Gold = 500,
            SelectedBattleProfession = "Mage",
            CurrentAction = "Idle",
            IsOnline = true,
            Attributes = new Dictionary<string, object>
            {
                ["Strength"] = 10,
                ["Intelligence"] = 15,
                ["Agility"] = 8
            },
            Skills = new List<string> { "Fireball", "Heal", "Shield" },
            Equipment = new Dictionary<string, string>
            {
                ["Weapon"] = "Magic Staff",
                ["Armor"] = "Robe of Wisdom"
            }
        };
        
        // 测试保存玩家
        var saveResult = await _service.SavePlayerAsync(testPlayer);
        Console.WriteLine($"保存玩家结果: {saveResult.Success}, 消息: {saveResult.Message}");
        
        if (!saveResult.Success)
        {
            throw new Exception($"保存玩家失败: {saveResult.Message}");
        }
        
        // 测试获取玩家
        var retrievedPlayer = await _service.GetPlayerAsync("test-player-1");
        Console.WriteLine($"获取玩家结果: {retrievedPlayer?.Name ?? "未找到"}");
        
        if (retrievedPlayer == null)
        {
            throw new Exception("获取玩家失败");
        }
        
        // 验证数据完整性
        if (retrievedPlayer.Name != testPlayer.Name ||
            retrievedPlayer.Level != testPlayer.Level ||
            retrievedPlayer.Experience != testPlayer.Experience ||
            retrievedPlayer.Skills.Count != testPlayer.Skills.Count)
        {
            throw new Exception("玩家数据不匹配");
        }
        
        // 测试更新玩家
        retrievedPlayer.Level = 6;
        retrievedPlayer.Experience = 1200;
        var updateResult = await _service.SavePlayerAsync(retrievedPlayer);
        Console.WriteLine($"更新玩家结果: {updateResult.Success}");
        
        // 测试获取在线玩家
        var onlinePlayersResult = await _service.GetOnlinePlayersAsync();
        Console.WriteLine($"在线玩家数量: {onlinePlayersResult.Data?.Count ?? 0}");
        
        // 测试删除玩家
        var deleteResult = await _service.DeletePlayerAsync("test-player-1");
        Console.WriteLine($"删除玩家结果: {deleteResult.Success}, 消息: {deleteResult.Message}");
        
        Console.WriteLine("玩家数据操作测试完成");
    }

    /// <summary>
    /// 测试队伍数据操作
    /// </summary>
    public async Task TestTeamOperations()
    {
        Console.WriteLine("\n=== 测试队伍数据操作 ===");
        
        // 创建测试队伍
        var testTeam = new TeamStorageDto
        {
            Id = "test-team-1",
            Name = "勇者小队",
            CaptainId = "captain-1",
            MemberIds = new List<string> { "captain-1", "member-2", "member-3" },
            MaxMembers = 5,
            Status = "Active"
        };
        
        // 测试保存队伍
        var saveResult = await _service.SaveTeamAsync(testTeam);
        Console.WriteLine($"保存队伍结果: {saveResult.Success}, 消息: {saveResult.Message}");
        
        if (!saveResult.Success)
        {
            throw new Exception($"保存队伍失败: {saveResult.Message}");
        }
        
        // 测试通过ID获取队伍
        var retrievedTeam = await _service.GetTeamAsync("test-team-1");
        Console.WriteLine($"获取队伍结果: {retrievedTeam?.Name ?? "未找到"}");
        
        // 测试通过队长ID获取队伍
        var teamByCaptain = await _service.GetTeamByCaptainAsync("captain-1");
        Console.WriteLine($"通过队长获取队伍: {teamByCaptain?.Name ?? "未找到"}");
        
        // 测试通过成员ID获取队伍
        var teamByPlayer = await _service.GetTeamByPlayerAsync("member-2");
        Console.WriteLine($"通过成员获取队伍: {teamByPlayer?.Name ?? "未找到"}");
        
        // 测试获取活跃队伍
        var activeTeamsResult = await _service.GetActiveTeamsAsync();
        Console.WriteLine($"活跃队伍数量: {activeTeamsResult.Data?.Count ?? 0}");
        
        // 测试删除队伍
        var deleteResult = await _service.DeleteTeamAsync("test-team-1");
        Console.WriteLine($"删除队伍结果: {deleteResult.Success}");
        
        Console.WriteLine("队伍数据操作测试完成");
    }

    /// <summary>
    /// 测试动作目标操作
    /// </summary>
    public async Task TestActionTargetOperations()
    {
        Console.WriteLine("\n=== 测试动作目标操作 ===");
        
        // 创建测试动作目标
        var testActionTarget = new ActionTargetStorageDto
        {
            Id = "action-target-1",
            PlayerId = "test-player-2",
            TargetType = "Enemy",
            TargetId = "goblin-1",
            TargetName = "哥布林战士",
            ActionType = "Combat",
            Progress = 0.5,
            Duration = 30.0,
            StartedAt = DateTime.UtcNow.AddMinutes(-1),
            IsCompleted = false,
            ProgressData = new Dictionary<string, object>
            {
                ["DamageDealt"] = 45,
                ["TurnsElapsed"] = 3
            }
        };
        
        // 测试保存动作目标
        var saveResult = await _service.SaveActionTargetAsync(testActionTarget);
        Console.WriteLine($"保存动作目标结果: {saveResult.Success}");
        
        // 测试获取当前动作目标
        var currentTarget = await _service.GetCurrentActionTargetAsync("test-player-2");
        Console.WriteLine($"当前动作目标: {currentTarget?.TargetName ?? "无"}");
        
        // 测试完成动作目标
        var completeResult = await _service.CompleteActionTargetAsync("action-target-1");
        Console.WriteLine($"完成动作目标结果: {completeResult.Success}");
        
        // 测试获取动作历史
        var historyResult = await _service.GetPlayerActionHistoryAsync("test-player-2", 10);
        Console.WriteLine($"动作历史数量: {historyResult.Data?.Count ?? 0}");
        
        Console.WriteLine("动作目标操作测试完成");
    }

    /// <summary>
    /// 测试战斗记录操作
    /// </summary>
    public async Task TestBattleRecordOperations()
    {
        Console.WriteLine("\n=== 测试战斗记录操作 ===");
        
        // 创建测试战斗记录
        var testBattleRecord = new BattleRecordStorageDto
        {
            Id = "battle-record-1",
            BattleId = "battle-123",
            BattleType = "Normal",
            Status = "InProgress",
            Participants = new List<string> { "player-1", "player-2" },
            Enemies = new List<object> { new { Name = "哥布林", Level = 3 } },
            Actions = new List<object>(),
            Results = new Dictionary<string, object>(),
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            WaveNumber = 1
        };
        
        // 测试保存战斗记录
        var saveResult = await _service.SaveBattleRecordAsync(testBattleRecord);
        Console.WriteLine($"保存战斗记录结果: {saveResult.Success}");
        
        // 测试获取战斗记录
        var retrievedRecord = await _service.GetBattleRecordAsync("battle-123");
        Console.WriteLine($"获取战斗记录: {retrievedRecord?.BattleType ?? "未找到"}");
        
        // 测试结束战斗
        var battleResults = new Dictionary<string, object>
        {
            ["Victory"] = true,
            ["ExperienceGained"] = 150,
            ["GoldGained"] = 75
        };
        var endResult = await _service.EndBattleRecordAsync("battle-123", "Victory", battleResults);
        Console.WriteLine($"结束战斗结果: {endResult.Success}");
        
        // 测试获取玩家战斗历史
        var query = new DataStorageQueryDto { Page = 1, PageSize = 10 };
        var historyResult = await _service.GetPlayerBattleHistoryAsync("player-1", query);
        Console.WriteLine($"玩家战斗历史数量: {historyResult.Data?.Count ?? 0}");
        
        // 测试获取活跃战斗
        var activeBattlesResult = await _service.GetActiveBattleRecordsAsync();
        Console.WriteLine($"活跃战斗数量: {activeBattlesResult.Data?.Count ?? 0}");
        
        Console.WriteLine("战斗记录操作测试完成");
    }

    /// <summary>
    /// 测试离线数据操作
    /// </summary>
    public async Task TestOfflineDataOperations()
    {
        Console.WriteLine("\n=== 测试离线数据操作 ===");
        
        // 创建测试离线数据
        var testOfflineData = new OfflineDataStorageDto
        {
            Id = "offline-data-1",
            PlayerId = "test-player-3",
            DataType = "PlayerProgress",
            Data = new Dictionary<string, object>
            {
                ["ExperienceGained"] = 200,
                ["ItemsCollected"] = new List<string> { "Iron Sword", "Health Potion" },
                ["QuestsCompleted"] = 2
            },
            IsSynced = false,
            Version = 1
        };
        
        // 测试保存离线数据
        var saveResult = await _service.SaveOfflineDataAsync(testOfflineData);
        Console.WriteLine($"保存离线数据结果: {saveResult.Success}");
        
        // 测试获取未同步的离线数据
        var unsyncedResult = await _service.GetUnsyncedOfflineDataAsync("test-player-3");
        Console.WriteLine($"未同步离线数据数量: {unsyncedResult.Data?.Count ?? 0}");
        
        // 测试标记为已同步
        var markSyncedResult = await _service.MarkOfflineDataSyncedAsync(new List<string> { "offline-data-1" });
        Console.WriteLine($"标记同步结果: {markSyncedResult.Success}");
        
        // 测试清理已同步数据
        var cleanupResult = await _service.CleanupSyncedOfflineDataAsync(DateTime.UtcNow.AddDays(-7));
        Console.WriteLine($"清理数据结果: {cleanupResult.Success}, 清理数量: {cleanupResult.Data}");
        
        Console.WriteLine("离线数据操作测试完成");
    }

    /// <summary>
    /// 测试统计和健康检查
    /// </summary>
    public async Task TestStatsAndHealthCheck()
    {
        Console.WriteLine("\n=== 测试统计和健康检查 ===");
        
        // 测试获取统计信息
        var statsResult = await _service.GetStorageStatsAsync();
        Console.WriteLine($"获取统计信息结果: {statsResult.Success}");
        
        if (statsResult.Success && statsResult.Data != null)
        {
            foreach (var stat in statsResult.Data)
            {
                Console.WriteLine($"  {stat.Key}: {stat.Value}");
            }
        }
        
        // 测试健康检查
        var healthResult = await _service.HealthCheckAsync();
        Console.WriteLine($"健康检查结果: {healthResult.Success}");
        Console.WriteLine($"健康检查状态: {healthResult.Data?.GetValueOrDefault("Status", "Unknown")}");
        
        // 测试搜索玩家
        var searchResult = await _service.SearchPlayersAsync("测试", 5);
        Console.WriteLine($"搜索玩家结果: {searchResult.Success}, 找到 {searchResult.Data?.Count ?? 0} 个玩家");
        
        Console.WriteLine("统计和健康检查测试完成");
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    public async Task RunAllTests()
    {
        try
        {
            Console.WriteLine("开始SQLite数据存储服务测试");
            Console.WriteLine($"测试数据库路径: {_testDatabasePath}");
            
            await TestPlayerOperations();
            await TestTeamOperations();
            await TestActionTargetOperations();
            await TestBattleRecordOperations();
            await TestOfflineDataOperations();
            await TestStatsAndHealthCheck();
            
            Console.WriteLine("\n=== 所有测试完成 ===");
            Console.WriteLine("✅ SQLite数据存储服务测试全部通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 测试失败: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            throw;
        }
    }
}

/// <summary>
/// 测试用的DbContext工厂
/// </summary>
public class TestDbContextFactory : IDbContextFactory<GameDbContext>
{
    private readonly DbContextOptions<GameDbContext> _options;

    public TestDbContextFactory(DbContextOptions<GameDbContext> options)
    {
        _options = options;
    }

    public GameDbContext CreateDbContext()
    {
        return new GameDbContext(_options);
    }
}