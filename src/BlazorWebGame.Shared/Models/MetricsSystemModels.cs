using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 指标收集器
/// </summary>
public class MetricsCollector
{
    private readonly Dictionary<string, MetricCounter> _counters = new();
    private readonly Dictionary<string, MetricGauge> _gauges = new();
    private readonly Dictionary<string, MetricHistogram> _histograms = new();

    /// <summary>
    /// 增加计数器
    /// </summary>
    public void IncrementCounter(string name, double value = 1, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        if (!_counters.ContainsKey(key))
        {
            _counters[key] = new MetricCounter(name, tags);
        }
        _counters[key].Increment(value);
    }

    /// <summary>
    /// 设置仪表值
    /// </summary>
    public void SetGauge(string name, double value, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        if (!_gauges.ContainsKey(key))
        {
            _gauges[key] = new MetricGauge(name, tags);
        }
        _gauges[key].Set(value);
    }

    /// <summary>
    /// 记录直方图值
    /// </summary>
    public void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        if (!_histograms.ContainsKey(key))
        {
            _histograms[key] = new MetricHistogram(name, tags);
        }
        _histograms[key].Record(value);
    }

    private string BuildKey(string name, Dictionary<string, string>? tags)
    {
        if (tags == null || tags.Count == 0)
            return name;

        var tagStr = string.Join(",", tags.OrderBy(kvp => kvp.Key)
            .Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{name}_{tagStr}";
    }

    /// <summary>
    /// 获取所有指标快照
    /// </summary>
    public MetricsSnapshot GetSnapshot()
    {
        return new MetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            Counters = _counters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value),
            Gauges = _gauges.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value),
            Histograms = _histograms.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetStats())
        };
    }
}

/// <summary>
/// 计数器指标
/// </summary>
public class MetricCounter
{
    public string Name { get; }
    public Dictionary<string, string>? Tags { get; }
    public double Value { get; private set; }

    public MetricCounter(string name, Dictionary<string, string>? tags = null)
    {
        Name = name;
        Tags = tags;
    }

    public void Increment(double amount = 1)
    {
        Value += amount;
    }
}

/// <summary>
/// 仪表指标
/// </summary>
public class MetricGauge
{
    public string Name { get; }
    public Dictionary<string, string>? Tags { get; }
    public double Value { get; private set; }

    public MetricGauge(string name, Dictionary<string, string>? tags = null)
    {
        Name = name;
        Tags = tags;
    }

    public void Set(double value)
    {
        Value = value;
    }
}

/// <summary>
/// 直方图指标
/// </summary>
public class MetricHistogram
{
    public string Name { get; }
    public Dictionary<string, string>? Tags { get; }
    private readonly List<double> _values = new();

    public MetricHistogram(string name, Dictionary<string, string>? tags = null)
    {
        Name = name;
        Tags = tags;
    }

    public void Record(double value)
    {
        _values.Add(value);

        // 限制历史数据量
        if (_values.Count > 10000)
        {
            _values.RemoveRange(0, _values.Count - 10000);
        }
    }

    public HistogramStats GetStats()
    {
        if (_values.Count == 0)
            return new HistogramStats();

        var sorted = _values.OrderBy(v => v).ToList();
        return new HistogramStats
        {
            Count = sorted.Count,
            Min = sorted[0],
            Max = sorted[sorted.Count - 1],
            Mean = sorted.Average(),
            P50 = GetPercentile(sorted, 0.50),
            P90 = GetPercentile(sorted, 0.90),
            P99 = GetPercentile(sorted, 0.99)
        };
    }

    private double GetPercentile(List<double> sorted, double percentile)
    {
        var index = (int)(sorted.Count * percentile);
        return sorted[Math.Min(index, sorted.Count - 1)];
    }
}

/// <summary>
/// 直方图统计
/// </summary>
public class HistogramStats
{
    public int Count { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Mean { get; set; }
    public double P50 { get; set; }
    public double P90 { get; set; }
    public double P99 { get; set; }
}

/// <summary>
/// 指标快照
/// </summary>
public class MetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, double> Counters { get; set; } = new();
    public Dictionary<string, double> Gauges { get; set; } = new();
    public Dictionary<string, HistogramStats> Histograms { get; set; } = new();
}

