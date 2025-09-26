using System.Collections.Generic;
using System.Linq;
using BlazorWebGame.Shared.Enums;

namespace BlazorWebGame.Models.Monsters
{
    // Re-export shared ElementType for backward compatibility
    using ElementType = BlazorWebGame.Shared.Enums.ElementType;
    /// <summary>
    /// ��������ö��
    /// </summary>
    public enum MonsterType
    {
        Normal,     // ��ͨ����
        Elite,      // ��Ӣ����
        Boss        // �������
    }

    /// <summary>
    /// ������������
    /// </summary>
    public enum MonsterRace
    {
        Humanoid,   // �������� (��粼��)
        Beast,      // Ұ��
        Elemental,  // Ԫ������
        Undead,     // ����
        Demon       // ��ħ
    }

    /// <summary>
    /// �ṩ�����й���ģ��ķ���
    /// </summary>
    public static class MonsterTemplates
    {
        /// <summary>
        /// ��ȡ���й���ģ��
        /// </summary>
        public static List<Enemy> All =>
            NormalMonsters.Monsters
            .Concat(EliteMonsters.Monsters)
            .Concat(BossMonsters.Monsters)
            .ToList();

        /// <summary>
        /// �������Ʋ��ҹ���ģ��
        /// </summary>
        public static Enemy? GetByName(string name) =>
            All.FirstOrDefault(m => m.Name == name);

        /// <summary>
        /// ��ȡ�ض����͵����й���
        /// </summary>
        public static List<Enemy> GetByType(MonsterType type)
        {
            return type switch
            {
                MonsterType.Normal => NormalMonsters.Monsters,
                MonsterType.Elite => EliteMonsters.Monsters,
                MonsterType.Boss => BossMonsters.Monsters,
                _ => new List<Enemy>()
            };
        }

        /// <summary>
        /// ��ȡ�ض��ȼ���Χ�Ĺ���
        /// </summary>
        public static List<Enemy> GetByLevelRange(int minLevel, int maxLevel)
        {
            return All.Where(m => m.Level >= minLevel && m.Level <= maxLevel).ToList();
        }

        /// <summary>
        /// ���ݹ������ͺ͵ȼ�Ӧ����������
        /// </summary>
        /// <param name="enemy">ҪӦ�����ԵĹ���</param>
        public static void ApplyAvoidanceRating(Enemy enemy)
        {
            // �������ͺ͵ȼ���������ֵ
            switch (enemy.Type)
            {
                case MonsterType.Normal:
                    enemy.AvoidanceRating = enemy.Level * 5;
                    enemy.DodgeChance = 0.02; // 2%��������
                    break;
                    
                case MonsterType.Elite:
                    enemy.AvoidanceRating = enemy.Level * 8;
                    enemy.DodgeChance = 0.05; // 5%��������
                    break;
                    
                case MonsterType.Boss:
                    enemy.AvoidanceRating = enemy.Level * 10;
                    enemy.DodgeChance = 0.08; // 8%��������
                    break;
            }
            
            // �������������������
            switch (enemy.Race)
            {
                case MonsterRace.Beast:
                    // Ұ�޸����ݣ���������
                    enemy.DodgeChance += 0.03;
                    enemy.AvoidanceRating += enemy.Level * 2;
                    break;
                    
                case MonsterRace.Elemental:
                    // Ԫ�����ﲻ�ȶ�����ʱ��������
                    enemy.DodgeChance += 0.02;
                    break;
                    
                case MonsterRace.Humanoid:
                    // ���������ܹ�ѵ�����и��õ����ܼ���
                    enemy.AvoidanceRating += enemy.Level * 1;
                    break;
            }
        }
        
        /// <summary>
        /// ��ʼ�����������ս������
        /// </summary>
        /// <param name="enemy">Ҫ��ʼ���Ĺ���</param>
        public static void InitializeCombatAttributes(Enemy enemy)
        {
            // Ӧ����������
            ApplyAvoidanceRating(enemy);
            
            // Ӧ�ÿ�������
            ApplyResistances(enemy);
            
            // Ӧ������ս������
            ApplyAdditionalCombatStats(enemy);
        }
        
        /// <summary>
        /// Ӧ�ù����Ԫ�ؿ���
        /// </summary>
        private static void ApplyResistances(Enemy enemy)
        {
            // ��ʼ��Ԫ�ؿ����ֵ�
            enemy.ElementalResistances = new Dictionary<ElementType, double>();
            
            // �����������û�������
            switch (enemy.Race)
            {
                case MonsterRace.Elemental:
                    // Ԫ�����������Ԫ���и߿���
                    if (enemy.ElementType != ElementType.None)
                    {
                        enemy.ElementalResistances[enemy.ElementType] = 0.75; // 75%����
                    }
                    break;
                    
                case MonsterRace.Undead:
                    // ���鿹�Ի�����������Ӱ
                    enemy.ElementalResistances[ElementType.Fire] = -0.25; // -25%���ԣ����㣩
                    enemy.ElementalResistances[ElementType.Shadow] = 0.5; // 50%����
                    break;
                    
                case MonsterRace.Demon:
                    // ��ħ��������ʥ
                    enemy.ElementalResistances[ElementType.Fire] = 0.5; // 50%����
                    enemy.ElementalResistances[ElementType.Holy] = -0.25; // -25%���ԣ����㣩
                    break;
            }
            
            // ���ݵȼ����������Ӷ��⿹��
            if (enemy.Type == MonsterType.Elite || enemy.Type == MonsterType.Boss)
            {
                // ��Ӣ��Boss���ж�����������
                AddRandomResistance(enemy);
            }
        }
        
        /// <summary>
        /// Ϊ��������һ�������Ԫ�ؿ���
        /// </summary>
        private static void AddRandomResistance(Enemy enemy)
        {
            var random = new System.Random();
            var elementTypes = System.Enum.GetValues<ElementType>();
            
            // �ų�None���ͺ����п��Ե�Ԫ������
            var availableTypes = elementTypes
                .Where(e => e != ElementType.None && !enemy.ElementalResistances.ContainsKey(e))
                .ToList();
                
            if (availableTypes.Any())
            {
                var selectedType = availableTypes[random.Next(availableTypes.Count)];
                double resistanceValue = 0.3 + (random.NextDouble() * 0.2); // 30%-50%�Ŀ���
                
                enemy.ElementalResistances[selectedType] = resistanceValue;
            }
        }
        
        /// <summary>
        /// Ӧ�ö����ս������
        /// </summary>
        private static void ApplyAdditionalCombatStats(Enemy enemy)
        {
            var random = new System.Random();
            
            // ���������� - ���ڵȼ�������
            enemy.AccuracyRating = enemy.Level * 5;
            
            if (enemy.Type == MonsterType.Elite)
                enemy.AccuracyRating += 10;
            else if (enemy.Type == MonsterType.Boss)
                enemy.AccuracyRating += 20;
                
            // ���ñ����ʺͱ����˺�
            enemy.CriticalChance = 0.05; // ����5%����
            enemy.CriticalMultiplier = 1.5; // ����150%�����˺�
            
            if (enemy.Type == MonsterType.Elite)
            {
                enemy.CriticalChance += 0.03;
                enemy.CriticalMultiplier += 0.2;
            }
            else if (enemy.Type == MonsterType.Boss)
            {
                enemy.CriticalChance += 0.05;
                enemy.CriticalMultiplier += 0.5;
            }
        }
    }
}