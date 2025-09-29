using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 敌人领域模型
/// </summary>
public class Enemy
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public EnemyType Type { get; private set; } = EnemyType.Normal;
    public EnemyRace Race { get; private set; } = EnemyRace.Humanoid;
    public int Level { get; private set; } = 1;
    public int Health { get; private set; } = 100;
    public int Mana { get; private set; } = 100;
    public BattleCombatStats CombatStats { get; private set; } = new();
    
    // 奖励信息
    public EnemyRewards Rewards { get; private set; } = new();
    
    // 掉落表
    public EnemyLootTable LootTable { get; private set; } = new();
    
    // 技能列表
    private readonly List<string> _skills = new();
    public IReadOnlyList<string> Skills => _skills.AsReadOnly();
    
    // AI行为
    public EnemyAI AI { get; private set; } = new();

    // 私有构造函数，用于反序列化
    private Enemy() { }

    /// <summary>
    /// 创建新敌人
    /// </summary>
    public Enemy(string name, string description, int level = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("敌人名称不能为空", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Level = Math.Max(1, level);
        
        // 根据等级设置基础属性
        SetBaseStats();
    }

    /// <summary>
    /// 设置敌人类型和种族
    /// </summary>
    public void SetTypeAndRace(EnemyType type, EnemyRace race)
    {
        Type = type;
        Race = race;
        
        // 根据类型调整属性
        ApplyTypeModifiers();
    }

    /// <summary>
    /// 设置基础属性
    /// </summary>
    private void SetBaseStats()
    {
        Health = 100 + (Level * 25);
        Mana = 50 + (Level * 10);
        
        CombatStats.AttackPower = 10 + (Level * 3);
        CombatStats.AttacksPerSecond = 1.0;
        CombatStats.CriticalChance = 0.05;
        CombatStats.CriticalMultiplier = 1.5;
        CombatStats.DodgeChance = 0.05;
        CombatStats.AccuracyRating = 95 + Level;
        CombatStats.ArmorValue = Level * 2;
        
        // 根据等级设置基础奖励
        Rewards.ExperienceReward = Level * 10;
        Rewards.MinGoldReward = Level;
        Rewards.MaxGoldReward = Level * 3;
    }

    /// <summary>
    /// 应用类型修正
    /// </summary>
    private void ApplyTypeModifiers()
    {
        switch (Type)
        {
            case EnemyType.Elite:
                Health = (int)(Health * 1.5f);
                CombatStats.AttackPower = (int)(CombatStats.AttackPower * 1.3f);
                Rewards.ExperienceReward = (int)(Rewards.ExperienceReward * 1.5f);
                Rewards.MaxGoldReward = (int)(Rewards.MaxGoldReward * 1.5f);
                break;
                
            case EnemyType.Boss:
                Health = (int)(Health * 3.0f);
                Mana = (int)(Mana * 2.0f);
                CombatStats.AttackPower = (int)(CombatStats.AttackPower * 2.0f);
                CombatStats.ArmorValue = (int)(CombatStats.ArmorValue * 1.5f);
                Rewards.ExperienceReward = (int)(Rewards.ExperienceReward * 3.0f);
                Rewards.MinGoldReward = (int)(Rewards.MinGoldReward * 2.0f);
                Rewards.MaxGoldReward = (int)(Rewards.MaxGoldReward * 4.0f);
                break;
                
            case EnemyType.Minion:
                Health = (int)(Health * 0.7f);
                CombatStats.AttackPower = (int)(CombatStats.AttackPower * 0.8f);
                Rewards.ExperienceReward = (int)(Rewards.ExperienceReward * 0.7f);
                Rewards.MaxGoldReward = (int)(Rewards.MaxGoldReward * 0.8f);
                break;
        }

        // 种族修正
        switch (Race)
        {
            case EnemyRace.Undead:
                CombatStats.ElementalResistances["Holy"] = 0.5;
                CombatStats.ElementalResistances["Dark"] = -0.3; // 负数表示免疫
                break;
                
            case EnemyRace.Elemental:
                CombatStats.ElementalResistances["Fire"] = -0.2;
                CombatStats.ElementalResistances["Ice"] = -0.2;
                CombatStats.ElementalResistances["Lightning"] = -0.2;
                break;
                
            case EnemyRace.Dragon:
                Health = (int)(Health * 1.2f);
                CombatStats.ArmorValue = (int)(CombatStats.ArmorValue * 1.3f);
                CombatStats.ElementalResistances["Fire"] = -0.5;
                break;
        }
    }

    /// <summary>
    /// 添加技能
    /// </summary>
    public void AddSkill(string skillId)
    {
        if (!string.IsNullOrWhiteSpace(skillId) && !_skills.Contains(skillId))
        {
            _skills.Add(skillId);
        }
    }

    /// <summary>
    /// 移除技能
    /// </summary>
    public void RemoveSkill(string skillId)
    {
        _skills.Remove(skillId);
    }

    /// <summary>
    /// 设置AI行为
    /// </summary>
    public void SetAI(EnemyAIBehavior behavior, double aggressionLevel = 0.5)
    {
        AI.Behavior = behavior;
        AI.AggressionLevel = Math.Clamp(aggressionLevel, 0.0, 1.0);
    }

    /// <summary>
    /// 生成掉落物品
    /// </summary>
    public List<LootDrop> GenerateLoot(Random random)
    {
        return LootTable.GenerateLoot(random);
    }

    /// <summary>
    /// 克隆战斗属性（用于战斗实例）
    /// </summary>
    public BattleCombatStats CloneCombatStats()
    {
        return new BattleCombatStats
        {
            AttackPower = CombatStats.AttackPower,
            AttacksPerSecond = CombatStats.AttacksPerSecond,
            AttackCooldown = CombatStats.AttackCooldown,
            CriticalChance = CombatStats.CriticalChance,
            CriticalMultiplier = CombatStats.CriticalMultiplier,
            DodgeChance = CombatStats.DodgeChance,
            AccuracyRating = CombatStats.AccuracyRating,
            ArmorValue = CombatStats.ArmorValue,
            BlockChance = CombatStats.BlockChance,
            ElementalResistances = new Dictionary<string, double>(CombatStats.ElementalResistances)
        };
    }
}

