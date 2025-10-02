using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Battles;

namespace BlazorIdleGame.Client.Services.Battle
{
    /// <summary>
    /// ս������ӿ� - ����ǰ����ս��ϵͳ����
    /// </summary>
    public interface IBattleService
    {
        // ��ǰս��״̬
        BattleDto? CurrentBattle { get; }
        BattleStatusDto? CurrentBattleStatus { get; }
        
        // �¼�
        event EventHandler<BattleDto> BattleCreated;
        event EventHandler<BattleDto> BattleStarted;
        event EventHandler<BattleStatusDto> BattleStatusUpdated;
        event EventHandler<BattleActionResultDto> ActionPerformed;
        event EventHandler<string> BattleError;
        event EventHandler<BattleStatusDto> BattleEnded;
        
        // ս������
        Task<BattleDto?> CreateBattleAsync(string characterId, string enemyId, string? battleType = null, string? regionId = null);
        Task<bool> StartBattleAsync(string battleId);
        Task<BattleActionResultDto?> UseSkillAsync(string battleId, string casterId, string skillId, string? targetId = null);
        Task<BattleStatusDto?> GetBattleStatusAsync(string battleId);
        
        // ս��״̬ˢ��
        Task<BattleStatusDto?> RefreshBattleStatusAsync();
        
        // ���õ�ǰս��״̬
        void ResetCurrentBattle();
    }
}