using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Server.Services.Inventory;

/// <summary>
/// 服务端物品系统服务 - 管理所有角色的物品和装备
/// </summary>
public class ServerInventoryService
{
    private readonly ILogger<ServerInventoryService> _logger;
    private readonly Dictionary<string, InventoryDto> _characterInventories = new();
    
    public event Action<string>? OnInventoryChanged; // characterId

    public ServerInventoryService(ILogger<ServerInventoryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取角色的库存
    /// </summary>
    public InventoryDto GetCharacterInventory(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return new InventoryDto { CharacterId = characterId };

        if (!_characterInventories.ContainsKey(characterId))
        {
            // 初始化角色库存
            _characterInventories[characterId] = CreateDefaultInventory(characterId);
        }

        return _characterInventories[characterId];
    }

    /// <summary>
    /// 添加物品到角色库存
    /// </summary>
    public async Task<ApiResponse<bool>> AddItemAsync(string characterId, string itemId, int quantity)
    {
        try
        {
            var inventory = GetCharacterInventory(characterId);
            
            // TODO: Replace with actual item data service when available
            // For now, assume items are stackable with max stack size 99
            var isStackable = true;
            var maxStackSize = 99;

            // 查找可堆叠的物品槽
            if (isStackable)
            {
                var existingSlot = inventory.Slots.FirstOrDefault(s => 
                    s.ItemId == itemId && s.Quantity < maxStackSize);
                
                if (existingSlot != null)
                {
                    var addableQuantity = Math.Min(quantity, maxStackSize - existingSlot.Quantity);
                    existingSlot.Quantity += addableQuantity;
                    quantity -= addableQuantity;
                    
                    if (quantity <= 0)
                    {
                        OnInventoryChanged?.Invoke(characterId);
                        return new ApiResponse<bool> { Success = true, Data = true };
                    }
                }
            }

            // 查找空槽位
            while (quantity > 0)
            {
                var emptySlot = inventory.Slots.FirstOrDefault(s => s.IsEmpty);
                if (emptySlot == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "库存已满"
                    };
                }

                var addQuantity = isStackable ? 
                    Math.Min(quantity, maxStackSize) : 1;
                
                emptySlot.ItemId = itemId;
                emptySlot.Quantity = addQuantity;
                quantity -= addQuantity;
            }

            OnInventoryChanged?.Invoke(characterId);
            _logger.LogInformation("Added {Quantity} of {ItemId} to character {CharacterId}", quantity, itemId, characterId);
            
            return new ApiResponse<bool> { Success = true, Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item {ItemId} to character {CharacterId}", itemId, characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "添加物品时发生错误"
            };
        }
    }

