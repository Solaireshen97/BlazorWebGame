using System;
using System.Collections.Generic;

namespace BlazorIdleGame.Client.Models
{
    public class PartyInfo
    {
        public string Version { get; set; } = "";

        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string LeaderId { get; set; } = "";
        public List<PartyMember> Members { get; set; } = new();
        public int MaxMembers { get; set; } = 5;
        public PartyStatus Status { get; set; }
        public string? CurrentActivityId { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public bool IsFull => Members.Count >= MaxMembers;
        public bool IsInActivity => !string.IsNullOrEmpty(CurrentActivityId);
    }
    
    public class PartyMember
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public CharacterStats Stats { get; set; } = new();
        public bool IsLeader { get; set; }
        public bool IsReady { get; set; }
        public MemberStatus Status { get; set; }
        public DateTime JoinedAt { get; set; }
    }
    
    public enum PartyStatus
    {
        Idle,           // 空闲
        Preparing,      // 准备中
        InActivity,     // 活动中
        Disbanding      // 解散中
    }
    
    public enum MemberStatus
    {
        Online,         // 在线
        InBattle,       // 战斗中
        Idle,          // 空闲
        Offline        // 离线
    }
    
    public class PartyInvite
    {
        public string InviteId { get; set; } = "";
        public string PartyId { get; set; } = "";
        public string PartyName { get; set; } = "";
        public string InviterName { get; set; } = "";
        public DateTime ExpireTime { get; set; }
    }
    
    public class PartyRequest
    {
        public string RequestId { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public string PlayerName { get; set; } = "";
        public int PlayerLevel { get; set; }
        public DateTime RequestTime { get; set; }
    }
}