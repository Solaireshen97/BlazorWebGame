using System.Collections.Generic;

namespace BlazorWebGame.Models.Recipes
{
    /// <summary>
    /// 工程学职业的所有配方数据
    /// </summary>
    public static class EngineeringRecipes
    {
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe
            {
                Id = "RECIPE_ENG_ROUGH_BOMB",
                Name = "制作劣质铜管炸弹",
                RequiredProfession = ProductionProfession.Engineering,
                RequiredLevel = 1,
                Ingredients = new Dictionary<string, int>
                {
                    { "BAR_COPPER", 1 },
                    { "MAT_ROUGH_STONE", 1 },
                    { "MAT_LINEN_CLOTH", 1 }
                },
                ResultingItemId = "CON_ENG_ROUGH_BOMB",
                CraftingTimeSeconds = 7,
                XpReward = 15,
                IsDefault = false,
                UnlockItemId = "RECIPE_ITEM_ENG_ROUGH_BOMB"
            }
            // 将来可以添加更多工程学配方
        };
    }
}