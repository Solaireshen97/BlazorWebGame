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
        var completedGatherings = new List<string>();

        foreach (var kvp in _gatheringStates)
        {
            var characterId = kvp.Key;
            var state = kvp.Value;

            if (!state.IsGathering) continue;

            // 更新剩余时间
            state.RemainingTimeSeconds -= deltaTimeSeconds;

            if (state.RemainingTimeSeconds <= 0)
            {
                // 采集完成
                await CompleteGatheringAsync(characterId, state);
                completedGatherings.Add(characterId);
            }
            else
            {
                // 通知客户端进度更新
                await _hubContext.Clients.User(characterId).SendAsync("GatheringProgress", state);
            }
        }

        // 移除已完成的采集
        foreach (var characterId in completedGatherings)
        {
            _gatheringStates.TryRemove(characterId, out _);
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

    // ============================================================================
    // 制作系统方法 (新增)
    // ============================================================================

    // 存储玩家的制作状态
    private readonly ConcurrentDictionary<string, CraftingStateDto> _craftingStates = new();
    
    // 制作配方数据
    private readonly Dictionary<string, RecipeDto> _recipes;

    /// <summary>
    /// 初始化制作配方数据
    /// </summary>
    private Dictionary<string, RecipeDto> InitializeRecipes()
    {
        var recipes = new Dictionary<string, RecipeDto>();

        // 烹饪配方
        recipes["RECIPE_BREAD"] = new RecipeDto
        {
            Id = "RECIPE_BREAD",
            Name = "面包",
            Description = "简单的面包，能恢复少量生命值。",
            RequiredProfession = ProductionProfession.Cooking,
            RequiredLevel = 1,
            CraftingTimeSeconds = 5,
            Ingredients = new Dictionary<string, int> { { "WHEAT", 2 } },
            ResultingItemId = "BREAD",
            ResultingItemQuantity = 1,
            XpReward = 5
        };

        recipes["RECIPE_STEW"] = new RecipeDto
        {
            Id = "RECIPE_STEW",
            Name = "炖菜",
            Description = "营养丰富的炖菜，能大幅恢复生命值。",
            RequiredProfession = ProductionProfession.Cooking,
            RequiredLevel = 3,
            CraftingTimeSeconds = 12,
            Ingredients = new Dictionary<string, int> 
            { 
                { "MEAT", 1 }, 
                { "VEGETABLES", 2 }, 
                { "WATER", 1 } 
            },
            ResultingItemId = "STEW",
            ResultingItemQuantity = 1,
            XpReward = 15
        };

        // 炼金配方
        recipes["RECIPE_HEALTH_POTION"] = new RecipeDto
        {
            Id = "RECIPE_HEALTH_POTION",
            Name = "治疗药水",
            Description = "能立即恢复生命值的药水。",
            RequiredProfession = ProductionProfession.Alchemy,
            RequiredLevel = 1,
            CraftingTimeSeconds = 8,
            Ingredients = new Dictionary<string, int> 
            { 
                { "HERB_HEALING", 1 }, 
                { "WATER", 1 } 
            },
            ResultingItemId = "HEALTH_POTION",
            ResultingItemQuantity = 1,
            XpReward = 8
        };

        // 锻造配方
        recipes["RECIPE_IRON_SWORD"] = new RecipeDto
        {
            Id = "RECIPE_IRON_SWORD",
            Name = "铁剑",
            Description = "用铁锭锻造的基础武器。",
            RequiredProfession = ProductionProfession.Blacksmithing,
            RequiredLevel = 2,
            CraftingTimeSeconds = 20,
            Ingredients = new Dictionary<string, int> 
            { 
                { "IRON_INGOT", 3 }, 
                { "WOOD", 1 } 
            },
            ResultingItemId = "IRON_SWORD",
            ResultingItemQuantity = 1,
            XpReward = 25
        };

        return recipes;
    }

    /// <summary>
    /// 获取所有可用的制作配方
    /// </summary>
    public List<RecipeDto> GetAvailableRecipes(string profession = "")
    {
        var recipes = _recipes.Values.ToList();
        
        if (!string.IsNullOrEmpty(profession) && Enum.TryParse<ProductionProfession>(profession, out var professionEnum))
        {
            recipes = recipes.Where(r => r.RequiredProfession == professionEnum).ToList();
        }

        return recipes;
    }

    /// <summary>
    /// 根据ID获取配方
    /// </summary>
    public RecipeDto? GetRecipeById(string recipeId)
    {
        return _recipes.GetValueOrDefault(recipeId);
    }

    /// <summary>
    /// 开始制作
    /// </summary>
    public async Task<ApiResponse<CraftingStateDto>> StartCraftingAsync(StartCraftingRequest request)
    {
        try
        {
            var recipe = GetRecipeById(request.RecipeId);
            if (recipe == null)
            {
                return new ApiResponse<CraftingStateDto>
                {
                    Success = false,
                    Message = "配方不存在"
                };
            }

            // 检查是否已在制作
            if (_craftingStates.ContainsKey(request.CharacterId))
            {
                return new ApiResponse<CraftingStateDto>
                {
                    Success = false,
                    Message = "已在制作中，请先停止当前制作"
                };
            }

            var startTime = DateTime.UtcNow;
            var craftingState = new CraftingStateDto
            {
                CharacterId = request.CharacterId,
                CurrentRecipeId = request.RecipeId,
                RemainingTimeSeconds = recipe.CraftingTimeSeconds,
                IsCrafting = true,
                StartTime = startTime,
                EstimatedCompletionTime = startTime.AddSeconds(recipe.CraftingTimeSeconds)
            };

            _craftingStates[request.CharacterId] = craftingState;

            _logger.LogInformation("Player {CharacterId} started crafting {RecipeId}, duration: {Duration}s",
                request.CharacterId, request.RecipeId, recipe.CraftingTimeSeconds);

            // 通知客户端制作开始
            await _hubContext.Clients.User(request.CharacterId).SendAsync("CraftingStarted", craftingState);

            return new ApiResponse<CraftingStateDto>
            {
                Success = true,
                Message = "开始制作成功",
                Data = craftingState
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting crafting for player {CharacterId}", request.CharacterId);
            return new ApiResponse<CraftingStateDto>
            {
                Success = false,
                Message = "开始制作时发生错误"
            };
        }
    }

    /// <summary>
    /// 停止制作
    /// </summary>
    public async Task<ApiResponse<string>> StopCraftingAsync(StopCraftingRequest request)
    {
        try
        {
            if (!_craftingStates.TryRemove(request.CharacterId, out var craftingState))
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "角色当前未在制作"
                };
            }

            _logger.LogInformation("Player {CharacterId} stopped crafting {RecipeId}",
                request.CharacterId, craftingState.CurrentRecipeId);

            // 通知客户端制作停止
            await _hubContext.Clients.User(request.CharacterId).SendAsync("CraftingStopped", request.CharacterId);

            return new ApiResponse<string>
            {
                Success = true,
                Message = "停止制作成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping crafting for player {CharacterId}", request.CharacterId);
            return new ApiResponse<string>
            {
                Success = false,
                Message = "停止制作时发生错误"
            };
        }
    }

    /// <summary>
    /// 获取玩家的制作状态
    /// </summary>
    public CraftingStateDto? GetCraftingState(string characterId)
    {
        return _craftingStates.GetValueOrDefault(characterId);
    }

    /// <summary>
    /// 停止所有生产活动
    /// </summary>
    public async Task<ApiResponse<string>> StopAllProductionAsync(StopAllProductionRequest request)
    {
        try
        {
            bool stoppedAny = false;

            // 停止采集
            if (_gatheringStates.TryRemove(request.CharacterId, out var gatheringState))
            {
                stoppedAny = true;
                _logger.LogInformation("Stopped gathering for player {CharacterId}", request.CharacterId);
            }

            // 停止制作
            if (_craftingStates.TryRemove(request.CharacterId, out var craftingState))
            {
                stoppedAny = true;
                _logger.LogInformation("Stopped crafting for player {CharacterId}", request.CharacterId);
            }

            if (stoppedAny)
            {
                // 通知客户端所有生产活动已停止
                await _hubContext.Clients.User(request.CharacterId).SendAsync("AllProductionStopped", request.CharacterId);
            }

            return new ApiResponse<string>
            {
                Success = true,
                Message = stoppedAny ? "停止所有生产活动成功" : "没有活跃的生产活动"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping all production for player {CharacterId}", request.CharacterId);
            return new ApiResponse<string>
            {
                Success = false,
                Message = "停止生产活动时发生错误"
            };
        }
    }

    /// <summary>
    /// 游戏循环处理制作状态
    /// </summary>
    public async Task ProcessCraftingTickAsync(double deltaTime)
    {
        var completedCrafting = new List<string>();

        foreach (var kvp in _craftingStates)
        {
            var characterId = kvp.Key;
            var state = kvp.Value;

            if (!state.IsCrafting) continue;

            state.RemainingTimeSeconds -= deltaTime;

            if (state.RemainingTimeSeconds <= 0)
            {
                completedCrafting.Add(characterId);
            }
        }

        // 处理完成的制作
        foreach (var characterId in completedCrafting)
        {
            await CompleteCraftingAsync(characterId);
        }
    }

    /// <summary>
    /// 完成制作
    /// </summary>
    private async Task CompleteCraftingAsync(string characterId)
    {
        try
        {
            if (!_craftingStates.TryRemove(characterId, out var state) || 
                string.IsNullOrEmpty(state.CurrentRecipeId))
            {
                return;
            }

            var recipe = GetRecipeById(state.CurrentRecipeId);
            if (recipe == null) return;

            // 创建制作结果
            var result = new CraftingResultDto
            {
                Success = true,
                ItemId = recipe.ResultingItemId,
                Quantity = recipe.ResultingItemQuantity,
                XpGained = recipe.XpReward,
                Message = $"成功制作了 {recipe.Name}"
            };

            _logger.LogInformation("Player {CharacterId} completed crafting {RecipeId}, gained {ItemId} x{Quantity} and {Xp} XP",
                characterId, recipe.Id, result.ItemId, result.Quantity, result.XpGained);

            // 通知客户端制作完成
            await _hubContext.Clients.User(characterId).SendAsync("CraftingCompleted", result);

            // TODO: 这里应该调用库存服务来添加物品，调用角色服务来添加经验
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing crafting for player {CharacterId}", characterId);
        }
    }

    /// <summary>
    /// 制作结果 DTO
    /// </summary>
    public class CraftingResultDto
    {
        public bool Success { get; set; }
        public string ItemId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int XpGained { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}