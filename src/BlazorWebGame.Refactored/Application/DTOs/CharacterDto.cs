using BlazorWebGame.Refactored.Domain.ValueObjects;

namespace BlazorWebGame.Refactored.Application.DTOs;

public class CharacterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CharacterClass { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Experience { get; set; } = "0"; // BigNumber as string
    public CharacterStatsDto Stats { get; set; } = new();
    public ActivitySlotsDto Activities { get; set; } = new();
    public ResourcePoolDto Resources { get; set; } = new();
    public List<CooldownDto> Cooldowns { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
}

public class CharacterStatsDto
{
    public int Strength { get; set; }
    public int Agility { get; set; }
    public int Intelligence { get; set; }
    public int Vitality { get; set; }
    public int Luck { get; set; }
    
    public int Health { get; set; }
    public int Mana { get; set; }
    public int Stamina { get; set; }
    
    public int AttackPower { get; set; }
    public int Defense { get; set; }
    public int CriticalChance { get; set; }
    public int CriticalDamage { get; set; }
}

public class ActivitySlotsDto
{
    public List<ActivitySlotDto> Slots { get; set; } = new();
    public int MaxSlots { get; set; }
    public int UsedSlots { get; set; }
}

public class ActivitySlotDto
{
    public int SlotIndex { get; set; }
    public Guid? ActivityId { get; set; }
    public string? ActivityType { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? UnlockTime { get; set; }
}

public class ResourcePoolDto
{
    public Dictionary<string, string> Resources { get; set; } = new(); // BigNumber values as strings
}

public class CooldownDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsActive { get; set; }
}