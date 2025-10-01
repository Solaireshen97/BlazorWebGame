using BlazorWebGame.Shared.Events;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using BlazorWebGame.Server.Services.Inventory;
using BlazorWebGame.Server.Services.System;

namespace BlazorWebGame.Server.Services.Profession;

/// <summary>
/// 基于事件队列的服务端职业系统服务 - 优化版本
/// 统一管理采集、制作等生产活动，使用事件驱动架构，优化性能和错误处理
/// </summary>
public class EventDrivenProfessionService : IDisposable
{
    private readonly UnifiedEventService _eventService;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<EventDrivenProfessionService> _logger;
    private readonly ServerInventoryService _inventoryService;

    // 活跃的职业活动状态
    private readonly ConcurrentDictionary<string, ProfessionActivityState> _activeActivities = new();
    
    // 节点和配方数据缓存
    private readonly Dictionary<string, GatheringNodeData> _gatheringNodes;
    private readonly Dictionary<string, CraftingRecipeData> _craftingRecipes;
    
    // 性能统计和监控
    private long _totalActivitiesStarted = 0;
    private long _totalActivitiesCompleted = 0;
    private long _totalEventsGenerated = 0;
    private readonly object _statsLock = new();
    
    // 事件批处理优化
    private readonly List<UnifiedEvent> _eventBatch = new(256);
    private readonly object _eventBatchLock = new();
    private readonly Timer _batchFlushTimer;
    
    // 错误处理和重试机制
    private readonly Dictionary<string, int> _activityRetryCount = new();
    private const int MAX_RETRY_COUNT = 3;

