using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// ���ݴ洢����ӿ� - ֧�����߻غ�����Ϸ�����ݹ���
/// </summary>
public interface IDataStorageService
{
    #region �û��˺Ź���

    /// <summary>
    /// �����û�����ȡ�û�����
    /// </summary>
    Task<UserStorageDto?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// ����ID��ȡ�û�����
    /// </summary>
    Task<UserStorageDto?> GetUserByIdAsync(string userId);

    /// <summary>
    /// ���������ȡ�û�����
    /// </summary>
    Task<UserStorageDto?> GetUserByEmailAsync(string email);

    /// <summary>
    /// �������û��˺�
    /// </summary>
    Task<ApiResponse<UserStorageDto>> CreateUserAsync(UserStorageDto user, string password);

    /// <summary>
    /// �����û�����
    /// </summary>
    Task<ApiResponse<UserStorageDto>> UpdateUserAsync(UserStorageDto user);

    /// <summary>
    /// ��֤�û�����
    /// </summary>
    Task<bool> ValidateUserPasswordAsync(string userId, string password);

    /// <summary>
    /// �����û�����
    /// </summary>
    Task<ApiResponse<bool>> UpdateUserPasswordAsync(string userId, string newPassword);

    /// <summary>
    /// �����û�����¼��Ϣ
    /// </summary>
    Task<ApiResponse<bool>> UpdateUserLastLoginAsync(string userId, string ipAddress);

    /// <summary>
    /// �����û��˺�
    /// </summary>
    Task<ApiResponse<bool>> LockUserAccountAsync(string userId, DateTime lockUntil);

    /// <summary>
    /// �����û��˺�
    /// </summary>
    Task<ApiResponse<bool>> UnlockUserAccountAsync(string userId);

    #endregion

    #region �û���ɫ��������

    /// <summary>
    /// �����û���ɫ����
    /// </summary>
    Task<ApiResponse<UserCharacterStorageDto>> CreateUserCharacterAsync(string userId, string characterId, string characterName, bool isDefault = false, int slotIndex = 0);

    /// <summary>
    /// ��ȡ�û������н�ɫ
    /// </summary>
    Task<ApiResponse<List<UserCharacterStorageDto>>> GetUserCharactersAsync(string userId);

    /// <summary>
    /// ��ȡ��ɫ��ӵ����
    /// </summary>
    Task<UserCharacterStorageDto?> GetCharacterOwnerAsync(string characterId);

    /// <summary>
    /// ��֤�û��Ƿ�ӵ��ָ����ɫ
    /// </summary>
    Task<bool> UserOwnsCharacterAsync(string userId, string characterId);

    /// <summary>
    /// �����û���Ĭ�Ͻ�ɫ
    /// </summary>
    Task<ApiResponse<bool>> SetDefaultCharacterAsync(string userId, string characterId);

    /// <summary>
    /// ɾ���û���ɫ����
    /// </summary>
    Task<ApiResponse<bool>> DeleteUserCharacterAsync(string userId, string characterId);

    /// <summary>
    /// ������ɫ��λ
    /// </summary>
    Task<ApiResponse<bool>> UnlockCharacterSlotAsync(string userId, int slotIndex);

    #endregion

    #region ��ɫ���ݹ���

    /// <summary>
    /// ���ݽ�ɫID��ȡ��ɫ����
    /// </summary>
    Task<ApiResponse<CharacterStorageDto>> GetCharacterByIdAsync(string characterId);

    /// <summary>
    /// �����ɫ����
    /// </summary>
    Task<ApiResponse<CharacterStorageDto>> SaveCharacterAsync(CharacterStorageDto character);

    /// <summary>
    /// ��ȡһ��ʱ���ڻ�Ծ�Ľ�ɫ�б�
    /// </summary>
    Task<ApiResponse<List<CharacterStorageDto>>> GetRecentActiveCharactersAsync(TimeSpan activeWithin);

    #endregion

    #region ��ǿ��ս��ϵͳ����

    /// <summary>
    /// ����ս����¼
    /// </summary>
    Task<ApiResponse<EnhancedBattleEntity>> CreateBattleAsync(EnhancedBattleEntity battle);

    /// <summary>
    /// ��ȡս����¼
    /// </summary>
    Task<ApiResponse<EnhancedBattleEntity>> GetBattleByIdAsync(string battleId);

    /// <summary>
    /// ����ս����¼
    /// </summary>
    Task<ApiResponse<EnhancedBattleEntity>> UpdateBattleAsync(EnhancedBattleEntity battle);

