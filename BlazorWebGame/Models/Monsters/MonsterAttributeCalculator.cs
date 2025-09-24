using BlazorWebGame.GameConfig;
using System;
using System.Collections.Generic;

namespace BlazorWebGame.Models.Monsters
{
    /// <summary>
    /// 怪物属性计算器
    /// </summary>
    public static class MonsterAttributeCalculator
    {
        /// <summary>
        /// 随机数生成器
        /// </summary>
        private static readonly Random Random = new Random();

        /// <summary>
        /// 根据等级和价值分配生成怪物模板
        /// </summary>
        /// <param name="level">怪物等级</param>
        /// <param name="expRatio">经验占比(0-1)</param>
        /// <param name="lootRatio">掉落物占比(0-1)</param>
        /// <param name="monsterType">怪物类型</param>
        /// <param name="predefinedEnemy">预定义的怪物实例(可选)</param>
        /// <returns>生成的怪物实例</returns>
        public static Enemy GenerateMonster(int level, double expRatio, double lootRatio, 
            MonsterType monsterType = MonsterType.Normal, Enemy? predefinedEnemy = null)
        {
            // 验证输入参数
            if (level < 1) level = 1;
            expRatio = Math.Clamp(expRatio, 0, 1);
            lootRatio = Math.Clamp(lootRatio, 0, 1);
            
            // 确保比例总和不超过1
            if (expRatio + lootRatio > 1)
            {
                double total = expRatio + lootRatio;
                expRatio /= total;
                lootRatio /= total;
            }
            
            // 使用预定义的怪物或创建新的怪物实例
            var monster = predefinedEnemy ?? new Enemy();
            
            // 设置基本属性(如果未预定义)
            if (monster.Level <= 0) monster.Level = level;
            if (monster.Type == MonsterType.Normal && monsterType != MonsterType.Normal) monster.Type = monsterType;
            
            // 计算怪物基础价值
            double baseValue = GetBaseMonsterValue(monster.Type);
            
            // 应用等级调整
            double leveledValue = CalculateLeveledMonsterValue(monster.Level, baseValue);
            
            // 分配各项价值
            double expValue = leveledValue * (expRatio + GetRandomVariance(0.05));
            double lootValue = leveledValue * (lootRatio + GetRandomVariance(0.01));
            double goldValue = leveledValue * (1 - expRatio - lootRatio + GetRandomVariance(0.05));
            
            // 保存物品价值
            monster.ItemValue = lootValue;
            
            // 计算战斗属性(如果未预定义)
            if (monster.Health <= 0 || monster.MaxHealth <= 0)
            {
                int health = CalculateMonsterHealth(monster.Level);
                monster.Health = health;
                monster.MaxHealth = health;
            }
            
            if (monster.AttackPower <= 0)
            {
                monster.AttackPower = CalculateMonsterAttackPower(monster.Level);
            }
            
            if (monster.AttacksPerSecond <= 0)
            {
                monster.AttacksPerSecond = CalculateMonsterAttackSpeed(monster.Type);
            }
            
            // 设置奖励(如果未预定义)
            if (monster.XpReward <= 0)
            {
                monster.XpReward = (int)Math.Round(expValue);
            }
            
            if (monster.MinGold <= 0 || monster.MaxGold <= 0)
            {
                monster.MinGold = (int)Math.Floor(goldValue * 0.8);
                monster.MaxGold = (int)Math.Ceiling(goldValue * 1.2);
            }
            
            // 初始化其他属性(如果未预定义)
            InitializeMonsterAttributes(monster, lootValue);
            
            return monster;
        }
        
        /// <summary>
        /// 获取怪物类型对应的基础价值
        /// </summary>
        private static double GetBaseMonsterValue(MonsterType type)
        {
            return type switch
            {
                MonsterType.Normal => 120,
                MonsterType.Elite => 300,
                MonsterType.Boss => 720,
                _ => 120
            };
        }
        
        /// <summary>
        /// 计算带等级调整的怪物价值
        /// </summary>
        private static double CalculateLeveledMonsterValue(int level, double baseValue)
        {
            // 应用怪物价值随等级的增长公式
            double expNeededForLevel = ExpSystem.GetExpRequiredForLevel(level + 1) - ExpSystem.GetExpRequiredForLevel(level);
            double offlineSecondsPerSecond = expNeededForLevel / 150;
            
            return baseValue * offlineSecondsPerSecond;
        }
        
