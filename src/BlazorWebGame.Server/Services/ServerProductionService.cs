using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using BlazorWebGame.Server.Hubs;
using System.Collections.Concurrent;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 服务端生产系统服务 - 处理采集、制作等生产活动
/// </summary>
public class ServerProductionService
{
    private readonly ILogger<ServerProductionService> _logger;
    private readonly IHubContext<GameHub> _hubContext;

    // 存储玩家的采集状态
    private readonly ConcurrentDictionary<string, GatheringStateDto> _gatheringStates = new();
    
    // 采集节点数据 - 从客户端迁移过来的静态数据
    private readonly Dictionary<string, GatheringNodeDto> _gatheringNodes;

    public ServerProductionService(ILogger<ServerProductionService> logger, IHubContext<GameHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
        _gatheringNodes = InitializeGatheringNodes();
        _recipes = InitializeRecipes();
    }

    /// <summary>
    /// 初始化采集节点数据
    /// </summary>
    private Dictionary<string, GatheringNodeDto> InitializeGatheringNodes()
    {
        var nodes = new Dictionary<string, GatheringNodeDto>();

        // 采矿节点
        nodes["NODE_COPPER_VEIN"] = new GatheringNodeDto
        {
            Id = "NODE_COPPER_VEIN",
            Name = "铜矿脉",
            Description = "富含铜矿石的矿脉，对新手矿工来说是很好的练手材料。",
            GatheringTimeSeconds = 7,
            ResultingItemId = "ORE_COPPER",
            XpReward = 7,
            RequiredProfession = "Mining",
            RequiredLevel = 1
        };

        nodes["NODE_IRON_VEIN"] = new GatheringNodeDto
        {
            Id = "NODE_IRON_VEIN",
            Name = "铁矿脉",
            Description = "更为坚硬的矿物，通常深埋在岩层中。",
            GatheringTimeSeconds = 12,
            ResultingItemId = "ORE_IRON",
            XpReward = 15,
            RequiredProfession = "Mining",
            RequiredLevel = 10
        };

        // 草药学节点
        nodes["NODE_HEALING_HERB"] = new GatheringNodeDto
        {
            Id = "NODE_HEALING_HERB",
            Name = "治疗草药",
            Description = "具有治疗属性的常见草药。",
            GatheringTimeSeconds = 5,
            ResultingItemId = "HERB_HEALING",
            XpReward = 5,
            RequiredProfession = "Herbalist",
            RequiredLevel = 1
        };

        nodes["NODE_MANA_FLOWER"] = new GatheringNodeDto
        {
            Id = "NODE_MANA_FLOWER",
            Name = "法力花",
            Description = "蕴含魔法能量的神秘花朵。",
            GatheringTimeSeconds = 8,
            ResultingItemId = "HERB_MANA",
            XpReward = 10,
            RequiredProfession = "Herbalist",
            RequiredLevel = 5
        };

        // 钓鱼节点
        nodes["NODE_FRESHWATER_FISH"] = new GatheringNodeDto
        {
            Id = "NODE_FRESHWATER_FISH",
            Name = "淡水鱼群",
            Description = "在清澈的淡水中游泳的鱼群。",
            GatheringTimeSeconds = 10,
            ResultingItemId = "FISH_COMMON",
            XpReward = 8,
            RequiredProfession = "Fishing",
            RequiredLevel = 1
        };

        return nodes;
    }

