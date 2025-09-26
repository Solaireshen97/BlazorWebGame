using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Models;
using BlazorWebGame.Services;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Enums;

namespace BlazorWebGame.Client.Services;

/// <summary>
/// 混合物品系统服务 - 支持逐步从客户端迁移到服务端
/// </summary>
public class HybridInventoryService
{
    private readonly ClientInventoryApiService _inventoryApi;
    private readonly InventoryService _legacyInventoryService;
    private readonly ILogger<HybridInventoryService> _logger;
    
    // 配置标志 - 是否使用服务端物品系统
    private bool _useServerInventory = true; // 默认使用服务端
    
    public event Action? OnInventoryChanged;

    public HybridInventoryService(
        ClientInventoryApiService inventoryApi,
        InventoryService legacyInventoryService,
        ILogger<HybridInventoryService> logger)
    {
        _inventoryApi = inventoryApi;
        _legacyInventoryService = legacyInventoryService;
        _logger = logger;
        
        // 订阅客户端服务事件
        _legacyInventoryService.OnStateChanged += () => OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 设置是否使用服务端物品系统
    /// </summary>
    public void SetUseServerInventory(bool useServer)
    {
        _useServerInventory = useServer;
        _logger.LogInformation("Inventory system switched to {Mode}", useServer ? "Server" : "Client");
    }

    /// <summary>
    /// 获取角色库存
    /// </summary>
    public async Task<InventoryDto?> GetInventoryAsync(string characterId)
    {
        if (_useServerInventory)
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
                    // 降级到客户端模式
                    return await GetClientInventoryAsync(characterId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory from server, falling back to client");
                return await GetClientInventoryAsync(characterId);
            }
        }
        else
        {
            return await GetClientInventoryAsync(characterId);
        }
    }

