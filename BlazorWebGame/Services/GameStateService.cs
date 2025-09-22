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
    private readonly QuestService _questService; // <-- 新增：注入QuestService
    private System.Timers.Timer? _gameLoopTimer;
    private const int GameLoopIntervalMs = 100;
    private const double RevivalDuration = 2;

    private double _playerAttackCooldown = 0;
    private double _enemyAttackCooldown = 0;
    private double _gatheringCooldown = 0;
    private double _craftingCooldown = 0;

    // --- 新增字段 ---
    private Enemy? _lastEnemyBeforeDeath;
    public List<Quest> DailyQuests { get; private set; } = new();
    public List<Quest> WeeklyQuests { get; private set; } = new();
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

    public GameStateService(GameStorage gameStorage, QuestService questService)
    {
        _gameStorage = gameStorage;
        _questService = questService; // <-- 依赖注入
    }

    public async Task InitializeAsync()
    {
        var loadedPlayer = await _gameStorage.LoadPlayerAsync();
        if (loadedPlayer != null)
        {
            Player = loadedPlayer;
            Player.EnsureDataConsistency();
        }

        // --- vvv 新增任务初始化逻辑 vvv ---
        DailyQuests = _questService.GetDailyQuests();
        WeeklyQuests = _questService.GetWeeklyQuests();
        // 这里可以添加一个逻辑，用于每日/每周重置任务进度和 `CompletedQuestIds`
        // --- ^^^ 新增结束 ^^^ ---

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
                UpdateQuestProgress(QuestType.GatherItem, CurrentGatheringNode.ResultingItemId, CurrentGatheringNode.ResultingItemQuantity);
                UpdateQuestProgress(QuestType.GatherItem, "any", 1); // 为周常任务的"任意采集"计数
                _gatheringCooldown += GetCurrentGatheringTime();
            }
        }
    }

    private void ProcessCrafting(double elapsedSeconds)
    {
        if (Player != null && CurrentRecipe != null)
        {
            _craftingCooldown -= elapsedSeconds;
            if (_craftingCooldown <= 0)
            {
                // 生产完成，先给予产出
                AddItemToInventory(CurrentRecipe.ResultingItemId, CurrentRecipe.ResultingItemQuantity);
                Player.AddProductionXP(CurrentRecipe.RequiredProfession, CurrentRecipe.XpReward);

                UpdateQuestProgress(QuestType.CraftItem, CurrentRecipe.ResultingItemId, CurrentRecipe.ResultingItemQuantity);
                UpdateQuestProgress(QuestType.CraftItem, "any", 1); // 为周常任务的"任意制作"计数

                // *** 核心修正点：在这里补上为“刚刚完成的这次制作”扣除材料的逻辑 ***
                foreach (var ingredient in CurrentRecipe.Ingredients)
                {
                    RemoveItemFromInventory(ingredient.Key, ingredient.Value, out _);
                }

                // 检查是否还有足够的材料开始下一次循环
                if (CanAffordRecipe(CurrentRecipe))
                {
                    // 重置冷却时间，继续下一次制作
                    _craftingCooldown += GetCurrentCraftingTime();
                }
                else
                {
                    // 材料不足，停止所有生产活动
                    StopCurrentAction();
                }
            }
        }
    }

    public void StartCrafting(Recipe recipe)
    {
        // 检查是否能负担得起至少一次制作
        if (!CanAffordRecipe(recipe)) return;

        // 停止当前任何活动，为开始生产做准备
        StopCurrentAction();

        // **重要**：这里不再预先扣除材料
        // 旧的扣材料逻辑 `foreach (...)` 已被移除

        // 设置玩家状态为制作中
        Player.CurrentAction = PlayerActionState.Crafting;
        CurrentRecipe = recipe;

        // 设置第一次制作的冷却/读条时间
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
    private void PlayerAttackEnemy()
    {
        if (Player == null || CurrentEnemy == null) return;
        var equippedSkillIds = Player.EquippedSkills[Player.SelectedBattleProfession];
        foreach (var skillId in equippedSkillIds) 
        { 
            var currentCooldown = Player.SkillCooldowns.GetValueOrDefault(skillId);
            if (currentCooldown <= 0) 
            {
                var skill = SkillData.GetSkillById(skillId);
                if (skill != null)
                {
                    ApplySkillEffect(skill, isPlayerSkill: true);
                    Player.SkillCooldowns[skillId] = skill.CooldownRounds;
                }
            }
            else
            {
                Player.SkillCooldowns[skillId]--;
            }
        }
        CurrentEnemy.Health -= Player.GetTotalAttackPower();
        if (CurrentEnemy.Health <= 0)
        { 
            Player.Gold += CurrentEnemy.GetGoldDropAmount();
            var random = new Random();
            foreach (var lootItem in CurrentEnemy.LootTable)
            {
                if (random.NextDouble() <= lootItem.Value)
                {
                    AddItemToInventory(lootItem.Key, 1); 
                }
            }
            var profession = Player.SelectedBattleProfession;
            var oldLevel = Player.GetLevel(profession);
            Player.AddBattleXP(profession, CurrentEnemy.XpReward);
            var newLevel = Player.GetLevel(profession);
            if (newLevel > oldLevel) 
            {
                CheckForNewSkillUnlocks(profession, newLevel);
            }
            UpdateQuestProgress(QuestType.KillMonster, CurrentEnemy.Name, 1);
            UpdateQuestProgress(QuestType.KillMonster, "any", 1); // 周常任务不受影响

            Player.DefeatedMonsterIds.Add(CurrentEnemy.Name); SpawnNewEnemy(CurrentEnemy);
        }
    }
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
    public void EquipItem(string itemId)
    {
        if (Player == null) return;
        var slotToEquipFrom = Player.Inventory.FirstOrDefault(s => s.ItemId == itemId);
        if (slotToEquipFrom == null) return;
        if (ItemData.GetItemById(itemId) is not Equipment equipmentToEquip) return;

        // --- vvv 这是新的核心逻辑 vvv ---

        List<EquipmentSlot> targetSlots = new();

        // 根据物品的“标识”槽位，决定它真正可以装备在哪些槽位上
        switch (equipmentToEquip.Slot)
        {
            case EquipmentSlot.Finger1:
            case EquipmentSlot.Finger2:
                targetSlots.Add(EquipmentSlot.Finger1);
                targetSlots.Add(EquipmentSlot.Finger2);
                break;

            case EquipmentSlot.Trinket1:
            case EquipmentSlot.Trinket2:
                targetSlots.Add(EquipmentSlot.Trinket1);
                targetSlots.Add(EquipmentSlot.Trinket2);
                break;

            default:
                // 对于所有其他单槽位物品，逻辑保持不变
                targetSlots.Add(equipmentToEquip.Slot);
                break;
        }

        // 寻找一个可用的目标槽位
        EquipmentSlot? finalSlot = null;
        // 1. 优先寻找空槽位
        foreach (var slot in targetSlots)
        {
            if (!Player.EquippedItems.ContainsKey(slot))
            {
                finalSlot = slot;
                break;
            }
        }

        // 2. 如果没有空槽位，默认替换第一个槽位
        if (finalSlot == null && targetSlots.Any())
        {
            finalSlot = targetSlots.First();
        }

        // 如果最终没有找到可装备的槽位，则直接返回
        if (finalSlot == null) return;

        // --- ^^^ 新逻辑结束 ^^^ ---

        // 如果最终确定的槽位上已经有装备，则先卸下它
        if (Player.EquippedItems.TryGetValue(finalSlot.Value, out var currentItemId))
        {
            UnequipItem(finalSlot.Value);
        }

        // 从背包中减少物品数量
        slotToEquipFrom.Quantity--;
        if (slotToEquipFrom.Quantity <= 0)
        {
            slotToEquipFrom.ItemId = null;
        }

        // 将新物品装备到最终确定的槽位上
        Player.EquippedItems[finalSlot.Value] = itemId;

        // 更新玩家状态
        Player.Health = Math.Min(Player.Health, Player.GetTotalMaxHealth());
        NotifyStateChanged();
    }
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

    /// <summary>
    /// 核心任务进度更新逻辑
    /// </summary>
    private void UpdateQuestProgress(QuestType type, string targetId, int amount)
    {
        if (Player == null) return;

        // 合并每日和周常任务列表进行检查
        var allQuests = DailyQuests.Concat(WeeklyQuests);

        foreach (var quest in allQuests)
        {
            // 如果任务已完成，则跳过
            if (Player.CompletedQuestIds.Contains(quest.Id)) continue;

            // 检查任务类型和目标ID是否匹配 (支持 "any" 通配符)
            if (quest.Type == type && (quest.TargetId == targetId || quest.TargetId == "any"))
            {
                // 获取或初始化当前进度
                var currentProgress = Player.QuestProgress.GetValueOrDefault(quest.Id, 0);

                // 更新进度，但不超过任务要求上限
                Player.QuestProgress[quest.Id] = Math.Min(currentProgress + amount, quest.RequiredAmount);
            }
        }
    }


    /// <summary>
    /// 尝试完成一个任务
    /// </summary>
    public void TryCompleteQuest(string questId)
    {
        if (Player == null) return;

        var quest = DailyQuests.Concat(WeeklyQuests).FirstOrDefault(q => q.Id == questId);
        if (quest == null || Player.CompletedQuestIds.Contains(questId)) return;

        // 检查进度是否达标
        var currentProgress = Player.QuestProgress.GetValueOrDefault(questId, 0);
        if (currentProgress >= quest.RequiredAmount)
        {
            // 给予奖励
            Player.Gold += quest.GoldReward;
            // Player.AddExperience... (如果需要奖励经验)

            if (quest.ReputationReward > 0)
            {
                Player.Reputation[quest.Faction] = Player.Reputation.GetValueOrDefault(quest.Faction, 0) + quest.ReputationReward;
            }

            foreach (var itemReward in quest.ItemRewards)
            {
                AddItemToInventory(itemReward.Key, itemReward.Value);
            }

            // 标记为已完成
            Player.CompletedQuestIds.Add(questId);

            // (可选) 从进度字典中移除，节省空间
            Player.QuestProgress.Remove(questId);

            NotifyStateChanged();
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