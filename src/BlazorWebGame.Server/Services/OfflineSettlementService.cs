using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 离线结算服务 - 处理玩家或队伍的离线期间数据结算
/// </summary>
public class OfflineSettlementService
{
    private readonly IDataStorageService _dataStorageService;
    private readonly ILogger<OfflineSettlementService> _logger;
    
    // 配置参数
    private readonly TimeSpan _maxOfflineSettlementTime = TimeSpan.FromHours(24); // 最大离线结算时间
    private readonly double _baseExperiencePerHour = 50.0; // 基础每小时经验值
    private readonly int _baseGoldPerHour = 10; // 基础每小时金币
    private readonly double _battleSimulationSpeedMultiplier = 1.0; // 战斗模拟速度倍数

    public OfflineSettlementService(
        IDataStorageService dataStorageService,
        ILogger<OfflineSettlementService> logger)
    {
        _dataStorageService = dataStorageService;
        _logger = logger;
    }

    /// <summary>
    /// 安全地截取ID用于日志记录，防止日志注入攻击
    /// </summary>
    private static string SafeLogId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "[empty]";
        
        // 只保留字母数字和连字符，并截取前8位
        var sanitized = new string(id.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return sanitized.Substring(0, Math.Min(8, sanitized.Length)) + (sanitized.Length > 8 ? "..." : "");
    }

