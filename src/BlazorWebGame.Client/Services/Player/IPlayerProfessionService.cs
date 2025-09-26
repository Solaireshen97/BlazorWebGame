using BlazorWebGame.Models;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Services.PlayerServices;

/// <summary>
/// 玩家专业技能管理服务接口
/// </summary>
public interface IPlayerProfessionService
{
    /// <summary>
    /// 获取采集专业技能奖励
    /// </summary>
    double GetTotalGatheringSpeedBonus(Models.Player player);
    
    /// <summary>
    /// 获取额外战利品几率
    /// </summary>
    double GetTotalExtraLootChance(Models.Player player);
    
    /// <summary>
    /// 获取总制作速度加成，小数形式（例如 0.1 表示 +10%）
    /// </summary>
    double GetTotalCraftingSpeedBonus(Models.Player player);
    
    /// <summary>
    /// 为指定战斗职业添加经验值，返回升级信息
    /// </summary>
    (bool LeveledUp, int OldLevel, int NewLevel) AddBattleXP(Models.Player player, BattleProfession profession, int amount);
    
    /// <summary>
    /// 为指定采集职业添加经验值
    /// </summary>
    void AddGatheringXP(Models.Player player, GatheringProfession profession, int amount);
    
    /// <summary>
    /// 为指定生产职业添加经验值
    /// </summary>
    (bool LeveledUp, int OldLevel, int NewLevel) AddProductionXP(Models.Player player, ProductionProfession profession, int amount);
    
    /// <summary>
    /// 获取等级
    /// </summary>
    int GetLevel(Models.Player player, BattleProfession profession);
    
    /// <summary>
    /// 获取等级
    /// </summary>
    int GetLevel(Models.Player player, GatheringProfession profession);
    
    /// <summary>
    /// 获取等级
    /// </summary>
    int GetLevel(Models.Player player, ProductionProfession profession);
    
    /// <summary>
    /// 获取等级进度百分比
    /// </summary>
    double GetLevelProgress(Models.Player player, BattleProfession profession);
    
    /// <summary>
    /// 获取等级进度百分比
    /// </summary>
    double GetLevelProgress(Models.Player player, GatheringProfession profession);
    
    /// <summary>
    /// 获取等级进度百分比
    /// </summary>
    double GetLevelProgress(Models.Player player, ProductionProfession profession);
}