using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 组队系统API服务实现
/// </summary>
public class PartyApiService : BaseApiService
{
    public PartyApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger<PartyApiService> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<ApiResponse<PartyDto>> CreatePartyAsync(string characterId)
    {
        var request = new CreatePartyRequest { CharacterId = characterId };
        return await PostAsync<PartyDto>("api/party/create", request);
    }

    public async Task<ApiResponse<bool>> JoinPartyAsync(string characterId, Guid partyId)
    {
        var request = new JoinPartyRequest { CharacterId = characterId, PartyId = partyId };
        return await PostAsync<bool>("api/party/join", request);
    }

    public async Task<ApiResponse<bool>> LeavePartyAsync(string characterId)
    {
        var request = new LeavePartyRequest { CharacterId = characterId };
        return await PostAsync<bool>("api/party/leave", request);
    }

    public async Task<ApiResponse<PartyDto>> GetPartyForCharacterAsync(string characterId)
    {
        return await GetAsync<PartyDto>($"api/party/character/{characterId}");
    }

    public async Task<ApiResponse<List<PartyDto>>> GetAllPartiesAsync()
    {
        return await GetAsync<List<PartyDto>>("api/party/all");
    }

    public async Task<ApiResponse<PartyDto>> GetPartyAsync(Guid partyId)
    {
        return await GetAsync<PartyDto>($"api/party/{partyId}");
    }

    /// <summary>
    /// 更新队伍进度（离线同步专用）
    /// </summary>
    public async Task<ApiResponse<object>> UpdateTeamProgressAsync(string partyId, TeamProgressUpdateRequest request)
    {
        return await PutAsync<object>($"api/party/{partyId}/progress", request);
    }
}