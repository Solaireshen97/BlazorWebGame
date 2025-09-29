using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Models;
using BlazorWebGame.Services;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services;

/// <summary>
/// 简化库存服务 - 现在只使用服务器API
/// </summary>
public class HybridInventoryService
{
    private readonly ClientInventoryApiService _inventoryApi;
    private readonly InventoryService _legacyInventoryService; // 仅用于UI兼容性
    private readonly ILogger<HybridInventoryService> _logger;
    
    public event Action? OnInventoryChanged;

    public HybridInventoryService(
        ClientInventoryApiService inventoryApi,
        InventoryService legacyInventoryService,
        ILogger<HybridInventoryService> logger)
    {
        _inventoryApi = inventoryApi;
        _legacyInventoryService = legacyInventoryService;
        _logger = logger;
        
        // 订阅客户端服务事件（用于UI兼容性）
        _legacyInventoryService.OnStateChanged += () => OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 设置库存系统模式 - 现在总是使用服务端
    /// </summary>
    [Obsolete("库存系统现在总是使用服务器模式")]
    public void SetUseServerInventory(bool useServer)
    {
        _logger.LogInformation("Inventory system is now always in server mode");
    }

    /// <summary>
    /// 获取角色库存 - 现在只从服务器获取
    /// </summary>
    public async Task<InventoryDto?> GetInventoryAsync(string characterId)
    {
        try
        {
            var response = await _inventoryApi.GetInventoryAsync(characterId);
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }
            else
            {
                _logger.LogWarning("Failed to get inventory from server: {Message}", response.Message);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory from server");
            return null;
        }
    }

    /// <summary>
    /// 添加物品到库存 - 现在只通过服务器API
    /// </summary>
    public async Task<bool> AddItemAsync(Player character, string itemId, int quantity)
    {
        try
        {
            var response = await _inventoryApi.AddItemAsync(character.Id, itemId, quantity);
            if (response.Success)
            {
                OnInventoryChanged?.Invoke();
                return response.Data;
            }
            else
            {
                _logger.LogWarning("Failed to add item via server: {Message}", response.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item via server");
            return false;
        }
    }

    /// <summary>
    /// 移除物品 - 现在通过服务器售卖API实现
    /// </summary>
    public async Task<bool> RemoveItemAsync(Player character, string itemId, int quantity)
    {
        try
        {
            // 由于没有直接的移除API，使用售卖API（设置价格为0）
            var response = await _inventoryApi.SellItemAsync(character.Id, itemId, quantity);
            if (response.Success)
            {
                OnInventoryChanged?.Invoke();
                return response.Data > 0;
            }
            else
            {
                _logger.LogWarning("Failed to remove item via server: {Message}", response.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item via server");
            return false;
        }
    }

    /// <summary>
    /// 使用物品 - 现在只通过服务器API
    /// </summary>
    public async Task<bool> UseItemAsync(Player character, string itemId)
    {
        try
        {
            var response = await _inventoryApi.UseItemAsync(character.Id, itemId);
            if (response.Success)
            {
                OnInventoryChanged?.Invoke();
                return response.Data;
            }
            else
            {
                _logger.LogWarning("Failed to use item via server: {Message}", response.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using item via server");
            return false;
        }
    }

    /// <summary>
    /// 装备物品 - 现在只通过服务器API
    /// </summary>
    public async Task<bool> EquipItemAsync(Player character, string itemId)
    {
        try
        {
            // 需要指定装备槽位，这里使用默认值
            var response = await _inventoryApi.EquipItemAsync(character.Id, itemId, "auto");
            if (response.Success)
            {
                OnInventoryChanged?.Invoke();
                return response.Data;
            }
            else
            {
                _logger.LogWarning("Failed to equip item via server: {Message}", response.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error equipping item via server");
            return false;
        }
    }

    /// <summary>
    /// 卸下装备 - 现在暂不支持，标记为过时
    /// </summary>
    [Obsolete("卸下装备功能需要服务器API支持")]
    public async Task<bool> UnequipItemAsync(Player character, EquipmentSlot slot)
    {
        _logger.LogWarning("UnequipItemAsync not implemented - server API needed");
        await Task.CompletedTask;
        return false;
    }

    // 保留一些同步方法用于UI兼容性（标记为过时）
    [Obsolete("请使用异步版本 AddItemAsync")]
    public void AddItem(Player character, string itemId, int quantity)
    {
        _ = Task.Run(async () => await AddItemAsync(character, itemId, quantity));
    }

    [Obsolete("请使用异步版本 RemoveItemAsync")]
    public bool RemoveItem(Player character, string itemId, int quantity)
    {
        var task = Task.Run(async () => await RemoveItemAsync(character, itemId, quantity));
        return task.Result;
    }

    [Obsolete("请使用异步版本 UseItemAsync")]
    public void UseItem(Player character, string itemId)
    {
        _ = Task.Run(async () => await UseItemAsync(character, itemId));
    }

    [Obsolete("请使用异步版本 EquipItemAsync")]
    public void EquipItem(Player character, string itemId)
    {
        _ = Task.Run(async () => await EquipItemAsync(character, itemId));
    }

    [Obsolete("请使用异步版本 UnequipItemAsync")]
    public void UnequipItem(Player character, EquipmentSlot slot)
    {
        _ = Task.Run(async () => await UnequipItemAsync(character, slot));
    }

    /// <summary>
    /// 获取客户端库存（仅用于UI兼容性）
    /// </summary>
    [Obsolete("本地库存已移除，请使用 GetInventoryAsync")]
    private async Task<InventoryDto?> GetClientInventoryAsync(string characterId)
    {
        // 返回空的库存DTO
        await Task.CompletedTask;
        return new InventoryDto
        {
            CharacterId = characterId,
            Slots = new List<InventorySlotDto>(),
            Equipment = new Dictionary<string, InventorySlotDto>(),
            QuickSlots = new Dictionary<string, List<InventorySlotDto>>()
        };
    }
}
