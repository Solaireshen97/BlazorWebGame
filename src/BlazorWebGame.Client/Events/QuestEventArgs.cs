using BlazorWebGame.Models;

namespace BlazorWebGame.Events
{
    /// <summary>
    /// �����¼�����
    /// </summary>
    public class QuestEventArgs : GameEventArgs
    {
        /// <summary>
        /// ����ID
        /// </summary>
        public string? QuestId { get; }
        
        /// <summary>
        /// ����ʵ��
        /// </summary>
        public Quest? Quest { get; }
        
        /// <summary>
        /// ����ֵ���������
        /// </summary>
        public int? Progress { get; }
        
        /// <summary>
        /// �Ƿ����ճ�����
        /// </summary>
        public bool IsDaily { get; }

        public QuestEventArgs(
            GameEventType eventType, 
            Player? player = null, 
            string? questId = null, 
            Quest? quest = null,
            int? progress = null,
            bool isDaily = true) : base(eventType, player)
        {
            QuestId = questId;
            Quest = quest;
            Progress = progress;
            IsDaily = isDaily;
        }
    }
}