using System.Collections.Generic;

namespace BlazorWebGame.Models.Monsters
{
    /// <summary>
    /// ��ͨ����ģ������
    /// </summary>
    public static class NormalMonsters
    {
        public static readonly List<Enemy> Monsters = new()
        {
            new Enemy
            {
                Name = "Goblin",
                Description = "һ����С����������Ƥ���ϲ���ռ���������Ʒ��",
                Level = 1,
                Type = MonsterType.Normal,
                Race = MonsterRace.Humanoid,
                Health = 50, MaxHealth = 50,
                AttackPower = 5, AttacksPerSecond = 0.8,
                XpReward = 15, MinGold = 2, MaxGold = 6,
                SkillIds = new List<string> { "MON_001" }, // �ͻ�
                LootTable = new Dictionary<string, double>
                {
                    { "EQ_WEP_002", 0.1 } // 10% ����粼�ֹ���
                }
            },
            new Enemy
            {
                Name = "Slime",
                Description = "һ�Ų����䶯���Һ��������ܽ�Ӵ��������塣",
                Level = 1,
                Type = MonsterType.Normal,
                Race = MonsterRace.Elemental,
                Health = 40, MaxHealth = 40,
                AttackPower = 3, AttacksPerSecond = 1.0,
                XpReward = 10, MinGold = 1, MaxGold = 4,
                SkillIds = new List<string> { "MON_003" }, // ��ʴ
                LootTable = new Dictionary<string, double>()
            },
            // ������Ӹ�����ͨ����...
        };
    }
}