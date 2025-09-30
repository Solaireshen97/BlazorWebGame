using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 战斗实例 - 运行时根对象
/// </summary>
public class BattleInstance
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string BattleType { get; private set; } = "Normal";
    public DateTime StartTime { get; private set; }
    public BattleState State { get; private set; } = BattleState.Active;

    // 参与者
    public Dictionary<string, CombatantState> Combatants { get; private set; } = new();

    // 轨道状态
    public Dictionary<string, TrackState> Tracks { get; private set; } = new();

    // 资源桶
    public Dictionary<string, ResourceBucket> ResourceBuckets { get; private set; } = new();

    // 活跃Buff
    public List<BuffInstance> ActiveBuffs { get; private set; } = new();

    // 技能冷却
    public Dictionary<string, CooldownState> SkillCooldowns { get; private set; } = new();

    // 战斗片段聚合器
    public CombatSegmentAggregator SegmentAggregator { get; private set; }

    // RNG上下文
    public RNGContext RngContext { get; private set; }

    // 配置版本固定
    public string ConfigVersion { get; private set; } = "1.0.0";

    public BattleInstance(string battleType, GameClock clock, string seed)
    {
        BattleType = battleType;
        StartTime = clock.CurrentTime;
        RngContext = new RNGContext(seed);
        SegmentAggregator = new CombatSegmentAggregator();

        // 初始化双轨
        InitializeTracks();
    }

    private void InitializeTracks()
    {
        // 攻击轨道
        Tracks["attack"] = new TrackState
        {
            TrackType = "attack",
            BaseInterval = TimeSpan.FromSeconds(1.0),
            HasteFactor = 1.0,
            NextTriggerAt = StartTime.AddSeconds(1.0)
        };

        // 特殊轨道
        Tracks["special"] = new TrackState
        {
            TrackType = "special",
            BaseInterval = TimeSpan.FromSeconds(5.0),
            HasteFactor = 1.0, // 特殊轨道默认不受急速影响
            NextTriggerAt = StartTime.AddSeconds(5.0)
        };
    }

    /// <summary>
    /// 添加参战者
    /// </summary>
    public void AddCombatant(CombatantState combatant)
    {
        Combatants[combatant.Id] = combatant;

        // 初始化参战者的资源桶
        InitializeCombatantResources(combatant);
    }

    private void InitializeCombatantResources(CombatantState combatant)
    {
        // 根据职业初始化不同资源
        if (combatant.Profession == "Warrior")
        {
            ResourceBuckets[$"{combatant.Id}_rage"] = new ResourceBucket
            {
                Id = "rage",
                OwnerId = combatant.Id,
                Current = 0,
                Max = 100,
                OverflowPolicy = OverflowPolicy.Convert,
                ConvertTarget = "battle_focus",
                ConvertRatio = 20 // 20怒气转1层专注
            };
        }
        else if (combatant.Profession == "Mage")
        {
            ResourceBuckets[$"{combatant.Id}_frost_shard"] = new ResourceBucket
            {
                Id = "frost_shard",
                OwnerId = combatant.Id,
                Current = 0,
                Max = 10,
                OverflowPolicy = OverflowPolicy.Clamp
            };
        }
    }
}

/// <summary>
/// 轨道状态
/// </summary>
public class TrackState
{
    public string TrackType { get; set; } = string.Empty;
    public TimeSpan BaseInterval { get; set; }
    public double HasteFactor { get; set; } = 1.0;
    public DateTime NextTriggerAt { get; set; }

    /// <summary>
    /// 计算实际间隔
    /// </summary>
    public TimeSpan GetActualInterval()
    {
        return TimeSpan.FromMilliseconds(BaseInterval.TotalMilliseconds / HasteFactor);
    }

    /// <summary>
    /// 更新下次触发时间
    /// </summary>
    public void UpdateNextTrigger(DateTime currentTime)
    {
        NextTriggerAt = currentTime.Add(GetActualInterval());
    }
}

/// <summary>
/// 资源桶
/// </summary>
public class ResourceBucket
{
    public string Id { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public double Current { get; set; }
    public double Max { get; set; }
    public OverflowPolicy OverflowPolicy { get; set; } = OverflowPolicy.Clamp;
    public string? ConvertTarget { get; set; }
    public double ConvertRatio { get; set; } = 1.0;

    /// <summary>
    /// 添加资源
    /// </summary>
    public ResourceGainResult Gain(double amount)
    {
        var result = new ResourceGainResult();
        var newValue = Current + amount;

        if (newValue > Max)
        {
            var overflow = newValue - Max;
            Current = Max;

            result.Overflow = overflow;
            result.OverflowHandled = OverflowPolicy switch
            {
                OverflowPolicy.Convert when !string.IsNullOrEmpty(ConvertTarget) =>
                    new OverflowConversion
                    {
                        TargetResource = ConvertTarget,
                        ConvertedAmount = Math.Floor(overflow / ConvertRatio)
                    },
                _ => null
            };
        }
        else
        {
            Current = Math.Max(0, newValue);
        }

        result.ActualGain = Current - (Current - amount);
        return result;
    }

    /// <summary>
    /// 消耗资源
    /// </summary>
    public bool Consume(double amount)
    {
        if (Current < amount)
            return false;

        Current -= amount;
        return true;
    }
}

/// <summary>
/// 资源获得结果
/// </summary>
public class ResourceGainResult
{
    public double ActualGain { get; set; }
    public double Overflow { get; set; }
    public OverflowConversion? OverflowHandled { get; set; }
}

/// <summary>
/// 溢出转换
/// </summary>
public class OverflowConversion
{
    public string TargetResource { get; set; } = string.Empty;
    public double ConvertedAmount { get; set; }
}

/// <summary>
/// 溢出策略
/// </summary>
public enum OverflowPolicy
{
    Clamp,      // 截断
    Convert     // 转换
}

/// <summary>
/// 参战者状态
/// </summary>
public class CombatantState
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsPlayer { get; set; }
    public string Profession { get; set; } = string.Empty;
    public int Level { get; set; } = 1;

