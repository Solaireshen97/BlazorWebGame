using System.ComponentModel.DataAnnotations;

namespace BlazorWebGame.Server.Configuration;

/// <summary>
/// 游戏服务器配置选项
/// </summary>
public class GameServerOptions
{
    public const string SectionName = "GameServer";

    /// <summary>
    /// 游戏循环间隔（毫秒）
    /// </summary>
    [Range(100, 5000)]
    public int GameLoopIntervalMs { get; set; } = 500;

    /// <summary>
    /// 最大并发战斗数量
    /// </summary>
    [Range(1, 10000)]
    public int MaxConcurrentBattles { get; set; } = 1000;

    /// <summary>
    /// 战斗超时（秒）
    /// </summary>
    [Range(60, 3600)]
    public int BattleTimeoutSeconds { get; set; } = 1800; // 30分钟

    /// <summary>
    /// 启用开发模式测试
    /// </summary>
    public bool EnableDevelopmentTests { get; set; } = true;

    /// <summary>
    /// 启用性能监控
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = false;

    /// <summary>
    /// 数据存储类型 (Memory, Database)
    /// </summary>
    public string DataStorageType { get; set; } = "Memory";

    /// <summary>
    /// 自动保存间隔（秒）
    /// </summary>
    [Range(30, 3600)]
    public int AutoSaveIntervalSeconds { get; set; } = 300; // 5分钟

    /// <summary>
    /// 数据库提供程序 (InMemory, LocalDb, SqlServer)
    /// </summary>
    public string DatabaseProvider { get; set; } = "InMemory";

    /// <summary>
    /// 启用数据库迁移
    /// </summary>
    public bool EnableDatabaseMigration { get; set; } = true;

    /// <summary>
    /// 启用数据库种子数据
    /// </summary>
    public bool EnableDatabaseSeeding { get; set; } = true;
}
