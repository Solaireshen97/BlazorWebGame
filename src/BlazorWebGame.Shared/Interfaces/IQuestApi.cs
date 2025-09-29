using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 任务系统API接口定义
/// </summary>
public interface IQuestApi
{
    /// <summary>
    /// 获取角色任务状态
    /// </summary>
    Task<ApiResponse<CharacterQuestStatusDto>> GetQuestStatusAsync(string characterId);

    /// <summary>
    /// 获取每日任务
    /// </summary>
    Task<ApiResponse<List<QuestDto>>> GetDailyQuestsAsync(string characterId);

    /// <summary>
    /// 获取每周任务
    /// </summary>
    Task<ApiResponse<List<QuestDto>>> GetWeeklyQuestsAsync(string characterId);

    /// <summary>
    /// 接受任务
    /// </summary>
    Task<ApiResponse<bool>> AcceptQuestAsync(AcceptQuestRequest request);

    /// <summary>
    /// 更新任务进度
    /// </summary>
    Task<ApiResponse<bool>> UpdateQuestProgressAsync(UpdateQuestProgressRequest request);

    /// <summary>
    /// 完成任务
    /// </summary>
    Task<ApiResponse<List<QuestRewardDto>>> CompleteQuestAsync(CompleteQuestRequest request);
}