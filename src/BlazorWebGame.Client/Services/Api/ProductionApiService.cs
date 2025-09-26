using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 生产系统API服务实现
/// </summary>
public class ProductionApiService : BaseApiService
{
    public ProductionApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger<ProductionApiService> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<ApiResponse<List<GatheringNodeDto>>> GetGatheringNodesAsync()
    {
        return await GetAsync<List<GatheringNodeDto>>("api/production/nodes");
    }

    public async Task<ApiResponse<GatheringNodeDto>> GetGatheringNodeAsync(string nodeId)
    {
        return await GetAsync<GatheringNodeDto>($"api/production/nodes/{nodeId}");
    }

    public async Task<ApiResponse<bool>> StartGatheringAsync(StartGatheringRequest request)
    {
        return await PostAsync<bool>("api/production/gathering/start", request);
    }

    public async Task<ApiResponse<GatheringResultDto>> StopGatheringAsync(StopGatheringRequest request)
    {
        return await PostAsync<GatheringResultDto>("api/production/gathering/stop", request);
    }

    public async Task<ApiResponse<GatheringStateDto>> GetGatheringStateAsync(string characterId)
    {
        return await GetAsync<GatheringStateDto>($"api/production/gathering/state/{characterId}");
    }

    // 保留原有的方法以保持向后兼容
    /// <summary>
    /// 获取所有可用的采集节点 (向后兼容方法)
    /// </summary>
    public async Task<List<GatheringNodeDto>> GetAvailableNodesAsync(string profession = "")
    {
        try
        {
            var response = await GetGatheringNodesAsync();
            if (response.Success && response.Data != null)
            {
                if (string.IsNullOrEmpty(profession))
                {
                    return response.Data;
                }
                return response.Data.Where(n => n.RequiredProfession.Equals(profession, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            return new List<GatheringNodeDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available nodes");
            return new List<GatheringNodeDto>();
        }
    }

    /// <summary>
    /// 根据ID获取特定采集节点 (向后兼容方法)
    /// </summary>
    public async Task<GatheringNodeDto?> GetNodeByIdAsync(string nodeId)
    {
        try
        {
            var response = await GetGatheringNodeAsync(nodeId);
            return response.Success ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting node {NodeId}", nodeId);
            return null;
        }
    }

    /// <summary>
    /// 开始采集 (向后兼容方法)
    /// </summary>
    public async Task<(bool Success, string Message, GatheringStateDto? State)> StartGatheringAsync(string characterId, string nodeId)
    {
        try
        {
            var request = new StartGatheringRequest
            {
                CharacterId = characterId,
                NodeId = nodeId
            };

            var response = await StartGatheringAsync(request);
            
            // 如果成功，获取最新状态
            GatheringStateDto? state = null;
            if (response.Success)
            {
                var stateResponse = await GetGatheringStateAsync(characterId);
                state = stateResponse.Success ? stateResponse.Data : null;
            }

            return (response.Success, response.Message, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting gathering for character {CharacterId} at node {NodeId}", characterId, nodeId);
            return (false, "开始采集时发生错误", null);
        }
    }

    /// <summary>
    /// 停止采集 (向后兼容方法)
    /// </summary>
    public async Task<(bool Success, string Message)> StopGatheringAsync(string characterId)
    {
        try
        {
            var request = new StopGatheringRequest
            {
                CharacterId = characterId
            };

            var response = await StopGatheringAsync(request);
            return (response.Success, response.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping gathering for character {CharacterId}", characterId);
            return (false, "停止采集时发生错误");
        }
    }

    /// <summary>
    /// 获取玩家当前的采集状态 (向后兼容方法)
    /// </summary>
    public async Task<GatheringStateDto?> GetGatheringStateAsync_Legacy(string characterId)
    {
        try
        {
            var response = await GetGatheringStateAsync(characterId);
            return response.Success ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gathering state for character {CharacterId}", characterId);
            return null;
        }
    }

    /// <summary>
    /// 获取所有活跃的采集状态（管理用）
    /// </summary>
    public async Task<List<GatheringStateDto>> GetActiveGatheringStatesAsync()
    {
        try
        {
            var response = await GetAsync<List<GatheringStateDto>>("api/production/gathering/active");
            return response.Success && response.Data != null ? response.Data : new List<GatheringStateDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active gathering states");
            return new List<GatheringStateDto>();
        }
    }

    // ============================================================================
    // 制作系统API方法 (新增)
    // ============================================================================

    /// <summary>
    /// 获取所有可用的制作配方
    /// </summary>
    public async Task<ApiResponse<List<RecipeDto>>> GetRecipesAsync(string profession = "")
    {
        var url = string.IsNullOrEmpty(profession) ? "api/production/recipes" : $"api/production/recipes?profession={profession}";
        return await GetAsync<List<RecipeDto>>(url);
    }

    /// <summary>
    /// 开始制作
    /// </summary>
    public async Task<ApiResponse<CraftingStateDto>> StartCraftingAsync(StartCraftingRequest request)
    {
        return await PostAsync<CraftingStateDto>("api/production/crafting/start", request);
    }

    /// <summary>
    /// 停止制作
    /// </summary>
    public async Task<ApiResponse<string>> StopCraftingAsync(StopCraftingRequest request)
    {
        return await PostAsync<string>("api/production/crafting/stop", request);
    }

    /// <summary>
    /// 获取角色的制作状态
    /// </summary>
    public async Task<ApiResponse<CraftingStateDto>> GetCraftingStateAsync(string characterId)
    {
        return await GetAsync<CraftingStateDto>($"api/production/crafting/state/{characterId}");
    }

    /// <summary>
    /// 停止所有生产活动
    /// </summary>
    public async Task<ApiResponse<string>> StopAllProductionAsync(string characterId)
    {
        var request = new StopAllProductionRequest { CharacterId = characterId };
        return await PostAsync<string>("api/production/stop-all", request);
    }

    // ============================================================================
    // 向后兼容的制作方法 (新增)
    // ============================================================================

    /// <summary>
    /// 获取所有可用的制作配方 (向后兼容方法)
    /// </summary>
    public async Task<List<RecipeDto>> GetAvailableRecipesAsync(string profession = "")
    {
        try
        {
            var response = await GetRecipesAsync(profession);
            return response.Success && response.Data != null ? response.Data : new List<RecipeDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available recipes");
            return new List<RecipeDto>();
        }
    }

    /// <summary>
    /// 开始制作 (向后兼容方法)
    /// </summary>
    public async Task<(bool success, string message)> StartCraftingAsync_Legacy(string characterId, string recipeId)
    {
        try
        {
            var request = new StartCraftingRequest { CharacterId = characterId, RecipeId = recipeId };
            var response = await StartCraftingAsync(request);
            return (response.Success, response.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting crafting for character {CharacterId}", characterId);
            return (false, "开始制作时发生错误");
        }
    }

    /// <summary>
    /// 停止制作 (向后兼容方法)
    /// </summary>
    public async Task<(bool success, string message)> StopCraftingAsync_Legacy(string characterId)
    {
        try
        {
            var request = new StopCraftingRequest { CharacterId = characterId };
            var response = await StopCraftingAsync(request);
            return (response.Success, response.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping crafting for character {CharacterId}", characterId);
            return (false, "停止制作时发生错误");
        }
    }

    /// <summary>
    /// 获取角色的制作状态 (向后兼容方法)
    /// </summary>
    public async Task<CraftingStateDto?> GetCraftingStateAsync_Legacy(string characterId)
    {
        try
        {
            var response = await GetCraftingStateAsync(characterId);
            return response.Success ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting crafting state for character {CharacterId}", characterId);
            return null;
        }
    }
}