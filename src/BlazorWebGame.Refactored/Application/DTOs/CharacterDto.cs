using BlazorWebGame.Refactored.Domain.ValueObjects;
using BlazorWebGame.Refactored.Domain.Entities;

namespace BlazorWebGame.Refactored.Application.DTOs;

public class CharacterDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CharacterClass CharacterClass { get; set; }
    public int Level { get; set; }
    public long Experience { get; set; } // BigNumber as long for simplicity
    public CharacterStatsDto Stats { get; set; } = new();
    public long Gold { get; set; } // BigNumber as long
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
    public ActivitySlotsDto Activities { get; set; } = new();
    public ResourcePoolDto Resources { get; set; } = new();
    public List<CooldownDto> Cooldowns { get; set; } = new();
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
    public int MagicPower { get; set; }
    public int MaxHealth { get; set; }
    public int MaxMana { get; set; }
    public double CriticalChance { get; set; }
    public double AttackSpeed { get; set; }
    public int Defense { get; set; }
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