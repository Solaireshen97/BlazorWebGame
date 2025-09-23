using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Monsters
{
    /// <summary>
    /// 怪物类型枚举
    /// </summary>
    public enum MonsterType
    {
        Normal,     // 普通怪物
        Elite,      // 精英怪物
        Boss        // 首领怪物
    }

    /// <summary>
    /// 怪物所属种族
    /// </summary>
    public enum MonsterRace
    {
        Humanoid,   // 人型生物 (如哥布林)
        Beast,      // 野兽
        Elemental,  // 元素生物
        Undead,     // 亡灵
        Demon       // 恶魔
    }

    /// <summary>
    /// 提供对所有怪物模板的访问
    /// </summary>
    public static class MonsterTemplates
    {
        /// <summary>
        /// 获取所有怪物模板
        /// </summary>
        public static List<Enemy> All =>
            NormalMonsters.Monsters
            .Concat(EliteMonsters.Monsters)
            .Concat(BossMonsters.Monsters)
            .ToList();

        /// <summary>
        /// 根据名称查找怪物模板
        /// </summary>
        public static Enemy? GetByName(string name) =>
            All.FirstOrDefault(m => m.Name == name);

        /// <summary>
        /// 获取特定类型的所有怪物
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
        /// 获取特定等级范围的怪物
        /// </summary>
        public static List<Enemy> GetByLevelRange(int minLevel, int maxLevel)
        {
            return All.Where(m => m.Level >= minLevel && m.Level <= maxLevel).ToList();
        }
    }
}