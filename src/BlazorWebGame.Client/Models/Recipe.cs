using System.Collections.Generic;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Models;

public class Recipe
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required ProductionProfession RequiredProfession { get; set; }
    public int RequiredLevel { get; set; }
    public required Dictionary<string, int> Ingredients { get; set; } // 消耗的材料 <ItemId, Quantity>
    public required string ResultingItemId { get; set; } // 产出的物品
    public int ResultingItemQuantity { get; set; } = 1;
    public double CraftingTimeSeconds { get; set; }
    public int XpReward { get; set; }
    public bool IsDefault { get; set; } = false; // 是否是默认学会的配方
    public string? UnlockItemId { get; set; } // 解锁此配方所需的图纸物品ID
}