        /// <summary>
        /// 计算怪物生命值
        /// </summary>
        private static int CalculateMonsterHealth(int level)
        {
            // 计算当前等级玩家的DPS
            double playerDPS = CalculatePlayerDPS(level);
            
            // 怪物血量 = 玩家DPS × 120秒（2分钟击杀时间）
            return (int)Math.Round(playerDPS * 120);
        }
        
        /// <summary>
        /// 计算怪物攻击力
        /// </summary>
        private static int CalculateMonsterAttackPower(int level)
        {
            // 计算当前等级玩家的生命值
            int playerHealth = CalculatePlayerHealth(level);
            
            // 攻击力设计为让玩家4-5次被击败
            int hitsToKill = Random.Next(4, 6);
            
            return playerHealth / hitsToKill;
        }
        
        /// <summary>
        /// 计算怪物攻击速度
        /// </summary>
        private static double CalculateMonsterAttackSpeed(MonsterType type)
        {
            // 基础攻速约为3
            double baseSpeed = 3.0;
            
            // 根据怪物类型调整
            return type switch
            {
                MonsterType.Normal => baseSpeed * (0.8 + Random.NextDouble() * 0.4), // 2.4-3.6
                MonsterType.Elite => baseSpeed * (0.9 + Random.NextDouble() * 0.3),  // 2.7-3.6
                MonsterType.Boss => baseSpeed * (0.7 + Random.NextDouble() * 0.3),   // 2.1-3.0
                _ => baseSpeed
            };
        }
        
        /// <summary>
        /// 计算当前等级玩家的DPS(每秒伤害)
        /// </summary>
        private static double CalculatePlayerDPS(int level)
        {
            // 计算主属性
            int mainAttribute = CalculatePlayerMainAttribute(level);
            
            // 武器DPS计算
            double weaponDPS = AttributeSystemConfig.BaseWeaponDPS * 
                Math.Pow(AttributeSystemConfig.WeaponDPSLevelMultiplier, level - 1);
                
            // 从主属性转换为攻击力
            double attackPower = mainAttribute * AttributeSystemConfig.MainAttributeToAPRatio;
            
            // 计算伤害倍率
            double damageMultiplier = 1.0 + mainAttribute * AttributeSystemConfig.MainAttributeToDamageMultiplier;
            
            // 最终DPS计算
            return weaponDPS * (1 + attackPower * AttributeSystemConfig.APToDPSRatio) * damageMultiplier;
        }
        
        /// <summary>
        /// 计算当前等级玩家的生命值
        /// </summary>
        private static int CalculatePlayerHealth(int level)
        {
            // 计算玩家耐力值
            int stamina = CalculatePlayerStamina(level);
            
            // 计算基础生命值
            int baseHealth = AttributeSystemConfig.BaseHealth;
            
            // 从耐力转换为生命值
            return baseHealth + (int)(stamina * AttributeSystemConfig.StaminaToHealthRatio);
        }
        
        /// <summary>
        /// 计算当前等级玩家的主属性值
        /// </summary>
        private static int CalculatePlayerMainAttribute(int level)
        {
            int baseAttr = AttributeSystemConfig.BaseMainAttribute;
            int threshold = AttributeSystemConfig.LevelThreshold;
            
            if (level <= 1)
                return baseAttr;
                
            if (level <= threshold)
            {
                return baseAttr + (level - 1) * AttributeSystemConfig.LowLevelMainAttributeGrowth;
            }
            else
            {
                return baseAttr + 
                    (threshold - 1) * AttributeSystemConfig.LowLevelMainAttributeGrowth +
                    (level - threshold) * AttributeSystemConfig.HighLevelMainAttributeGrowth;
            }
        }
        
        /// <summary>
        /// 计算当前等级玩家的耐力值
        /// </summary>
        private static int CalculatePlayerStamina(int level)
        {
            int baseStamina = AttributeSystemConfig.BaseStamina;
            int threshold = AttributeSystemConfig.LevelThreshold;
            
            if (level <= 1)
                return baseStamina;
                
            if (level <= threshold)
            {
                return baseStamina + (level - 1) * AttributeSystemConfig.LowLevelStaminaGrowth;
            }
            else
            {
                return baseStamina + 
                    (threshold - 1) * AttributeSystemConfig.LowLevelStaminaGrowth +
                    (level - threshold) * AttributeSystemConfig.HighLevelStaminaGrowth;
            }
        }
        
        /// <summary>
        /// 获取随机波动值
        /// </summary>
        private static double GetRandomVariance(double maxVariance)
        {
            return (Random.NextDouble() * 2 - 1) * maxVariance;
        }
        
