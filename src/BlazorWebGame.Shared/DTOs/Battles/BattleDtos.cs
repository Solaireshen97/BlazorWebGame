using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.DTOs.Battles
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

    // 在文件末尾添加以下DTO类

    /// <summary>
    /// 战斗DTO - 用于创建和开始战斗时返回
    /// </summary>
    public class BattleDto
    {
        public string Id { get; set; } = string.Empty;
        public string BattleType { get; set; } = string.Empty;
        public List<CombatantDto> Players { get; set; } = new();
        public List<CombatantDto> Enemies { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CurrentRound { get; set; }
        public string? CurrentTurnEntityId { get; set; }
    }

    /// <summary>
    /// 战斗状态DTO - 用于获取战斗状态
    /// </summary>
    public class BattleStatusDto
    {
        public string State { get; set; } = string.Empty;
        public int CurrentRound { get; set; }
        public string CurrentTurnEntityId { get; set; } = string.Empty;
        public List<BattleEntityStatusDto> Entities { get; set; } = new();
        public bool IsEnded { get; set; }
        public string? WinnerSide { get; set; }
    }

    /// <summary>
    /// 战斗实体状态DTO - 用于表示战场上的实体状态
    /// </summary>
    public class BattleEntityStatusDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public bool IsAlive { get; set; }
        public List<ActiveBuffDto> Buffs { get; set; } = new();
        public string Side { get; set; } = string.Empty; // Player/Enemy
    }

    /// <summary>
    /// 战斗行动结果DTO - 用于技能使用结果
    /// </summary>
    public class BattleActionResultDto
    {
        public string SkillName { get; set; } = string.Empty;
        public List<BattleEffectDto> Effects { get; set; } = new();
        public bool BattleEnded { get; set; }
        public string? BattleResult { get; set; }
    }

    /// <summary>
    /// 战斗效果DTO - 用于表示技能效果
    /// </summary>
    public class BattleEffectDto
    {
        public string Type { get; set; } = string.Empty; // Damage/Healing/Buff/Debuff
        public string Target { get; set; } = string.Empty;
        public double Value { get; set; }
        public bool IsCritical { get; set; }
        public string? BuffId { get; set; }
        public double? Duration { get; set; }
    }

    /// <summary>
    /// 创建战斗请求
    /// </summary>
    public class CreateBattleRequest
    {
        public string CharacterId { get; set; } = string.Empty;
        public string EnemyId { get; set; } = string.Empty;
        public string? BattleType { get; set; }
        public string? RegionId { get; set; }
    }

    /// <summary>
    /// 使用技能请求
    /// </summary>
    public class UseSkillRequest
    {
        public string CasterId { get; set; } = string.Empty;
        public string SkillId { get; set; } = string.Empty;
        public string? TargetId { get; set; }
    }
}