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
    /// ��������ϵͳ�����ò���
    /// </summary>
    public static class MonsterAttributeConfig
    {
        #region ���������ֵ
        /// <summary>
        /// ��ͨ���������ֵ
        /// </summary>
        public static double NormalMonsterBaseValue { get; set; } = 30;

        /// <summary>
        /// ��Ӣ���������ֵ
        /// </summary>
        public static double EliteMonsterBaseValue { get; set; } = 75;

        /// <summary>
        /// Boss���������ֵ
        /// </summary>
        public static double BossMonsterBaseValue { get; set; } = 180;
        #endregion

        #region ��ֵ�������
        /// <summary>
        /// �����ֵ������Χ(��)
        /// </summary>
        public static double ExpValueVariance { get; set; } = 0.05;

        /// <summary>
        /// �������ֵ������Χ(��)
        /// </summary>
        public static double LootValueVariance { get; set; } = 0.01;

        /// <summary>
        /// ��Ҽ�ֵ������Χ(��)
        /// </summary>
        public static double GoldValueVariance { get; set; } = 0.05;

        /// <summary>
        /// ���߾���һ����ʣ�����ֵ/����������
        /// </summary>
        public static double ExpToOfflineSecondsRatio { get; set; } = 150;

        /// <summary>
        /// ��ҵ�������ϵ��(����ڽ�Ҽ�ֵ)
        /// </summary>
        public static double GoldDropMinRatio { get; set; } = 0.8;

        /// <summary>
        /// ��ҵ�������ϵ��(����ڽ�Ҽ�ֵ)
        /// </summary>
        public static double GoldDropMaxRatio { get; set; } = 1.2;
        #endregion

        #region ս�����Լ���
        /// <summary>
        /// ����ƽ����ɱʱ��(��)
        /// </summary>
        public static double AverageKillTimeSeconds { get; set; } =30;

        /// <summary>
        /// ������������С����
        /// </summary>
        public static int MinHitsToKillPlayer { get; set; } = 4;

        /// <summary>
        /// ����������������
        /// </summary>
        public static int MaxHitsToKillPlayer { get; set; } = 6;

        /// <summary>
        /// ���﹥�������������Χ(��)
        /// </summary>
        public static double AttackPowerVariance { get; set; } = 0.05;
        #endregion

        #region �����ٶȲ���
        /// <summary>
        /// ���������ٶ�(ÿ�빥������)
        /// </summary>
        public static double BaseAttacksPerSecond { get; set; } = 0.4;

        /// <summary>
        /// ��ͨ���﹥���ٶ�����ϵ��
        /// </summary>
        public static double NormalAttackSpeedMinMultiplier { get; set; } = 0.8;

        /// <summary>
        /// ��ͨ���﹥���ٶ�����ϵ��
        /// </summary>
        public static double NormalAttackSpeedMaxMultiplier { get; set; } = 1.1;

        /// <summary>
        /// ��Ӣ���﹥���ٶ�����ϵ��
        /// </summary>
        public static double EliteAttackSpeedMinMultiplier { get; set; } = 1.0;

        /// <summary>
        /// ��Ӣ���﹥���ٶ�����ϵ��
        /// </summary>
        public static double EliteAttackSpeedMaxMultiplier { get; set; } = 1.3;

        /// <summary>
        /// Boss���﹥���ٶ�����ϵ��
        /// </summary>
        public static double BossAttackSpeedMinMultiplier { get; set; } = 0.6;

        /// <summary>
        /// Boss���﹥���ٶ�����ϵ��
        /// </summary>
        public static double BossAttackSpeedMaxMultiplier { get; set; } = 0.8;

        /// <summary>
        /// ���幥���ٶȵ���ϵ��
        /// </summary>
        public static Dictionary<MonsterRace, double> RaceAttackSpeedMultipliers { get; } = new Dictionary<MonsterRace, double>
        {
            { MonsterRace.Beast, 1.3 },      // Ұ�޹�������
            { MonsterRace.Undead, 0.7 },     // ���鹥������
            { MonsterRace.Elemental, 1.0 },  // Ԫ�������׼ֵ��ʵ�ʻ������������
            { MonsterRace.Demon, 1.2 },      // ��ħ�����Կ�
            { MonsterRace.Humanoid, 1.0 }    // ���������׼ֵ
        };

        /// <summary>
        /// Ԫ�����﹥���ٶȲ�������
        /// </summary>
        public static double ElementalAttackSpeedMinMultiplier { get; set; } = 0.8;

        /// <summary>
        /// Ԫ�����﹥���ٶȲ�������
        /// </summary>
        public static double ElementalAttackSpeedMaxMultiplier { get; set; } = 1.2;
        #endregion

        #region ���幥��������
        /// <summary>
        /// ���幥������������ϵ��
        /// </summary>
        public static Dictionary<MonsterRace, double> RaceAttackPowerMultipliers { get; } = new Dictionary<MonsterRace, double>
        {
            { MonsterRace.Beast, 0.9 },     // Ұ�޹������Եͣ��ٶ�������
            { MonsterRace.Undead, 1.25 },   // ���鹥�����ߣ��ֲ��ٶ���
            { MonsterRace.Elemental, 1.0 }, // Ԫ�������׼ֵ����Ԫ�����;�����
            { MonsterRace.Demon, 1.15 },    // ��ħ�������ϸ�
            { MonsterRace.Humanoid, 1.0 }   // ���������׼ֵ
        };

        /// <summary>
        /// �������﹥���������������
        /// </summary>
        public static double HumanoidAttackPowerRandomVariance { get; set; } = 0.1;

        /// <summary>
        /// Ԫ�����͹���������ϵ��
        /// </summary>
        public static Dictionary<ElementType, double> ElementalAttackPowerMultipliers { get; } = new Dictionary<ElementType, double>
        {
            { ElementType.Fire, 1.15 },      // ��Ԫ���˺���
            { ElementType.Ice, 1.05 },       // ��Ԫ���˺��е�
            { ElementType.Lightning, 1.1 },  // ����Ԫ���˺��ϸ�
            { ElementType.Nature, 0.9 },     // ��ȻԪ���˺�ƫ��
            { ElementType.Shadow, 1.2 },     // ��ӰԪ���˺��ܸ�
            { ElementType.Holy, 1.0 },       // ��ʥԪ�ػ�׼ֵ
            { ElementType.None, 1.0 }        // ��Ԫ�ػ�׼ֵ
        };
        #endregion

        #region ���������
        /// <summary>
        /// �������͵����������
        /// </summary>
        public static Dictionary<MonsterType, double> MonsterTypeDropChances { get; } = new Dictionary<MonsterType, double>
        {
            { MonsterType.Normal, 0.1 }, // ��ͨ����10%����������
            { MonsterType.Elite, 0.3 },  // ��Ӣ����30%����������
            { MonsterType.Boss, 0.8 }    // Boss����80%����������
        };

        /// <summary>
        /// ����ֵ�����伸�ʵ�ת��ϵ��
        /// </summary>
        public static double LootValueToChanceRatio { get; set; } = 100.0;

        /// <summary>
        /// ��ƷƷ�ʵ��伸��ϵ��
        /// </summary>
        public static Dictionary<string, double> ItemQualityDropMultipliers { get; } = new Dictionary<string, double>
        {
            { "common", 1.0 },    // ��ͨƷ�ʻ�׼������
            { "uncommon", 0.5 },  // ����Ʒ�ʵ�����Ϊ��׼��50%
            { "rare", 0.2 }       // ϡ��Ʒ�ʵ�����Ϊ��׼��20%
        };
        #endregion
    }
}