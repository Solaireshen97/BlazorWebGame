using BlazorWebGame.Models.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Battles
{
    /// <summary>
    /// 战斗上下文，描述一场战斗的全部信息
    /// </summary>
    public class BattleContext
    {
        /// <summary>
        /// 战斗唯一ID
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// 战斗类型
        /// </summary>
        public BattleType BattleType { get; set; }

        /// <summary>
        /// 参与战斗的玩家列表
        /// </summary>
        public List<Player> Players { get; } = new();

        /// <summary>
        /// 战斗中的敌人列表
        /// </summary>
        public List<Enemy> Enemies { get; } = new();

        /// <summary>
        /// 玩家队伍引用(可选)
        /// </summary>
        public Party? Party { get; set; }
        
        /// <summary>
        /// 战斗所属副本ID(如果是副本战斗)
        /// </summary>
        public string? DungeonId { get; set; }

        /// <summary>
        /// 副本中的波次(如果是副本战斗)
        /// </summary>
        public int WaveNumber { get; set; }

        /// <summary>
        /// 战斗开始时间
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 战斗状态
        /// </summary>
        public BattleState State { get; set; } = BattleState.Active;

        /// <summary>
        /// 目标选择策略
        /// </summary>
        public TargetSelectionStrategy PlayerTargetStrategy { get; set; } = TargetSelectionStrategy.LowestHealth;
        
        /// <summary>
        /// 敌人目标选择策略
        /// </summary>
        public TargetSelectionStrategy EnemyTargetStrategy { get; set; } = TargetSelectionStrategy.Random;
        
        /// <summary>
        /// 玩家攻击目标映射
        /// </summary>
        public Dictionary<string, string> PlayerTargets { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 是否允许死亡玩家自动复活
        /// </summary>
        public bool AllowAutoRevive { get; set; } = true;

        /// <summary>
        /// 判断战斗是否完成
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                // 所有敌人死亡 = 胜利
                if (!Enemies.Any())
                    return true;

                // 如果允许自动复活，即使所有玩家死亡也不算战斗失败
                if (AllowAutoRevive)
                {
                    // 只有在明确要求结束战斗时才算完成（例如玩家主动退出）
                    return false;
                }

                // 不允许自动复活时，所有玩家死亡 = 失败
                return Players.All(p => p.IsDead);
            }
        }

        /// <summary>
        /// 检查是否获胜
        /// </summary>
        public bool IsVictory => Enemies.Count == 0 && Players.Any(p => !p.IsDead);
    }

    /// <summary>
    /// 战斗类型
    /// </summary>
    public enum BattleType
    {
        /// <summary>
        /// 单人战斗
        /// </summary>
        Solo,
        
        /// <summary>
        /// 团队战斗
        /// </summary>
        Party,
        
        /// <summary>
        /// 副本战斗
        /// </summary>
        Dungeon
    }

    /// <summary>
    /// 战斗状态
    /// </summary>
    public enum BattleState
    {
        /// <summary>
        /// 准备中
        /// </summary>
        Preparing,
        
        /// <summary>
        /// 进行中
        /// </summary>
        Active,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        Cancelled,
    }

    /// <summary>
    /// 目标选择策略
    /// </summary>
    public enum TargetSelectionStrategy
    {
        /// <summary>
        /// 随机目标
        /// </summary>
        Random,
        
        /// <summary>
        /// 最低生命值目标
        /// </summary>
        LowestHealth,
        
        /// <summary>
        /// 最高生命值目标
        /// </summary>
        HighestHealth,
        
        /// <summary>
        /// 最高威胁目标(坦克)
        /// </summary>
        HighestThreat
    }
}