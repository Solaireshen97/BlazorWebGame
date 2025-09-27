using BlazorWebGame.Refactored.Domain.ValueObjects;

namespace BlazorWebGame.Refactored.Domain.Entities;

/// <summary>
/// 活动系统抽象基类
/// </summary>
public abstract class Activity : Entity
{
    public Guid Id { get; protected set; }
    public Guid CharacterId { get; protected set; }
    public ActivityType Type { get; protected set; }
    public ActivityState State { get; protected set; }
    public DateTime StartTimeUtc { get; protected set; }
    public DateTime? EndTimeUtc { get; protected set; }
    public int Priority { get; protected set; }
    public ActivityMetadata Metadata { get; protected set; } = new(new ActivityParameters());

    protected Activity() { } // For serialization

    protected Activity(Guid id, Guid characterId, ActivityType type, ActivityParameters parameters)
    {
        Id = id;
        CharacterId = characterId;
        Type = type;
        State = ActivityState.Active;
        StartTimeUtc = DateTime.UtcNow;
        Priority = CalculatePriority();
        Metadata = new ActivityMetadata(parameters);
    }

    public abstract double GetProgress(DateTime currentTimeUtc);
    public abstract bool CanInterrupt();
    public abstract ActivityResult Complete();
    public abstract void UpdateState(ActivityUpdateData data);

    protected virtual int CalculatePriority()
    {
        return Type switch
        {
            ActivityType.Battle => 1000,
            ActivityType.Boss => 1100,
            ActivityType.Crafting => 800,
            ActivityType.Gathering => 600,
            ActivityType.Quest => 700,
            ActivityType.Idle => 100,
            _ => 500
        };
    }

    public void SetEndTime(DateTime endTime)
    {
        EndTimeUtc = endTime;
    }

    public void UpdatePriority(DateTime serverNow)
    {
        var baseScore = CalculatePriority();

        // 即将完成的活动提高优先级
        if (EndTimeUtc.HasValue)
        {
            var remaining = (EndTimeUtc.Value - serverNow).TotalSeconds;
            if (remaining < 10)
                baseScore += 500;
            else if (remaining < 30)
                baseScore += 200;
        }

        Priority = baseScore;
    }
}

/// <summary>
/// 战斗活动
/// </summary>
public class BattleActivity : Activity
{
    public Guid EnemyId { get; private set; }
    public BattleConfiguration Configuration { get; private set; } = new();

    private BattleActivity() { } // For serialization

    public BattleActivity(Guid id, Guid characterId, Guid enemyId, ActivityParameters parameters)
        : base(id, characterId, ActivityType.Battle, parameters)
    {
        EnemyId = enemyId;
        Configuration = parameters.GetValue<BattleConfiguration>("Configuration") ?? new BattleConfiguration();
    }

    protected override object GetId() => Id;

    public override double GetProgress(DateTime currentTimeUtc)
    {
        if (!EndTimeUtc.HasValue) return 0.0;
        
        var totalDuration = (EndTimeUtc.Value - StartTimeUtc).TotalMilliseconds;
        var elapsed = (currentTimeUtc - StartTimeUtc).TotalMilliseconds;
        
        return Math.Clamp(elapsed / totalDuration, 0.0, 1.0);
    }

    public override bool CanInterrupt()
    {
        return State == ActivityState.Active && 
               (DateTime.UtcNow - StartTimeUtc).TotalSeconds > 5; // 5秒后可中断
    }

    public override ActivityResult Complete()
    {
        State = ActivityState.Completed;
        var result = new ActivityResult
        {
            Success = true,
            Experience = Configuration.BaseExperience,
            Items = Configuration.PossibleRewards.ToList(),
            Gold = Configuration.BaseGold
        };
        
        return result;
    }

    public override void UpdateState(ActivityUpdateData data)
    {
        // 更新战斗状态逻辑
        if (data.Properties.ContainsKey("Health"))
        {
            var health = data.Properties["Health"];
            Metadata.SetProperty("CurrentHealth", health);
        }
    }
}

/// <summary>
/// 采集活动
/// </summary>
public class GatheringActivity : Activity
{
    public GatheringType GatheringType { get; private set; }
    public ResourceNode TargetNode { get; private set; } = new();

    private GatheringActivity() { } // For serialization

