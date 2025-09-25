using System.Collections.Generic;
using System.Linq;
using BlazorWebGame.Models.Monsters;

namespace BlazorWebGame.Models.Dungeons
{
    /// <summary>
    /// 提供所有可用副本模板的管理类
    /// </summary>
    public static class DungeonTemplates
    {
        /// <summary>
        /// 获取所有可用副本
        /// </summary>
        public static List<Dungeon> All =>
            BasicDungeons.Dungeons
            .Concat(EliteDungeons.Dungeons)
            .ToList();

        /// <summary>
        /// 通过ID获取副本
        /// </summary>
        public static Dungeon? GetDungeonById(string id)
        {
            return All.FirstOrDefault(d => d.Id == id);
        }

        /// <summary>
        /// 获取指定等级范围的副本
        /// </summary>
        public static List<Dungeon> GetByLevelRange(int minLevel, int maxLevel)
        {
            return All.Where(d => d.RecommendedLevel >= minLevel && 
                               d.RecommendedLevel <= maxLevel).ToList();
        }

        /// <summary>
        /// 获取指定玩家数量可进入的副本
        /// </summary>
        public static List<Dungeon> GetByPlayerCount(int playerCount)
        {
            return All.Where(d => d.MinPlayers <= playerCount && 
                               d.MaxPlayers >= playerCount).ToList();
        }

        /// <summary>
        /// 检查副本中引用的怪物是否有效
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
        /// 获取指定副本的所有怪物类型
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