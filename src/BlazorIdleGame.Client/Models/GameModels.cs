using System;
using System.Collections.Generic;

namespace BlazorIdleGame.Client.Models
{
    // ========== 基础游戏模型 ==========
    public class GameState
    {
        public PlayerInfo Player { get; set; } = new();
        public Resources Resources { get; set; } = new();
        public List<Activity> Activities { get; set; } = new();
        public PartyInfo? CurrentParty { get; set; }
        public BattleState? CurrentBattle { get; set; }
        public DateTime ServerTime { get; set; }
        public int Version { get; set; }
        public DateTime LastSyncTime { get; set; }

    }
    
    public class PlayerInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Level { get; set; } = 1;
        public long Experience { get; set; }
        public long ExperienceToNext { get; set; } = 100;
        public CharacterStats Stats { get; set; } = new();
        public bool InParty => !string.IsNullOrEmpty(PartyId);
        public string? PartyId { get; set; }
        
        public double ExperiencePercent => ExperienceToNext > 0 
            ? (double)Experience / ExperienceToNext * 100 
            : 0;
    }
    
    public class CharacterStats
    {
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int AttackPower { get; set; } = 10;
        public int Defense { get; set; } = 5;
        public double AttackSpeed { get; set; } = 1.0;
    }
    
    public class Resources
    {
        public long Gold { get; set; }
        public long Wood { get; set; }
        public long Stone { get; set; }
        public long Iron { get; set; }
        public Dictionary<string, long> Others { get; set; } = new();
    }
    
    public class Activity
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public ActivityType Type { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int CurrentLoop { get; set; }
        public int MaxLoops { get; set; }
        public Dictionary<string, long> RewardsPerLoop { get; set; } = new();
        public bool IsPartyActivity { get; set; }
        
        public double Progress
        {
            get
            {
                var now = DateTime.UtcNow;
                if (now >= EndTime) return 1.0;
                if (now <= StartTime) return 0.0;
                
                var total = (EndTime - StartTime).TotalSeconds;
                var elapsed = (now - StartTime).TotalSeconds;
                return Math.Min(elapsed / total, 1.0);
            }
        }
        
        public TimeSpan TimeRemaining => EndTime > DateTime.UtcNow 
            ? EndTime - DateTime.UtcNow 
            : TimeSpan.Zero;
    }
    
    public enum ActivityType
    {
        Battle,
        Gathering,
        Crafting,
        Training,
        PartyBattle,    // 组队战斗
        PartyRaid       // 团队副本
    }
    
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();
    }
    
    public class GameAction
    {
        public string ActionType { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime ClientTime { get; set; } = DateTime.UtcNow;
    }
}