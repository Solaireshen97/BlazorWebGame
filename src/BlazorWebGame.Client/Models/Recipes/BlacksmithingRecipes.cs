using BlazorWebGame.Shared.Enums;
using System.Collections.Generic;

namespace BlazorWebGame.Models.Recipes
{
    /// <summary>
    /// ����ְҵ�������䷽����
    /// </summary>
    public static class BlacksmithingRecipes
    {
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe
            {
                Id = "RECIPE_SMELT_COPPER",
                Name = "����ͭ��",
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
                Name = "ͭ��ذ��",
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
                Name = "����ͭ˿",
                RequiredProfession = ProductionProfession.Blacksmithing,
                RequiredLevel = 8,
                Ingredients = new Dictionary<string, int> { { "BAR_COPPER", 1 } },
                ResultingItemId = "MAT_COPPER_WIRE",
                ResultingItemQuantity = 2,
                CraftingTimeSeconds = 6,
                XpReward = 8,
                IsDefault = true
            }
            // ����������Ӹ�������䷽
        };
    }
}