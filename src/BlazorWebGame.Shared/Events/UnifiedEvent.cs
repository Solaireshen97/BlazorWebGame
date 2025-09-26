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
        public const ushort BATTLE_ATTACK = 9;
        public const ushort BATTLE_HEAL = 10;
        public const ushort BATTLE_BUFF_APPLIED = 11;
        public const ushort BATTLE_DEBUFF_APPLIED = 12;

        // 角色相关事件 (100-199)
        public const ushort CHARACTER_CREATED = 100;
        public const ushort CHARACTER_LEVEL_UP = 101;
        public const ushort CHARACTER_STAT_CHANGED = 102;
        public const ushort EXPERIENCE_GAINED = 103;
        public const ushort PLAYER_ACTION_STARTED = 104;
        public const ushort PLAYER_ACTION_COMPLETED = 105;
        public const ushort PLAYER_LOCATION_CHANGED = 106;

        // 物品相关事件 (200-299)
        public const ushort ITEM_ACQUIRED = 200;
        public const ushort ITEM_SOLD = 201;
        public const ushort ITEM_EQUIPPED = 202;
        public const ushort GOLD_CHANGED = 203;

        // 队伍相关事件 (300-399)
        public const ushort PARTY_CREATED = 300;
        public const ushort PARTY_JOINED = 301;
        public const ushort PARTY_LEFT = 302;

        // 采集和生产相关事件 (400-499)
        public const ushort GATHERING_STARTED = 400;
        public const ushort GATHERING_PROGRESS = 401;
        public const ushort GATHERING_COMPLETED = 402;
        public const ushort GATHERING_CANCELLED = 403;
        public const ushort CRAFTING_STARTED = 410;
        public const ushort CRAFTING_PROGRESS = 411;
        public const ushort CRAFTING_COMPLETED = 412;
        public const ushort CRAFTING_CANCELLED = 413;
        public const ushort PROFESSION_LEVEL_UP = 420;
        public const ushort PROFESSION_XP_GAINED = 421;

        // 任务相关事件 (500-599)
        public const ushort QUEST_ACCEPTED = 500;
        public const ushort QUEST_PROGRESS = 501;
        public const ushort QUEST_COMPLETED = 502;
        public const ushort QUEST_CANCELLED = 503;
        public const ushort DAILY_QUEST_REFRESH = 510;
        public const ushort WEEKLY_QUEST_REFRESH = 511;

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

    /// <summary>
    /// 采集事件数据
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GatheringEventData
    {
        public ushort NodeId;        // 2 bytes - 节点ID的哈希值
        public ushort ItemId;        // 2 bytes - 物品ID的哈希值
        public byte Quantity;        // 1 byte  - 数量
        public byte ExtraLoot;       // 1 byte  - 是否额外掉落
        public float Progress;       // 4 bytes - 进度(0.0-1.0)
        public int XpGained;         // 4 bytes - 获得的经验
        public ushort ProfessionType; // 2 bytes - 职业类型
        // Total: 16 bytes (fits in 28-byte limit)
    }

    /// <summary>
    /// 生产制作事件数据
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CraftingEventData
    {
        public ushort RecipeId;      // 2 bytes - 配方ID的哈希值
        public ushort ResultItemId;  // 2 bytes - 结果物品ID的哈希值
        public byte Quantity;        // 1 byte  - 数量
        public byte QualityBonus;    // 1 byte  - 品质加成
        public float Progress;       // 4 bytes - 进度(0.0-1.0)
        public int XpGained;         // 4 bytes - 获得的经验
        public ushort ProfessionType; // 2 bytes - 职业类型
        public int MaterialCost;     // 4 bytes - 材料成本
        // Total: 20 bytes (fits in 28-byte limit)
    }

    /// <summary>
    /// 战斗攻击事件数据
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BattleAttackEventData
    {
        public int BaseDamage;       // 4 bytes - 基础伤害
        public int ActualDamage;     // 4 bytes - 实际伤害
        public ushort SkillId;       // 2 bytes - 技能ID
        public byte IsCritical;      // 1 byte  - 是否暴击
        public byte AttackType;      // 1 byte  - 攻击类型
        public float CritMultiplier; // 4 bytes - 暴击倍数
        public int RemainingHealth;  // 4 bytes - 剩余血量
        public ushort StatusEffect;  // 2 bytes - 状态效果
        // Total: 22 bytes (fits in 28-byte limit)
    }
}