/// <summary>
/// 敌人奖励信息
/// </summary>
public class EnemyRewards
{
    public int ExperienceReward { get; set; } = 10;
    public int MinGoldReward { get; set; } = 1;
    public int MaxGoldReward { get; set; } = 5;
    public Dictionary<string, int> ProfessionExperience { get; set; } = new(); // profession -> xp

    /// <summary>
    /// 设置职业经验奖励
    /// </summary>
    public void SetProfessionExperience(string profession, int experience)
    {
        ProfessionExperience[profession] = Math.Max(0, experience);
    }

    /// <summary>
    /// 获取职业经验奖励
    /// </summary>
    public int GetProfessionExperience(string profession)
    {
        return ProfessionExperience.GetValueOrDefault(profession, 0);
    }

    /// <summary>
    /// 生成金币奖励
    /// </summary>
    public int GenerateGoldReward(Random random)
    {
        if (MinGoldReward >= MaxGoldReward)
            return MinGoldReward;
        
        return random.Next(MinGoldReward, MaxGoldReward + 1);
    }
}

/// <summary>
/// 敌人掉落表
/// </summary>
public class EnemyLootTable
{
    private readonly List<LootEntry> _lootEntries = new();
    public IReadOnlyList<LootEntry> LootEntries => _lootEntries.AsReadOnly();

    /// <summary>
    /// 添加掉落项
    /// </summary>
    public void AddLootEntry(string itemId, double dropChance, int minQuantity = 1, int maxQuantity = 1, ItemRarity rarity = ItemRarity.Common)
    {
        var entry = new LootEntry(itemId, dropChance, minQuantity, maxQuantity, rarity);
        _lootEntries.Add(entry);
    }

    /// <summary>
    /// 移除掉落项
    /// </summary>
    public void RemoveLootEntry(string itemId)
    {
        _lootEntries.RemoveAll(entry => entry.ItemId == itemId);
    }

    /// <summary>
    /// 生成掉落物品
    /// </summary>
    public List<LootDrop> GenerateLoot(Random random)
    {
        var drops = new List<LootDrop>();

        foreach (var entry in _lootEntries)
        {
            if (random.NextDouble() < entry.DropChance)
            {
                var quantity = entry.MinQuantity == entry.MaxQuantity 
                    ? entry.MinQuantity 
                    : random.Next(entry.MinQuantity, entry.MaxQuantity + 1);
                
                drops.Add(new LootDrop(entry.ItemId, quantity, entry.Rarity));
            }
        }

        return drops;
    }

    /// <summary>
    /// 清除所有掉落项
    /// </summary>
    public void ClearLootEntries()
    {
        _lootEntries.Clear();
    }
}

/// <summary>
/// 掉落条目
/// </summary>
public class LootEntry
{
    public string ItemId { get; private set; } = string.Empty;
    public double DropChance { get; private set; } = 0.1; // 10%
    public int MinQuantity { get; private set; } = 1;
    public int MaxQuantity { get; private set; } = 1;
    public ItemRarity Rarity { get; private set; } = ItemRarity.Common;

    // 私有构造函数，用于反序列化
    private LootEntry() { }

    /// <summary>
    /// 创建掉落条目
    /// </summary>
    public LootEntry(string itemId, double dropChance, int minQuantity = 1, int maxQuantity = 1, ItemRarity rarity = ItemRarity.Common)
    {
        ItemId = itemId;
        DropChance = Math.Clamp(dropChance, 0.0, 1.0);
        MinQuantity = Math.Max(1, minQuantity);
        MaxQuantity = Math.Max(minQuantity, maxQuantity);
        Rarity = rarity;
    }
}

