using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 库存系统API接口定义
/// </summary>
public interface IInventoryApi
{
    /// <summary>
    /// 获取角色库存
    /// </summary>
    Task<ApiResponse<InventoryDto>> GetInventoryAsync(string characterId);

    /// <summary>
    /// 添加物品到库存
    /// </summary>
    Task<ApiResponse<bool>> AddItemAsync(AddItemRequest request);

    /// <summary>
    /// 使用物品
    /// </summary>
    Task<ApiResponse<bool>> UseItemAsync(UseItemRequest request);

    /// <summary>
    /// 装备物品
    /// </summary>
    Task<ApiResponse<bool>> EquipItemAsync(EquipItemRequest request);

    /// <summary>
    /// 出售物品
    /// </summary>
    Task<ApiResponse<int>> SellItemAsync(SellItemRequest request);
}