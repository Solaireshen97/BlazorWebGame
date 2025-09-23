using BlazorWebGame.Models.Monsters;
using System;
using System.Collections.Generic;

namespace BlazorWebGame.Models.Dungeons
{
    /// <summary>
    /// ����ϵͳ
    /// </summary>
    public class Dungeon
    {
        /// <summary>
        /// ����ΨһID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ��������
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// ��������
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// �Ƽ��ȼ�
        /// </summary>
        public int RecommendedLevel { get; set; }
        
        /// <summary>
        /// ��Ͳ�������
        /// </summary>
        public int MinPlayers { get; set; } = 1;
        
        /// <summary>
        /// ����������
        /// </summary>
        public int MaxPlayers { get; set; } = 5;
        
        /// <summary>
        /// ������������
        /// </summary>
        public List<DungeonWave> Waves { get; set; } = new();
        
        /// <summary>
        /// ������ɽ���
        /// </summary>
        public List<DungeonReward> Rewards { get; set; } = new();
        
        /// <summary>
        /// ������ȴʱ��(Сʱ)
        /// </summary>
        public int CooldownHours { get; set; }
        
        /// <summary>
        /// ������Ҫ���Ⱦ�����
        /// </summary>
        public List<string> Prerequisites { get; set; } = new();
    }

    /// <summary>
    /// ��������
    /// </summary>
    public class DungeonWave
    {
        /// <summary>
        /// ���α��
        /// </summary>
        public int WaveNumber { get; set; }
        
        /// <summary>
        /// ��������
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// ������������
        /// </summary>
        public List<EnemySpawnInfo> Enemies { get; set; } = new();
        
        /// <summary>
        /// �����¼�
        /// </summary>
        public string? SpecialEvent { get; set; }
    }

    /// <summary>
    /// ����������Ϣ
    /// </summary>
    public class EnemySpawnInfo
    {
        /// <summary>
        /// ����ģ������
        /// </summary>
        public string EnemyTemplateName { get; set; } = string.Empty;
        
        /// <summary>
        /// ����
        /// </summary>
        public int Count { get; set; } = 1;
        
        /// <summary>
        /// �ȼ�����
        /// </summary>
        public int LevelAdjustment { get; set; } = 0;
        
        /// <summary>
        /// ����ֵ����
        /// </summary>
        public double HealthMultiplier { get; set; } = 1.0;
        
        /// <summary>
        /// ��Ӣ����
        /// </summary>
        public bool IsElite { get; set; } = false;
    }

    /// <summary>
    /// ��������
    /// </summary>
    public class DungeonReward
    {
        /// <summary>
        /// ��ƷID
        /// </summary>
        public string? ItemId { get; set; }
        
        /// <summary>
        /// ��Ʒ����
        /// </summary>
        public int ItemQuantity { get; set; }
        
        /// <summary>
        /// ��ҽ���
        /// </summary>
        public int Gold { get; set; }
        
        /// <summary>
        /// ���齱��
        /// </summary>
        public int Experience { get; set; }
        
        /// <summary>
        /// �������
        /// </summary>
        public double DropChance { get; set; } = 1.0;
    }
}