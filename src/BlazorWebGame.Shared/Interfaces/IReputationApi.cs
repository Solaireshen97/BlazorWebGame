using BlazorWebGame.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 声望系统API接口
/// </summary>
public interface IReputationApi
{
    /// <summary>
    /// 获取角色的声望信息
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <returns>声望信息响应</returns>
    Task<ApiResponse<ReputationDto>> GetReputationAsync(string characterId);

    /// <summary>
    /// 获取指定阵营的详细声望信息
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="factionName">阵营名称</param>
    /// <returns>详细声望信息响应</returns>
    Task<ApiResponse<ReputationDetailDto>> GetReputationDetailAsync(string characterId, string factionName);

    /// <summary>
    /// 获取所有阵营的详细声望信息
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <returns>所有阵营的详细声望信息列表</returns>
    Task<ApiResponse<List<ReputationDetailDto>>> GetAllReputationDetailsAsync(string characterId);

    /// <summary>
    /// 更新角色声望
    /// </summary>
    /// <param name="request">更新声望请求</param>
    /// <returns>更新后的声望信息</returns>
    Task<ApiResponse<ReputationDto>> UpdateReputationAsync(UpdateReputationRequest request);

    /// <summary>
    /// 批量更新角色声望
    /// </summary>
    /// <param name="request">批量更新声望请求</param>
    /// <returns>更新后的声望信息</returns>
    Task<ApiResponse<ReputationDto>> BatchUpdateReputationAsync(BatchUpdateReputationRequest request);

    /// <summary>
    /// 获取声望奖励信息
    /// </summary>
    /// <param name="request">声望奖励查询请求</param>
    /// <returns>声望奖励信息列表</returns>
    Task<ApiResponse<List<ReputationRewardDto>>> GetReputationRewardsAsync(ReputationRewardsRequest request);

    /// <summary>
    /// 获取角色可获得的声望奖励
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <returns>可获得的奖励列表</returns>
    Task<ApiResponse<List<ReputationRewardDto>>> GetAvailableRewardsAsync(string characterId);

    /// <summary>
    /// 获取声望历史记录
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="factionName">阵营名称（可选）</param>
    /// <param name="days">查询天数（默认30天）</param>
    /// <returns>声望历史记录列表</returns>
    Task<ApiResponse<List<ReputationHistoryDto>>> GetReputationHistoryAsync(string characterId, string? factionName = null, int days = 30);

    /// <summary>
    /// 获取声望统计信息
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <returns>声望统计信息</returns>
    Task<ApiResponse<ReputationStatsDto>> GetReputationStatsAsync(string characterId);

    /// <summary>
    /// 重置角色声望（管理员功能）
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="factionName">阵营名称（可选，不指定则重置所有）</param>
    /// <returns>重置结果</returns>
    Task<ApiResponse<ReputationDto>> ResetReputationAsync(string characterId, string? factionName = null);
}