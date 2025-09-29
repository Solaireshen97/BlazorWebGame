using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 采集节点领域模型
/// </summary>
public class GatheringNode
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public GatheringNodeType Type { get; private set; } = GatheringNodeType.Ore;
    public int RequiredLevel { get; private set; } = 1;
    public string RequiredProfession { get; private set; } = "Mining";
    public TimeSpan GatheringTime { get; private set; } = TimeSpan.FromSeconds(5);
    public int ExperienceReward { get; private set; } = 10;
    public string? RequiredMonsterId { get; private set; } // 需要击败的怪物才能采集
    public bool IsRespawnable { get; private set; } = true;
    public TimeSpan? RespawnTime { get; private set; } = TimeSpan.FromMinutes(5);

    // 产出物品
    private readonly List<GatheringResult> _results = new();
    public IReadOnlyList<GatheringResult> Results => _results.AsReadOnly();

    // 私有构造函数，用于反序列化
    private GatheringNode() { }

    /// <summary>
    /// 创建采集节点
    /// </summary>
    public GatheringNode(string name, string description, GatheringNodeType type, string requiredProfession, int requiredLevel = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("节点名称不能为空", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        RequiredProfession = requiredProfession?.Trim() ?? string.Empty;
        RequiredLevel = Math.Max(1, requiredLevel);
    }

    /// <summary>
    /// 设置采集时间
    /// </summary>
    public void SetGatheringTime(TimeSpan time)
    {
        GatheringTime = time > TimeSpan.Zero ? time : TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// 设置经验奖励
    /// </summary>
    public void SetExperienceReward(int experience)
    {
        ExperienceReward = Math.Max(0, experience);
    }

    /// <summary>
    /// 设置怪物要求
    /// </summary>
    public void SetMonsterRequirement(string? monsterId)
    {
        RequiredMonsterId = monsterId;
    }

    /// <summary>
    /// 设置重生设置
    /// </summary>
    public void SetRespawnSettings(bool isRespawnable, TimeSpan? respawnTime = null)
    {
        IsRespawnable = isRespawnable;
        RespawnTime = isRespawnable ? (respawnTime ?? TimeSpan.FromMinutes(5)) : null;
    }

    /// <summary>
    /// 添加产出结果
    /// </summary>
    public void AddResult(string itemId, int minQuantity = 1, int maxQuantity = 1, double probability = 1.0)
    {
        var result = new GatheringResult(itemId, minQuantity, maxQuantity, probability);
        _results.Add(result);
    }

    /// <summary>
    /// 生成采集结果
    /// </summary>
    public List<GatheringReward> GenerateRewards(Random random)
    {
        var rewards = new List<GatheringReward>();
        
        foreach (var result in _results)
        {
            if (random.NextDouble() < result.Probability)
            {
                var quantity = result.MinQuantity == result.MaxQuantity 
                    ? result.MinQuantity 
                    : random.Next(result.MinQuantity, result.MaxQuantity + 1);
                
                rewards.Add(new GatheringReward(result.ItemId, quantity));
            }
        }

        return rewards;
    }

    /// <summary>
    /// 检查是否可以采集
    /// </summary>
    public bool CanGather(string profession, int level, bool hasDefeatedRequiredMonster = true)
    {
        if (RequiredProfession != profession) return false;
        if (level < RequiredLevel) return false;
        if (!string.IsNullOrEmpty(RequiredMonsterId) && !hasDefeatedRequiredMonster) return false;
        
        return true;
    }
}

/// <summary>
/// 采集结果配置
/// </summary>
public class GatheringResult
{
    public string ItemId { get; private set; } = string.Empty;
    public int MinQuantity { get; private set; } = 1;
    public int MaxQuantity { get; private set; } = 1;
    public double Probability { get; private set; } = 1.0;

    // 私有构造函数，用于反序列化
    private GatheringResult() { }

    /// <summary>
    /// 创建采集结果
    /// </summary>
    public GatheringResult(string itemId, int minQuantity = 1, int maxQuantity = 1, double probability = 1.0)
    {
        ItemId = itemId;
        MinQuantity = Math.Max(1, minQuantity);
        MaxQuantity = Math.Max(minQuantity, maxQuantity);
        Probability = Math.Clamp(probability, 0.0, 1.0);
    }
}

/// <summary>
/// 采集奖励
/// </summary>
public class GatheringReward
{
    public string ItemId { get; private set; } = string.Empty;
    public int Quantity { get; private set; } = 1;

    // 私有构造函数，用于反序列化
    private GatheringReward() { }

    /// <summary>
    /// 创建采集奖励
    /// </summary>
    public GatheringReward(string itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = Math.Max(1, quantity);
    }
}

/// <summary>
/// 制作配方领域模型
/// </summary>
public class Recipe
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string RequiredProfession { get; private set; } = "Alchemy";
    public int RequiredLevel { get; private set; } = 1;
    public TimeSpan CraftingTime { get; private set; } = TimeSpan.FromSeconds(10);
    public int ExperienceReward { get; private set; } = 10;
    public bool IsDefault { get; private set; } = false; // 是否默认学会
    public string? UnlockItemId { get; private set; } // 解锁配方所需的图纸物品ID

    // 所需材料
    private readonly Dictionary<string, int> _ingredients = new();
    public IReadOnlyDictionary<string, int> Ingredients => _ingredients;

    // 产出物品
    private readonly List<RecipeResult> _results = new();
    public IReadOnlyList<RecipeResult> Results => _results.AsReadOnly();

    // 私有构造函数，用于反序列化
    private Recipe() { }

    /// <summary>
    /// 创建制作配方
    /// </summary>
    public Recipe(string name, string description, string requiredProfession, int requiredLevel = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("配方名称不能为空", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        RequiredProfession = requiredProfession?.Trim() ?? string.Empty;
        RequiredLevel = Math.Max(1, requiredLevel);
    }

    /// <summary>
    /// 设置制作时间
    /// </summary>
    public void SetCraftingTime(TimeSpan time)
    {
        CraftingTime = time > TimeSpan.Zero ? time : TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// 设置经验奖励
    /// </summary>
    public void SetExperienceReward(int experience)
    {
        ExperienceReward = Math.Max(0, experience);
    }

    /// <summary>
    /// 设置为默认配方
    /// </summary>
    public void SetAsDefault(bool isDefault = true)
    {
        IsDefault = isDefault;
    }

    /// <summary>
    /// 设置解锁物品
    /// </summary>
    public void SetUnlockItem(string? itemId)
    {
        UnlockItemId = itemId;
    }

    /// <summary>
    /// 添加所需材料
    /// </summary>
    public void AddIngredient(string itemId, int quantity)
    {
        if (!string.IsNullOrWhiteSpace(itemId) && quantity > 0)
        {
            _ingredients[itemId] = quantity;
        }
    }

    /// <summary>
    /// 移除所需材料
    /// </summary>
    public void RemoveIngredient(string itemId)
    {
        _ingredients.Remove(itemId);
    }

    /// <summary>
    /// 添加产出结果
    /// </summary>
    public void AddResult(string itemId, int minQuantity = 1, int maxQuantity = 1, double probability = 1.0)
    {
        var result = new RecipeResult(itemId, minQuantity, maxQuantity, probability);
        _results.Add(result);
    }

    /// <summary>
    /// 检查材料是否足够
    /// </summary>
    public bool HasEnoughMaterials(Dictionary<string, int> availableItems)
    {
        foreach (var ingredient in _ingredients)
        {
            var available = availableItems.GetValueOrDefault(ingredient.Key, 0);
            if (available < ingredient.Value)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 生成制作结果
    /// </summary>
    public List<CraftingReward> GenerateResults(Random random)
    {
        var rewards = new List<CraftingReward>();
        
        foreach (var result in _results)
        {
            if (random.NextDouble() < result.Probability)
            {
                var quantity = result.MinQuantity == result.MaxQuantity 
                    ? result.MinQuantity 
                    : random.Next(result.MinQuantity, result.MaxQuantity + 1);
                
                rewards.Add(new CraftingReward(result.ItemId, quantity));
            }
        }

        return rewards;
    }
}

/// <summary>
/// 配方产出结果
/// </summary>
public class RecipeResult
{
    public string ItemId { get; private set; } = string.Empty;
    public int MinQuantity { get; private set; } = 1;
    public int MaxQuantity { get; private set; } = 1;
    public double Probability { get; private set; } = 1.0;

    // 私有构造函数，用于反序列化
    private RecipeResult() { }

    /// <summary>
    /// 创建配方结果
    /// </summary>
    public RecipeResult(string itemId, int minQuantity = 1, int maxQuantity = 1, double probability = 1.0)
    {
        ItemId = itemId;
        MinQuantity = Math.Max(1, minQuantity);
        MaxQuantity = Math.Max(minQuantity, maxQuantity);
        Probability = Math.Clamp(probability, 0.0, 1.0);
    }
}

/// <summary>
/// 制作奖励
/// </summary>
public class CraftingReward
{
    public string ItemId { get; private set; } = string.Empty;
    public int Quantity { get; private set; } = 1;

    // 私有构造函数，用于反序列化
    private CraftingReward() { }

    /// <summary>
    /// 创建制作奖励
    /// </summary>
    public CraftingReward(string itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = Math.Max(1, quantity);
    }
}

/// <summary>
/// 制作状态
/// </summary>
public class CraftingState
{
    public string CharacterId { get; private set; } = string.Empty;
    public string? CurrentRecipeId { get; private set; }
    public int TotalQuantity { get; private set; } = 0;
    public int CompletedQuantity { get; private set; } = 0;
    public DateTime? StartTime { get; private set; }
    public DateTime? EstimatedCompletionTime { get; private set; }

    // 队列中的制作任务
    private readonly List<CraftingTask> _craftingQueue = new();
    public IReadOnlyList<CraftingTask> CraftingQueue => _craftingQueue.AsReadOnly();

    public bool IsCrafting => CurrentRecipeId != null;
    public int RemainingQuantity => TotalQuantity - CompletedQuantity;

    // 私有构造函数，用于反序列化
    private CraftingState() { }

    /// <summary>
    /// 创建制作状态
    /// </summary>
    public CraftingState(string characterId)
    {
        CharacterId = characterId;
    }

    /// <summary>
    /// 开始制作
    /// </summary>
    public bool StartCrafting(string recipeId, int quantity, TimeSpan craftingTime)
    {
        if (IsCrafting) return false;

        CurrentRecipeId = recipeId;
        TotalQuantity = quantity;
        CompletedQuantity = 0;
        StartTime = DateTime.UtcNow;
        EstimatedCompletionTime = DateTime.UtcNow.Add(TimeSpan.FromTicks(craftingTime.Ticks * quantity));

        return true;
    }

    /// <summary>
    /// 停止制作
    /// </summary>
    public void StopCrafting()
    {
        CurrentRecipeId = null;
        TotalQuantity = 0;
        CompletedQuantity = 0;
        StartTime = null;
        EstimatedCompletionTime = null;
    }

    /// <summary>
    /// 完成一个制作
    /// </summary>
    public bool CompleteOne()
    {
        if (!IsCrafting || CompletedQuantity >= TotalQuantity)
            return false;

        CompletedQuantity++;
        
        // 如果全部完成，停止制作
        if (CompletedQuantity >= TotalQuantity)
        {
            StopCrafting();
            ProcessNextInQueue();
        }

        return true;
    }

    /// <summary>
    /// 添加到制作队列
    /// </summary>
    public void AddToQueue(string recipeId, int quantity)
    {
        var task = new CraftingTask(recipeId, quantity);
        _craftingQueue.Add(task);
    }

    /// <summary>
    /// 处理队列中的下一个任务
    /// </summary>
    private void ProcessNextInQueue()
    {
        if (_craftingQueue.Count > 0)
        {
            var nextTask = _craftingQueue[0];
            _craftingQueue.RemoveAt(0);
            
            // 这里需要外部提供制作时间，暂时使用默认值
            StartCrafting(nextTask.RecipeId, nextTask.Quantity, TimeSpan.FromSeconds(10));
        }
    }

    /// <summary>
    /// 获取制作进度
    /// </summary>
    public double GetProgress()
    {
        return TotalQuantity > 0 ? (double)CompletedQuantity / TotalQuantity : 0.0;
    }

    /// <summary>
    /// 获取剩余时间
    /// </summary>
    public TimeSpan GetRemainingTime()
    {
        if (EstimatedCompletionTime == null)
            return TimeSpan.Zero;

        var remaining = EstimatedCompletionTime.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}

/// <summary>
/// 制作任务
/// </summary>
public class CraftingTask
{
    public string RecipeId { get; private set; } = string.Empty;
    public int Quantity { get; private set; } = 1;
    public DateTime QueuedAt { get; private set; } = DateTime.UtcNow;

    // 私有构造函数，用于反序列化
    private CraftingTask() { }

    /// <summary>
    /// 创建制作任务
    /// </summary>
    public CraftingTask(string recipeId, int quantity)
    {
        RecipeId = recipeId;
        Quantity = Math.Max(1, quantity);
        QueuedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// 采集节点类型枚举
/// </summary>
public enum GatheringNodeType
{
    Ore,        // 矿石
    Herb,       // 草药
    Fish,       // 钓鱼点
    Wood,       // 木材
    Gem,        // 宝石
    Special     // 特殊
}

/// <summary>
/// 采集状态
/// </summary>
public class GatheringState
{
    public string CharacterId { get; private set; } = string.Empty;
    public string? CurrentNodeId { get; private set; }
    public DateTime? StartTime { get; private set; }
    public DateTime? EstimatedCompletionTime { get; private set; }

    public bool IsGathering => CurrentNodeId != null;

    // 私有构造函数，用于反序列化
    private GatheringState() { }

    /// <summary>
    /// 创建采集状态
    /// </summary>
    public GatheringState(string characterId)
    {
        CharacterId = characterId;
    }

    /// <summary>
    /// 开始采集
    /// </summary>
    public bool StartGathering(string nodeId, TimeSpan gatheringTime)
    {
        if (IsGathering) return false;

        CurrentNodeId = nodeId;
        StartTime = DateTime.UtcNow;
        EstimatedCompletionTime = DateTime.UtcNow.Add(gatheringTime);

        return true;
    }

    /// <summary>
    /// 停止采集
    /// </summary>
    public void StopGathering()
    {
        CurrentNodeId = null;
        StartTime = null;
        EstimatedCompletionTime = null;
    }

    /// <summary>
    /// 获取采集进度
    /// </summary>
    public double GetProgress()
    {
        if (StartTime == null || EstimatedCompletionTime == null)
            return 0.0;

        var totalTime = EstimatedCompletionTime.Value - StartTime.Value;
        var elapsed = DateTime.UtcNow - StartTime.Value;

        if (totalTime.TotalSeconds <= 0)
            return 1.0;

        return Math.Clamp(elapsed.TotalSeconds / totalTime.TotalSeconds, 0.0, 1.0);
    }

    /// <summary>
    /// 获取剩余时间
    /// </summary>
    public TimeSpan GetRemainingTime()
    {
        if (EstimatedCompletionTime == null)
            return TimeSpan.Zero;

        var remaining = EstimatedCompletionTime.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// 检查是否完成
    /// </summary>
    public bool IsCompleted()
    {
        return EstimatedCompletionTime != null && DateTime.UtcNow >= EstimatedCompletionTime.Value;
    }
}