using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 离线活动管理器 - 实现基于事件队列的离线推进机制
/// 按照离线结算设计文档实现"整段+余数"模式和事件驱动推进
/// </summary>
public class OfflineActivityManager
{
    private readonly IDataStorageService _dataStorageService;
    private readonly UnifiedEventService _eventService;
    private readonly ILogger<OfflineActivityManager> _logger;
    
    // 配置参数
    private readonly TimeSpan _maxOfflineTime = TimeSpan.FromHours(24); // 最大离线时间上限
    private readonly TimeSpan _settlementGranularity = TimeSpan.FromHours(1); // 结算粒度
    private readonly double _timeJumpThreshold = 3600.0; // 时间跳跃阈值（秒）

    public OfflineActivityManager(
        IDataStorageService dataStorageService,
        UnifiedEventService eventService,
        ILogger<OfflineActivityManager> logger)
    {
        _dataStorageService = dataStorageService;
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// 处理角色的离线活动推进
    /// 实现设计文档中的"整段+余数"模式
    /// </summary>
    public async Task<OfflineActivityResult> ProcessOfflineActivityAsync(string playerId)
    {
        try
        {
            var player = await _dataStorageService.GetPlayerAsync(playerId);
            if (player == null)
            {
                return new OfflineActivityResult
                {
                    Success = false,
                    Message = "玩家不存在"
                };
            }

            // 1. 计算有效离线时间
            var rawOfflineTime = DateTime.UtcNow - player.LastActiveAt;
            var effectiveOfflineTime = rawOfflineTime > _maxOfflineTime ? _maxOfflineTime : rawOfflineTime;

            _logger.LogInformation("处理玩家 {PlayerId} 离线活动，原始离线时间 {RawTime}，有效时间 {EffectiveTime}",
                SafeLogId(playerId), rawOfflineTime, effectiveOfflineTime);

            if (effectiveOfflineTime.TotalMinutes < 1)
            {
                return new OfflineActivityResult
                {
                    Success = true,
                    Message = "离线时间不足1分钟，无需处理",
                    PlayerId = playerId,
                    ProcessedTime = TimeSpan.Zero
                };
            }

            // 2. 根据当前活动类型选择处理器
            var activityProcessor = GetActivityProcessor(player.CurrentAction);
            
            // 3. 执行"整段+余数"处理模式
            var result = await ProcessWithSegmentedTime(player, effectiveOfflineTime, activityProcessor);
            
            // 4. 处理超时警告
            if (rawOfflineTime > _maxOfflineTime)
            {
                result.Warnings.Add($"离线时间超过上限，仅结算前 {_maxOfflineTime.TotalHours} 小时");
                _logger.LogWarning("玩家 {PlayerId} 离线时间超限：{RawTime} > {MaxTime}",
                    SafeLogId(playerId), rawOfflineTime, _maxOfflineTime);
            }

            // 5. 更新玩家状态
            await UpdatePlayerStateAsync(player, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理玩家 {PlayerId} 离线活动时发生错误", SafeLogId(playerId));
            return new OfflineActivityResult
            {
                Success = false,
                Message = $"处理离线活动失败: {ex.Message}",
                PlayerId = playerId
            };
        }
    }

    /// <summary>
    /// 实现"整段+余数"时间处理模式
    /// </summary>
    private async Task<OfflineActivityResult> ProcessWithSegmentedTime(
        PlayerStorageDto player, 
        TimeSpan effectiveTime, 
        IOfflineActivityProcessor processor)
    {
        var result = new OfflineActivityResult
        {
            Success = true,
            PlayerId = player.Id,
            ProcessedTime = effectiveTime
        };

        // 1. 计算整段数量和余数
        var totalSegments = (int)(effectiveTime.TotalSeconds / _settlementGranularity.TotalSeconds);
        var remainder = effectiveTime - TimeSpan.FromSeconds(totalSegments * _settlementGranularity.TotalSeconds);

        _logger.LogDebug("玩家 {PlayerId} 时间分段：{Segments} 个整段，余数 {Remainder}",
            SafeLogId(player.Id), totalSegments, remainder);

        // 2. 处理整段时间（批量处理，高效）
        if (totalSegments > 0)
        {
            var segmentResult = await processor.ProcessBulkSegmentsAsync(
                player, 
                _settlementGranularity, 
                totalSegments);
            
            result.MergeResults(segmentResult);
        }

        // 3. 处理余数时间（精确处理）
        if (remainder.TotalMinutes >= 1)
        {
            var remainderResult = await processor.ProcessRemainingTimeAsync(player, remainder);
            result.MergeResults(remainderResult);
        }

        // 4. 更新下一次触发时间
        result.NextTriggerTime = CalculateNextTriggerTime(player, processor, remainder);

        return result;
    }

    /// <summary>
    /// 获取活动处理器
    /// </summary>
    private IOfflineActivityProcessor GetActivityProcessor(string activityType)
    {
        return activityType.ToLower() switch
        {
            "combat" or "battle" => new CombatEventProcessor(_eventService, _logger),
            "gathering" => new RecurringActivityProcessor(ActivityType.Gathering, _logger),
            "crafting" or "production" => new RecurringActivityProcessor(ActivityType.Production, _logger),
            _ => new IdleActivityProcessor(_logger)
        };
    }

    /// <summary>
    /// 计算下一次触发时间
    /// </summary>
    private DateTime CalculateNextTriggerTime(
        PlayerStorageDto player, 
        IOfflineActivityProcessor processor, 
        TimeSpan remainderTime)
    {
        var baseCycleDuration = processor.GetBaseCycleDuration(player);
        var remainingCycleTime = baseCycleDuration - remainderTime;
        
        return DateTime.UtcNow + (remainingCycleTime.TotalSeconds > 0 ? remainingCycleTime : baseCycleDuration);
    }

    /// <summary>
    /// 更新玩家状态
    /// </summary>
    private async Task UpdatePlayerStateAsync(PlayerStorageDto player, OfflineActivityResult result)
    {
        // 应用奖励
        player.Experience += result.TotalExperience;
        player.Gold += result.TotalGold;
        
        // 更新等级
        var newLevel = CalculateLevel(player.Experience);
        if (newLevel > player.Level)
        {
            player.Level = newLevel;
            player.MaxHealth += (newLevel - player.Level) * 10;
            player.Health = player.MaxHealth;
            
            result.LevelUps = newLevel - player.Level;
        }

        // 更新时间戳
        player.LastActiveAt = DateTime.UtcNow;
        player.UpdatedAt = DateTime.UtcNow;

        // 保存玩家数据
        await _dataStorageService.SavePlayerAsync(player);
        
        // 记录离线活动历史
        await RecordOfflineActivityHistory(result);
    }

    /// <summary>
    /// 记录离线活动历史
    /// </summary>
    private async Task RecordOfflineActivityHistory(OfflineActivityResult result)
    {
        var historyRecord = new OfflineDataStorageDto
        {
            Id = Guid.NewGuid().ToString(),
            PlayerId = result.PlayerId,
            DataType = "OfflineActivityHistory",
            Data = new Dictionary<string, object>
            {
                ["ProcessedTime"] = result.ProcessedTime.ToString(),
                ["TotalExperience"] = result.TotalExperience,
                ["TotalGold"] = result.TotalGold,
                ["LevelUps"] = result.LevelUps,
                ["ActivityType"] = result.ActivityType,
                ["NextTriggerTime"] = result.NextTriggerTime,
                ["ProcessingMode"] = "SegmentedTime",
                ["Timestamp"] = DateTime.UtcNow
            },
            IsSynced = true,
            Version = 1
        };

        await _dataStorageService.SaveOfflineDataAsync(historyRecord);
    }

    /// <summary>
    /// 计算等级
    /// </summary>
    private int CalculateLevel(int experience)
    {
        return Math.Max(1, experience / 100 + 1);
    }

    /// <summary>
    /// 安全的日志ID处理
    /// </summary>
    private static string SafeLogId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "[empty]";
        
        var sanitized = new string(id.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return sanitized.Substring(0, Math.Min(8, sanitized.Length)) + (sanitized.Length > 8 ? "..." : "");
    }
}

/// <summary>
/// 离线活动结果
/// </summary>
public class OfflineActivityResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public TimeSpan ProcessedTime { get; set; }
    public int TotalExperience { get; set; }
    public int TotalGold { get; set; }
    public int LevelUps { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public DateTime NextTriggerTime { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<OfflineRewardDto> Rewards { get; set; } = new();
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    /// <summary>
    /// 合并其他结果
    /// </summary>
    public void MergeResults(OfflineActivityResult other)
    {
        TotalExperience += other.TotalExperience;
        TotalGold += other.TotalGold;
        LevelUps += other.LevelUps;
        Rewards.AddRange(other.Rewards);
        Warnings.AddRange(other.Warnings);
        
        foreach (var kvp in other.AdditionalData)
        {
            AdditionalData[kvp.Key] = kvp.Value;
        }
    }
}

/// <summary>
/// 离线活动处理器接口
/// </summary>
public interface IOfflineActivityProcessor
{
    Task<OfflineActivityResult> ProcessBulkSegmentsAsync(PlayerStorageDto player, TimeSpan segmentDuration, int segmentCount);
    Task<OfflineActivityResult> ProcessRemainingTimeAsync(PlayerStorageDto player, TimeSpan remainingTime);
    TimeSpan GetBaseCycleDuration(PlayerStorageDto player);
    string GetActivityName();
}

/// <summary>
/// 活动类型枚举
/// </summary>
public enum ActivityType
{
    Combat,
    Gathering,
    Production,
    Idle
}