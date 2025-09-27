using System.Numerics;
using BlazorWebGame.Refactored.Domain.Entities;

namespace BlazorWebGame.Refactored.Domain.ValueObjects;

/// <summary>
/// 大数值类型 - 支持无限增长的数值
/// </summary>
public readonly struct BigNumber : IComparable<BigNumber>, IEquatable<BigNumber>
{
    private readonly BigInteger _value;
    
    public static BigNumber Zero => new(0);
    public static BigNumber One => new(1);

    public BigNumber(long value) => _value = new BigInteger(value);
    public BigNumber(double value) => _value = new BigInteger(value);
    public BigNumber(BigInteger value) => _value = value;
    
    public long ToLong() => (long)_value;

    public static BigNumber operator +(BigNumber left, BigNumber right) => new(left._value + right._value);
    public static BigNumber operator -(BigNumber left, BigNumber right) => new(left._value - right._value);
    public static BigNumber operator *(BigNumber left, BigNumber right) => new(left._value * right._value);
    public static BigNumber operator /(BigNumber left, BigNumber right) => new(left._value / right._value);
    
    public static bool operator >(BigNumber left, BigNumber right) => left._value > right._value;
    public static bool operator <(BigNumber left, BigNumber right) => left._value < right._value;
    public static bool operator >=(BigNumber left, BigNumber right) => left._value >= right._value;
    public static bool operator <=(BigNumber left, BigNumber right) => left._value <= right._value;
    public static bool operator ==(BigNumber left, BigNumber right) => left._value == right._value;
    public static bool operator !=(BigNumber left, BigNumber right) => left._value != right._value;

    public int CompareTo(BigNumber other) => _value.CompareTo(other._value);
    public bool Equals(BigNumber other) => _value.Equals(other._value);
    public override bool Equals(object? obj) => obj is BigNumber other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString()
    {
        if (_value < 1000) return _value.ToString();
        if (_value < 1000000) return $"{(double)_value / 1000.0:F1}K";
        if (_value < 1000000000) return $"{(double)_value / 1000000.0:F1}M";
        if (_value < new BigInteger(1000000000000)) return $"{(double)_value / 1000000000.0:F1}B";
        return $"{(double)_value / 1000000000000.0:F1}T";
    }
}

/// <summary>
/// 角色属性统计
/// </summary>
public record CharacterStats
{
    public int Strength { get; init; }
    public int Intelligence { get; init; }
    public int Agility { get; init; }
    public int Vitality { get; init; }
    
    public int AttackPower => Strength * 2 + Agility;
    public int MagicPower => Intelligence * 2;
    public int MaxHealth => Vitality * 10 + 100;
    public int MaxMana => Intelligence * 5 + 50;
    public double CriticalChance => Agility * 0.1;
    public double AttackSpeed => 1.0 + (Agility * 0.01);

    public CharacterStats() : this(10, 10, 10, 10) { }
    
    public CharacterStats(int strength, int intelligence, int agility, int vitality)
    {
        Strength = Math.Max(1, strength);
        Intelligence = Math.Max(1, intelligence);
        Agility = Math.Max(1, agility);
        Vitality = Math.Max(1, vitality);
    }

    public CharacterStats OnLevelUp()
    {
        return this with 
        { 
            Strength = Strength + 2,
            Intelligence = Intelligence + 2,
            Agility = Agility + 2,
            Vitality = Vitality + 3
        };
    }
}

/// <summary>
/// 活动槽位管理
/// </summary>
public class ActivitySlots
{
    private readonly Dictionary<ActivityType, Activity?> _slots = new();
    private readonly int _maxSlots;

    public ActivitySlots(int maxSlots = 3)
    {
        _maxSlots = maxSlots;
        InitializeSlots();
    }

    public IReadOnlyDictionary<ActivityType, Activity?> Slots => _slots.AsReadOnly();
    public int AvailableSlots => _slots.Values.Count(s => s == null);
    public int UsedSlots => _slots.Values.Count(s => s != null);

    public bool CanStartActivity(ActivityType type)
    {
        // 某些活动类型可以替换现有活动
        if (type == ActivityType.Battle && _slots.ContainsKey(ActivityType.Idle))
        {
            return true;
        }

        return AvailableSlots > 0;
    }

    public bool HasAvailableSlot()
    {
        return AvailableSlots > 0;
    }

    public void AddActivity(Activity activity)
    {
        if (!CanStartActivity(activity.Type))
            throw new InvalidOperationException("No available slots for activity");

        // 如果是高优先级活动，替换低优先级活动
        if (activity.Type == ActivityType.Battle)
        {
            var idleSlot = _slots.FirstOrDefault(s => s.Value?.Type == ActivityType.Idle);
            if (idleSlot.Key != default)
            {
                _slots[idleSlot.Key] = activity;
                return;
            }
        }

        var availableSlot = _slots.First(s => s.Value == null);
        _slots[availableSlot.Key] = activity;
    }

    public void RemoveActivity(Guid activityId)
    {
        var slot = _slots.FirstOrDefault(s => s.Value?.Id == activityId);
        if (slot.Key != default)
        {
            _slots[slot.Key] = null;
        }
    }

    public Activity? GetActivity(Guid activityId)
    {
        return _slots.Values.FirstOrDefault(a => a?.Id == activityId);
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < _maxSlots; i++)
        {
            _slots.Add((ActivityType)i, null);
        }
    }
}

