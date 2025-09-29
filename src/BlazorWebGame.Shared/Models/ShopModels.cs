using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 商店领域模型
/// </summary>
public class Shop
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ShopType Type { get; private set; } = ShopType.General;
    public bool IsEnabled { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // 商店物品
    private readonly Dictionary<string, ShopItem> _items = new();
    public IReadOnlyDictionary<string, ShopItem> Items => _items;

    // 商店设置
    public ShopSettings Settings { get; private set; } = new();

    // 私有构造函数，用于反序列化
    private Shop() { }

    /// <summary>
    /// 创建商店
    /// </summary>
    public Shop(string name, string description, ShopType type = ShopType.General)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("商店名称不能为空", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加商品
    /// </summary>
    public bool AddItem(string itemId, int price, CurrencyType currencyType = CurrencyType.Gold, int stock = -1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || price <= 0)
            return false;

        var shopItem = new ShopItem(itemId, price, currencyType, stock);
        _items[itemId] = shopItem;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// 移除商品
    /// </summary>
    public bool RemoveItem(string itemId)
    {
        if (_items.Remove(itemId))
        {
            UpdatedAt = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 更新商品价格
    /// </summary>
    public bool UpdateItemPrice(string itemId, int newPrice)
    {
        if (_items.TryGetValue(itemId, out var item) && newPrice > 0)
        {
            item.UpdatePrice(newPrice);
            UpdatedAt = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 更新商品库存
    /// </summary>
    public bool UpdateItemStock(string itemId, int newStock)
    {
        if (_items.TryGetValue(itemId, out var item))
        {
            item.UpdateStock(newStock);
            UpdatedAt = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 购买商品
    /// </summary>
    public PurchaseResult PurchaseItem(string itemId, int quantity, Character character)
    {
        if (!IsEnabled)
            return new PurchaseResult(false, "商店暂时关闭");

        if (!_items.TryGetValue(itemId, out var shopItem))
            return new PurchaseResult(false, "商品不存在");

        if (!shopItem.IsAvailable)
            return new PurchaseResult(false, "商品暂时不可购买");

        if (shopItem.HasStock && shopItem.Stock < quantity)
            return new PurchaseResult(false, "库存不足");

        var totalPrice = shopItem.Price * quantity;

        // 检查货币
        bool canAfford = shopItem.CurrencyType switch
        {
            CurrencyType.Gold => character.Gold >= totalPrice,
            CurrencyType.Item => character.Inventory.HasItem(shopItem.CurrencyItemId ?? "", totalPrice),
            _ => false
        };

        if (!canAfford)
            return new PurchaseResult(false, "资金不足");

        // 扣除货币
        switch (shopItem.CurrencyType)
        {
            case CurrencyType.Gold:
                if (!character.SpendGold(totalPrice))
                    return new PurchaseResult(false, "扣除金币失败");
                break;
            case CurrencyType.Item:
                if (!character.Inventory.RemoveItem(shopItem.CurrencyItemId ?? "", totalPrice))
                    return new PurchaseResult(false, "扣除货币物品失败");
                break;
        }

        // 添加物品到背包
        if (!character.Inventory.AddItem(itemId, quantity))
        {
            // 回滚货币扣除
            switch (shopItem.CurrencyType)
            {
                case CurrencyType.Gold:
                    character.GainGold(totalPrice);
                    break;
                case CurrencyType.Item:
                    character.Inventory.AddItem(shopItem.CurrencyItemId ?? "", totalPrice);
                    break;
            }
            return new PurchaseResult(false, "背包空间不足");
        }

        // 减少库存
        if (shopItem.HasStock)
        {
            shopItem.DecreaseStock(quantity);
        }

        UpdatedAt = DateTime.UtcNow;
        return new PurchaseResult(true, "购买成功", totalPrice);
    }

    /// <summary>
    /// 出售物品给商店
    /// </summary>
    public SaleResult SellItem(string itemId, int quantity, Character character)
    {
        if (!IsEnabled)
            return new SaleResult(false, "商店暂时关闭");

        if (!Settings.AcceptsSellback)
            return new SaleResult(false, "此商店不收购物品");

        if (!character.Inventory.HasItem(itemId, quantity))
            return new SaleResult(false, "物品数量不足");

        // 计算售价（通常是购买价格的一定百分比）
        var sellPrice = CalculateSellPrice(itemId, quantity);

        if (!character.Inventory.RemoveItem(itemId, quantity))
            return new SaleResult(false, "移除物品失败");

        character.GainGold(sellPrice);
        UpdatedAt = DateTime.UtcNow;
        
        return new SaleResult(true, "出售成功", sellPrice);
    }

    /// <summary>
    /// 计算售价
    /// </summary>
    private int CalculateSellPrice(string itemId, int quantity)
    {
        if (_items.TryGetValue(itemId, out var shopItem))
        {
            // 按购买价格的50%计算售价
            return (int)(shopItem.Price * quantity * 0.5f);
        }
        
        // 如果商店没有这个物品，使用基础价格
        return quantity * 10; // 默认每个物品10金币
    }

    /// <summary>
    /// 启用商店
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 禁用商店
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取可用商品列表
    /// </summary>
    public List<ShopItem> GetAvailableItems()
    {
        return _items.Values.Where(item => item.IsAvailable).ToList();
    }

    /// <summary>
    /// 按分类获取商品
    /// </summary>
    public List<ShopItem> GetItemsByCategory(string category)
    {
        return _items.Values.Where(item => item.Category == category && item.IsAvailable).ToList();
    }
}

/// <summary>
/// 商店商品
/// </summary>
public class ShopItem
{
    public string ItemId { get; private set; } = string.Empty;
    public int Price { get; private set; } = 0;
    public CurrencyType CurrencyType { get; private set; } = CurrencyType.Gold;
    public string? CurrencyItemId { get; private set; } // 当货币类型为物品时使用
    public int Stock { get; private set; } = -1; // -1 表示无限库存
    public bool IsEnabled { get; private set; } = true;
    public string Category { get; private set; } = "General";
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public bool HasStock => Stock > 0 || Stock == -1;
    public bool IsAvailable => IsEnabled && HasStock;

    // 私有构造函数，用于反序列化
    private ShopItem() { }

    /// <summary>
    /// 创建商店商品
    /// </summary>
    public ShopItem(string itemId, int price, CurrencyType currencyType = CurrencyType.Gold, int stock = -1)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("物品ID不能为空", nameof(itemId));
        if (price <= 0)
            throw new ArgumentException("价格必须大于0", nameof(price));

        ItemId = itemId;
        Price = price;
        CurrencyType = currencyType;
        Stock = stock;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置货币物品ID（当货币类型为物品时）
    /// </summary>
    public void SetCurrencyItem(string currencyItemId)
    {
        CurrencyItemId = currencyItemId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置分类
    /// </summary>
    public void SetCategory(string category)
    {
        Category = category?.Trim() ?? "General";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新价格
    /// </summary>
    public void UpdatePrice(int newPrice)
    {
        if (newPrice > 0)
        {
            Price = newPrice;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 更新库存
    /// </summary>
    public void UpdateStock(int newStock)
    {
        Stock = newStock;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 减少库存
    /// </summary>
    public void DecreaseStock(int amount)
    {
        if (Stock > 0)
        {
            Stock = Math.Max(0, Stock - amount);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 增加库存
    /// </summary>
    public void IncreaseStock(int amount)
    {
        if (Stock >= 0 && amount > 0)
        {
            Stock += amount;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 启用商品
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 禁用商品
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// 商店设置
/// </summary>
public class ShopSettings
{
    public bool AcceptsSellback { get; set; } = true; // 是否接受回购
    public double SellbackRate { get; set; } = 0.5; // 回购价格比例
    public TimeSpan? StockRefreshInterval { get; set; } // 库存刷新间隔
    public DateTime? LastStockRefresh { get; set; }
    public int MaxTransactionPerDay { get; set; } = -1; // 每日最大交易次数，-1为无限制
    public Dictionary<string, object> CustomSettings { get; set; } = new();

    /// <summary>
    /// 检查是否需要刷新库存
    /// </summary>
    public bool NeedsStockRefresh()
    {
        if (StockRefreshInterval == null || LastStockRefresh == null)
            return false;

        return DateTime.UtcNow - LastStockRefresh.Value >= StockRefreshInterval.Value;
    }

    /// <summary>
    /// 标记库存已刷新
    /// </summary>
    public void MarkStockRefreshed()
    {
        LastStockRefresh = DateTime.UtcNow;
    }
}

/// <summary>
/// 购买结果
/// </summary>
public class PurchaseResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public int TotalCost { get; private set; } = 0;
    public DateTime TransactionTime { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建购买结果
    /// </summary>
    public PurchaseResult(bool success, string message, int totalCost = 0)
    {
        Success = success;
        Message = message;
        TotalCost = totalCost;
        TransactionTime = DateTime.UtcNow;
    }
}

/// <summary>
/// 出售结果
/// </summary>
public class SaleResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public int TotalEarnings { get; private set; } = 0;
    public DateTime TransactionTime { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建出售结果
    /// </summary>
    public SaleResult(bool success, string message, int totalEarnings = 0)
    {
        Success = success;
        Message = message;
        TotalEarnings = totalEarnings;
        TransactionTime = DateTime.UtcNow;
    }
}

/// <summary>
/// 交易记录
/// </summary>
public class Transaction
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string CharacterId { get; private set; } = string.Empty;
    public string ShopId { get; private set; } = string.Empty;
    public TransactionType Type { get; private set; } = TransactionType.Purchase;
    public string ItemId { get; private set; } = string.Empty;
    public int Quantity { get; private set; } = 0;
    public int UnitPrice { get; private set; } = 0;
    public int TotalAmount { get; private set; } = 0;
    public CurrencyType CurrencyType { get; private set; } = CurrencyType.Gold;
    public DateTime TransactionTime { get; private set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; private set; } = new();

    // 私有构造函数，用于反序列化
    private Transaction() { }

    /// <summary>
    /// 创建交易记录
    /// </summary>
    public Transaction(string characterId, string shopId, TransactionType type, string itemId, int quantity, int unitPrice, CurrencyType currencyType = CurrencyType.Gold)
    {
        CharacterId = characterId;
        ShopId = shopId;
        Type = type;
        ItemId = itemId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalAmount = unitPrice * quantity;
        CurrencyType = currencyType;
        TransactionTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加元数据
    /// </summary>
    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
    }
}

/// <summary>
/// 声望系统
/// </summary>
public class Reputation
{
    public string CharacterId { get; private set; } = string.Empty;
    public string FactionId { get; private set; } = string.Empty;
    public int Points { get; private set; } = 0;
    public ReputationLevel Level { get; private set; } = ReputationLevel.Neutral;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // 私有构造函数，用于反序列化
    private Reputation() { }

    /// <summary>
    /// 创建声望记录
    /// </summary>
    public Reputation(string characterId, string factionId)
    {
        CharacterId = characterId;
        FactionId = factionId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 增加声望
    /// </summary>
    public void GainReputation(int amount)
    {
        if (amount > 0)
        {
            Points += amount;
            UpdateLevel();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 减少声望
    /// </summary>
    public void LoseReputation(int amount)
    {
        if (amount > 0)
        {
            Points = Math.Max(0, Points - amount);
            UpdateLevel();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 更新声望等级
    /// </summary>
    private void UpdateLevel()
    {
        Level = Points switch
        {
            >= 10000 => ReputationLevel.Exalted,
            >= 5000 => ReputationLevel.Revered,
            >= 2000 => ReputationLevel.Honored,
            >= 500 => ReputationLevel.Friendly,
            >= 0 => ReputationLevel.Neutral,
            >= -500 => ReputationLevel.Unfriendly,
            >= -2000 => ReputationLevel.Hostile,
            _ => ReputationLevel.Hated
        };
    }

    /// <summary>
    /// 获取到下一等级所需的声望
    /// </summary>
    public int GetPointsToNextLevel()
    {
        return Level switch
        {
            ReputationLevel.Hated => -2000 - Points,
            ReputationLevel.Hostile => -500 - Points,
            ReputationLevel.Unfriendly => 0 - Points,
            ReputationLevel.Neutral => 500 - Points,
            ReputationLevel.Friendly => 2000 - Points,
            ReputationLevel.Honored => 5000 - Points,
            ReputationLevel.Revered => 10000 - Points,
            ReputationLevel.Exalted => 0, // 已是最高等级
            _ => 0
        };
    }
}

/// <summary>
/// 商店类型枚举
/// </summary>
public enum ShopType
{
    General,        // 杂货店
    Weapon,         // 武器店
    Armor,          // 护甲店
    Potion,         // 药水店
    Material,       // 材料店
    Food,           // 食物店
    Gem,            // 宝石店
    Recipe,         // 配方店
    Special         // 特殊商店
}

/// <summary>
/// 货币类型枚举
/// </summary>
public enum CurrencyType
{
    Gold,           // 金币
    Item,           // 物品货币
    Reputation,     // 声望
    Special         // 特殊货币
}

/// <summary>
/// 交易类型枚举
/// </summary>
public enum TransactionType
{
    Purchase,       // 购买
    Sale,           // 出售
    Trade,          // 交易
    Gift            // 赠送
}

/// <summary>
/// 声望等级枚举
/// </summary>
public enum ReputationLevel
{
    Hated = -3,         // 仇恨
    Hostile = -2,       // 敌对
    Unfriendly = -1,    // 不友好
    Neutral = 0,        // 中立
    Friendly = 1,       // 友好
    Honored = 2,        // 尊敬
    Revered = 3,        // 崇敬
    Exalted = 4         // 崇拜
}