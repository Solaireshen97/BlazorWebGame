using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 离线快进引擎
/// </summary>
public class OfflineFastForwardEngine
{
    private readonly GameClock _clock;
    private readonly EventScheduler _scheduler;
    private readonly IGameContext _context;

    public OfflineFastForwardEngine(IGameContext context)
    {
        _context = context;
        _clock = context.Clock;
        _scheduler = context.Scheduler;
    }

    /// <summary>
    /// 执行离线快进
    /// </summary>
    public OfflineRewards FastForward(Character character, TimeSpan offlineDuration)
    {
        var rewards = new OfflineRewards();
        var actualDuration = TimeSpan.FromMilliseconds(
            Math.Min(offlineDuration.TotalMilliseconds, TimeSpan.FromHours(12).TotalMilliseconds)
        );

        var endTime = _clock.CurrentTime.Add(actualDuration);
        var segments = new List<CombatSegment>();

        // 恢复活动计划
        var activities = RestoreActivities(character);

        // 开始模拟
        while (_clock.CurrentTime < endTime)
        {
            // 获取下一个事件
            var nextEvent = _scheduler.PopNext();

            if (nextEvent == null || nextEvent.TriggerTime > endTime)
            {
                // 没有更多事件或超出时间范围
                _clock.AdvanceTo(endTime);
                break;
            }

            // 推进时间
            _clock.AdvanceTo(nextEvent.TriggerTime);

            // 执行事件
            nextEvent.Event.Execute(_context);

            // 收集奖励
            if (nextEvent.Event is ICombatRewardEvent combatEvent)
            {
                rewards.Experience += combatEvent.Experience;
                rewards.Gold += combatEvent.Gold;
                foreach (var item in combatEvent.Items)
                {
                    rewards.Items.Add(item);
                }
            }

            // 检查活动完成
            CheckActivityCompletion(activities, rewards);
        }

        rewards.Duration = actualDuration;
        rewards.SegmentsGenerated = segments.Count;

        return rewards;
    }

    private List<ActivityPlan> RestoreActivities(Character character)
    {
        // TODO: 从character恢复活动计划
        return new List<ActivityPlan>();
    }

    private void CheckActivityCompletion(List<ActivityPlan> activities, OfflineRewards rewards)
    {
        foreach (var activity in activities)
        {
            if (activity.IsCompleted())
            {
                rewards.CompletedActivities.Add(activity.Id);
            }
        }
    }
}

/// <summary>
/// 离线奖励
/// </summary>
public class OfflineRewards
{
    public TimeSpan Duration { get; set; }
    public int Experience { get; set; }
    public int Gold { get; set; }
    public List<string> Items { get; set; } = new();
    public List<string> CompletedActivities { get; set; } = new();
    public int SegmentsGenerated { get; set; }
    public Dictionary<string, int> ResourceGains { get; set; } = new();
    public Dictionary<string, int> ProfessionExperience { get; set; } = new();
}

/// <summary>
/// 战斗奖励事件接口
/// </summary>
public interface ICombatRewardEvent : IGameEvent
{
    int Experience { get; }
    int Gold { get; }
    List<string> Items { get; }
}