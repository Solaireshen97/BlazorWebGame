using BlazorWebGame.GameConfig;
using System;

namespace BlazorWebGame.Models
{
    /// <summary>
    /// 管理游戏经验值系统的静态类
    /// </summary>
    public static class ExpSystem
    {
        /// <summary>
        /// 获取指定等级所需的经验值
        /// </summary>
        /// <param name="level">目标等级</param>
        /// <returns>达到该等级所需的经验值</returns>
        public static long GetExpRequiredForLevel(int level)
        {
            if (level <= 1) return 0;
            if (level == 2) return ExpSystemConfig.BaseExp;
            if (level > ExpSystemConfig.MaxLevel) return long.MaxValue; // 超过最大等级返回一个不可能达到的值

            // 计算累积经验值
            long previousLevelExp = GetExpRequiredForLevel(level - 1);
            
            // 应用核心公式: 
            // 如果是10级的倍数，额外乘以惩罚系数
            if ((level - 1) % 10 == 0)
            {
                return (long)(previousLevelExp * ExpSystemConfig.LevelExpMultiplier * ExpSystemConfig.TierExpMultiplier);
            }
            else
            {
                return (long)(previousLevelExp * ExpSystemConfig.LevelExpMultiplier);
            }
        }

        /// <summary>
        /// 根据累计经验值计算当前等级
        /// </summary>
        /// <param name="totalExp">累计获得的经验值</param>
        /// <returns>当前等级</returns>
        public static int GetLevelFromExp(long totalExp)
        {
            int level = 1;
            
            // 当累计经验值不足以达到下一级时返回当前等级
            while (totalExp >= GetExpRequiredForLevel(level + 1) && level < ExpSystemConfig.MaxLevel)
            {
                level++;
            }
            
            return level;
        }
        
        /// <summary>
        /// 计算升级到下一级所需的剩余经验值
        /// </summary>
        /// <param name="totalExp">当前累计经验值</param>
        /// <returns>升级所需的剩余经验值，如已达最大等级则返回0</returns>
        public static long GetExpToNextLevel(long totalExp)
        {
            int currentLevel = GetLevelFromExp(totalExp);
            
            // 如果已达最大等级，返回0
            if (currentLevel >= ExpSystemConfig.MaxLevel) 
                return 0;
                
            long nextLevelExp = GetExpRequiredForLevel(currentLevel + 1);
            return nextLevelExp - totalExp;
        }
        
        /// <summary>
        /// 计算当前等级的经验值进度百分比
        /// </summary>
        /// <param name="totalExp">当前累计经验值</param>
        /// <returns>0-100的进度百分比，如已达最大等级则返回100</returns>
        public static double GetLevelProgressPercentage(long totalExp)
        {
            int currentLevel = GetLevelFromExp(totalExp);
            
            // 如果已达最大等级，返回100%
            if (currentLevel >= ExpSystemConfig.MaxLevel) 
                return 100.0;
                
            long currentLevelExp = GetExpRequiredForLevel(currentLevel);
            long nextLevelExp = GetExpRequiredForLevel(currentLevel + 1);
            
            // 防止除以零
            if (nextLevelExp - currentLevelExp <= 0) 
                return 100.0;
                
            double progress = (double)(totalExp - currentLevelExp) / (nextLevelExp - currentLevelExp) * 100.0;
            return Math.Min(Math.Max(progress, 0), 100); // 确保在0-100范围内
        }
    }
}