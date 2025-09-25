using System.Collections.Generic;

namespace BlazorWebGame.Models.Recipes
{
    /// <summary>
    /// 裁缝职业的所有配方数据
    /// </summary>
    public static class TailoringRecipes
    {
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe
            {
                Id = "RECIPE_TAILOR_LINEN_SHIRT",
                Name = "制作简易亚麻衬衣",
                RequiredProfession = ProductionProfession.Tailoring,
                RequiredLevel = 1,
                Ingredients = new Dictionary<string, int>
                {
                    { "MAT_LINEN_CLOTH", 3 },
                    { "MAT_COARSE_THREAD", 1 }
                },
                ResultingItemId = "EQ_CHEST_LINEN_SHIRT",
                CraftingTimeSeconds = 10,
                XpReward = 12,
                IsDefault = true
            }
            // 将来可以添加更多裁缝配方
        };
    }
}