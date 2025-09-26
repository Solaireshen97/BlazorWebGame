// This file is now moved to BlazorWebGame.Shared.Models.Skills.Skill
// Import the shared version for backward compatibility
using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Skills;

// Re-export the shared types in the original namespace for compatibility
namespace BlazorWebGame.Models
{
    using SkillType = BlazorWebGame.Shared.Enums.SkillType;
    using Skill = BlazorWebGame.Shared.Models.Skills.Skill;
}