    /// <summary>
    /// 添加物品到库存
    /// </summary>
    public async Task<bool> AddItemAsync(Player character, string itemId, int quantity)
    {
        if (_useServerInventory)
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
                    // 降级到客户端模式
                    _legacyInventoryService.AddItemToInventory(character, itemId, quantity);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item via server, falling back to client");
                _legacyInventoryService.AddItemToInventory(character, itemId, quantity);
                return true;
            }
        }
        else
        {
            _legacyInventoryService.AddItemToInventory(character, itemId, quantity);
            return true;
        }
    }

    /// <summary>
    /// 使用物品
    /// </summary>
    public async Task<bool> UseItemAsync(Player character, string itemId, int slotIndex = -1)
    {
        if (_useServerInventory)
        {
            try
            {
                var response = await _inventoryApi.UseItemAsync(character.Id, itemId, slotIndex);
                if (response.Success)
                {
                    OnInventoryChanged?.Invoke();
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to use item via server: {Message}", response.Message);
                    // 降级到客户端模式
                    _legacyInventoryService.UseItem(character, itemId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error using item via server, falling back to client");
                _legacyInventoryService.UseItem(character, itemId);
                return true;
            }
        }
        else
        {
            _legacyInventoryService.UseItem(character, itemId);
            return true;
        }
    }

    /// <summary>
    /// 装备物品
    /// </summary>
    public async Task<bool> EquipItemAsync(Player character, string itemId, string equipmentSlot)
    {
        if (_useServerInventory)
        {
            try
            {
                var response = await _inventoryApi.EquipItemAsync(character.Id, itemId, equipmentSlot);
                if (response.Success)
                {
                    OnInventoryChanged?.Invoke();
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to equip item via server: {Message}", response.Message);
                    // 降级到客户端模式
                    _legacyInventoryService.EquipItem(character, itemId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error equipping item via server, falling back to client");
                _legacyInventoryService.EquipItem(character, itemId);
                return true;
            }
        }
        else
        {
            _legacyInventoryService.EquipItem(character, itemId);
            return true;
        }
    }

    /// <summary>
    /// 出售物品
    /// </summary>
    public async Task<int> SellItemAsync(Player character, string itemId, int quantity)
    {
        if (_useServerInventory)
        {
            try
            {
                var response = await _inventoryApi.SellItemAsync(character.Id, itemId, quantity);
                if (response.Success)
                {
                    OnInventoryChanged?.Invoke();
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to sell item via server: {Message}", response.Message);
                    // 降级到客户端模式
                    _legacyInventoryService.SellItem(character, itemId, quantity);
                    return 0; // 客户端版本不返回实际价值
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selling item via server, falling back to client");
                _legacyInventoryService.SellItem(character, itemId, quantity);
                return 0;
            }
        }
        else
        {
            _legacyInventoryService.SellItem(character, itemId, quantity);
            return 0;
        }
    }

    /// <summary>
    /// 同步客户端库存到服务端
    /// </summary>
    public async Task<bool> SyncToServerAsync(Player character)
    {
        if (!_useServerInventory) return false;

        try
        {
            // 将客户端库存转换为DTO
            var inventoryDto = ConvertToInventoryDto(character);
            var response = await _inventoryApi.SyncInventoryAsync(inventoryDto);
            
            if (response.Success)
            {
                _logger.LogInformation("Successfully synced inventory to server for character {CharacterId}", character.Id);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to sync inventory to server: {Message}", response.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing inventory to server");
            return false;
        }
    }

    /// <summary>
    /// 从服务端同步库存到客户端
    /// </summary>
    public async Task<bool> SyncFromServerAsync(Player character)
    {
        if (!_useServerInventory) return false;

        try
        {
            var response = await _inventoryApi.GetInventoryAsync(character.Id);
            if (response.Success && response.Data != null)
            {
                ApplyInventoryDto(character, response.Data);
                OnInventoryChanged?.Invoke();
                _logger.LogInformation("Successfully synced inventory from server for character {CharacterId}", character.Id);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to sync inventory from server: {Message}", response.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing inventory from server");
            return false;
        }
    }

    /// <summary>
    /// 获取客户端库存
    /// </summary>
    private async Task<InventoryDto> GetClientInventoryAsync(string characterId)
    {
        // 这里应该从客户端数据中创建库存DTO
        // 暂时返回空库存
        return new InventoryDto
        {
            CharacterId = characterId,
            Slots = new List<InventorySlotDto>(),
            Equipment = new Dictionary<string, InventorySlotDto>(),
            QuickSlots = new Dictionary<string, List<InventorySlotDto>>()
        };
    }

    /// <summary>
    /// 将Player库存转换为InventoryDto
    /// </summary>
    private InventoryDto ConvertToInventoryDto(Player character)
    {
        var dto = new InventoryDto
        {
            CharacterId = character.Id
        };

        // 转换库存槽
        for (int i = 0; i < character.Inventory.Count; i++)
        {
            var slot = character.Inventory[i];
            dto.Slots.Add(new InventorySlotDto
            {
                SlotIndex = i,
                ItemId = slot.ItemId ?? string.Empty,
                Quantity = slot.Quantity
            });
        }

        // 转换装备槽
        foreach (var kvp in character.EquippedItems)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                dto.Equipment[kvp.Key.ToString()] = new InventorySlotDto
                {
                    ItemId = kvp.Value,
                    Quantity = 1
                };
            }
        }

        return dto;
    }

    /// <summary>
    /// 将InventoryDto应用到Player
    /// </summary>
    private void ApplyInventoryDto(Player character, InventoryDto dto)
    {
        // 应用库存槽
        for (int i = 0; i < dto.Slots.Count && i < character.Inventory.Count; i++)
        {
            var dtoSlot = dto.Slots[i];
            var playerSlot = character.Inventory[i];
            
            playerSlot.ItemId = dtoSlot.ItemId;
            playerSlot.Quantity = dtoSlot.Quantity;
        }

        // 应用装备
        foreach (var kvp in dto.Equipment)
        {
            if (Enum.TryParse<EquipmentSlot>(kvp.Key, out var slot))
            {
                character.EquippedItems[slot] = kvp.Value.ItemId;
            }
        }
    }
}