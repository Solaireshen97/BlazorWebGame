using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 活动计划系统 - 多槽位管理
/// </summary>
public class ActivitySystem
{
    public string CharacterId { get; private set; }
    public List<ActivitySlot> Slots { get; private set; } = new();
    public int MaxSlots { get; private set; } = 3;

    public ActivitySystem(string characterId, int maxSlots = 3)
    {
        CharacterId = characterId;
        MaxSlots = maxSlots;

        // 初始化槽位
        for (int i = 0; i < maxSlots; i++)
        {
            Slots.Add(new ActivitySlot(i));
        }
    }

    /// <summary>
    /// 添加活动计划到槽位
    /// </summary>
    public bool AddPlan(ActivityPlan plan, int slotIndex = 0)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
            return false;

        var slot = Slots[slotIndex];
        return slot.EnqueuePlan(plan);
    }

    /// <summary>
    /// 获取当前活跃的计划
    /// </summary>
    public List<ActivityPlan> GetActivePlans()
    {
        return Slots
            .Where(s => s.CurrentPlan != null && s.CurrentPlan.State == PlanState.Running)
            .Select(s => s.CurrentPlan!)
            .ToList();
    }

    /// <summary>
    /// 处理时间推进
    /// </summary>
    public void ProcessTick(GameClock clock, IGameContext context)
    {
        foreach (var slot in Slots)
        {
            slot.ProcessTick(clock, context);
        }
    }
}

/// <summary>
/// 活动槽位
/// </summary>
public class ActivitySlot
{
    public int Index { get; private set; }
    public ActivityPlan? CurrentPlan { get; private set; }
    public Queue<ActivityPlan> PlanQueue { get; private set; } = new();
    public bool IsLocked { get; private set; } = false;

    public ActivitySlot(int index)
    {
        Index = index;
    }

    /// <summary>
    /// 将计划加入队列
    /// </summary>
    public bool EnqueuePlan(ActivityPlan plan)
    {
        if (IsLocked)
            return false;

        if (CurrentPlan == null)
        {
            CurrentPlan = plan;
            plan.Start();
            return true;
        }

        PlanQueue.Enqueue(plan);
        return true;
    }

    /// <summary>
    /// 处理时间推进
    /// </summary>
    public void ProcessTick(GameClock clock, IGameContext context)
    {
        if (CurrentPlan == null)
        {
            TryStartNextPlan();
            return;
        }

        if (CurrentPlan.State == PlanState.Running)
        {
            CurrentPlan.UpdateProgress(clock);

            if (CurrentPlan.IsCompleted())
            {
                CompletePlan(context);
            }
        }
    }

    private void CompletePlan(IGameContext context)
    {
        if (CurrentPlan == null) return;

        CurrentPlan.Complete();

        // 发出完成事件
        context.EmitDomainEvent(new PlanCompletedEvent
        {
            PlanId = CurrentPlan.Id,
            PlanType = CurrentPlan.Type,
            CompletedAt = context.Clock.CurrentTime
        });

        CurrentPlan = null;
        TryStartNextPlan();
    }

    private void TryStartNextPlan()
    {
        if (PlanQueue.Count > 0)
        {
            CurrentPlan = PlanQueue.Dequeue();
            CurrentPlan.Start();
        }
    }
}

/// <summary>
/// 活动计划
/// </summary>
public class ActivityPlan
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Type { get; private set; } = string.Empty; // Combat, Gather, Craft, Quest
    public PlanState State { get; private set; } = PlanState.Pending;
    public LimitSpec Limit { get; private set; }
    public Dictionary<string, object> Payload { get; private set; } = new();
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public double Progress { get; private set; } = 0;

    public ActivityPlan(string type, LimitSpec limit)
    {
        Type = type;
        Limit = limit;
    }

    public void Start()
    {
        State = PlanState.Running;
        StartedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(GameClock clock)
    {
        if (State != PlanState.Running) return;

        switch (Limit.Type)
        {
            case LimitType.Count:
                // 计数型进度由外部事件更新
                break;

            case LimitType.Duration:
                if (StartedAt.HasValue)
                {
                    var elapsed = clock.CurrentTime - StartedAt.Value;
                    Progress = Math.Min(1.0, elapsed.TotalSeconds / Limit.TargetValue);
                }
                break;

            case LimitType.Infinite:
                // 无限制，永不完成
                Progress = 0;
                break;
        }
    }

    public bool IsCompleted()
    {
        return Limit.Type switch
        {
            LimitType.Count => Limit.Remaining <= 0,
            LimitType.Duration => Progress >= 1.0,
            LimitType.Infinite => false,
            _ => false
        };
    }

    public void Complete()
    {
        State = PlanState.Completed;
        CompletedAt = DateTime.UtcNow;
        Progress = 1.0;
    }

    public void Cancel()
    {
        State = PlanState.Cancelled;
    }
}

/// <summary>
/// 限制规格
/// </summary>
public class LimitSpec
{
    public LimitType Type { get; set; }
    public double TargetValue { get; set; }
    public double Remaining { get; set; }

    public static LimitSpec Count(int count)
    {
        return new LimitSpec
        {
            Type = LimitType.Count,
            TargetValue = count,
            Remaining = count
        };
    }

    public static LimitSpec Duration(TimeSpan duration)
    {
        return new LimitSpec
        {
            Type = LimitType.Duration,
            TargetValue = duration.TotalSeconds,
            Remaining = duration.TotalSeconds
        };
    }

    public static LimitSpec Infinite()
    {
        return new LimitSpec
        {
            Type = LimitType.Infinite,
            TargetValue = double.MaxValue,
            Remaining = double.MaxValue
        };
    }

    public void Decrement(double amount = 1)
    {
        if (Type == LimitType.Count)
        {
            Remaining = Math.Max(0, Remaining - amount);
        }
    }
}

/// <summary>
/// 限制类型
/// </summary>
public enum LimitType
{
    Count,      // 次数限制
    Duration,   // 时间限制
    Infinite    // 无限制
}

/// <summary>
/// 计划状态
/// </summary>
public enum PlanState
{
    Pending,
    Running,
    Completed,
    Cancelled
}