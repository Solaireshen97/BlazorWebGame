using BlazorWebGame.Shared.Enums;
using System.Collections.Generic;

namespace BlazorWebGame.Models.Recipes
{
    /// <summary>
    /// 珠宝加工职业的所有配方数据
    /// </summary>
    public static class JewelcraftingRecipes
    {
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe
            {
                Id = "RECIPE_CUT_TIGERSEYE",
                Name = "切割劣质的虎眼石",
                RequiredProfession = ProductionProfession.Jewelcrafting,
                RequiredLevel = 1,
                Ingredients = new Dictionary<string, int> { { "GEM_ROUGH_TIGERSEYE", 1 } },
                ResultingItemId = "GEM_TIGERSEYE",
                CraftingTimeSeconds = 4,
                XpReward = 5,
                IsDefault = true
            },
            new Recipe
            {
                Id = "RECIPE_TIGERSEYE_RING",
                Name = "虎眼石戒指",
                RequiredProfession = ProductionProfession.Jewelcrafting,
                RequiredLevel = 1,
                Ingredients = new Dictionary<string, int>
                {
                    { "GEM_TIGERSEYE", 1 },
                    { "MAT_COPPER_WIRE", 2 }
                },
                ResultingItemId = "EQ_FINGER_TIGERSEYE_RING",
                CraftingTimeSeconds = 15,
                XpReward = 20,
                IsDefault = true
            }
            // 将来可以添加更多珠宝加工配方
        };
    }
}