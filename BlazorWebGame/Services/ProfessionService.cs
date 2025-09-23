using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using System;
using System.Collections.Generic;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// רҵ����ϵͳ���񣬸�����ɼ���������ص��߼�
    /// </summary>
    public class ProfessionService
    {
        private readonly InventoryService _inventoryService;
        private readonly QuestService _questService;

        /// <summary>
        /// ״̬����¼�
        /// </summary>
        public event Action? OnStateChanged;

        public ProfessionService(InventoryService inventoryService, QuestService questService)
        {
            _inventoryService = inventoryService;
            _questService = questService;
        }

        /// <summary>
        /// ����ɼ��
        /// </summary>
        public void ProcessGathering(Player character, double elapsedSeconds)
        {
            if (character?.CurrentGatheringNode == null) return;

            character.GatheringCooldown -= elapsedSeconds;
            if (character.GatheringCooldown <= 0)
            {
                // ��ȡ������Ʒ
                _inventoryService.AddItemToInventory(
                    character, 
                    character.CurrentGatheringNode.ResultingItemId, 
                    character.CurrentGatheringNode.ResultingItemQuantity
                );

                // ���������Ʒ����
                double extraLootChance = character.GetTotalExtraLootChance();
                if (extraLootChance > 0 && new Random().NextDouble() < extraLootChance)
                {
                    _inventoryService.AddItemToInventory(
                        character, 
                        character.CurrentGatheringNode.ResultingItemId, 
                        character.CurrentGatheringNode.ResultingItemQuantity
                    );
                }

                // ���Ӳɼ�����
                character.AddGatheringXP(
                    character.CurrentGatheringNode.RequiredProfession, 
                    character.CurrentGatheringNode.XpReward
                );

                // �����������
                _questService.UpdateQuestProgress(
                    character, 
                    QuestType.GatherItem, 
                    character.CurrentGatheringNode.ResultingItemId, 
                    character.CurrentGatheringNode.ResultingItemQuantity
                );
                _questService.UpdateQuestProgress(character, QuestType.GatherItem, "any", 1);

                // ������ȴʱ��
                character.GatheringCooldown += GetCurrentGatheringTime(character);
                
                // ֪ͨUI����
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// ���������
        /// </summary>
        public void ProcessCrafting(Player character, double elapsedSeconds)
        {
            if (character?.CurrentRecipe == null) return;

            character.CraftingCooldown -= elapsedSeconds;
            if (character.CraftingCooldown <= 0)
            {
                // ����Ƿ����㹻�Ĳ���
                if (!_inventoryService.CanAffordRecipe(character, character.CurrentRecipe))
                {
                    StopCrafting(character);
                    return;
                }

                // ���Ĳ���
                foreach (var ingredient in character.CurrentRecipe.Ingredients)
                {
                    _inventoryService.RemoveItemFromInventory(character, ingredient.Key, ingredient.Value, out _);
                }

                // �����������Ʒ
                _inventoryService.AddItemToInventory(
                    character, 
                    character.CurrentRecipe.ResultingItemId, 
                    character.CurrentRecipe.ResultingItemQuantity
                );

                // ������������
                character.AddProductionXP(
                    character.CurrentRecipe.RequiredProfession, 
                    character.CurrentRecipe.XpReward
                );

                // �����������
                _questService.UpdateQuestProgress(
                    character, 
                    QuestType.CraftItem, 
                    character.CurrentRecipe.ResultingItemId, 
                    character.CurrentRecipe.ResultingItemQuantity
                );
                _questService.UpdateQuestProgress(character, QuestType.CraftItem, "any", 1);

                // ����Ƿ����㹻�Ĳ��ϼ�������
                if (_inventoryService.CanAffordRecipe(character, character.CurrentRecipe))
                {
                    character.CraftingCooldown += GetCurrentCraftingTime(character);
                }
                else
                {
                    StopCrafting(character);
                }
                
                // ֪ͨUI����
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// ��ʼ�ɼ��
        /// </summary>
        public void StartGathering(Player character, GatheringNode node)
        {
            if (character == null || node == null) return;
            
            // ����Ѿ��ڲɼ���ͬ�ڵ㣬����Ҫ���¿�ʼ
            if (character.CurrentAction == PlayerActionState.Gathering && 
                character.CurrentGatheringNode?.Id == node.Id) return;

            // ֹͣ��ǰ�
            StopCurrentAction(character);
            
            // ���òɼ�״̬
            character.CurrentAction = PlayerActionState.Gathering;
            character.CurrentGatheringNode = node;
            character.GatheringCooldown = GetCurrentGatheringTime(character);
            
            NotifyStateChanged();
        }

        /// <summary>
        /// ��ʼ�����
        /// </summary>
        public void StartCrafting(Player character, Recipe recipe)
        {
            if (character == null || recipe == null) return;
            
            // ����Ƿ����㹻����
            if (!_inventoryService.CanAffordRecipe(character, recipe)) return;

            // ֹͣ��ǰ�
            StopCurrentAction(character);
            
            // ��������״̬
            character.CurrentAction = PlayerActionState.Crafting;
            character.CurrentRecipe = recipe;
            character.CraftingCooldown = GetCurrentCraftingTime(character);
            
            NotifyStateChanged();
        }

        /// <summary>
        /// ֹͣ��ǰ����ɼ���������
        /// </summary>
        public void StopCurrentAction(Player character)
        {
            if (character == null) return;

            // ���û״̬
            character.CurrentAction = PlayerActionState.Idle;
            character.CurrentGatheringNode = null;
            character.CurrentRecipe = null;
            character.GatheringCooldown = 0;
            character.CraftingCooldown = 0;
            
            NotifyStateChanged();
        }
        
        /// <summary>
        /// ֹͣ�����
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
        /// ��ȡ��ǰ�ɼ�ʱ�䣨�����ٶȼӳɣ�
        /// </summary>
        public double GetCurrentGatheringTime(Player character)
        {
            if (character?.CurrentGatheringNode == null) return 0;
            
            double speedBonus = character.GetTotalGatheringSpeedBonus();
            return character.CurrentGatheringNode.GatheringTimeSeconds / (1 + speedBonus);
        }

        /// <summary>
        /// ��ȡ��ǰ����ʱ�䣨�����ٶȼӳɣ�
        /// </summary>
        public double GetCurrentCraftingTime(Player character)
        {
            if (character?.CurrentRecipe == null) return 0;
            
            double speedBonus = character.GetTotalCraftingSpeedBonus();
            return character.CurrentRecipe.CraftingTimeSeconds / (1 + speedBonus);
        }

        /// <summary>
        /// ����״̬����¼�
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}