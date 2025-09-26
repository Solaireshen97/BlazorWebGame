// This file is now moved to BlazorWebGame.Shared.Models.Items.Consumable
// Import the shared version for backward compatibility
using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;

// Re-export the shared types in the original namespace for compatibility
namespace BlazorWebGame.Models
{
    using ConsumableCategory = BlazorWebGame.Shared.Enums.ConsumableCategory;
    using FoodType = BlazorWebGame.Shared.Enums.FoodType;
    using ConsumableEffectType = BlazorWebGame.Shared.Enums.ConsumableEffectType;
    using Consumable = BlazorWebGame.Shared.Models.Items.Consumable;
}