using BlazorWebGame.Shared.Enums;
using System.Collections.Generic;

namespace BlazorWebGame.Models.Recipes
{
    /// <summary>
    /// 锻造职业的所有配方数据
    /// </summary>
    public static class BlacksmithingRecipes
    {
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe
            {
                Id = "RECIPE_SMELT_COPPER",
                Name = "熔炼铜矿",
                RequiredProfession = ProductionProfession.Blacksmithing,
                RequiredLevel = 1,
                Ingredients = new Dictionary<string, int> { { "ORE_COPPER", 2 } },
                ResultingItemId = "BAR_COPPER",
                CraftingTimeSeconds = 5,
                XpReward = 5,
                IsDefault = true
            },
            new Recipe
            {
                Id = "RECIPE_COPPER_DAGGER",
                Name = "铜质匕首",
                RequiredProfession = ProductionProfession.Blacksmithing,
                RequiredLevel = 1,
                Ingredients = new Dictionary<string, int> { { "BAR_COPPER", 4 } },
                ResultingItemId = "EQ_WEP_COPPER_DAGGER",
                CraftingTimeSeconds = 12,
                XpReward = 15,
                IsDefault = true
            },
            new Recipe
            {
                Id = "RECIPE_COPPER_WIRE",
                Name = "制作铜丝",
                RequiredProfession = ProductionProfession.Blacksmithing,
                RequiredLevel = 8,
                Ingredients = new Dictionary<string, int> { { "BAR_COPPER", 1 } },
                ResultingItemId = "MAT_COPPER_WIRE",
                ResultingItemQuantity = 2,
                CraftingTimeSeconds = 6,
                XpReward = 8,
                IsDefault = true
            }
            // 将来可以添加更多锻造配方
        };
    }
}