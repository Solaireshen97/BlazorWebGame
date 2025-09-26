using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 角色系统API接口定义
/// </summary>
public interface ICharacterApi
{
    /// <summary>
    /// 获取所有角色
    /// </summary>
    Task<ApiResponse<List<CharacterDto>>> GetCharactersAsync();

    /// <summary>
    /// 获取角色详细信息
    /// </summary>
    Task<ApiResponse<CharacterDetailsDto>> GetCharacterDetailsAsync(string characterId);

    /// <summary>
    /// 创建新角色
    /// </summary>
    Task<ApiResponse<CharacterDto>> CreateCharacterAsync(CreateCharacterRequest request);

    /// <summary>
    /// 添加经验值
    /// </summary>
    Task<ApiResponse<bool>> AddExperienceAsync(string characterId, AddExperienceRequest request);

    /// <summary>
    /// 更新角色状态
    /// </summary>
    Task<ApiResponse<bool>> UpdateCharacterStatusAsync(string characterId, UpdateCharacterStatusRequest request);

    /// <summary>
    /// 获取角色属性
    /// </summary>
    Task<ApiResponse<AttributeSetDto>> GetCharacterAttributesAsync(string characterId);

    /// <summary>
    /// 获取角色攻击力
    /// </summary>
    Task<ApiResponse<int>> GetCharacterAttackPowerAsync(string characterId);

    /// <summary>
    /// 获取角色最大生命值
    /// </summary>
    Task<ApiResponse<int>> GetCharacterMaxHealthAsync(string characterId);

    /// <summary>
    /// 获取专业等级
    /// </summary>
    Task<ApiResponse<int>> GetProfessionLevelAsync(string characterId, string professionType, string profession);

    /// <summary>
    /// 获取专业进度
    /// </summary>
    Task<ApiResponse<double>> GetProfessionProgressAsync(string characterId, string professionType, string profession);
}