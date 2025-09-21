using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using BlazorWebGame.Models;
using BlazorWebGame.Utils;

namespace BlazorWebGame.Services;

public class GameStateService : IAsyncDisposable
{
    private readonly GameStorage _gameStorage;
    private System.Timers.Timer? _gameLoopTimer;
    private const int GameLoopIntervalMs = 100;
    private const double RevivalDuration = 2;

    private double _playerAttackCooldown = 0;
    private double _enemyAttackCooldown = 0;
    private double _gatheringCooldown = 0;
    private double _craftingCooldown = 0;

    // --- 新增字段 ---
    private Enemy? _lastEnemyBeforeDeath;

    public Player Player { get; private set; } = new();
    public Enemy? CurrentEnemy { get; private set; }
    public GatheringNode? CurrentGatheringNode { get; private set; }
    public Recipe? CurrentRecipe { get; private set; }
    public bool IsPlayerDead { get; private set; } = false;
    public double RevivalTimeRemaining { get; private set; } = 0;

    public double PlayerAttackProgress => GetAttackProgress(_playerAttackCooldown, Player.AttacksPerSecond);
    public double EnemyAttackProgress => CurrentEnemy == null ? 0 : GetAttackProgress(_enemyAttackCooldown, CurrentEnemy.AttacksPerSecond);
    public double GatheringProgress => CurrentGatheringNode == null ? 0 : GetAttackProgress(_gatheringCooldown, 1.0 / GetCurrentGatheringTime());
    public double CraftingProgress => CurrentRecipe == null ? 0 : GetAttackProgress(_craftingCooldown, 1.0 / GetCurrentCraftingTime());

    public List<Enemy> AvailableMonsters => MonsterTemplates.All;
    public List<GatheringNode> AvailableGatheringNodes => GatheringData.AllNodes;
    public const int MaxEquippedSkills = 4;

    public event Action? OnStateChanged;

    public GameStateService(GameStorage gameStorage) => _gameStorage = gameStorage;

    public async Task InitializeAsync()
    {
        var loadedPlayer = await _gameStorage.LoadPlayerAsync();
        if (loadedPlayer != null)
        {
            Player = loadedPlayer;
            Player.EnsureDataConsistency();
        }
        InitializePlayerState();
        _gameLoopTimer = new System.Timers.Timer(GameLoopIntervalMs);
        _gameLoopTimer.Elapsed += GameLoopTick;
        _gameLoopTimer.AutoReset = true;
        _gameLoopTimer.Start();
    }

    private void GameLoopTick(object? sender, ElapsedEventArgs e)
    {
        double elapsedSeconds = GameLoopIntervalMs / 1000.0;
        UpdateBuffs(elapsedSeconds);
        UpdateConsumableCooldowns(elapsedSeconds);

        if (IsPlayerDead)
        {
            RevivalTimeRemaining -= elapsedSeconds;
            if (RevivalTimeRemaining <= 0) RevivePlayer();
            NotifyStateChanged();
            return;
        }

        switch (Player.CurrentAction)
        {
            case PlayerActionState.Combat: ProcessCombat(elapsedSeconds); break;
            case PlayerActionState.Gathering: ProcessGathering(elapsedSeconds); break;
            case PlayerActionState.Crafting: ProcessCrafting(elapsedSeconds); break; // 新增
        }

        ProcessAutoConsumables();
        NotifyStateChanged();
    }

    // --- HandlePlayerDeath 方法修改 ---
    private void HandlePlayerDeath()
    {
        if (Player == null) return;

        // 保存死亡前正在战斗的怪物
        _lastEnemyBeforeDeath = CurrentEnemy;

        IsPlayerDead = true;
        Player.Health = 0;
        RevivalTimeRemaining = RevivalDuration;
        StopCurrentAction(); // 此处调用时 CurrentEnemy 会被设为 null

        // 移除药水等非食物Buff
        Player.ActiveBuffs.RemoveAll(buff =>
        {
            var item = ItemData.GetItemById(buff.SourceItemId);
            if (item is Consumable consumable)
            {
                return consumable.Category != ConsumableCategory.Food;
            }
            return true;
        });
    }

