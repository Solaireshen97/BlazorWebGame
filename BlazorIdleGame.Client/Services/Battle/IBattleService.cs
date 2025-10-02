using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Battles;

namespace BlazorIdleGame.Client.Services.Battle
{
    /// <summary>
    /// 战斗服务接口 - 用于前端与战斗系统交互
    /// </summary>
    public interface IBattleService
    {
        // 当前战斗状态
        BattleDto? CurrentBattle { get; }
        BattleStatusDto? CurrentBattleStatus { get; }
        
        // 事件
        event EventHandler<BattleDto> BattleCreated;
        event EventHandler<BattleDto> BattleStarted;
        event EventHandler<BattleStatusDto> BattleStatusUpdated;
        event EventHandler<BattleActionResultDto> ActionPerformed;
        event EventHandler<string> BattleError;
        event EventHandler<BattleStatusDto> BattleEnded;
        
        // 战斗操作
        Task<BattleDto?> CreateBattleAsync(string characterId, string enemyId, string? battleType = null, string? regionId = null);
        Task<bool> StartBattleAsync(string battleId);
        Task<BattleActionResultDto?> UseSkillAsync(string battleId, string casterId, string skillId, string? targetId = null);
        Task<BattleStatusDto?> GetBattleStatusAsync(string battleId);
        
        // 战斗状态刷新
        Task<BattleStatusDto?> RefreshBattleStatusAsync();
        
        // 重置当前战斗状态
        void ResetCurrentBattle();
    }
}