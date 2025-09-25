using BlazorWebGame.Models;

namespace BlazorWebGame.Events
{
    /// <summary>
    /// ����ֵ�仯�¼�����
    /// </summary>
    public class ExperienceEventArgs : GameEventArgs
    {
        /// <summary>
        /// ��õľ���ֵ����
        /// </summary>
        public int ExperienceAmount { get; }
        
        /// <summary>
        /// ���ְҵ����
        /// </summary>
        public object ProfessionType { get; }
        
        /// <summary>
        /// ����ֵ��ú���ܾ���ֵ
        /// </summary>
        public long TotalExperience { get; }
        
        /// <summary>
        /// �Ƿ񴥷�������
        /// </summary>
        public bool LeveledUp { get; }
        
        /// <summary>
        /// ��ǰ�ȼ�
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