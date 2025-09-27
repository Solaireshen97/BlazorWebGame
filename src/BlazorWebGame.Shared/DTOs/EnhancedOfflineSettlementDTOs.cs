using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.DTOs;

/// <summary>
/// 增强的离线结算结果传输对象
/// </summary>
public class EnhancedOfflineSettlementResultDto
{
    public string PlayerId { get; set; } = string.Empty;
    
    // 时间信息
    public TimeSpan ProcessedTime { get; set; }
    public TimeSpan RawOfflineTime { get; set; }
    public TimeSpan EffectiveOfflineTime { get; set; }
    public DateTime NextTriggerTime { get; set; }
    
    // 基础奖励
    public int TotalExperience { get; set; }
    public int TotalGold { get; set; }
    public int LevelUps { get; set; }
    
    // 活动信息
    public string ActivityType { get; set; } = string.Empty;
    public List<OfflineRewardDto> Rewards { get; set; } = new();
    
    // 处理信息
    public string ProcessingMode { get; set; } = string.Empty; // EventDriven, Legacy, Hybrid
    public double ProcessingTimeMs { get; set; }
    
    // 时间管理
    public bool DecayApplied { get; set; }
    public double DecayFactor { get; set; } = 1.0;
    public bool OverTimeLimit { get; set; }
    
