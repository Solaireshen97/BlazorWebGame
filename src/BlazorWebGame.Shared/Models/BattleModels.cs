using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 战斗领域模型
/// </summary>
public class Battle
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public BattleType Type { get; private set; } = BattleType.Normal;
    public BattleStatus Status { get; private set; } = BattleStatus.Preparing;
    public DateTime StartTime { get; private set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; private set; }
    public Guid? PartyId { get; private set; }
    public string? DungeonId { get; private set; }
    public int WaveNumber { get; private set; } = 0;
    public bool AllowAutoRevive { get; private set; } = false;
    // 新增：战斗实例引用
    public BattleInstance? Instance { get; private set; }

    // 新增：区域修饰符
    public string? RegionId { get; private set; }
    public RegionModifiers? RegionModifiers { get; private set; }

    // 战斗参与者
    private readonly List<BattleParticipant> _participants = new();
    public IReadOnlyList<BattleParticipant> Participants => _participants.AsReadOnly();

    // 战斗日志
    private readonly List<CombatSegment> _segments = new();
    public IReadOnlyList<CombatSegment> Segments => _segments.AsReadOnly();

    // 战斗结果
    public BattleResult? Result { get; private set; }

    // 私有构造函数，用于反序列化
    private Battle() { }

    /// <summary>
    /// 创建新战斗
    /// </summary>
    public Battle(BattleType type, GameClock clock, string? regionId = null,
                  Guid? partyId = null, string? dungeonId = null)
    {
        Type = type;
        PartyId = partyId;
        DungeonId = dungeonId;
        RegionId = regionId;
        StartTime = clock.CurrentTime;

        // 创建战斗实例
        var seed = $"{Id}_{DateTime.UtcNow.Ticks}";
        Instance = new BattleInstance(type.ToString(), clock, seed);
    }

    /// <summary>
    /// 设置区域修饰符
    /// </summary>
    public void SetRegionModifiers(RegionModifiers modifiers)
    {
        RegionModifiers = modifiers;
    }

    /// <summary>
    /// 获取最近的片段
    /// </summary>
    public CombatSegment? GetLatestSegment()
    {
        return _segments.LastOrDefault();
    }

    /// <summary>
    /// 计算总伤害输出
    /// </summary>
    public Dictionary<string, double> GetTotalDamageBySource()
    {
        var totalDamage = new Dictionary<string, double>();

        foreach (var segment in _segments)
        {
            foreach (var kvp in segment.DamageBySource)
            {
                if (!totalDamage.ContainsKey(kvp.Key))
                    totalDamage[kvp.Key] = 0;
                totalDamage[kvp.Key] += kvp.Value;
            }
        }

        return totalDamage;
    }

    /// <summary>
    /// 添加参与者
    /// </summary>
    public bool AddParticipant(BattleParticipant participant)
    {
        if (participant == null || _participants.Any(p => p.Id == participant.Id))
            return false;

        _participants.Add(participant);
        return true;
    }

    /// <summary>
    /// 移除参与者
    /// </summary>
    public bool RemoveParticipant(string participantId)
    {
        var participant = _participants.FirstOrDefault(p => p.Id == participantId);
        if (participant != null)
        {
            return _participants.Remove(participant);
        }
        return false;
    }

    /// <summary>
    /// 开始战斗
    /// </summary>
    public bool StartBattle()
    {
        if (Status != BattleStatus.Preparing)
            return false;

        if (!HasValidParticipants())
            return false;

        Status = BattleStatus.Active;
        StartTime = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// 暂停战斗
    /// </summary>
    public bool PauseBattle()
    {
        if (Status != BattleStatus.Active)
            return false;

        Status = BattleStatus.Paused;
        return true;
    }

    /// <summary>
    /// 恢复战斗
    /// </summary>
    public bool ResumeBattle()
    {
        if (Status != BattleStatus.Paused)
            return false;

        Status = BattleStatus.Active;
        return true;
    }

    /// <summary>
    /// 结束战斗
    /// </summary>
    public bool EndBattle(bool isVictory, BattleResult result)
    {
        if (Status != BattleStatus.Active && Status != BattleStatus.Paused)
            return false;

        Status = BattleStatus.Completed;
        EndTime = DateTime.UtcNow;
        Result = result;
        return true;
    }

    /// <summary>
    /// 取消战斗
    /// </summary>
    public bool CancelBattle()
    {
        if (Status == BattleStatus.Completed)
            return false;

        Status = BattleStatus.Cancelled;
        EndTime = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// 检查是否有有效参与者
    /// </summary>
    private bool HasValidParticipants()
    {
        var players = GetPlayerParticipants();
        var enemies = GetEnemyParticipants();
        
        return players.Any(p => p.IsAlive) && enemies.Any(e => e.IsAlive);
    }

    /// <summary>
    /// 获取玩家参与者
    /// </summary>
    public List<BattleParticipant> GetPlayerParticipants()
    {
        return _participants.Where(p => p.IsPlayer).ToList();
    }

    /// <summary>
    /// 获取敌人参与者
    /// </summary>
    public List<BattleParticipant> GetEnemyParticipants()
    {
        return _participants.Where(p => !p.IsPlayer).ToList();
    }

    /// <summary>
    /// 获取存活的参与者
    /// </summary>
    public List<BattleParticipant> GetAliveParticipants()
    {
        return _participants.Where(p => p.IsAlive).ToList();
    }

    /// <summary>
    /// 获取战斗持续时间
    /// </summary>
    public TimeSpan GetDuration()
    {
        var endTime = EndTime ?? DateTime.UtcNow;
        return endTime - StartTime;
    }

    /// <summary>
    /// 检查战斗是否结束
    /// </summary>
    public bool IsFinished()
    {
        if (Status == BattleStatus.Completed || Status == BattleStatus.Cancelled)
            return true;

        var alivePlayers = GetPlayerParticipants().Count(p => p.IsAlive);
        var aliveEnemies = GetEnemyParticipants().Count(p => p.IsAlive);

        return alivePlayers == 0 || aliveEnemies == 0;
    }

    /// <summary>
    /// 检查玩家是否获胜
    /// </summary>
    public bool IsPlayerVictory()
    {
        if (!IsFinished()) return false;
        return GetEnemyParticipants().All(e => !e.IsAlive);
    }

    /// <summary>
    /// 设置下一波敌人（副本模式）
    /// </summary>
    public void SetNextWave(int waveNumber)
    {
        WaveNumber = waveNumber;
    }
}

/// <summary>
/// 战斗参与者基类
/// </summary>
public abstract class BattleParticipant
{
    public string Id { get; protected set; } = Guid.NewGuid().ToString();
    public string Name { get; protected set; } = string.Empty;
    public int Health { get; protected set; }
    public int MaxHealth { get; protected set; }
    public int Mana { get; protected set; }
    public int MaxMana { get; protected set; }
    public bool IsAlive { get; protected set; } = true;
    public DateTime? DeathTime { get; protected set; }

    // 战斗属性
    public BattleCombatStats CombatStats { get; protected set; } = new();
    
    // 装备的技能
    public List<string> EquippedSkills { get; protected set; } = new();
    
    // 技能冷却
    public Dictionary<string, DateTime> SkillCooldowns { get; protected set; } = new();
    
    // 当前目标
    public string? CurrentTargetId { get; protected set; }

    public abstract bool IsPlayer { get; }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public virtual void TakeDamage(int damage)
    {
        if (!IsAlive) return;

        Health = Math.Max(0, Health - damage);
        if (Health <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    public virtual void Heal(int amount)
    {
        if (!IsAlive) return;
        Health = Math.Min(MaxHealth, Health + amount);
    }

    /// <summary>
    /// 消耗法力值
    /// </summary>
    public virtual bool ConsumeMana(int amount)
    {
        if (Mana < amount) return false;
        Mana -= amount;
        return true;
    }

    /// <summary>
    /// 恢复法力值
    /// </summary>
    public virtual void RestoreMana(int amount)
    {
        Mana = Math.Min(MaxMana, Mana + amount);
    }

    /// <summary>
    /// 死亡
    /// </summary>
    protected virtual void Die()
    {
        IsAlive = false;
        DeathTime = DateTime.UtcNow;
        CurrentTargetId = null;
    }

    /// <summary>
    /// 复活
    /// </summary>
    public virtual void Revive(int healthPercent = 50)
    {
        IsAlive = true;
        DeathTime = null;
        Health = MaxHealth * healthPercent / 100;
        Mana = MaxMana / 2;
    }

    /// <summary>
    /// 设置目标
    /// </summary>
    public virtual void SetTarget(string? targetId)
    {
        CurrentTargetId = targetId;
    }

    /// <summary>
    /// 检查技能是否在冷却中
    /// </summary>
    public bool IsSkillOnCooldown(string skillId)
    {
        if (SkillCooldowns.TryGetValue(skillId, out var endTime))
        {
            return DateTime.UtcNow < endTime;
        }
        return false;
    }

    /// <summary>
    /// 设置技能冷却
    /// </summary>
    public void SetSkillCooldown(string skillId, TimeSpan cooldown)
    {
        SkillCooldowns[skillId] = DateTime.UtcNow.Add(cooldown);
    }
}

/// <summary>
/// 战斗属性统计
/// </summary>
public class BattleCombatStats
{
    public int AttackPower { get; set; } = 10;
    public double AttacksPerSecond { get; set; } = 1.0;
    public double AttackCooldown { get; set; } = 0.0;
    public double CriticalChance { get; set; } = 0.05;
    public double CriticalMultiplier { get; set; } = 1.5;
    public double DodgeChance { get; set; } = 0.0;
    public int AccuracyRating { get; set; } = 100;
    public int ArmorValue { get; set; } = 0;
    public int BlockChance { get; set; } = 0;
    public Dictionary<string, double> ElementalResistances { get; set; } = new();

    /// <summary>
    /// 计算实际伤害
    /// </summary>
    public int CalculateDamage(Random random)
    {
        var damage = AttackPower;
        
        // 检查暴击
        if (random.NextDouble() < CriticalChance)
        {
            damage = (int)(damage * CriticalMultiplier);
        }
        
        return damage;
    }

    /// <summary>
    /// 检查是否命中
    /// </summary>
    public bool CheckHit(Random random, double targetDodgeChance)
    {
        var hitChance = AccuracyRating / 100.0;
        var finalHitChance = hitChance * (1.0 - targetDodgeChance);
        return random.NextDouble() < finalHitChance;
    }

    /// <summary>
    /// 计算受到的伤害（考虑护甲）
    /// </summary>
    public int CalculateReceivedDamage(int incomingDamage, Random random)
    {
        // 检查格挡
        if (BlockChance > 0 && random.Next(100) < BlockChance)
        {
            return 0; // 格挡成功
        }
        
        // 护甲减伤
        var damageReduction = ArmorValue / (ArmorValue + 100.0);
        var finalDamage = (int)(incomingDamage * (1.0 - damageReduction));
        
        return Math.Max(1, finalDamage); // 至少造成1点伤害
    }

    /// <summary>
    /// 克隆战斗属性
    /// </summary>
    public BattleCombatStats Clone()
    {
        return new BattleCombatStats
        {
            AttackPower = AttackPower,
            AttacksPerSecond = AttacksPerSecond,
            AttackCooldown = AttackCooldown,
            CriticalChance = CriticalChance,
            CriticalMultiplier = CriticalMultiplier,
            DodgeChance = DodgeChance,
            AccuracyRating = AccuracyRating,
            ArmorValue = ArmorValue,
            BlockChance = BlockChance,
            ElementalResistances = new Dictionary<string, double>(ElementalResistances)
        };
    }
}

/// <summary>
/// 战斗玩家参与者
/// </summary>
public class BattlePlayer : BattleParticipant
{
    public override bool IsPlayer => true;
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; } = 0;
    public string SelectedProfession { get; private set; } = "Warrior";
    public Guid? PartyId { get; private set; }
    
    // 属性
    public CharacterAttributes Attributes { get; private set; } = new();

    // 私有构造函数，用于反序列化
    private BattlePlayer() { }

    /// <summary>
    /// 创建战斗玩家
    /// </summary>
    public BattlePlayer(string characterId, string name, int level, CharacterAttributes attributes, string profession = "Warrior")
    {
        Id = characterId;
        Name = name;
        Level = level;
        Attributes = attributes;
        SelectedProfession = profession;
        
        // 根据属性计算生命值和法力值
        RecalculateVitals();
    }

    /// <summary>
    /// 重新计算生命值和法力值
    /// </summary>
    public void RecalculateVitals()
    {
        MaxHealth = 100 + (Attributes.Stamina * 10) + (Attributes.Strength * 2);
        MaxMana = 100 + (Attributes.Intellect * 10) + (Attributes.Spirit * 5);
        Health = MaxHealth;
        Mana = MaxMana;
        
        // 更新战斗属性
        CombatStats.AttackPower = 10 + Attributes.Strength + (Level * 2);
        CombatStats.CriticalChance = 0.05 + (Attributes.Agility * 0.001);
        CombatStats.DodgeChance = Attributes.Agility * 0.0005;
    }

    /// <summary>
    /// 设置组队ID
    /// </summary>
    public void SetPartyId(Guid? partyId)
    {
        PartyId = partyId;
    }

    /// <summary>
    /// 获得经验值
    /// </summary>
    public void GainExperience(int amount)
    {
        Experience += amount;
    }
}

/// <summary>
/// 战斗敌人参与者
/// </summary>
public class BattleEnemy : BattleParticipant
{
    public override bool IsPlayer => false;
    public Enemy EnemyData { get; private set; }

    // 私有构造函数，用于反序列化  
    private BattleEnemy() { EnemyData = new Enemy("", ""); }

    /// <summary>
    /// 创建战斗敌人
    /// </summary>
    public BattleEnemy(Enemy enemyData)
    {
        EnemyData = enemyData ?? throw new ArgumentNullException(nameof(enemyData));
        Id = Guid.NewGuid().ToString();
        Name = enemyData.Name;
        Health = enemyData.Health;
        MaxHealth = enemyData.Health;
        Mana = enemyData.Mana;
        MaxMana = enemyData.Mana;
        
        // 复制战斗属性
        CombatStats = enemyData.CombatStats.Clone();
    }

    /// <summary>
    /// 获取掉落奖励
    /// </summary>
    public List<LootDrop> GetLootDrops(Random random)
    {
        return EnemyData.GenerateLoot(random);
    }
}

/// <summary>
/// 战斗动作记录
/// </summary>
public class BattleAction
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string ActorId { get; private set; } = string.Empty;
    public string ActorName { get; private set; } = string.Empty;
    public string? TargetId { get; private set; }
    public string? TargetName { get; private set; }
    public BattleActionType ActionType { get; private set; } = BattleActionType.Attack;
    public string? SkillId { get; private set; }
    public int Damage { get; private set; } = 0;
    public bool IsCritical { get; private set; } = false;
    public bool IsHit { get; private set; } = true;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public Dictionary<string, object> AdditionalData { get; private set; } = new();

    // 私有构造函数，用于反序列化
    private BattleAction() { }

    /// <summary>
    /// 创建战斗动作记录
    /// </summary>
    public BattleAction(string actorId, string actorName, BattleActionType actionType, string? targetId = null, string? targetName = null)
    {
        ActorId = actorId;
        ActorName = actorName;
        TargetId = targetId;
        TargetName = targetName;
        ActionType = actionType;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置技能信息
    /// </summary>
    public void SetSkill(string skillId)
    {
        SkillId = skillId;
    }

    /// <summary>
    /// 设置伤害信息
    /// </summary>
    public void SetDamage(int damage, bool isCritical = false, bool isHit = true)
    {
        Damage = damage;
        IsCritical = isCritical;
        IsHit = isHit;
    }

    /// <summary>
    /// 设置附加数据
    /// </summary>
    public void SetAdditionalData(string key, object value)
    {
        AdditionalData[key] = value;
    }
}

/// <summary>
/// 战斗结果
/// </summary>
public class BattleResult
{
    public bool Victory { get; private set; }
    public int ExperienceGained { get; private set; } = 0;
    public int GoldGained { get; private set; } = 0;
    public List<LootDrop> ItemsLooted { get; private set; } = new();
    public DateTime CompletedAt { get; private set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; private set; }
    public Dictionary<string, object> Statistics { get; private set; } = new();

    // 新增：资源收获
    public Dictionary<string, double> ResourceGains { get; private set; } = new();

    // 新增：职业经验
    public Dictionary<string, int> ProfessionExperience { get; private set; } = new();

    // 新增：声望奖励
    public Dictionary<string, int> ReputationGains { get; private set; } = new();

    // 私有构造函数，用于反序列化
    private BattleResult() { }

    /// <summary>
    /// 创建战斗结果
    /// </summary>
    public BattleResult(bool victory, TimeSpan duration)
    {
        Victory = victory;
        Duration = duration;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置经验奖励
    /// </summary>
    public void SetExperienceReward(int experience)
    {
        ExperienceGained = Math.Max(0, experience);
    }

    /// <summary>
    /// 设置金币奖励
    /// </summary>
    public void SetGoldReward(int gold)
    {
        GoldGained = Math.Max(0, gold);
    }

    /// <summary>
    /// 添加掉落物品
    /// </summary>
    public void AddLootDrop(LootDrop item)
    {
        ItemsLooted.Add(item);
    }

    /// <summary>
    /// 设置统计信息
    /// </summary>
    public void SetStatistic(string key, object value)
    {
        Statistics[key] = value;
    }

    /// <summary>
    /// 应用区域修饰符
    /// </summary>
    public void ApplyRegionModifiers(RegionModifiers? modifiers)
    {
        if (modifiers == null) return;

        ExperienceGained = (int)(ExperienceGained * modifiers.ExperienceMultiplier);
        GoldGained = (int)(GoldGained * modifiers.GoldMultiplier);

        // 应用掉落率修饰
        if (modifiers.DropRateMultiplier > 1.0)
        {
            // 可能增加额外掉落
        }
    }
}

/// <summary>
/// 掉落物品
/// </summary>
public class LootDrop
{
    public string ItemId { get; private set; } = string.Empty;
    public int Quantity { get; private set; } = 1;
    public ItemRarity Rarity { get; private set; } = ItemRarity.Common;

    // 私有构造函数，用于反序列化
    private LootDrop() { }

    /// <summary>
    /// 创建掉落物品
    /// </summary>
    public LootDrop(string itemId, int quantity = 1, ItemRarity rarity = ItemRarity.Common)
    {
        ItemId = itemId;
        Quantity = Math.Max(1, quantity);
        Rarity = rarity;
    }
}

/// <summary>
/// 战斗类型枚举
/// </summary>
public enum BattleType
{
    Normal,         // 普通战斗
    Dungeon,        // 副本战斗
    PvP,           // 玩家对战
    Raid,          // 团队副本
    Event          // 活动战斗
}

/// <summary>
/// 战斗状态枚举
/// </summary>
public enum BattleStatus
{
    Preparing,      // 准备中
    Active,         // 进行中
    Paused,         // 暂停
    Completed,      // 已完成
    Cancelled       // 已取消
}

/// <summary>
/// 战斗动作类型枚举
/// </summary>
public enum BattleActionType
{
    Attack,         // 攻击
    UseSkill,       // 使用技能
    Defend,         // 防御
    Move,           // 移动
    UseItem,        // 使用物品
    Flee,           // 逃跑
    Resurrect       // 复活
}