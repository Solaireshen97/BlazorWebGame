using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorWebGame.Server.Services.System;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Events;
using BlazorWebGame.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Services.Activities;

/// <summary>
/// 增强的离线结算服务 - 集成新的事件驱动离线推进机制
/// 保留原有接口兼容性，同时提供增强功能
/// </summary>
public class EnhancedOfflineSettlementService
{
    private readonly OfflineActivityManager _activityManager;
    private readonly OfflineSettlementService _legacyService;
    private readonly UnifiedEventService _eventService;
    private readonly IDataStorageService _dataStorageService;
    private readonly ILogger<EnhancedOfflineSettlementService> _logger;
    
    // 增强配置
    private readonly TimeSpan _maxOfflineTime = TimeSpan.FromHours(24);
    private readonly TimeSpan _timeDecayThreshold = TimeSpan.FromHours(48);
    private readonly double _decayFactor = 0.8; // 超时衰减系数
    private readonly int _maxBatchSize = 50; // 批量处理最大大小

    public EnhancedOfflineSettlementService(
        OfflineActivityManager activityManager,
        OfflineSettlementService legacyService,
        UnifiedEventService eventService,
        IDataStorageService dataStorageService,
        ILogger<EnhancedOfflineSettlementService> logger)
    {
        _activityManager = activityManager;
        _legacyService = legacyService;
        _eventService = eventService;
        _dataStorageService = dataStorageService;
        _logger = logger;
    }

