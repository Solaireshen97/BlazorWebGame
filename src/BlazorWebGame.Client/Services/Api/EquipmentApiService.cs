using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 装备系统API服务实现
/// </summary>
public class EquipmentApiService : BaseApiService, IEquipmentApi
{
    public EquipmentApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger<EquipmentApiService> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<ApiResponse<EquipmentDto>> GenerateEquipmentAsync(EquipmentGenerationRequest request)
    {
        return await PostAsync<EquipmentDto>("api/equipment/generate", request);
    }

    public async Task<ApiResponse<int>> CalculateEquipmentValueAsync(EquipmentDto equipment)
    {
        return await PostAsync<int>("api/equipment/calculate-value", equipment);
    }

    public async Task<ApiResponse<string>> GuessWeaponTypeAsync(string name)
    {
        return await GetAsync<string>($"api/equipment/guess-weapon-type/{Uri.EscapeDataString(name)}");
    }

    public async Task<ApiResponse<string>> GuessArmorTypeAsync(string name)
    {
        return await GetAsync<string>($"api/equipment/guess-armor-type/{Uri.EscapeDataString(name)}");
    }

    public async Task<ApiResponse<List<EquipmentDto>>> GenerateBatchEquipmentAsync(List<EquipmentGenerationRequest> requests)
    {
        return await PostAsync<List<EquipmentDto>>("api/equipment/generate-batch", requests);
    }
}