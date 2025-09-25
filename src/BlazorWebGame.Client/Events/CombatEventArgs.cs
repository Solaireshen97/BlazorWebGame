using BlazorWebGame.Models;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Models.Skills;

namespace BlazorWebGame.Events
{
    /// <summary>
    /// 战斗事件参数
    /// </summary>
    public class CombatEventArgs : GameEventArgs
    {
        /// <summary>
        /// 相关敌人
        /// </summary>
        public Enemy? Enemy { get; }
        
        /// <summary>
        /// 伤害值，如果适用
        /// </summary>
        public int? Damage { get; }
        
        /// <summary>
        /// 使用的技能，如果适用
        /// </summary>
        public Skill? Skill { get; }
        
        /// <summary>
        /// 队伍，如果适用
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