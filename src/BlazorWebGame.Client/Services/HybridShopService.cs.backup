using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Services;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services;

/// <summary>
/// 混合商店服务 - 支持本地和服务器端商店功能切换
/// </summary>
public class HybridShopService
{
    private readonly ShopApiService _shopApiService;
    private readonly InventoryService _localInventoryService;
    private readonly GameStateService _gameStateService;
    private readonly ILogger<HybridShopService> _logger;
    private bool _useServerMode = true;

    public HybridShopService(
        ShopApiService shopApiService,
        InventoryService inventoryService,
        GameStateService gameStateService,
        ILogger<HybridShopService> logger)
    {
        _shopApiService = shopApiService;
        _localInventoryService = inventoryService;
        _gameStateService = gameStateService;
        _logger = logger;
    }

    /// <summary>
    /// 设置运行模式
    /// </summary>
    public void SetServerMode(bool useServer)
    {
        _useServerMode = useServer;
        _logger.LogInformation("商店服务切换到 {Mode} 模式", useServer ? "服务器" : "本地");
    }

    /// <summary>
    /// 获取商店物品（优先使用服务器端）
    /// </summary>
    public async Task<List<Item>> GetShopItemsAsync()
    {
        if (_useServerMode)
        {
            try
            {
                var response = await _shopApiService.GetShopItemsAsync();
                if (response.Success && response.Data != null)
                {
                    return ConvertShopItemDtosToItems(response.Data);
                }
                
                _logger.LogWarning("服务器端获取商店物品失败，回退到本地模式: {Message}", response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用服务器端商店API失败，回退到本地模式");
            }
        }

        // 回退到本地模式
        return ItemData.GetShopItems();
    }

    /// <summary>
    /// 根据分类获取商店物品
    /// </summary>
    public async Task<List<Item>> GetShopItemsByCategoryAsync(string category)
    {
        if (_useServerMode)
        {
            try
            {
                var response = await _shopApiService.GetShopItemsByCategoryAsync(category);
                if (response.Success && response.Data != null)
                {
                    return ConvertShopItemDtosToItems(response.Data);
                }
                
                _logger.LogWarning("服务器端获取分类商店物品失败，回退到本地模式: {Message}", response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用服务器端商店分类API失败，回退到本地模式");
            }
        }

        // 回退到本地模式
        return ItemData.GetShopItemsByCategory(category);
    }

    /// <summary>
    /// 获取商店分类
    /// </summary>
    public async Task<List<string>> GetShopCategoriesAsync()
    {
        if (_useServerMode)
        {
            try
            {
                var response = await _shopApiService.GetShopCategoriesAsync();
                if (response.Success && response.Data != null)
                {
                    return response.Data.Select(c => c.Name).ToList();
                }
                
                _logger.LogWarning("服务器端获取商店分类失败，回退到本地模式: {Message}", response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用服务器端商店分类API失败，回退到本地模式");
            }
        }

        // 回退到本地模式
        return ItemData.GetShopCategories();
    }

    /// <summary>
    /// 购买物品
    /// </summary>
    public async Task<(bool Success, string Message)> BuyItemAsync(string itemId, int quantity = 1)
    {
        var activeCharacter = _gameStateService.ActiveCharacter;
        if (activeCharacter == null)
        {
            return (false, "没有激活的角色");
        }

        if (_useServerMode)
        {
            try
            {
                var request = new PurchaseRequestDto
                {
                    CharacterId = activeCharacter.Id,
                    ItemId = itemId,
                    Quantity = quantity
                };

                var response = await _shopApiService.PurchaseItemAsync(request);
                if (response.Success && response.Data != null)
                {
                    // The game state will be updated automatically through the existing event system
                    return (true, response.Data.Message);
                }
                
                _logger.LogWarning("服务器端购买物品失败，回退到本地模式: {Message}", response.Message);
                return (false, response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用服务器端购买API失败，回退到本地模式");
            }
        }

        // 回退到本地模式
        if (activeCharacter is Player player)
        {
            bool success = _localInventoryService.BuyItem(player, itemId);
            return (success, success ? "购买成功" : "购买失败");
        }

        return (false, "角色类型不支持");
    }

    /// <summary>
    /// 出售物品
    /// </summary>
    public async Task<(bool Success, string Message, int GoldEarned)> SellItemAsync(string itemId, int quantity = 1)
    {
        var activeCharacter = _gameStateService.ActiveCharacter;
        if (activeCharacter == null)
        {
            return (false, "没有激活的角色", 0);
        }

        if (_useServerMode)
        {
            try
            {
                var request = new SellRequestDto
                {
                    CharacterId = activeCharacter.Id,
                    ItemId = itemId,
                    Quantity = quantity
                };

                var response = await _shopApiService.SellItemAsync(request);
                if (response.Success && response.Data != null)
                {
                    // The game state will be updated automatically through the existing event system
                    return (true, response.Data.Message, response.Data.GoldEarned);
                }
                
                _logger.LogWarning("服务器端出售物品失败，回退到本地模式: {Message}", response.Message);
                return (false, response.Message, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用服务器端出售API失败，回退到本地模式");
            }
        }

        // 回退到本地模式 - 注意：本地模式可能没有出售功能，所以先检查
        if (activeCharacter is Player player)
        {
            var item = ItemData.GetItemById(itemId);
            if (item != null)
            {
                var sellPrice = item.Value * quantity;
                _localInventoryService.SellItem(player, itemId, quantity);
                return (true, "出售成功", sellPrice);
            }
        }

        return (false, "出售失败", 0);
    }

    /// <summary>
    /// 检查是否能够购买物品
    /// </summary>
    public bool CanAffordItem(string itemId, int quantity = 1)
    {
        var activeCharacter = _gameStateService.ActiveCharacter;
        if (activeCharacter is not Player player)
            return false;

        var item = ItemData.GetItemById(itemId);
        if (item?.ShopPurchaseInfo == null)
            return false;

        var purchaseInfo = item.ShopPurchaseInfo;
        var totalPrice = purchaseInfo.Price * quantity;

        if (purchaseInfo.Currency == CurrencyType.Gold)
        {
            return player.Gold >= totalPrice;
        }
        else if (!string.IsNullOrEmpty(purchaseInfo.CurrencyItemId))
        {
            int ownedAmount = player.Inventory
                .Where(s => s.ItemId == purchaseInfo.CurrencyItemId)
                .Sum(s => s.Quantity);
            return ownedAmount >= totalPrice;
        }

        return false;
    }

    /// <summary>
    /// 转换ShopItemDto到Item对象
    /// </summary>
    private List<Item> ConvertShopItemDtosToItems(List<ShopItemDto> shopItemDtos)
    {
        var items = new List<Item>();
        
        foreach (var dto in shopItemDtos)
        {
            // 先尝试从现有数据中找到物品
            var existingItem = ItemData.GetItemById(dto.Id);
            if (existingItem != null)
            {
                items.Add(existingItem);
                continue;
            }

            // 如果找不到，创建一个基本的Item对象
            var item = new Item
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                Type = ConvertItemType(dto.Type),
                ShopPurchaseInfo = new PurchaseInfo
                {
                    ShopCategory = dto.Category,
                    Price = dto.Price,
                    Currency = ConvertCurrencyType(dto.Currency),
                    CurrencyItemId = dto.CurrencyItemId
                }
            };

            items.Add(item);
        }

        return items;
    }

    private ItemType ConvertItemType(ItemTypeDto type)
    {
        return type switch
        {
            ItemTypeDto.Equipment => ItemType.Equipment,
            ItemTypeDto.Consumable => ItemType.Consumable,
            ItemTypeDto.Material => ItemType.Material,
            _ => ItemType.Material
        };
    }

    private CurrencyType ConvertCurrencyType(CurrencyTypeDto type)
    {
        return type switch
        {
            CurrencyTypeDto.Gold => CurrencyType.Gold,
            CurrencyTypeDto.Item => CurrencyType.Item,
            _ => CurrencyType.Gold
        };
    }
}