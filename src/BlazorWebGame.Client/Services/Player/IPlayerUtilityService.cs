using BlazorWebGame.Models;

namespace BlazorWebGame.Services.PlayerServices;

/// <summary>
/// 玩家实用工具服务接口
/// </summary>
public interface IPlayerUtilityService
{
    /// <summary>
    /// 检查是否拥有物品的个数
    /// </summary>
    bool HasItemInInventory(Models.Player player, string itemId);
    
    /// <summary>
    /// 获取声望等级
    /// </summary>
    ReputationTier GetReputationLevel(Models.Player player, Faction faction);
    
    /// <summary>
    /// 获取声望进度百分比
    /// </summary>
    double GetReputationProgressPercentage(Models.Player player, Faction faction);
    
    /// <summary>
    /// 确保玩家数据一致性，在存储后加载数据后调用此方法来确保数据一致性
    /// </summary>
    void EnsureDataConsistency(Models.Player player);
    
    /// <summary>
    /// 初始化玩家集合数据，确保所有集合和字典都已初始化，防止从旧存档加载时出现null问题
    /// </summary>
    void InitializeCollections(Models.Player player);
}