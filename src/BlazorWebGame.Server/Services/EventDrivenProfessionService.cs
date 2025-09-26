using BlazorWebGame.Shared.Events;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 基于事件队列的服务端职业系统服务
/// 统一管理采集、制作等生产活动，使用事件驱动架构
/// </summary>
public class EventDrivenProfessionService : IDisposable
{
    private readonly UnifiedEventService _eventService;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<EventDrivenProfessionService> _logger;

    // 活跃的职业活动状态
    private readonly ConcurrentDictionary<string, ProfessionActivityState> _activeActivities = new();
    
    // 节点和配方数据缓存
    private readonly Dictionary<string, GatheringNodeData> _gatheringNodes;
    private readonly Dictionary<string, CraftingRecipeData> _craftingRecipes;
    
    // 性能统计
    private long _totalActivitiesStarted = 0;
    private long _totalActivitiesCompleted = 0;
    private readonly object _statsLock = new();

    public EventDrivenProfessionService(
        UnifiedEventService eventService,
        IHubContext<GameHub> hubContext,
        ILogger<EventDrivenProfessionService> logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 初始化数据
        _gatheringNodes = InitializeGatheringNodes();
        _craftingRecipes = InitializeCraftingRecipes();

        // 注册职业事件处理器
        RegisterProfessionEventHandlers();
    }

    /// <summary>
    /// 注册职业相关事件处理器
    /// </summary>
    private void RegisterProfessionEventHandlers()
    {
        _eventService.RegisterHandler(GameEventTypes.GATHERING_STARTED, new GatheringStartHandler(this, _logger));
        _eventService.RegisterHandler(GameEventTypes.GATHERING_PROGRESS, new GatheringProgressHandler(this, _logger));
        _eventService.RegisterHandler(GameEventTypes.GATHERING_COMPLETED, new GatheringCompletedHandler(this, _logger));
        _eventService.RegisterHandler(GameEventTypes.GATHERING_CANCELLED, new GatheringCancelledHandler(this, _logger));
        
        _eventService.RegisterHandler(GameEventTypes.CRAFTING_STARTED, new CraftingStartHandler(this, _logger));
        _eventService.RegisterHandler(GameEventTypes.CRAFTING_PROGRESS, new CraftingProgressHandler(this, _logger));
        _eventService.RegisterHandler(GameEventTypes.CRAFTING_COMPLETED, new CraftingCompletedHandler(this, _logger));
        _eventService.RegisterHandler(GameEventTypes.CRAFTING_CANCELLED, new CraftingCancelledHandler(this, _logger));
        
        _eventService.RegisterHandler(GameEventTypes.PROFESSION_XP_GAINED, new ProfessionXpHandler(this, _logger));
        _eventService.RegisterHandler(GameEventTypes.PROFESSION_LEVEL_UP, new ProfessionLevelUpHandler(this, _logger));
    }

