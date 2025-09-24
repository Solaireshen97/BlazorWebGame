using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using System;

namespace BlazorWebGame.Events
{
    /// <summary>
    /// ��Ϸ�¼�����ö��
    /// </summary>
    public enum GameEventType
    {
        // ��ɫ����¼�
        CharacterCreated,
        CharacterLevelUp,
        CharacterDeath,
        CharacterRevived,
        CharacterStatChanged,
        ActiveCharacterChanged,
        LevelUp,

        // ս������¼�
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

        // ��Ʒ����¼�
        ItemAcquired,
        ItemSold,
        ItemUsed,
        ItemEquipped,
        ItemUnequipped,
        GoldChanged,
        InventoryFull,
        
        // ��������¼�
        PartyCreated,
        PartyJoined,
        PartyLeft,
        PartyDisbanded,
        
        // �ɼ�����������¼�
        GatheringStarted,
        GatheringCompleted,
        CraftingStarted,
        CraftingCompleted,
        ProfessionLevelUp,
        
        // ��������¼�
        QuestAccepted,
        QuestUpdated,
        QuestCompleted,
        DailyQuestsRefreshed,
        WeeklyQuestsRefreshed,
        
        // ϵͳ�¼�
        GameInitialized,
        GameStateLoaded,
        GameStateSaved,
        GameError,
        
        // ����ͨ���¼�
        GenericStateChanged
    }
    
    /// <summary>
    /// ��Ϸ�¼���������
    /// </summary>
    public class GameEventArgs : EventArgs
    {
        /// <summary>
        /// �¼�����
        /// </summary>
        public GameEventType EventType { get; }
        
        /// <summary>
        /// �¼������ң�����еĻ�
        /// </summary>
        public Player? Player { get; }
        
        /// <summary>
        /// �¼�ʱ���
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        
        /// <summary>
        /// �Ƿ�ȡ���¼�����
        /// </summary>
        public bool IsCancelled { get; set; } = false;

        public GameEventArgs(GameEventType eventType, Player? player = null)
        {
            EventType = eventType;
            Player = player;
        }
    }
}