    /// <summary>
    /// 移除指定数量的物品
    /// </summary>
    public async Task<ApiResponse<bool>> RemoveItemAsync(string characterId, string itemId, int quantity)
    {
        try
        {
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(itemId) || quantity <= 0)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "参数无效"
                };
            }

            var inventory = GetCharacterInventory(characterId);
            var remainingToRemove = quantity;

            // 检查是否有足够的物品可以移除
            var totalOwned = inventory.Slots
                .Where(s => s.ItemId == itemId)
                .Sum(s => s.Quantity);

            if (totalOwned < quantity)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "库存中物品数量不足"
                };
            }

            // 移除物品
            for (int i = 0; i < inventory.Slots.Count && remainingToRemove > 0; i++)
            {
                var slot = inventory.Slots[i];
                if (slot.ItemId == itemId && slot.Quantity > 0)
                {
                    var removeAmount = Math.Min(slot.Quantity, remainingToRemove);
                    slot.Quantity -= removeAmount;
                    remainingToRemove -= removeAmount;

                    // 如果槽位为空，清空物品ID
                    if (slot.Quantity == 0)
                    {
                        slot.ItemId = string.Empty;
                    }
                }
            }

            OnInventoryChanged?.Invoke(characterId);
            _logger.LogInformation("Removed {Quantity} {ItemId} from character {CharacterId}", 
                quantity - remainingToRemove, itemId, characterId);

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = $"成功移除 {quantity - remainingToRemove} 个物品"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item {ItemId} from character {CharacterId}", itemId, characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "移除物品时发生错误"
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
            var inventory = GetCharacterInventory(characterId);
            
            InventorySlotDto? targetSlot = null;
            
            if (slotIndex >= 0 && slotIndex < inventory.Slots.Count)
            {
                targetSlot = inventory.Slots[slotIndex];
            }
            else
            {
                targetSlot = inventory.Slots.FirstOrDefault(s => s.ItemId == itemId && s.Quantity > 0);
            }

            if (targetSlot == null || targetSlot.IsEmpty || targetSlot.ItemId != itemId)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "没有找到指定物品"
                };
            }

            // 使用物品 - 这里应该调用物品的使用逻辑
            // 暂时只是减少数量
            targetSlot.Quantity--;
            if (targetSlot.Quantity <= 0)
            {
                targetSlot.ItemId = string.Empty;
                targetSlot.Quantity = 0;
            }

            OnInventoryChanged?.Invoke(characterId);
            _logger.LogInformation("Character {CharacterId} used item {ItemId}", characterId, itemId);
            
            return new ApiResponse<bool> { Success = true, Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using item {ItemId} for character {CharacterId}", itemId, characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "使用物品时发生错误"
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
            var inventory = GetCharacterInventory(characterId);
            
            // TODO: Add item validation when item service is available
            // For now, assume all items can be equipped
            
            // 查找物品在库存中的位置
            var sourceSlot = inventory.Slots.FirstOrDefault(s => s.ItemId == itemId && s.Quantity > 0);
            if (sourceSlot == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "库存中没有此物品"
                };
            }

            // 处理当前装备
            if (inventory.Equipment.ContainsKey(equipmentSlot))
            {
                var currentEquipped = inventory.Equipment[equipmentSlot];
                if (!currentEquipped.IsEmpty)
                {
                    // 卸下当前装备到库存
                    await AddItemAsync(characterId, currentEquipped.ItemId, currentEquipped.Quantity);
                }
            }

            // 装备新物品
            inventory.Equipment[equipmentSlot] = new InventorySlotDto
            {
                ItemId = itemId,
                Quantity = 1
            };

            // 从库存中移除
            sourceSlot.Quantity--;
            if (sourceSlot.Quantity <= 0)
            {
                sourceSlot.ItemId = string.Empty;
                sourceSlot.Quantity = 0;
            }

            OnInventoryChanged?.Invoke(characterId);
            _logger.LogInformation("Character {CharacterId} equipped {ItemId} to {Slot}", characterId, itemId, equipmentSlot);
            
            return new ApiResponse<bool> { Success = true, Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error equipping item {ItemId} for character {CharacterId}", itemId, characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "装备物品时发生错误"
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
            var inventory = GetCharacterInventory(characterId);
            
            // TODO: Replace with actual item data service when available
            var itemValue = 10; // Default item value

            int totalSold = 0;
            int totalValue = 0;

            // 查找并移除物品
            foreach (var slot in inventory.Slots.Where(s => s.ItemId == itemId).ToList())
            {
                var sellQuantity = Math.Min(quantity - totalSold, slot.Quantity);
                slot.Quantity -= sellQuantity;
                totalSold += sellQuantity;
                totalValue += sellQuantity * itemValue;

                if (slot.Quantity <= 0)
                {
                    slot.ItemId = string.Empty;
                    slot.Quantity = 0;
                }

                if (totalSold >= quantity)
                    break;
            }

            if (totalSold > 0)
            {
                OnInventoryChanged?.Invoke(characterId);
                _logger.LogInformation("Character {CharacterId} sold {Quantity} of {ItemId} for {Gold} gold", 
                    characterId, totalSold, itemId, totalValue);
            }

            return new ApiResponse<int> 
            { 
                Success = totalSold > 0, 
                Data = totalValue,
                Message = totalSold > 0 ? $"出售了 {totalSold} 个物品，获得 {totalValue} 金币" : "没有足够的物品出售"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selling item {ItemId} for character {CharacterId}", itemId, characterId);
            return new ApiResponse<int>
            {
                Success = false,
                Message = "出售物品时发生错误"
            };
        }
    }

    /// <summary>
    /// 创建默认库存
    /// </summary>
    private InventoryDto CreateDefaultInventory(string characterId)
    {
        var inventory = new InventoryDto
        {
            CharacterId = characterId
        };

        // 创建50个空库存槽
        for (int i = 0; i < 50; i++)
        {
            inventory.Slots.Add(new InventorySlotDto
            {
                SlotIndex = i,
                ItemId = string.Empty,
                Quantity = 0
            });
        }

        // 初始化装备槽
        var equipmentSlots = new[] { "Weapon", "Helmet", "Chest", "Legs", "Feet", "Gloves", "Ring1", "Ring2", "Necklace" };
        foreach (var slot in equipmentSlots)
        {
            inventory.Equipment[slot] = new InventorySlotDto
            {
                ItemId = string.Empty,
                Quantity = 0
            };
        }

        return inventory;
    }

    /// <summary>
    /// 同步客户端库存到服务端
    /// </summary>
    public async Task<ApiResponse<bool>> SyncInventoryAsync(string characterId, InventoryDto clientInventory)
    {
        try
        {
            _characterInventories[characterId] = clientInventory;
            OnInventoryChanged?.Invoke(characterId);
            
            _logger.LogInformation("Synced inventory for character {CharacterId}", characterId);
            return new ApiResponse<bool> { Success = true, Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing inventory for character {CharacterId}", characterId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "同步库存时发生错误"
            };
        }
    }

    /// <summary>
    /// 检查角色是否有足够的材料制作配方
    /// </summary>
    public bool CanAffordRecipe(string characterId, Dictionary<string, int> materials)
    {
        if (string.IsNullOrEmpty(characterId) || materials == null || materials.Count == 0)
            return false;

        try
        {
            var inventory = GetCharacterInventory(characterId);
            
            foreach (var material in materials)
            {
                var itemId = material.Key;
                var requiredQuantity = material.Value;
                
                // 计算角色拥有的该物品总数量
                var ownedQuantity = inventory.Slots
                    .Where(slot => !string.IsNullOrEmpty(slot.ItemId) && slot.ItemId == itemId)
                    .Sum(slot => slot.Quantity);
                
                if (ownedQuantity < requiredQuantity)
                {
                    return false;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recipe affordability for character {CharacterId}", characterId);
            return false;
        }
    }

    /// <summary>
    /// 消耗制作材料
    /// </summary>
    public async Task<bool> ConsumeCraftingMaterialsAsync(string characterId, Dictionary<string, int> materials)
    {
        if (!CanAffordRecipe(characterId, materials))
            return false;

        try
        {
            var inventory = GetCharacterInventory(characterId);
            
            foreach (var material in materials)
            {
                var itemId = material.Key;
                var requiredQuantity = material.Value;
                var remainingToRemove = requiredQuantity;
                
                // 从背包中移除材料
                for (int i = 0; i < inventory.Slots.Count && remainingToRemove > 0; i++)
                {
                    var slot = inventory.Slots[i];
                    if (!string.IsNullOrEmpty(slot.ItemId) && slot.ItemId == itemId && slot.Quantity > 0)
                    {
                        var removeAmount = Math.Min(slot.Quantity, remainingToRemove);
                        slot.Quantity -= removeAmount;
                        remainingToRemove -= removeAmount;
                        
                        // 如果物品数量为0，清空物品槽
                        if (slot.Quantity <= 0)
                        {
                            slot.ItemId = "";
                            slot.Quantity = 0;
                        }
                    }
                }
            }
            
            OnInventoryChanged?.Invoke(characterId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming crafting materials for character {CharacterId}", characterId);
            return false;
        }
    }
}