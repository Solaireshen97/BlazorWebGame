using BlazorWebGame.Shared.Enums;
using System.Collections.Generic;

namespace BlazorWebGame.Models.Recipes
{
    /// <summary>
    /// 炼金职业的所有配方数据
    /// </summary>
    public static class AlchemyRecipes
    {
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe
            {
                Id = "RECIPE_MINOR_HEALING_POTION",
                Name = "初级治疗药水",
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
            // 将来可以添加更多炼金配方
        };
    }
}