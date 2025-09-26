using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 角色系统API服务实现
/// </summary>
public class CharacterApiService : BaseApiService
{
    public CharacterApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger<CharacterApiService> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<ApiResponse<List<CharacterDto>>> GetCharactersAsync()
    {
        return await GetAsync<List<CharacterDto>>("api/character");
    }

    public async Task<ApiResponse<CharacterDetailsDto>> GetCharacterDetailsAsync(string characterId)
    {
        return await GetAsync<CharacterDetailsDto>($"api/character/{characterId}");
    }

    public async Task<ApiResponse<CharacterDto>> CreateCharacterAsync(CreateCharacterRequest request)
    {
        return await PostAsync<CharacterDto>("api/character", request);
    }

    public async Task<ApiResponse<bool>> AddExperienceAsync(string characterId, AddExperienceRequest request)
    {
        return await PostAsync<bool>($"api/character/{characterId}/experience", request);
    }

    public async Task<ApiResponse<bool>> UpdateCharacterStatusAsync(string characterId, UpdateCharacterStatusRequest request)
    {
        return await PutAsync<bool>($"api/character/{characterId}/status", request);
    }

    public async Task<ApiResponse<AttributeSetDto>> GetCharacterAttributesAsync(string characterId)
    {
        return await GetAsync<AttributeSetDto>($"api/player/{characterId}/attributes");
    }

    public async Task<ApiResponse<int>> GetCharacterAttackPowerAsync(string characterId)
    {
        return await GetAsync<int>($"api/player/{characterId}/attack-power");
    }

    public async Task<ApiResponse<int>> GetCharacterMaxHealthAsync(string characterId)
    {
        return await GetAsync<int>($"api/player/{characterId}/max-health");
    }

    public async Task<ApiResponse<int>> GetProfessionLevelAsync(string characterId, string professionType, string profession)
    {
        return await GetAsync<int>($"api/player/{characterId}/profession/{professionType}/{profession}/level");
    }

    public async Task<ApiResponse<double>> GetProfessionProgressAsync(string characterId, string professionType, string profession)
    {
        return await GetAsync<double>($"api/player/{characterId}/profession/{professionType}/{profession}/progress");
    }
}