    /// <summary>
    /// 处理单个玩家的离线结算
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <returns>结算结果</returns>
    public async Task<ApiResponse<OfflineSettlementResultDto>> ProcessPlayerOfflineSettlementAsync(string playerId)
    {
        try
        {
            _logger.LogInformation("开始处理玩家 {PlayerId} 的离线结算", SafeLogId(playerId));

            // 1. 获取玩家数据
            var player = await _dataStorageService.GetPlayerAsync(playerId);
            if (player == null)
            {
                return new ApiResponse<OfflineSettlementResultDto>
                {
                    Success = false,
                    Message = "玩家不存在"
                };
            }

            // 2. 计算离线时间
            var offlineTime = DateTime.UtcNow - player.LastActiveAt;
            var settlementTime = offlineTime > _maxOfflineSettlementTime ? _maxOfflineSettlementTime : offlineTime;
            
            if (settlementTime.TotalMinutes < 1)
            {
                return new ApiResponse<OfflineSettlementResultDto>
                {
                    Success = true,
                    Message = "离线时间不足，无需结算",
                    Data = new OfflineSettlementResultDto
                    {
                        PlayerId = playerId,
                        OfflineTime = settlementTime,
                        TotalExperience = 0,
                        TotalGold = 0,
                        BattleResults = new List<OfflineBattleResultDto>(),
                        Rewards = new List<OfflineRewardDto>()
                    }
                };
            }

            // 3. 模拟离线期间的战斗和产出
            var settlementResult = await SimulateOfflineProgressAsync(player, settlementTime);

            // 4. 更新玩家数据
            await UpdatePlayerDataAsync(player, settlementResult);

            // 5. 记录结算历史
            await RecordSettlementHistoryAsync(settlementResult);

            _logger.LogInformation("玩家 {PlayerId} 离线结算完成，获得经验 {Experience}，金币 {Gold}", 
                SafeLogId(playerId), settlementResult.TotalExperience, settlementResult.TotalGold);

            return new ApiResponse<OfflineSettlementResultDto>
            {
                Success = true,
                Message = "离线结算成功",
                Data = settlementResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理玩家 {PlayerId} 离线结算时发生错误", SafeLogId(playerId));
            return new ApiResponse<OfflineSettlementResultDto>
            {
                Success = false,
                Message = $"离线结算失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 处理队伍的离线结算
    /// </summary>
    /// <param name="teamId">队伍ID</param>
    /// <returns>结算结果列表</returns>
    public async Task<ApiResponse<List<OfflineSettlementResultDto>>> ProcessTeamOfflineSettlementAsync(string teamId)
    {
        try
        {
            _logger.LogInformation("开始处理队伍 {TeamId} 的离线结算", SafeLogId(teamId));

            // 1. 获取队伍数据
            var team = await _dataStorageService.GetTeamAsync(teamId);
            if (team == null)
            {
                return new ApiResponse<List<OfflineSettlementResultDto>>
                {
                    Success = false,
                    Message = "队伍不存在"
                };
            }

            // 2. 获取队伍所有成员数据
            var memberTasks = team.MemberIds.Select(memberId => _dataStorageService.GetPlayerAsync(memberId));
            var members = await Task.WhenAll(memberTasks);
            var validMembers = members.Where(m => m != null).ToList();

            if (!validMembers.Any())
            {
                return new ApiResponse<List<OfflineSettlementResultDto>>
                {
                    Success = false,
                    Message = "队伍没有有效成员"
                };
            }

            // 3. 分析队伍成员的离线时间差异
            var teamSyncInfo = AnalyzeTeamOfflineSynchronization(validMembers!);

            // 4. 基于队伍协作模拟离线进度
            var settlementResults = await ProcessTeamCooperativeSettlement(team, validMembers!, teamSyncInfo);

            // 5. 应用队伍协作加成
            ApplyTeamBonuses(settlementResults, teamSyncInfo);

            // 6. 更新队伍整体进度和同步状态
            await UpdateTeamProgress(team, settlementResults, teamSyncInfo);

            _logger.LogInformation("队伍 {TeamId} 离线结算完成，处理了 {MemberCount} 个成员，队伍同步率 {SyncRate:P2}", 
                SafeLogId(teamId), settlementResults.Count, teamSyncInfo.SynchronizationRate);

            return new ApiResponse<List<OfflineSettlementResultDto>>
            {
                Success = true,
                Message = "队伍离线结算成功",
                Data = settlementResults
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理队伍 {TeamId} 离线结算时发生错误", SafeLogId(teamId));
            return new ApiResponse<List<OfflineSettlementResultDto>>
            {
                Success = false,
                Message = $"队伍离线结算失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 分析队伍离线同步情况
    /// </summary>
    private TeamOfflineSyncInfo AnalyzeTeamOfflineSynchronization(List<PlayerStorageDto> members)
    {
        var syncInfo = new TeamOfflineSyncInfo();
        var offlineTimes = new List<TimeSpan>();
        var now = DateTime.UtcNow;

        foreach (var member in members)
        {
            var offlineTime = now - member.LastActiveAt;
            offlineTimes.Add(offlineTime);
            syncInfo.MemberOfflineTimes[member.Id] = offlineTime;
        }

        // 计算同步统计
        syncInfo.AverageOfflineTime = TimeSpan.FromTicks((long)offlineTimes.Average(t => t.Ticks));
        syncInfo.MinOfflineTime = offlineTimes.Min();
        syncInfo.MaxOfflineTime = offlineTimes.Max();
        
        // 计算同步率：基于离线时间的标准差
        var totalTicks = offlineTimes.Sum(t => t.Ticks);
        var avgTicks = totalTicks / offlineTimes.Count;
        var variance = offlineTimes.Sum(t => Math.Pow(t.Ticks - avgTicks, 2)) / offlineTimes.Count;
        var standardDeviation = Math.Sqrt(variance);
        
        // 同步率计算：标准差越小，同步率越高
        var maxStdDev = TimeSpan.FromHours(12).Ticks; // 假设12小时差异为最大不同步
        syncInfo.SynchronizationRate = Math.Max(0, 1.0 - (standardDeviation / maxStdDev));

        // 确定队伍协作模式
        syncInfo.CooperationMode = syncInfo.SynchronizationRate switch
        {
            >= 0.8 => TeamCooperationMode.HighSync,    // 高度同步
            >= 0.5 => TeamCooperationMode.MediumSync,  // 中等同步
            >= 0.2 => TeamCooperationMode.LowSync,     // 低同步
            _ => TeamCooperationMode.Individual        // 个人模式
        };

        return syncInfo;
    }

    /// <summary>
    /// 处理队伍协作结算
    /// </summary>
    private async Task<List<OfflineSettlementResultDto>> ProcessTeamCooperativeSettlement(
        TeamStorageDto team, List<PlayerStorageDto> members, TeamOfflineSyncInfo syncInfo)
    {
        var settlementResults = new List<OfflineSettlementResultDto>();

        // 根据协作模式处理结算
        switch (syncInfo.CooperationMode)
        {
            case TeamCooperationMode.HighSync:
                // 高度同步：使用统一时间段进行协作战斗模拟
                settlementResults = await ProcessSynchronizedTeamBattles(members, syncInfo);
                break;
                
            case TeamCooperationMode.MediumSync:
                // 中等同步：部分协作，部分独立
                settlementResults = await ProcessPartiallyCooperativeBattles(members, syncInfo);
                break;
                
            case TeamCooperationMode.LowSync:
            case TeamCooperationMode.Individual:
            default:
                // 低同步或个人模式：独立处理，但应用少量队伍加成
                foreach (var member in members)
                {
                    var memberResult = await ProcessPlayerOfflineSettlementAsync(member.Id);
                    if (memberResult.Success && memberResult.Data != null)
                    {
                        settlementResults.Add(memberResult.Data);
                    }
                }
                break;
        }

        return settlementResults;
    }

    /// <summary>
    /// 处理高度同步的队伍战斗
    /// </summary>
    private async Task<List<OfflineSettlementResultDto>> ProcessSynchronizedTeamBattles(
        List<PlayerStorageDto> members, TeamOfflineSyncInfo syncInfo)
    {
        var results = new List<OfflineSettlementResultDto>();
        var cooperativeTime = syncInfo.MinOfflineTime; // 使用最短离线时间作为协作时间
        
        // 创建队伍战斗会话
        var teamSession = new TeamCombatSession
        {
            Members = members,
            CooperativeTime = cooperativeTime,
            TeamBonus = CalculateTeamBonus(members.Count, syncInfo.SynchronizationRate)
        };

        // 为每个成员进行协作战斗模拟
        foreach (var member in members)
        {
            var result = new OfflineSettlementResultDto
            {
                PlayerId = member.Id,
                OfflineTime = syncInfo.MemberOfflineTimes[member.Id]
            };

            // 协作战斗模拟
            await SimulateTeamCooperativeCombat(member, teamSession, result);
            
            // 处理剩余的个人时间（如果有）
            var remainingTime = syncInfo.MemberOfflineTimes[member.Id] - cooperativeTime;
            if (remainingTime.TotalMinutes > 1)
            {
                await SimulateIndividualProgressAsync(member, remainingTime, result);
            }

            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// 模拟队伍协作战斗
    /// </summary>
    private async Task SimulateTeamCooperativeCombat(PlayerStorageDto player, TeamCombatSession teamSession, OfflineSettlementResultDto result)
    {
        var totalHours = teamSession.CooperativeTime.TotalHours;
        var combatSession = new OfflineCombatSession
        {
            Player = player,
            TotalHours = totalHours,
            InitialDifficulty = 0.8 // 队伍协作降低初始难度
        };

        // 队伍协作战斗的特殊逻辑
        var teamMultiplier = teamSession.TeamBonus;
        await SimulateProgressiveCombat(combatSession, result);
        
        // 应用队伍加成
        result.TotalExperience = (int)(result.TotalExperience * teamMultiplier);
        result.TotalGold = (int)(result.TotalGold * teamMultiplier);

        // 添加队伍协作奖励
        result.Rewards.Add(new OfflineRewardDto
        {
            Type = "TeamCooperation",
            Description = $"队伍协作战斗 {combatSession.TotalBattles} 次",
            Experience = (int)(result.TotalExperience * (teamMultiplier - 1.0)),
            Gold = (int)(result.TotalGold * (teamMultiplier - 1.0)),
            AdditionalRewards = new Dictionary<string, object>
            {
                ["TeamBonus"] = teamMultiplier,
                ["TeamSize"] = teamSession.Members.Count,
                ["SyncRate"] = teamSession.TeamBonus
            }
        });
    }

    /// <summary>
    /// 处理部分协作战斗
    /// </summary>
    private async Task<List<OfflineSettlementResultDto>> ProcessPartiallyCooperativeBattles(
        List<PlayerStorageDto> members, TeamOfflineSyncInfo syncInfo)
    {
        var results = new List<OfflineSettlementResultDto>();
        var cooperativeTime = TimeSpan.FromTicks((long)(syncInfo.AverageOfflineTime.Ticks * 0.6)); // 60%时间协作

        foreach (var member in members)
        {
            var result = new OfflineSettlementResultDto
            {
                PlayerId = member.Id,
                OfflineTime = syncInfo.MemberOfflineTimes[member.Id]
            };

            // 协作部分
            if (cooperativeTime.TotalMinutes > 1)
            {
                var teamSession = new TeamCombatSession
                {
                    Members = members,
                    CooperativeTime = cooperativeTime,
                    TeamBonus = CalculateTeamBonus(members.Count, syncInfo.SynchronizationRate) * 0.7 // 部分协作减少加成
                };
                
                await SimulateTeamCooperativeCombat(member, teamSession, result);
            }

            // 个人部分
            var individualTime = syncInfo.MemberOfflineTimes[member.Id] - cooperativeTime;
            if (individualTime.TotalMinutes > 1)
            {
                await SimulateIndividualProgressAsync(member, individualTime, result);
            }

            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// 模拟个人进度（用于队伍非协作时间）
    /// </summary>
    private async Task SimulateIndividualProgressAsync(PlayerStorageDto player, TimeSpan timeSpan, OfflineSettlementResultDto result)
    {
        var individualResult = new OfflineSettlementResultDto
        {
            PlayerId = player.Id,
            OfflineTime = timeSpan
        };

        await SimulateOfflineProgressAsync(player, timeSpan);
        
        // 合并结果
        result.TotalExperience += individualResult.TotalExperience;
        result.TotalGold += individualResult.TotalGold;
        result.BattleResults.AddRange(individualResult.BattleResults);
        result.Rewards.AddRange(individualResult.Rewards);
    }

    /// <summary>
    /// 计算队伍加成
    /// </summary>
    private double CalculateTeamBonus(int memberCount, double syncRate)
    {
        var baseBonus = 1.0 + (memberCount - 1) * 0.15; // 每个额外成员15%加成
        var syncBonus = syncRate * 0.3; // 同步率提供最多30%额外加成
        return baseBonus + syncBonus;
    }

    /// <summary>
    /// 应用队伍奖励加成
    /// </summary>
    private void ApplyTeamBonuses(List<OfflineSettlementResultDto> results, TeamOfflineSyncInfo syncInfo)
    {
        var teamLevelBonus = CalculateTeamLevelBonus(results);
        var loyaltyBonus = CalculateTeamLoyaltyBonus(syncInfo);

        foreach (var result in results)
        {
            var additionalExp = (int)(result.TotalExperience * teamLevelBonus);
            var additionalGold = (int)(result.TotalGold * loyaltyBonus);

            result.TotalExperience += additionalExp;
            result.TotalGold += additionalGold;

            if (additionalExp > 0 || additionalGold > 0)
            {
                result.Rewards.Add(new OfflineRewardDto
                {
                    Type = "TeamBonus",
                    Description = "队伍忠诚度和等级加成",
                    Experience = additionalExp,
                    Gold = additionalGold
                });
            }
        }
    }

    /// <summary>
    /// 计算队伍等级加成
    /// </summary>
    private double CalculateTeamLevelBonus(List<OfflineSettlementResultDto> results)
    {
        if (!results.Any()) return 0;
        
        var avgExperience = results.Average(r => r.TotalExperience);
        return Math.Min(0.2, avgExperience / 10000.0); // 基于平均经验获得，最多20%加成
    }

    /// <summary>
    /// 计算队伍忠诚度加成
    /// </summary>
    private double CalculateTeamLoyaltyBonus(TeamOfflineSyncInfo syncInfo)
    {
        return syncInfo.SynchronizationRate * 0.15; // 同步率越高，忠诚度加成越高，最多15%
    }

    /// <summary>
    /// 更新队伍进度
    /// </summary>
    private async Task UpdateTeamProgress(TeamStorageDto team, List<OfflineSettlementResultDto> results, TeamOfflineSyncInfo syncInfo)
    {
        // 更新队伍最后战斗时间
        team.LastBattleAt = DateTime.UtcNow;
        team.UpdatedAt = DateTime.UtcNow;

        // 记录队伍协作数据
        var teamProgressData = new Dictionary<string, object>
        {
            ["TotalExperience"] = results.Sum(r => r.TotalExperience),
            ["TotalGold"] = results.Sum(r => r.TotalGold),
            ["TotalBattles"] = results.Sum(r => r.BattleResults.Count),
            ["SynchronizationRate"] = syncInfo.SynchronizationRate,
            ["CooperationMode"] = syncInfo.CooperationMode.ToString(),
            ["SettlementTime"] = DateTime.UtcNow
        };

        // 保存队伍进度记录
        var teamProgressRecord = new OfflineDataStorageDto
        {
            Id = Guid.NewGuid().ToString(),
            PlayerId = team.Id,
            DataType = "TeamProgress",
            Data = teamProgressData,
            IsSynced = true,
            Version = 1
        };

        await _dataStorageService.SaveOfflineDataAsync(teamProgressRecord);
        await _dataStorageService.SaveTeamAsync(team);
    }

    /// <summary>
    /// 队伍离线同步信息
    /// </summary>
    private class TeamOfflineSyncInfo
    {
        public Dictionary<string, TimeSpan> MemberOfflineTimes { get; set; } = new();
        public TimeSpan AverageOfflineTime { get; set; }
        public TimeSpan MinOfflineTime { get; set; }
        public TimeSpan MaxOfflineTime { get; set; }
        public double SynchronizationRate { get; set; }
        public TeamCooperationMode CooperationMode { get; set; }
    }

    /// <summary>
    /// 队伍协作战斗会话
    /// </summary>
    private class TeamCombatSession
    {
        public List<PlayerStorageDto> Members { get; set; } = new();
        public TimeSpan CooperativeTime { get; set; }
        public double TeamBonus { get; set; } = 1.0;
    }

    /// <summary>
    /// 队伍协作模式
    /// </summary>
    private enum TeamCooperationMode
    {
        Individual,   // 个人模式
        LowSync,      // 低同步
        MediumSync,   // 中等同步  
        HighSync      // 高度同步
    }

    /// <summary>
    /// 批量处理多个玩家的离线结算
    /// </summary>
    /// <param name="playerIds">玩家ID列表</param>
    /// <returns>批量结算结果</returns>
    public async Task<BatchOperationResponseDto<OfflineSettlementResultDto>> ProcessBatchOfflineSettlementAsync(List<string> playerIds)
    {
        var result = new BatchOperationResponseDto<OfflineSettlementResultDto>();
        result.TotalProcessed = playerIds.Count;

        foreach (var playerId in playerIds)
        {
            try
            {
                var settlementResult = await ProcessPlayerOfflineSettlementAsync(playerId);
                if (settlementResult.Success && settlementResult.Data != null)
                {
                    result.SuccessfulItems.Add(settlementResult.Data);
                    result.SuccessCount++;
                }
                else
                {
                    result.Errors.Add($"玩家 {SafeLogId(playerId)}: {settlementResult.Message}");
                    result.ErrorCount++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"玩家 {SafeLogId(playerId)}: {ex.Message}");
                result.ErrorCount++;
            }
        }

        _logger.LogInformation("批量离线结算完成，成功 {SuccessCount}，失败 {ErrorCount}", 
            result.SuccessCount, result.ErrorCount);

        return result;
    }

    /// <summary>
    /// 模拟离线期间的进度
    /// </summary>
    private async Task<OfflineSettlementResultDto> SimulateOfflineProgressAsync(PlayerStorageDto player, TimeSpan offlineTime)
    {
        var result = new OfflineSettlementResultDto
        {
            PlayerId = player.Id,
            OfflineTime = offlineTime,
            BattleResults = new List<OfflineBattleResultDto>(),
            Rewards = new List<OfflineRewardDto>()
        };

        // 根据玩家当前行动状态模拟进度
        switch (player.CurrentAction.ToLower())
        {
            case "combat":
            case "battle":
                await SimulateOfflineCombatAsync(player, offlineTime, result);
                break;
            case "gathering":
                await SimulateOfflineGatheringAsync(player, offlineTime, result);
                break;
            case "crafting":
                await SimulateOfflineCraftingAsync(player, offlineTime, result);
                break;
            default:
                // 默认情况：基础经验和金币增长
                SimulateIdleProgress(player, offlineTime, result);
                break;
        }

        return result;
    }

    /// <summary>
    /// 模拟离线战斗
    /// </summary>
    private async Task SimulateOfflineCombatAsync(PlayerStorageDto player, TimeSpan offlineTime, OfflineSettlementResultDto result)
    {
        var totalHours = offlineTime.TotalHours;
        
        // 增强的战斗模拟：考虑渐进式难度提升和疲劳系统
        var combatSession = new OfflineCombatSession
        {
            Player = player,
            TotalHours = totalHours,
            InitialDifficulty = 1.0
        };

        await SimulateProgressiveCombat(combatSession, result);

        // 添加战斗奖励
        result.Rewards.Add(new OfflineRewardDto
        {
            Type = "Combat",
            Description = $"离线战斗 {combatSession.TotalBattles} 次，胜利 {combatSession.VictoryCount} 次",
            Experience = result.TotalExperience,
            Gold = result.TotalGold,
            AdditionalRewards = new Dictionary<string, object>
            {
                ["MaxWaveReached"] = combatSession.MaxWaveReached,
                ["AverageWinRate"] = combatSession.AverageWinRate,
                ["CombatRating"] = combatSession.CombatRating
            }
        });

        _logger.LogDebug("玩家 {PlayerId} 离线战斗模拟完成：战斗 {BattleCount} 次，胜率 {WinRate:P2}，最高波次 {MaxWave}", 
            SafeLogId(player.Id), combatSession.TotalBattles, combatSession.AverageWinRate, combatSession.MaxWaveReached);
    }

    /// <summary>
    /// 模拟渐进式战斗（考虑疲劳、难度递增等因素）
    /// </summary>
    private async Task SimulateProgressiveCombat(OfflineCombatSession session, OfflineSettlementResultDto result)
    {
        var currentHour = 0.0;
        var currentWave = 1;
        var fatigueLevel = 0.0;
        var consecutiveVictories = 0;
        var consecutiveDefeats = 0;

        while (currentHour < session.TotalHours)
        {
            // 每小时可进行的战斗次数（受疲劳影响）
            var battlesPerHour = _battleSimulationSpeedMultiplier * (1.0 - fatigueLevel * 0.3);
            var battleCount = Math.Max(1, (int)battlesPerHour);

            for (int i = 0; i < battleCount && currentHour < session.TotalHours; i++)
            {
                // 计算当前战斗的难度系数
                var difficulty = session.InitialDifficulty + (currentWave - 1) * 0.1 + fatigueLevel * 0.2;
                
                // 模拟单次战斗
                var battleResult = SimulateEnhancedBattle(session.Player, difficulty, currentWave, fatigueLevel);
                result.BattleResults.Add(battleResult);
                session.TotalBattles++;

                // 更新统计数据
                result.TotalExperience += battleResult.ExperienceGained;
                result.TotalGold += battleResult.GoldGained;

                if (battleResult.IsVictory)
                {
                    session.VictoryCount++;
                    consecutiveVictories++;
                    consecutiveDefeats = 0;
                    
                    // 连胜奖励
                    if (consecutiveVictories > 0 && consecutiveVictories % 5 == 0)
                    {
                        var bonusExp = (int)(battleResult.ExperienceGained * 0.2);
                        result.TotalExperience += bonusExp;
                        battleResult.AdditionalData["ConsecutiveWinBonus"] = bonusExp;
                    }

                    // 每3次胜利进入下一波
                    if (consecutiveVictories % 3 == 0)
                    {
                        currentWave++;
                        session.MaxWaveReached = Math.Max(session.MaxWaveReached, currentWave);
                    }

                    // 胜利后稍微恢复体力
                    fatigueLevel = Math.Max(0, fatigueLevel - 0.02);
                }
                else
                {
                    consecutiveDefeats++;
                    consecutiveVictories = 0;
                    
                    // 连败后难度稍微降低
                    if (consecutiveDefeats >= 3)
                    {
                        currentWave = Math.Max(1, currentWave - 1);
                        consecutiveDefeats = 0;
                    }

                    // 失败后增加疲劳
                    fatigueLevel = Math.Min(0.8, fatigueLevel + 0.05);
                }

                // 时间推进
                currentHour += 1.0 / battlesPerHour;
            }

            // 每小时增加少量疲劳
            fatigueLevel = Math.Min(0.9, fatigueLevel + 0.01);
            
            // 模拟休息恢复
            if (fatigueLevel > 0.6)
            {
                currentHour += 0.5; // 强制休息30分钟
                fatigueLevel = Math.Max(0, fatigueLevel - 0.3);
            }

            await Task.Delay(1); // 避免长时间同步操作阻塞
        }

        // 计算平均胜率和战斗评级
        session.AverageWinRate = session.TotalBattles > 0 ? (double)session.VictoryCount / session.TotalBattles : 0;
        session.CombatRating = CalculateCombatRating(session.AverageWinRate, session.MaxWaveReached, session.TotalBattles);
    }

    /// <summary>
    /// 计算战斗评级
    /// </summary>
    private string CalculateCombatRating(double winRate, int maxWave, int totalBattles)
    {
        var score = winRate * 100 + maxWave * 10 + Math.Min(totalBattles, 100) * 0.5;
        
        return score switch
        {
            >= 200 => "Legendary",
            >= 150 => "Epic",
            >= 100 => "Excellent",
            >= 80 => "Good",
            >= 60 => "Average",
            _ => "Beginner"
        };
    }

    /// <summary>
    /// 离线战斗会话数据
    /// </summary>
    private class OfflineCombatSession
    {
        public PlayerStorageDto Player { get; set; } = null!;
        public double TotalHours { get; set; }
        public double InitialDifficulty { get; set; }
        public int TotalBattles { get; set; }
        public int VictoryCount { get; set; }
        public int MaxWaveReached { get; set; } = 1;
        public double AverageWinRate { get; set; }
        public string CombatRating { get; set; } = "Beginner";
    }

    /// <summary>
    /// 模拟离线采集
    /// </summary>
    private async Task SimulateOfflineGatheringAsync(PlayerStorageDto player, TimeSpan offlineTime, OfflineSettlementResultDto result)
    {
        var baseGatheringPerHour = 5;
        var gatheringCount = (int)(offlineTime.TotalHours * baseGatheringPerHour);
        
        result.TotalExperience = (int)(gatheringCount * 20); // 每次采集20经验
        result.TotalGold = gatheringCount * 5; // 每次采集5金币

        result.Rewards.Add(new OfflineRewardDto
        {
            Type = "Gathering",
            Description = $"离线采集 {gatheringCount} 次",
            Experience = result.TotalExperience,
            Gold = result.TotalGold
        });
    }

    /// <summary>
    /// 模拟离线制作
    /// </summary>
    private async Task SimulateOfflineCraftingAsync(PlayerStorageDto player, TimeSpan offlineTime, OfflineSettlementResultDto result)
    {
        var baseCraftingPerHour = 3;
        var craftingCount = (int)(offlineTime.TotalHours * baseCraftingPerHour);
        
        result.TotalExperience = (int)(craftingCount * 50); // 每次制作50经验
        result.TotalGold = craftingCount * 15; // 每次制作15金币

        result.Rewards.Add(new OfflineRewardDto
        {
            Type = "Crafting",
            Description = $"离线制作 {craftingCount} 次",
            Experience = result.TotalExperience,
            Gold = result.TotalGold
        });
    }

    /// <summary>
    /// 模拟空闲状态进度
    /// </summary>
    private void SimulateIdleProgress(PlayerStorageDto player, TimeSpan offlineTime, OfflineSettlementResultDto result)
    {
        var hours = offlineTime.TotalHours;
        result.TotalExperience = (int)(hours * _baseExperiencePerHour * 0.5); // 空闲状态经验减半
        result.TotalGold = (int)(hours * _baseGoldPerHour);

        result.Rewards.Add(new OfflineRewardDto
        {
            Type = "Idle",
            Description = $"离线休息 {hours:F1} 小时",
            Experience = result.TotalExperience,
            Gold = result.TotalGold
        });
    }

    /// <summary>
    /// 计算战斗效率
    /// </summary>
    private double CalculateCombatEfficiency(PlayerStorageDto player)
    {
        var baseEfficiency = 1.0;
        var levelBonus = player.Level * 0.1;
        var professionBonus = player.SelectedBattleProfession.ToLower() switch
        {
            "warrior" => 1.2,
            "mage" => 1.1,
            "archer" => 1.15,
            _ => 1.0
        };

        return baseEfficiency + levelBonus * professionBonus;
    }

    /// <summary>
    /// 模拟增强版单次战斗（考虑波次、疲劳等因素）
    /// </summary>
    private OfflineBattleResultDto SimulateEnhancedBattle(PlayerStorageDto player, double difficulty, int wave, double fatigueLevel)
    {
        var random = new Random();
        var efficiency = CalculateCombatEfficiency(player);
        
        // 计算胜率：基础胜率受效率、难度、疲劳影响
        var baseWinRate = 0.7;
        var adjustedWinRate = baseWinRate * efficiency * (1.0 - difficulty * 0.1) * (1.0 - fatigueLevel * 0.2);
        adjustedWinRate = Math.Max(0.1, Math.Min(0.95, adjustedWinRate)); // 限制在10%-95%之间
        
        var isVictory = random.NextDouble() < adjustedWinRate;

        // 基础奖励计算
        var baseExp = _baseExperiencePerHour * efficiency * 0.5;
        var baseGold = _baseGoldPerHour * efficiency * 0.5;
        
        // 波次奖励加成
        var waveMultiplier = 1.0 + (wave - 1) * 0.15;
        
        var experienceGained = isVictory ? 
            (int)(baseExp * waveMultiplier * (0.8 + random.NextDouble() * 0.4)) : 
            (int)(baseExp * 0.3); // 失败仍有少量经验

        var goldGained = isVictory ? 
            (int)(baseGold * waveMultiplier * (0.8 + random.NextDouble() * 0.4)) : 
            (int)(baseGold * 0.15); // 失败仍有少量金币

        // 战斗时长：高难度战斗耗时更长
        var baseDuration = 8 + wave * 2; // 基础8分钟 + 波次加成
        var durationMinutes = (int)(baseDuration * (1.0 + difficulty * 0.3) * (0.8 + random.NextDouble() * 0.4));

        return new OfflineBattleResultDto
        {
            BattleId = Guid.NewGuid().ToString(),
            IsVictory = isVictory,
            ExperienceGained = experienceGained,
            GoldGained = goldGained,
            Duration = TimeSpan.FromMinutes(durationMinutes),
            EnemyType = GetEnemyTypeByWave(wave),
            AdditionalData = new Dictionary<string, object>
            {
                ["Wave"] = wave,
                ["Difficulty"] = difficulty,
                ["WinRate"] = adjustedWinRate,
                ["FatigueLevel"] = fatigueLevel,
                ["WaveMultiplier"] = waveMultiplier
            }
        };
    }

    /// <summary>
    /// 根据波次获取敌人类型
    /// </summary>
    private string GetEnemyTypeByWave(int wave)
    {
        return wave switch
        {
            <= 3 => "Goblin",
            <= 7 => "Orc",
            <= 12 => "Troll",
            <= 18 => "Drake",
            <= 25 => "Dragon",
            _ => "Legendary"
        };
    }

    /// <summary>
    /// 模拟单次战斗（保留向后兼容性）
    /// </summary>
    private OfflineBattleResultDto SimulateSingleBattle(PlayerStorageDto player, double efficiency)
    {
        return SimulateEnhancedBattle(player, 1.0, 1, 0.0);
    }

    /// <summary>
    /// 更新玩家数据
    /// </summary>
    private async Task UpdatePlayerDataAsync(PlayerStorageDto player, OfflineSettlementResultDto result)
    {
        // 更新玩家经验和等级
        player.Experience += result.TotalExperience;
        player.Gold += result.TotalGold;
        
        // 计算等级提升
        var newLevel = CalculateLevel(player.Experience);
        if (newLevel > player.Level)
        {
            player.Level = newLevel;
            // 等级提升时增加生命值
            player.MaxHealth += (newLevel - player.Level) * 10;
            player.Health = player.MaxHealth; // 满血
        }

        player.LastActiveAt = DateTime.UtcNow;
        player.UpdatedAt = DateTime.UtcNow;

        // 保存玩家数据
        await _dataStorageService.SavePlayerAsync(player);
    }

    /// <summary>
    /// 计算等级
    /// </summary>
    private int CalculateLevel(int experience)
    {
        // 简单的等级计算公式：每100经验升1级
        return Math.Max(1, experience / 100 + 1);
    }

    /// <summary>
    /// 记录结算历史
    /// </summary>
    private async Task RecordSettlementHistoryAsync(OfflineSettlementResultDto result)
    {
        var offlineData = new OfflineDataStorageDto
        {
            Id = Guid.NewGuid().ToString(),
            PlayerId = result.PlayerId,
            DataType = "OfflineSettlement",
            Data = new Dictionary<string, object>
            {
                ["OfflineTime"] = result.OfflineTime.ToString(),
                ["TotalExperience"] = result.TotalExperience,
                ["TotalGold"] = result.TotalGold,
                ["BattleCount"] = result.BattleResults.Count,
                ["RewardCount"] = result.Rewards.Count,
                ["SettlementTime"] = DateTime.UtcNow
            },
            IsSynced = true,
            Version = 1
        };

        await _dataStorageService.SaveOfflineDataAsync(offlineData);
    }

    /// <summary>
    /// 计算队伍整体进度 - 为后续功能扩展
    /// </summary>
    private async Task CalculateTeamOverallProgressAsync(TeamStorageDto team, List<OfflineSettlementResultDto> memberResults)
    {
        // 这里可以实现队伍整体战斗进度推算逻辑
        // 例如：基于队伍成员的平均等级、总战斗次数等计算队伍整体实力变化
        
        var totalExperience = memberResults.Sum(r => r.TotalExperience);
        var totalBattles = memberResults.Sum(r => r.BattleResults.Count);
        
        _logger.LogInformation("队伍 {TeamId} 整体进度：总经验 {TotalExp}，总战斗 {TotalBattles}", 
            SafeLogId(team.Id), totalExperience, totalBattles);
        
        // 可以在这里更新队伍数据，记录整体进度信息
        // 为多人组队功能预留扩展点
    }
}