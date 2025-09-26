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
    /// 物品系统服务，负责管理物品的所有操作
    /// </summary>
    public class InventoryService
    {
        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event Action? OnStateChanged;

        /// <summary>
        /// 添加物品到角色背包
        /// </summary>
        public void AddItemToInventory(Player character, string itemId, int quantity)
        {
            if (character == null) return;
            
            var itemToAdd = ItemData.GetItemById(itemId);
            if (itemToAdd == null) return;
            
            // 检查物品是否设置为自动出售
            if (character.AutoSellItemIds.Contains(itemId))
            {
                character.Gold += itemToAdd.Value * quantity;
                return;
            }
            
            // 尝试堆叠物品
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
            
            // 查找空槽位放入物品
            var emptySlot = character.Inventory.FirstOrDefault(s => s.IsEmpty);
            if (emptySlot != null)
            {
                emptySlot.ItemId = itemId;
                emptySlot.Quantity = quantity;
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 从角色背包移除物品
        /// </summary>
        /// <returns>是否成功移除至少一个物品</returns>
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
        /// 简化版移除物品方法，无需输出参数
        /// </summary>
        public void RemoveItem(Player character, string itemId, int quantity)
        {
            if (character == null) return;
            
            int quantityToRemove = quantity;
            
            // 从后往前遍历，这样即使移除了一个堆叠，索引也不会出错
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
        /// 装备物品
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

            // 1. 优先寻找一个空的可用槽位
            int emptySlotIndex = targetSlots.FindIndex(slot => !character.EquippedItems.ContainsKey(slot));
            
            EquipmentSlot? finalSlot;
            if (emptySlotIndex != -1)
            {
                // 如果找到了空位，就用它
                finalSlot = targetSlots[emptySlotIndex];
            }
            else if (targetSlots.Any())
            {
                // 如果没有空位，但至少有一个目标槽位，则准备替换第一个
                finalSlot = targetSlots.First();
            }
            else
            {
                // 如果连目标槽位都没有（不应该发生），则无法装备
                finalSlot = null;
            }

            // 如果最终没有找到可装备的槽位，则直接返回
            if (finalSlot == null) return;

            // 如果最终确定的槽位上已经有装备，则先卸下它
            if (character.EquippedItems.TryGetValue(finalSlot.Value, out var currentItemId))
            {
                UnequipItem(character, finalSlot.Value);
            }

            // 从背包中减少物品数量
            slotToEquipFrom.Quantity--;
            if (slotToEquipFrom.Quantity <= 0) 
                slotToEquipFrom.ItemId = null;

            // 将新物品装备到最终确定的槽位上
            character.EquippedItems[finalSlot.Value] = itemId;
            character.Health = Math.Min(character.Health, character.GetTotalMaxHealth());
            NotifyStateChanged();
        }

        /// <summary>
        /// 卸下装备
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
        /// 出售物品
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
        /// 使用物品
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
        /// 购买物品
        /// </summary>
        public bool BuyItem(Player character, string itemId)
        {
            if (character == null) return false;
            
            // 查找物品及其商店信息
            var itemToBuy = ItemData.GetItemById(itemId);
            if (itemToBuy?.ShopPurchaseInfo == null)
                return false; // 物品不存在或不可购买
            
            var purchaseInfo = itemToBuy.ShopPurchaseInfo;

            // 检查货币是否足够
            if (purchaseInfo.Currency == CurrencyType.Gold)
            {
                if (character.Gold < purchaseInfo.Price)
                    return false; // 金币不足
            }
            else // 如果货币是物品
            {
                int ownedAmount = character.Inventory
                    .Where(s => s.ItemId == purchaseInfo.CurrencyItemId)
                    .Sum(s => s.Quantity);
                    
                if (ownedAmount < purchaseInfo.Price)
                    return false; // 物品货币不足
            }

            // 扣除花费
            if (purchaseInfo.Currency == CurrencyType.Gold)
            {
                character.Gold -= purchaseInfo.Price;
            }
            else
            {
                RemoveItem(character, purchaseInfo.CurrencyItemId!, purchaseInfo.Price);
            }

            // 将购买的物品添加到背包
            AddItemToInventory(character, itemToBuy.Id, 1);
            return true;
        }

        /// <summary>
        /// 设置快速栏物品
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

            // 如果该物品已在其他快捷栏，则移除
            var otherItemSlots = targetSlots.Where(kv => kv.Value == itemId && kv.Key != slotId).ToList();
            foreach (var otherSlot in otherItemSlots)
            {
                targetSlots.Remove(otherSlot.Key);
            }

            // 设置新的快捷栏
            targetSlots[slotId] = itemId;
            NotifyStateChanged();
        }

        /// <summary>
        /// 清除快速栏物品
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
        /// 切换自动出售物品设置
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
        /// 检查角色是否能负担某个配方的材料
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
        /// 应用消耗品效果
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
                        // 将配方ID添加到玩家的已学习列表中
                        character.LearnedRecipeIds.Add(consumable.RecipeIdToLearn);
                    }
                    break;
            }
            
            character.ConsumableCooldowns[consumable.Id] = consumable.CooldownSeconds;
            NotifyStateChanged();
        }

        /// <summary>
        /// 处理自动消耗品使用
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
        /// 更新消耗品冷却时间
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
        /// 触发状态变更事件
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}