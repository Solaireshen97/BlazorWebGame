using System.Collections.Generic;

namespace BlazorWebGame.Models
{
    public static class MonsterTemplates
    {
        public static readonly List<Enemy> All = new()
        {
            new Enemy
            {
                Name = "�粼��",
                Health = 50, MaxHealth = 50,
                AttackPower = 5, AttacksPerSecond = 0.8,
                XpReward = 15, MinGold = 2, MaxGold = 6,
                // Ϊ�粼��װ������
                SkillIds = new List<string> { "MON_001" } // �ͻ�
            },
            new Enemy
            {
                Name = "ʷ��ķ",
                Health = 40, MaxHealth = 40,
                AttackPower = 3, AttacksPerSecond = 1.0,
                XpReward = 10, MinGold = 1, MaxGold = 4,
                // Ϊʷ��ķװ������
                SkillIds = new List<string> { "MON_003" } // ��ʴ
            },
            new Enemy
            {
                Name = "�粼������",
                Health = 60, MaxHealth = 60,
                AttackPower = 4, AttacksPerSecond = 0.7,
                XpReward = 25, MinGold = 5, MaxGold = 10,
                // װ����������
                SkillIds = new List<string> { "MON_001", "MON_002" } // �ͻ� + С������
            }
        };
    }
}