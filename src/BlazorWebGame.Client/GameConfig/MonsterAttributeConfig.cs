using System;
using System.Collections.Generic;
using BlazorWebGame.Models.Monsters;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.GameConfig
{
    /// <summary>
    /// 怪物属性系统的配置参数
    /// </summary>
    public static class MonsterAttributeConfig
    {
        #region 怪物基础价值
        /// <summary>
        /// 普通怪物基础价值
        /// </summary>
        public static double NormalMonsterBaseValue { get; set; } = 30;

        /// <summary>
        /// 精英怪物基础价值
        /// </summary>
        public static double EliteMonsterBaseValue { get; set; } = 75;

        /// <summary>
        /// Boss怪物基础价值
        /// </summary>
        public static double BossMonsterBaseValue { get; set; } = 180;
        #endregion

        #region 价值计算参数
        /// <summary>
        /// 经验价值波动范围(±)
        /// </summary>
        public static double ExpValueVariance { get; set; } = 0.05;

        /// <summary>
        /// 掉落物价值波动范围(±)
        /// </summary>
        public static double LootValueVariance { get; set; } = 0.01;

        /// <summary>
        /// 金币价值波动范围(±)
        /// </summary>
        public static double GoldValueVariance { get; set; } = 0.05;

        /// <summary>
        /// 离线经验兑换比率（经验值/离线秒数）
        /// </summary>
        public static double ExpToOfflineSecondsRatio { get; set; } = 150;

        /// <summary>
        /// 金币掉落下限系数(相对于金币价值)
        /// </summary>
        public static double GoldDropMinRatio { get; set; } = 0.8;

        /// <summary>
        /// 金币掉落上限系数(相对于金币价值)
        /// </summary>
        public static double GoldDropMaxRatio { get; set; } = 1.2;
        #endregion

        #region 战斗属性计算
        /// <summary>
        /// 怪物平均击杀时间(秒)
        /// </summary>
        public static double AverageKillTimeSeconds { get; set; } =30;

        /// <summary>
        /// 怪物击败玩家最小次数
        /// </summary>
        public static int MinHitsToKillPlayer { get; set; } = 4;

        /// <summary>
        /// 怪物击败玩家最大次数
        /// </summary>
        public static int MaxHitsToKillPlayer { get; set; } = 6;

        /// <summary>
        /// 怪物攻击力随机波动范围(±)
        /// </summary>
        public static double AttackPowerVariance { get; set; } = 0.05;
        #endregion

        #region 攻击速度参数
        /// <summary>
        /// 基础攻击速度(每秒攻击次数)
        /// </summary>
        public static double BaseAttacksPerSecond { get; set; } = 0.4;

        /// <summary>
        /// 普通怪物攻击速度下限系数
        /// </summary>
        public static double NormalAttackSpeedMinMultiplier { get; set; } = 0.8;

        /// <summary>
        /// 普通怪物攻击速度上限系数
        /// </summary>
        public static double NormalAttackSpeedMaxMultiplier { get; set; } = 1.1;

        /// <summary>
        /// 精英怪物攻击速度下限系数
        /// </summary>
        public static double EliteAttackSpeedMinMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 精英怪物攻击速度上限系数
        /// </summary>
        public static double EliteAttackSpeedMaxMultiplier { get; set; } = 1.3;

        /// <summary>
        /// Boss怪物攻击速度下限系数
        /// </summary>
        public static double BossAttackSpeedMinMultiplier { get; set; } = 0.6;

        /// <summary>
        /// Boss怪物攻击速度上限系数
        /// </summary>
        public static double BossAttackSpeedMaxMultiplier { get; set; } = 0.8;

        /// <summary>
        /// 种族攻击速度调整系数
        /// </summary>
        public static Dictionary<MonsterRace, double> RaceAttackSpeedMultipliers { get; } = new Dictionary<MonsterRace, double>
        {
            { MonsterRace.Beast, 1.3 },      // 野兽攻击更快
            { MonsterRace.Undead, 0.7 },     // 亡灵攻击较慢
            { MonsterRace.Elemental, 1.0 },  // 元素生物基准值（实际会有随机波动）
            { MonsterRace.Demon, 1.2 },      // 恶魔攻击略快
            { MonsterRace.Humanoid, 1.0 }    // 人型生物基准值
        };

        /// <summary>
        /// 元素生物攻击速度波动下限
        /// </summary>
        public static double ElementalAttackSpeedMinMultiplier { get; set; } = 0.8;

        /// <summary>
        /// 元素生物攻击速度波动上限
        /// </summary>
        public static double ElementalAttackSpeedMaxMultiplier { get; set; } = 1.2;
        #endregion

        #region 种族攻击力调整
        /// <summary>
        /// 种族攻击力基础调整系数
        /// </summary>
        public static Dictionary<MonsterRace, double> RaceAttackPowerMultipliers { get; } = new Dictionary<MonsterRace, double>
        {
            { MonsterRace.Beast, 0.9 },     // 野兽攻击力略低，速度是优势
            { MonsterRace.Undead, 1.25 },   // 亡灵攻击力高，弥补速度慢
            { MonsterRace.Elemental, 1.0 }, // 元素生物基准值（由元素类型决定）
            { MonsterRace.Demon, 1.15 },    // 恶魔攻击力较高
            { MonsterRace.Humanoid, 1.0 }   // 人型生物基准值
        };

        /// <summary>
        /// 人型生物攻击力随机波动上限
        /// </summary>
        public static double HumanoidAttackPowerRandomVariance { get; set; } = 0.1;

        /// <summary>
        /// 元素类型攻击力调整系数
        /// </summary>
        public static Dictionary<ElementType, double> ElementalAttackPowerMultipliers { get; } = new Dictionary<ElementType, double>
        {
            { ElementType.Fire, 1.15 },      // 火元素伤害高
            { ElementType.Ice, 1.05 },       // 冰元素伤害中等
            { ElementType.Lightning, 1.1 },  // 闪电元素伤害较高
            { ElementType.Nature, 0.9 },     // 自然元素伤害偏低
            { ElementType.Shadow, 1.2 },     // 暗影元素伤害很高
            { ElementType.Holy, 1.0 },       // 神圣元素基准值
            { ElementType.None, 1.0 }        // 无元素基准值
        };
        #endregion

        #region 掉落物参数
        /// <summary>
        /// 怪物类型掉落基础几率
        /// </summary>
        public static Dictionary<MonsterType, double> MonsterTypeDropChances { get; } = new Dictionary<MonsterType, double>
        {
            { MonsterType.Normal, 0.1 }, // 普通怪物10%基础掉落率
            { MonsterType.Elite, 0.3 },  // 精英怪物30%基础掉落率
            { MonsterType.Boss, 0.8 }    // Boss怪物80%基础掉落率
        };

        /// <summary>
        /// 掉落值到掉落几率的转换系数
        /// </summary>
        public static double LootValueToChanceRatio { get; set; } = 100.0;

        /// <summary>
        /// 物品品质掉落几率系数
        /// </summary>
        public static Dictionary<string, double> ItemQualityDropMultipliers { get; } = new Dictionary<string, double>
        {
            { "common", 1.0 },    // 普通品质基准掉落率
            { "uncommon", 0.5 },  // 优秀品质掉落率为基准的50%
            { "rare", 0.2 }       // 稀有品质掉落率为基准的20%
        };
        #endregion
    }
}