    /// <summary>
    /// 增强的单玩家离线结算 - 使用新的事件驱动机制
    /// </summary>
    public async Task<ApiResponse<EnhancedOfflineSettlementResultDto>> ProcessEnhancedPlayerOfflineSettlementAsync(
        string playerId, 
        bool useEventDriven = true,
        bool enableTimeDecay = true)
    {
        try
        {
            _logger.LogInformation("开始增强离线结算：玩家 {PlayerId}，事件驱动模式 {EventDriven}",
                SafeLogId(playerId), useEventDriven);

            var startTime = DateTime.UtcNow;

            // 1. 获取玩家数据和基础验证
            var player = await _dataStorageService.GetPlayerAsync(playerId);
            if (player == null)
            {
                return new ApiResponse<EnhancedOfflineSettlementResultDto>
                {
                    Success = false,
                    Message = "玩家不存在"
                };
            }

            // 2. 计算离线时间和应用衰减策略
            var rawOfflineTime = DateTime.UtcNow - player.LastActiveAt;
            var timeAnalysis = AnalyzeOfflineTime(rawOfflineTime, enableTimeDecay);

            // 3. 安全检查和反作弊验证
            var securityCheck = await PerformSecurityChecks(player, timeAnalysis);
            if (!securityCheck.IsValid)
            {
                return new ApiResponse<EnhancedOfflineSettlementResultDto>
                {
                    Success = false,
                    Message = securityCheck.ErrorMessage
                };
            }

            // 4. 选择结算模式
            var settlementResult = useEventDriven 
                ? await ProcessWithEventDrivenMode(player, timeAnalysis)
                : await ProcessWithLegacyMode(player, timeAnalysis);

            // 5. 应用时间衰减（如果启用）
            if (enableTimeDecay && timeAnalysis.DecayFactor < 1.0)
            {
                ApplyTimeDecay(settlementResult, timeAnalysis.DecayFactor);
            }

            // 6. 生成增强结果
            var enhancedResult = CreateEnhancedResult(settlementResult, timeAnalysis, startTime);

            // 7. 触发后处理事件
            await TriggerPostSettlementEvents(player, enhancedResult);

            _logger.LogInformation("增强离线结算完成：玩家 {PlayerId}，处理时间 {ProcessTime}ms，获得经验 {Experience}，金币 {Gold}",
                SafeLogId(playerId), (DateTime.UtcNow - startTime).TotalMilliseconds, 
                enhancedResult.Data?.TotalExperience ?? 0, enhancedResult.Data?.TotalGold ?? 0);

            return enhancedResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "增强离线结算失败：玩家 {PlayerId}", SafeLogId(playerId));
            return new ApiResponse<EnhancedOfflineSettlementResultDto>
            {
                Success = false,
                Message = $"离线结算失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 智能批量离线结算 - 支持优先级和并发控制
    /// </summary>
    public async Task<BatchOperationResponseDto<EnhancedOfflineSettlementResultDto>> ProcessSmartBatchOfflineSettlementAsync(
        List<string> playerIds,
        BatchSettlementOptions? options = null)
    {
        options ??= new BatchSettlementOptions();
        var result = new BatchOperationResponseDto<EnhancedOfflineSettlementResultDto>
        {
            TotalProcessed = playerIds.Count
        };

        try
        {
            _logger.LogInformation("开始智能批量离线结算：{PlayerCount} 个玩家，并发度 {Concurrency}",
                playerIds.Count, options.MaxConcurrency);

            // 1. 分析玩家优先级
            var priorityGroups = await AnalyzePlayerPriorities(playerIds, options);

            // 2. 按优先级分批处理
            foreach (var group in priorityGroups.OrderByDescending(g => g.Priority))
            {
                var batchTasks = group.PlayerIds
                    .Take(Math.Min(group.PlayerIds.Count, _maxBatchSize))
                    .Select(async playerId =>
                    {
                        try
                        {
                            var playerResult = await ProcessEnhancedPlayerOfflineSettlementAsync(
                                playerId, 
                                options.UseEventDriven, 
                                options.EnableTimeDecay);

                            if (playerResult.Success && playerResult.Data != null)
                            {
                                lock (result)
                                {
                                    result.SuccessfulItems.Add(playerResult.Data);
                                    result.SuccessCount++;
                                }
                            }
                            else
                            {
                                lock (result)
                                {
                                    result.Errors.Add($"玩家 {SafeLogId(playerId)}: {playerResult.Message}");
                                    result.ErrorCount++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (result)
                            {
                                result.Errors.Add($"玩家 {SafeLogId(playerId)}: {ex.Message}");
                                result.ErrorCount++;
                            }
                        }
                    });

                // 并发执行当前批次
                await Task.WhenAll(batchTasks);

                // 批次间延迟（避免系统过载）
                if (options.BatchDelayMs > 0)
                {
                    await Task.Delay(options.BatchDelayMs);
                }
            }

            _logger.LogInformation("智能批量离线结算完成：成功 {SuccessCount}，失败 {ErrorCount}",
                result.SuccessCount, result.ErrorCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "智能批量离线结算发生错误");
            result.Errors.Add($"批量处理错误: {ex.Message}");
            result.ErrorCount = Math.Max(result.ErrorCount, 1);
            return result;
        }
    }

    /// <summary>
    /// 预估离线收益 - 不执行实际结算
    /// </summary>
    public async Task<ApiResponse<OfflineRevenueEstimateDto>> EstimateOfflineRevenueAsync(
        string playerId,
        TimeSpan? customOfflineTime = null)
    {
        try
        {
            var player = await _dataStorageService.GetPlayerAsync(playerId);
            if (player == null)
            {
                return new ApiResponse<OfflineRevenueEstimateDto>
                {
                    Success = false,
                    Message = "玩家不存在"
                };
            }

            var offlineTime = customOfflineTime ?? DateTime.UtcNow - player.LastActiveAt;
            var timeAnalysis = AnalyzeOfflineTime(offlineTime, true);

            // 模拟结算但不保存结果
            var activityResult = await _activityManager.ProcessOfflineActivityAsync(playerId);
            
            var estimate = new OfflineRevenueEstimateDto
            {
                PlayerId = playerId,
                EstimatedOfflineTime = timeAnalysis.EffectiveTime,
                EstimatedExperience = activityResult.TotalExperience,
                EstimatedGold = activityResult.TotalGold,
                EstimatedRewards = activityResult.Rewards,
                ActivityType = player.CurrentAction,
                NextTriggerTime = activityResult.NextTriggerTime,
                DecayApplied = timeAnalysis.DecayFactor < 1.0,
                DecayFactor = timeAnalysis.DecayFactor,
                MaxOfflineTimeReached = timeAnalysis.RawTime > _maxOfflineTime
            };

            return new ApiResponse<OfflineRevenueEstimateDto>
            {
                Success = true,
                Data = estimate,
                Message = "离线收益预估完成"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预估离线收益失败：玩家 {PlayerId}", SafeLogId(playerId));
            return new ApiResponse<OfflineRevenueEstimateDto>
            {
                Success = false,
                Message = $"预估失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 分析离线时间并应用策略
    /// </summary>
    private OfflineTimeAnalysis AnalyzeOfflineTime(TimeSpan rawOfflineTime, bool enableDecay)
    {
        var analysis = new OfflineTimeAnalysis
        {
            RawTime = rawOfflineTime,
            EffectiveTime = rawOfflineTime > _maxOfflineTime ? _maxOfflineTime : rawOfflineTime,
            DecayFactor = 1.0
        };

        // 应用时间衰减策略
        if (enableDecay && rawOfflineTime > _timeDecayThreshold)
        {
            var excessTime = rawOfflineTime - _timeDecayThreshold;
            var decayMultiplier = Math.Pow(_decayFactor, excessTime.TotalHours / 24.0);
            analysis.DecayFactor = Math.Max(0.5, decayMultiplier); // 最低保留50%收益
        }

        analysis.IsOverLimit = rawOfflineTime > _maxOfflineTime;
        analysis.RequiresDecay = analysis.DecayFactor < 1.0;

        return analysis;
    }

    /// <summary>
    /// 安全检查和反作弊验证
    /// </summary>
    private async Task<SecurityCheckResult> PerformSecurityChecks(PlayerStorageDto player, OfflineTimeAnalysis timeAnalysis)
    {
        var result = new SecurityCheckResult { IsValid = true };

        // 1. 时间回拨检测
        if (player.LastActiveAt > DateTime.UtcNow.AddMinutes(5))
        {
            result.IsValid = false;
            result.ErrorMessage = "检测到时间异常，请同步系统时间";
            _logger.LogWarning("时间回拨检测：玩家 {PlayerId}，上次活动时间 {LastActive} > 当前时间 {Now}",
                SafeLogId(player.Id), player.LastActiveAt, DateTime.UtcNow);
            return result;
        }

        // 2. 异常离线时间检测
        if (timeAnalysis.RawTime.TotalDays > 30)
        {
            result.IsValid = false;
            result.ErrorMessage = "离线时间异常，请联系管理员";
            _logger.LogWarning("异常离线时间：玩家 {PlayerId}，离线时间 {OfflineTime} 天",
                SafeLogId(player.Id), timeAnalysis.RawTime.TotalDays);
            return result;
        }

        // 3. 多端并发检测
        var activeSession = await CheckActiveSession(player.Id);
        if (activeSession)
        {
            result.IsValid = false;
            result.ErrorMessage = "检测到其他活跃会话，请先结束其他登录";
            _logger.LogWarning("多端并发检测：玩家 {PlayerId} 存在活跃会话", SafeLogId(player.Id));
            return result;
        }

        return result;
    }

    /// <summary>
    /// 使用事件驱动模式处理
    /// </summary>
    private async Task<OfflineActivityResult> ProcessWithEventDrivenMode(PlayerStorageDto player, OfflineTimeAnalysis timeAnalysis)
    {
        _logger.LogDebug("使用事件驱动模式处理离线结算：玩家 {PlayerId}", SafeLogId(player.Id));
        return await _activityManager.ProcessOfflineActivityAsync(player.Id);
    }

    /// <summary>
    /// 使用传统模式处理（向后兼容）
    /// </summary>
    private async Task<OfflineActivityResult> ProcessWithLegacyMode(PlayerStorageDto player, OfflineTimeAnalysis timeAnalysis)
    {
        _logger.LogDebug("使用传统模式处理离线结算：玩家 {PlayerId}", SafeLogId(player.Id));
        
        var legacyResult = await _legacyService.ProcessPlayerOfflineSettlementAsync(player.Id);
        
        // 转换传统结果为新格式
        return new OfflineActivityResult
        {
            Success = legacyResult.Success,
            Message = legacyResult.Message ?? string.Empty,
            PlayerId = player.Id,
            ProcessedTime = timeAnalysis.EffectiveTime,
            TotalExperience = legacyResult.Data?.TotalExperience ?? 0,
            TotalGold = legacyResult.Data?.TotalGold ?? 0,
            ActivityType = player.CurrentAction,
            Rewards = legacyResult.Data?.Rewards ?? new List<OfflineRewardDto>()
        };
    }

    /// <summary>
    /// 应用时间衰减
    /// </summary>
    private void ApplyTimeDecay(OfflineActivityResult result, double decayFactor)
    {
        if (decayFactor >= 1.0) return;

        var originalExp = result.TotalExperience;
        var originalGold = result.TotalGold;

        result.TotalExperience = (int)(result.TotalExperience * decayFactor);
        result.TotalGold = (int)(result.TotalGold * decayFactor);

        result.Warnings.Add($"由于长时间离线，收益已衰减至 {decayFactor:P1}");
        
        // 更新奖励中的数值
        foreach (var reward in result.Rewards)
        {
            reward.Experience = (int)(reward.Experience * decayFactor);
            reward.Gold = (int)(reward.Gold * decayFactor);
        }

        _logger.LogInformation("应用时间衰减：玩家 {PlayerId}，衰减系数 {DecayFactor:P2}，经验 {OriginalExp} -> {NewExp}，金币 {OriginalGold} -> {NewGold}",
            SafeLogId(result.PlayerId), decayFactor, originalExp, result.TotalExperience, originalGold, result.TotalGold);
    }

    /// <summary>
    /// 创建增强结果
    /// </summary>
    private ApiResponse<EnhancedOfflineSettlementResultDto> CreateEnhancedResult(
        OfflineActivityResult activityResult, 
        OfflineTimeAnalysis timeAnalysis, 
        DateTime startTime)
    {
        var enhancedResult = new EnhancedOfflineSettlementResultDto
        {
            PlayerId = activityResult.PlayerId,
            ProcessedTime = activityResult.ProcessedTime,
            RawOfflineTime = timeAnalysis.RawTime,
            EffectiveOfflineTime = timeAnalysis.EffectiveTime,
            TotalExperience = activityResult.TotalExperience,
            TotalGold = activityResult.TotalGold,
            LevelUps = activityResult.LevelUps,
            ActivityType = activityResult.ActivityType,
            NextTriggerTime = activityResult.NextTriggerTime,
            Rewards = activityResult.Rewards,
            ProcessingMode = "EventDriven",
            ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
            DecayApplied = timeAnalysis.RequiresDecay,
            DecayFactor = timeAnalysis.DecayFactor,
            OverTimeLimit = timeAnalysis.IsOverLimit,
            Warnings = activityResult.Warnings,
            AdditionalData = activityResult.AdditionalData,
            SettlementTime = DateTime.UtcNow
        };

        return new ApiResponse<EnhancedOfflineSettlementResultDto>
        {
            Success = activityResult.Success,
            Data = enhancedResult,
            Message = activityResult.Success ? "增强离线结算完成" : activityResult.Message
        };
    }

    /// <summary>
    /// 分析玩家优先级
    /// </summary>
    private async Task<List<PlayerPriorityGroup>> AnalyzePlayerPriorities(List<string> playerIds, BatchSettlementOptions options)
    {
        var groups = new List<PlayerPriorityGroup>
        {
            new PlayerPriorityGroup { Priority = 1, PlayerIds = new List<string>() }, // 高优先级
            new PlayerPriorityGroup { Priority = 2, PlayerIds = new List<string>() }, // 中优先级
            new PlayerPriorityGroup { Priority = 3, PlayerIds = new List<string>() }  // 低优先级
        };

        foreach (var playerId in playerIds)
        {
            try
            {
                var player = await _dataStorageService.GetPlayerAsync(playerId);
                if (player != null)
                {
                    var offlineTime = DateTime.UtcNow - player.LastActiveAt;
                    var priority = CalculatePlayerPriority(player, offlineTime);
                    groups[priority - 1].PlayerIds.Add(playerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "分析玩家优先级失败：{PlayerId}", SafeLogId(playerId));
                groups[2].PlayerIds.Add(playerId); // 默认低优先级
            }
        }

        return groups.Where(g => g.PlayerIds.Any()).ToList();
    }

    /// <summary>
    /// 计算玩家优先级
    /// </summary>
    private int CalculatePlayerPriority(PlayerStorageDto player, TimeSpan offlineTime)
    {
        // 高优先级：活跃玩家、高等级、短离线时间
        if (player.Level >= 20 && offlineTime.TotalHours <= 6)
            return 1;

        // 中优先级：中等等级或中等离线时间
        if (player.Level >= 10 || offlineTime.TotalHours <= 24)
            return 2;

        // 低优先级：其他情况
        return 3;
    }

    /// <summary>
    /// 触发后处理事件
    /// </summary>
    private async Task TriggerPostSettlementEvents(PlayerStorageDto player, ApiResponse<EnhancedOfflineSettlementResultDto> result)
    {
        if (!result.Success || result.Data == null) return;

        try
        {
            // 触发离线结算完成事件
            var actorId = (ulong)Math.Abs(player.Id.GetHashCode());
            _eventService.EnqueueEvent(GameEventTypes.OFFLINE_SETTLEMENT_COMPLETED, 
                EventPriority.Analytics, actorId, 0);

            _logger.LogDebug("触发离线结算完成事件：玩家 {PlayerId}", SafeLogId(player.Id));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "触发后处理事件失败：玩家 {PlayerId}", SafeLogId(player.Id));
        }
    }

    /// <summary>
    /// 检查活跃会话
    /// </summary>
    private async Task<bool> CheckActiveSession(string playerId)
    {
        // 这里可以集成实际的会话管理系统
        // 例如检查 SignalR 连接、Redis 会话等
        return false; // 简化实现
    }

    private static string SafeLogId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "[empty]";
        
        var sanitized = new string(id.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return sanitized.Substring(0, Math.Min(8, sanitized.Length)) + (sanitized.Length > 8 ? "..." : "");
    }
}

// 数据结构
public class OfflineTimeAnalysis
{
    public TimeSpan RawTime { get; set; }
    public TimeSpan EffectiveTime { get; set; }
    public double DecayFactor { get; set; } = 1.0;
    public bool IsOverLimit { get; set; }
    public bool RequiresDecay { get; set; }
}

public class SecurityCheckResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class PlayerPriorityGroup
{
    public int Priority { get; set; }
    public List<string> PlayerIds { get; set; } = new();
}

public class BatchSettlementOptions
{
    public int MaxConcurrency { get; set; } = 10;
    public bool UseEventDriven { get; set; } = true;
    public bool EnableTimeDecay { get; set; } = true;
    public int BatchDelayMs { get; set; } = 100;
}

public class OfflineSettlementCompletedEvent
{
    public string PlayerId { get; set; } = string.Empty;
    public int ExperienceGained { get; set; }
    public int GoldGained { get; set; }
    public int LevelUps { get; set; }
    public double ProcessingTime { get; set; }
    public string ActivityType { get; set; } = string.Empty;
}