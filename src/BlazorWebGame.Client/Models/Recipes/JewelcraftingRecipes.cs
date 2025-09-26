using BlazorWebGame.Shared.Enums;
using System.Collections.Generic;

namespace BlazorWebGame.Models.Recipes
{
    /// <summary>
    /// �鱦�ӹ�ְҵ�������䷽����
    /// </summary>
    public static class JewelcraftingRecipes
    {
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe
            {
                Id = "RECIPE_CUT_TIGERSEYE",
                Name = "�и����ʵĻ���ʯ",
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
                Name = "����ʯ��ָ",
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
            // ����������Ӹ����鱦�ӹ��䷽
        };
    }
}