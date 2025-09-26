using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.DTOs;

/// <summary>
/// 离线结算结果传输对象
/// </summary>
public class OfflineSettlementResultDto
{
    public string PlayerId { get; set; } = string.Empty;
    public TimeSpan OfflineTime { get; set; }
    public int TotalExperience { get; set; }
    public int TotalGold { get; set; }
    public List<OfflineBattleResultDto> BattleResults { get; set; } = new();
    public List<OfflineRewardDto> Rewards { get; set; } = new();
    public DateTime SettlementTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 离线战斗结果传输对象
/// </summary>
public class OfflineBattleResultDto
{
    public string BattleId { get; set; } = string.Empty;
    public bool IsVictory { get; set; }
    public int ExperienceGained { get; set; }
    public int GoldGained { get; set; }
    public TimeSpan Duration { get; set; }
    public string EnemyType { get; set; } = "Normal";
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// 离线奖励传输对象
/// </summary>
public class OfflineRewardDto
{
    public string Type { get; set; } = string.Empty; // Combat, Gathering, Crafting, Idle, etc.
    public string Description { get; set; } = string.Empty;
    public int Experience { get; set; }
    public int Gold { get; set; }
    public List<string> Items { get; set; } = new();
    public Dictionary<string, object> AdditionalRewards { get; set; } = new();
}

/// <summary>
/// 离线结算请求传输对象
/// </summary>
public class OfflineSettlementRequestDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string? TeamId { get; set; }
    public bool ForceSettlement { get; set; } = false;
    public DateTime? LastActiveTime { get; set; }
}

/// <summary>
/// 批量离线结算请求传输对象
/// </summary>
public class BatchOfflineSettlementRequestDto
{
    public List<string> PlayerIds { get; set; } = new();
    public string? TeamId { get; set; }
    public bool ForceSettlement { get; set; } = false;
    public int MaxConcurrency { get; set; } = 10;
}

/// <summary>
/// 队伍离线结算结果传输对象
/// </summary>
public class TeamOfflineSettlementResultDto
{
    public string TeamId { get; set; } = string.Empty;
    public List<OfflineSettlementResultDto> MemberResults { get; set; } = new();
    public TeamProgressSummaryDto TeamProgress { get; set; } = new();
    public DateTime SettlementTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 队伍进度摘要传输对象
/// </summary>
public class TeamProgressSummaryDto
{
    public int TotalExperience { get; set; }
    public int TotalGold { get; set; }
    public int TotalBattles { get; set; }
    public double AverageLevel { get; set; }
    public TimeSpan TotalOfflineTime { get; set; }
    public string OverallPerformance { get; set; } = "Good"; // Excellent, Good, Average, Poor
    public Dictionary<string, object> TeamStats { get; set; } = new();
}

/// <summary>
/// 离线结算配置传输对象
/// </summary>
public class OfflineSettlementConfigDto
{
    public TimeSpan MaxOfflineTime { get; set; } = TimeSpan.FromHours(24);
    public double BaseExperiencePerHour { get; set; } = 50.0;
    public int BaseGoldPerHour { get; set; } = 10;
    public double BattleSimulationSpeedMultiplier { get; set; } = 1.0;
    public Dictionary<string, double> ActionMultipliers { get; set; } = new()
    {
        ["Combat"] = 1.0,
        ["Gathering"] = 0.8,
        ["Crafting"] = 1.2,
        ["Idle"] = 0.5
    };
    public Dictionary<string, double> ProfessionMultipliers { get; set; } = new()
    {
        ["Warrior"] = 1.2,
        ["Mage"] = 1.1,
        ["Archer"] = 1.15,
        ["Default"] = 1.0
    };
}