    /// <summary>
    /// 获取所有可用的采集节点
    /// </summary>
    public List<GatheringNodeDto> GetAvailableNodes(string profession = "")
    {
        if (string.IsNullOrEmpty(profession))
        {
            return _gatheringNodes.Values.ToList();
        }

        return _gatheringNodes.Values
            .Where(n => n.RequiredProfession.Equals(profession, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// 根据ID获取采集节点
    /// </summary>
    public GatheringNodeDto? GetNodeById(string nodeId)
    {
        _gatheringNodes.TryGetValue(nodeId, out var node);
        return node;
    }

    /// <summary>
    /// 开始采集
    /// </summary>
    public async Task<ApiResponse<GatheringStateDto>> StartGatheringAsync(StartGatheringRequest request)
    {
        try
        {
            // 验证节点是否存在
            var node = GetNodeById(request.NodeId);
            if (node == null)
            {
                return new ApiResponse<GatheringStateDto>
                {
                    Success = false,
                    Message = "采集节点不存在"
                };
            }

            // 检查玩家是否已经在采集
            if (_gatheringStates.TryGetValue(request.CharacterId, out var currentState) && currentState.IsGathering)
            {
                return new ApiResponse<GatheringStateDto>
                {
                    Success = false,
                    Message = "玩家已经在进行采集活动"
                };
            }

            // 创建新的采集状态
            var startTime = DateTime.UtcNow;
            var newState = new GatheringStateDto
            {
                CharacterId = request.CharacterId,
                CurrentNodeId = request.NodeId,
                RemainingTimeSeconds = node.GatheringTimeSeconds,
                IsGathering = true,
                StartTime = startTime,
                EstimatedCompletionTime = startTime.AddSeconds(node.GatheringTimeSeconds)
            };

            _gatheringStates[request.CharacterId] = newState;

            _logger.LogInformation("Player {CharacterId} started gathering at node {NodeId} for {Duration} seconds",
                request.CharacterId, request.NodeId, node.GatheringTimeSeconds);

            // 通知客户端采集开始
            await _hubContext.Clients.User(request.CharacterId).SendAsync("GatheringStarted", newState);

            return new ApiResponse<GatheringStateDto>
            {
                Success = true,
                Message = "采集开始",
                Data = newState
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting gathering for player {CharacterId}", request.CharacterId);
            return new ApiResponse<GatheringStateDto>
            {
                Success = false,
                Message = "开始采集时发生错误"
            };
        }
    }

    /// <summary>
    /// 停止采集
    /// </summary>
    public async Task<ApiResponse<string>> StopGatheringAsync(StopGatheringRequest request)
    {
        try
        {
            if (_gatheringStates.TryRemove(request.CharacterId, out var state))
            {
                _logger.LogInformation("Player {CharacterId} stopped gathering", request.CharacterId);
                
                // 通知客户端采集停止
                await _hubContext.Clients.User(request.CharacterId).SendAsync("GatheringStopped");

                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "采集已停止"
                };
            }

            return new ApiResponse<string>
            {
                Success = false,
                Message = "玩家当前未在进行采集"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping gathering for player {CharacterId}", request.CharacterId);
            return new ApiResponse<string>
            {
                Success = false,
                Message = "停止采集时发生错误"
            };
        }
    }

    /// <summary>
    /// 停止采集（内部使用的简化版本）
    /// </summary>
    private async Task StopGatheringAsync(string characterId)
    {
        if (_gatheringStates.TryRemove(characterId, out var state))
        {
            _logger.LogInformation("Player {CharacterId} stopped gathering (auto)", characterId);
            
            // 通知客户端采集停止
            await _hubContext.Clients.User(characterId).SendAsync("GatheringStopped");
        }
    }

    /// <summary>
    /// 获取玩家的采集状态
    /// </summary>
    public GatheringStateDto? GetGatheringState(string characterId)
    {
        _gatheringStates.TryGetValue(characterId, out var state);
        return state;
    }

    /// <summary>
    /// 更新采集状态 - 由游戏循环调用
    /// </summary>
    public async Task UpdateGatheringStatesAsync(double deltaTimeSeconds)
    {
        var statesToProcess = _gatheringStates.ToArray(); // 避免在迭代时修改集合

        foreach (var kvp in statesToProcess)
        {
            var characterId = kvp.Key;
            var state = kvp.Value;

            if (!state.IsGathering) continue;

            // 更新剩余时间
            state.RemainingTimeSeconds -= deltaTimeSeconds;

            if (state.RemainingTimeSeconds <= 0)
            {
                // 采集完成 - 实现类似战斗系统的自动重复机制
                await CompleteAndRestartGatheringAsync(characterId, state);
            }
            else
            {
                // 通知客户端进度更新
                await _hubContext.Clients.User(characterId).SendAsync("GatheringProgress", state);
            }
        }

        // 不再移除已完成的采集状态，让它们自动重复
        // 这实现了类似战斗系统的连续执行模式
    }

    /// <summary>
    /// 完成当前采集周期并自动重启下一周期 - 实现类似战斗系统的连续执行
    /// </summary>
    private async Task CompleteAndRestartGatheringAsync(string characterId, GatheringStateDto state)
    {
        try
        {
            // 先完成当前周期
            await CompleteGatheringAsync(characterId, state);

            // 检查是否应该继续采集（类似战斗系统检查是否有敌人）
            var shouldContinue = ShouldContinueGathering(characterId, state);
            
            if (shouldContinue)
            {
                // 重启采集进入下一周期
                RestartGatheringForNextCycle(state);
                
                _logger.LogDebug("Character {CharacterId} automatically restarted gathering at node {NodeId}", 
                    characterId, state.CurrentNodeId);
            }
            else
            {
                // 条件不满足，停止采集
                await StopGatheringAsync(characterId);
                
                _logger.LogInformation("Character {CharacterId} stopped gathering due to conditions not met", 
                    characterId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing and restarting gathering for character {CharacterId}", characterId);
            
            // 发生错误时停止采集以防止无限循环错误
            await StopGatheringAsync(characterId);
        }
    }

    /// <summary>
    /// 检查是否应该继续采集（类似战斗系统检查战斗条件）
    /// </summary>
    private bool ShouldContinueGathering(string characterId, GatheringStateDto state)
    {
        try
        {
            // 检查采集节点是否仍然存在
            var node = GetNodeById(state.CurrentNodeId!);
            if (node == null)
            {
                _logger.LogWarning("Gathering node {NodeId} no longer exists", state.CurrentNodeId);
                return false;
            }

            // TODO: 未来可以添加更多条件检查：
            // - 角色是否还在节点附近
            // - 节点是否已被耗尽
            // - 角色背包是否已满
            // - 角色是否有足够的工具耐久度
            
            return true; // 目前简化为始终可以继续
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if gathering should continue for character {CharacterId}", characterId);
            return false; // 出错时停止采集
        }
    }

    /// <summary>
    /// 重启采集进入下一周期
    /// </summary>
    private void RestartGatheringForNextCycle(GatheringStateDto state)
    {
        var node = GetNodeById(state.CurrentNodeId!);
        if (node != null)
        {
            // 重置采集状态开始新周期
            state.RemainingTimeSeconds = node.GatheringTimeSeconds;
            state.IsGathering = true;
            state.StartTime = DateTime.UtcNow;
            
            _logger.LogDebug("Restarted gathering cycle for node {NodeId}, duration: {Duration}s", 
                node.Id, node.GatheringTimeSeconds);
        }
    }

    /// <summary>
    /// 完成采集
    /// </summary>
    private async Task CompleteGatheringAsync(string characterId, GatheringStateDto state)
    {
        try
        {
            var node = GetNodeById(state.CurrentNodeId!);
            if (node == null)
            {
                _logger.LogWarning("Could not find node {NodeId} for completed gathering", state.CurrentNodeId);
                return;
            }

            // 计算采集结果
            var result = new GatheringResultDto
            {
                Success = true,
                ItemId = node.ResultingItemId,
                Quantity = node.ResultingItemQuantity,
                XpGained = node.XpReward,
                ExtraLoot = false, // TODO: 实现额外掉落逻辑
                Message = $"成功采集到 {node.Name}!"
            };

            _logger.LogInformation("Player {CharacterId} completed gathering {NodeId}, gained {ItemId} x{Quantity} and {Xp} XP",
                characterId, node.Id, result.ItemId, result.Quantity, result.XpGained);

            // 通知客户端采集完成
            await _hubContext.Clients.User(characterId).SendAsync("GatheringCompleted", result);

            // TODO: 这里应该调用库存服务来添加物品，调用角色服务来添加经验
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing gathering for player {CharacterId}", characterId);
        }
    }

    /// <summary>
    /// 获取所有活跃的采集状态（用于管理和调试）
    /// </summary>
    public List<GatheringStateDto> GetAllActiveGatheringStates()
    {
        return _gatheringStates.Values.Where(s => s.IsGathering).ToList();
    }

    // ==================== 制作系统方法 ====================

    private readonly ConcurrentDictionary<string, CraftingStateDto> _craftingStates = new();
    private readonly Dictionary<string, RecipeDto> _recipes;

    /// <summary>
    /// 初始化配方数据（需要从客户端迁移过来）
    /// </summary>
    private Dictionary<string, RecipeDto> InitializeRecipes()
    {
        var recipes = new Dictionary<string, RecipeDto>();

        // 炼金配方示例
        recipes["POTION_MINOR_HEALTH"] = new RecipeDto
        {
            Id = "POTION_MINOR_HEALTH",
            Name = "初级生命药水",
            RequiredProfession = "Alchemy",
            RequiredLevel = 1,
            Ingredients = new Dictionary<string, int>
            {
                ["HERB_COMMON_GRASS"] = 2,
                ["ITEM_EMPTY_VIAL"] = 1
            },
            ResultingItemId = "POTION_MINOR_HEALTH",
            ResultingItemQuantity = 1,
            CraftingTimeSeconds = 5.0,
            XpReward = 5,
            IsDefault = true
        };

        // 工程学配方示例
        recipes["TRAP_BASIC"] = new RecipeDto
        {
            Id = "TRAP_BASIC",
            Name = "基础陷阱",
            RequiredProfession = "Engineering",
            RequiredLevel = 1,
            Ingredients = new Dictionary<string, int>
            {
                ["ORE_COPPER"] = 3,
                ["ITEM_ROPE"] = 1
            },
            ResultingItemId = "TRAP_BASIC",
            ResultingItemQuantity = 1,
            CraftingTimeSeconds = 8.0,
            XpReward = 8,
            IsDefault = true
        };

        // TODO: 这里应该从完整的配方数据文件中加载所有配方
        return recipes;
    }

    /// <summary>
    /// 获取角色可用的配方列表
    /// </summary>
    public List<RecipeDto> GetAvailableRecipes(string characterId, string? profession = null, int? maxLevel = null)
    {
        // TODO: 这里应该根据角色的职业等级和学习状态过滤配方
        var filteredRecipes = _recipes.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(profession))
        {
            filteredRecipes = filteredRecipes.Where(r => r.RequiredProfession.Equals(profession, StringComparison.OrdinalIgnoreCase));
        }

        if (maxLevel.HasValue)
        {
            filteredRecipes = filteredRecipes.Where(r => r.RequiredLevel <= maxLevel.Value);
        }

        return filteredRecipes.ToList();
    }

    /// <summary>
    /// 根据ID获取配方
    /// </summary>
    public RecipeDto? GetRecipeById(string recipeId)
    {
        _recipes.TryGetValue(recipeId, out var recipe);
        return recipe;
    }

    /// <summary>
    /// 开始制作
    /// </summary>
    public async Task<ApiResponse<bool>> StartCraftingAsync(string characterId, string recipeId, int quantity = 1)
    {
        try
        {
            // 验证配方是否存在
            var recipe = GetRecipeById(recipeId);
            if (recipe == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "配方不存在"
                };
            }

            // 检查玩家是否已经在制作
            if (_craftingStates.TryGetValue(characterId, out var currentState) && currentState.IsCrafting)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "玩家已经在进行制作活动"
                };
            }

            // TODO: 验证玩家职业等级和材料
            // TODO: 消耗材料

            var totalCraftingTime = recipe.CraftingTimeSeconds * quantity;
            var craftingState = new CraftingStateDto
            {
                CharacterId = characterId,
                CurrentRecipeId = recipeId,
                TotalQuantity = quantity,
                CompletedQuantity = 0,
                RemainingTimeSeconds = totalCraftingTime,
                IsCrafting = true,
                StartTime = DateTime.UtcNow,
                EstimatedCompletionTime = DateTime.UtcNow.AddSeconds(totalCraftingTime)
            };

            _craftingStates[characterId] = craftingState;

            // 通知客户端制作开始
            await _hubContext.Clients.User(characterId).SendAsync("CraftingStarted", craftingState);

            // 开始制作计时器
            _ = Task.Run(async () => await ProcessCraftingAsync(characterId, totalCraftingTime));

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "开始制作成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting crafting for character {CharacterId}", characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "开始制作时发生错误"
            };
        }
    }

    /// <summary>
    /// 批量制作
    /// </summary>
    public async Task<ApiResponse<bool>> StartBatchCraftingAsync(string characterId, List<BatchCraftingItem> items)
    {
        try
        {
            // 验证所有配方是否存在
            foreach (var item in items)
            {
                var recipe = GetRecipeById(item.RecipeId);
                if (recipe == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = $"配方 {item.RecipeId} 不存在"
                    };
                }
            }

            // 检查玩家是否已经在制作
            if (_craftingStates.TryGetValue(characterId, out var currentState) && currentState.IsCrafting)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "玩家已经在进行制作活动"
                };
            }

