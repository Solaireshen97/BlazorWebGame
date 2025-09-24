using BlazorWebGame.GameConfig;
using System;

namespace BlazorWebGame.Models
{
    /// <summary>
    /// ������Ϸ����ֵϵͳ�ľ�̬��
    /// </summary>
    public static class ExpSystem
    {
        /// <summary>
        /// ��ȡָ���ȼ�����ľ���ֵ
        /// </summary>
        /// <param name="level">Ŀ��ȼ�</param>
        /// <returns>�ﵽ�õȼ�����ľ���ֵ</returns>
        public static long GetExpRequiredForLevel(int level)
        {
            if (level <= 1) return 0;
            if (level == 2) return ExpSystemConfig.BaseExp;
            if (level > ExpSystemConfig.MaxLevel) return long.MaxValue; // �������ȼ�����һ�������ܴﵽ��ֵ

            // �����ۻ�����ֵ
            long previousLevelExp = GetExpRequiredForLevel(level - 1);
            
            // Ӧ�ú��Ĺ�ʽ: 
            // �����10���ı�����������Գͷ�ϵ��
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
        /// �����ۼƾ���ֵ���㵱ǰ�ȼ�
        /// </summary>
        /// <param name="totalExp">�ۼƻ�õľ���ֵ</param>
        /// <returns>��ǰ�ȼ�</returns>
        public static int GetLevelFromExp(long totalExp)
        {
            int level = 1;
            
            // ���ۼƾ���ֵ�����Դﵽ��һ��ʱ���ص�ǰ�ȼ�
            while (totalExp >= GetExpRequiredForLevel(level + 1) && level < ExpSystemConfig.MaxLevel)
            {
                level++;
            }
            
            return level;
        }
        
        /// <summary>
        /// ������������һ�������ʣ�ྭ��ֵ
        /// </summary>
        /// <param name="totalExp">��ǰ�ۼƾ���ֵ</param>
        /// <returns>���������ʣ�ྭ��ֵ�����Ѵ����ȼ��򷵻�0</returns>
        public static long GetExpToNextLevel(long totalExp)
        {
            int currentLevel = GetLevelFromExp(totalExp);
            
            // ����Ѵ����ȼ�������0
            if (currentLevel >= ExpSystemConfig.MaxLevel) 
                return 0;
                
            long nextLevelExp = GetExpRequiredForLevel(currentLevel + 1);
            return nextLevelExp - totalExp;
        }
        
        /// <summary>
        /// ���㵱ǰ�ȼ��ľ���ֵ���Ȱٷֱ�
        /// </summary>
        /// <param name="totalExp">��ǰ�ۼƾ���ֵ</param>
        /// <returns>0-100�Ľ��Ȱٷֱȣ����Ѵ����ȼ��򷵻�100</returns>
        public static double GetLevelProgressPercentage(long totalExp)
        {
            int currentLevel = GetLevelFromExp(totalExp);
            
            // ����Ѵ����ȼ�������100%
            if (currentLevel >= ExpSystemConfig.MaxLevel) 
                return 100.0;
                
            long currentLevelExp = GetExpRequiredForLevel(currentLevel);
            long nextLevelExp = GetExpRequiredForLevel(currentLevel + 1);
            
            // ��ֹ������
            if (nextLevelExp - currentLevelExp <= 0) 
                return 100.0;
                
            double progress = (double)(totalExp - currentLevelExp) / (nextLevelExp - currentLevelExp) * 100.0;
            return Math.Min(Math.Max(progress, 0), 100); // ȷ����0-100��Χ��
        }
    }
}