    // 警告和附加数据
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    
    // 时间戳
    public DateTime SettlementTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 离线收益预估传输对象
/// </summary>
public class OfflineRevenueEstimateDto
{
    public string PlayerId { get; set; } = string.Empty;
    public TimeSpan EstimatedOfflineTime { get; set; }
    public int EstimatedExperience { get; set; }
    public int EstimatedGold { get; set; }
    public List<OfflineRewardDto> EstimatedRewards { get; set; } = new();
    public string ActivityType { get; set; } = string.Empty;
    public DateTime NextTriggerTime { get; set; }
    public bool DecayApplied { get; set; }
    public double DecayFactor { get; set; } = 1.0;
    public bool MaxOfflineTimeReached { get; set; }
    public DateTime EstimatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 增强的离线结算请求传输对象
/// </summary>
public class EnhancedOfflineSettlementRequestDto
{
    public string PlayerId { get; set; } = string.Empty;
    public bool UseEventDriven { get; set; } = true;
    public bool EnableTimeDecay { get; set; } = true;
    public bool ForceSettlement { get; set; } = false;
    public TimeSpan? CustomOfflineTime { get; set; }
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// 智能批量离线结算请求传输对象
/// </summary>
public class SmartBatchOfflineSettlementRequestDto
{
    public List<string> PlayerIds { get; set; } = new();
    public int MaxConcurrency { get; set; } = 10;
    public bool UseEventDriven { get; set; } = true;
    public bool EnableTimeDecay { get; set; } = true;
    public bool PrioritizeByActivity { get; set; } = true;
    public int BatchDelayMs { get; set; } = 100;
    public Dictionary<string, object> GlobalOptions { get; set; } = new();
}

/// <summary>
/// 离线活动进度同步传输对象
/// </summary>
public class OfflineActivityProgressDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public DateTime NextTriggerTime { get; set; }
    public double ProgressPercentage { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public Dictionary<string, object> ActivityData { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 离线战斗事件快照传输对象  
/// </summary>
public class OfflineCombatSnapshotDto
{
    public string PlayerId { get; set; } = string.Empty;
    public double GameClock { get; set; }
    public int CurrentWave { get; set; }
    public double Difficulty { get; set; }
    public int PlayerHealth { get; set; }
    public int PlayerMaxHealth { get; set; }
    public int EnemyHealth { get; set; }
    public int EnemyMaxHealth { get; set; }
    public List<CombatEventDto> PendingEvents { get; set; } = new();
    public DateTime SnapshotTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 战斗事件传输对象
/// </summary>
public class CombatEventDto
{
    public string EventType { get; set; } = string.Empty;
    public double TriggerTime { get; set; }
    public Dictionary<string, object> EventData { get; set; } = new();
    public int Version { get; set; } = 1;
}

/// <summary>
/// 循环活动状态传输对象
/// </summary>
public class RecurringActivityStatusDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public TimeSpan CycleDuration { get; set; }
    public DateTime NextCompletionTime { get; set; }
    public int CompletedCycles { get; set; }
    public double Efficiency { get; set; } = 1.0;
    public List<string> ResourcesProduced { get; set; } = new();
    public Dictionary<string, object> ActivityState { get; set; } = new();
}

/// <summary>
/// 离线时间分析传输对象
/// </summary>
public class OfflineTimeAnalysisDto
{
    public TimeSpan RawOfflineTime { get; set; }
    public TimeSpan EffectiveOfflineTime { get; set; }
    public double DecayFactor { get; set; } = 1.0;
    public bool IsOverLimit { get; set; }
    public bool RequiresDecay { get; set; }
    public string TimeCategory { get; set; } = string.Empty; // Short, Medium, Long, Extended
    public List<string> TimeWarnings { get; set; } = new();
}

/// <summary>
/// 离线结算统计摘要传输对象
/// </summary>
public class OfflineSettlementSummaryDto
{
    public string PlayerId { get; set; } = string.Empty;
    public int TotalSettlements { get; set; }
    public TimeSpan TotalOfflineTime { get; set; }
    public long TotalExperienceGained { get; set; }
    public long TotalGoldGained { get; set; }
    public Dictionary<string, int> ActivityBreakdown { get; set; } = new();
    public double AverageEfficiency { get; set; }
    public DateTime FirstSettlement { get; set; }
    public DateTime LastSettlement { get; set; }
}

/// <summary>
/// 离线结算性能指标传输对象
/// </summary>
public class OfflineSettlementMetricsDto
{
    public double AverageProcessingTimeMs { get; set; }
    public int TotalPlayersProcessed { get; set; }
    public int SuccessfulSettlements { get; set; }
    public int FailedSettlements { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<string, double> ProcessingModeBreakdown { get; set; } = new();
    public Dictionary<string, double> ActivityTypeBreakdown { get; set; } = new();
    public DateTime MetricsGeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 离线结算配置更新传输对象
/// </summary>
public class OfflineSettlementConfigUpdateDto
{
    public TimeSpan? MaxOfflineTime { get; set; }
    public double? BaseExperiencePerHour { get; set; }
    public int? BaseGoldPerHour { get; set; }
    public double? TimeDecayThreshold { get; set; }
    public double? DecayFactor { get; set; }
    public Dictionary<string, double>? ActivityMultipliers { get; set; }
    public Dictionary<string, double>? ProfessionMultipliers { get; set; }
    public bool? EnableEventDrivenMode { get; set; }
    public bool? EnableTimeDecay { get; set; }
}

/// <summary>
/// 离线结算调试信息传输对象
/// </summary>
public class OfflineSettlementDebugDto
{
    public string PlayerId { get; set; } = string.Empty;
    public List<string> ProcessingSteps { get; set; } = new();
    public Dictionary<string, object> DebugData { get; set; } = new();
    public List<string> PerformanceMarkers { get; set; } = new();
    public double TotalProcessingTimeMs { get; set; }
    public DateTime DebugCapturedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 队伍离线协作状态传输对象
/// </summary>
public class TeamOfflineCollaborationDto
{
    public string TeamId { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = new();
    public Dictionary<string, TimeSpan> MemberOfflineTimes { get; set; } = new();
    public double SynchronizationRate { get; set; }
    public string CooperationMode { get; set; } = string.Empty; // HighSync, MediumSync, LowSync, Individual
    public TimeSpan CollaborativeTime { get; set; }
    public double TeamEfficiencyBonus { get; set; }
    public List<TeamCollaborativeRewardDto> CollaborativeRewards { get; set; } = new();
}

/// <summary>
/// 队伍协作奖励传输对象
/// </summary>
public class TeamCollaborativeRewardDto
{
    public string RewardType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, int> MemberRewards { get; set; } = new(); // PlayerId -> RewardAmount
    public double SyncRateBonus { get; set; }
    public double TeamSizeBonus { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}