using BlazorWebGame.Models.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Battles
{
    /// <summary>
    /// ս�������ģ�����һ��ս����ȫ����Ϣ
    /// </summary>
    public class BattleContext
    {
        /// <summary>
        /// ս��ΨһID
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// ս������
        /// </summary>
        public BattleType BattleType { get; set; }

        /// <summary>
        /// ����ս��������б�
        /// </summary>
        public List<Player> Players { get; } = new();

        /// <summary>
        /// ս���еĵ����б�
        /// </summary>
        public List<Enemy> Enemies { get; } = new();

        /// <summary>
        /// ��Ҷ�������(��ѡ)
        /// </summary>
        public Party? Party { get; set; }
        
        /// <summary>
        /// ս����������ID(����Ǹ���ս��)
        /// </summary>
        public string? DungeonId { get; set; }

        /// <summary>
        /// �����еĲ���(����Ǹ���ս��)
        /// </summary>
        public int WaveNumber { get; set; }

        /// <summary>
        /// ս����ʼʱ��
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ս��״̬
        /// </summary>
        public BattleState State { get; set; } = BattleState.Active;

        /// <summary>
        /// Ŀ��ѡ�����
        /// </summary>
        public TargetSelectionStrategy PlayerTargetStrategy { get; set; } = TargetSelectionStrategy.LowestHealth;
        
        /// <summary>
        /// ����Ŀ��ѡ�����
        /// </summary>
        public TargetSelectionStrategy EnemyTargetStrategy { get; set; } = TargetSelectionStrategy.Random;
        
        /// <summary>
        /// ��ҹ���Ŀ��ӳ��
        /// </summary>
        public Dictionary<string, string> PlayerTargets { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// �Ƿ�������������Զ�����
        /// </summary>
        public bool AllowAutoRevive { get; set; } = true;

        /// <summary>
        /// �ж�ս���Ƿ����
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                // ���е������� = ʤ��
                if (!Enemies.Any())
                    return true;

                // ��������Զ������ʹ�����������Ҳ����ս��ʧ��
                if (AllowAutoRevive)
                {
                    // ֻ������ȷҪ�����ս��ʱ������ɣ�������������˳���
                    return false;
                }

                // �������Զ�����ʱ������������� = ʧ��
                return Players.All(p => p.IsDead);
            }
        }

        /// <summary>
        /// ����Ƿ��ʤ
        /// </summary>
        public bool IsVictory => Enemies.Count == 0 && Players.Any(p => !p.IsDead);
    }

    /// <summary>
    /// ս������
    /// </summary>
    public enum BattleType
    {
        /// <summary>
        /// ����ս��
        /// </summary>
        Solo,
        
        /// <summary>
        /// �Ŷ�ս��
        /// </summary>
        Party,
        
        /// <summary>
        /// ����ս��
        /// </summary>
        Dungeon
    }

    /// <summary>
    /// ս��״̬
    /// </summary>
    public enum BattleState
    {
        /// <summary>
        /// ׼����
        /// </summary>
        Preparing,
        
        /// <summary>
        /// ������
        /// </summary>
        Active,
        
        /// <summary>
        /// �����
        /// </summary>
        Completed,
        Cancelled,
    }

    /// <summary>
    /// Ŀ��ѡ�����
    /// </summary>
    public enum TargetSelectionStrategy
    {
        /// <summary>
        /// ���Ŀ��
        /// </summary>
        Random,
        
        /// <summary>
        /// �������ֵĿ��
        /// </summary>
        LowestHealth,
        
        /// <summary>
        /// �������ֵĿ��
        /// </summary>
        HighestHealth,
        
        /// <summary>
        /// �����вĿ��(̹��)
        /// </summary>
        HighestThreat
    }
}