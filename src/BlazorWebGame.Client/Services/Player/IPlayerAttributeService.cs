using BlazorWebGame.Models;

namespace BlazorWebGame.Services.PlayerServices;

/// <summary>
/// 玩家属性管理服务接口
/// </summary>
public interface IPlayerAttributeService
{
    /// <summary>
    /// 获取角色的总属性值（基础+装备+buff）
    /// </summary>
    AttributeSet GetTotalAttributes(Models.Player player);
    
    /// <summary>
    /// 获取当前职业的主属性值
    /// </summary>
    int GetPrimaryAttributeValue(Models.Player player);
    
    /// <summary>
    /// 更新角色基础属性，应在角色升级或职业时调用
    /// </summary>
    void UpdateBaseAttributes(Models.Player player);
    
    /// <summary>
    /// 初始化角色属性
    /// </summary>
    void InitializePlayerAttributes(Models.Player player);
    
    /// <summary>
    /// 获取总攻击力
    /// </summary>
    int GetTotalAttackPower(Models.Player player);
    
    /// <summary>
    /// 获取伤害倍数
    /// </summary>
    double GetDamageMultiplier(Models.Player player);
    
    /// <summary>
    /// 获取总生命值上限
    /// </summary>
    int GetTotalMaxHealth(Models.Player player);
    
    /// <summary>
    /// 获取总精确度
    /// </summary>
    int GetTotalAccuracy(Models.Player player);
}