using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using System;
using System.Collections.Generic;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Services
{
    /// <summary>
    /// 专业技能系统服务，负责处理采集和制作相关的逻辑
    /// </summary>
    public class ProfessionService
    {
        private readonly InventoryService _inventoryService;
        private readonly QuestService _questService;

        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event Action? OnStateChanged;

        public ProfessionService(InventoryService inventoryService, QuestService questService)
        {
            _inventoryService = inventoryService;
            _questService = questService;
        }

        /// <summary>
        /// 处理采集活动
        /// </summary>
        public void ProcessGathering(Player character, double elapsedSeconds)
        {
            if (character?.CurrentGatheringNode == null) return;

            character.GatheringCooldown -= elapsedSeconds;
            if (character.GatheringCooldown <= 0)
            {
                // 获取基础物品
                _inventoryService.AddItemToInventory(
                    character, 
                    character.CurrentGatheringNode.ResultingItemId, 
                    character.CurrentGatheringNode.ResultingItemQuantity
                );

                // 计算额外物品机会
                double extraLootChance = character.GetTotalExtraLootChance();
                if (extraLootChance > 0 && new Random().NextDouble() < extraLootChance)
                {
                    _inventoryService.AddItemToInventory(
                        character, 
                        character.CurrentGatheringNode.ResultingItemId, 
                        character.CurrentGatheringNode.ResultingItemQuantity
                    );
                }

                // 增加采集经验
                character.AddGatheringXP(
                    character.CurrentGatheringNode.RequiredProfession, 
                    character.CurrentGatheringNode.XpReward
                );

                // 更新任务进度
                _questService.UpdateQuestProgress(
                    character, 
                    QuestType.GatherItem, 
                    character.CurrentGatheringNode.ResultingItemId, 
                    character.CurrentGatheringNode.ResultingItemQuantity
                );
                _questService.UpdateQuestProgress(character, QuestType.GatherItem, "any", 1);

                // 重置冷却时间
                character.GatheringCooldown += GetCurrentGatheringTime(character);
                
                // 通知UI更新
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 处理制作活动
        /// </summary>
        public void ProcessCrafting(Player character, double elapsedSeconds)
        {
            if (character?.CurrentRecipe == null) return;

            character.CraftingCooldown -= elapsedSeconds;
            if (character.CraftingCooldown <= 0)
            {
                // 检查是否有足够的材料
                if (!_inventoryService.CanAffordRecipe(character, character.CurrentRecipe))
                {
                    StopCrafting(character);
                    return;
                }

                // 消耗材料
                foreach (var ingredient in character.CurrentRecipe.Ingredients)
                {
                    _inventoryService.RemoveItemFromInventory(character, ingredient.Key, ingredient.Value, out _);
                }

                // 添加制作的物品
                _inventoryService.AddItemToInventory(
                    character, 
                    character.CurrentRecipe.ResultingItemId, 
                    character.CurrentRecipe.ResultingItemQuantity
                );

                // 增加制作经验
                character.AddProductionXP(
                    character.CurrentRecipe.RequiredProfession, 
                    character.CurrentRecipe.XpReward
                );

                // 更新任务进度
                _questService.UpdateQuestProgress(
                    character, 
                    QuestType.CraftItem, 
                    character.CurrentRecipe.ResultingItemId, 
                    character.CurrentRecipe.ResultingItemQuantity
                );
                _questService.UpdateQuestProgress(character, QuestType.CraftItem, "any", 1);

                // 检查是否还有足够的材料继续制作
                if (_inventoryService.CanAffordRecipe(character, character.CurrentRecipe))
                {
                    character.CraftingCooldown += GetCurrentCraftingTime(character);
                }
                else
                {
                    StopCrafting(character);
                }
                
                // 通知UI更新
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 开始采集活动
        /// </summary>
        public void StartGathering(Player character, GatheringNode node)
        {
            if (character == null || node == null) return;
            
            // 获取具体的采集状态
            PlayerActionState gatheringState = GetGatheringActionState(node.RequiredProfession);
            
            // 如果已经在采集相同节点，则不需要重新开始
            if (character.CurrentAction == gatheringState && 
                character.CurrentGatheringNode?.Id == node.Id) return;

            // 停止当前活动
            StopCurrentAction(character);
            
            // 设置采集状态
            character.CurrentAction = gatheringState;
            character.CurrentGatheringNode = node;
            character.GatheringCooldown = GetCurrentGatheringTime(character);
            
            NotifyStateChanged();
        }

        /// <summary>
        /// 开始制作活动
        /// </summary>
        public void StartCrafting(Player character, Recipe recipe)
        {
            if (character == null || recipe == null) return;
            
            // 检查是否有足够材料
            if (!_inventoryService.CanAffordRecipe(character, recipe)) return;

            // 获取具体的制作状态
            PlayerActionState craftingState = GetCraftingActionState(recipe.RequiredProfession);
            
            // 停止当前活动
            StopCurrentAction(character);
            
            // 设置制作状态
            character.CurrentAction = craftingState;
            character.CurrentRecipe = recipe;
            character.CraftingCooldown = GetCurrentCraftingTime(character);
            
            NotifyStateChanged();
        }

        /// <summary>
        /// 停止当前活动（采集或制作）
        /// </summary>
        public void StopCurrentAction(Player character)
        {
            if (character == null) return;

            // 重置活动状态
            character.CurrentAction = PlayerActionState.Idle;
            character.CurrentGatheringNode = null;
            character.CurrentRecipe = null;
            character.GatheringCooldown = 0;
            character.CraftingCooldown = 0;
            
            NotifyStateChanged();
        }
        
        /// <summary>
        /// 停止制作活动
        /// </summary>
        private void StopCrafting(Player character)
        {
            if (character == null) return;
            
            character.CurrentAction = PlayerActionState.Idle;
            character.CurrentRecipe = null;
            character.CraftingCooldown = 0;
            
            NotifyStateChanged();
        }

        /// <summary>
        /// 获取当前采集时间（考虑速度加成）
        /// </summary>
        public double GetCurrentGatheringTime(Player character)
        {
            if (character?.CurrentGatheringNode == null) return 0;
            
            double speedBonus = character.GetTotalGatheringSpeedBonus();
            return character.CurrentGatheringNode.GatheringTimeSeconds / (1 + speedBonus);
        }

        /// <summary>
        /// 获取当前制作时间（考虑速度加成）
        /// </summary>
        public double GetCurrentCraftingTime(Player character)
        {
            if (character?.CurrentRecipe == null) return 0;
            
            double speedBonus = character.GetTotalCraftingSpeedBonus();
            return character.CurrentRecipe.CraftingTimeSeconds / (1 + speedBonus);
        }

        /// <summary>
        /// 根据采集职业获取对应的行动状态
        /// </summary>
        private PlayerActionState GetGatheringActionState(GatheringProfession profession)
        {
            return profession switch
            {
                GatheringProfession.Miner => PlayerActionState.GatheringMining,
                GatheringProfession.Herbalist => PlayerActionState.GatheringHerbalism,
                GatheringProfession.Fishing => PlayerActionState.GatheringFishing,
                _ => PlayerActionState.GatheringMining  // 默认使用采矿作为备用
            };
        }

        /// <summary>
        /// 根据制作职业获取对应的行动状态
        /// </summary>
        private PlayerActionState GetCraftingActionState(ProductionProfession profession)
        {
            return profession switch
            {
                ProductionProfession.Cooking => PlayerActionState.CraftingCooking,
                ProductionProfession.Alchemy => PlayerActionState.CraftingAlchemy,
                ProductionProfession.Blacksmithing => PlayerActionState.CraftingBlacksmithing,
                ProductionProfession.Jewelcrafting => PlayerActionState.CraftingJewelcrafting,
                ProductionProfession.Leatherworking => PlayerActionState.CraftingLeatherworking,
                ProductionProfession.Tailoring => PlayerActionState.CraftingTailoring,
                ProductionProfession.Engineering => PlayerActionState.CraftingEngineering,
                _ => PlayerActionState.CraftingCooking  // 默认使用烹饪作为备用
            };
        }

        /// <summary>
        /// 触发状态变更事件
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}