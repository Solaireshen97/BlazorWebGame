using BlazorWebGame.Shared.Enums;
using System.Collections.Generic;

namespace BlazorWebGame.Models.Recipes
{
    /// <summary>
    /// ����ְҵ�������䷽����
    /// </summary>
    public static class AlchemyRecipes
    {
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe
            {
                Id = "RECIPE_MINOR_HEALING_POTION",
                Name = "��������ҩˮ",
                RequiredProfession = ProductionProfession.Alchemy,
                RequiredLevel = 1,
                Ingredients = new Dictionary<string, int> { 
                    { "HERB_PEACEBLOOM", 1 }, 
                    { "HERB_SILVERLEAF", 1 } 
                },
                ResultingItemId = "CON_HP_POTION_1",
                CraftingTimeSeconds = 6,
                XpReward = 8,
                IsDefault = true
            }
            // ����������Ӹ��������䷽
        };
    }
}