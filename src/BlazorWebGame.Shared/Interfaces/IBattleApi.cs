using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 战斗系统API接口定义
/// </summary>
public interface IBattleApi
{
    /// <summary>
    /// 开始新战斗
    /// </summary>
    Task<ApiResponse<BattleStateDto>> StartBattleAsync(StartBattleRequest request);

    /// <summary>
    /// 获取战斗状态
    /// </summary>
    Task<ApiResponse<BattleStateDto>> GetBattleStateAsync(Guid battleId);

    /// <summary>
    /// 执行战斗动作
    /// </summary>
    Task<ApiResponse<bool>> ExecuteBattleActionAsync(BattleActionRequest request);

    /// <summary>
    /// 停止战斗
    /// </summary>
    Task<ApiResponse<bool>> StopBattleAsync(Guid battleId);

    /// <summary>
    /// 获取用户的活跃战斗列表
    /// </summary>
    Task<ApiResponse<List<BattleStateDto>>> GetActiveBattlesAsync();
}