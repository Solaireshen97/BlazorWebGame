using System;

namespace BlazorWebGame.GameConfig
{
    /// <summary>
    /// 属性系统的配置参数
    /// </summary>
    public static class AttributeSystemConfig
    {
        #region 基础属性配置
        /// <summary>
        /// 1级基础主属性值
        /// </summary>
        public static int BaseMainAttribute { get; set; } = 10;
        
        /// <summary>
        /// 1级基础耐力值
        /// </summary>
        public static int BaseStamina { get; set; } = 10;
        
        /// <summary>
        /// 1级基础生命值
        /// </summary>
        public static int BaseHealth { get; set; } = 200;
        #endregion

        #region 属性成长系数
        /// <summary>
        /// 低等级/高等级分界点
        /// </summary>
        public static int LevelThreshold { get; set; } = 10;
        
        /// <summary>
        /// 1-10级每级主属性增长
        /// </summary>
        public static int LowLevelMainAttributeGrowth { get; set; } = 2;
        
        /// <summary>
        /// 1-10级每级耐力增长
        /// </summary>
        public static int LowLevelStaminaGrowth { get; set; } = 2;
        
        /// <summary>
        /// 11-60级每级主属性增长
        /// </summary>
        public static int HighLevelMainAttributeGrowth { get; set; } = 3;
        
        /// <summary>
        /// 11-60级每级耐力增长
        /// </summary>
        public static int HighLevelStaminaGrowth { get; set; } = 3;
        #endregion

        #region 伤害计算系数
        /// <summary>
        /// 主属性到攻击力转换系数
        /// </summary>
        public static double MainAttributeToAPRatio { get; set; } = 1.0;
        
        /// <summary>
        /// 主属性到伤害倍率系数
        /// </summary>
        public static double MainAttributeToDamageMultiplier { get; set; } = 0.01;
        
        /// <summary>
        /// 攻击力到DPS转换系数
        /// </summary>
        public static double APToDPSRatio { get; set; } = 1.0 / 14.0;
        
        /// <summary>
        /// 武器DPS每级增长系数
        /// </summary>
        public static double WeaponDPSLevelMultiplier { get; set; } = 1.35;
        
        /// <summary>
        /// 1级基础武器DPS
        /// </summary>
        public static double BaseWeaponDPS { get; set; } = 5.0;
        #endregion

        #region 生命值计算
        /// <summary>
        /// 耐力到生命值转换系数
        /// </summary>
        public static double StaminaToHealthRatio { get; set; } = 1.0;
        #endregion

        #region 装备属性系数
        /// <summary>
        /// 白装主属性系数（相对于当前等级）
        /// </summary>
        public static double CommonItemAttributeRatio { get; set; } = 1.0;
        
        /// <summary>
        /// 蓝装主属性系数（相对于当前等级）
        /// </summary>
        public static double UncommonItemAttributeRatio { get; set; } = 2.0;
        
        /// <summary>
        /// 紫装主属性系数（相对于当前等级）
        /// </summary>
        public static double RareItemAttributeRatio { get; set; } = 3.0;
        #endregion
    }
}