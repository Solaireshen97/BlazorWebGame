using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 组队系统API接口定义
/// </summary>
public interface IPartyApi
{
    /// <summary>
    /// 创建新组队
    /// </summary>
    Task<ApiResponse<PartyDto>> CreatePartyAsync(string characterId);

    /// <summary>
    /// 加入组队
    /// </summary>
    Task<ApiResponse<bool>> JoinPartyAsync(string characterId, Guid partyId);

    /// <summary>
    /// 离开组队
    /// </summary>
    Task<ApiResponse<bool>> LeavePartyAsync(string characterId);

    /// <summary>
    /// 获取角色的组队信息
    /// </summary>
    Task<ApiResponse<PartyDto>> GetPartyForCharacterAsync(string characterId);

    /// <summary>
    /// 获取所有组队列表
    /// </summary>
    Task<ApiResponse<List<PartyDto>>> GetAllPartiesAsync();

    /// <summary>
    /// 根据ID获取组队信息
    /// </summary>
    Task<ApiResponse<PartyDto>> GetPartyAsync(Guid partyId);
}