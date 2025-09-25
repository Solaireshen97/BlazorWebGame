using System;

namespace BlazorWebGame.GameConfig
{
    /// <summary>
    /// ����ֵϵͳ�����ò���
    /// </summary>
    public static class ExpSystemConfig
    {
        /// <summary>
        /// �ȼ�����
        /// </summary>
        public static int MaxLevel { get; set; } = 10;

        /// <summary>
        /// ��������ֵ��1����2����Ҫ�ľ���ֵ
        /// </summary>
        public static long BaseExp { get; set; } = 1800;

        /// <summary>
        /// ÿ������ֵ������ϵ��
        /// </summary>
        public static double LevelExpMultiplier { get; set; } = 1.5;

        /// <summary>
        /// ÿ10���Ķ���ͷ�ϵ��
        /// </summary>
        public static double TierExpMultiplier { get; set; } = 1.8;
    }
}