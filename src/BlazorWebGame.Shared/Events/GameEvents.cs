using System;

namespace BlazorWebGame.Shared.Events
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
        ExperienceGained,
        CharacterDeleted,          // 角色删除事件
        OfflineProgressCalculated, // 离线进度计算事件

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

        // 采集和生产相关事件
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

        // 通用通知事件
        GenericStateChanged,
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
        /// 事件相关角色，如果有的话
        /// </summary>
        public string? PlayerId { get; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        /// <summary>
        /// 是否取消事件处理
        /// </summary>
        public bool IsCancelled { get; set; } = false;

        /// <summary>
        /// 事件数据
        /// </summary>
        public object? Data { get; set; }

        public GameEventArgs(GameEventType eventType, string? playerId = null, object? data = null)
        {
            EventType = eventType;
            PlayerId = playerId;
            Data = data;
        }
    }
}