    public GatheringActivity(Guid id, Guid characterId, GatheringType gatheringType, ActivityParameters parameters)
        : base(id, characterId, ActivityType.Gathering, parameters)
    {
        GatheringType = gatheringType;
        TargetNode = parameters.GetValue<ResourceNode>("TargetNode") ?? new ResourceNode();
        
        // 设置预计完成时间
        EndTimeUtc = StartTimeUtc.AddSeconds(TargetNode.HarvestTime);
    }

    protected override object GetId() => Id;

    public override double GetProgress(DateTime currentTimeUtc)
    {
        if (!EndTimeUtc.HasValue) return 0.0;
        
        var totalDuration = (EndTimeUtc.Value - StartTimeUtc).TotalMilliseconds;
        var elapsed = (currentTimeUtc - StartTimeUtc).TotalMilliseconds;
        
        return Math.Clamp(elapsed / totalDuration, 0.0, 1.0);
    }

    public override bool CanInterrupt()
    {
        return true; // 采集活动总是可以中断
    }

    public override ActivityResult Complete()
    {
        State = ActivityState.Completed;
        var result = new ActivityResult
        {
            Success = true,
            Experience = new BigNumber(TargetNode.ExperienceReward.ToLong()),
            Items = TargetNode.GenerateRewards().ToList(),
            Gold = BigNumber.Zero
        };
        
        return result;
    }

    public override void UpdateState(ActivityUpdateData data)
    {
        // 采集进度更新
        if (data.Properties.ContainsKey("Progress"))
        {
            var progress = data.Properties["Progress"];
            Metadata.SetProperty("Progress", progress);
        }
    }
}

/// <summary>
/// 制作活动
/// </summary>
public class CraftingActivity : Activity
{
    public Recipe Recipe { get; private set; } = new();
    public int Quantity { get; private set; }

    private CraftingActivity() { } // For serialization

    public CraftingActivity(Guid id, Guid characterId, Recipe recipe, int quantity, ActivityParameters parameters)
        : base(id, characterId, ActivityType.Crafting, parameters)
    {
        Recipe = recipe;
        Quantity = quantity;
        
        // 设置预计完成时间
        var totalTime = recipe.CraftTime * quantity;
        EndTimeUtc = StartTimeUtc.AddSeconds(totalTime);
    }

    protected override object GetId() => Id;

    public override double GetProgress(DateTime currentTimeUtc)
    {
        if (!EndTimeUtc.HasValue) return 0.0;
        
        var totalDuration = (EndTimeUtc.Value - StartTimeUtc).TotalMilliseconds;
        var elapsed = (currentTimeUtc - StartTimeUtc).TotalMilliseconds;
        
        return Math.Clamp(elapsed / totalDuration, 0.0, 1.0);
    }

    public override bool CanInterrupt()
    {
        var progress = GetProgress(DateTime.UtcNow);
        return progress < 0.8; // 80%以后不可中断
    }

    public override ActivityResult Complete()
    {
        State = ActivityState.Completed;
        var result = new ActivityResult
        {
            Success = true,
            Experience = Recipe.ExperienceReward * new BigNumber(Quantity),
            Items = Enumerable.Repeat(Recipe.OutputItem, Quantity).ToList(),
            Gold = BigNumber.Zero
        };
        
        return result;
    }

    public override void UpdateState(ActivityUpdateData data)
    {
        // 制作进度更新
        if (data.Properties.ContainsKey("Quality"))
        {
            var quality = data.Properties["Quality"];
            Metadata.SetProperty("Quality", quality);
        }
    }
}

/// <summary>
/// 实体基类
/// </summary>
public abstract class Entity
{
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        
        return GetId().Equals(other.GetId());
    }

    public override int GetHashCode()
    {
        return GetId().GetHashCode();
    }

    protected abstract object GetId();
}

/// <summary>
/// 活动工厂
/// </summary>
public static class ActivityFactory
{
    public static Activity Create(ActivityType type, Guid characterId, ActivityParameters parameters)
    {
        var id = Guid.NewGuid();
        
        return type switch
        {
            ActivityType.Battle => new BattleActivity(id, characterId, 
                parameters.GetValue<Guid>("EnemyId"), parameters),
            ActivityType.Gathering => new GatheringActivity(id, characterId, 
                parameters.GetValue<GatheringType>("GatheringType"), parameters),
            ActivityType.Crafting => new CraftingActivity(id, characterId, 
                parameters.GetValue<Recipe>("Recipe"), 
                parameters.GetValue<int>("Quantity"), parameters),
            _ => throw new ArgumentException($"Unknown activity type: {type}")
        };
    }
}