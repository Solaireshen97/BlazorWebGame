using BlazorWebGame.Client.Services.Api;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Services;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Client.Services;

/// <summary>
/// 简化商店服务 - 现在只使用服务器API (简化版，待完善API接口)
/// </summary>
public class HybridShopService
{
    private readonly ShopApiService _shopApiService;
    private readonly InventoryService _localInventoryService; // 仅用于UI兼容性
    private readonly GameStateService _gameStateService;
    private readonly ILogger<HybridShopService> _logger;

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
    /// 设置运行模式 - 现在总是使用服务器
    /// </summary>
    [Obsolete("商店服务现在总是使用服务器模式")]
    public void SetServerMode(bool useServer)
    {
        _logger.LogInformation("商店服务现在总是使用服务器模式");
    }

    /// <summary>
    /// 获取商店物品 - 暂时返回空列表，待服务器API完善
    /// </summary>
    public async Task<List<Item>> GetShopItemsAsync()
    {
        _logger.LogWarning("GetShopItemsAsync - 待服务器API完善");
        await Task.CompletedTask;
        return new List<Item>();
    }

    /// <summary>
    /// 购买物品 - 为了兼容UI调用
    /// </summary>
    public async Task<BlazorWebGame.Shared.DTOs.ApiResponse<bool>> BuyItemAsync(string itemId, int quantity = 1)
    {
        _logger.LogWarning("BuyItemAsync called - 待服务器API完善");
        await Task.CompletedTask;
        
        return new BlazorWebGame.Shared.DTOs.ApiResponse<bool>
        {
            Success = false,
            Message = "商店功能待服务器API完善",
            Data = false
        };
    }

    /// <summary>
    /// 售卖物品 - 现在只通过服务器API
    /// </summary>
    public async Task<bool> SellItemAsync(Player character, string itemId, int quantity = 1)
    {
        _logger.LogWarning("SellItemAsync called - 待服务器API完善");
        await Task.CompletedTask;
        return false;
    }

    /// <summary>
    /// 获取物品价格 - 现在只从服务器获取
    /// </summary>
    public async Task<int> GetItemPriceAsync(string itemId)
    {
        _logger.LogWarning("GetItemPriceAsync called - 待服务器API完善");
        await Task.CompletedTask;
        return 0;
    }

    /// <summary>
    /// 获取商店分类 - 现在只从服务器获取
    /// </summary>
    public async Task<List<string>> GetShopCategoriesAsync()
    {
        _logger.LogWarning("GetShopCategoriesAsync called - 待服务器API完善");
        await Task.CompletedTask;
        return new List<string> { "武器", "装备", "消耗品", "材料", "其他" };
    }

    /// <summary>
    /// 按分类获取商店物品 - 现在只从服务器获取
    /// </summary>
    public async Task<List<Item>> GetShopItemsByCategoryAsync(string category)
    {
        _logger.LogWarning("GetShopItemsByCategoryAsync called - 待服务器API完善");
        await Task.CompletedTask;
        return new List<Item>();
    }

    /// <summary>
    /// 检查玩家是否能买得起物品
    /// </summary>
    public bool CanAffordItem(Player character, Item item)
    {
        if (character == null || item == null) return false;
        return character.Gold >= item.Value;
    }

    // 保留一些同步方法用于UI兼容性（标记为过时）
    [Obsolete("请使用异步版本 BuyItemAsync")]
    public bool BuyItem(Player character, string itemId)
    {
        _logger.LogWarning("BuyItem called - 待服务器API完善");
        return false;
    }

    [Obsolete("请使用异步版本 SellItemAsync")]
    public void SellItem(Player character, string itemId, int quantity)
    {
        _logger.LogWarning("SellItem called - 待服务器API完善"); 
    }
}
