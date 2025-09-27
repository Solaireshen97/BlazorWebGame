using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorIdleGame.Client.Models
{
    public class BattleState
    {
        public string BattleId { get; set; } = "";
        public BattleType Type { get; set; }
        public List<BattleParticipant> PlayerTeam { get; set; } = new();
        public List<BattleParticipant> EnemyTeam { get; set; } = new();
        public int Round { get; set; }
        public BattleStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<BattleLog> Logs { get; set; } = new();
        public BattleRewards? Rewards { get; set; }
        
        public bool IsPartyBattle => PlayerTeam.Count > 1;
        public bool IsActive => Status == BattleStatus.InProgress;
    }
    
    public class BattleParticipant
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public int AttackPower { get; set; }
        public int Defense { get; set; }
        public double AttackSpeed { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsAlive => CurrentHealth > 0;
        public double NextAttackTime { get; set; }
        
        public double HealthPercent => MaxHealth > 0 
            ? (double)CurrentHealth / MaxHealth * 100 
            : 0;
    }
    
    public enum BattleType
    {
        Solo,           // 单人战斗
        Party,          // 组队战斗
        Raid,           // 团队副本
        PvP            // PvP对战
    }
    
    public enum BattleStatus
    {
        Preparing,      // 准备中
        InProgress,     // 进行中
        Victory,        // 胜利
        Defeat,         // 失败
        Abandoned       // 放弃
    }
    
    public class BattleLog
    {
        public DateTime Timestamp { get; set; }
        public string AttackerId { get; set; } = "";
        public string AttackerName { get; set; } = "";
        public string TargetId { get; set; } = "";
        public string TargetName { get; set; } = "";
        public int Damage { get; set; }
        public BattleActionType ActionType { get; set; }
        public bool IsCritical { get; set; }
    }
    
    public enum BattleActionType
    {
        Attack,         // 普通攻击
        Skill,          // 技能
        Heal,           // 治疗
        Buff,           // 增益
        Debuff         // 减益
    }
    
    public class BattleRewards
    {
        public long Experience { get; set; }
        public long Gold { get; set; }
        public Dictionary<string, int> Items { get; set; } = new();
        public bool IsPartyReward { get; set; }
        public Dictionary<string, long>? MemberRewards { get; set; }
    }
}