    public EventDrivenProfessionService(
        UnifiedEventService eventService,
        IHubContext<GameHub> hubContext,
        ILogger<EventDrivenProfessionService> logger,
        ServerInventoryService inventoryService)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));

        // 初始化数据
        _gatheringNodes = InitializeGatheringNodes();
        _craftingRecipes = InitializeCraftingRecipes();

        // 注册职业事件处理器
        RegisterProfessionEventHandlers();
        
        // 启动批处理刷新定时器（每秒刷新一次未满的批次）
        _batchFlushTimer = new Timer(FlushEventBatch, null, 
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
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

            // 使用优化的批处理事件系统
            EnqueueOptimizedGatheringEvent(GameEventTypes.GATHERING_STARTED, gatheringData, 
                EventPriority.Gameplay, (ulong)HashString(characterId), (ulong)HashString(nodeId));

            lock (_statsLock)
            {
                _totalActivitiesStarted++;
                _totalEventsGenerated++;
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

            EnqueueOptimizedCraftingEvent(GameEventTypes.CRAFTING_STARTED, craftingData,
                EventPriority.Gameplay, (ulong)HashString(characterId), (ulong)HashString(recipeId));

            lock (_statsLock)
            {
                _totalActivitiesStarted++;
                _totalEventsGenerated++;
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

            _eventService.EnqueueEvent(eventType, EventPriority.Gameplay, 0, 0);

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
                // 完成当前周期并自动开始下一周期（类似战斗系统的持续执行）
                await CompleteAndRestartActivityAsync(activity);
            }
        }

        // 不再移除已完成的活动，而是让它们自动重复
        // 这实现了类似战斗系统的连续执行模式
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

            EnqueueOptimizedGatheringEvent(GameEventTypes.GATHERING_PROGRESS, gatheringData,
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

            EnqueueOptimizedCraftingEvent(GameEventTypes.CRAFTING_PROGRESS, craftingData,
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

                EnqueueOptimizedGatheringEvent(GameEventTypes.GATHERING_COMPLETED, gatheringData,
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

                EnqueueOptimizedCraftingEvent(GameEventTypes.CRAFTING_COMPLETED, craftingData,
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

            EnqueueOptimizedExperienceEvent(GameEventTypes.PROFESSION_XP_GAINED, expData,
                EventPriority.Gameplay, (ulong)HashString(activity.CharacterId));

            lock (_statsLock)
            {
                _totalActivitiesCompleted++;
                _totalEventsGenerated++;
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
    /// 完成当前活动周期并自动重启下一周期 - 实现类似战斗系统的连续执行
    /// </summary>
    private async Task CompleteAndRestartActivityAsync(ProfessionActivityState activity)
    {
        try
        {
            // 先完成当前周期
            await CompleteActivityAsync(activity);

            // 检查是否应该继续（类似战斗系统检查是否有敌人）
            var shouldContinue = ShouldContinueActivity(activity);
            
            if (shouldContinue)
            {
                // 重启活动进入下一周期
                RestartActivityForNextCycle(activity);
                
                _logger.LogDebug("Character {CharacterId} automatically restarted {ActivityType} activity", 
                    activity.CharacterId, activity.ActivityType);
            }
            else
            {
                // 条件不满足，停止活动（例如制作材料不足）
                await StopCurrentActivityAsync(activity.CharacterId);
                
                _logger.LogInformation("Character {CharacterId} stopped {ActivityType} activity due to conditions not met", 
                    activity.CharacterId, activity.ActivityType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing and restarting activity for character {CharacterId}", activity.CharacterId);
            
            // 发生错误时停止活动以防止无限循环错误
            await StopCurrentActivityAsync(activity.CharacterId);
        }
    }

    /// <summary>
    /// 检查活动是否应该继续（类似战斗系统检查战斗条件）
    /// </summary>
    private bool ShouldContinueActivity(ProfessionActivityState activity)
    {
        try
        {
            if (activity.ActivityType == ProfessionActivityType.Gathering)
            {
                // 采集活动始终可以继续，除非资源节点耗尽（这里简化为始终可以）
                return _gatheringNodes.ContainsKey(activity.NodeId!);
            }
            else if (activity.ActivityType == ProfessionActivityType.Crafting)
            {
                // 制作活动需要检查材料是否充足
                if (!_craftingRecipes.TryGetValue(activity.RecipeId!, out var recipe))
                {
                    return false;
                }
                
                // TODO: 实现真正的材料检查
                // 当前的CraftingRecipeData只有MaterialCost (int)，不是详细的材料列表
                // 这里暂时简化为始终可以继续，实际应该根据具体材料需求检查
                _logger.LogDebug("Continuing crafting for character {CharacterId} - material checking simplified", 
                    activity.CharacterId);
                
                return true; // 暂时始终允许继续制作
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if activity should continue for character {CharacterId}", activity.CharacterId);
            return false; // 出错时停止活动
        }
    }

    /// <summary>
    /// 重启活动进入下一周期
    /// </summary>
    private void RestartActivityForNextCycle(ProfessionActivityState activity)
    {
        // 重置活动状态开始新周期
        activity.StartTime = DateTime.UtcNow;
        activity.Progress = 0.0f;
        activity.IsActive = true;

        // 根据活动类型设置持续时间
        if (activity.ActivityType == ProfessionActivityType.Gathering && _gatheringNodes.TryGetValue(activity.NodeId!, out var node))
        {
            activity.Duration = TimeSpan.FromSeconds(node.GatheringTimeSeconds);
        }
        else if (activity.ActivityType == ProfessionActivityType.Crafting && _craftingRecipes.TryGetValue(activity.RecipeId!, out var recipe))
        {
            activity.Duration = TimeSpan.FromSeconds(recipe.CraftingTimeSeconds);
        }

        // 生成重启事件
        var eventType = activity.ActivityType == ProfessionActivityType.Gathering 
            ? GameEventTypes.GATHERING_STARTED 
            : GameEventTypes.CRAFTING_STARTED;

        _eventService.EnqueueEvent(eventType, EventPriority.Gameplay, 
            (ulong)HashString(activity.CharacterId), 
            activity.ActivityType == ProfessionActivityType.Gathering 
                ? (ulong)HashString(activity.NodeId!) 
                : (ulong)HashString(activity.RecipeId!));

        lock (_statsLock)
        {
            _totalActivitiesStarted++; // 统计重新开始的活动
            _totalEventsGenerated++;
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

    /// <summary>
    /// 优化的事件入队方法 - 针对采集事件
    /// </summary>
    private void EnqueueOptimizedGatheringEvent(ushort eventType, GatheringEventData data, EventPriority priority, 
        ulong actorId, ulong targetId = 0)
    {
        try
        {
            var evt = new UnifiedEvent(eventType, priority)
            {
                ActorId = actorId,
                TargetId = targetId
            };
            evt.SetData(data);

            lock (_eventBatchLock)
            {
                _eventBatch.Add(evt);
                
                // 当批次达到一定大小时立即刷新
                if (_eventBatch.Count >= 64)
                {
                    FlushEventBatchInternal();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueueing optimized gathering event for actor {ActorId}", actorId);
            // 作为备用方案，直接入队单个事件
            _eventService.EnqueueEvent(eventType, data, priority, actorId, targetId);
        }
    }

    /// <summary>
    /// 优化的事件入队方法 - 针对制作事件
    /// </summary>
    private void EnqueueOptimizedCraftingEvent(ushort eventType, CraftingEventData data, EventPriority priority, 
        ulong actorId, ulong targetId = 0)
    {
        try
        {
            var evt = new UnifiedEvent(eventType, priority)
            {
                ActorId = actorId,
                TargetId = targetId
            };
            evt.SetData(data);

            lock (_eventBatchLock)
            {
                _eventBatch.Add(evt);
                
                // 当批次达到一定大小时立即刷新
                if (_eventBatch.Count >= 64)
                {
                    FlushEventBatchInternal();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueueing optimized crafting event for actor {ActorId}", actorId);
            // 作为备用方案，直接入队单个事件
            _eventService.EnqueueEvent(eventType, data, priority, actorId, targetId);
        }
    }

    /// <summary>
    /// 优化的事件入队方法 - 针对经验事件
    /// </summary>
    private void EnqueueOptimizedExperienceEvent(ushort eventType, ExperienceEventData data, EventPriority priority, 
        ulong actorId, ulong targetId = 0)
    {
        try
        {
            var evt = new UnifiedEvent(eventType, priority)
            {
                ActorId = actorId,
                TargetId = targetId
            };
            evt.SetData(data);

            lock (_eventBatchLock)
            {
                _eventBatch.Add(evt);
                
                // 当批次达到一定大小时立即刷新
                if (_eventBatch.Count >= 64)
                {
                    FlushEventBatchInternal();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueueing optimized experience event for actor {ActorId}", actorId);
            // 作为备用方案，直接入队单个事件
            _eventService.EnqueueEvent(eventType, data, priority, actorId, targetId);
        }
    }

    /// <summary>
    /// 刷新事件批次
    /// </summary>
    private void FlushEventBatch(object? state)
    {
        lock (_eventBatchLock)
        {
            FlushEventBatchInternal();
        }
    }

    /// <summary>
    /// 内部批次刷新实现
    /// </summary>
    private void FlushEventBatchInternal()
    {
        if (_eventBatch.Count == 0) return;

        try
        {
            var eventsToFlush = _eventBatch.ToArray();
            var enqueuedCount = _eventService.EnqueueBatch(eventsToFlush, eventsToFlush.Length);
            
            if (enqueuedCount != eventsToFlush.Length)
            {
                _logger.LogWarning("Failed to enqueue {FailedCount} out of {TotalCount} profession events", 
                    eventsToFlush.Length - enqueuedCount, eventsToFlush.Length);
            }
            
            _eventBatch.Clear();
            
            lock (_statsLock)
            {
                _totalEventsGenerated += enqueuedCount;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing profession event batch");
            _eventBatch.Clear(); // 清空批次避免无限循环
        }
    }

    /// <summary>
    /// 获取性能统计信息
    /// </summary>
    public ProfessionServiceStats GetStats()
    {
        lock (_statsLock)
        {
            return new ProfessionServiceStats
            {
                ActiveActivities = _activeActivities.Count,
                TotalActivitiesStarted = _totalActivitiesStarted,
                TotalActivitiesCompleted = _totalActivitiesCompleted,
                TotalEventsGenerated = _totalEventsGenerated,
                AvailableGatheringNodes = _gatheringNodes.Count,
                AvailableCraftingRecipes = _craftingRecipes.Count
            };
        }
    }

    /// <summary>
    /// 重试失败的活动
    /// </summary>
    private bool ShouldRetryActivity(string characterId)
    {
        if (!_activityRetryCount.TryGetValue(characterId, out var retryCount))
        {
            _activityRetryCount[characterId] = 1;
            return true;
        }

        if (retryCount < MAX_RETRY_COUNT)
        {
            _activityRetryCount[characterId] = retryCount + 1;
            return true;
        }

        _activityRetryCount.Remove(characterId);
        return false;
    }

    /// <summary>
    /// 清理重试计数
    /// </summary>
    private void ClearRetryCount(string characterId)
    {
        _activityRetryCount.Remove(characterId);
    }

    public void Dispose()
    {
        _batchFlushTimer?.Dispose();
        
        // 刷新剩余的事件批次
        lock (_eventBatchLock)
        {
            FlushEventBatchInternal();
        }
        
        _activeActivities.Clear();
        _activityRetryCount.Clear();
    }
}

/// <summary>
/// 职业服务性能统计
/// </summary>
public class ProfessionServiceStats
{
    public int ActiveActivities { get; set; }
    public long TotalActivitiesStarted { get; set; }
    public long TotalActivitiesCompleted { get; set; }
    public long TotalEventsGenerated { get; set; }
    public int AvailableGatheringNodes { get; set; }
    public int AvailableCraftingRecipes { get; set; }
    public double CompletionRate => TotalActivitiesStarted > 0 ? (double)TotalActivitiesCompleted / TotalActivitiesStarted : 0.0;
    public double EventsPerActivity => TotalActivitiesCompleted > 0 ? (double)TotalEventsGenerated / TotalActivitiesCompleted : 0.0;
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