    /// <summary>
    /// 开始采集活动
    /// </summary>
    public async Task<bool> StartGatheringAsync(string characterId, string nodeId)
    {
        try
        {
            // 检查节点是否存在
            if (!_gatheringNodes.TryGetValue(nodeId, out var node))
            {
                _logger.LogWarning("Gathering node {NodeId} not found", nodeId);
                return false;
            }

            // 检查角色是否已经在进行活动
            if (_activeActivities.ContainsKey(characterId))
            {
                _logger.LogWarning("Character {CharacterId} is already engaged in a profession activity", characterId);
                return false;
            }

            // 创建活动状态
            var activity = new ProfessionActivityState
            {
                CharacterId = characterId,
                ActivityType = ProfessionActivityType.Gathering,
                NodeId = nodeId,
                StartTime = DateTime.UtcNow,
                Duration = TimeSpan.FromSeconds(node.GatheringTimeSeconds),
                Progress = 0.0f,
                ProfessionType = node.ProfessionType,
                IsActive = true
            };

            _activeActivities[characterId] = activity;

            // 生成采集开始事件
            var gatheringData = new GatheringEventData
            {
                NodeId = (ushort)HashString(nodeId),
                ItemId = (ushort)HashString(node.ResultItemId),
                Quantity = (byte)node.ResultQuantity,
                ExtraLoot = 0,
                Progress = 0.0f,
                XpGained = node.XpReward,
                ProfessionType = (ushort)node.ProfessionType
            };

            _eventService.EnqueueEvent(GameEventTypes.GATHERING_STARTED, gatheringData, 
                EventPriority.Gameplay, (ulong)HashString(characterId), (ulong)HashString(nodeId));

            lock (_statsLock)
            {
                _totalActivitiesStarted++;
            }

            _logger.LogInformation("Character {CharacterId} started gathering at node {NodeId}", characterId, nodeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting gathering for character {CharacterId} at node {NodeId}", characterId, nodeId);
            return false;
        }
    }

    /// <summary>
    /// 开始制作活动
    /// </summary>
    public async Task<bool> StartCraftingAsync(string characterId, string recipeId)
    {
        try
        {
            // 检查配方是否存在
            if (!_craftingRecipes.TryGetValue(recipeId, out var recipe))
            {
                _logger.LogWarning("Crafting recipe {RecipeId} not found", recipeId);
                return false;
            }

            // 检查角色是否已经在进行活动
            if (_activeActivities.ContainsKey(characterId))
            {
                _logger.LogWarning("Character {CharacterId} is already engaged in a profession activity", characterId);
                return false;
            }

            // 创建活动状态
            var activity = new ProfessionActivityState
            {
                CharacterId = characterId,
                ActivityType = ProfessionActivityType.Crafting,
                RecipeId = recipeId,
                StartTime = DateTime.UtcNow,
                Duration = TimeSpan.FromSeconds(recipe.CraftingTimeSeconds),
                Progress = 0.0f,
                ProfessionType = recipe.ProfessionType,
                IsActive = true
            };

            _activeActivities[characterId] = activity;

            // 生成制作开始事件
            var craftingData = new CraftingEventData
            {
                RecipeId = (ushort)HashString(recipeId),
                ResultItemId = (ushort)HashString(recipe.ResultItemId),
                Quantity = (byte)recipe.ResultQuantity,
                QualityBonus = 0,
                Progress = 0.0f,
                XpGained = recipe.XpReward,
                ProfessionType = (ushort)recipe.ProfessionType,
                MaterialCost = recipe.MaterialCost
            };

            _eventService.EnqueueEvent(GameEventTypes.CRAFTING_STARTED, craftingData,
                EventPriority.Gameplay, (ulong)HashString(characterId), (ulong)HashString(recipeId));

            lock (_statsLock)
            {
                _totalActivitiesStarted++;
            }

            _logger.LogInformation("Character {CharacterId} started crafting recipe {RecipeId}", characterId, recipeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting crafting for character {CharacterId} with recipe {RecipeId}", characterId, recipeId);
            return false;
        }
    }

    /// <summary>
    /// 停止当前活动
    /// </summary>
    public async Task<bool> StopCurrentActivityAsync(string characterId)
    {
        try
        {
            if (!_activeActivities.TryRemove(characterId, out var activity))
            {
                return false; // 没有活动需要停止
            }

            // 生成取消事件
            var eventType = activity.ActivityType == ProfessionActivityType.Gathering 
                ? GameEventTypes.GATHERING_CANCELLED 
                : GameEventTypes.CRAFTING_CANCELLED;

            _eventService.EnqueueEvent(GameEventTypes.GATHERING_CANCELLED, EventPriority.Gameplay);

            _logger.LogInformation("Character {CharacterId} stopped {ActivityType} activity", 
                characterId, activity.ActivityType);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping activity for character {CharacterId}", characterId);
            return false;
        }
    }

    /// <summary>
    /// 更新职业活动状态 - 由游戏循环调用
    /// </summary>
    public async Task UpdateProfessionActivitiesAsync(double deltaTimeSeconds)
    {
        var completedActivities = new List<string>();
        var currentTime = DateTime.UtcNow;

        // 批量处理所有活动
        var activities = _activeActivities.ToArray();
        foreach (var kvp in activities)
        {
            var characterId = kvp.Key;
            var activity = kvp.Value;

            if (!activity.IsActive) continue;

            // 计算进度
            var elapsedTime = currentTime - activity.StartTime;
            var newProgress = Math.Min(1.0f, (float)(elapsedTime.TotalSeconds / activity.Duration.TotalSeconds));
            
            if (Math.Abs(newProgress - activity.Progress) > 0.01f) // 避免频繁更新
            {
                activity.Progress = newProgress;

                // 生成进度更新事件
                await GenerateProgressEventAsync(activity);
            }

            // 检查是否完成
            if (newProgress >= 1.0f)
            {
                await CompleteActivityAsync(activity);
                completedActivities.Add(characterId);
            }
        }

        // 移除已完成的活动
        foreach (var characterId in completedActivities)
        {
            _activeActivities.TryRemove(characterId, out _);
        }
    }

    /// <summary>
    /// 生成进度更新事件
    /// </summary>
    private async Task GenerateProgressEventAsync(ProfessionActivityState activity)
    {
        if (activity.ActivityType == ProfessionActivityType.Gathering)
        {
            var node = _gatheringNodes[activity.NodeId!];
            var gatheringData = new GatheringEventData
            {
                NodeId = (ushort)HashString(activity.NodeId!),
                ItemId = (ushort)HashString(node.ResultItemId),
                Quantity = (byte)node.ResultQuantity,
                ExtraLoot = 0,
                Progress = activity.Progress,
                XpGained = 0, // 进度事件不给经验
                ProfessionType = (ushort)activity.ProfessionType
            };

            _eventService.EnqueueEvent(GameEventTypes.GATHERING_PROGRESS, gatheringData,
                EventPriority.Gameplay, (ulong)HashString(activity.CharacterId));
        }
        else if (activity.ActivityType == ProfessionActivityType.Crafting)
        {
            var recipe = _craftingRecipes[activity.RecipeId!];
            var craftingData = new CraftingEventData
            {
                RecipeId = (ushort)HashString(activity.RecipeId!),
                ResultItemId = (ushort)HashString(recipe.ResultItemId),
                Quantity = (byte)recipe.ResultQuantity,
                QualityBonus = 0,
                Progress = activity.Progress,
                XpGained = 0, // 进度事件不给经验
                ProfessionType = (ushort)activity.ProfessionType,
                MaterialCost = recipe.MaterialCost
            };

            _eventService.EnqueueEvent(GameEventTypes.CRAFTING_PROGRESS, craftingData,
                EventPriority.Gameplay, (ulong)HashString(activity.CharacterId));
        }
    }

    /// <summary>
    /// 完成职业活动
    /// </summary>
    private async Task CompleteActivityAsync(ProfessionActivityState activity)
    {
        try
        {
            if (activity.ActivityType == ProfessionActivityType.Gathering)
            {
                var node = _gatheringNodes[activity.NodeId!];
                var gatheringData = new GatheringEventData
                {
                    NodeId = (ushort)HashString(activity.NodeId!),
                    ItemId = (ushort)HashString(node.ResultItemId),
                    Quantity = (byte)node.ResultQuantity,
                    ExtraLoot = (byte)(Random.Shared.NextDouble() < 0.1 ? 1 : 0), // 10% 额外掉落几率
                    Progress = 1.0f,
                    XpGained = node.XpReward,
                    ProfessionType = (ushort)activity.ProfessionType
                };

                _eventService.EnqueueEvent(GameEventTypes.GATHERING_COMPLETED, gatheringData,
                    EventPriority.Gameplay, (ulong)HashString(activity.CharacterId));
            }
            else if (activity.ActivityType == ProfessionActivityType.Crafting)
            {
                var recipe = _craftingRecipes[activity.RecipeId!];
                var craftingData = new CraftingEventData
                {
                    RecipeId = (ushort)HashString(activity.RecipeId!),
                    ResultItemId = (ushort)HashString(recipe.ResultItemId),
                    Quantity = (byte)recipe.ResultQuantity,
                    QualityBonus = (byte)(Random.Shared.NextDouble() < 0.05 ? 1 : 0), // 5% 品质加成几率
                    Progress = 1.0f,
                    XpGained = recipe.XpReward,
                    ProfessionType = (ushort)activity.ProfessionType,
                    MaterialCost = recipe.MaterialCost
                };

                _eventService.EnqueueEvent(GameEventTypes.CRAFTING_COMPLETED, craftingData,
                    EventPriority.Gameplay, (ulong)HashString(activity.CharacterId));
            }

            // 生成经验获得事件
            var expData = new ExperienceEventData
            {
                Amount = activity.ActivityType == ProfessionActivityType.Gathering 
                    ? _gatheringNodes[activity.NodeId!].XpReward
                    : _craftingRecipes[activity.RecipeId!].XpReward,
                ProfessionId = (ushort)activity.ProfessionType,
                NewLevel = 0, // TODO: 计算新等级
                TotalExperience = 0 // TODO: 获取总经验
            };

            _eventService.EnqueueEvent(GameEventTypes.PROFESSION_XP_GAINED, expData,
                EventPriority.Gameplay, (ulong)HashString(activity.CharacterId));

            lock (_statsLock)
            {
                _totalActivitiesCompleted++;
            }

            _logger.LogInformation("Character {CharacterId} completed {ActivityType} activity", 
                activity.CharacterId, activity.ActivityType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing activity for character {CharacterId}", activity.CharacterId);
        }
    }

    /// <summary>
    /// 获取角色当前活动状态
    /// </summary>
    public ProfessionActivityState? GetCharacterActivity(string characterId)
    {
        return _activeActivities.TryGetValue(characterId, out var activity) ? activity : null;
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public ProfessionServiceStats GetStatistics()
    {
        lock (_statsLock)
        {
            return new ProfessionServiceStats
            {
                ActiveActivities = _activeActivities.Count,
                TotalActivitiesStarted = _totalActivitiesStarted,
                TotalActivitiesCompleted = _totalActivitiesCompleted,
                CompletionRate = _totalActivitiesStarted > 0 ? (double)_totalActivitiesCompleted / _totalActivitiesStarted : 0.0
            };
        }
    }

    /// <summary>
    /// 初始化采集节点数据
    /// </summary>
    private Dictionary<string, GatheringNodeData> InitializeGatheringNodes()
    {
        return new Dictionary<string, GatheringNodeData>
        {
            ["NODE_COPPER_VEIN"] = new GatheringNodeData
            {
                Id = "NODE_COPPER_VEIN",
                Name = "铜矿脉",
                GatheringTimeSeconds = 7,
                ResultItemId = "ORE_COPPER",
                ResultQuantity = 1,
                XpReward = 7,
                ProfessionType = ProfessionType.Mining,
                RequiredLevel = 1
            },
            ["NODE_IRON_VEIN"] = new GatheringNodeData
            {
                Id = "NODE_IRON_VEIN",
                Name = "铁矿脉",
                GatheringTimeSeconds = 12,
                ResultItemId = "ORE_IRON",
                ResultQuantity = 1,
                XpReward = 15,
                ProfessionType = ProfessionType.Mining,
                RequiredLevel = 10
            },
            ["NODE_HEALING_HERB"] = new GatheringNodeData
            {
                Id = "NODE_HEALING_HERB",
                Name = "治疗草药",
                GatheringTimeSeconds = 5,
                ResultItemId = "HERB_HEALING",
                ResultQuantity = 1,
                XpReward = 5,
                ProfessionType = ProfessionType.Herbalism,
                RequiredLevel = 1
            }
        };
    }

    /// <summary>
    /// 初始化制作配方数据
    /// </summary>
    private Dictionary<string, CraftingRecipeData> InitializeCraftingRecipes()
    {
        return new Dictionary<string, CraftingRecipeData>
        {
            ["RECIPE_COPPER_SWORD"] = new CraftingRecipeData
            {
                Id = "RECIPE_COPPER_SWORD",
                Name = "铜剑",
                CraftingTimeSeconds = 30,
                ResultItemId = "WEAPON_COPPER_SWORD",
                ResultQuantity = 1,
                XpReward = 25,
                ProfessionType = ProfessionType.Blacksmithing,
                RequiredLevel = 1,
                MaterialCost = 100
            },
            ["RECIPE_HEALING_POTION"] = new CraftingRecipeData
            {
                Id = "RECIPE_HEALING_POTION",
                Name = "治疗药水",
                CraftingTimeSeconds = 15,
                ResultItemId = "CONSUMABLE_HEALING_POTION",
                ResultQuantity = 1,
                XpReward = 12,
                ProfessionType = ProfessionType.Alchemy,
                RequiredLevel = 1,
                MaterialCost = 50
            }
        };
    }

    /// <summary>
    /// 字符串哈希函数
    /// </summary>
    private static int HashString(string input)
    {
        if (string.IsNullOrEmpty(input)) return 0;
        return input.GetHashCode();
    }

    public void Dispose()
    {
        _activeActivities.Clear();
    }
}

/// <summary>
/// 职业活动状态
/// </summary>
public class ProfessionActivityState
{
    public string CharacterId { get; set; } = string.Empty;
    public ProfessionActivityType ActivityType { get; set; }
    public string? NodeId { get; set; }
    public string? RecipeId { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public float Progress { get; set; }
    public ProfessionType ProfessionType { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// 职业活动类型
/// </summary>
public enum ProfessionActivityType
{
    Gathering,
    Crafting
}

/// <summary>
/// 职业类型
/// </summary>
public enum ProfessionType : ushort
{
    Mining = 1,
    Herbalism = 2,
    Fishing = 3,
    Blacksmithing = 10,
    Alchemy = 11,
    Cooking = 12,
    Tailoring = 13,
    Leatherworking = 14,
    Jewelcrafting = 15,
    Engineering = 16
}

/// <summary>
/// 采集节点数据
/// </summary>
public class GatheringNodeData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double GatheringTimeSeconds { get; set; }
    public string ResultItemId { get; set; } = string.Empty;
    public int ResultQuantity { get; set; }
    public int XpReward { get; set; }
    public ProfessionType ProfessionType { get; set; }
    public int RequiredLevel { get; set; }
}

/// <summary>
/// 制作配方数据
/// </summary>
public class CraftingRecipeData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double CraftingTimeSeconds { get; set; }
    public string ResultItemId { get; set; } = string.Empty;
    public int ResultQuantity { get; set; }
    public int XpReward { get; set; }
    public ProfessionType ProfessionType { get; set; }
    public int RequiredLevel { get; set; }
    public int MaterialCost { get; set; }
}

/// <summary>
/// 职业服务统计信息
/// </summary>
public struct ProfessionServiceStats
{
    public int ActiveActivities;
    public long TotalActivitiesStarted;
    public long TotalActivitiesCompleted;
    public double CompletionRate;
}

// 事件处理器实现

/// <summary>
/// 采集开始事件处理器
/// </summary>
public class GatheringStartHandler : IUnifiedEventHandler
{
    private readonly EventDrivenProfessionService _professionService;
    private readonly ILogger _logger;

    public GatheringStartHandler(EventDrivenProfessionService professionService, ILogger logger)
    {
        _professionService = professionService;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        var gatheringData = evt.GetData<GatheringEventData>();
        _logger.LogDebug("Handling gathering start event for character {CharacterId}", evt.ActorId);
        // TODO: 通知客户端采集开始
        await Task.CompletedTask;
    }
}

/// <summary>
/// 采集进度事件处理器
/// </summary>
public class GatheringProgressHandler : IUnifiedEventHandler
{
    private readonly EventDrivenProfessionService _professionService;
    private readonly ILogger _logger;

    public GatheringProgressHandler(EventDrivenProfessionService professionService, ILogger logger)
    {
        _professionService = professionService;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        var gatheringData = evt.GetData<GatheringEventData>();
        // TODO: 通知客户端采集进度更新
        await Task.CompletedTask;
    }
}

/// <summary>
/// 采集完成事件处理器
/// </summary>
public class GatheringCompletedHandler : IUnifiedEventHandler
{
    private readonly EventDrivenProfessionService _professionService;
    private readonly ILogger _logger;

    public GatheringCompletedHandler(EventDrivenProfessionService professionService, ILogger logger)
    {
        _professionService = professionService;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        var gatheringData = evt.GetData<GatheringEventData>();
        _logger.LogDebug("Handling gathering completed event for character {CharacterId}", evt.ActorId);
        // TODO: 添加物品到背包，通知客户端
        await Task.CompletedTask;
    }
}

/// <summary>
/// 采集取消事件处理器
/// </summary>
public class GatheringCancelledHandler : IUnifiedEventHandler
{
    private readonly EventDrivenProfessionService _professionService;
    private readonly ILogger _logger;

    public GatheringCancelledHandler(EventDrivenProfessionService professionService, ILogger logger)
    {
        _professionService = professionService;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        // TODO: 通知客户端采集取消
        await Task.CompletedTask;
    }
}

/// <summary>
/// 制作开始事件处理器
/// </summary>
public class CraftingStartHandler : IUnifiedEventHandler
{
    private readonly EventDrivenProfessionService _professionService;
    private readonly ILogger _logger;

    public CraftingStartHandler(EventDrivenProfessionService professionService, ILogger logger)
    {
        _professionService = professionService;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        var craftingData = evt.GetData<CraftingEventData>();
        _logger.LogDebug("Handling crafting start event for character {CharacterId}", evt.ActorId);
        // TODO: 扣除材料，通知客户端制作开始
        await Task.CompletedTask;
    }
}

/// <summary>
/// 制作进度事件处理器
/// </summary>
public class CraftingProgressHandler : IUnifiedEventHandler
{
    private readonly EventDrivenProfessionService _professionService;
    private readonly ILogger _logger;

    public CraftingProgressHandler(EventDrivenProfessionService professionService, ILogger logger)
    {
        _professionService = professionService;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        var craftingData = evt.GetData<CraftingEventData>();
        // TODO: 通知客户端制作进度更新
        await Task.CompletedTask;
    }
}

/// <summary>
/// 制作完成事件处理器
/// </summary>
public class CraftingCompletedHandler : IUnifiedEventHandler
{
    private readonly EventDrivenProfessionService _professionService;
    private readonly ILogger _logger;

    public CraftingCompletedHandler(EventDrivenProfessionService professionService, ILogger logger)
    {
        _professionService = professionService;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        var craftingData = evt.GetData<CraftingEventData>();
        _logger.LogDebug("Handling crafting completed event for character {CharacterId}", evt.ActorId);
        // TODO: 添加物品到背包，通知客户端
        await Task.CompletedTask;
    }
}

/// <summary>
/// 制作取消事件处理器
/// </summary>
public class CraftingCancelledHandler : IUnifiedEventHandler
{
    private readonly EventDrivenProfessionService _professionService;
    private readonly ILogger _logger;

    public CraftingCancelledHandler(EventDrivenProfessionService professionService, ILogger logger)
    {
        _professionService = professionService;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        // TODO: 通知客户端制作取消
        await Task.CompletedTask;
    }
}

/// <summary>
/// 职业经验获得事件处理器
/// </summary>
public class ProfessionXpHandler : IUnifiedEventHandler
{
    private readonly EventDrivenProfessionService _professionService;
    private readonly ILogger _logger;

    public ProfessionXpHandler(EventDrivenProfessionService professionService, ILogger logger)
    {
        _professionService = professionService;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        var expData = evt.GetData<ExperienceEventData>();
        // TODO: 更新角色经验，检查升级
        await Task.CompletedTask;
    }
}

/// <summary>
/// 职业升级事件处理器
/// </summary>
public class ProfessionLevelUpHandler : IUnifiedEventHandler
{
    private readonly EventDrivenProfessionService _professionService;
    private readonly ILogger _logger;

    public ProfessionLevelUpHandler(EventDrivenProfessionService professionService, ILogger logger)
    {
        _professionService = professionService;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        var expData = evt.GetData<ExperienceEventData>();
        _logger.LogInformation("Character {CharacterId} leveled up profession {ProfessionId} to level {NewLevel}", 
            evt.ActorId, expData.ProfessionId, expData.NewLevel);
        // TODO: 通知客户端升级，解锁新内容
        await Task.CompletedTask;
    }
}