using BlazorWebGame.Refactored.Domain.ValueObjects;
using BlazorWebGame.Refactored.Domain.Events;

namespace BlazorWebGame.Refactored.Domain.Entities;

/// <summary>
/// 角色聚合根 - 核心领域实体
/// </summary>
public class Character : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Level { get; private set; }
    public BigNumber Experience { get; private set; } = BigNumber.Zero;
    public CharacterClass Class { get; private set; }
    public CharacterStats Stats { get; private set; } = new();
    public ActivitySlots Activities { get; private set; } = new();
    public ResourcePool Resources { get; private set; } = new();
    public CooldownTracker Cooldowns { get; private set; } = new();
    
    public DateTime LastLogin { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Character() { } // For serialization

    public Character(Guid id, string name, CharacterClass characterClass)
    {
        Id = id;
        Name = name;
        Class = characterClass;
        Level = 1;
        Experience = BigNumber.Zero;
        CreatedAt = DateTime.UtcNow;
        LastLogin = DateTime.UtcNow;
        IsActive = true;
        
        InitializeStartingStats();
    }

    public Result StartActivity(ActivityType type, ActivityParameters parameters)
    {
        if (!Activities.CanStartActivity(type))
        {
            return Result.Failure($"Cannot start {type} activity - no available slots or conflicting activity");
        }

        var activity = ActivityFactory.Create(type, Id, parameters);
        Activities.AddActivity(activity);
        
        AddDomainEvent(new ActivityStartedEvent(Id, activity.Id, type));
        return Result.Success();
    }

    public Result CancelActivity(Guid activityId)
    {
        var activity = Activities.GetActivity(activityId);
        if (activity == null)
        {
            return Result.Failure("Activity not found");
        }

        if (!activity.CanInterrupt())
        {
            return Result.Failure("Activity cannot be interrupted");
        }

        Activities.RemoveActivity(activityId);
        AddDomainEvent(new ActivityCancelledEvent(Id, activityId));
        return Result.Success();
    }

    public void ApplyExperience(BigNumber amount)
    {
        var oldLevel = Level;
        Experience += amount;
        
        // Check for level up
        while (Experience >= GetExperienceForLevel(Level + 1))
        {
            Level++;
            Stats.OnLevelUp();
        }

        if (Level > oldLevel)
        {
            AddDomainEvent(new CharacterLevelUpEvent(Id, oldLevel, Level));
        }
    }

    public void UpdateStats(CharacterStats newStats)
    {
        Stats = newStats;
        AddDomainEvent(new CharacterStatsUpdatedEvent(Id, newStats));
    }

    public void UpdateLastLogin()
    {
        LastLogin = DateTime.UtcNow;
    }

    private void InitializeStartingStats()
    {
        Stats = Class switch
        {
            CharacterClass.Warrior => new CharacterStats(15, 8, 10, 12),
            CharacterClass.Mage => new CharacterStats(8, 15, 12, 10),
            CharacterClass.Archer => new CharacterStats(10, 10, 15, 12),
            CharacterClass.Rogue => new CharacterStats(12, 10, 13, 10),
            _ => new CharacterStats(10, 10, 10, 10)
        };
    }

    private BigNumber GetExperienceForLevel(int level)
    {
        // Exponential experience curve
        return new BigNumber(Math.Pow(level * 100, 1.5));
    }
}

/// <summary>
/// 聚合根基类
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// 角色职业枚举
/// </summary>
public enum CharacterClass
{
    Warrior,
    Mage,
    Archer,
    Rogue
}