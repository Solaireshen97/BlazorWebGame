using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorWebGame.Rebuild.Services.Activities;
using BlazorWebGame.Rebuild.Services.System;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Events;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Rebuild.Services.Battle;

/// <summary>
/// 战斗事件处理器 - 基于事件队列的离线战斗推进
/// 实现设计文档中的事件队列驱动战斗系统
/// </summary>
public class CombatEventProcessor : IOfflineActivityProcessor
{
    private readonly UnifiedEventService _eventService;
    private readonly ILogger _logger;
    
    // 战斗配置
    private readonly double _baseAttackCooldown = 3.0; // 基础攻击冷却时间（秒）
    private readonly double _combatEfficiencyDecay = 0.95; // 战斗效率衰减系数
    private readonly int _maxWaveProgression = 50; // 最大波次进展

    public CombatEventProcessor(UnifiedEventService eventService, ILogger logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    public string GetActivityName() => "Combat";

    public TimeSpan GetBaseCycleDuration(PlayerStorageDto player)
    {
        // 基础战斗周期：一次完整的攻击循环
        var attackSpeed = CalculateAttackSpeed(player);
        return TimeSpan.FromSeconds(_baseAttackCooldown / attackSpeed);
    }

    /// <summary>
    /// 批量处理战斗段落 - 使用事件队列快速推进
    /// </summary>
    public async Task<OfflineActivityResult> ProcessBulkSegmentsAsync(
        PlayerStorageDto player, 
        TimeSpan segmentDuration, 
        int segmentCount)
    {
        var result = new OfflineActivityResult
        {
            Success = true,
            PlayerId = player.Id,
            ActivityType = "Combat"
        };

        try
        {
            // 创建或恢复战斗实例
            var combatInstance = await GetOrCreateCombatInstance(player);
            
            var totalProcessedTime = TimeSpan.FromSeconds(segmentDuration.TotalSeconds * segmentCount);
            
            _logger.LogInformation("开始批量处理玩家 {PlayerId} 的战斗段落：{SegmentCount} 段，总时长 {TotalTime}",
                SafeLogId(player.Id), segmentCount, totalProcessedTime);

            // 使用时间跳跃推进战斗
            var fastForwardResult = await FastForwardCombatInstance(combatInstance, totalProcessedTime);
            
            result.TotalExperience = fastForwardResult.TotalExperience;
            result.TotalGold = fastForwardResult.TotalGold;
            result.Rewards.AddRange(fastForwardResult.BattleRewards);
            result.AdditionalData["BattleCount"] = fastForwardResult.BattleCount;
            result.AdditionalData["VictoryCount"] = fastForwardResult.VictoryCount;
            result.AdditionalData["MaxWaveReached"] = fastForwardResult.MaxWaveReached;
            result.AdditionalData["FinalDifficulty"] = fastForwardResult.FinalDifficulty;

            // 保存战斗实例状态
            await SaveCombatInstance(combatInstance);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量处理战斗段落时发生错误：玩家 {PlayerId}", SafeLogId(player.Id));
            result.Success = false;
            result.Message = $"战斗处理失败: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 处理余数时间 - 精确的事件队列处理
    /// </summary>
    public async Task<OfflineActivityResult> ProcessRemainingTimeAsync(
        PlayerStorageDto player, 
        TimeSpan remainingTime)
    {
        var result = new OfflineActivityResult
        {
            Success = true,
            PlayerId = player.Id,
            ActivityType = "Combat"
        };

        try
        {
            var combatInstance = await GetOrCreateCombatInstance(player);
            
            _logger.LogDebug("处理玩家 {PlayerId} 的余数战斗时间：{RemainingTime}",
                SafeLogId(player.Id), remainingTime);

            // 精确处理剩余时间
            var preciseResult = await ProcessPreciseCombatTime(combatInstance, remainingTime);
            
            result.TotalExperience = preciseResult.ExperienceGained;
            result.TotalGold = preciseResult.GoldGained;
            
            if (preciseResult.CompletedBattles.Any())
            {
                result.Rewards.Add(new OfflineRewardDto
                {
                    Type = "CombatRemainder",
                    Description = $"余数时间战斗奖励（{preciseResult.CompletedBattles.Count} 场战斗）",
                    Experience = preciseResult.ExperienceGained,
                    Gold = preciseResult.GoldGained
                });
            }

            result.AdditionalData["RemainingBattles"] = preciseResult.CompletedBattles.Count;
            result.AdditionalData["PartialProgress"] = preciseResult.PartialProgress;

            await SaveCombatInstance(combatInstance);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理余数战斗时间时发生错误：玩家 {PlayerId}", SafeLogId(player.Id));
            result.Success = false;
            result.Message = $"余数战斗处理失败: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 获取或创建战斗实例
    /// </summary>
    private async Task<OfflineCombatInstance> GetOrCreateCombatInstance(PlayerStorageDto player)
    {
        // 尝试从存储中加载现有战斗实例
        // 这里可以集成现有的战斗状态管理系统
        
        return new OfflineCombatInstance
        {
            PlayerId = player.Id,
            GameClock = 0.0,
            CurrentWave = 1,
            Difficulty = 1.0,
            PlayerHealth = player.Health,
            PlayerMaxHealth = player.MaxHealth,
            EnemyHealth = CalculateEnemyHealth(1),
            EnemyMaxHealth = CalculateEnemyHealth(1),
            EventQueue = new List<CombatEvent>(),
            LastUpdateTime = DateTime.UtcNow,
            AttackSpeed = CalculateAttackSpeed(player),
            CombatEfficiency = CalculateCombatEfficiency(player)
        };
    }

    /// <summary>
    /// 快速推进战斗实例 - 时间跳跃机制
    /// </summary>
    private async Task<CombatFastForwardResult> FastForwardCombatInstance(
        OfflineCombatInstance instance, 
        TimeSpan timeToAdvance)
    {
        var result = new CombatFastForwardResult();
        var targetTime = instance.GameClock + timeToAdvance.TotalSeconds;
        
        _logger.LogDebug("快速推进战斗：从 {StartTime} 到 {TargetTime}，推进 {Duration} 秒",
            instance.GameClock, targetTime, timeToAdvance.TotalSeconds);

        // 初始化事件队列（如果为空）
        if (!instance.EventQueue.Any())
        {
            ScheduleInitialCombatEvents(instance);
        }

        // 事件队列驱动的时间推进
        while (instance.GameClock < targetTime && instance.EventQueue.Any())
        {
            var nextEvent = instance.EventQueue.OrderBy(e => e.TriggerTime).First();
            
            if (nextEvent.TriggerTime > targetTime)
            {
                // 没有更多事件需要在目标时间前执行
                break;
            }

            // 推进到事件触发时间
            instance.GameClock = nextEvent.TriggerTime;
            instance.EventQueue.Remove(nextEvent);

            // 执行事件
            await ExecuteCombatEvent(instance, nextEvent, result);

            // 检查战斗是否结束
            if (instance.PlayerHealth <= 0 || instance.EnemyHealth <= 0)
            {
                await HandleBattleEnd(instance, result);
                
                // 如果玩家死亡，停止处理
                if (instance.PlayerHealth <= 0)
                {
                    break;
                }
            }

            // 避免无限循环
            if (result.BattleCount > 1000)
            {
                _logger.LogWarning("战斗次数超过限制，停止处理：玩家 {PlayerId}", SafeLogId(instance.PlayerId));
                break;
            }
        }

        // 推进到目标时间
        instance.GameClock = Math.Min(targetTime, instance.GameClock);

        _logger.LogInformation("战斗快速推进完成：玩家 {PlayerId}，战斗 {BattleCount} 次，胜利 {VictoryCount} 次，最高波次 {MaxWave}",
            SafeLogId(instance.PlayerId), result.BattleCount, result.VictoryCount, result.MaxWaveReached);

        return result;
    }

    /// <summary>
    /// 精确处理战斗时间 - 用于余数时间
    /// </summary>
    private async Task<PreciseCombatResult> ProcessPreciseCombatTime(
        OfflineCombatInstance instance, 
        TimeSpan preciseTime)
    {
        var result = new PreciseCombatResult();
        var targetTime = instance.GameClock + preciseTime.TotalSeconds;

        // 处理精确时间内的事件
        var eventsToProcess = instance.EventQueue
            .Where(e => e.TriggerTime <= targetTime)
            .OrderBy(e => e.TriggerTime)
            .ToList();

        foreach (var combatEvent in eventsToProcess)
        {
            instance.GameClock = combatEvent.TriggerTime;
            instance.EventQueue.Remove(combatEvent);

            // 执行事件并记录结果
            var battleResult = await ExecuteCombatEventPrecise(instance, combatEvent);
            if (battleResult != null)
            {
                result.CompletedBattles.Add(battleResult);
                result.ExperienceGained += battleResult.ExperienceGained;
                result.GoldGained += battleResult.GoldGained;
            }
        }

        // 计算部分进度
        var nextEvent = instance.EventQueue.OrderBy(e => e.TriggerTime).FirstOrDefault();
        if (nextEvent != null)
        {
            var totalCycleTime = nextEvent.TriggerTime - instance.GameClock;
            var elapsedTime = targetTime - instance.GameClock;
            result.PartialProgress = Math.Min(1.0, elapsedTime / totalCycleTime);
        }

        instance.GameClock = targetTime;
        return result;
    }

    /// <summary>
    /// 初始化战斗事件队列
    /// </summary>
    private void ScheduleInitialCombatEvents(OfflineCombatInstance instance)
    {
        // 调度下一次攻击事件
        var nextAttackTime = instance.GameClock + _baseAttackCooldown / instance.AttackSpeed;
        instance.EventQueue.Add(new CombatEvent
        {
            Type = CombatEventType.PlayerAttack,
            TriggerTime = nextAttackTime,
            Data = new Dictionary<string, object>
            {
                ["AttackPower"] = CalculateAttackPower(instance),
                ["WaveNumber"] = instance.CurrentWave
            }
        });

        // 调度敌人攻击事件
        var enemyAttackTime = instance.GameClock + _baseAttackCooldown * 1.2; // 敌人稍慢
        instance.EventQueue.Add(new CombatEvent
        {
            Type = CombatEventType.EnemyAttack,
            TriggerTime = enemyAttackTime,
            Data = new Dictionary<string, object>
            {
                ["AttackPower"] = CalculateEnemyAttackPower(instance.CurrentWave),
                ["WaveNumber"] = instance.CurrentWave
            }
        });
    }

    /// <summary>
    /// 执行战斗事件
    /// </summary>
    private async Task ExecuteCombatEvent(
        OfflineCombatInstance instance, 
        CombatEvent combatEvent, 
        CombatFastForwardResult result)
    {
        switch (combatEvent.Type)
        {
            case CombatEventType.PlayerAttack:
                await HandlePlayerAttack(instance, combatEvent, result);
                break;
            case CombatEventType.EnemyAttack:
                await HandleEnemyAttack(instance, combatEvent);
                break;
            case CombatEventType.SkillCast:
                await HandleSkillCast(instance, combatEvent, result);
                break;
            case CombatEventType.BuffExpire:
                await HandleBuffExpire(instance, combatEvent);
                break;
        }
    }

    /// <summary>
    /// 处理玩家攻击事件
    /// </summary>
    private async Task HandlePlayerAttack(
        OfflineCombatInstance instance, 
        CombatEvent combatEvent, 
        CombatFastForwardResult result)
    {
        var attackPower = (int)combatEvent.Data.GetValueOrDefault("AttackPower", 10);
        var damage = CalculateDamage(attackPower, instance.Difficulty);
        
        instance.EnemyHealth -= damage;
        
        // 调度下一次攻击
        var nextAttackTime = instance.GameClock + _baseAttackCooldown / instance.AttackSpeed;
        instance.EventQueue.Add(new CombatEvent
        {
            Type = CombatEventType.PlayerAttack,
            TriggerTime = nextAttackTime,
            Data = combatEvent.Data
        });

        _logger.LogTrace("玩家攻击：造成 {Damage} 伤害，敌人剩余血量 {EnemyHealth}", damage, instance.EnemyHealth);
    }

    /// <summary>
    /// 处理敌人攻击事件
    /// </summary>
    private async Task HandleEnemyAttack(OfflineCombatInstance instance, CombatEvent combatEvent)
    {
        var attackPower = (int)combatEvent.Data.GetValueOrDefault("AttackPower", 8);
        var damage = Math.Max(1, attackPower - CalculatePlayerDefense(instance));
        
        instance.PlayerHealth -= damage;
        
        // 调度下一次敌人攻击
        var nextAttackTime = instance.GameClock + _baseAttackCooldown * 1.2;
        instance.EventQueue.Add(new CombatEvent
        {
            Type = CombatEventType.EnemyAttack,
            TriggerTime = nextAttackTime,
            Data = combatEvent.Data
        });

        _logger.LogTrace("敌人攻击：造成 {Damage} 伤害，玩家剩余血量 {PlayerHealth}", damage, instance.PlayerHealth);
    }

    /// <summary>
    /// 处理战斗结束
    /// </summary>
    private async Task HandleBattleEnd(OfflineCombatInstance instance, CombatFastForwardResult result)
    {
        result.BattleCount++;
        
        if (instance.PlayerHealth > 0) // 玩家胜利
        {
            result.VictoryCount++;
            
            // 计算奖励
            var expReward = CalculateExperienceReward(instance.CurrentWave, instance.Difficulty);
            var goldReward = CalculateGoldReward(instance.CurrentWave, instance.Difficulty);
            
            result.TotalExperience += expReward;
            result.TotalGold += goldReward;
            
            // 进入下一波
            instance.CurrentWave++;
            result.MaxWaveReached = Math.Max(result.MaxWaveReached, instance.CurrentWave);
            
            // 增加难度
            instance.Difficulty = Math.Min(5.0, instance.Difficulty * 1.05);
            result.FinalDifficulty = instance.Difficulty;
            
            // 重新生成敌人
            instance.EnemyHealth = CalculateEnemyHealth(instance.CurrentWave);
            instance.EnemyMaxHealth = instance.EnemyHealth;
            
            // 玩家部分恢复
            instance.PlayerHealth = Math.Min(instance.PlayerMaxHealth, 
                instance.PlayerHealth + instance.PlayerMaxHealth / 4);
        }
        else // 玩家失败
        {
            // 复活并回到较低波次
            instance.PlayerHealth = instance.PlayerMaxHealth;
            instance.CurrentWave = Math.Max(1, instance.CurrentWave - 2);
            instance.Difficulty = Math.Max(0.5, instance.Difficulty * 0.9);
            
            // 重新生成敌人
            instance.EnemyHealth = CalculateEnemyHealth(instance.CurrentWave);
            instance.EnemyMaxHealth = instance.EnemyHealth;
            
            // 失败也有少量经验奖励
            result.TotalExperience += CalculateExperienceReward(instance.CurrentWave, instance.Difficulty) / 4;
        }

        // 清空当前事件队列，重新调度
        instance.EventQueue.Clear();
        ScheduleInitialCombatEvents(instance);

        _logger.LogTrace("战斗结束：玩家 {PlayerId}，波次 {Wave}，{"+(instance.PlayerHealth > 0 ? "胜利" : "失败")+"}",
            SafeLogId(instance.PlayerId), instance.CurrentWave);
    }

    // 计算方法
    private double CalculateAttackSpeed(PlayerStorageDto player) => 1.0 + player.Level * 0.05;
    private double CalculateCombatEfficiency(PlayerStorageDto player) => 1.0 + player.Level * 0.02;
    private int CalculateAttackPower(OfflineCombatInstance instance) => 10 + (int)(instance.CombatEfficiency * 5);
    private int CalculateEnemyAttackPower(int wave) => 8 + wave * 2;
    private int CalculateEnemyHealth(int wave) => 50 + wave * 25;
    private int CalculatePlayerDefense(OfflineCombatInstance instance) => (int)(instance.CombatEfficiency * 2);
    private int CalculateDamage(int attackPower, double difficulty) => (int)(attackPower * (0.8 + new Random().NextDouble() * 0.4) / difficulty);
    private int CalculateExperienceReward(int wave, double difficulty) => (int)(20 + wave * 10 * difficulty);
    private int CalculateGoldReward(int wave, double difficulty) => (int)(5 + wave * 3 * difficulty);

    // 辅助方法
    private async Task<OfflineBattleResultDto?> ExecuteCombatEventPrecise(OfflineCombatInstance instance, CombatEvent combatEvent)
    {
        // 简化版的精确事件执行，用于余数时间处理
        return null; // 这里可以返回具体的战斗结果
    }

    private Task HandleSkillCast(OfflineCombatInstance instance, CombatEvent combatEvent, CombatFastForwardResult result)
    {
        // 技能释放处理逻辑
        return Task.CompletedTask;
    }

    private Task HandleBuffExpire(OfflineCombatInstance instance, CombatEvent combatEvent)
    {
        // Buff过期处理逻辑
        return Task.CompletedTask;
    }

    private async Task SaveCombatInstance(OfflineCombatInstance instance)
    {
        // 保存战斗实例状态到存储系统
        // 这里可以集成现有的数据存储服务
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
public class OfflineCombatInstance
{
    public string PlayerId { get; set; } = string.Empty;
    public double GameClock { get; set; }
    public int CurrentWave { get; set; } = 1;
    public double Difficulty { get; set; } = 1.0;
    public int PlayerHealth { get; set; }
    public int PlayerMaxHealth { get; set; }
    public int EnemyHealth { get; set; }
    public int EnemyMaxHealth { get; set; }
    public List<CombatEvent> EventQueue { get; set; } = new();
    public DateTime LastUpdateTime { get; set; }
    public double AttackSpeed { get; set; } = 1.0;
    public double CombatEfficiency { get; set; } = 1.0;
}

public class CombatEvent
{
    public CombatEventType Type { get; set; }
    public double TriggerTime { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public enum CombatEventType
{
    PlayerAttack,
    EnemyAttack,
    SkillCast,
    BuffExpire
}

public class CombatFastForwardResult
{
    public int BattleCount { get; set; }
    public int VictoryCount { get; set; }
    public int TotalExperience { get; set; }
    public int TotalGold { get; set; }
    public int MaxWaveReached { get; set; } = 1;
    public double FinalDifficulty { get; set; } = 1.0;
    public List<OfflineRewardDto> BattleRewards { get; set; } = new();
}

public class PreciseCombatResult
{
    public List<OfflineBattleResultDto> CompletedBattles { get; set; } = new();
    public int ExperienceGained { get; set; }
    public int GoldGained { get; set; }
    public double PartialProgress { get; set; }
}