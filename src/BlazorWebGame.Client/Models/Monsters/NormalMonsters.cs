using System.Collections.Generic;

namespace BlazorWebGame.Models.Monsters
{
    /// <summary>
    /// ��ͨ����ģ������
    /// </summary>
    public static class NormalMonsters
    {
        // ��̬���캯�������ڳ�ʼ��ʱ���ͨ�����������ɵĹ���
        static NormalMonsters()
        {
            // ���Ԥ��Ĺ���ʵ��
            //itializeMonsters();

            // ���ͨ�����������ɵĹ���
            AddCalculatedMonsters();
        }

        public static readonly List<Enemy> Monsters = new();

        /// <summary>
        /// ��ʼ��Ԥ��Ĺ���ʵ��
        /// </summary>
        private static void InitializeMonsters()
        {
            Monsters.Add(new Enemy
            {
                Name = "Goblin",
                Description = "һ����С����������Ƥ���ϲ���ռ���������Ʒ��",
                Level = 1,
                Type = MonsterType.Normal,
                Race = MonsterRace.Humanoid,
                Health = 50,
                MaxHealth = 50,
                AttackPower = 5,
                AttacksPerSecond = 0.8,
                XpReward = 15,
                MinGold = 2,
                MaxGold = 6,
                SkillIds = new List<string> { "MON_001" }, // �ͻ�
                LootTable = new Dictionary<string, double>
                {
                    { "EQ_WEP_002", 0.1 } // 10% ����粼�ֹ���
                }
            });

            Monsters.Add(new Enemy
            {
                Name = "Slime",
                Description = "һ�Ų����䶯���Һ��������ܽ�Ӵ��������塣",
                Level = 1,
                Type = MonsterType.Normal,
                Race = MonsterRace.Elemental,
                Health = 40,
                MaxHealth = 40,
                AttackPower = 3,
                AttacksPerSecond = 1.0,
                XpReward = 10,
                MinGold = 1,
                MaxGold = 4,
                SkillIds = new List<string> { "MON_003" }, // ��ʴ
                LootTable = new Dictionary<string, double>()
            });
        }

        /// <summary>
        /// ���ͨ�����������ɵĹ���
        /// </summary>
        private static void AddCalculatedMonsters()
        {
            var monsterAttribute1 = new Enemy
            {
                Name = "Ⱦ��������",
                Description = "",
                Level = 1,
                Type = MonsterType.Normal,
                Race = MonsterRace.Beast,
                ElementType = ElementType.None,
                SkillIds = new List<string> { },
                LootTable = new Dictionary<string, double>
                {
                }
            };

            var monsters1 = MonsterAttributeCalculator.GenerateMonster(
                level: 1,
                expRatio: 0.6,
                lootRatio: 0.2,
                monsterType: MonsterType.Normal,
                predefinedEnemy: monsterAttribute1);
            Monsters.Add(monsters1);

            var monsterAttribute2 = new Enemy
            {
                Name = "Ⱦ����ɭ����",
                Description = "",
                Level = 3,
                Type = MonsterType.Normal,
                Race = MonsterRace.Beast,
                ElementType = ElementType.None,
                SkillIds = new List<string> { },
                LootTable = new Dictionary<string, double>
                {
                }
            };

            var monsters2 = MonsterAttributeCalculator.GenerateMonster(
                level: 3,
                expRatio: 0.7,
                lootRatio: 0.2,
                monsterType: MonsterType.Normal,
                predefinedEnemy: monsterAttribute2);
            Monsters.Add(monsters2);
        }

        /// <summary>
        /// �������������һ���µļ������ɵĹ���
        /// </summary>
        public static void AddNewCalculatedMonster(string name, string description, int level,
            MonsterRace race = MonsterRace.Humanoid, ElementType elementType = ElementType.None,
            double expRatio = 0.6, double lootRatio = 0.15)
        {
            var predefinedMonster = new Enemy
            {
                Name = name,
                Description = description,
                Level = level,
                Type = MonsterType.Normal,
                Race = race,
                ElementType = elementType
            };

            var calculatedMonster = MonsterAttributeCalculator.GenerateMonster(
                level: level,
                expRatio: expRatio,
                lootRatio: lootRatio,
                monsterType: MonsterType.Normal,
                predefinedEnemy: predefinedMonster);

            Monsters.Add(calculatedMonster);
        }
    }
}