    /// <summary>
    /// ����ս��������
    /// </summary>
    Task<ApiResponse<EnhancedBattleParticipantEntity>> SaveBattleParticipantAsync(EnhancedBattleParticipantEntity participant);

    /// <summary>
    /// ����ս��������
    /// </summary>
    Task<ApiResponse<EnhancedBattleParticipantEntity>> UpdateBattleParticipantAsync(EnhancedBattleParticipantEntity participant);

    /// <summary>
    /// ��ȡս���������б�
    /// </summary>
    Task<ApiResponse<List<EnhancedBattleParticipantEntity>>> GetBattleParticipantsAsync(string battleId);

    /// <summary>
    /// ����ս���¼�
    /// </summary>
    Task<ApiResponse<EnhancedBattleEventEntity>> SaveBattleEventAsync(EnhancedBattleEventEntity battleEvent);

    /// <summary>
    /// ��ȡս���¼��б�
    /// </summary>
    Task<ApiResponse<List<EnhancedBattleEventEntity>>> GetBattleEventsAsync(string battleId);

    /// <summary>
    /// ����ս�����
    /// </summary>
    Task<ApiResponse<EnhancedBattleResultEntity>> SaveBattleResultAsync(EnhancedBattleResultEntity battleResult);

    /// <summary>
    /// ��ȡս�����
    /// </summary>
    Task<ApiResponse<EnhancedBattleResultEntity>> GetBattleResultAsync(string battleId);

    /// <summary>
    /// ��ȡ�����е�ս���б�
    /// </summary>
    Task<ApiResponse<List<EnhancedBattleEntity>>> GetActiveBattlesAsync();

    /// <summary>
    /// ��ȡ��ɫ����Ľ�����ս��
    /// </summary>
    Task<ApiResponse<EnhancedBattleEntity>> GetCharacterActiveBattleAsync(string characterId);

    /// <summary>
    /// ��ȡ�������Ľ�����ս��
    /// </summary>
    Task<ApiResponse<EnhancedBattleEntity>> GetTeamActiveBattleAsync(string teamId);

    /// <summary>
    /// �������ս����¼���¼�
    /// </summary>
    Task<ApiResponse<int>> CleanupBattleDataAsync(TimeSpan olderThan);

    /// <summary>
    /// ��ȡս��ϵͳ����
    /// </summary>
    Task<ApiResponse<EnhancedBattleSystemConfigEntity>> GetBattleSystemConfigAsync(string configType, string? battleTypeReference = null);

    /// <summary>
    /// ����ս��ϵͳ����
    /// </summary>
    Task<ApiResponse<EnhancedBattleSystemConfigEntity>> SaveBattleSystemConfigAsync(EnhancedBattleSystemConfigEntity config);

    #endregion

    #region ������ݹ���

    /// <summary>
    /// ��ȡ�������
    /// </summary>
    Task<PlayerStorageDto?> GetPlayerAsync(string playerId);

    /// <summary>
    /// ����������������
    /// </summary>
    Task<ApiResponse<PlayerStorageDto>> SavePlayerAsync(PlayerStorageDto player);

    /// <summary>
    /// ɾ���������
    /// </summary>
    Task<ApiResponse<bool>> DeletePlayerAsync(string playerId);

    /// <summary>
    /// ��ȡ�����������
    /// </summary>
    Task<ApiResponse<List<PlayerStorageDto>>> GetOnlinePlayersAsync();

    /// <summary>
    /// ���������������
    /// </summary>
    Task<BatchOperationResponseDto<PlayerStorageDto>> SavePlayersAsync(List<PlayerStorageDto> players);

    #endregion

    #region �������ݹ���

    /// <summary>
    /// ��ȡ��������
    /// </summary>
    Task<TeamStorageDto?> GetTeamAsync(string teamId);

    /// <summary>
    /// ���ݶӳ�ID��ȡ����
    /// </summary>
    Task<TeamStorageDto?> GetTeamByCaptainAsync(string captainId);

    /// <summary>
    /// �������ID��ȡ�����ڶ���
    /// </summary>
    Task<TeamStorageDto?> GetTeamByPlayerAsync(string playerId);

    /// <summary>
    /// ��������¶�������
    /// </summary>
    Task<ApiResponse<TeamStorageDto>> SaveTeamAsync(TeamStorageDto team);

    /// <summary>
    /// ɾ����������
    /// </summary>
    Task<ApiResponse<bool>> DeleteTeamAsync(string teamId);

    /// <summary>
    /// ��ȡ��Ծ�����б�
    /// </summary>
    Task<ApiResponse<List<TeamStorageDto>>> GetActiveTeamsAsync();

