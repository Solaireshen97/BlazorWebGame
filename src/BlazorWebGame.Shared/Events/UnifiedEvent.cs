using System;
using System.Runtime.InteropServices;

namespace BlazorWebGame.Shared.Events
{
    /// <summary>
    /// 事件优先级枚举
    /// </summary>
    public enum EventPriority : byte
    {
        Gameplay = 0,    // 最高优先级：游戏逻辑、战斗、移动
        AI = 1,          // AI决策、技能使用
        Analytics = 2,   // 数据分析、统计
        Telemetry = 3    // 最低优先级：遥测、日志
    }

    /// <summary>
    /// 统一事件结构体 - 64字节对齐以优化缓存性能
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct UnifiedEvent
    {
        // 8 bytes
        public ulong Frame;
        
        // 8 bytes  
        public long TimestampNs;
        
        // 2 bytes
        public ushort EventType;
        
        // 1 byte
        public byte Priority;
        
        // 1 byte - 标志位：bit0=IsCancelled, bit1=IsProcessed, bit2-7=Reserved
        public byte Flags;
        
        // 8 bytes
        public ulong ActorId;
        
        // 8 bytes
        public ulong TargetId;
        
        // 28 bytes - 内联数据，避免指针和堆分配
        public unsafe fixed byte Data[28];

        public UnifiedEvent(ushort eventType, EventPriority priority = EventPriority.Gameplay)
        {
            Frame = 0;
            TimestampNs = DateTime.UtcNow.Ticks * 100; // Convert to nanoseconds
            EventType = eventType;
            Priority = (byte)priority;
            Flags = 0;
            ActorId = 0;
            TargetId = 0;
        }

        public bool IsCancelled 
        {
            get => (Flags & 0x01) != 0;
            set => Flags = (byte)(value ? (Flags | 0x01) : (Flags & ~0x01));
        }

        public bool IsProcessed
        {
            get => (Flags & 0x02) != 0;
            set => Flags = (byte)(value ? (Flags | 0x02) : (Flags & ~0x02));
        }

        /// <summary>
        /// 安全地写入数据到内联数据区域
        /// </summary>
        public unsafe void SetData<T>(T data) where T : unmanaged
        {
            if (sizeof(T) > 28)
                throw new ArgumentException($"Data size {sizeof(T)} exceeds maximum 28 bytes");
            
            fixed (byte* ptr = Data)
            {
                *(T*)ptr = data;
            }
        }

        /// <summary>
        /// 安全地从内联数据区域读取数据
        /// </summary>
        public unsafe T GetData<T>() where T : unmanaged
        {
            if (sizeof(T) > 28)
                throw new ArgumentException($"Data size {sizeof(T)} exceeds maximum 28 bytes");
            
            fixed (byte* ptr = Data)
            {
                return *(T*)ptr;
            }
        }
    }

    /// <summary>
    /// 游戏事件类型常量 - 使用ushort以节省空间
    /// </summary>
    public static class GameEventTypes
    {
        // 战斗相关事件 (0-99)
        public const ushort BATTLE_STARTED = 1;
        public const ushort BATTLE_ENDED = 2;
        public const ushort DAMAGE_DEALT = 3;
        public const ushort DAMAGE_TAKEN = 4;
        public const ushort SKILL_USED = 5;
        public const ushort ENEMY_KILLED = 6;
        public const ushort PLAYER_REVIVED = 7;
        public const ushort BATTLE_TICK = 8;

        // 角色相关事件 (100-199)
        public const ushort CHARACTER_CREATED = 100;
        public const ushort CHARACTER_LEVEL_UP = 101;
        public const ushort CHARACTER_STAT_CHANGED = 102;
        public const ushort EXPERIENCE_GAINED = 103;

        // 物品相关事件 (200-299)
        public const ushort ITEM_ACQUIRED = 200;
        public const ushort ITEM_SOLD = 201;
        public const ushort ITEM_EQUIPPED = 202;
        public const ushort GOLD_CHANGED = 203;

        // 队伍相关事件 (300-399)
        public const ushort PARTY_CREATED = 300;
        public const ushort PARTY_JOINED = 301;
        public const ushort PARTY_LEFT = 302;

        // 系统事件 (900-999)
        public const ushort SYSTEM_TICK = 900;
        public const ushort STATE_CHANGED = 901;
        public const ushort SYSTEM_ERROR = 902;
    }

    /// <summary>
    /// 内联数据结构示例 - 战斗伤害数据
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DamageEventData
    {
        public int Damage;           // 4 bytes
        public int ActualDamage;     // 4 bytes
        public ushort SkillId;       // 2 bytes
        public byte IsCritical;      // 1 byte
        public byte DamageType;      // 1 bytes  
        // Total: 12 bytes (fits in 28-byte limit)
    }

    /// <summary>
    /// 内联数据结构示例 - 经验获得数据
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ExperienceEventData
    {
        public int Amount;           // 4 bytes
        public ushort ProfessionId;  // 2 bytes
        public int NewLevel;         // 4 bytes
        public int TotalExperience;  // 4 bytes
        // Total: 14 bytes (fits in 28-byte limit)
    }
}