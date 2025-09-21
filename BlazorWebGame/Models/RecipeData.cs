using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models;

public static class RecipeData
{
    private static readonly List<Recipe> _allRecipes = new()
    {
        // --- 烹饪配方 ---
        new Recipe
        {
            Id = "RECIPE_COOKED_TROUT",
            Name = "烤鳟鱼",
            RequiredProfession = ProductionProfession.Cooking,
            RequiredLevel = 1,
            Ingredients = new Dictionary<string, int> { { "FISH_TROUT", 1 } }, // 需要1个生鳟鱼
            ResultingItemId = "FOOD_COOKED_TROUT", // 产出烤鳟鱼
            CraftingTimeSeconds = 8,
            XpReward = 10,
            IsDefault = true // 默认学会
        },
        new Recipe
        {
            Id = "RECIPE_GOBLIN_OMELETTE",
            Name = "哥布林煎蛋",
            RequiredProfession = ProductionProfession.Cooking,
            RequiredLevel = 1,
            Ingredients = new Dictionary<string, int> { { "EQ_WEP_002", 1 } }, // 需要1个哥布林短棍
            ResultingItemId = "FOOD_GOBLIN_OMELETTE", // 产出哥布林煎蛋
            CraftingTimeSeconds = 15,
            XpReward = 25,
            IsDefault = false, // 非默认
            UnlockItemId = "RECIPE_ITEM_GOBLIN_OMELETTE" // 需要图纸解锁
        }
    };

    public static List<Recipe> AllRecipes => _allRecipes;
    public static Recipe? GetRecipeById(string id) => _allRecipes.FirstOrDefault(r => r.Id == id);
}