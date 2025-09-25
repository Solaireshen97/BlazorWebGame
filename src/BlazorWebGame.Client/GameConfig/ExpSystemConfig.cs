using System;

namespace BlazorWebGame.GameConfig
{
    /// <summary>
    /// 经验值系统的配置参数
    /// </summary>
    public static class ExpSystemConfig
    {
        /// <summary>
        /// 等级上限
        /// </summary>
        public static int MaxLevel { get; set; } = 10;

        /// <summary>
        /// 基础经验值：1级到2级需要的经验值
        /// </summary>
        public static long BaseExp { get; set; } = 1800;

        /// <summary>
        /// 每级经验值的增长系数
        /// </summary>
        public static double LevelExpMultiplier { get; set; } = 1.5;

        /// <summary>
        /// 每10级的额外惩罚系数
        /// </summary>
        public static double TierExpMultiplier { get; set; } = 1.8;
    }
}