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
        },
        new Recipe
        {
            Id = "RECIPE_SMELT_COPPER",
            Name = "熔炼铜矿",
            RequiredProfession = ProductionProfession.Blacksmithing, // 专业为锻造
            RequiredLevel = 1,
            Ingredients = new Dictionary<string, int> { { "ORE_COPPER", 2 } }, // 2个铜矿石
            ResultingItemId = "BAR_COPPER", // 产出1个铜锭
            CraftingTimeSeconds = 5,
            XpReward = 5,
            IsDefault = true // 默认学会
        },
        new Recipe
        {
            Id = "RECIPE_COPPER_DAGGER",
            Name = "铜质匕首",
            RequiredProfession = ProductionProfession.Blacksmithing,
            RequiredLevel = 1, // 需要5级锻造
            Ingredients = new Dictionary<string, int> { { "BAR_COPPER", 4 } }, // 需要4个铜锭
            ResultingItemId = "EQ_WEP_COPPER_DAGGER", // 产出铜质匕首
            CraftingTimeSeconds = 12,
            XpReward = 15,
            IsDefault = true // 默认学会
        },
        // ... 在 _allRecipes 列表中添加以下新配方 ...

// --- 珠宝加工配方 ---
new Recipe
{
    Id = "RECIPE_CUT_TIGERSEYE",
    Name = "切割劣质的虎眼石",
    RequiredProfession = ProductionProfession.Jewelcrafting, // 专业
    RequiredLevel = 1,
    Ingredients = new Dictionary<string, int> { { "GEM_ROUGH_TIGERSEYE", 1 } }, // 1个劣质虎眼石
    ResultingItemId = "GEM_TIGERSEYE", // 产出1个虎眼石
    CraftingTimeSeconds = 4,
    XpReward = 5,
    IsDefault = true // 默认学会
},
new Recipe
{
    Id = "RECIPE_COPPER_WIRE",
    Name = "制作铜丝",
    RequiredProfession = ProductionProfession.Blacksmithing, // 这个配方属于锻造
    RequiredLevel = 8, // 假设8级锻造可学
    Ingredients = new Dictionary<string, int> { { "BAR_COPPER", 1 } }, // 1个铜锭
    ResultingItemId = "MAT_COPPER_WIRE", // 产出2个铜丝
    ResultingItemQuantity = 2,
    CraftingTimeSeconds = 6,
    XpReward = 8,
    IsDefault = true // 默认学会
},
new Recipe
{
    Id = "RECIPE_TIGERSEYE_RING",
    Name = "虎眼石戒指",
    RequiredProfession = ProductionProfession.Jewelcrafting,
    RequiredLevel = 1,
    Ingredients = new Dictionary<string, int>
    {
        { "GEM_TIGERSEYE", 1 }, // 1个虎眼石
        { "MAT_COPPER_WIRE", 2 }  // 2个铜丝
    },
    ResultingItemId = "EQ_FINGER_TIGERSEYE_RING",
    CraftingTimeSeconds = 15,
    XpReward = 20,
    IsDefault = true // 默认学会
},
new Recipe
{
    Id = "RECIPE_LW_RAGGED_GLOVES",
    Name = "制作破烂的皮手套",
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
    IsDefault = true, // 这个配方不是默认学会的
},
new Recipe
{
    Id = "RECIPE_TAILOR_LINEN_SHIRT",
    Name = "制作简易亚麻衬衣",
    RequiredProfession = ProductionProfession.Tailoring,
    RequiredLevel = 1,
    Ingredients = new Dictionary<string, int>
    {
        { "MAT_LINEN_CLOTH", 3 },
        { "MAT_COARSE_THREAD", 1 } // 复用粗线
    },
    ResultingItemId = "EQ_CHEST_LINEN_SHIRT",
    CraftingTimeSeconds = 10,
    XpReward = 12,
    IsDefault = true,
},
    };

    public static List<Recipe> AllRecipes => _allRecipes;
    public static Recipe? GetRecipeById(string id) => _allRecipes.FirstOrDefault(r => r.Id == id);
}