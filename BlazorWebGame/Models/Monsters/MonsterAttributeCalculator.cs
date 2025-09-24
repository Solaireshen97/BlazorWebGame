using BlazorWebGame.GameConfig;
using System;
using System.Collections.Generic;

namespace BlazorWebGame.Models.Monsters
{
    /// <summary>
    /// �������Լ�����
    /// </summary>
    public static class MonsterAttributeCalculator
    {
        /// <summary>
        /// �����������
        /// </summary>
        private static readonly Random Random = new Random();

        /// <summary>
        /// ���ݵȼ��ͼ�ֵ�������ɹ���ģ��
        /// </summary>
        /// <param name="level">����ȼ�</param>
        /// <param name="expRatio">����ռ��(0-1)</param>
        /// <param name="lootRatio">������ռ��(0-1)</param>
        /// <param name="monsterType">��������</param>
        /// <param name="predefinedEnemy">Ԥ����Ĺ���ʵ��(��ѡ)</param>
        /// <returns>���ɵĹ���ʵ��</returns>
        public static Enemy GenerateMonster(int level, double expRatio, double lootRatio, 
            MonsterType monsterType = MonsterType.Normal, Enemy? predefinedEnemy = null)
        {
            // ��֤�������
            if (level < 1) level = 1;
            expRatio = Math.Clamp(expRatio, 0, 1);
            lootRatio = Math.Clamp(lootRatio, 0, 1);
            
            // ȷ�������ܺͲ�����1
            if (expRatio + lootRatio > 1)
            {
                double total = expRatio + lootRatio;
                expRatio /= total;
                lootRatio /= total;
            }
            
            // ʹ��Ԥ����Ĺ���򴴽��µĹ���ʵ��
            var monster = predefinedEnemy ?? new Enemy();
            
            // ���û�������(���δԤ����)
            if (monster.Level <= 0) monster.Level = level;
            if (monster.Type == MonsterType.Normal && monsterType != MonsterType.Normal) monster.Type = monsterType;
            
            // ������������ֵ
            double baseValue = GetBaseMonsterValue(monster.Type);
            
            // Ӧ�õȼ�����
            double leveledValue = CalculateLeveledMonsterValue(monster.Level, baseValue);
            
            // ��������ֵ
            double expValue = leveledValue * (expRatio + GetRandomVariance(0.05));
            double lootValue = leveledValue * (lootRatio + GetRandomVariance(0.01));
            double goldValue = leveledValue * (1 - expRatio - lootRatio + GetRandomVariance(0.05));
            
            // ������Ʒ��ֵ
            monster.ItemValue = lootValue;
            
            // ����ս������(���δԤ����)
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
            
            // ���ý���(���δԤ����)
            if (monster.XpReward <= 0)
            {
                monster.XpReward = (int)Math.Round(expValue);
            }
            
            if (monster.MinGold <= 0 || monster.MaxGold <= 0)
            {
                monster.MinGold = (int)Math.Floor(goldValue * 0.8);
                monster.MaxGold = (int)Math.Ceiling(goldValue * 1.2);
            }
            
            // ��ʼ����������(���δԤ����)
            InitializeMonsterAttributes(monster, lootValue);
            
            return monster;
        }
        
        /// <summary>
        /// ��ȡ�������Ͷ�Ӧ�Ļ�����ֵ
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
        /// ������ȼ������Ĺ����ֵ
        /// </summary>
        private static double CalculateLeveledMonsterValue(int level, double baseValue)
        {
            // Ӧ�ù����ֵ��ȼ���������ʽ
            double expNeededForLevel = ExpSystem.GetExpRequiredForLevel(level + 1) - ExpSystem.GetExpRequiredForLevel(level);
            double offlineSecondsPerSecond = expNeededForLevel / 150;
            
            return baseValue * offlineSecondsPerSecond;
        }
        
        /// <summary>
        /// �����������ֵ
        /// </summary>
        private static int CalculateMonsterHealth(int level)
        {
            // ���㵱ǰ�ȼ���ҵ�DPS
            double playerDPS = CalculatePlayerDPS(level);
            
            // ����Ѫ�� = ���DPS �� 120�루2���ӻ�ɱʱ�䣩
            return (int)Math.Round(playerDPS * 120);
        }
        
        /// <summary>
        /// ������﹥����
        /// </summary>
        private static int CalculateMonsterAttackPower(int level)
        {
            // ���㵱ǰ�ȼ���ҵ�����ֵ
            int playerHealth = CalculatePlayerHealth(level);
            
            // ���������Ϊ�����4-5�α�����
            int hitsToKill = Random.Next(4, 6);
            
            return playerHealth / hitsToKill;
        }
        
        /// <summary>
        /// ������﹥���ٶ�
        /// </summary>
        private static double CalculateMonsterAttackSpeed(MonsterType type)
        {
            // ��������ԼΪ3
            double baseSpeed = 3.0;
            
            // ���ݹ������͵���
            return type switch
            {
                MonsterType.Normal => baseSpeed * (0.8 + Random.NextDouble() * 0.4), // 2.4-3.6
                MonsterType.Elite => baseSpeed * (0.9 + Random.NextDouble() * 0.3),  // 2.7-3.6
                MonsterType.Boss => baseSpeed * (0.7 + Random.NextDouble() * 0.3),   // 2.1-3.0
                _ => baseSpeed
            };
        }
        
