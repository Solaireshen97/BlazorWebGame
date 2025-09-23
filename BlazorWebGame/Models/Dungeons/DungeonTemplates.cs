using System.Collections.Generic;
using System.Linq;
using BlazorWebGame.Models.Monsters;

namespace BlazorWebGame.Models.Dungeons
{
    /// <summary>
    /// �ṩ���п��ø���ģ��Ĺ�����
    /// </summary>
    public static class DungeonTemplates
    {
        /// <summary>
        /// ��ȡ���п��ø���
        /// </summary>
        public static List<Dungeon> All =>
            BasicDungeons.Dungeons
            .Concat(EliteDungeons.Dungeons)
            .ToList();

        /// <summary>
        /// ͨ��ID��ȡ����
        /// </summary>
        public static Dungeon? GetDungeonById(string id)
        {
            return All.FirstOrDefault(d => d.Id == id);
        }

        /// <summary>
        /// ��ȡָ���ȼ���Χ�ĸ���
        /// </summary>
        public static List<Dungeon> GetByLevelRange(int minLevel, int maxLevel)
        {
            return All.Where(d => d.RecommendedLevel >= minLevel && 
                               d.RecommendedLevel <= maxLevel).ToList();
        }

        /// <summary>
        /// ��ȡָ����������ɽ���ĸ���
        /// </summary>
        public static List<Dungeon> GetByPlayerCount(int playerCount)
        {
            return All.Where(d => d.MinPlayers <= playerCount && 
                               d.MaxPlayers >= playerCount).ToList();
        }

        /// <summary>
        /// ��鸱�������õĹ����Ƿ���Ч
        /// </summary>
        public static bool ValidateMonsterReferences(Dungeon dungeon)
        {
            foreach (var wave in dungeon.Waves)
            {
                foreach (var enemySpawn in wave.Enemies)
                {
                    if (MonsterTemplates.GetByName(enemySpawn.EnemyTemplateName) == null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// ��ȡָ�����������й�������
        /// </summary>
        public static List<Enemy> GetAllMonsterTemplatesInDungeon(string dungeonId)
        {
            var dungeon = GetDungeonById(dungeonId);
            if (dungeon == null)
                return new List<Enemy>();

            var monsterNames = new HashSet<string>();
            
            foreach (var wave in dungeon.Waves)
            {
                foreach (var enemySpawn in wave.Enemies)
                {
                    monsterNames.Add(enemySpawn.EnemyTemplateName);
                }
            }

            return monsterNames
                .Select(name => MonsterTemplates.GetByName(name))
                .Where(m => m != null)
                .Cast<Enemy>()
                .ToList();
        }
    }
}