using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Domain.Events;
using BlazorWebGame.Refactored.Infrastructure.Events.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BlazorWebGame.Refactored.Application.Systems;

/// <summary>
/// 活动系统 - 管理所有角色活动
/// </summary>
public sealed class ActivitySystem : IGameSystem
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ActivitySystem> _logger;
    private readonly ConcurrentDictionary<Guid, ActivityInstance> _activeActivities = new();
    
    public string Name => "ActivitySystem";
    public int Priority => 2;
    
    public ActivitySystem(IEventBus eventBus, ILogger<ActivitySystem> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // 订阅活动相关命令
        _eventBus.Subscribe<StartActivityCommand>(HandleStartActivityAsync);
        _eventBus.Subscribe<StopActivityCommand>(HandleStopActivityAsync);
        
        _logger.LogInformation("Activity system initialized");
    }
    
    public bool ShouldProcess(double deltaTime) => _activeActivities.Any();
    
    public async Task ProcessAsync(double deltaTime, CancellationToken cancellationToken)
    {
        var completedActivities = new List<Guid>();
        
        // 处理所有活跃活动
        await Parallel.ForEachAsync(_activeActivities.Values, cancellationToken, async (activity, ct) =>
        {
            activity.Update(deltaTime);
            
            // 发送进度更新事件
            await _eventBus.PublishAsync(new ActivityProgressEvent
            {
                ActivityId = activity.Id,
                CharacterId = activity.CharacterId,
                Progress = activity.Progress,
                RemainingTime = activity.RemainingTime
            }, ct);
            
            // 检查活动完成
            if (activity.IsCompleted)
            {
                completedActivities.Add(activity.Id);
            }
        });
        
        // 完成已结束的活动
        foreach (var activityId in completedActivities)
        {
            await CompleteActivityAsync(activityId, cancellationToken);
        }
    }
    
    private async Task HandleStartActivityAsync(StartActivityCommand command, CancellationToken cancellationToken)
    {
        var activityId = Guid.NewGuid();
        var duration = GetActivityDuration(command.ActivityType);
        
        var activity = new ActivityInstance
        {
            Id = activityId,
            CharacterId = command.CharacterId,
            Type = command.ActivityType,
            StartTime = DateTime.UtcNow,
            Duration = duration,
            TargetId = command.TargetId
        };
        
        _activeActivities[activityId] = activity;
        
        await _eventBus.PublishAsync(new NewActivityStartedEvent
        {
            ActivityId = activityId,
            CharacterId = command.CharacterId,
            ActivityType = command.ActivityType,
            Duration = duration
        }, cancellationToken);
        
        _logger.LogInformation("Activity {ActivityType} started for character {CharacterId}", 
            command.ActivityType, command.CharacterId);
    }
    
    private async Task HandleStopActivityAsync(StopActivityCommand command, CancellationToken cancellationToken)
    {
        if (_activeActivities.TryRemove(command.ActivityId, out var activity))
        {
            _logger.LogInformation("Activity {ActivityId} stopped for character {CharacterId}", 
                command.ActivityId, command.CharacterId);
        }
    }
    
    private async Task CompleteActivityAsync(Guid activityId, CancellationToken cancellationToken)
    {
        if (!_activeActivities.TryRemove(activityId, out var activity))
            return;
        
        var rewards = CalculateRewards(activity);
        
        await _eventBus.PublishAsync(new NewActivityCompletedEvent
        {
            ActivityId = activityId,
            CharacterId = activity.CharacterId,
            Rewards = rewards
        }, cancellationToken);
        
        // 发放奖励
        foreach (var reward in rewards)
        {
            await _eventBus.PublishAsync(new ItemAcquiredEvent
            {
                CharacterId = activity.CharacterId,
                ItemId = reward.Id,
                Quantity = reward.Quantity,
                Source = $"Activity:{activity.Type}"
            }, cancellationToken);
        }
        
        _logger.LogInformation("Activity {ActivityId} completed for character {CharacterId}", 
            activityId, activity.CharacterId);
    }
    
    private TimeSpan GetActivityDuration(EventActivityType type)
    {
        return type switch
        {
            EventActivityType.Mining => TimeSpan.FromSeconds(30),
            EventActivityType.Fishing => TimeSpan.FromSeconds(45),
            EventActivityType.Crafting => TimeSpan.FromMinutes(2),
            EventActivityType.Gathering => TimeSpan.FromSeconds(20),
            EventActivityType.Battle => TimeSpan.FromMinutes(1),
            EventActivityType.Quest => TimeSpan.FromMinutes(5),
            _ => TimeSpan.FromSeconds(30)
        };
    }
    
    private List<Reward> CalculateRewards(ActivityInstance activity)
    {
        var rewards = new List<Reward>();
        
        // 简单的奖励计算逻辑
        switch (activity.Type)
        {
            case EventActivityType.Mining:
                rewards.Add(new Reward("Item", "ore_iron", Random.Shared.Next(1, 4)));
                break;
            case EventActivityType.Fishing:
                rewards.Add(new Reward("Item", "fish_common", Random.Shared.Next(1, 3)));
                break;
            case EventActivityType.Crafting:
                rewards.Add(new Reward("Item", "crafted_item", 1));
                break;
            case EventActivityType.Gathering:
                rewards.Add(new Reward("Item", "herb_common", Random.Shared.Next(2, 6)));
                break;
            case EventActivityType.Battle:
                rewards.Add(new Reward("Experience", "", Random.Shared.Next(10, 50)));
                rewards.Add(new Reward("Gold", "", Random.Shared.Next(5, 25)));
                break;
            case EventActivityType.Quest:
                rewards.Add(new Reward("Experience", "", Random.Shared.Next(50, 200)));
                rewards.Add(new Reward("Gold", "", Random.Shared.Next(25, 100)));
                break;
        }
        
        return rewards;
    }
}

/// <summary>
/// 活动实例
/// </summary>
public class ActivityInstance
{
    public required Guid Id { get; init; }
    public required string CharacterId { get; init; }
    public required EventActivityType Type { get; init; }
    public required DateTime StartTime { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? TargetId { get; init; }
    
    public double Progress => Math.Min(1.0, (DateTime.UtcNow - StartTime).TotalSeconds / Duration.TotalSeconds);
    public TimeSpan RemainingTime => Duration - (DateTime.UtcNow - StartTime);
    public bool IsCompleted => Progress >= 1.0;
    
    public void Update(double deltaTime)
    {
        // 活动更新逻辑，如果需要的话
    }
}