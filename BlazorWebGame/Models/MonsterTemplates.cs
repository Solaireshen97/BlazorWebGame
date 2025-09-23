using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Monsters
{
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
    }
}