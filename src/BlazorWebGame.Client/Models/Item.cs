// This file is now moved to BlazorWebGame.Shared.Models.Items
// Import the shared version for backward compatibility
using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;

// Re-export the shared types in the original namespace for compatibility
namespace BlazorWebGame.Models
{
    using ItemType = BlazorWebGame.Shared.Enums.ItemType;
    using Item = BlazorWebGame.Shared.Models.Items.Item;
}