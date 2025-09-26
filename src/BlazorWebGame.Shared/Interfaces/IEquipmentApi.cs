using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 装备系统API接口定义
/// </summary>
public interface IEquipmentApi
{
    /// <summary>
    /// 生成装备
    /// </summary>
    Task<ApiResponse<EquipmentDto>> GenerateEquipmentAsync(EquipmentGenerationRequest request);

    /// <summary>
    /// 计算装备价值
    /// </summary>
    Task<ApiResponse<int>> CalculateEquipmentValueAsync(EquipmentDto equipment);

    /// <summary>
    /// 根据名称猜测武器类型
    /// </summary>
    Task<ApiResponse<string>> GuessWeaponTypeAsync(string name);

    /// <summary>
    /// 根据名称猜测护甲类型
    /// </summary>
    Task<ApiResponse<string>> GuessArmorTypeAsync(string name);

    /// <summary>
    /// 批量生成装备
    /// </summary>
    Task<ApiResponse<List<EquipmentDto>>> GenerateBatchEquipmentAsync(List<EquipmentGenerationRequest> requests);
}