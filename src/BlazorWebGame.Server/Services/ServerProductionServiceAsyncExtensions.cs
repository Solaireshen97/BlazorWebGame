using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using BlazorWebGame.Server.Hubs;
using System.Collections.Concurrent;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// ServerProductionService 异步方法扩展 - 优化性能的异步实现
/// </summary>
public partial class ServerProductionService
{
    #region 异步版本的现有方法

    /// <summary>
    /// 获取可用的采集节点（异步版本）
    /// </summary>
    public async Task<List<GatheringNodeDto>> GetAvailableNodesAsync(string profession = "")
    {
        return await Task.Run(() =>
        {
            var allNodes = _gatheringNodes.Values.ToList();
            
            if (string.IsNullOrEmpty(profession))
            {
                return allNodes;
            }
            
            return allNodes.Where(n => n.RequiredProfession == profession).ToList();
        });
    }

    /// <summary>
    /// 根据ID获取采集节点（异步版本）
    /// </summary>
    public async Task<GatheringNodeDto?> GetNodeByIdAsync(string nodeId)
    {
        return await Task.FromResult(_gatheringNodes.TryGetValue(nodeId, out var node) ? node : null);
    }

    /// <summary>
    /// 获取玩家的采集状态（异步版本）
    /// </summary>
    public async Task<GatheringStateDto?> GetGatheringStateAsync(string characterId)
    {
        return await Task.FromResult(_gatheringStates.TryGetValue(characterId, out var state) ? state : null);
    }

    /// <summary>
    /// 获取所有活跃的采集状态（异步版本）
    /// </summary>
    public async Task<List<GatheringStateDto>> GetAllActiveGatheringStatesAsync()
    {
        return await Task.Run(() => _gatheringStates.Values.Where(s => s.IsGathering).ToList());
    }

    /// <summary>
    /// 获取可用的配方（异步版本）
    /// </summary>
    public async Task<List<RecipeDto>> GetAvailableRecipesAsync(string characterId, string? profession = null, int? maxLevel = null)
    {
        return await Task.Run(() =>
        {
            var allRecipes = _recipes.Values.ToList();
            
            // 按专业筛选
            if (!string.IsNullOrEmpty(profession))
            {
                allRecipes = allRecipes.Where(r => r.RequiredProfession == profession).ToList();
            }
            
            // 按等级筛选
            if (maxLevel.HasValue)
            {
                allRecipes = allRecipes.Where(r => r.RequiredLevel <= maxLevel.Value).ToList();
            }
            
            return allRecipes.OrderBy(r => r.RequiredLevel).ToList();
        });
    }

    /// <summary>
    /// 根据ID获取配方（异步版本）
    /// </summary>
    public async Task<RecipeDto?> GetRecipeByIdAsync(string recipeId)
    {
        return await Task.FromResult(_recipes.TryGetValue(recipeId, out var recipe) ? recipe : null);
    }

    /// <summary>
    /// 获取制作状态（异步版本）
    /// </summary>
    public async Task<CraftingStateDto?> GetCraftingStateAsync(string characterId)
    {
        return await Task.FromResult(_craftingStates.TryGetValue(characterId, out var state) ? state : null);
    }

    /// <summary>
    /// 检查节点解锁状态（异步版本）
    /// </summary>
    public async Task<NodeUnlockStatusDto> CheckNodeUnlockStatusAsync(string characterId, string nodeId)
    {
        return await Task.Run(() =>
        {
            if (!_gatheringNodes.TryGetValue(nodeId, out var node))
            {
                return new NodeUnlockStatusDto
                {
                    NodeId = nodeId,
                    IsUnlocked = false,
                    RequirementMet = false,
                    RequiredLevel = 0,
                    RequiredProfession = "",
                    UnlockMessage = "节点不存在"
                };
            }

            // 这里应该检查角色的实际等级和专业，现在是简化版本
            var isUnlocked = true; // 简化为全部解锁
            
            return new NodeUnlockStatusDto
            {
                NodeId = nodeId,
                IsUnlocked = isUnlocked,
                RequirementMet = isUnlocked,
                RequiredLevel = node.RequiredLevel,
                RequiredProfession = node.RequiredProfession,
                UnlockMessage = isUnlocked ? "已解锁" : $"需要 {node.RequiredProfession} 等级 {node.RequiredLevel}"
            };
        });
    }

    /// <summary>
    /// 检查制作材料（异步版本）
    /// </summary>
    public async Task<bool> CheckCraftingMaterialsAsync(string characterId, string recipeId, int quantity = 1)
    {
        return await Task.Run(() =>
        {
            if (!_recipes.TryGetValue(recipeId, out var recipe))
            {
                return false;
            }

            // 这里应该检查角色的实际背包，现在是简化版本
            // 在实际实现中，需要访问 ServerInventoryService 来检查材料
            return true; // 简化为材料充足
        });
    }

    #endregion

    #region 新增的缓存优化异步方法

    /// <summary>
    /// 批量获取多个角色的采集状态（异步版本）
    /// </summary>
    public async Task<Dictionary<string, GatheringStateDto?>> GetMultipleGatheringStatesAsync(IEnumerable<string> characterIds)
    {
        return await Task.Run(() =>
        {
            var result = new Dictionary<string, GatheringStateDto?>();
            
            foreach (var characterId in characterIds)
            {
                result[characterId] = _gatheringStates.TryGetValue(characterId, out var state) ? state : null;
            }
            
            return result;
        });
    }

