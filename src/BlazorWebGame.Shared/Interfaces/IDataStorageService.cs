using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 数据存储服务接口 - 支持离线回合制游戏的数据管理
/// </summary>
public interface IDataStorageService
{
    #region 用户账号管理

    /// <summary>
    /// 根据用户名获取用户数据
    /// </summary>
    Task<UserStorageDto?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// 根据ID获取用户数据
    /// </summary>
    Task<UserStorageDto?> GetUserByIdAsync(string userId);

    /// <summary>
    /// 根据邮箱获取用户数据
    /// </summary>
    Task<UserStorageDto?> GetUserByEmailAsync(string email);

    /// <summary>
    /// 创建新用户账号
    /// </summary>
    Task<ApiResponse<UserStorageDto>> CreateUserAsync(UserStorageDto user, string password);

    /// <summary>
    /// 更新用户数据
    /// </summary>
    Task<ApiResponse<UserStorageDto>> UpdateUserAsync(UserStorageDto user);

    /// <summary>
    /// 验证用户密码
    /// </summary>
    Task<bool> ValidateUserPasswordAsync(string userId, string password);

    /// <summary>
    /// 更新用户密码
    /// </summary>
    Task<ApiResponse<bool>> UpdateUserPasswordAsync(string userId, string newPassword);

    /// <summary>
    /// 更新用户最后登录信息
    /// </summary>
    Task<ApiResponse<bool>> UpdateUserLastLoginAsync(string userId, string ipAddress);

    /// <summary>
    /// 锁定用户账号
    /// </summary>
    Task<ApiResponse<bool>> LockUserAccountAsync(string userId, DateTime lockUntil);

    /// <summary>
    /// 解锁用户账号
    /// </summary>
    Task<ApiResponse<bool>> UnlockUserAccountAsync(string userId);

    #endregion

    #region 用户角色关联管理

    /// <summary>
    /// 创建用户角色关联
    /// </summary>
    Task<ApiResponse<UserCharacterStorageDto>> CreateUserCharacterAsync(string userId, string characterId, string characterName, bool isDefault = false, int slotIndex = 0);

    /// <summary>
    /// 获取用户的所有角色
    /// </summary>
    Task<ApiResponse<List<UserCharacterStorageDto>>> GetUserCharactersAsync(string userId);

    /// <summary>
    /// 获取角色的拥有者
    /// </summary>
    Task<UserCharacterStorageDto?> GetCharacterOwnerAsync(string characterId);

    /// <summary>
    /// 验证用户是否拥有指定角色
    /// </summary>
    Task<bool> UserOwnsCharacterAsync(string userId, string characterId);

    /// <summary>
    /// 设置用户的默认角色
    /// </summary>
    Task<ApiResponse<bool>> SetDefaultCharacterAsync(string userId, string characterId);

    /// <summary>
    /// 删除用户角色关联
    /// </summary>
    Task<ApiResponse<bool>> DeleteUserCharacterAsync(string userId, string characterId);

    /// <summary>
    /// 解锁角色槽位
    /// </summary>
    Task<ApiResponse<bool>> UnlockCharacterSlotAsync(string userId, int slotIndex);

    #endregion

    #region 角色数据管理

    /// <summary>
    /// 根据角色ID获取角色数据
    /// </summary>
    Task<ApiResponse<CharacterStorageDto>> GetCharacterByIdAsync(string characterId);

    /// <summary>
    /// 保存角色数据
    /// </summary>
    Task<ApiResponse<CharacterStorageDto>> SaveCharacterAsync(CharacterStorageDto character);

    /// <summary>
    /// 获取一段时间内活跃的角色列表
    /// </summary>
    Task<ApiResponse<List<CharacterStorageDto>>> GetRecentActiveCharactersAsync(TimeSpan activeWithin);

    #endregion

    #region 增强版战斗系统管理

    /// <summary>
    /// 创建战斗记录
    /// </summary>
    Task<ApiResponse<EnhancedBattleEntity>> CreateBattleAsync(EnhancedBattleEntity battle);

    /// <summary>
    /// 获取战斗记录
    /// </summary>
    Task<ApiResponse<EnhancedBattleEntity>> GetBattleByIdAsync(string battleId);

    /// <summary>
    /// 更新战斗记录
    /// </summary>
    Task<ApiResponse<EnhancedBattleEntity>> UpdateBattleAsync(EnhancedBattleEntity battle);

