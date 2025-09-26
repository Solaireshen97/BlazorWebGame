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

            // 3. 为每个成员处理离线结算
            var settlementResults = new List<OfflineSettlementResultDto>();
            foreach (var member in validMembers)
            {
                var memberResult = await ProcessPlayerOfflineSettlementAsync(member!.Id);
                if (memberResult.Success && memberResult.Data != null)
                {
                    settlementResults.Add(memberResult.Data);
                }
            }

            // 4. 计算队伍整体进度
            await CalculateTeamOverallProgressAsync(team, settlementResults);

            _logger.LogInformation("队伍 {TeamId} 离线结算完成，处理了 {MemberCount} 个成员", 
                SafeLogId(teamId), settlementResults.Count);

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
        var battleCount = (int)(totalHours * _battleSimulationSpeedMultiplier);

        // 基于玩家等级和职业计算战斗效率
        var efficiency = CalculateCombatEfficiency(player);
        
        for (int i = 0; i < battleCount; i++)
        {
            var battleResult = SimulateSingleBattle(player, efficiency);
            result.BattleResults.Add(battleResult);
            
            result.TotalExperience += battleResult.ExperienceGained;
            result.TotalGold += battleResult.GoldGained;
        }

        // 添加战斗奖励
        result.Rewards.Add(new OfflineRewardDto
        {
            Type = "Combat",
            Description = $"离线战斗 {battleCount} 次",
            Experience = result.TotalExperience,
            Gold = result.TotalGold
        });

        _logger.LogDebug("玩家 {PlayerId} 离线战斗模拟完成，战斗 {BattleCount} 次", 
            SafeLogId(player.Id), battleCount);
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
    /// 模拟单次战斗
    /// </summary>
    private OfflineBattleResultDto SimulateSingleBattle(PlayerStorageDto player, double efficiency)
    {
        var random = new Random();
        var isVictory = random.NextDouble() < (0.7 * efficiency); // 基础70%胜率，受效率影响

        var experienceGained = isVictory ? 
            (int)(_baseExperiencePerHour * efficiency * random.NextDouble() * 0.5) : 
            (int)(_baseExperiencePerHour * 0.2);

        var goldGained = isVictory ? 
            (int)(_baseGoldPerHour * efficiency * random.NextDouble() * 0.5) : 
            (int)(_baseGoldPerHour * 0.1);

        return new OfflineBattleResultDto
        {
            BattleId = Guid.NewGuid().ToString(),
            IsVictory = isVictory,
            ExperienceGained = experienceGained,
            GoldGained = goldGained,
            Duration = TimeSpan.FromMinutes(random.Next(5, 15))
        };
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