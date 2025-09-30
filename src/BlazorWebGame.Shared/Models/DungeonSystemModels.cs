using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 副本系统
/// </summary>
public class DungeonRun
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string DungeonId { get; private set; } = string.Empty;
    public Guid PartyId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public DungeonState State { get; private set; } = DungeonState.InProgress;
    public int CurrentWave { get; private set; } = 1;
    public int TotalWaves { get; private set; }

    // 持续模式设置
    public bool ContinuousMode { get; private set; } = false;
    public TimeSpan MaxContinuousDuration { get; private set; } = TimeSpan.FromHours(8);
    public double DiminishingFactor { get; private set; } = 1.0;

    // 副本片段
    private readonly List<DungeonSegment> _segments = new();
    public IReadOnlyList<DungeonSegment> Segments => _segments.AsReadOnly();

    // 参与者状态
    public Dictionary<string, DungeonParticipantState> Participants { get; private set; } = new();

    public DungeonRun(string dungeonId, Guid partyId, int totalWaves = 10)
    {
        DungeonId = dungeonId;
        PartyId = partyId;
        TotalWaves = totalWaves;
        StartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 启用持续模式
    /// </summary>
    public void EnableContinuousMode(TimeSpan? maxDuration = null)
    {
        ContinuousMode = true;
        if (maxDuration.HasValue)
        {
            MaxContinuousDuration = maxDuration.Value;
        }
    }

    /// <summary>
    /// 完成当前波次
    /// </summary>
    public DungeonWaveResult CompleteWave(List<LootDrop> loot, int experience)
    {
        var result = new DungeonWaveResult
        {
            Wave = CurrentWave,
            Loot = loot,
            Experience = experience,
            CompletedAt = DateTime.UtcNow
        };

        // 应用递减因子
        if (ContinuousMode)
        {
            var duration = DateTime.UtcNow - StartTime;
            if (duration > TimeSpan.FromHours(2))
            {
                // 2小时后开始递减
                var hours = duration.TotalHours - 2;
                DiminishingFactor = Math.Max(0.5, 1.0 - (hours * 0.1));

                result.Experience = (int)(result.Experience * DiminishingFactor);
                // 减少掉落数量
                var reducedLoot = result.Loot.Take((int)(result.Loot.Count * DiminishingFactor)).ToList();
                result.Loot = reducedLoot;
            }
        }

        CurrentWave++;

        if (CurrentWave > TotalWaves && !ContinuousMode)
        {
            Complete();
        }

        return result;
    }

    /// <summary>
    /// 添加副本片段
    /// </summary>
    public void AddSegment(DungeonSegment segment)
    {
        _segments.Add(segment);

        // 限制片段数量
        if (_segments.Count > 100)
        {
            _segments.RemoveAt(0);
        }
    }

    /// <summary>
    /// 检查是否应该终止
    /// </summary>
    public bool ShouldTerminate()
    {
        if (!ContinuousMode)
            return CurrentWave > TotalWaves;

        var duration = DateTime.UtcNow - StartTime;
        return duration >= MaxContinuousDuration;
    }

    /// <summary>
    /// 完成副本
    /// </summary>
    public void Complete()
    {
        State = DungeonState.Completed;
        EndTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 放弃副本
    /// </summary>
    public void Abandon()
    {
        State = DungeonState.Abandoned;
        EndTime = DateTime.UtcNow;
    }
}

/// <summary>
/// 副本片段
/// </summary>
public class DungeonSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int StartWave { get; set; }
    public int EndWave { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Dictionary<string, DungeonParticipantStats> ParticipantStats { get; set; } = new();
    public int TotalDamage { get; set; }
    public int TotalHealing { get; set; }
    public List<string> LootedItems { get; set; } = new();
    public int ExperienceGained { get; set; }
}

/// <summary>
/// 副本参与者状态
/// </summary>
public class DungeonParticipantState
{
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = true;
    public int DeathCount { get; set; } = 0;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
}

/// <summary>
/// 副本参与者统计
/// </summary>
public class DungeonParticipantStats
{
    public int DamageDealt { get; set; }
    public int HealingDone { get; set; }
    public int DamageTaken { get; set; }
    public int Deaths { get; set; }
    public int SkillsUsed { get; set; }
}

/// <summary>
/// 副本波次结果
/// </summary>
public class DungeonWaveResult
{
    public int Wave { get; set; }
    public List<LootDrop> Loot { get; set; } = new();
    public int Experience { get; set; }
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// 副本状态
/// </summary>
public enum DungeonState
{
    InProgress,
    Completed,
    Failed,
    Abandoned
}

/// <summary>
/// 副本定义
/// </summary>
public class DungeonDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MinLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 60;
    public int MinPlayers { get; set; } = 1;
    public int MaxPlayers { get; set; } = 5;
    public List<DungeonWaveDefinition> Waves { get; set; } = new();
    public string UnlockConditionExpr { get; set; } = string.Empty;
    public bool AllowContinuousMode { get; set; } = false;
    public DungeonRewards BaseRewards { get; set; } = new();
}

/// <summary>
/// 副本波次定义
/// </summary>
public class DungeonWaveDefinition
{
    public int WaveNumber { get; set; }
    public List<string> EnemyIds { get; set; } = new();
    public Dictionary<string, int> EnemyCounts { get; set; } = new();
    public double DifficultyMultiplier { get; set; } = 1.0;
    public TimeSpan? TimeLimit { get; set; }
}

/// <summary>
/// 副本奖励
/// </summary>
public class DungeonRewards
{
    public int BaseExperience { get; set; }
    public int BaseGold { get; set; }
    public Dictionary<string, double> GuaranteedItems { get; set; } = new();
    public Dictionary<string, double> ChanceItems { get; set; } = new();
    public Dictionary<string, int> ReputationRewards { get; set; } = new();
}