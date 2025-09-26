// This file is now moved to BlazorWebGame.Shared.Models.Combat.Buff
// Import the shared version for backward compatibility
using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Combat;

// Re-export the shared types in the original namespace for compatibility
namespace BlazorWebGame.Models
{
    using StatBuffType = BlazorWebGame.Shared.Enums.StatBuffType;
    using Buff = BlazorWebGame.Shared.Models.Combat.Buff;
}