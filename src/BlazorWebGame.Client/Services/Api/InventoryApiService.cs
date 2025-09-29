using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 库存系统API服务实现
/// </summary>
public class InventoryApiService : BaseApiService
{
    public InventoryApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger<InventoryApiService> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<ApiResponse<InventoryDto>> GetInventoryAsync(string characterId)
    {
        return await GetAsync<InventoryDto>($"api/inventory/{characterId}");
    }

    public async Task<ApiResponse<bool>> AddItemAsync(AddItemRequest request)
    {
        return await PostAsync<bool>("api/inventory/add", request);
    }

    public async Task<ApiResponse<bool>> UseItemAsync(UseItemRequest request)
    {
        return await PostAsync<bool>("api/inventory/use", request);
    }

    public async Task<ApiResponse<bool>> EquipItemAsync(EquipItemRequest request)
    {
        return await PostAsync<bool>("api/inventory/equip", request);
    }

    public async Task<ApiResponse<int>> SellItemAsync(SellItemRequest request)
    {
        return await PostAsync<int>("api/inventory/sell", request);
    }
}