/// <summary>
/// 调试快照构建器
/// </summary>
public class DebugSnapshotBuilder
{
    public DebugSnapshot Build(BattleInstance battle, ActivitySystem activities, GameClock clock)
    {
        return new DebugSnapshot
        {
            Timestamp = clock.CurrentTime,
            BattleState = BuildBattleState(battle),
            ActivityState = BuildActivityState(activities),
            ResourceState = BuildResourceState(battle),
            TrackState = BuildTrackState(battle),
            RecentSegments = battle.SegmentAggregator != null
                ? new List<CombatSegment>()
                : new List<CombatSegment>()
        };
    }

    private BattleDebugState BuildBattleState(BattleInstance battle)
    {
        return new BattleDebugState
        {
            BattleId = battle.Id,
            State = battle.State.ToString(),
            CombatantCount = battle.Combatants.Count,
            ActiveBuffCount = battle.ActiveBuffs.Count,
            SegmentEventCount = battle.SegmentAggregator?.EventCount ?? 0
        };
    }

    private ActivityDebugState BuildActivityState(ActivitySystem activities)
    {
        return new ActivityDebugState
        {
            ActivePlans = activities.GetActivePlans().Count,
            SlotStates = activities.Slots.Select(s => new SlotDebugInfo
            {
                Index = s.Index,
                HasCurrentPlan = s.CurrentPlan != null,
                QueueLength = s.PlanQueue.Count
            }).ToList()
        };
    }

    private ResourceDebugState BuildResourceState(BattleInstance battle)
    {
        return new ResourceDebugState
        {
            Buckets = battle.ResourceBuckets.Select(kvp => new BucketDebugInfo
            {
                Id = kvp.Key,
                Current = kvp.Value.Current,
                Max = kvp.Value.Max,
                FillRatio = kvp.Value.Current / kvp.Value.Max
            }).ToList()
        };
    }

    private TrackDebugState BuildTrackState(BattleInstance battle)
    {
        return new TrackDebugState
        {
            Tracks = battle.Tracks.Select(kvp => new TrackDebugInfo
            {
                Type = kvp.Key,
                NextTriggerAt = kvp.Value.NextTriggerAt,
                Interval = kvp.Value.GetActualInterval()
            }).ToList()
        };
    }
}

/// <summary>
/// 调试快照
/// </summary>
public class DebugSnapshot
{
    public DateTime Timestamp { get; set; }
    public BattleDebugState? BattleState { get; set; }
    public ActivityDebugState? ActivityState { get; set; }
    public ResourceDebugState? ResourceState { get; set; }
    public TrackDebugState? TrackState { get; set; }
    public List<CombatSegment> RecentSegments { get; set; } = new();
}

public class BattleDebugState
{
    public Guid BattleId { get; set; }
    public string State { get; set; } = string.Empty;
    public int CombatantCount { get; set; }
    public int ActiveBuffCount { get; set; }
    public int SegmentEventCount { get; set; }
}

public class ActivityDebugState
{
    public int ActivePlans { get; set; }
    public List<SlotDebugInfo> SlotStates { get; set; } = new();
}

public class SlotDebugInfo
{
    public int Index { get; set; }
    public bool HasCurrentPlan { get; set; }
    public int QueueLength { get; set; }
}

public class ResourceDebugState
{
    public List<BucketDebugInfo> Buckets { get; set; } = new();
}

public class BucketDebugInfo
{
    public string Id { get; set; } = string.Empty;
    public double Current { get; set; }
    public double Max { get; set; }
    public double FillRatio { get; set; }
}

public class TrackDebugState
{
    public List<TrackDebugInfo> Tracks { get; set; } = new();
}

public class TrackDebugInfo
{
    public string Type { get; set; } = string.Empty;
    public DateTime NextTriggerAt { get; set; }
    public TimeSpan Interval { get; set; }
}