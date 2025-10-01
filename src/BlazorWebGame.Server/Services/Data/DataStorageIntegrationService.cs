using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorWebGame.Server.Services.Data;

/// <summary>
/// 数据存储集成服务 - 连接现有系统与数据存储服务
/// </summary>
public class DataStorageIntegrationService
{
    private readonly IDataStorageService _dataStorageService;
    private readonly ILogger<DataStorageIntegrationService> _logger;

    public DataStorageIntegrationService(
        IDataStorageService dataStorageService,
        ILogger<DataStorageIntegrationService> logger)
    {
        _dataStorageService = dataStorageService;
        _logger = logger;
    }

    #region 与现有Player模型的集成

    /// <summary>
    /// 从现有Player模型同步到数据存储
    /// </summary>
    public async Task<bool> SyncPlayerToStorageAsync(Models.Player player)
    {
        try
        {
            var playerDto = ConvertPlayerToStorageDto(player);
            var result = await _dataStorageService.SavePlayerAsync(playerDto);
            
            if (result.Success)
            {
                _logger.LogDebug("Player synced to storage successfully");
                return true;
            }
            
            _logger.LogWarning("Failed to sync player to storage: {Message}", result.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing player to storage");
            return false;
        }
    }

    /// <summary>
    /// 从数据存储加载Player数据到现有模型
    /// </summary>
    public async Task<Models.Player?> LoadPlayerFromStorageAsync(string playerId)
    {
        try
        {
            var playerDto = await _dataStorageService.GetPlayerAsync(playerId);
            if (playerDto != null)
            {
                return ConvertStorageDtoToPlayer(playerDto);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading player from storage");
            return null;
        }
    }

    #endregion

    #region 与现有Party模型的集成

    /// <summary>
    /// 从现有Party模型同步到数据存储
    /// </summary>
    public async Task<bool> SyncPartyToStorageAsync(Models.Party party)
    {
        try
        {
            var teamDto = ConvertPartyToTeamDto(party);
            var result = await _dataStorageService.SaveTeamAsync(teamDto);
            
            if (result.Success)
            {
                _logger.LogDebug("Party synced to storage successfully");
                return true;
            }
            
            _logger.LogWarning("Failed to sync party to storage: {Message}", result.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing party to storage");
            return false;
        }
    }

    /// <summary>
    /// 从数据存储加载Party数据到现有模型
    /// </summary>
    public async Task<Models.Party?> LoadPartyFromStorageAsync(string teamId)
    {
        try
        {
            var teamDto = await _dataStorageService.GetTeamAsync(teamId);
            if (teamDto != null)
            {
                return ConvertTeamDtoToParty(teamDto);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading party from storage");
            return null;
        }
    }

    #endregion

    #region 战斗记录集成

    /// <summary>
    /// 记录战斗开始
    /// </summary>
    public async Task<string?> RecordBattleStartAsync(
        string battleId,
        List<string> participantIds,
        string battleType = "Normal",
        Guid? partyId = null,
        string? dungeonId = null)
    {
        try
        {
            var battleRecord = new BattleRecordStorageDto
            {
                Id = Guid.NewGuid().ToString(),
                BattleId = battleId,
                BattleType = battleType,
                Status = "InProgress",
                Participants = participantIds,
                PartyId = partyId,
                DungeonId = dungeonId,
                StartedAt = DateTime.UtcNow,
                Enemies = new List<object>(),
                Actions = new List<object>(),
                Results = new Dictionary<string, object>()
            };

            var result = await _dataStorageService.SaveBattleRecordAsync(battleRecord);
            if (result.Success)
            {
                _logger.LogInformation("Battle record created for battle ID: {BattleId}", battleId);
                return battleRecord.Id;
            }

            _logger.LogWarning("Failed to create battle record: {Message}", result.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating battle record");
            return null;
        }
    }

    /// <summary>
    /// 记录战斗结束
    /// </summary>
    public async Task<bool> RecordBattleEndAsync(
        string battleId,
        string status,
        Dictionary<string, object>? results = null)
    {
        try
        {
            var battleResults = results ?? new Dictionary<string, object>();
            var result = await _dataStorageService.EndBattleRecordAsync(battleId, status, battleResults);
            
            if (result.Success)
            {
                _logger.LogInformation("Battle record ended for battle ID: {BattleId} with status: {Status}", battleId, status);
                return true;
            }

            _logger.LogWarning("Failed to end battle record: {Message}", result.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending battle record");
            return false;
        }
    }

    #endregion

    #region 动作目标集成

    /// <summary>
    /// 设置玩家当前动作目标
    /// </summary>
    public async Task<bool> SetPlayerActionTargetAsync(
        string playerId,
        string targetType,
        string targetId,
        string targetName,
        string actionType,
        double duration = 0.0)
    {
        try
        {
            var actionTarget = new ActionTargetStorageDto
            {
                Id = Guid.NewGuid().ToString(),
                PlayerId = playerId,
                TargetType = targetType,
                TargetId = targetId,
                TargetName = targetName,
                ActionType = actionType,
                Duration = duration,
                Progress = 0.0,
                IsCompleted = false,
                StartedAt = DateTime.UtcNow,
                ProgressData = new Dictionary<string, object>()
            };

            var result = await _dataStorageService.SaveActionTargetAsync(actionTarget);
            if (result.Success)
            {
                _logger.LogDebug("Action target set for player: {ActionType} -> {TargetName}", actionType, targetName);
                return true;
            }

            _logger.LogWarning("Failed to set action target: {Message}", result.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting player action target");
            return false;
        }
    }

    /// <summary>
    /// 清除玩家当前动作目标
    /// </summary>
    public async Task<bool> ClearPlayerActionTargetAsync(string playerId)
    {
        try
        {
            var result = await _dataStorageService.CancelActionTargetAsync(playerId);
            if (result.Success)
            {
                _logger.LogDebug("Action target cleared for player");
                return true;
            }

            _logger.LogDebug("No action target to clear or failed: {Message}", result.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing player action target");
            return false;
        }
    }

    #endregion

    #region 数据转换方法

    private PlayerStorageDto ConvertPlayerToStorageDto(Models.Player player)
    {
        return new PlayerStorageDto
        {
            Id = player.Id,
            Name = player.Name,
            Level = player.GetLevel(player.SelectedBattleProfession),
            Experience = player.BattleProfessionXP.GetValueOrDefault(player.SelectedBattleProfession, 0),
            Health = player.Health,
            MaxHealth = player.MaxHealth,
            Gold = player.Gold,
            SelectedBattleProfession = player.SelectedBattleProfession.ToString(),
            CurrentAction = player.CurrentAction.ToString(),
            IsOnline = true, // 假设在线
            LastActiveAt = DateTime.UtcNow,
            // 复杂属性转换为简化版本，实际应用中可以进行更详细的转换
            Attributes = new Dictionary<string, object>
            {
                ["Strength"] = player.BaseAttributes?.Strength ?? 0,
                ["Agility"] = player.BaseAttributes?.Agility ?? 0,
                ["Intellect"] = player.BaseAttributes?.Intellect ?? 0,
                ["Spirit"] = player.BaseAttributes?.Spirit ?? 0,
                ["Stamina"] = player.BaseAttributes?.Stamina ?? 0
            },
            Inventory = new List<object>(), // 简化版本
            Skills = player.LearnedSharedSkills?.ToList() ?? new List<string>(),
            Equipment = player.EquippedItems?.ToDictionary(
                kv => kv.Key.ToString(), 
                kv => kv.Value) ?? new Dictionary<string, string>()
        };
    }

    private Models.Player ConvertStorageDtoToPlayer(PlayerStorageDto dto)
    {
        var player = new Models.Player
        {
            Id = dto.Id,
            Name = dto.Name,
            Health = dto.Health,
            MaxHealth = dto.MaxHealth,
            Gold = dto.Gold
        };

        // 设置选中的战斗职业
        if (Enum.TryParse<Models.BattleProfession>(dto.SelectedBattleProfession, out var profession))
        {
            player.SelectedBattleProfession = profession;
        }

        // 设置当前动作状态
        if (Enum.TryParse<Models.PlayerActionState>(dto.CurrentAction, out var actionState))
        {
            player.CurrentAction = actionState;
        }

        return player;
    }

    private TeamStorageDto ConvertPartyToTeamDto(Models.Party party)
    {
        return new TeamStorageDto
        {
            Id = party.Id.ToString(),
            Name = $"Party-{party.CaptainId}",
            CaptainId = party.CaptainId,
            MemberIds = party.MemberIds.ToList(),
            MaxMembers = Models.Party.MaxMembers,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Models.Party ConvertTeamDtoToParty(TeamStorageDto dto)
    {
        var party = new Models.Party
        {
            Id = Guid.Parse(dto.Id),
            CaptainId = dto.CaptainId,
            MemberIds = dto.MemberIds.ToList()
        };

        return party;
    }

    #endregion

    #region 批量同步方法

    /// <summary>
    /// 批量同步多个玩家数据到存储
    /// </summary>
    public async Task<int> BatchSyncPlayersToStorageAsync(IEnumerable<Models.Player> players)
    {
        var playerDtos = players.Select(ConvertPlayerToStorageDto).ToList();
        var result = await _dataStorageService.SavePlayersAsync(playerDtos);
        
        _logger.LogInformation("Batch sync completed: {SuccessCount}/{TotalCount} players synced", 
            result.SuccessCount, result.TotalProcessed);
        
        return result.SuccessCount;
    }

    /// <summary>
    /// 获取数据存储统计信息
    /// </summary>
    public async Task<Dictionary<string, object>?> GetStorageStatsAsync()
    {
        try
        {
            var result = await _dataStorageService.GetStorageStatsAsync();
            return result.Success ? result.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage stats");
            return null;
        }
    }

    #endregion
}