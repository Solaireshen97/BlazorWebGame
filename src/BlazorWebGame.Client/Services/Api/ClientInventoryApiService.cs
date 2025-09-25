using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 客户端物品系统API服务
/// </summary>
public class ClientInventoryApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClientInventoryApiService> _logger;

    public ClientInventoryApiService(HttpClient httpClient, ILogger<ClientInventoryApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取角色库存
    /// </summary>
    public async Task<ApiResponse<InventoryDto>> GetInventoryAsync(string characterId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ApiResponse<InventoryDto>>($"api/inventory/{characterId}")
                ?? new ApiResponse<InventoryDto> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory for character {CharacterId}", characterId);
            return new ApiResponse<InventoryDto>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 添加物品到库存
    /// </summary>
    public async Task<ApiResponse<bool>> AddItemAsync(string characterId, string itemId, int quantity)
    {
        try
        {
            var request = new AddItemRequest
            {
                CharacterId = characterId,
                ItemId = itemId,
                Quantity = quantity
            };

            var response = await _httpClient.PostAsJsonAsync("api/inventory/add", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>()
                ?? new ApiResponse<bool> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item {ItemId} for character {CharacterId}", itemId, characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 使用物品
    /// </summary>
    public async Task<ApiResponse<bool>> UseItemAsync(string characterId, string itemId, int slotIndex = -1)
    {
        try
        {
            var request = new UseItemRequest
            {
                CharacterId = characterId,
                ItemId = itemId,
                SlotIndex = slotIndex
            };

            var response = await _httpClient.PostAsJsonAsync("api/inventory/use", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>()
                ?? new ApiResponse<bool> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using item {ItemId} for character {CharacterId}", itemId, characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 装备物品
    /// </summary>
    public async Task<ApiResponse<bool>> EquipItemAsync(string characterId, string itemId, string equipmentSlot)
    {
        try
        {
            var request = new EquipItemRequest
            {
                CharacterId = characterId,
                ItemId = itemId,
                EquipmentSlot = equipmentSlot
            };

            var response = await _httpClient.PostAsJsonAsync("api/inventory/equip", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>()
                ?? new ApiResponse<bool> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error equipping item {ItemId} for character {CharacterId}", itemId, characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 出售物品
    /// </summary>
    public async Task<ApiResponse<int>> SellItemAsync(string characterId, string itemId, int quantity)
    {
        try
        {
            var request = new SellItemRequest
            {
                CharacterId = characterId,
                ItemId = itemId,
                Quantity = quantity
            };

            var response = await _httpClient.PostAsJsonAsync("api/inventory/sell", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<int>>()
                ?? new ApiResponse<int> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selling item {ItemId} for character {CharacterId}", itemId, characterId);
            return new ApiResponse<int>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }

    /// <summary>
    /// 同步库存数据到服务器
    /// </summary>
    public async Task<ApiResponse<bool>> SyncInventoryAsync(InventoryDto inventory)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/inventory/sync", inventory);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>()
                ?? new ApiResponse<bool> { Success = false, Message = "请求失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing inventory for character {CharacterId}", inventory.CharacterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "网络错误"
            };
        }
    }
}