    #endregion

    #region �ж�Ŀ�����

    /// <summary>
    /// ��ȡ��ҵ�ǰ�ж�Ŀ��
    /// </summary>
    Task<ActionTargetStorageDto?> GetCurrentActionTargetAsync(string playerId);

    /// <summary>
    /// �����ж�Ŀ������
    /// </summary>
    Task<ApiResponse<ActionTargetStorageDto>> SaveActionTargetAsync(ActionTargetStorageDto actionTarget);

    /// <summary>
    /// ����ж�Ŀ��
    /// </summary>
    Task<ApiResponse<bool>> CompleteActionTargetAsync(string actionTargetId);

    /// <summary>
    /// ȡ���ж�Ŀ��
    /// </summary>
    Task<ApiResponse<bool>> CancelActionTargetAsync(string playerId);

    /// <summary>
    /// ��ȡ�����ʷ�ж�Ŀ��
    /// </summary>
    Task<ApiResponse<List<ActionTargetStorageDto>>> GetPlayerActionHistoryAsync(string playerId, int limit = 50);

    #endregion

    #region ս����¼����

    /// <summary>
    /// ��ȡս����¼
    /// </summary>
    Task<BattleRecordStorageDto?> GetBattleRecordAsync(string battleId);

    /// <summary>
    /// ����ս����¼
    /// </summary>
    Task<ApiResponse<BattleRecordStorageDto>> SaveBattleRecordAsync(BattleRecordStorageDto battleRecord);

    /// <summary>
    /// ����ս����¼
    /// </summary>
    Task<ApiResponse<bool>> EndBattleRecordAsync(string battleId, string status, Dictionary<string, object> results);

    /// <summary>
    /// ��ȡ���ս����ʷ
    /// </summary>
    Task<ApiResponse<List<BattleRecordStorageDto>>> GetPlayerBattleHistoryAsync(string playerId, DataStorageQueryDto query);

    /// <summary>
    /// ��ȡ����ս����ʷ
    /// </summary>
    Task<ApiResponse<List<BattleRecordStorageDto>>> GetTeamBattleHistoryAsync(string teamId, DataStorageQueryDto query);

    /// <summary>
    /// ��ȡ�����е�ս����¼
    /// </summary>
    Task<ApiResponse<List<BattleRecordStorageDto>>> GetActiveBattleRecordsAsync();

    #endregion

    #region �������ݹ���

    /// <summary>
    /// ������������
    /// </summary>
    Task<ApiResponse<OfflineDataStorageDto>> SaveOfflineDataAsync(OfflineDataStorageDto offlineData);

    /// <summary>
    /// ��ȡδͬ������������
    /// </summary>
    Task<ApiResponse<List<OfflineDataStorageDto>>> GetUnsyncedOfflineDataAsync(string playerId);

    /// <summary>
    /// �����������Ϊ��ͬ��
    /// </summary>
    Task<ApiResponse<bool>> MarkOfflineDataSyncedAsync(List<string> offlineDataIds);

    /// <summary>
    /// ������ͬ���ľ���������
    /// </summary>
    Task<ApiResponse<int>> CleanupSyncedOfflineDataAsync(DateTime olderThan);

    #endregion

    #region ���ݲ�ѯ��ͳ��

    /// <summary>
    /// �������
    /// </summary>
    Task<ApiResponse<List<PlayerStorageDto>>> SearchPlayersAsync(string searchTerm, int limit = 20);

    /// <summary>
    /// ��ȡ���ݴ洢ͳ����Ϣ
    /// </summary>
    Task<ApiResponse<Dictionary<string, object>>> GetStorageStatsAsync();

    /// <summary>
    /// ���ݽ������
    /// </summary>
    Task<ApiResponse<Dictionary<string, object>>> HealthCheckAsync();

    #endregion

    #region ����ͬ���ͱ���

    /// <summary>
    /// �����������
    /// </summary>
    Task<ApiResponse<Dictionary<string, object>>> ExportPlayerDataAsync(string playerId);

    /// <summary>
    /// �����������
    /// </summary>
    Task<ApiResponse<bool>> ImportPlayerDataAsync(string playerId, Dictionary<string, object> data);

    /// <summary>
    /// ���ݱ���
    /// </summary>
    Task<ApiResponse<string>> BackupDataAsync();

    /// <summary>
    /// �������� - ɾ����������
    /// </summary>
    Task<ApiResponse<int>> CleanupExpiredDataAsync(TimeSpan olderThan);

    #endregion
}