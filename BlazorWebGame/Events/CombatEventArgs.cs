using BlazorWebGame.Models;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Models.Skills;

namespace BlazorWebGame.Events
{
    /// <summary>
    /// ս���¼�����
    /// </summary>
    public class CombatEventArgs : GameEventArgs
    {
        /// <summary>
        /// ��ص���
        /// </summary>
        public Enemy? Enemy { get; }
        
        /// <summary>
        /// �˺�ֵ���������
        /// </summary>
        public int? Damage { get; }
        
        /// <summary>
        /// ʹ�õļ��ܣ��������
        /// </summary>
        public Skill? Skill { get; }
        
        /// <summary>
        /// ���飬�������
        /// </summary>
        public Party? Party { get; }

        public CombatEventArgs(
            GameEventType eventType, 
            Player? player = null, 
            Enemy? enemy = null, 
            int? damage = null, 
            Skill? skill = null,
            Party? party = null) : base(eventType, player)
        {
            Enemy = enemy;
            Damage = damage;
            Skill = skill;
            Party = party;
        }
    }
}