        /// <summary>
        /// 初始化怪物的其他属性
        /// </summary>
        private static void InitializeMonsterAttributes(Enemy monster, double lootValue)
        {
            // 只在未设置种族时随机选择
            if (monster.Race == MonsterRace.Humanoid && Random.Next(5) != 0) // 80%概率更换默认种族
            {
                monster.Race = GetRandomMonsterRace();
            }
            
            // 只在未设置名称时生成
            if (string.IsNullOrEmpty(monster.Name) || monster.Name == "未知生物")
            {
                monster.Name = GenerateMonsterName(monster);
            }
            
            // 只在未设置描述时生成
            if (string.IsNullOrEmpty(monster.Description))
            {
                monster.Description = $"{monster.Level}级{GetRaceDescription(monster.Race)}";
            }
            
            // 初始化战斗属性
            MonsterTemplates.InitializeCombatAttributes(monster);
            
            // 只在未设置掉落表时生成
            if (monster.LootTable == null || monster.LootTable.Count == 0)
            {
                if (lootValue > 0)
                {
                    monster.LootTable = GenerateLootTable(monster.Level, lootValue, monster.Type);
                }
                else
                {
                    monster.LootTable = new Dictionary<string, double>();
                }
            }
        }
        
        /// <summary>
        /// 随机选择怪物种族
        /// </summary>
        private static MonsterRace GetRandomMonsterRace()
        {
            var races = Enum.GetValues<MonsterRace>();
            return races[Random.Next(races.Length)];
        }
        
        /// <summary>
        /// 获取种族描述文本
        /// </summary>
        private static string GetRaceDescription(MonsterRace race)
        {
            return race switch
            {
                MonsterRace.Humanoid => "人型生物",
                MonsterRace.Beast => "野兽",
                MonsterRace.Elemental => "元素生物",
                MonsterRace.Undead => "亡灵",
                MonsterRace.Demon => "恶魔",
                _ => "未知生物"
            };
        }
        
        /// <summary>
        /// 生成怪物名称
        /// </summary>
        private static string GenerateMonsterName(Enemy monster)
        {
            string racePrefix = monster.Race switch
            {
                MonsterRace.Humanoid => new[] { "暴徒", "强盗", "战士", "斗士", "武师" }[Random.Next(5)],
                MonsterRace.Beast => new[] { "野狼", "猛虎", "巨熊", "凶蛇", "猎鹰" }[Random.Next(5)],
                MonsterRace.Elemental => new[] { "火灵", "水元素", "风之精灵", "岩石傀儡", "雷霆使者" }[Random.Next(5)],
                MonsterRace.Undead => new[] { "僵尸", "骷髅", "幽灵", "死灵法师", "亡骨" }[Random.Next(5)],
                MonsterRace.Demon => new[] { "小鬼", "魔爪", "邪眼", "恶魔猎手", "地狱守卫" }[Random.Next(5)],
                _ => "怪物"
            };
            
            string typePrefix = monster.Type switch
            {
                MonsterType.Normal => "",
                MonsterType.Elite => new[] { "精英", "强大的", "危险的", "凶残的" }[Random.Next(4)],
                MonsterType.Boss => new[] { "首领", "霸主", "统治者", "王者" }[Random.Next(4)],
                _ => ""
            };
            
            return string.IsNullOrEmpty(typePrefix) ? racePrefix : $"{typePrefix}{racePrefix}";
        }
        
        /// <summary>
        /// 生成怪物掉落表
        /// </summary>
        private static Dictionary<string, double> GenerateLootTable(int level, double lootValue, MonsterType type)
        {
            var lootTable = new Dictionary<string, double>();
            
            // 简化实现，基于lootValue计算掉落逻辑
            double dropChance = type switch
            {
                MonsterType.Normal => 0.1,
                MonsterType.Elite => 0.3,
                MonsterType.Boss => 0.8,
                _ => 0.1
            };
            
            // 掉落几率基于lootValue调整
            double adjustedChance = Math.Min(dropChance * (lootValue / 100), 1.0);
            
            // 添加普通品质物品
            lootTable.Add($"item_{level}_common", adjustedChance);
            
            // 精英和Boss有机会掉落更好的物品
            if (type == MonsterType.Elite || type == MonsterType.Boss)
            {
                lootTable.Add($"item_{level}_uncommon", adjustedChance * 0.5);
            }
            
            if (type == MonsterType.Boss)
            {
                lootTable.Add($"item_{level}_rare", adjustedChance * 0.2);
            }
            
            return lootTable;
        }
    }
}