    /// <summary>
    /// 保存战斗参与者
    /// </summary>
    Task<ApiResponse<EnhancedBattleParticipantEntity>> SaveBattleParticipantAsync(EnhancedBattleParticipantEntity participant);

    /// <summary>
    /// 更新战斗参与者
    /// </summary>
    Task<ApiResponse<EnhancedBattleParticipantEntity>> UpdateBattleParticipantAsync(EnhancedBattleParticipantEntity participant);

    /// <summary>
    /// 获取战斗参与者列表
    /// </summary>
    Task<ApiResponse<List<EnhancedBattleParticipantEntity>>> GetBattleParticipantsAsync(string battleId);

    /// <summary>
    /// 保存战斗事件
    /// </summary>
    Task<ApiResponse<EnhancedBattleEventEntity>> SaveBattleEventAsync(EnhancedBattleEventEntity battleEvent);

    /// <summary>
    /// 获取战斗事件列表
    /// </summary>
    Task<ApiResponse<List<EnhancedBattleEventEntity>>> GetBattleEventsAsync(string battleId);

    /// <summary>
    /// 保存战斗结果
    /// </summary>
    Task<ApiResponse<EnhancedBattleResultEntity>> SaveBattleResultAsync(EnhancedBattleResultEntity battleResult);

    /// <summary>
    /// 获取战斗结果
    /// </summary>
    Task<ApiResponse<EnhancedBattleResultEntity>> GetBattleResultAsync(string battleId);

    /// <summary>
    /// 获取进行中的战斗列表
    /// </summary>
    Task<ApiResponse<List<EnhancedBattleEntity>>> GetActiveBattlesAsync();

    /// <summary>
    /// 获取角色参与的进行中战斗
    /// </summary>
    Task<ApiResponse<EnhancedBattleEntity>> GetCharacterActiveBattleAsync(string characterId);

    /// <summary>
    /// 获取队伍参与的进行中战斗
    /// </summary>
    Task<ApiResponse<EnhancedBattleEntity>> GetTeamActiveBattleAsync(string teamId);

    /// <summary>
    /// 清理过期战斗记录和事件
    /// </summary>
    Task<ApiResponse<int>> CleanupBattleDataAsync(TimeSpan olderThan);

    /// <summary>
    /// 获取战斗系统配置
    /// </summary>
    Task<ApiResponse<EnhancedBattleSystemConfigEntity>> GetBattleSystemConfigAsync(string configType, string? battleTypeReference = null);

    /// <summary>
    /// 保存战斗系统配置
    /// </summary>
    Task<ApiResponse<EnhancedBattleSystemConfigEntity>> SaveBattleSystemConfigAsync(EnhancedBattleSystemConfigEntity config);

    #endregion

    #region 玩家数据管理

    /// <summary>
    /// 获取玩家数据
    /// </summary>
    Task<PlayerStorageDto?> GetPlayerAsync(string playerId);

    /// <summary>
    /// 创建或更新玩家数据
    /// </summary>
    Task<ApiResponse<PlayerStorageDto>> SavePlayerAsync(PlayerStorageDto player);

    /// <summary>
    /// 删除玩家数据
    /// </summary>
    Task<ApiResponse<bool>> DeletePlayerAsync(string playerId);

    /// <summary>
    /// 获取所有在线玩家
    /// </summary>
    Task<ApiResponse<List<PlayerStorageDto>>> GetOnlinePlayersAsync();

    /// <summary>
    /// 批量保存玩家数据
    /// </summary>
    Task<BatchOperationResponseDto<PlayerStorageDto>> SavePlayersAsync(List<PlayerStorageDto> players);

    #endregion

    #region 队伍数据管理

    /// <summary>
    /// 获取队伍数据
    /// </summary>
    Task<TeamStorageDto?> GetTeamAsync(string teamId);

    /// <summary>
    /// 根据队长ID获取队伍
    /// </summary>
    Task<TeamStorageDto?> GetTeamByCaptainAsync(string captainId);

    /// <summary>
    /// 根据玩家ID获取其所在队伍
    /// </summary>
    Task<TeamStorageDto?> GetTeamByPlayerAsync(string playerId);

    /// <summary>
    /// 创建或更新队伍数据
    /// </summary>
    Task<ApiResponse<TeamStorageDto>> SaveTeamAsync(TeamStorageDto team);

    /// <summary>
    /// 删除队伍数据
    /// </summary>
    Task<ApiResponse<bool>> DeleteTeamAsync(string teamId);

