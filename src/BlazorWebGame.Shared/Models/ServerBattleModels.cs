using System;
using System.Collections.Generic;
using System.Linq;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 服务端战斗参与者基类
/// </summary>
public abstract class ServerBattleParticipant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int BaseAttackPower { get; set; }
    public double AttacksPerSecond { get; set; } = 1.0;
    public double AttackCooldown { get; set; }
    
    // Battle stats
    public double CriticalChance { get; set; } = 0.05;
    public double CriticalMultiplier { get; set; } = 1.5;
    public double DodgeChance { get; set; } = 0.0;
    public int AccuracyRating { get; set; } = 0;
    
    // Skills
    public List<string> EquippedSkills { get; set; } = new();
    public Dictionary<string, double> SkillCooldowns { get; set; } = new();
    
    public abstract bool IsPlayer { get; }
    
    public virtual bool IsAlive 
    { 
        get => Health > 0; 
        set 
        { 
            // Allow setting IsAlive for battle management
            if (!value && Health > 0)
                Health = 0;
        } 
    }
    public double HealthPercentage => MaxHealth > 0 ? (double)Health / MaxHealth : 0;
}

/// <summary>
/// 服务端玩家战斗数据
/// </summary>
public class ServerBattlePlayer : ServerBattleParticipant
{
    public override bool IsPlayer => true;
    
    // Player-specific properties
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public string SelectedBattleProfession { get; set; } = "Warrior";
    public Dictionary<string, int> Attributes { get; set; } = new();
    public List<string> Inventory { get; set; } = new();
    
    // Additional attributes for combat calculations
    public int Strength { get; set; } = 10;
    public int Agility { get; set; } = 10;
    public int Intellect { get; set; } = 10;
    public int Spirit { get; set; } = 10;
    public int Stamina { get; set; } = 10;
    public int Mana { get; set; } = 100;
    public int MaxMana { get; set; } = 100;
    
    // Combat state
    public string CurrentAction { get; set; } = "Idle";
    public string? CurrentEnemyId { get; set; }
    public Guid? PartyId { get; set; }
}

/// <summary>
/// 服务端敌人战斗数据
/// </summary>
public class ServerBattleEnemy : ServerBattleParticipant
{
    public override bool IsPlayer => false;
    
    // Enemy-specific properties
    public int Level { get; set; } = 1;
    public int XpReward { get; set; } = 0;
    public int MinGoldReward { get; set; } = 0;
    public int MaxGoldReward { get; set; } = 0;
    public Dictionary<string, double> LootTable { get; set; } = new();
    public string EnemyType { get; set; } = "Normal";
    public string Race { get; set; } = "Humanoid";
}

/// <summary>
/// 服务端战斗上下文
/// </summary>
public class ServerBattleContext
{
    public Guid BattleId { get; set; } = Guid.NewGuid();
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    public string BattleType { get; set; } = "Normal";
    public string Status { get; set; } = "Active";
    
    public List<ServerBattlePlayer> Players { get; set; } = new();
    public List<ServerBattleEnemy> Enemies { get; set; } = new();
    
    // Battle state
    public Dictionary<string, string> PlayerTargets { get; set; } = new(); // playerId -> enemyId
    public List<ServerBattleAction> ActionHistory { get; set; } = new();
    
    // Party info (if applicable)
    public Guid? PartyId { get; set; }
    
    // Dungeon-specific properties
    public string? DungeonId { get; set; }
    public int WaveNumber { get; set; } = 0;
    public bool AllowAutoRevive { get; set; } = false;
    
    // Battle result
    public bool IsVictory { get; set; } = false;
    
    // State enum property  
    public ServerBattleState State { get; set; } = ServerBattleState.Active;
    
    public bool IsActive => State == ServerBattleState.Active;
    public bool HasActiveParticipants => 
        Players.Any(p => p.IsAlive) && Enemies.Any(e => e.IsAlive);
}

/// <summary>
/// 服务端战斗动作记录
/// </summary>
public class ServerBattleAction
{
    public string ActorId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string ActionType { get; set; } = "Attack";
    public int Damage { get; set; }
    public string? SkillId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsCritical { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// 服务端战斗刷新状态
/// </summary>
public class ServerBattleRefreshState
{
    public ServerBattleContext OriginalBattle { get; set; } = new();
    public double RemainingCooldown { get; set; }
    public string BattleType { get; set; } = "Normal";
    public List<ServerEnemyInfo> EnemyInfos { get; set; } = new();
    public string? DungeonId { get; set; }
}

/// <summary>
/// 服务端副本波次刷新状态
/// </summary>
public class ServerDungeonWaveRefreshState
{
    public Guid DungeonId { get; set; }
    public int CurrentWave { get; set; }
    public double RemainingCooldown { get; set; }
    public List<ServerBattlePlayer> Players { get; set; } = new();
}

/// <summary>
/// 服务端敌人信息记录
/// </summary>
public class ServerEnemyInfo
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Level { get; set; } = 1;
    public string EnemyType { get; set; } = "Normal";
}

/// <summary>
/// 服务端战斗状态枚举
/// </summary>
public enum ServerBattleState
{
    Preparing,
    Active,
    Paused,
    Completed,
    Cancelled
}
