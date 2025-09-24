using System;

namespace BlazorWebGame.GameConfig
{
    /// <summary>
    /// ����ϵͳ�����ò���
    /// </summary>
    public static class AttributeSystemConfig
    {
        #region ������������
        /// <summary>
        /// 1������������ֵ
        /// </summary>
        public static int BaseMainAttribute { get; set; } = 10;
        
        /// <summary>
        /// 1����������ֵ
        /// </summary>
        public static int BaseStamina { get; set; } = 10;
        
        /// <summary>
        /// 1����������ֵ
        /// </summary>
        public static int BaseHealth { get; set; } = 200;
        #endregion

        #region ���Գɳ�ϵ��
        /// <summary>
        /// �͵ȼ�/�ߵȼ��ֽ��
        /// </summary>
        public static int LevelThreshold { get; set; } = 10;
        
        /// <summary>
        /// 1-10��ÿ������������
        /// </summary>
        public static int LowLevelMainAttributeGrowth { get; set; } = 2;
        
        /// <summary>
        /// 1-10��ÿ����������
        /// </summary>
        public static int LowLevelStaminaGrowth { get; set; } = 2;
        
        /// <summary>
        /// 11-60��ÿ������������
        /// </summary>
        public static int HighLevelMainAttributeGrowth { get; set; } = 3;
        
        /// <summary>
        /// 11-60��ÿ����������
        /// </summary>
        public static int HighLevelStaminaGrowth { get; set; } = 3;
        #endregion

        #region �˺�����ϵ��
        /// <summary>
        /// �����Ե�������ת��ϵ��
        /// </summary>
        public static double MainAttributeToAPRatio { get; set; } = 1.0;
        
        /// <summary>
        /// �����Ե��˺�����ϵ��
        /// </summary>
        public static double MainAttributeToDamageMultiplier { get; set; } = 0.01;
        
        /// <summary>
        /// ��������DPSת��ϵ��
        /// </summary>
        public static double APToDPSRatio { get; set; } = 1.0 / 14.0;
        
        /// <summary>
        /// ����DPSÿ������ϵ��
        /// </summary>
        public static double WeaponDPSLevelMultiplier { get; set; } = 1.35;
        
        /// <summary>
        /// 1����������DPS
        /// </summary>
        public static double BaseWeaponDPS { get; set; } = 5.0;
        #endregion

        #region ����ֵ����
        /// <summary>
        /// ����������ֵת��ϵ��
        /// </summary>
        public static double StaminaToHealthRatio { get; set; } = 1.0;
        #endregion

        #region װ������ϵ��
        /// <summary>
        /// ��װ������ϵ��������ڵ�ǰ�ȼ���
        /// </summary>
        public static double CommonItemAttributeRatio { get; set; } = 1.0;
        
        /// <summary>
        /// ��װ������ϵ��������ڵ�ǰ�ȼ���
        /// </summary>
        public static double UncommonItemAttributeRatio { get; set; } = 2.0;
        
        /// <summary>
        /// ��װ������ϵ��������ڵ�ǰ�ȼ���
        /// </summary>
        public static double RareItemAttributeRatio { get; set; } = 3.0;
        #endregion
    }
}