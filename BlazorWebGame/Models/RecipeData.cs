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
        },
        new Recipe
        {
            Id = "RECIPE_MINOR_HEALING_POTION",
            Name = "初级治疗药水",
            RequiredProfession = ProductionProfession.Alchemy, // 专业为炼金
            RequiredLevel = 1,
            // 需要 1个宁神花 和 1个银叶草
            Ingredients = new Dictionary<string, int> { { "HERB_PEACEBLOOM", 1 }, { "HERB_SILVERLEAF", 1 } },
            ResultingItemId = "CON_HP_POTION_1", // 产出就是我们已有的初级治疗药水
            CraftingTimeSeconds = 6,
            XpReward = 8,
            IsDefault = true // 默认学会
        }
    };

    public static List<Recipe> AllRecipes => _allRecipes;
    public static Recipe? GetRecipeById(string id) => _allRecipes.FirstOrDefault(r => r.Id == id);
}