    // --- RevivePlayer 方法修改 ---
    private void RevivePlayer()
    {
        IsPlayerDead = false;
        if (Player != null)
        {
            Player.Health = Player.GetTotalMaxHealth();
        }
        RevivalTimeRemaining = 0;

        // 如果死亡前有正在战斗的怪物，则自动重新开始战斗
        if (_lastEnemyBeforeDeath != null)
        {
            StartCombat(_lastEnemyBeforeDeath);
            _lastEnemyBeforeDeath = null; // 清除记录
        }
        else
        {
            // 如果没有，则正常初始化玩家状态
            InitializePlayerState();
        }
    }

    private void ProcessAutoConsumables()
    {
        if (Player == null) return;
        var allQuickSlotItems = Player.PotionQuickSlots
            .Concat(Player.CombatFoodQuickSlots)
            .Concat(Player.GatheringFoodQuickSlots)
            .Concat(Player.ProductionFoodQuickSlots);
        foreach (var slot in allQuickSlotItems)
        {
            var itemId = slot.Value;
            if (string.IsNullOrEmpty(itemId) || Player.ConsumableCooldowns.ContainsKey(itemId)) continue;
            if (ItemData.GetItemById(itemId) is not Consumable item) continue;
            bool shouldUse = false;
            switch (item.Category)
            {
                case ConsumableCategory.Potion:
                    if ((double)Player.Health / Player.GetTotalMaxHealth() < 0.7) shouldUse = true;
                    break;
                case ConsumableCategory.Food:
                    if (item.FoodType == FoodType.Combat && Player.CurrentAction == PlayerActionState.Combat ||
                        item.FoodType == FoodType.Gathering && Player.CurrentAction == PlayerActionState.Gathering||
                        item.FoodType == FoodType.Production && Player.CurrentAction == PlayerActionState.Crafting)
                    {
                        if (!Player.ActiveBuffs.Any(b => b.BuffType == item.BuffType)) shouldUse = true;
                    }
                    break;
            }
            if (shouldUse && RemoveItemFromInventory(itemId, 1, out int removedCount) && removedCount > 0)
            {
                ApplyConsumableEffect(item);
            }
        }
    }

