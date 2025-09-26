using BlazorWebGame.Shared.Enums;
using System.Collections.Generic;

namespace BlazorWebGame.Models.Recipes
{
    /// <summary>
    /// ���ְҵ�������䷽����
    /// </summary>
    public static class CookingRecipes
    {
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe
            {
                Id = "RECIPE_COOKED_TROUT",
                Name = "������",
                RequiredProfession = ProductionProfession.Cooking,
                RequiredLevel = 1,
                Ingredients = new Dictionary<string, int> { { "FISH_TROUT", 1 } },
                ResultingItemId = "FOOD_COOKED_TROUT",
                CraftingTimeSeconds = 8,
                XpReward = 10,
                IsDefault = true
            },
            new Recipe
            {
                Id = "RECIPE_GOBLIN_OMELETTE",
                Name = "�粼�ּ嵰",
                RequiredProfession = ProductionProfession.Cooking,
                RequiredLevel = 1,
                Ingredients = new Dictionary<string, int> { { "EQ_WEP_002", 1 } },
                ResultingItemId = "FOOD_GOBLIN_OMELETTE",
                CraftingTimeSeconds = 15,
                XpReward = 25,
                IsDefault = false,
                UnlockItemId = "RECIPE_ITEM_GOBLIN_OMELETTE"
            }
            // ����������Ӹ�������䷽
        };
    }
}