            // TODO: 验证材料和职业等级
            // TODO: 实现批量制作逻辑

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "批量制作暂未实现，请使用单个制作"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting batch crafting for character {CharacterId}", characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "开始批量制作时发生错误"
            };
        }
    }

    /// <summary>
    /// 停止制作
    /// </summary>
    public async Task<ApiResponse<CraftingResultDto>> StopCraftingAsync(string characterId)
    {
        try
        {
            if (!_craftingStates.TryGetValue(characterId, out var state) || !state.IsCrafting)
            {
                return new ApiResponse<CraftingResultDto>
                {
                    Success = false,
                    Message = "玩家当前未在制作"
                };
            }

            // 停止制作
            state.IsCrafting = false;
            _craftingStates.TryRemove(characterId, out _);

            var result = new CraftingResultDto
            {
                Success = true,
                Message = "制作已停止",
                Quantity = state.CompletedQuantity
            };

            await _hubContext.Clients.User(characterId).SendAsync("CraftingStopped", result);

            return new ApiResponse<CraftingResultDto>
            {
                Success = true,
                Message = "停止制作成功",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping crafting for character {CharacterId}", characterId);
            return new ApiResponse<CraftingResultDto>
            {
                Success = false,
                Message = "停止制作时发生错误"
            };
        }
    }

    /// <summary>
    /// 获取制作状态
    /// </summary>
    public CraftingStateDto? GetCraftingState(string characterId)
    {
        _craftingStates.TryGetValue(characterId, out var state);
        return state;
    }

    /// <summary>
    /// 检查节点解锁状态
    /// </summary>
    public NodeUnlockStatusDto CheckNodeUnlockStatus(string characterId, string nodeId)
    {
        // TODO: 这里应该获取角色信息并检查解锁条件
        var node = GetNodeById(nodeId);
        if (node == null)
        {
            return new NodeUnlockStatusDto
            {
                IsUnlocked = false,
                Reason = "节点不存在"
            };
        }

        // 临时实现 - 应该从实际角色数据中获取
        return new NodeUnlockStatusDto
        {
            IsUnlocked = true,
            Reason = "",
            RequiredLevel = node.RequiredLevel
        };
    }

    /// <summary>
    /// 检查制作材料是否充足
    /// </summary>
    public bool CheckCraftingMaterials(string characterId, string recipeId, int quantity = 1)
    {
        var recipe = GetRecipeById(recipeId);
        if (recipe == null) return false;

        // TODO: 这里应该检查角色库存中的实际材料数量
        // 临时返回true
        return true;
    }

    /// <summary>
    /// 处理制作进度
    /// </summary>
    private async Task ProcessCraftingAsync(string characterId, double totalTime)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(totalTime));

            if (!_craftingStates.TryGetValue(characterId, out var state) || !state.IsCrafting)
            {
                return; // 制作已被停止
            }

            // 完成制作
            await CompleteCraftingAsync(characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing crafting for player {CharacterId}", characterId);
        }
    }

    /// <summary>
    /// 完成制作
    /// </summary>
    private async Task CompleteCraftingAsync(string characterId)
    {
        try
        {
            if (!_craftingStates.TryGetValue(characterId, out var state))
            {
                return;
            }

            var recipe = GetRecipeById(state.CurrentRecipeId!);
            if (recipe == null)
            {
                return;
            }

            // 移除制作状态
            _craftingStates.TryRemove(characterId, out _);

            var result = new CraftingResultDto
            {
                Success = true,
                ItemId = recipe.ResultingItemId,
                Quantity = recipe.ResultingItemQuantity * state.TotalQuantity,
                XpGained = recipe.XpReward * state.TotalQuantity,
                Message = $"成功制作了 {recipe.Name} x{state.TotalQuantity}"
            };

            // 通知客户端制作完成
            await _hubContext.Clients.User(characterId).SendAsync("CraftingCompleted", result);

            // TODO: 这里应该调用库存服务来添加物品，调用角色服务来添加经验
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing crafting for player {CharacterId}", characterId);
        }
    }
}