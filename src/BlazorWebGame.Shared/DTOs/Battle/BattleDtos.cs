using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.DTOs.Battle
{
    /// <summary>
    /// 战斗状态DTO
    /// </summary>
    public class BattleStateDto
    {
        public Guid BattleId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int Round { get; set; }
        public List<CombatantDto> Combatants { get; set; } = new();
        public BattleResourcesDto Resources { get; set; } = new();
        public List<ActiveBuffDto> ActiveBuffs { get; set; } = new();
        public CombatSegmentDto? LatestSegment { get; set; }
    }

    /// <summary>
    /// 战斗参与者DTO
    /// </summary>
    public class CombatantDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Player/Monster/NPC
        public int Level { get; set; }
        public VitalsDto Vitals { get; set; } = new();
        public bool IsAlive { get; set; } = true;
        public string? TargetId { get; set; }
    }

    /// <summary>
    /// 生命值DTO
    /// </summary>
    public class VitalsDto
    {
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Mana { get; set; }
        public int MaxMana { get; set; }
        public double HealthPercentage => MaxHealth > 0 ? (double)Health / MaxHealth * 100 : 0;
        public double ManaPercentage => MaxMana > 0 ? (double)Mana / MaxMana * 100 : 0;
    }

    /// <summary>
    /// 战斗资源DTO
    /// </summary>
    public class BattleResourcesDto
    {
        public Dictionary<string, ResourceBucketDto> Buckets { get; set; } = new();
    }

    /// <summary>
    /// 资源桶DTO
    /// </summary>
    public class ResourceBucketDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Current { get; set; }
        public double Max { get; set; }
        public double FillRate { get; set; }
        public string OverflowPolicy { get; set; } = "Waste";
    }

    /// <summary>
    /// 激活的Buff DTO
    /// </summary>
    public class ActiveBuffDto
    {
        public string BuffId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public int Stacks { get; set; } = 1;
        public DateTime AppliedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public TimeSpan? RemainingDuration => ExpiresAt?.Subtract(DateTime.UtcNow);
    }

    /// <summary>
    /// 战斗片段DTO
    /// </summary>
    public class CombatSegmentDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<CombatEventDto> Events { get; set; } = new();
        public Dictionary<string, double> DamageBySource { get; set; } = new();
        public Dictionary<string, double> HealingBySource { get; set; } = new();
        public int TotalActions { get; set; }
    }

    /// <summary>
    /// 战斗事件DTO
    /// </summary>
    public class CombatEventDto
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = string.Empty; // Attack/Skill/Buff/Death
        public string SourceId { get; set; } = string.Empty;
        public string? TargetId { get; set; }
        public double? Value { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}