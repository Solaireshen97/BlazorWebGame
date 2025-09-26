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
}