        /// <summary>
        /// ���㵱ǰ�ȼ���ҵ�DPS(ÿ���˺�)
        /// </summary>
        private static double CalculatePlayerDPS(int level)
        {
            // ����������
            int mainAttribute = CalculatePlayerMainAttribute(level);
            
            // ����DPS����
            double weaponDPS = AttributeSystemConfig.BaseWeaponDPS * 
                Math.Pow(AttributeSystemConfig.WeaponDPSLevelMultiplier, level - 1);
                
            // ��������ת��Ϊ������
            double attackPower = mainAttribute * AttributeSystemConfig.MainAttributeToAPRatio;
            
            // �����˺�����
            double damageMultiplier = 1.0 + mainAttribute * AttributeSystemConfig.MainAttributeToDamageMultiplier;
            
            // ����DPS����
            return weaponDPS * (1 + attackPower * AttributeSystemConfig.APToDPSRatio) * damageMultiplier;
        }
        
        /// <summary>
        /// ���㵱ǰ�ȼ���ҵ�����ֵ
        /// </summary>
        private static int CalculatePlayerHealth(int level)
        {
            // �����������ֵ
            int stamina = CalculatePlayerStamina(level);
            
            // �����������ֵ
            int baseHealth = AttributeSystemConfig.BaseHealth;
            
            // ������ת��Ϊ����ֵ
            return baseHealth + (int)(stamina * AttributeSystemConfig.StaminaToHealthRatio);
        }
        
        /// <summary>
        /// ���㵱ǰ�ȼ���ҵ�������ֵ
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
        /// ���㵱ǰ�ȼ���ҵ�����ֵ
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
        /// ��ȡ�������ֵ
        /// </summary>
        private static double GetRandomVariance(double maxVariance)
        {
            return (Random.NextDouble() * 2 - 1) * maxVariance;
        }
        
        /// <summary>
        /// ��ʼ���������������
        /// </summary>
        private static void InitializeMonsterAttributes(Enemy monster, double lootValue)
        {
            // ֻ��δ��������ʱ���ѡ��
            if (monster.Race == MonsterRace.Humanoid && Random.Next(5) != 0) // 80%���ʸ���Ĭ������
            {
                monster.Race = GetRandomMonsterRace();
            }
            
            // ֻ��δ��������ʱ����
            if (string.IsNullOrEmpty(monster.Name) || monster.Name == "δ֪����")
            {
                monster.Name = GenerateMonsterName(monster);
            }
            
            // ֻ��δ��������ʱ����
            if (string.IsNullOrEmpty(monster.Description))
            {
                monster.Description = $"{monster.Level}��{GetRaceDescription(monster.Race)}";
            }
            
            // ��ʼ��ս������
            MonsterTemplates.InitializeCombatAttributes(monster);
            
            // ֻ��δ���õ����ʱ����
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
        /// ���ѡ���������
        /// </summary>
        private static MonsterRace GetRandomMonsterRace()
        {
            var races = Enum.GetValues<MonsterRace>();
            return races[Random.Next(races.Length)];
        }
        
        /// <summary>
        /// ��ȡ���������ı�
        /// </summary>
        private static string GetRaceDescription(MonsterRace race)
        {
            return race switch
            {
                MonsterRace.Humanoid => "��������",
                MonsterRace.Beast => "Ұ��",
                MonsterRace.Elemental => "Ԫ������",
                MonsterRace.Undead => "����",
                MonsterRace.Demon => "��ħ",
                _ => "δ֪����"
            };
        }
        
        /// <summary>
        /// ���ɹ�������
        /// </summary>
        private static string GenerateMonsterName(Enemy monster)
        {
            string racePrefix = monster.Race switch
            {
                MonsterRace.Humanoid => new[] { "��ͽ", "ǿ��", "սʿ", "��ʿ", "��ʦ" }[Random.Next(5)],
                MonsterRace.Beast => new[] { "Ұ��", "�ͻ�", "����", "����", "��ӥ" }[Random.Next(5)],
                MonsterRace.Elemental => new[] { "����", "ˮԪ��", "��֮����", "��ʯ����", "����ʹ��" }[Random.Next(5)],
                MonsterRace.Undead => new[] { "��ʬ", "����", "����", "���鷨ʦ", "����" }[Random.Next(5)],
                MonsterRace.Demon => new[] { "С��", "ħצ", "а��", "��ħ����", "��������" }[Random.Next(5)],
                _ => "����"
            };
            
            string typePrefix = monster.Type switch
            {
                MonsterType.Normal => "",
                MonsterType.Elite => new[] { "��Ӣ", "ǿ���", "Σ�յ�", "�ײе�" }[Random.Next(4)],
                MonsterType.Boss => new[] { "����", "����", "ͳ����", "����" }[Random.Next(4)],
                _ => ""
            };
            
            return string.IsNullOrEmpty(typePrefix) ? racePrefix : $"{typePrefix}{racePrefix}";
        }
        
        /// <summary>
        /// ���ɹ�������
        /// </summary>
        private static Dictionary<string, double> GenerateLootTable(int level, double lootValue, MonsterType type)
        {
            var lootTable = new Dictionary<string, double>();
            
            // ��ʵ�֣�����lootValue��������߼�
            double dropChance = type switch
            {
                MonsterType.Normal => 0.1,
                MonsterType.Elite => 0.3,
                MonsterType.Boss => 0.8,
                _ => 0.1
            };
            
            // ���伸�ʻ���lootValue����
            double adjustedChance = Math.Min(dropChance * (lootValue / 100), 1.0);
            
            // �����ͨƷ����Ʒ
            lootTable.Add($"item_{level}_common", adjustedChance);
            
            // ��Ӣ��Boss�л��������õ���Ʒ
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