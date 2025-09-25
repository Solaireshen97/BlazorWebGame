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

        /// <summary>
        /// 根据怪物类型和等级应用闪避属性
        /// </summary>
        /// <param name="enemy">要应用属性的怪物</param>
        public static void ApplyAvoidanceRating(Enemy enemy)
        {
            // 基于类型和等级设置闪避值
            switch (enemy.Type)
            {
                case MonsterType.Normal:
                    enemy.AvoidanceRating = enemy.Level * 5;
                    enemy.DodgeChance = 0.02; // 2%的闪避率
                    break;
                    
                case MonsterType.Elite:
                    enemy.AvoidanceRating = enemy.Level * 8;
                    enemy.DodgeChance = 0.05; // 5%的闪避率
                    break;
                    
                case MonsterType.Boss:
                    enemy.AvoidanceRating = enemy.Level * 10;
                    enemy.DodgeChance = 0.08; // 8%的闪避率
                    break;
            }
            
            // 基于种族调整闪避属性
            switch (enemy.Race)
            {
                case MonsterRace.Beast:
                    // 野兽更敏捷，增加闪避
                    enemy.DodgeChance += 0.03;
                    enemy.AvoidanceRating += enemy.Level * 2;
                    break;
                    
                case MonsterRace.Elemental:
                    // 元素生物不稳定，有时更难命中
                    enemy.DodgeChance += 0.02;
                    break;
                    
                case MonsterRace.Humanoid:
                    // 人型生物受过训练，有更好的闪避技巧
                    enemy.AvoidanceRating += enemy.Level * 1;
                    break;
            }
        }
        
        /// <summary>
        /// 初始化怪物的所有战斗属性
        /// </summary>
        /// <param name="enemy">要初始化的怪物</param>
        public static void InitializeCombatAttributes(Enemy enemy)
        {
            // 应用闪避属性
            ApplyAvoidanceRating(enemy);
            
            // 应用抗性属性
            ApplyResistances(enemy);
            
            // 应用其他战斗属性
            ApplyAdditionalCombatStats(enemy);
        }
        
        /// <summary>
        /// 应用怪物的元素抗性
        /// </summary>
        private static void ApplyResistances(Enemy enemy)
        {
            // 初始化元素抗性字典
            enemy.ElementalResistances = new Dictionary<ElementType, double>();
            
            // 根据种族设置基础抗性
            switch (enemy.Race)
            {
                case MonsterRace.Elemental:
                    // 元素生物对自身元素有高抗性
                    if (enemy.ElementType != ElementType.None)
                    {
                        enemy.ElementalResistances[enemy.ElementType] = 0.75; // 75%抗性
                    }
                    break;
                    
                case MonsterRace.Undead:
                    // 亡灵抗性火焰弱，抗暗影
                    enemy.ElementalResistances[ElementType.Fire] = -0.25; // -25%抗性（弱点）
                    enemy.ElementalResistances[ElementType.Shadow] = 0.5; // 50%抗性
                    break;
                    
                case MonsterRace.Demon:
                    // 恶魔抗火，弱神圣
                    enemy.ElementalResistances[ElementType.Fire] = 0.5; // 50%抗性
                    enemy.ElementalResistances[ElementType.Holy] = -0.25; // -25%抗性（弱点）
                    break;
            }
            
            // 根据等级和类型增加额外抗性
            if (enemy.Type == MonsterType.Elite || enemy.Type == MonsterType.Boss)
            {
                // 精英和Boss怪有额外的随机抗性
                AddRandomResistance(enemy);
            }
        }
        
        /// <summary>
        /// 为怪物添加一个随机的元素抗性
        /// </summary>
        private static void AddRandomResistance(Enemy enemy)
        {
            var random = new System.Random();
            var elementTypes = System.Enum.GetValues<ElementType>();
            
            // 排除None类型和已有抗性的元素类型
            var availableTypes = elementTypes
                .Where(e => e != ElementType.None && !enemy.ElementalResistances.ContainsKey(e))
                .ToList();
                
            if (availableTypes.Any())
            {
                var selectedType = availableTypes[random.Next(availableTypes.Count)];
                double resistanceValue = 0.3 + (random.NextDouble() * 0.2); // 30%-50%的抗性
                
                enemy.ElementalResistances[selectedType] = resistanceValue;
            }
        }
        
        /// <summary>
        /// 应用额外的战斗属性
        /// </summary>
        private static void ApplyAdditionalCombatStats(Enemy enemy)
        {
            var random = new System.Random();
            
            // 设置命中率 - 基于等级和类型
            enemy.AccuracyRating = enemy.Level * 5;
            
            if (enemy.Type == MonsterType.Elite)
                enemy.AccuracyRating += 10;
            else if (enemy.Type == MonsterType.Boss)
                enemy.AccuracyRating += 20;
                
            // 设置暴击率和暴击伤害
            enemy.CriticalChance = 0.05; // 基础5%暴击
            enemy.CriticalMultiplier = 1.5; // 基础150%暴击伤害
            
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
    
    /// <summary>
    /// 元素类型枚举
    /// </summary>
    public enum ElementType
    {
        None,
        Fire,
        Ice,
        Lightning,
        Nature,
        Shadow,
        Holy
    }
}