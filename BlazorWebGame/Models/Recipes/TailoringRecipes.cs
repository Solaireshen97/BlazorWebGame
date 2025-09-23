using System.Collections.Generic;

namespace BlazorWebGame.Models.Recipes
{
    /// <summary>
    /// �÷�ְҵ�������䷽����
    /// </summary>
    public static class TailoringRecipes
    {
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe
            {
                Id = "RECIPE_TAILOR_LINEN_SHIRT",
                Name = "���������������",
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
            // ����������Ӹ���÷��䷽
        };
    }
}