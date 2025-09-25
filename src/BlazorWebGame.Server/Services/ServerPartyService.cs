using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using BlazorWebGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 服务端组队服务
/// </summary>
public class ServerPartyService
{
    private readonly Dictionary<Guid, ServerParty> _parties = new();
    private readonly Dictionary<string, Guid> _characterPartyMap = new(); // characterId -> partyId
    private readonly ILogger<ServerPartyService> _logger;
    private readonly IHubContext<GameHub> _hubContext;

    public ServerPartyService(ILogger<ServerPartyService> logger, IHubContext<GameHub>? hubContext = null)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// 创建新组队
    /// </summary>
    public PartyDto? CreateParty(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
        {
            return null;
        }

        // 检查角色是否已在其他队伍中
        if (_characterPartyMap.ContainsKey(characterId))
        {
            _logger.LogWarning("Character {CharacterId} is already in a party", characterId);
            return null;
        }

        var party = new ServerParty
        {
            CaptainId = characterId,
            MemberIds = new List<string> { characterId }
        };

        _parties[party.Id] = party;
        _characterPartyMap[characterId] = party.Id;

        _logger.LogInformation("Party created: {PartyId} by character {CharacterId}", party.Id, characterId);

        var dto = ConvertToDto(party);
        _ = BroadcastPartyUpdate(dto, "PartyCreated");
        
        return dto;
    }

    /// <summary>
    /// 加入组队
    /// </summary>
    public bool JoinParty(string characterId, Guid partyId)
    {
        if (string.IsNullOrEmpty(characterId))
        {
            return false;
        }

        // 检查角色是否已在其他队伍中
        if (_characterPartyMap.ContainsKey(characterId))
        {
            _logger.LogWarning("Character {CharacterId} is already in a party", characterId);
            return false;
        }

        // 查找目标队伍
        if (!_parties.TryGetValue(partyId, out var party))
        {
            _logger.LogWarning("Party {PartyId} not found", partyId);
            return false;
        }

        // 尝试添加成员
        if (!party.AddMember(characterId))
        {
            _logger.LogWarning("Failed to add character {CharacterId} to party {PartyId} (party full or already member)", 
                characterId, partyId);
            return false;
        }

        _characterPartyMap[characterId] = partyId;

        _logger.LogInformation("Character {CharacterId} joined party {PartyId}", characterId, partyId);

        var dto = ConvertToDto(party);
        _ = BroadcastPartyUpdate(dto, "PartyMemberJoined");

        return true;
    }

    /// <summary>
    /// 离开组队
    /// </summary>
    public bool LeaveParty(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
        {
            return false;
        }

        // 查找角色所在的队伍
        if (!_characterPartyMap.TryGetValue(characterId, out var partyId))
        {
            _logger.LogWarning("Character {CharacterId} is not in any party", characterId);
            return false;
        }

        if (!_parties.TryGetValue(partyId, out var party))
        {
            // 清理无效映射
            _characterPartyMap.Remove(characterId);
            return false;
        }

        // 移除成员
        bool wasLeader = party.IsCaptain(characterId);
        party.RemoveMember(characterId);
        _characterPartyMap.Remove(characterId);

        if (wasLeader || party.GetMemberCount() == 0)
        {
            // 队长离开或队伍为空，解散队伍
            DisbandParty(partyId);
            _logger.LogInformation("Party {PartyId} disbanded", partyId);
            _ = BroadcastPartyUpdate(ConvertToDto(party), "PartyDisbanded");
        }
        else
        {
            _logger.LogInformation("Character {CharacterId} left party {PartyId}", characterId, partyId);
            var dto = ConvertToDto(party);
            _ = BroadcastPartyUpdate(dto, "PartyMemberLeft");
        }

        return true;
    }

    /// <summary>
    /// 获取角色所在的组队
    /// </summary>
    public PartyDto? GetPartyForCharacter(string characterId)
    {
        if (!_characterPartyMap.TryGetValue(characterId, out var partyId))
        {
            return null;
        }

        if (!_parties.TryGetValue(partyId, out var party))
        {
            // 清理无效映射
            _characterPartyMap.Remove(characterId);
            return null;
        }

        return ConvertToDto(party);
    }

    /// <summary>
    /// 获取所有组队
    /// </summary>
    public List<PartyDto> GetAllParties()
    {
        return _parties.Values.Select(ConvertToDto).ToList();
    }

    /// <summary>
    /// 获取指定组队
    /// </summary>
    public PartyDto? GetParty(Guid partyId)
    {
        return _parties.TryGetValue(partyId, out var party) ? ConvertToDto(party) : null;
    }

    /// <summary>
    /// 解散组队
    /// </summary>
    private void DisbandParty(Guid partyId)
    {
        if (_parties.TryGetValue(partyId, out var party))
        {
            // 清理所有成员映射
            foreach (var memberId in party.MemberIds)
            {
                _characterPartyMap.Remove(memberId);
            }

            _parties.Remove(partyId);
        }
    }

    /// <summary>
    /// 将服务端模型转换为DTO
    /// </summary>
    private PartyDto ConvertToDto(ServerParty party)
    {
        return new PartyDto
        {
            Id = party.Id,
            CaptainId = party.CaptainId,
            MemberIds = new List<string>(party.MemberIds),
            CreatedAt = party.CreatedAt,
            LastUpdated = party.LastUpdated,
            MaxMembers = party.MaxMembers
        };
    }

    /// <summary>
    /// 广播组队更新
    /// </summary>
    private async Task BroadcastPartyUpdate(PartyDto party, string eventType)
    {
        if (_hubContext == null)
        {
            _logger.LogDebug("Hub context not available, skipping party update broadcast");
            return;
        }

        try
        {
            // 向组队成员广播更新
            foreach (var memberId in party.MemberIds)
            {
                var groupName = $"character-{memberId}";
                await _hubContext.Clients.Group(groupName).SendAsync("PartyUpdate", new { 
                    Type = eventType, 
                    Party = party 
                });
            }

            _logger.LogDebug("Party update broadcasted: {EventType} for party {PartyId}", eventType, party.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting party update for party {PartyId}", party.Id);
        }
    }

    /// <summary>
    /// 获取组队的活跃成员（用于战斗）
    /// </summary>
    public List<string> GetActivePartyMembers(Guid partyId)
    {
        if (!_parties.TryGetValue(partyId, out var party))
        {
            return new List<string>();
        }

        return new List<string>(party.MemberIds);
    }

    /// <summary>
    /// 检查角色是否可以发起组队战斗
    /// </summary>
    public bool CanStartPartyBattle(string characterId)
    {
        if (!_characterPartyMap.TryGetValue(characterId, out var partyId))
        {
            return false; // 不在任何队伍中
        }

        if (!_parties.TryGetValue(partyId, out var party))
        {
            return false;
        }

        // 只有队长可以发起组队战斗
        return party.IsCaptain(characterId);
    }
}