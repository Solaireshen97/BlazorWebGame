using BlazorWebGame.Models;

namespace BlazorWebGame.Events
{
    /// <summary>
    /// 任务事件参数
    /// </summary>
    public class QuestEventArgs : GameEventArgs
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public string? QuestId { get; }
        
        /// <summary>
        /// 任务实例
        /// </summary>
        public Quest? Quest { get; }
        
        /// <summary>
        /// 进度值，如果适用
        /// </summary>
        public int? Progress { get; }
        
        /// <summary>
        /// 是否是日常任务
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