    public double Health { get; set; }
    public double MaxHealth { get; set; }
    public bool IsAlive => Health > 0;

    // 战斗属性
    public double AttackPower { get; set; }
    public double AttackSpeed { get; set; } = 1.0;
    public double CriticalChance { get; set; } = 0.05;
    public double CriticalMultiplier { get; set; } = 1.5;
    public double DodgeChance { get; set; } = 0.0;

    // 当前目标
    public string? CurrentTargetId { get; set; }
}

/// <summary>
/// Buff实例
/// </summary>
public class BuffInstance
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BuffDefId { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public int Stacks { get; set; } = 1;
    public int MaxStacks { get; set; } = 1;
    public DateTime AppliedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, double> Effects { get; set; } = new();

    public bool IsExpired(DateTime currentTime)
    {
        return ExpiresAt.HasValue && currentTime >= ExpiresAt.Value;
    }
}

/// <summary>
/// 技能冷却状态
/// </summary>
public class CooldownState
{
    public string SkillId { get; set; } = string.Empty;
    public DateTime ReadyAt { get; set; }

    public bool IsReady(DateTime currentTime)
    {
        return currentTime >= ReadyAt;
    }
}

/// <summary>
/// 战斗片段聚合器
/// </summary>
public class CombatSegmentAggregator
{
    private readonly List<IGameEvent> _events = new();
    private DateTime _startTime;
    private DateTime _lastEventTime;
    private readonly Dictionary<string, double> _damageBySource = new();
    private readonly Dictionary<string, double> _resourceGains = new();

    public int EventCount => _events.Count;
    public TimeSpan Duration => _lastEventTime - _startTime;

    private const int MaxEventsPerSegment = 200;
    private static readonly TimeSpan MaxSegmentDuration = TimeSpan.FromSeconds(5);

    public void StartNewSegment(DateTime time)
    {
        _startTime = time;
        _lastEventTime = time;
        _events.Clear();
        _damageBySource.Clear();
        _resourceGains.Clear();
    }

    public void AddEvent(IGameEvent gameEvent)
    {
        _events.Add(gameEvent);
        _lastEventTime = gameEvent.Timestamp;

        // 聚合统计
        if (gameEvent is DamageEvent dmgEvent)
        {
            if (!_damageBySource.ContainsKey(dmgEvent.SourceId))
                _damageBySource[dmgEvent.SourceId] = 0;
            _damageBySource[dmgEvent.SourceId] += dmgEvent.Amount;
        }
        else if (gameEvent is ResourceGainEvent resEvent)
        {
            if (!_resourceGains.ContainsKey(resEvent.ResourceId))
                _resourceGains[resEvent.ResourceId] = 0;
            _resourceGains[resEvent.ResourceId] += resEvent.Amount;
        }
    }

    public bool ShouldFlush(DateTime currentTime)
    {
        return EventCount >= MaxEventsPerSegment ||
               (currentTime - _startTime) >= MaxSegmentDuration;
    }

    public CombatSegment CreateSegment()
    {
        return new CombatSegment
        {
            StartTime = _startTime,
            EndTime = _lastEventTime,
            EventCount = EventCount,
            DamageBySource = new Dictionary<string, double>(_damageBySource),
            ResourceFlow = new Dictionary<string, double>(_resourceGains),
            RngSeedStart = 0, // TODO: 从RNGContext获取
            RngSeedEnd = 0
        };
    }
}

/// <summary>
/// 战斗片段
/// </summary>
public class CombatSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int EventCount { get; set; }
    public Dictionary<string, double> DamageBySource { get; set; } = new();
    public Dictionary<string, double> ResourceFlow { get; set; } = new();
    public Dictionary<string, double> BuffUptime { get; set; } = new();
    public long RngSeedStart { get; set; }
    public long RngSeedEnd { get; set; }
}

/// <summary>
/// RNG上下文
/// </summary>
public class RNGContext
{
    private readonly Random _random;
    private long _seedIndex;

    public RNGContext(string seed)
    {
        var hash = seed.GetHashCode();
        _random = new Random(hash);
        _seedIndex = 0;
    }

    public double NextDouble()
    {
        _seedIndex++;
        return _random.NextDouble();
    }

    public int Next(int min, int max)
    {
        _seedIndex++;
        return _random.Next(min, max);
    }

    public long GetSeedIndex() => _seedIndex;
}

/// <summary>
/// 战斗状态枚举
/// </summary>
public enum BattleState
{
    Preparing,
    Active,
    Paused,
    Completed,
    Cancelled
}