    /// <summary>
    /// 批量获取多个配方信息（异步版本）
    /// </summary>
    public async Task<Dictionary<string, RecipeDto?>> GetMultipleRecipesAsync(IEnumerable<string> recipeIds)
    {
        return await Task.Run(() =>
        {
            var result = new Dictionary<string, RecipeDto?>();
            
            foreach (var recipeId in recipeIds)
            {
                result[recipeId] = _recipes.TryGetValue(recipeId, out var recipe) ? recipe : null;
            }
            
            return result;
        });
    }

    /// <summary>
    /// 获取专业相关的统计信息（异步版本）
    /// </summary>
    public async Task<ProfessionStatsDto> GetProfessionStatsAsync(string profession)
    {
        return await Task.Run(() =>
        {
            var nodes = _gatheringNodes.Values.Where(n => n.RequiredProfession == profession).ToList();
            var recipes = _recipes.Values.Where(r => r.RequiredProfession == profession).ToList();
            var activeGathering = _gatheringStates.Values.Count(s => s.IsGathering && 
                _gatheringNodes.TryGetValue(s.NodeId, out var node) && node.RequiredProfession == profession);
            var activeCrafting = _craftingStates.Values.Count(s => s.IsCrafting && 
                _recipes.TryGetValue(s.RecipeId, out var recipe) && recipe.RequiredProfession == profession);

            return new ProfessionStatsDto
            {
                Profession = profession,
                TotalNodes = nodes.Count,
                TotalRecipes = recipes.Count,
                ActiveGathering = activeGathering,
                ActiveCrafting = activeCrafting,
                AverageNodeLevel = nodes.Any() ? (int)nodes.Average(n => n.RequiredLevel) : 0,
                AverageRecipeLevel = recipes.Any() ? (int)recipes.Average(r => r.RequiredLevel) : 0
            };
        });
    }

    /// <summary>
    /// 获取系统范围的生产统计信息（异步版本）
    /// </summary>
    public async Task<ProductionSystemStatsDto> GetSystemStatsAsync()
    {
        return await Task.Run(() =>
        {
            var totalNodes = _gatheringNodes.Count;
            var totalRecipes = _recipes.Count;
            var activeGathering = _gatheringStates.Values.Count(s => s.IsGathering);
            var activeCrafting = _craftingStates.Values.Count(s => s.IsCrafting);

            var professionStats = _gatheringNodes.Values
                .GroupBy(n => n.RequiredProfession)
                .ToDictionary(g => g.Key, g => g.Count());

            return new ProductionSystemStatsDto
            {
                TotalNodes = totalNodes,
                TotalRecipes = totalRecipes,
                ActiveGatheringCount = activeGathering,
                ActiveCraftingCount = activeCrafting,
                NodesPerProfession = professionStats,
                Timestamp = DateTime.UtcNow
            };
        });
    }

    /// <summary>
    /// 并行处理多个采集完成事件（性能优化）
    /// </summary>
    public async Task ProcessMultipleGatheringCompletionsAsync(IEnumerable<string> characterIds)
    {
        var tasks = characterIds.Select(async characterId =>
        {
            if (_gatheringStates.TryGetValue(characterId, out var state) && 
                state.IsGathering && state.RemainingTimeSeconds <= 0)
            {
                await CompleteAndRestartGatheringAsync(characterId, state);
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 异步预热缓存 - 预加载常用数据
    /// </summary>
    public async Task WarmupCacheAsync()
    {
        _logger.LogInformation("Starting production service cache warmup");

        var warmupTasks = new List<Task>
        {
            // 预加载所有专业的节点
            Task.Run(() => _gatheringNodes.Values.GroupBy(n => n.RequiredProfession).ToList()),
            
            // 预加载所有专业的配方
            Task.Run(() => _recipes.Values.GroupBy(r => r.RequiredProfession).ToList()),
            
            // 统计当前活跃状态
            Task.Run(() => 
            {
                var activeGathering = _gatheringStates.Values.Count(s => s.IsGathering);
                var activeCrafting = _craftingStates.Values.Count(s => s.IsCrafting);
                _logger.LogDebug("Cache warmup: {ActiveGathering} gathering, {ActiveCrafting} crafting", 
                    activeGathering, activeCrafting);
            })
        };

        await Task.WhenAll(warmupTasks);
        _logger.LogInformation("Production service cache warmup completed");
    }

    #endregion
}

/// <summary>
/// 专业统计信息 DTO
/// </summary>
public class ProfessionStatsDto
{
    public string Profession { get; set; } = string.Empty;
    public int TotalNodes { get; set; }
    public int TotalRecipes { get; set; }
    public int ActiveGathering { get; set; }
    public int ActiveCrafting { get; set; }
    public int AverageNodeLevel { get; set; }
    public int AverageRecipeLevel { get; set; }
}

/// <summary>
/// 生产系统统计信息 DTO
/// </summary>
public class ProductionSystemStatsDto
{
    public int TotalNodes { get; set; }
    public int TotalRecipes { get; set; }
    public int ActiveGatheringCount { get; set; }
    public int ActiveCraftingCount { get; set; }
    public Dictionary<string, int> NodesPerProfession { get; set; } = new();
    public DateTime Timestamp { get; set; }
}