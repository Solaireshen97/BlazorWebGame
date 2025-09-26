using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.DTOs;

/// <summary>
/// 商店物品信息传输对象
/// </summary>
public class ShopItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemTypeDto Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Price { get; set; }
    public CurrencyTypeDto Currency { get; set; }
    public string? CurrencyItemId { get; set; }
    
    // Equipment specific stats (if applicable)
    public int AttackBonus { get; set; }
    public int HealthBonus { get; set; }
    public string? EquipmentSlot { get; set; }
}

/// <summary>
/// 商店分类信息传输对象
/// </summary>
public class ShopCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

/// <summary>
/// 购买请求传输对象
/// </summary>
public class PurchaseRequestDto
{
    public string CharacterId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// 购买响应传输对象
/// </summary>
public class PurchaseResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RemainingGold { get; set; }
    public Dictionary<string, int> RemainingCurrencyItems { get; set; } = new();
    public string PurchasedItemId { get; set; } = string.Empty;
    public int PurchasedQuantity { get; set; }
}

/// <summary>
/// 销售请求传输对象
/// </summary>
public class SellRequestDto
{
    public string CharacterId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// 销售响应传输对象
/// </summary>
public class SellResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int GoldEarned { get; set; }
    public int RemainingGold { get; set; }
    public string SoldItemId { get; set; } = string.Empty;
    public int SoldQuantity { get; set; }
}

/// <summary>
/// 物品类型枚举
/// </summary>
public enum ItemTypeDto
{
    Equipment,
    Consumable,
    Material
}

/// <summary>
/// 货币类型枚举
/// </summary>
public enum CurrencyTypeDto
{
    Gold,
    Item
}