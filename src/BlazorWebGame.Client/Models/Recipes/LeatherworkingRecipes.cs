using System.Collections.Generic;

namespace BlazorWebGame.Models.Recipes
{
    /// <summary>
    /// ��Ƥְҵ�������䷽����
    /// </summary>
    public static class LeatherworkingRecipes
    {
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe
            {
                Id = "RECIPE_LW_RAGGED_GLOVES",
                Name = "�������õ�Ƥ����",
                RequiredProfession = ProductionProfession.Leatherworking,
                RequiredLevel = 1,
                Ingredients = new Dictionary<string, int>
                {
                    { "MAT_RUINED_LEATHER_SCRAPS", 5 },
                    { "MAT_COARSE_THREAD", 1 }
                },
                ResultingItemId = "EQ_HANDS_RAGGED_GLOVES",
                CraftingTimeSeconds = 8,
                XpReward = 10,
                IsDefault = true
            }
            // ����������Ӹ�����Ƥ�䷽
        };
    }
}