    /// <summary>
    /// 获取活跃队伍列表
    /// </summary>
    Task<ApiResponse<List<TeamStorageDto>>> GetActiveTeamsAsync();

    #endregion

    #region 行动目标管理

    /// <summary>
    /// 获取玩家当前行动目标
    /// </summary>
    Task<ActionTargetStorageDto?> GetCurrentActionTargetAsync(string playerId);

    /// <summary>
    /// 保存行动目标数据
    /// </summary>
    Task<ApiResponse<ActionTargetStorageDto>> SaveActionTargetAsync(ActionTargetStorageDto actionTarget);

    /// <summary>
    /// 完成行动目标
    /// </summary>
    Task<ApiResponse<bool>> CompleteActionTargetAsync(string actionTargetId);

    /// <summary>
    /// 取消行动目标
    /// </summary>
    Task<ApiResponse<bool>> CancelActionTargetAsync(string playerId);

    /// <summary>
    /// 获取玩家历史行动目标
    /// </summary>
    Task<ApiResponse<List<ActionTargetStorageDto>>> GetPlayerActionHistoryAsync(string playerId, int limit = 50);

    #endregion

    #region 战斗记录管理

    /// <summary>
    /// 获取战斗记录
    /// </summary>
    Task<BattleRecordStorageDto?> GetBattleRecordAsync(string battleId);

    /// <summary>
    /// 保存战斗记录
    /// </summary>
    Task<ApiResponse<BattleRecordStorageDto>> SaveBattleRecordAsync(BattleRecordStorageDto battleRecord);

    /// <summary>
    /// 结束战斗记录
    /// </summary>
    Task<ApiResponse<bool>> EndBattleRecordAsync(string battleId, string status, Dictionary<string, object> results);

    /// <summary>
    /// 获取玩家战斗历史
    /// </summary>
    Task<ApiResponse<List<BattleRecordStorageDto>>> GetPlayerBattleHistoryAsync(string playerId, DataStorageQueryDto query);

    /// <summary>
    /// 获取队伍战斗历史
    /// </summary>
    Task<ApiResponse<List<BattleRecordStorageDto>>> GetTeamBattleHistoryAsync(string teamId, DataStorageQueryDto query);

    /// <summary>
    /// 获取进行中的战斗记录
    /// </summary>
    Task<ApiResponse<List<BattleRecordStorageDto>>> GetActiveBattleRecordsAsync();

    #endregion

    #region 离线数据管理

    /// <summary>
    /// 保存离线数据
    /// </summary>
    Task<ApiResponse<OfflineDataStorageDto>> SaveOfflineDataAsync(OfflineDataStorageDto offlineData);

    /// <summary>
    /// 获取未同步的离线数据
    /// </summary>
    Task<ApiResponse<List<OfflineDataStorageDto>>> GetUnsyncedOfflineDataAsync(string playerId);

    /// <summary>
    /// 标记离线数据为已同步
    /// </summary>
    Task<ApiResponse<bool>> MarkOfflineDataSyncedAsync(List<string> offlineDataIds);

    /// <summary>
    /// 清理已同步的旧离线数据
    /// </summary>
    Task<ApiResponse<int>> CleanupSyncedOfflineDataAsync(DateTime olderThan);

    #endregion

    #region 数据查询和统计

    /// <summary>
    /// 搜索玩家
    /// </summary>
    Task<ApiResponse<List<PlayerStorageDto>>> SearchPlayersAsync(string searchTerm, int limit = 20);

    /// <summary>
    /// 获取数据存储统计信息
    /// </summary>
    Task<ApiResponse<Dictionary<string, object>>> GetStorageStatsAsync();

    /// <summary>
    /// 数据健康检查
    /// </summary>
    Task<ApiResponse<Dictionary<string, object>>> HealthCheckAsync();

    #endregion

    #region 数据同步和备份

    /// <summary>
    /// 导出玩家数据
    /// </summary>
    Task<ApiResponse<Dictionary<string, object>>> ExportPlayerDataAsync(string playerId);

    /// <summary>
    /// 导入玩家数据
    /// </summary>
    Task<ApiResponse<bool>> ImportPlayerDataAsync(string playerId, Dictionary<string, object> data);

    /// <summary>
    /// 数据备份
    /// </summary>
    Task<ApiResponse<string>> BackupDataAsync();

    /// <summary>
    /// 数据清理 - 删除过期数据
    /// </summary>
    Task<ApiResponse<int>> CleanupExpiredDataAsync(TimeSpan olderThan);

    #endregion
}