/// <summary>
/// 敌人AI
/// </summary>
public class EnemyAI
{
    public EnemyAIBehavior Behavior { get; set; } = EnemyAIBehavior.Aggressive;
    public double AggressionLevel { get; set; } = 0.5; // 0.0 = 完全被动, 1.0 = 极度主动
    public double SkillUsageChance { get; set; } = 0.3; // 使用技能的概率
    public string PreferredTargetType { get; set; } = "Nearest"; // Nearest, Weakest, Strongest, Random
    public Dictionary<string, object> BehaviorParameters { get; set; } = new();

    /// <summary>
    /// 选择目标
    /// </summary>
    public string? SelectTarget(List<BattleParticipant> possibleTargets, Random random)
    {
        if (possibleTargets == null || possibleTargets.Count == 0)
            return null;

        var aliveTargets = possibleTargets.Where(t => t.IsAlive).ToList();
        if (aliveTargets.Count == 0)
            return null;

        return PreferredTargetType switch
        {
            "Weakest" => aliveTargets.OrderBy(t => t.Health).First().Id,
            "Strongest" => aliveTargets.OrderByDescending(t => t.Health).First().Id,
            "Random" => aliveTargets[random.Next(aliveTargets.Count)].Id,
            _ => aliveTargets.First().Id // Default to first (nearest)
        };
    }

    /// <summary>
    /// 决定是否使用技能
    /// </summary>
    public bool ShouldUseSkill(Random random)
    {
        return random.NextDouble() < SkillUsageChance;
    }

    /// <summary>
    /// 选择技能
    /// </summary>
    public string? SelectSkill(List<string> availableSkills, Random random)
    {
        if (availableSkills == null || availableSkills.Count == 0)
            return null;

        // 简单随机选择，可以根据需要实现更复杂的逻辑
        return availableSkills[random.Next(availableSkills.Count)];
    }
}

/// <summary>
/// 敌人类型枚举
/// </summary>
public enum EnemyType
{
    Normal,     // 普通怪物
    Minion,     // 小怪
    Elite,      // 精英怪物
    Boss,       // Boss
    Rare        // 稀有怪物
}

/// <summary>
/// 敌人种族枚举
/// </summary>
public enum EnemyRace
{
    Humanoid,   // 人形
    Beast,      // 野兽
    Undead,     // 亡灵
    Elemental,  // 元素
    Dragon,     // 龙族
    Demon,      // 恶魔
    Angel,      // 天使
    Plant,      // 植物
    Construct,  // 构造体
    Aberration  // 异形
}

/// <summary>
/// 敌人AI行为枚举
/// </summary>
public enum EnemyAIBehavior
{
    Passive,        // 被动：不主动攻击
    Defensive,      // 防御：受到攻击才反击
    Aggressive,     // 主动：主动攻击玩家
    Berserk,        // 狂暴：低血量时变得更加危险
    Tactical,       // 战术：使用复杂的战斗策略
    Support,        // 支援：优先支援其他敌人
    Coward,         // 胆小：低血量时尝试逃跑
    Guardian        // 守卫：保护特定目标或区域
}

/// <summary>
/// 敌人生成器
/// </summary>
public static class EnemyGenerator
{
    /// <summary>
    /// 创建基础敌人模板
    /// </summary>
    public static Enemy CreateBasicEnemy(string name, int level, EnemyType type = EnemyType.Normal, EnemyRace race = EnemyRace.Humanoid)
    {
        var enemy = new Enemy(name, $"等级 {level} 的 {name}", level);
        enemy.SetTypeAndRace(type, race);
        return enemy;
    }

    /// <summary>
    /// 创建随机敌人
    /// </summary>
    public static Enemy CreateRandomEnemy(int level, Random random, List<string> namePool)
    {
        var name = namePool[random.Next(namePool.Count)];
        var type = (EnemyType)random.Next(Enum.GetValues<EnemyType>().Length);
        var race = (EnemyRace)random.Next(Enum.GetValues<EnemyRace>().Length);
        
        return CreateBasicEnemy(name, level, type, race);
    }

    /// <summary>
    /// 创建副本Boss
    /// </summary>
    public static Enemy CreateDungeonBoss(string name, int level, List<string> skills)
    {
        var boss = CreateBasicEnemy(name, level, EnemyType.Boss, EnemyRace.Dragon);
        
        // 添加技能
        foreach (var skill in skills)
        {
            boss.AddSkill(skill);
        }
        
        // 设置更智能的AI
        boss.SetAI(EnemyAIBehavior.Tactical, 0.8);
        
        // 添加特殊掉落
        boss.LootTable.AddLootEntry("rare_gem", 0.5, 1, 3, ItemRarity.Rare);
        boss.LootTable.AddLootEntry("boss_equipment", 0.2, 1, 1, ItemRarity.Epic);
        
        return boss;
    }
}