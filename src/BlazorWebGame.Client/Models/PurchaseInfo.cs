// This file is now moved to BlazorWebGame.Shared.Models.Base.PurchaseInfo
// Import the shared version for backward compatibility
using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Base;

// Re-export the shared types in the original namespace for compatibility
namespace BlazorWebGame.Models
{
    using CurrencyType = BlazorWebGame.Shared.Enums.CurrencyType;
    using PurchaseInfo = BlazorWebGame.Shared.Models.Base.PurchaseInfo;
}