using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 游戏角色领域模型
/// </summary>
public class Character
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Name { get; private set; } = string.Empty;
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; } = 0;
    public int Gold { get; private set; } = 0;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; private set; } = DateTime.UtcNow;
    public bool IsOnline { get; private set; } = false;

    // 角色状态
    public CharacterVitals Vitals { get; private set; } = new();
    public CharacterAttributes Attributes { get; private set; } = new();
    public CharacterProfessions Professions { get; private set; } = new();
    public CharacterInventory Inventory { get; private set; } = new();
    public CharacterActions Actions { get; private set; } = new();
    public CharacterQuests Quests { get; private set; } = new();

    // 组队信息
    public Guid? PartyId { get; private set; }

    // 私有构造函数，用于反序列化
    private Character() { }

    /// <summary>
    /// 创建新角色
    /// </summary>
    public Character(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("角色名称不能为空", nameof(name));

        Name = name.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        LastActiveAt = DateTime.UtcNow;

        // 初始化基础属性
        Attributes = new CharacterAttributes();
        Vitals = new CharacterVitals(Attributes);
        Professions = new CharacterProfessions();
        Inventory = new CharacterInventory();
        Actions = new CharacterActions();
        Quests = new CharacterQuests();
    }

    /// <summary>
    /// 上线
    /// </summary>
    public void GoOnline()
    {
        IsOnline = true;
        LastActiveAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 下线
    /// </summary>
    public void GoOffline()
    {
        IsOnline = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新活跃时间
    /// </summary>
    public void UpdateActivity()
    {
        LastActiveAt = DateTime.UtcNow;
        if (IsOnline)
            UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 获得经验值
    /// </summary>
    public bool GainExperience(int amount)
    {
        if (amount <= 0) return false;

        Experience += amount;
        UpdatedAt = DateTime.UtcNow;

        // 检查是否升级
        var requiredExp = GetRequiredExperienceForNextLevel();
        if (Experience >= requiredExp)
        {
            return LevelUp();
        }

        return false;
    }

    /// <summary>
    /// 升级
    /// </summary>
    private bool LevelUp()
    {
        Level++;
        
        // 升级时回复生命值和法力值
        Vitals.RestoreToFull();
        
        // 增加属性点
        Attributes.AddAttributePoints(5);
        
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// 获得金币
    /// </summary>
    public void GainGold(int amount)
    {
        if (amount > 0)
        {
            Gold += amount;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 花费金币
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0 || Gold < amount)
            return false;

        Gold -= amount;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// 加入组队
    /// </summary>
    public void JoinParty(Guid partyId)
    {
        PartyId = partyId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 离开组队
    /// </summary>
    public void LeaveParty()
    {
        PartyId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取下一级所需经验值
    /// </summary>
    public int GetRequiredExperienceForNextLevel()
    {
        return Level * 100 + (Level - 1) * 50; // 简单的经验值公式
    }

    /// <summary>
    /// 获取当前等级进度百分比
    /// </summary>
    public double GetLevelProgress()
    {
        var currentLevelExp = GetRequiredExperienceForLevel(Level);
        var nextLevelExp = GetRequiredExperienceForNextLevel();
        var progressExp = Experience - currentLevelExp;
        var levelExp = nextLevelExp - currentLevelExp;
        
        return levelExp > 0 ? (double)progressExp / levelExp : 0;
    }

    /// <summary>
    /// 获取指定等级所需的经验值
    /// </summary>
    private int GetRequiredExperienceForLevel(int level)
    {
        if (level <= 1) return 0;
        
        int total = 0;
        for (int i = 2; i <= level; i++)
        {
            total += (i - 1) * 100 + (i - 2) * 50;
        }
        return total;
    }
}

/// <summary>
/// 角色生命值和法力值
/// </summary>
public class CharacterVitals
{
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public int Mana { get; private set; }
    public int MaxMana { get; private set; }
    public bool IsDead { get; private set; } = false;
    public DateTime? DeathTime { get; private set; }
    public double RevivalTimeRemaining { get; private set; } = 0;

    public CharacterVitals() { }

    public CharacterVitals(CharacterAttributes attributes)
    {
        RecalculateMaxValues(attributes);
        RestoreToFull();
    }

    /// <summary>
    /// 重新计算最大生命值和法力值
    /// </summary>
    public void RecalculateMaxValues(CharacterAttributes attributes)
    {
        MaxHealth = 100 + (attributes.Stamina * 10) + (attributes.Strength * 2);
        MaxMana = 100 + (attributes.Intellect * 10) + (attributes.Spirit * 5);
    }

    /// <summary>
    /// 恢复到满值
    /// </summary>
    public void RestoreToFull()
    {
        Health = MaxHealth;
        Mana = MaxMana;
        IsDead = false;
        DeathTime = null;
        RevivalTimeRemaining = 0;
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        Health = Math.Max(0, Health - damage);
        if (Health <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    public void Heal(int amount)
    {
        if (IsDead) return;
        Health = Math.Min(MaxHealth, Health + amount);
    }

    /// <summary>
    /// 消耗法力值
    /// </summary>
    public bool ConsumeMana(int amount)
    {
        if (Mana < amount) return false;
        Mana -= amount;
        return true;
    }

    /// <summary>
    /// 恢复法力值
    /// </summary>
    public void RestoreMana(int amount)
    {
        Mana = Math.Min(MaxMana, Mana + amount);
    }

    /// <summary>
    /// 死亡
    /// </summary>
    private void Die()
    {
        IsDead = true;
        DeathTime = DateTime.UtcNow;
        RevivalTimeRemaining = 30.0; // 30秒复活时间
    }

    /// <summary>
    /// 复活
    /// </summary>
    public void Revive()
    {
        if (!IsDead) return;
        
        IsDead = false;
        DeathTime = null;
        RevivalTimeRemaining = 0;
        Health = MaxHealth / 2; // 复活时恢复一半生命值
        Mana = MaxMana / 2;     // 复活时恢复一半法力值
    }

    /// <summary>
    /// 更新复活倒计时
    /// </summary>
    public void UpdateRevivalTimer()
    {
        if (!IsDead) return;
        
        var elapsed = DateTime.UtcNow - (DeathTime ?? DateTime.UtcNow);
        RevivalTimeRemaining = Math.Max(0, 30.0 - elapsed.TotalSeconds);
        
        if (RevivalTimeRemaining <= 0)
        {
            Revive();
        }
    }
}

/// <summary>
/// 角色属性
/// </summary>
public class CharacterAttributes
{
    public int Strength { get; private set; } = 10;
    public int Agility { get; private set; } = 10;
    public int Intellect { get; private set; } = 10;
    public int Spirit { get; private set; } = 10;
    public int Stamina { get; private set; } = 10;
    public int AttributePoints { get; private set; } = 0;

    /// <summary>
    /// 添加属性点
    /// </summary>
    public void AddAttributePoints(int points)
    {
        AttributePoints += points;
    }

    /// <summary>
    /// 分配属性点
    /// </summary>
    public bool AllocateAttribute(string attributeName, int points)
    {
        if (points <= 0 || points > AttributePoints)
            return false;

        switch (attributeName.ToLower())
        {
            case "strength":
                Strength += points;
                break;
            case "agility":
                Agility += points;
                break;
            case "intellect":
                Intellect += points;
                break;
            case "spirit":
                Spirit += points;
                break;
            case "stamina":
                Stamina += points;
                break;
            default:
                return false;
        }

        AttributePoints -= points;
        return true;
    }

    /// <summary>
    /// 重置属性点
    /// </summary>
    public void ResetAttributes()
    {
        var totalPoints = (Strength - 10) + (Agility - 10) + (Intellect - 10) + (Spirit - 10) + (Stamina - 10) + AttributePoints;
        
        Strength = 10;
        Agility = 10;
        Intellect = 10;
        Spirit = 10;
        Stamina = 10;
        AttributePoints = totalPoints;
    }

    /// <summary>
    /// 获得总属性点数
    /// </summary>
    public int GetTotalAttributePoints()
    {
        return Strength + Agility + Intellect + Spirit + Stamina;
    }
}

/// <summary>
/// 角色职业
/// </summary>
public class CharacterProfessions
{
    // 战斗职业
    public Dictionary<string, CharacterProfessionLevel> BattleProfessions { get; private set; } = new();
    
    // 采集职业
    public Dictionary<string, CharacterProfessionLevel> GatheringProfessions { get; private set; } = new();
    
    // 生产职业
    public Dictionary<string, CharacterProfessionLevel> ProductionProfessions { get; private set; } = new();

    public string SelectedBattleProfession { get; private set; } = "Warrior";

    public CharacterProfessions()
    {
        // 初始化战斗职业
        BattleProfessions["Warrior"] = new CharacterProfessionLevel();
        BattleProfessions["Mage"] = new CharacterProfessionLevel();
        BattleProfessions["Archer"] = new CharacterProfessionLevel();
        
        // 初始化采集职业
        GatheringProfessions["Mining"] = new CharacterProfessionLevel();
        GatheringProfessions["Herbalist"] = new CharacterProfessionLevel();
        GatheringProfessions["Fishing"] = new CharacterProfessionLevel();
        
        // 初始化生产职业
        ProductionProfessions["Alchemy"] = new CharacterProfessionLevel();
        ProductionProfessions["Engineering"] = new CharacterProfessionLevel();
        ProductionProfessions["Cooking"] = new CharacterProfessionLevel();
    }

    /// <summary>
    /// 选择战斗职业
    /// </summary>
    public void SelectBattleProfession(string profession)
    {
        if (BattleProfessions.ContainsKey(profession))
        {
            SelectedBattleProfession = profession;
        }
    }

    /// <summary>
    /// 获得职业经验值
    /// </summary>
    public bool GainProfessionExperience(string professionType, string profession, int experience)
    {
        Dictionary<string, CharacterProfessionLevel>? professions = professionType switch
        {
            "Battle" => BattleProfessions,
            "Gathering" => GatheringProfessions,
            "Production" => ProductionProfessions,
            _ => null
        };

        if (professions?.TryGetValue(profession, out var professionLevel) == true)
        {
            return professionLevel.GainExperience(experience);
        }

        return false;
    }

    /// <summary>
    /// 获取职业等级
    /// </summary>
    public int GetProfessionLevel(string professionType, string profession)
    {
        Dictionary<string, CharacterProfessionLevel>? professions = professionType switch
        {
            "Battle" => BattleProfessions,
            "Gathering" => GatheringProfessions,
            "Production" => ProductionProfessions,
            _ => null
        };

        return professions?.GetValueOrDefault(profession)?.Level ?? 0;
    }
}

/// <summary>
/// 职业等级信息
/// </summary>
public class CharacterProfessionLevel
{
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; } = 0;

    /// <summary>
    /// 获得经验值
    /// </summary>
    public bool GainExperience(int amount)
    {
        if (amount <= 0) return false;

        Experience += amount;
        return CheckLevelUp();
    }

    /// <summary>
    /// 检查升级
    /// </summary>
    private bool CheckLevelUp()
    {
        var requiredExp = GetRequiredExperience(Level + 1);
        if (Experience >= requiredExp)
        {
            Level++;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取指定等级所需经验值
    /// </summary>
    public static int GetRequiredExperience(int level)
    {
        if (level <= 1) return 0;
        return (level - 1) * 100 + ((level - 1) * (level - 2) / 2) * 25;
    }

    /// <summary>
    /// 获取当前等级进度
    /// </summary>
    public double GetLevelProgress()
    {
        var currentLevelExp = GetRequiredExperience(Level);
        var nextLevelExp = GetRequiredExperience(Level + 1);
        var progressExp = Experience - currentLevelExp;
        var levelExp = nextLevelExp - currentLevelExp;
        
        return levelExp > 0 ? (double)progressExp / levelExp : 0;
    }
}

/// <summary>
/// 角色背包
/// </summary>
public class CharacterInventory
{
    public const int MaxSlots = 50;
    
    private readonly Dictionary<string, string> _equippedItems = new(); // slot -> itemId
    private readonly List<InventoryItem> _items = new();

    public IReadOnlyDictionary<string, string> EquippedItems => _equippedItems;
    public IReadOnlyList<InventoryItem> Items => _items;

    /// <summary>
    /// 添加物品
    /// </summary>
    public bool AddItem(string itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            return false;

        // 查找是否已有相同物品可以堆叠
        var existingItem = _items.FirstOrDefault(item => item.ItemId == itemId);
        if (existingItem != null)
        {
            existingItem.AddQuantity(quantity);
            return true;
        }

        // 检查是否有空位
        if (_items.Count >= MaxSlots)
            return false;

        // 添加新物品
        _items.Add(new InventoryItem(itemId, quantity));
        return true;
    }

    /// <summary>
    /// 移除物品
    /// </summary>
    public bool RemoveItem(string itemId, int quantity = 1)
    {
        var item = _items.FirstOrDefault(i => i.ItemId == itemId);
        if (item == null || item.Quantity < quantity)
            return false;

        item.RemoveQuantity(quantity);
        if (item.Quantity <= 0)
        {
            _items.Remove(item);
        }

        return true;
    }

    /// <summary>
    /// 装备物品
    /// </summary>
    public bool EquipItem(string itemId, string slot)
    {
        if (!HasItem(itemId)) return false;

        // 卸下当前装备
        if (_equippedItems.TryGetValue(slot, out var currentItem))
        {
            AddItem(currentItem, 1);
        }

        // 装备新物品
        _equippedItems[slot] = itemId;
        RemoveItem(itemId, 1);
        return true;
    }

    /// <summary>
    /// 卸下装备
    /// </summary>
    public bool UnequipItem(string slot)
    {
        if (!_equippedItems.TryGetValue(slot, out var itemId))
            return false;

        if (_items.Count >= MaxSlots)
            return false; // 背包已满

        _equippedItems.Remove(slot);
        AddItem(itemId, 1);
        return true;
    }

    /// <summary>
    /// 检查是否拥有物品
    /// </summary>
    public bool HasItem(string itemId, int quantity = 1)
    {
        var item = _items.FirstOrDefault(i => i.ItemId == itemId);
        return item?.Quantity >= quantity;
    }

    /// <summary>
    /// 获取物品数量
    /// </summary>
    public int GetItemQuantity(string itemId)
    {
        return _items.FirstOrDefault(i => i.ItemId == itemId)?.Quantity ?? 0;
    }
}

/// <summary>
/// 背包物品
/// </summary>
public class InventoryItem
{
    public string ItemId { get; private set; }
    public int Quantity { get; private set; }

    public InventoryItem(string itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }

    public void AddQuantity(int amount)
    {
        if (amount > 0)
            Quantity += amount;
    }

    public void RemoveQuantity(int amount)
    {
        if (amount > 0)
            Quantity = Math.Max(0, Quantity - amount);
    }
}

/// <summary>
/// 角色动作状态
/// </summary>
public class CharacterActions
{
    public string CurrentAction { get; private set; } = "Idle";
    public string? ActionTargetId { get; private set; }
    public double Progress { get; private set; } = 0.0;
    public double Duration { get; private set; } = 0.0;
    public DateTime? StartTime { get; private set; }
    public Dictionary<string, object> ActionData { get; private set; } = new();

    /// <summary>
    /// 开始动作
    /// </summary>
    public void StartAction(string actionType, string? targetId = null, double duration = 0.0, Dictionary<string, object>? actionData = null)
    {
        CurrentAction = actionType;
        ActionTargetId = targetId;
        Duration = duration;
        Progress = 0.0;
        StartTime = DateTime.UtcNow;
        ActionData = actionData ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// 停止动作
    /// </summary>
    public void StopAction()
    {
        CurrentAction = "Idle";
        ActionTargetId = null;
        Progress = 0.0;
        Duration = 0.0;
        StartTime = null;
        ActionData.Clear();
    }

    /// <summary>
    /// 更新动作进度
    /// </summary>
    public void UpdateProgress()
    {
        if (StartTime == null || Duration <= 0) return;

        var elapsed = DateTime.UtcNow - StartTime.Value;
        Progress = Math.Min(1.0, elapsed.TotalSeconds / Duration);

        if (Progress >= 1.0)
        {
            CompleteAction();
        }
    }

    /// <summary>
    /// 完成动作
    /// </summary>
    private void CompleteAction()
    {
        // 动作完成逻辑可以在这里实现
        StopAction();
    }

    /// <summary>
    /// 获取剩余时间
    /// </summary>
    public double GetTimeRemaining()
    {
        if (StartTime == null || Duration <= 0) return 0.0;

        var elapsed = DateTime.UtcNow - StartTime.Value;
        return Math.Max(0.0, Duration - elapsed.TotalSeconds);
    }
}

/// <summary>
/// 角色任务管理
/// </summary>
public class CharacterQuests
{
    private readonly List<string> _activeQuestIds = new();
    private readonly List<string> _completedQuestIds = new();
    private readonly Dictionary<string, int> _questProgress = new();

    public IReadOnlyList<string> ActiveQuestIds => _activeQuestIds;
    public IReadOnlyList<string> CompletedQuestIds => _completedQuestIds;
    public IReadOnlyDictionary<string, int> QuestProgress => _questProgress;

    /// <summary>
    /// 接受任务
    /// </summary>
    public bool AcceptQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId) || _activeQuestIds.Contains(questId) || _completedQuestIds.Contains(questId))
            return false;

        _activeQuestIds.Add(questId);
        _questProgress[questId] = 0;
        return true;
    }

    /// <summary>
    /// 完成任务
    /// </summary>
    public bool CompleteQuest(string questId)
    {
        if (!_activeQuestIds.Contains(questId))
            return false;

        _activeQuestIds.Remove(questId);
        _completedQuestIds.Add(questId);
        _questProgress.Remove(questId);
        return true;
    }

    /// <summary>
    /// 更新任务进度
    /// </summary>
    public void UpdateQuestProgress(string questId, int progress)
    {
        if (_activeQuestIds.Contains(questId))
        {
            _questProgress[questId] = Math.Max(0, progress);
        }
    }

    /// <summary>
    /// 获取任务进度
    /// </summary>
    public int GetQuestProgress(string questId)
    {
        return _questProgress.GetValueOrDefault(questId, 0);
    }

    /// <summary>
    /// 检查任务是否已完成
    /// </summary>
    public bool IsQuestCompleted(string questId)
    {
        return _completedQuestIds.Contains(questId);
    }

    /// <summary>
    /// 检查任务是否激活
    /// </summary>
    public bool IsQuestActive(string questId)
    {
        return _activeQuestIds.Contains(questId);
    }
}