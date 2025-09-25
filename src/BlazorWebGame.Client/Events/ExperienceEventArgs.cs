using BlazorWebGame.Models;

namespace BlazorWebGame.Events
{
    /// <summary>
    /// 经验值变化事件参数
    /// </summary>
    public class ExperienceEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获得的经验值数量
        /// </summary>
        public int ExperienceAmount { get; }
        
        /// <summary>
        /// 相关职业类型
        /// </summary>
        public object ProfessionType { get; }
        
        /// <summary>
        /// 经验值获得后的总经验值
        /// </summary>
        public long TotalExperience { get; }
        
        /// <summary>
        /// 是否触发了升级
        /// </summary>
        public bool LeveledUp { get; }
        
        /// <summary>
        /// 当前等级
        /// </summary>
        public int CurrentLevel { get; }

        public ExperienceEventArgs(
            GameEventType eventType, 
            Player? player,
            object professionType, 
            int expAmount, 
            long totalExp, 
            bool leveledUp,
            int currentLevel) 
            : base(eventType, player)
        {
            ExperienceAmount = expAmount;
            ProfessionType = professionType;
            TotalExperience = totalExp;
            LeveledUp = leveledUp;
            CurrentLevel = currentLevel;
        }
    }
}