/// <summary>
/// 资源池
/// </summary>
public record ResourcePool
{
    public BigNumber Gold { get; init; } = BigNumber.Zero;
    public Dictionary<string, int> Materials { get; init; } = new();
    
    public ResourcePool AddGold(BigNumber amount)
    {
        return this with { Gold = Gold + amount };
    }
    
    public ResourcePool AddMaterial(string materialId, int quantity)
    {
        var materials = new Dictionary<string, int>(Materials);
        materials[materialId] = materials.GetValueOrDefault(materialId, 0) + quantity;
        return this with { Materials = materials };
    }
    
    public bool HasEnoughGold(BigNumber amount) => Gold >= amount;
    public bool HasEnoughMaterial(string materialId, int quantity) => 
        Materials.GetValueOrDefault(materialId, 0) >= quantity;
}

/// <summary>
/// 冷却时间追踪器
/// </summary>
public class CooldownTracker
{
    private readonly Dictionary<string, DateTime> _cooldowns = new();

    public bool IsOnCooldown(string key) => 
        _cooldowns.ContainsKey(key) && _cooldowns[key] > DateTime.UtcNow;

    public TimeSpan GetRemainingCooldown(string key)
    {
        if (!_cooldowns.ContainsKey(key) || _cooldowns[key] <= DateTime.UtcNow)
            return TimeSpan.Zero;
        
        return _cooldowns[key] - DateTime.UtcNow;
    }

    public void SetCooldown(string key, TimeSpan duration)
    {
        _cooldowns[key] = DateTime.UtcNow.Add(duration);
    }

    public void ClearCooldown(string key)
    {
        _cooldowns.Remove(key);
    }
}

/// <summary>
/// 活动元数据
/// </summary>
public class ActivityMetadata
{
    private readonly Dictionary<string, object> _properties = new();

    public ActivityMetadata(ActivityParameters parameters)
    {
        foreach (var param in parameters.Parameters)
        {
            _properties[param.Key] = param.Value;
        }
    }

    public T? GetProperty<T>(string key)
    {
        if (_properties.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return default;
    }

    public void SetProperty<T>(string key, T value)
    {
        if (value != null)
            _properties[key] = value;
    }
}

/// <summary>
/// 活动参数
/// </summary>
public class ActivityParameters
{
    public Dictionary<string, object> Parameters { get; } = new();

    public ActivityParameters() { }

    public ActivityParameters(Dictionary<string, object> parameters)
    {
        Parameters = parameters ?? new Dictionary<string, object>();
    }

    public T? GetValue<T>(string key)
    {
        if (Parameters.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return default;
    }

    public void SetValue<T>(string key, T value)
    {
        if (value != null)
            Parameters[key] = value;
    }
}

/// <summary>
/// 活动结果
/// </summary>
public record ActivityResult
{
    public bool Success { get; init; }
    public BigNumber Experience { get; init; } = BigNumber.Zero;
    public List<ItemReward> Items { get; init; } = new();
    public BigNumber Gold { get; init; } = BigNumber.Zero;
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 物品奖励
/// </summary>
public record ItemReward(string ItemId, int Quantity, ItemRarity Rarity = ItemRarity.Common);

/// <summary>
/// 活动更新数据
/// </summary>
public record ActivityUpdateData(Dictionary<string, object> Properties);

/// <summary>
/// 战斗配置
/// </summary>
public record BattleConfiguration
{
    public BigNumber BaseExperience { get; init; } = new BigNumber(100);
    public BigNumber BaseGold { get; init; } = new BigNumber(50);
    public List<ItemReward> PossibleRewards { get; init; } = new();
    public TimeSpan EstimatedDuration { get; init; } = TimeSpan.FromMinutes(2);
}

/// <summary>
/// 资源节点
/// </summary>
public record ResourceNode
{
    public string NodeId { get; init; } = string.Empty;
    public GatheringType Type { get; init; }
    public int HarvestTime { get; init; } = 30; // seconds
    public BigNumber ExperienceReward { get; init; } = new BigNumber(25);
    public List<ItemReward> PossibleRewards { get; init; } = new();

    public IEnumerable<ItemReward> GenerateRewards()
    {
        var random = new Random();
        return PossibleRewards.Where(r => random.NextDouble() < 0.5); // 50% chance for each reward
    }
}

/// <summary>
/// 制作配方
/// </summary>
public record Recipe
{
    public string RecipeId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<MaterialRequirement> RequiredMaterials { get; init; } = new();
    public ItemReward OutputItem { get; init; } = new("", 1);
    public int CraftTime { get; init; } = 60; // seconds
    public BigNumber ExperienceReward { get; init; } = new BigNumber(50);
    public int RequiredLevel { get; init; } = 1;
}

/// <summary>
/// 材料需求
/// </summary>
public record MaterialRequirement(string MaterialId, int Quantity);

/// <summary>
/// 操作结果
/// </summary>
public record Result
{
    public bool IsSuccess { get; init; }
    public string Error { get; init; } = string.Empty;
    public string ErrorMessage => Error; // Alias for compatibility

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}

/// <summary>
/// 枚举定义
/// </summary>
public enum ActivityType
{
    Idle = 0,
    Battle = 1,
    Gathering = 2,
    Crafting = 3,
    Quest = 4,
    Boss = 5,
    Training = 6
}

public enum ActivityState
{
    Active,
    Paused,
    Completed,
    Cancelled,
    Failed
}

public enum GatheringType
{
    Mining,
    Herbalism,
    Fishing,
    Logging
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}