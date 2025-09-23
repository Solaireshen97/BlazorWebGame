using BlazorWebGame.Models.Monsters;
using System;
using System.Collections.Generic;

namespace BlazorWebGame.Models.Dungeons
{
    /// <summary>
    /// 副本系统
    /// </summary>
    public class Dungeon
    {
        /// <summary>
        /// 副本唯一ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// 副本名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 副本描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 推荐等级
        /// </summary>
        public int RecommendedLevel { get; set; }
        
        /// <summary>
        /// 最低参与人数
        /// </summary>
        public int MinPlayers { get; set; } = 1;
        
        /// <summary>
        /// 最大参与人数
        /// </summary>
        public int MaxPlayers { get; set; } = 5;
        
        /// <summary>
        /// 副本波次配置
        /// </summary>
        public List<DungeonWave> Waves { get; set; } = new();
        
        /// <summary>
        /// 副本完成奖励
        /// </summary>
        public List<DungeonReward> Rewards { get; set; } = new();
        
        /// <summary>
        /// 副本冷却时间(小时)
        /// </summary>
        public int CooldownHours { get; set; }
        
        /// <summary>
        /// 解锁需要的先决条件
        /// </summary>
        public List<string> Prerequisites { get; set; } = new();
    }

    /// <summary>
    /// 副本波次
    /// </summary>
    public class DungeonWave
    {
        /// <summary>
        /// 波次编号
        /// </summary>
        public int WaveNumber { get; set; }
        
        /// <summary>
        /// 波次描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 敌人生成配置
        /// </summary>
        public List<EnemySpawnInfo> Enemies { get; set; } = new();
        
        /// <summary>
        /// 特殊事件
        /// </summary>
        public string? SpecialEvent { get; set; }
    }

    /// <summary>
    /// 敌人生成信息
    /// </summary>
    public class EnemySpawnInfo
    {
        /// <summary>
        /// 敌人模板名称
        /// </summary>
        public string EnemyTemplateName { get; set; } = string.Empty;
        
        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; } = 1;
        
        /// <summary>
        /// 等级调整
        /// </summary>
        public int LevelAdjustment { get; set; } = 0;
        
        /// <summary>
        /// 生命值倍率
        /// </summary>
        public double HealthMultiplier { get; set; } = 1.0;
        
        /// <summary>
        /// 精英敌人
        /// </summary>
        public bool IsElite { get; set; } = false;
    }

    /// <summary>
    /// 副本奖励
    /// </summary>
    public class DungeonReward
    {
        /// <summary>
        /// 物品ID
        /// </summary>
        public string? ItemId { get; set; }
        
        /// <summary>
        /// 物品数量
        /// </summary>
        public int ItemQuantity { get; set; }
        
        /// <summary>
        /// 金币奖励
        /// </summary>
        public int Gold { get; set; }
        
        /// <summary>
        /// 经验奖励
        /// </summary>
        public int Experience { get; set; }
        
        /// <summary>
        /// 掉落概率
        /// </summary>
        public double DropChance { get; set; } = 1.0;
    }
}