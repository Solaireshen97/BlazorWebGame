using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 离线结算系统API接口定义
/// </summary>
public interface IOfflineSettlementApi
{
    /// <summary>
    /// 处理玩家离线结算
    /// </summary>
    Task<ApiResponse<OfflineSettlementResultDto>> ProcessPlayerOfflineSettlementAsync(string playerId, OfflineSettlementRequestDto request);

    /// <summary>
    /// 处理团队离线结算
    /// </summary>
    Task<ApiResponse<OfflineSettlementResultDto>> ProcessTeamOfflineSettlementAsync(string teamId, OfflineSettlementRequestDto request);

    /// <summary>
    /// 批量处理离线结算
    /// </summary>
    Task<ApiResponse<List<OfflineSettlementResultDto>>> ProcessBatchOfflineSettlementAsync(BatchOfflineSettlementRequestDto request);

    /// <summary>
    /// 获取玩家离线信息
    /// </summary>
    Task<ApiResponse<PlayerOfflineInfoDto>> GetPlayerOfflineInfoAsync(string playerId);

    /// <summary>
    /// 获取离线结算统计信息
    /// </summary>
    Task<ApiResponse<OfflineSettlementStatisticsDto>> GetOfflineSettlementStatisticsAsync();
}