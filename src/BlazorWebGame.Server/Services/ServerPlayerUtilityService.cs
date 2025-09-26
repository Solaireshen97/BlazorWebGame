using BlazorWebGame.GameConfig;
using BlazorWebGame.Models;
using BlazorWebGame.Shared.DTOs;
using System;
using System.Linq;

namespace BlazorWebGame.Server.Services
{
    /// <summary>
    /// 服务端玩家实用工具服务
    /// </summary>
    public class ServerPlayerUtilityService
    {
        private readonly ILogger<ServerPlayerUtilityService> _logger;

        public ServerPlayerUtilityService(ILogger<ServerPlayerUtilityService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 检查角色背包中是否有指定物品
        /// </summary>
        public bool HasItemInInventory(CharacterDetailsDto character, string itemId)
        {
            return character.Inventory?.Any(s => !s.IsEmpty && s.ItemId == itemId) ?? false;
        }

        /// <summary>
        /// 获取声望等级
        /// </summary>
        public ReputationTier GetReputationLevel(CharacterDetailsDto character, Faction faction)
        {
            var factionName = faction.ToString();
            var rep = character.Reputation?.GetValueOrDefault(factionName, 0) ?? 0;
            // 从高到低查找，返回第一个满足最低条件的等级
            return Models.Player.ReputationTiers.LastOrDefault(t => rep >= t.MinValue) ?? Models.Player.ReputationTiers.First();
        }

        /// <summary>
        /// 获取声望进度百分比
        /// </summary>
        public double GetReputationProgressPercentage(CharacterDetailsDto character, Faction faction)
        {
            var factionName = faction.ToString();
            var rep = character.Reputation?.GetValueOrDefault(factionName, 0) ?? 0;
            var tier = GetReputationLevel(character, faction);

            // 如果是最高等级，直接返回满进度
            if (tier.MaxValue - tier.MinValue <= 1)
            {
                return 100.0;
            }

            var progressInTier = rep - tier.MinValue;
            var totalForTier = tier.MaxValue - tier.MinValue;

            return (double)progressInTier / totalForTier * 100.0;
        }

        /// <summary>
        /// 确保数据一致性
        /// </summary>
        public void EnsureDataConsistency(CharacterDetailsDto character)
        {
            InitializeCollections(character);
        }

        /// <summary>
        /// 初始化集合
        /// </summary>
        public void InitializeCollections(CharacterDetailsDto character)
        {
            // 初始化专业经验字典
            character.BattleProfessionXP ??= new Dictionary<string, int>();
            character.GatheringProfessionXP ??= new Dictionary<string, int>();
            character.ProductionProfessionXP ??= new Dictionary<string, int>();

            // 初始化装备技能字典
            character.EquippedSkills ??= new Dictionary<string, List<string>>();

            // 为所有可能的职业初始化经验字典和技能列表
            foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
            {
                var professionName = profession.ToString();
                character.BattleProfessionXP.TryAdd(professionName, 10000);
                character.EquippedSkills.TryAdd(professionName, new List<string>());
            }

            foreach (var profession in (GatheringProfession[])Enum.GetValues(typeof(GatheringProfession)))
            {
                var professionName = profession.ToString();
                character.GatheringProfessionXP.TryAdd(professionName, 0);
            }

            foreach (var profession in (ProductionProfession[])Enum.GetValues(typeof(ProductionProfession)))
            {
                var professionName = profession.ToString();
                character.ProductionProfessionXP.TryAdd(professionName, 0);
            }

            // 初始化学习的配方
            character.LearnedRecipeIds ??= new HashSet<string>();
            
            // 确保默认配方已被学习
            foreach (var recipe in RecipeData.AllRecipes.Where(r => r.IsDefault))
            {
                character.LearnedRecipeIds.Add(recipe.Id);
            }

            // 初始化背包
            if (character.Inventory == null || !character.Inventory.Any())
            {
                character.Inventory = new List<InventorySlotDto>();
                for (int i = 0; i < 20; i++)
                {
                    character.Inventory.Add(new InventorySlotDto { SlotIndex = i });
                }
            }

            // 确保各种关键集合和字典，确保现有和旧集合都是非空
            character.ActiveBuffs ??= new List<StatBuffDto>();
            character.LearnedSharedSkills ??= new List<string>();
            character.SkillCooldowns ??= new Dictionary<string, DateTime>();
            character.EquippedItems ??= new Dictionary<string, string>();
            character.AutoSellItemIds ??= new HashSet<string>();
            character.DefeatedMonsterIds ??= new HashSet<string>();
            character.ConsumableCooldowns ??= new Dictionary<string, DateTime>();
            character.Reputation ??= new Dictionary<string, int>();

            // 初始化快捷栏
            character.PotionQuickSlots ??= new Dictionary<int, string>();
            character.CombatFoodQuickSlots ??= new Dictionary<int, string>();
            character.GatheringFoodQuickSlots ??= new Dictionary<int, string>();
            character.ProductionFoodQuickSlots ??= new Dictionary<int, string>();

            // 初始化基础属性
            character.BaseAttributes ??= new AttributeSetDto();
        }

        /// <summary>
        /// 获取角色背包中指定物品的数量
        /// </summary>
        public int GetItemQuantity(CharacterDetailsDto character, string itemId)
        {
            return character.Inventory?.Where(s => !s.IsEmpty && s.ItemId == itemId).Sum(s => s.Quantity) ?? 0;
        }

        /// <summary>
        /// 检查角色是否可以学习指定配方
        /// </summary>
        public bool CanLearnRecipe(CharacterDetailsDto character, Recipe recipe)
        {
            if (character.LearnedRecipeIds?.Contains(recipe.Id) == true)
            {
                return false; // 已经学会了
            }

            // 检查前置条件（如果有的话）
            // TODO: 添加配方学习的前置条件检查

            return true;
        }

        /// <summary>
        /// 学习配方
        /// </summary>
        public bool LearnRecipe(CharacterDetailsDto character, string recipeId)
        {
            var recipe = RecipeData.AllRecipes.FirstOrDefault(r => r.Id == recipeId);
            if (recipe == null)
            {
                return false;
            }

            if (!CanLearnRecipe(character, recipe))
            {
                return false;
            }

            character.LearnedRecipeIds ??= new HashSet<string>();
            return character.LearnedRecipeIds.Add(recipeId);
        }

        /// <summary>
        /// 检查角色是否满足等级要求
        /// </summary>
        public bool MeetsLevelRequirement(CharacterDetailsDto character, BattleProfession profession, int requiredLevel)
        {
            var professionName = profession.ToString();
            int xp = character.BattleProfessionXP?.GetValueOrDefault(professionName, 0) ?? 0;
            int currentLevel = ExpSystem.GetLevelFromExp(xp);
            return currentLevel >= requiredLevel;
        }
    }
}