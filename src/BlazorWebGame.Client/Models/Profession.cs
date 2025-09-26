// This file is now moved to BlazorWebGame.Shared.Enums.ProfessionEnums
// Import the shared version for backward compatibility
using BlazorWebGame.Shared.Enums;

// Re-export the shared types in the original namespace for compatibility
namespace BlazorWebGame.Models
{
    using BattleProfession = BlazorWebGame.Shared.Enums.BattleProfession;
    using GatheringProfession = BlazorWebGame.Shared.Enums.GatheringProfession;
    using ProductionProfession = BlazorWebGame.Shared.Enums.ProductionProfession;
}