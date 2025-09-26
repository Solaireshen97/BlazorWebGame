// This file is now moved to BlazorWebGame.Shared.Enums.SkillEnums
// Import the shared version for backward compatibility
using BlazorWebGame.Shared.Enums;

// Re-export the shared types in the original namespace for compatibility
namespace BlazorWebGame.Models
{
    using SkillEffectType = BlazorWebGame.Shared.Enums.SkillEffectType;
}