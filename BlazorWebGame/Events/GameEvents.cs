using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using System;

namespace BlazorWebGame.Events
{
    /// <summary>
    /// 游戏事件类型枚举
    /// </summary>
    public enum GameEventType
    {
        // 角色相关事件
        CharacterCreated,
        CharacterLevelUp,
        CharacterDeath,
        CharacterRevived,
        CharacterStatChanged,
        ActiveCharacterChanged,
        LevelUp,

        // 战斗相关事件
        CombatStarted,
        CombatEnded,
        EnemyDamaged,
        EnemyKilled,
        PlayerDamaged,
        SkillUsed,
        DungeonWaveStarted,
        BattleCompleted,
        BattleDefeated,
        DungeonCompleted,
        CombatStatusChanged,
        BattleCancelled,
        AttackMissed,

        // 物品相关事件
        ItemAcquired,
        ItemSold,
        ItemUsed,
        ItemEquipped,
        ItemUnequipped,
        GoldChanged,
        InventoryFull,
        
        // 队伍相关事件
        PartyCreated,
        PartyJoined,
        PartyLeft,
        PartyDisbanded,
        
        // 采集和制作相关事件
        GatheringStarted,
        GatheringCompleted,
        CraftingStarted,
        CraftingCompleted,
        ProfessionLevelUp,
        
        // 任务相关事件
        QuestAccepted,
        QuestUpdated,
        QuestCompleted,
        DailyQuestsRefreshed,
        WeeklyQuestsRefreshed,
        
        // 系统事件
        GameInitialized,
        GameStateLoaded,
        GameStateSaved,
        GameError,
        
        // 其他通用事件
        GenericStateChanged
    }
    
    /// <summary>
    /// 游戏事件参数基类
    /// </summary>
    public class GameEventArgs : EventArgs
    {
        /// <summary>
        /// 事件类型
        /// </summary>
        public GameEventType EventType { get; }
        
        /// <summary>
        /// 事件相关玩家，如果有的话
        /// </summary>
        public Player? Player { get; }
        
        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        
        /// <summary>
        /// 是否取消事件传播
        /// </summary>
        public bool IsCancelled { get; set; } = false;

        public GameEventArgs(GameEventType eventType, Player? player = null)
        {
            EventType = eventType;
            Player = player;
        }
    }
}