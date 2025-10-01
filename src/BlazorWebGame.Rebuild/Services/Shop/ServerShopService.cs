using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Rebuild.Services.Character;
using BlazorWebGame.Rebuild.Services.Inventory;

namespace BlazorWebGame.Rebuild.Services.Shop;

/// <summary>
/// 服务端商店系统服务 - 处理商店相关的业务逻辑
/// </summary>
public class ServerShopService
{
    private readonly ILogger<ServerShopService> _logger;
    private readonly ServerInventoryService _inventoryService;
    private readonly ServerCharacterService _characterService;
    private readonly IDataStorageService _dataStorage;

    public ServerShopService(
        ILogger<ServerShopService> logger,
        ServerInventoryService inventoryService,
        ServerCharacterService characterService,
        IDataStorageService dataStorage)
    {
        _logger = logger;
        _inventoryService = inventoryService;
        _characterService = characterService;
        _dataStorage = dataStorage;
    }

    /// <summary>
    /// 获取所有商店物品
    /// </summary>
    public List<ShopItemDto> GetShopItems()
    {
        try
        {
            var shopItems = ItemData.GetShopItems();
            return shopItems.Select(ConvertToShopItemDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取商店物品时发生错误");
            return new List<ShopItemDto>();
        }
    }

    /// <summary>
    /// 根据分类获取商店物品
    /// </summary>
    public List<ShopItemDto> GetShopItemsByCategory(string category)
    {
        try
        {
            var shopItems = ItemData.GetShopItemsByCategory(category);
            return shopItems.Select(ConvertToShopItemDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类 {Category} 的商店物品时发生错误", category);
            return new List<ShopItemDto>();
        }
    }

    /// <summary>
    /// 获取所有商店分类
    /// </summary>
    public List<ShopCategoryDto> GetShopCategories()
    {
        try
        {
            var categories = ItemData.GetShopCategories();
            return categories.Select(category => new ShopCategoryDto
            {
                Name = category,
                ItemCount = ItemData.GetShopItemsByCategory(category).Count
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取商店分类时发生错误");
            return new List<ShopCategoryDto>();
        }
    }

    /// <summary>
    /// 购买物品
    /// </summary>
    public async Task<PurchaseResponseDto> PurchaseItemAsync(PurchaseRequestDto request)
    {
        try
        {
            // 验证请求
            if (string.IsNullOrEmpty(request.CharacterId) || string.IsNullOrEmpty(request.ItemId))
            {
                return new PurchaseResponseDto
                {
                    Success = false,
                    Message = "无效的购买请求"
                };
            }

            // 获取角色数据
            var playerData = await _dataStorage.GetPlayerAsync(request.CharacterId);
            if (playerData == null)
            {
                return new PurchaseResponseDto
                {
                    Success = false,
                    Message = "角色不存在"
                };
            }

            // 获取物品信息
            var item = ItemData.GetItemById(request.ItemId);
            if (item?.ShopPurchaseInfo == null)
            {
                return new PurchaseResponseDto
                {
                    Success = false,
                    Message = "物品不存在或不可购买"
                };
            }

            var purchaseInfo = item.ShopPurchaseInfo;
            var totalPrice = purchaseInfo.Price * request.Quantity;

            // 检查货币是否充足
            if (purchaseInfo.Currency == CurrencyType.Gold)
            {
                if (playerData.Gold < totalPrice)
                {
                    return new PurchaseResponseDto
                    {
                        Success = false,
                        Message = "金币不足",
                        RemainingGold = playerData.Gold
                    };
                }
            }
            else if (!string.IsNullOrEmpty(purchaseInfo.CurrencyItemId))
            {
                var inventory = _inventoryService.GetCharacterInventory(request.CharacterId);
                var ownedAmount = inventory.Slots
                    .Where(s => s.ItemId == purchaseInfo.CurrencyItemId)
                    .Sum(s => s.Quantity);

                if (ownedAmount < totalPrice)
                {
                    return new PurchaseResponseDto
                    {
                        Success = false,
                        Message = "所需物品数量不足",
                        RemainingCurrencyItems = new Dictionary<string, int>
                        {
                            { purchaseInfo.CurrencyItemId, ownedAmount }
                        }
                    };
                }
            }

            // 执行购买
            if (purchaseInfo.Currency == CurrencyType.Gold)
            {
                playerData.Gold -= totalPrice;
            }
            else if (!string.IsNullOrEmpty(purchaseInfo.CurrencyItemId))
            {
                // 移除消费的物品
                await _inventoryService.RemoveItemAsync(request.CharacterId, purchaseInfo.CurrencyItemId, totalPrice);
            }

            // 添加购买的物品到库存
            await _inventoryService.AddItemAsync(request.CharacterId, request.ItemId, request.Quantity);

            // 保存角色数据
            await _dataStorage.SavePlayerAsync(playerData);

            _logger.LogInformation("角色 {CharacterId} 购买了 {Quantity} 个 {ItemId}", 
                request.CharacterId, request.Quantity, request.ItemId);

            return new PurchaseResponseDto
            {
                Success = true,
                Message = "购买成功",
                RemainingGold = playerData.Gold,
                PurchasedItemId = request.ItemId,
                PurchasedQuantity = request.Quantity
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "购买物品时发生错误: CharacterId={CharacterId}, ItemId={ItemId}", 
                request.CharacterId, request.ItemId);
            
            return new PurchaseResponseDto
            {
                Success = false,
                Message = "购买过程中发生错误，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 出售物品
    /// </summary>
    public async Task<SellResponseDto> SellItemAsync(SellRequestDto request)
    {
        try
        {
            // 验证请求
            if (string.IsNullOrEmpty(request.CharacterId) || string.IsNullOrEmpty(request.ItemId))
            {
                return new SellResponseDto
                {
                    Success = false,
                    Message = "无效的销售请求"
                };
            }

            // 获取角色数据
            var playerData = await _dataStorage.GetPlayerAsync(request.CharacterId);
            if (playerData == null)
            {
                return new SellResponseDto
                {
                    Success = false,
                    Message = "角色不存在"
                };
            }

            // 获取物品信息
            var item = ItemData.GetItemById(request.ItemId);
            if (item == null)
            {
                return new SellResponseDto
                {
                    Success = false,
                    Message = "物品不存在"
                };
            }

            // 检查库存中是否有足够的物品
            var inventory = _inventoryService.GetCharacterInventory(request.CharacterId);
            var ownedAmount = inventory.Slots
                .Where(s => s.ItemId == request.ItemId)
                .Sum(s => s.Quantity);

            if (ownedAmount < request.Quantity)
            {
                return new SellResponseDto
                {
                    Success = false,
                    Message = "库存中物品数量不足"
                };
            }

            // 计算销售价格 (通常是购买价格的一半)
            var sellPrice = (item.Value > 0 ? item.Value : 1) * request.Quantity;

            // 移除物品
            await _inventoryService.RemoveItemAsync(request.CharacterId, request.ItemId, request.Quantity);

            // 增加金币
            playerData.Gold += sellPrice;
            await _dataStorage.SavePlayerAsync(playerData);

            _logger.LogInformation("角色 {CharacterId} 出售了 {Quantity} 个 {ItemId}，获得 {Gold} 金币", 
                request.CharacterId, request.Quantity, request.ItemId, sellPrice);

            return new SellResponseDto
            {
                Success = true,
                Message = "出售成功",
                GoldEarned = sellPrice,
                RemainingGold = playerData.Gold,
                SoldItemId = request.ItemId,
                SoldQuantity = request.Quantity
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出售物品时发生错误: CharacterId={CharacterId}, ItemId={ItemId}", 
                request.CharacterId, request.ItemId);
            
            return new SellResponseDto
            {
                Success = false,
                Message = "出售过程中发生错误，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 将 Item 转换为 ShopItemDto
    /// </summary>
    private ShopItemDto ConvertToShopItemDto(Item item)
    {
        var dto = new ShopItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Type = ConvertItemType(item.Type),
            Category = item.ShopPurchaseInfo?.ShopCategory ?? "杂项",
            Price = item.ShopPurchaseInfo?.Price ?? 0,
            Currency = ConvertCurrencyType(item.ShopPurchaseInfo?.Currency ?? CurrencyType.Gold),
            CurrencyItemId = item.ShopPurchaseInfo?.CurrencyItemId
        };

        // 如果是装备，添加装备特有属性
        if (item is Equipment equipment)
        {
            dto.AttackBonus = equipment.AttackBonus;
            dto.HealthBonus = equipment.HealthBonus;
            dto.EquipmentSlot = equipment.Slot.ToString();
        }

        return dto;
    }

    private ItemTypeDto ConvertItemType(ItemType type)
    {
        return type switch
        {
            ItemType.Equipment => ItemTypeDto.Equipment,
            ItemType.Consumable => ItemTypeDto.Consumable,
            ItemType.Material => ItemTypeDto.Material,
            _ => ItemTypeDto.Material
        };
    }

    private CurrencyTypeDto ConvertCurrencyType(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.Gold => CurrencyTypeDto.Gold,
            CurrencyType.Item => CurrencyTypeDto.Item,
            _ => CurrencyTypeDto.Gold
        };
    }
}