    private void ApplyConsumableEffect(Consumable consumable)
    {
        switch (consumable.Effect)
        {
            case ConsumableEffectType.Heal:
                Player.Health = Math.Min(Player.GetTotalMaxHealth(), Player.Health + (int)consumable.EffectValue);
                break;
            case ConsumableEffectType.StatBuff:
                if (consumable.BuffType.HasValue && consumable.DurationSeconds.HasValue)
                {
                    Player.ActiveBuffs.RemoveAll(b => b.FoodType == consumable.FoodType && b.BuffType == consumable.BuffType.Value);
                    Player.ActiveBuffs.Add(new Buff
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
                    Player.LearnedRecipeIds.Add(consumable.RecipeIdToLearn);
                }
                break;
        }
        Player.ConsumableCooldowns[consumable.Id] = consumable.CooldownSeconds;
        NotifyStateChanged();
    }

    public void SetQuickSlotItem(ConsumableCategory category, int slotId, string itemId)
    {
        if (Player == null) return;
        var item = ItemData.GetItemById(itemId) as Consumable;
        if (item == null || item.Category != category) return;
        Dictionary<int, string>? targetSlots = category switch
        {
            ConsumableCategory.Potion => Player.PotionQuickSlots,
            ConsumableCategory.Food when item.FoodType == FoodType.Combat => Player.CombatFoodQuickSlots,
            ConsumableCategory.Food when item.FoodType == FoodType.Gathering => Player.GatheringFoodQuickSlots,
            ConsumableCategory.Food when item.FoodType == FoodType.Production => Player.ProductionFoodQuickSlots,
            _ => null
        };
        if (targetSlots == null) return;
        var otherItemSlots = targetSlots.Where(kv => kv.Value == itemId && kv.Key != slotId).ToList();
        foreach (var otherSlot in otherItemSlots) targetSlots.Remove(otherSlot.Key);
        targetSlots[slotId] = itemId;
        NotifyStateChanged();
    }

    public void ClearQuickSlotItem(ConsumableCategory category, int slotId, FoodType foodType = FoodType.None)
    {
        if (Player == null) return;
        Dictionary<int, string>? targetSlots = category switch
        {
            ConsumableCategory.Potion => Player.PotionQuickSlots,
            ConsumableCategory.Food when foodType == FoodType.Combat => Player.CombatFoodQuickSlots,
            ConsumableCategory.Food when foodType == FoodType.Gathering => Player.GatheringFoodQuickSlots,
            ConsumableCategory.Food when foodType == FoodType.Production => Player.ProductionFoodQuickSlots,
            _ => null
        };
        targetSlots?.Remove(slotId);
        NotifyStateChanged();
    }

    public double GetCurrentGatheringTime()
    {
        if (CurrentGatheringNode == null) return 0;
        double speedBonus = Player.GetTotalGatheringSpeedBonus();
        return CurrentGatheringNode.GatheringTimeSeconds / (1 + speedBonus);
    }

    /// <summary>
    /// 获取当前配方的实际制作时间（秒），会计算玩家的速度加成
    /// </summary>
    public double GetCurrentCraftingTime()
    {
        if (CurrentRecipe == null) return 0;

        // 获取玩家的总制作速度加成 (例如 0.15 代表 +15%)
        double speedBonus = Player.GetTotalCraftingSpeedBonus();

        // 基础时间 / (1 + 加成比例)
        return CurrentRecipe.CraftingTimeSeconds / (1 + speedBonus);
    }

    private void ProcessGathering(double elapsedSeconds)
    {
        if (Player != null && CurrentGatheringNode != null)
        {
            _gatheringCooldown -= elapsedSeconds;
            if (_gatheringCooldown <= 0)
            {
                AddItemToInventory(CurrentGatheringNode.ResultingItemId, CurrentGatheringNode.ResultingItemQuantity);
                double extraLootChance = Player.GetTotalExtraLootChance();
                if (extraLootChance > 0 && new Random().NextDouble() < extraLootChance)
                {
                    AddItemToInventory(CurrentGatheringNode.ResultingItemId, CurrentGatheringNode.ResultingItemQuantity);
                }
                Player.AddGatheringXP(CurrentGatheringNode.RequiredProfession, CurrentGatheringNode.XpReward);
                _gatheringCooldown += GetCurrentGatheringTime();
            }
        }
    }

    // 新的生产处理方法
    private void ProcessCrafting(double elapsedSeconds)
    {
        if (Player != null && CurrentRecipe != null)
        {
            _craftingCooldown -= elapsedSeconds;
            if (_craftingCooldown <= 0)
            {
                // 生产完成
                AddItemToInventory(CurrentRecipe.ResultingItemId, CurrentRecipe.ResultingItemQuantity);
                Player.ProductionProfessionXP[CurrentRecipe.RequiredProfession] =
                    Player.ProductionProfessionXP.GetValueOrDefault(CurrentRecipe.RequiredProfession) + CurrentRecipe.XpReward;

                if (CanAffordRecipe(CurrentRecipe))
                {
                    // 自动开始下一次
                    StartCrafting(CurrentRecipe);
                }
                else
                {
                    StopCurrentAction();
                }
            }
        }
    }

    // 开始生产的方法
    public void StartCrafting(Recipe recipe)
    {
        if (!CanAffordRecipe(recipe)) return;

        StopCurrentAction();

        // 扣除材料
        foreach (var ingredient in recipe.Ingredients)
        {
            RemoveItemFromInventory(ingredient.Key, ingredient.Value, out _);
        }

        Player.CurrentAction = PlayerActionState.Crafting;
        CurrentRecipe = recipe;
        // *** 这是修正点：使用新的方法来计算初始冷却时间 ***
        _craftingCooldown = GetCurrentCraftingTime();
        NotifyStateChanged();
    }

    // 检查材料是否足够的方法
    public bool CanAffordRecipe(Recipe recipe)
    {
        if (Player == null) return false;
        foreach (var ingredient in recipe.Ingredients)
        {
            var ownedCount = Player.Inventory.Where(s => s.ItemId == ingredient.Key).Sum(s => s.Quantity);
            if (ownedCount < ingredient.Value)
            {
                return false;
            }
        }
        return true;
    }

    public void StartGathering(GatheringNode node)
    {
        if (Player.CurrentAction == PlayerActionState.Gathering && CurrentGatheringNode?.Id == node.Id) return;
        StopCurrentAction();
        Player.CurrentAction = PlayerActionState.Gathering;
        CurrentGatheringNode = node;
        _gatheringCooldown = GetCurrentGatheringTime();
    }

    // --- 以下是未作修改的既有方法，保持原样即可 ---
    private void ProcessCombat(double elapsedSeconds) { if (Player != null && CurrentEnemy != null) { _playerAttackCooldown -= elapsedSeconds; if (_playerAttackCooldown <= 0) { PlayerAttackEnemy(); _playerAttackCooldown += 1.0 / Player.AttacksPerSecond; } _enemyAttackCooldown -= elapsedSeconds; if (_enemyAttackCooldown <= 0) { EnemyAttackPlayer(); _enemyAttackCooldown += 1.0 / CurrentEnemy.AttacksPerSecond; } } }
    private void PlayerAttackEnemy() { if (Player == null || CurrentEnemy == null) return; var equippedSkillIds = Player.EquippedSkills[Player.SelectedBattleProfession]; foreach (var skillId in equippedSkillIds) { var currentCooldown = Player.SkillCooldowns.GetValueOrDefault(skillId); if (currentCooldown <= 0) { var skill = SkillData.GetSkillById(skillId); if (skill != null) { ApplySkillEffect(skill, isPlayerSkill: true); Player.SkillCooldowns[skillId] = skill.CooldownRounds; } } else { Player.SkillCooldowns[skillId]--; } } CurrentEnemy.Health -= Player.GetTotalAttackPower(); if (CurrentEnemy.Health <= 0) { Player.Gold += CurrentEnemy.GetGoldDropAmount(); var random = new Random(); foreach (var lootItem in CurrentEnemy.LootTable) { if (random.NextDouble() <= lootItem.Value) { AddItemToInventory(lootItem.Key, 1); } } var profession = Player.SelectedBattleProfession; var oldLevel = Player.GetLevel(profession); Player.AddBattleXP(profession, CurrentEnemy.XpReward); var newLevel = Player.GetLevel(profession); if (newLevel > oldLevel) { CheckForNewSkillUnlocks(profession, newLevel); } Player.DefeatedMonsterIds.Add(CurrentEnemy.Name); SpawnNewEnemy(CurrentEnemy); } }
    public void StartCombat(Enemy enemyTemplate) { if (Player.CurrentAction == PlayerActionState.Combat && CurrentEnemy?.Name == enemyTemplate.Name) return; StopCurrentAction(); Player.CurrentAction = PlayerActionState.Combat; SpawnNewEnemy(enemyTemplate); }
    public void StopCurrentAction()
    {
        Player.CurrentAction = PlayerActionState.Idle;
        CurrentEnemy = null;
        CurrentGatheringNode = null;
        CurrentRecipe = null; // 新增
        _playerAttackCooldown = 0;
        _enemyAttackCooldown = 0;
        _gatheringCooldown = 0;
        _craftingCooldown = 0; // 新增
        NotifyStateChanged();
    }
    private void SpawnNewEnemy(Enemy enemyTemplate) { var originalTemplate = AvailableMonsters.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate; CurrentEnemy = originalTemplate.Clone(); CurrentEnemy.SkillCooldowns.Clear(); foreach (var skillId in CurrentEnemy.SkillIds) { var skill = SkillData.GetSkillById(skillId); if (skill != null) { CurrentEnemy.SkillCooldowns[skillId] = skill.InitialCooldownRounds; } } if (CurrentEnemy != null) { _enemyAttackCooldown = 1.0 / CurrentEnemy.AttacksPerSecond; } NotifyStateChanged(); }
    private void UpdateBuffs(double elapsedSeconds) { if (Player == null || !Player.ActiveBuffs.Any()) return; bool buffsChanged = false; for (int i = Player.ActiveBuffs.Count - 1; i >= 0; i--) { var buff = Player.ActiveBuffs[i]; buff.TimeRemainingSeconds -= elapsedSeconds; if (buff.TimeRemainingSeconds <= 0) { Player.ActiveBuffs.RemoveAt(i); buffsChanged = true; } } if (buffsChanged) { Player.Health = Math.Min(Player.Health, Player.GetTotalMaxHealth()); } }
    private void UpdateConsumableCooldowns(double elapsedSeconds) { if (Player == null || !Player.ConsumableCooldowns.Any()) return; var keys = Player.ConsumableCooldowns.Keys.ToList(); foreach (var key in keys) { Player.ConsumableCooldowns[key] -= elapsedSeconds; if (Player.ConsumableCooldowns[key] <= 0) { Player.ConsumableCooldowns.Remove(key); } } }
    private double GetAttackProgress(double currentCooldown, double attacksPerSecond) { if (attacksPerSecond <= 0) return 0; var totalCooldown = 1.0 / attacksPerSecond; var progress = (totalCooldown - currentCooldown) / totalCooldown; return Math.Clamp(progress * 100, 0, 100); }
    private void EnemyAttackPlayer() { if (Player == null || CurrentEnemy == null) return; foreach (var skillId in CurrentEnemy.SkillIds) { var currentCooldown = CurrentEnemy.SkillCooldowns.GetValueOrDefault(skillId); if (currentCooldown <= 0) { var skill = SkillData.GetSkillById(skillId); if (skill != null) { ApplySkillEffect(skill, isPlayerSkill: false); CurrentEnemy.SkillCooldowns[skillId] = skill.CooldownRounds; } } else { CurrentEnemy.SkillCooldowns[skillId]--; } } Player.Health -= CurrentEnemy.AttackPower; if (Player.Health <= 0) { HandlePlayerDeath(); } }
    private void ApplySkillEffect(Skill skill, bool isPlayerSkill) { if (Player == null || CurrentEnemy == null) return; var caster = isPlayerSkill ? (object)Player : CurrentEnemy; var target = isPlayerSkill ? (object)CurrentEnemy : Player; switch (skill.EffectType) { case SkillEffectType.DirectDamage: if (target is Player p) p.Health -= (int)skill.EffectValue; if (target is Enemy e) e.Health -= (int)skill.EffectValue; break; case SkillEffectType.Heal: if (caster is Player pCaster) { var healAmount = skill.EffectValue < 1.0 ? (int)(pCaster.GetTotalMaxHealth() * skill.EffectValue) : (int)skill.EffectValue; pCaster.Health = Math.Min(pCaster.GetTotalMaxHealth(), pCaster.Health + healAmount); } if (caster is Enemy eCaster) { var healAmount = skill.EffectValue < 1.0 ? (int)(eCaster.MaxHealth * skill.EffectValue) : (int)skill.EffectValue; eCaster.Health = Math.Min(eCaster.MaxHealth, eCaster.Health + healAmount); } break; } }
    private void InitializePlayerState() { if (Player == null) return; foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession))) { var currentLevel = Player.GetLevel(profession); CheckForNewSkillUnlocks(profession, currentLevel, true); } ResetPlayerSkillCooldowns(); NotifyStateChanged(); }
    private void CheckForNewSkillUnlocks(BattleProfession profession, int level, bool checkAllLevels = false) { if (Player == null) return; var skillsToLearnQuery = SkillData.AllSkills.Where(s => s.RequiredProfession == profession); if (checkAllLevels) { skillsToLearnQuery = skillsToLearnQuery.Where(s => s.RequiredLevel <= level); } else { skillsToLearnQuery = skillsToLearnQuery.Where(s => s.RequiredLevel == level); } var newlyLearnedSkills = skillsToLearnQuery.ToList(); foreach (var skill in newlyLearnedSkills) { if (skill.Type == SkillType.Shared) { Player.LearnedSharedSkills.Add(skill.Id); } if (skill.Type == SkillType.Fixed) { if (!Player.EquippedSkills.TryGetValue(profession, out var equipped) || !equipped.Contains(skill.Id)) { Player.EquippedSkills[profession].Insert(0, skill.Id); } } } }
    public void AddItemToInventory(string itemId, int quantity) { if (Player == null) return; var itemToAdd = ItemData.GetItemById(itemId); if (itemToAdd == null) return; if (Player.AutoSellItemIds.Contains(itemId)) { Player.Gold += itemToAdd.Value * quantity; NotifyStateChanged(); return; } if (itemToAdd.IsStackable) { var existingSlot = Player.Inventory.FirstOrDefault(s => s.ItemId == itemId && s.Quantity < 99); if (existingSlot != null) { existingSlot.Quantity += quantity; NotifyStateChanged(); return; } } var emptySlot = Player.Inventory.FirstOrDefault(s => s.IsEmpty); if (emptySlot != null) { emptySlot.ItemId = itemId; emptySlot.Quantity = quantity; } NotifyStateChanged(); }
    public void EquipItem(string itemId) { if (Player == null) return; var slotToEquipFrom = Player.Inventory.FirstOrDefault(s => s.ItemId == itemId); if (slotToEquipFrom == null) return; if (ItemData.GetItemById(itemId) is not Equipment equipmentToEquip) return; if (Player.EquippedItems.TryGetValue(equipmentToEquip.Slot, out var currentItemId)) { UnequipItem(equipmentToEquip.Slot); } slotToEquipFrom.Quantity--; if (slotToEquipFrom.Quantity <= 0) { slotToEquipFrom.ItemId = null; } Player.EquippedItems[equipmentToEquip.Slot] = itemId; Player.Health = Math.Min(Player.Health, Player.GetTotalMaxHealth()); NotifyStateChanged(); }
    public void UnequipItem(EquipmentSlot slot) { if (Player == null) return; if (!Player.EquippedItems.TryGetValue(slot, out var itemIdToUnequip)) return; Player.EquippedItems.Remove(slot); AddItemToInventory(itemIdToUnequip, 1); Player.Health = Math.Min(Player.Health, Player.GetTotalMaxHealth()); NotifyStateChanged(); }
    public void SellItem(string itemId, int quantity = 1) { if (Player == null) return; var itemData = ItemData.GetItemById(itemId); if (itemData == null) return; RemoveItemFromInventory(itemId, quantity, out int soldCount); if (soldCount > 0) { Player.Gold += itemData.Value * soldCount; NotifyStateChanged(); } }
    public bool BuyItem(string itemId) { if (Player == null) return false; var itemToBuy = ItemData.GetItemById(itemId); var purchaseInfo = itemToBuy?.ShopPurchaseInfo; if (purchaseInfo == null) return false; bool canAfford = false; switch (purchaseInfo.Currency) { case CurrencyType.Gold: if (Player.Gold >= purchaseInfo.Price) { Player.Gold -= purchaseInfo.Price; canAfford = true; } break; case CurrencyType.Item: if (!string.IsNullOrEmpty(purchaseInfo.CurrencyItemId)) { int ownedAmount = Player.Inventory.Where(s => s.ItemId == purchaseInfo.CurrencyItemId).Sum(s => s.Quantity); if (ownedAmount >= purchaseInfo.Price) { RemoveItemFromInventory(purchaseInfo.CurrencyItemId, purchaseInfo.Price, out _); canAfford = true; } } break; } if (canAfford) { AddItemToInventory(itemId, 1); NotifyStateChanged(); return true; } return false; }
    private bool RemoveItemFromInventory(string itemId, int quantityToRemove, out int actuallyRemoved) { actuallyRemoved = 0; if (Player == null) return false; for (int i = Player.Inventory.Count - 1; i >= 0; i--) { var slot = Player.Inventory[i]; if (slot.ItemId == itemId) { int amountToRemoveFromSlot = Math.Min(quantityToRemove - actuallyRemoved, slot.Quantity); slot.Quantity -= amountToRemoveFromSlot; actuallyRemoved += amountToRemoveFromSlot; if (slot.Quantity <= 0) { slot.ItemId = null; } if (actuallyRemoved >= quantityToRemove) { break; } } } return actuallyRemoved > 0; }
    public void UseItem(string itemId)
    {
        if (Player == null) return;
        if (ItemData.GetItemById(itemId) is not Consumable consumable) return;

        // 对于需要冷却的物品，检查冷却时间
        if (consumable.CooldownSeconds > 0 && Player.ConsumableCooldowns.ContainsKey(consumable.Id))
        {
            // 可以在这里添加一个UI反馈，告诉玩家物品在冷却中
            return;
        }

        // 尝试从背包移除物品
        if (RemoveItemFromInventory(itemId, 1, out int removedCount) && removedCount > 0)
        {
            // 应用物品效果
            ApplyConsumableEffect(consumable);
        }
    }
    public void ToggleAutoSellItem(string itemId) { if (Player == null) return; if (Player.AutoSellItemIds.Contains(itemId)) { Player.AutoSellItemIds.Remove(itemId); } else { Player.AutoSellItemIds.Add(itemId); } NotifyStateChanged(); }
    public void SetBattleProfession(BattleProfession profession) { if (Player != null) { Player.SelectedBattleProfession = profession; NotifyStateChanged(); } }
    public void EquipSkill(string skillId) { if (Player == null) return; var profession = Player.SelectedBattleProfession; var equipped = Player.EquippedSkills[profession]; var skill = SkillData.GetSkillById(skillId); if (skill == null || skill.Type == SkillType.Fixed) return; if (equipped.Contains(skillId)) return; var currentSelectableSkills = equipped.Count(id => SkillData.GetSkillById(id)?.Type != SkillType.Fixed); if (currentSelectableSkills < MaxEquippedSkills) { equipped.Add(skillId); Player.SkillCooldowns[skillId] = skill.InitialCooldownRounds; NotifyStateChanged(); } }
    public void UnequipSkill(string skillId) { if (Player == null) return; var profession = Player.SelectedBattleProfession; var skill = SkillData.GetSkillById(skillId); if (skill == null || skill.Type == SkillType.Fixed) return; if (Player.EquippedSkills[profession].Remove(skillId)) { Player.SkillCooldowns.Remove(skillId); NotifyStateChanged(); } }
    private void ResetPlayerSkillCooldowns() { if (Player == null) return; Player.SkillCooldowns.Clear(); foreach (var skillId in Player.EquippedSkills.Values.SelectMany(s => s)) { var skill = SkillData.GetSkillById(skillId); if (skill != null) { Player.SkillCooldowns[skillId] = skill.InitialCooldownRounds; } } }
    public async Task SaveStateAsync() { if (Player != null) { await _gameStorage.SavePlayerAsync(Player); } }
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
    public async ValueTask DisposeAsync() { if (_gameLoopTimer != null) { _gameLoopTimer.Stop(); _gameLoopTimer.Dispose(); } await SaveStateAsync(); }
}