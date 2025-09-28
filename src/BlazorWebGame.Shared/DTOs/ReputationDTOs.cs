using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.DTOs;

/// <summary>
/// 声望传输对象
/// </summary>
public class ReputationDto
{
    public string CharacterId { get; set; } = string.Empty;
    public Dictionary<string, int> FactionReputation { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// 声望等级信息
/// </summary>
public class ReputationTierDto
{
    public string Name { get; set; } = string.Empty;
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public string BarColorClass { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
}

/// <summary>
/// 声望详细信息（包含等级信息）
/// </summary>
public class ReputationDetailDto
{
    public string CharacterId { get; set; } = string.Empty;
    public string FactionName { get; set; } = string.Empty;
    public int CurrentValue { get; set; }
    public ReputationTierDto CurrentTier { get; set; } = new();
    public ReputationTierDto? NextTier { get; set; }
    public double ProgressPercentage { get; set; }
}

/// <summary>
/// 更新声望请求
/// </summary>
public class UpdateReputationRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string FactionName { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Reason { get; set; } = string.Empty; // 声望变更原因（如完成任务、击杀怪物等）
}

/// <summary>
/// 批量更新声望请求
/// </summary>
public class BatchUpdateReputationRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public List<ReputationChange> Changes { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 单个声望变更
/// </summary>
public class ReputationChange
{
    public string FactionName { get; set; } = string.Empty;
    public int Amount { get; set; }
}

/// <summary>
/// 声望奖励查询请求
/// </summary>
public class ReputationRewardsRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public string? FactionName { get; set; } // 可选，不指定则返回所有阵营的奖励
}

/// <summary>
/// 声望奖励信息
/// </summary>
public class ReputationRewardDto
{
    public string FactionName { get; set; } = string.Empty;
    public string TierName { get; set; } = string.Empty;
    public int RequiredReputation { get; set; }
    public List<RewardItemDto> Items { get; set; } = new();
    public List<string> Perks { get; set; } = new(); // 声望特权，如折扣、特殊任务等
}

/// <summary>
/// 奖励物品
/// </summary>
public class RewardItemDto
{
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 声望历史记录
/// </summary>
public class ReputationHistoryDto
{
    public DateTime Timestamp { get; set; }
    public string FactionName { get; set; } = string.Empty;
    public int AmountChanged { get; set; }
    public int NewTotal { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // 来源：任务、战斗、事件等
}

/// <summary>
/// 声望统计信息
/// </summary>
public class ReputationStatsDto
{
    public string CharacterId { get; set; } = string.Empty;
    public int TotalReputationEarned { get; set; }
    public Dictionary<string, int> FactionTotals { get; set; } = new();
    public string HighestFaction { get; set; } = string.Empty;
    public int HighestReputationValue { get; set; }
    public DateTime LastUpdated { get; set; }
}