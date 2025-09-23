using System.Collections.Generic;

namespace BlazorWebGame.Models.Recipes
{
    /// <summary>
    /// ����ѧְҵ�������䷽����
    /// </summary>
    public static class EngineeringRecipes
    {
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe
            {
                Id = "RECIPE_ENG_ROUGH_BOMB",
                Name = "��������ͭ��ը��",
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
            // ����������Ӹ��๤��ѧ�䷽
        };
    }
}