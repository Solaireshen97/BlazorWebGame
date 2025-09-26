using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 生产系统API接口定义
/// </summary>
public interface IProductionApi
{
    /// <summary>
    /// 获取所有采集节点
    /// </summary>
    Task<ApiResponse<List<GatheringNodeDto>>> GetGatheringNodesAsync();

    /// <summary>
    /// 获取指定采集节点信息
    /// </summary>
    Task<ApiResponse<GatheringNodeDto>> GetGatheringNodeAsync(string nodeId);

    /// <summary>
    /// 开始采集
    /// </summary>
    Task<ApiResponse<bool>> StartGatheringAsync(StartGatheringRequest request);

    /// <summary>
    /// 停止采集
    /// </summary>
    Task<ApiResponse<GatheringResultDto>> StopGatheringAsync(StopGatheringRequest request);

    /// <summary>
    /// 获取角色采集状态
    /// </summary>
    Task<ApiResponse<GatheringStateDto>> GetGatheringStateAsync(string characterId);

    // ==================== 制作系统 API ====================

    /// <summary>
    /// 获取可用配方列表
    /// </summary>
    Task<ApiResponse<List<RecipeDto>>> GetRecipesAsync(GetRecipesRequest request);

    /// <summary>
    /// 获取指定配方信息
    /// </summary>
    Task<ApiResponse<RecipeDto>> GetRecipeAsync(string recipeId);

    /// <summary>
    /// 开始制作
    /// </summary>
    Task<ApiResponse<bool>> StartCraftingAsync(StartCraftingRequest request);

    /// <summary>
    /// 批量制作
    /// </summary>
    Task<ApiResponse<bool>> StartBatchCraftingAsync(BatchCraftingRequest request);

    /// <summary>
    /// 停止制作
    /// </summary>
    Task<ApiResponse<CraftingResultDto>> StopCraftingAsync(StopCraftingRequest request);

    /// <summary>
    /// 获取角色制作状态
    /// </summary>
    Task<ApiResponse<CraftingStateDto>> GetCraftingStateAsync(string characterId);

    /// <summary>
    /// 检查节点解锁状态
    /// </summary>
    Task<ApiResponse<NodeUnlockStatusDto>> CheckNodeUnlockStatusAsync(NodeUnlockCheckRequest request);

    /// <summary>
    /// 验证制作材料是否充足
    /// </summary>
    Task<ApiResponse<bool>> CheckCraftingMaterialsAsync(string characterId, string recipeId, int quantity = 1);
}