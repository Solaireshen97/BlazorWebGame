using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using System;
using System.Collections.Generic;
using System.Linq;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Services
{
    /// <summary>
    /// ��Ʒϵͳ���񣬸��������Ʒ�����в���
    /// </summary>
    public class InventoryService
    {
        /// <summary>
        /// ״̬����¼�
        /// </summary>
        public event Action? OnStateChanged;

        /// <summary>
        /// �����Ʒ����ɫ����
        /// </summary>
        public void AddItemToInventory(Player character, string itemId, int quantity)
        {
            if (character == null) return;
            
            var itemToAdd = ItemData.GetItemById(itemId);
            if (itemToAdd == null) return;
            
            // �����Ʒ�Ƿ�����Ϊ�Զ�����
            if (character.AutoSellItemIds.Contains(itemId))
            {
                character.Gold += itemToAdd.Value * quantity;
                return;
            }
            
            // ���Զѵ���Ʒ
            if (itemToAdd.IsStackable)
            {
                var existingSlot = character.Inventory.FirstOrDefault(s => s.ItemId == itemId && s.Quantity < 99);
                if (existingSlot != null)
                {
                    existingSlot.Quantity += quantity;
                    NotifyStateChanged();
                    return;
                }
            }
            
            // ���ҿղ�λ������Ʒ
            var emptySlot = character.Inventory.FirstOrDefault(s => s.IsEmpty);
            if (emptySlot != null)
            {
                emptySlot.ItemId = itemId;
                emptySlot.Quantity = quantity;
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// �ӽ�ɫ�����Ƴ���Ʒ
        /// </summary>
        /// <returns>�Ƿ�ɹ��Ƴ�����һ����Ʒ</returns>
        public bool RemoveItemFromInventory(Player character, string itemId, int quantityToRemove, out int actuallyRemoved)
        {
            actuallyRemoved = 0;
            if (character == null) return false;
            
            for (int i = character.Inventory.Count - 1; i >= 0; i--)
            {
                var slot = character.Inventory[i];
                if (slot.ItemId == itemId)
                {
                    int amountToRemoveFromSlot = Math.Min(quantityToRemove - actuallyRemoved, slot.Quantity);
                    slot.Quantity -= amountToRemoveFromSlot;
                    actuallyRemoved += amountToRemoveFromSlot;
                    
                    if (slot.Quantity <= 0) 
                        slot.ItemId = null;
                        
                    if (actuallyRemoved >= quantityToRemove) 
                        break;
                }
            }
            
            if (actuallyRemoved > 0)
                NotifyStateChanged();
                
            return actuallyRemoved > 0;
        }
        
        /// <summary>
        /// �򻯰��Ƴ���Ʒ�����������������
        /// </summary>
        public void RemoveItem(Player character, string itemId, int quantity)
        {
            if (character == null) return;
            
            int quantityToRemove = quantity;
            
            // �Ӻ���ǰ������������ʹ�Ƴ���һ���ѵ�������Ҳ�������
            for (int i = character.Inventory.Count - 1; i >= 0; i--)
            {
                var stack = character.Inventory[i];
                if (stack.ItemId == itemId)
                {
                    if (stack.Quantity > quantityToRemove)
                    {
                        stack.Quantity -= quantityToRemove;
                        quantityToRemove = 0;
                    }
                    else
                    {
                        quantityToRemove -= stack.Quantity;
                        stack.ItemId = null;
                        stack.Quantity = 0;
                    }
                    
                    if (quantityToRemove == 0)
                    {
                        NotifyStateChanged();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// װ����Ʒ
        /// </summary>
        public void EquipItem(Player character, string itemId)
        {
            if (character == null) return;
            
            var slotToEquipFrom = character.Inventory.FirstOrDefault(s => s.ItemId == itemId);
            if (slotToEquipFrom == null) return;
            
            if (ItemData.GetItemById(itemId) is not Equipment equipmentToEquip) return;

            List<EquipmentSlot> targetSlots = equipmentToEquip.Slot switch
            {
                EquipmentSlot.Finger1 or EquipmentSlot.Finger2 => new() { EquipmentSlot.Finger1, EquipmentSlot.Finger2 },
                EquipmentSlot.Trinket1 or EquipmentSlot.Trinket2 => new() { EquipmentSlot.Trinket1, EquipmentSlot.Trinket2 },
                _ => new() { equipmentToEquip.Slot }
            };

            // 1. ����Ѱ��һ���յĿ��ò�λ
            int emptySlotIndex = targetSlots.FindIndex(slot => !character.EquippedItems.ContainsKey(slot));
            
            EquipmentSlot? finalSlot;
            if (emptySlotIndex != -1)
            {
                // ����ҵ��˿�λ��������
                finalSlot = targetSlots[emptySlotIndex];
            }
            else if (targetSlots.Any())
            {
                // ���û�п�λ����������һ��Ŀ���λ����׼���滻��һ��
                finalSlot = targetSlots.First();
            }
            else
            {
                // �����Ŀ���λ��û�У���Ӧ�÷����������޷�װ��
                finalSlot = null;
            }

            // �������û���ҵ���װ���Ĳ�λ����ֱ�ӷ���
            if (finalSlot == null) return;

            // �������ȷ���Ĳ�λ���Ѿ���װ��������ж����
            if (character.EquippedItems.TryGetValue(finalSlot.Value, out var currentItemId))
            {
                UnequipItem(character, finalSlot.Value);
            }

            // �ӱ����м�����Ʒ����
            slotToEquipFrom.Quantity--;
            if (slotToEquipFrom.Quantity <= 0) 
                slotToEquipFrom.ItemId = null;

            // ������Ʒװ��������ȷ���Ĳ�λ��
            character.EquippedItems[finalSlot.Value] = itemId;
            character.Health = Math.Min(character.Health, character.GetTotalMaxHealth());
            NotifyStateChanged();
        }

        /// <summary>
        /// ж��װ��
        /// </summary>
        public void UnequipItem(Player character, EquipmentSlot slot)
        {
            if (character == null || !character.EquippedItems.TryGetValue(slot, out var itemIdToUnequip)) 
                return;
                
            character.EquippedItems.Remove(slot);
            AddItemToInventory(character, itemIdToUnequip, 1);
            character.Health = Math.Min(character.Health, character.GetTotalMaxHealth());
            NotifyStateChanged();
        }

        /// <summary>
        /// ������Ʒ
        /// </summary>
        public void SellItem(Player character, string itemId, int quantity = 1)
        {
            if (character == null) return;
            
            var itemData = ItemData.GetItemById(itemId);
            if (itemData == null) return;
            
            if (RemoveItemFromInventory(character, itemId, quantity, out int soldCount) && soldCount > 0)
            {
                character.Gold += itemData.Value * soldCount;
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// ʹ����Ʒ
        /// </summary>
        public void UseItem(Player character, string itemId)
        {
            if (character == null) return;
            
            if (ItemData.GetItemById(itemId) is not Consumable consumable) 
                return;
                
            if (consumable.CooldownSeconds > 0 && character.ConsumableCooldowns.ContainsKey(consumable.Id)) 
                return;
                
            if (RemoveItemFromInventory(character, itemId, 1, out int removedCount) && removedCount > 0)
            {
                ApplyConsumableEffect(character, consumable);
            }
        }

        /// <summary>
        /// ������Ʒ
        /// </summary>
        public bool BuyItem(Player character, string itemId)
        {
            if (character == null) return false;
            
            // ������Ʒ�����̵���Ϣ
            var itemToBuy = ItemData.GetItemById(itemId);
            if (itemToBuy?.ShopPurchaseInfo == null)
                return false; // ��Ʒ�����ڻ򲻿ɹ���
            
            var purchaseInfo = itemToBuy.ShopPurchaseInfo;

            // �������Ƿ��㹻
            if (purchaseInfo.Currency == CurrencyType.Gold)
            {
                if (character.Gold < purchaseInfo.Price)
                    return false; // ��Ҳ���
            }
            else // �����������Ʒ
            {
                int ownedAmount = character.Inventory
                    .Where(s => s.ItemId == purchaseInfo.CurrencyItemId)
                    .Sum(s => s.Quantity);
                    
                if (ownedAmount < purchaseInfo.Price)
                    return false; // ��Ʒ���Ҳ���
            }

            // �۳�����
            if (purchaseInfo.Currency == CurrencyType.Gold)
            {
                character.Gold -= purchaseInfo.Price;
            }
            else
            {
                RemoveItem(character, purchaseInfo.CurrencyItemId!, purchaseInfo.Price);
            }

            // ���������Ʒ��ӵ�����
            AddItemToInventory(character, itemToBuy.Id, 1);
            return true;
        }

        /// <summary>
        /// ���ÿ�������Ʒ
        /// </summary>
        public void SetQuickSlotItem(Player character, ConsumableCategory category, int slotId, string itemId)
        {
            if (character == null) return;
            
            var item = ItemData.GetItemById(itemId) as Consumable;
            if (item == null || item.Category != category) return;

            Dictionary<int, string>? targetSlots = category switch
            {
                ConsumableCategory.Potion => character.PotionQuickSlots,
                ConsumableCategory.Food when item.FoodType == FoodType.Combat => character.CombatFoodQuickSlots,
                ConsumableCategory.Food when item.FoodType == FoodType.Gathering => character.GatheringFoodQuickSlots,
                ConsumableCategory.Food when item.FoodType == FoodType.Production => character.ProductionFoodQuickSlots,
                _ => null
            };
            
            if (targetSlots == null) return;

            // �������Ʒ������������������Ƴ�
            var otherItemSlots = targetSlots.Where(kv => kv.Value == itemId && kv.Key != slotId).ToList();
            foreach (var otherSlot in otherItemSlots)
            {
                targetSlots.Remove(otherSlot.Key);
            }

            // �����µĿ����
            targetSlots[slotId] = itemId;
            NotifyStateChanged();
        }

        /// <summary>
        /// �����������Ʒ
        /// </summary>
        public void ClearQuickSlotItem(Player character, ConsumableCategory category, int slotId, FoodType foodType = FoodType.None)
        {
            if (character == null) return;
            
            Dictionary<int, string>? targetSlots = category switch
            {
                ConsumableCategory.Potion => character.PotionQuickSlots,
                ConsumableCategory.Food when foodType == FoodType.Combat => character.CombatFoodQuickSlots,
                ConsumableCategory.Food when foodType == FoodType.Gathering => character.GatheringFoodQuickSlots,
                ConsumableCategory.Food when foodType == FoodType.Production => character.ProductionFoodQuickSlots,
                _ => null
            };
            
            targetSlots?.Remove(slotId);
            NotifyStateChanged();
        }

        /// <summary>
        /// �л��Զ�������Ʒ����
        /// </summary>
        public void ToggleAutoSellItem(Player character, string itemId)
        {
            if (character == null) return;

            if (character.AutoSellItemIds.Contains(itemId))
            {
                character.AutoSellItemIds.Remove(itemId);
            }
            else
            {
                character.AutoSellItemIds.Add(itemId);
            }
            
            NotifyStateChanged();
        }

        /// <summary>
        /// ����ɫ�Ƿ��ܸ���ĳ���䷽�Ĳ���
        /// </summary>
        public bool CanAffordRecipe(Player character, Recipe recipe)
        {
            if (character == null || recipe == null) return false;
            
            foreach (var ingredient in recipe.Ingredients)
            {
                if (character.Inventory
                    .Where(s => s.ItemId == ingredient.Key)
                    .Sum(s => s.Quantity) < ingredient.Value) 
                    return false;
            }
            
            return true;
        }

        /// <summary>
        /// Ӧ������ƷЧ��
        /// </summary>
        private void ApplyConsumableEffect(Player character, Consumable consumable)
        {
            switch (consumable.Effect)
            {
                case ConsumableEffectType.Heal:
                    character.Health = Math.Min(character.GetTotalMaxHealth(), character.Health + (int)consumable.EffectValue);
                    break;
                    
                case ConsumableEffectType.StatBuff:
                    if (consumable.BuffType.HasValue && consumable.DurationSeconds.HasValue)
                    {
                        character.ActiveBuffs.RemoveAll(b => 
                            b.FoodType == consumable.FoodType && 
                            b.BuffType == consumable.BuffType.Value);
                            
                        character.ActiveBuffs.Add(new Buff
                        {
                            SourceItemId = consumable.Id,
                            BuffType = consumable.BuffType.Value,
                            BuffValue = (int)consumable.EffectValue,
                            TimeRemainingSeconds = consumable.DurationSeconds.Value,
                            FoodType = consumable.FoodType
                        });
                    }
                    break;
                    
                case ConsumableEffectType.LearnRecipe:
                    if (!string.IsNullOrEmpty(consumable.RecipeIdToLearn))
                    {
                        // ���䷽ID��ӵ���ҵ���ѧϰ�б���
                        character.LearnedRecipeIds.Add(consumable.RecipeIdToLearn);
                    }
                    break;
            }
            
            character.ConsumableCooldowns[consumable.Id] = consumable.CooldownSeconds;
            NotifyStateChanged();
        }

        /// <summary>
        /// �����Զ�����Ʒʹ��
        /// </summary>
        public void ProcessAutoConsumables(Player character)
        {
            if (character == null) return;

            var allQuickSlotItems = character.PotionQuickSlots
                .Concat(character.CombatFoodQuickSlots)
                .Concat(character.GatheringFoodQuickSlots)
                .Concat(character.ProductionFoodQuickSlots);

            foreach (var slot in allQuickSlotItems)
            {
                var itemId = slot.Value;
                if (string.IsNullOrEmpty(itemId) || character.ConsumableCooldowns.ContainsKey(itemId))
                    continue;

                if (ItemData.GetItemById(itemId) is not Consumable item)
                    continue;

                bool shouldUse = false;
                switch (item.Category)
                {
                    case ConsumableCategory.Potion:
                        if ((double)character.Health / character.GetTotalMaxHealth() < 0.7)
                            shouldUse = true;
                        break;

                    case ConsumableCategory.Food:
                        var actionState = character.CurrentAction.ToString();
                        if (item.FoodType == FoodType.Combat && character.CurrentAction == PlayerActionState.Combat ||
                            item.FoodType == FoodType.Gathering && actionState.StartsWith("Gathering") ||
                            item.FoodType == FoodType.Production && actionState.StartsWith("Crafting"))
                        {
                            if (!character.ActiveBuffs.Any(b => b.BuffType == item.BuffType))
                                shouldUse = true;
                        }
                        break;
                }

                if (shouldUse && RemoveItemFromInventory(character, itemId, 1, out int removedCount) && removedCount > 0)
                {
                    ApplyConsumableEffect(character, item);
                }
            }
        }

        /// <summary>
        /// ��������Ʒ��ȴʱ��
        /// </summary>
        public void UpdateConsumableCooldowns(Player character, double elapsedSeconds)
        {
            if (character == null || !character.ConsumableCooldowns.Any()) 
                return;
                
            var keys = character.ConsumableCooldowns.Keys.ToList();
            foreach (var key in keys)
            {
                character.ConsumableCooldowns[key] -= elapsedSeconds;
                if (character.ConsumableCooldowns[key] <= 0) 
                    character.ConsumableCooldowns.Remove(key);
            }
        }

        /// <summary>
        /// ����״̬����¼�
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}