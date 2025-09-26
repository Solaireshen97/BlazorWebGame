using BlazorWebGame.Models.Recipes;
using System.Collections.Generic;
using System.Linq;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Models
{
    /// <summary>
    /// 提供对所有制作配方的统一访问
    /// </summary>
    public static class RecipeData
    {
        private static readonly List<Recipe> _allRecipes;

        static RecipeData()
        {
            // 在静态构造函数中初始化所有配方
            _allRecipes = new List<Recipe>();
            _allRecipes.AddRange(CookingRecipes.Recipes);
            _allRecipes.AddRange(AlchemyRecipes.Recipes);
            _allRecipes.AddRange(BlacksmithingRecipes.Recipes);
            _allRecipes.AddRange(JewelcraftingRecipes.Recipes);
            _allRecipes.AddRange(LeatherworkingRecipes.Recipes);
            _allRecipes.AddRange(TailoringRecipes.Recipes);
            _allRecipes.AddRange(EngineeringRecipes.Recipes);
        }

        /// <summary>
        /// 获取所有配方列表
        /// </summary>
        public static List<Recipe> AllRecipes => _allRecipes;

        /// <summary>
        /// 根据ID获取配方
        /// </summary>
        public static Recipe? GetRecipeById(string id) => _allRecipes.FirstOrDefault(r => r.Id == id);

        /// <summary>
        /// 获取特定职业的所有配方
        /// </summary>
        public static List<Recipe> GetRecipesByProfession(ProductionProfession profession) =>
            _allRecipes.Where(r => r.RequiredProfession == profession).ToList();

        /// <summary>
        /// 获取特定等级及以下的配方
        /// </summary>
        public static List<Recipe> GetRecipesByMaxLevel(ProductionProfession profession, int maxLevel) =>
            _allRecipes.Where(r => r.RequiredProfession == profession && r.RequiredLevel <= maxLevel).ToList();

        /// <summary>
        /// 获取默认学会的配方
        /// </summary>
        public static List<Recipe> GetDefaultRecipes() =>
            _allRecipes.Where(r => r.IsDefault).ToList();

        /// <summary>
        /// 获取需要通过物品学习的配方
        /// </summary>
        public static List<Recipe> GetLearnableRecipes() =>
            _allRecipes.Where(r => !r.IsDefault && r.UnlockItemId != null).ToList();

        /// <summary>
        /// 获取使用特定材料的配方
        /// </summary>
        public static List<Recipe> GetRecipesUsingIngredient(string ingredientId) =>
            _allRecipes.Where(r => r.Ingredients.ContainsKey(ingredientId)).ToList();
    }
}