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

    public Player Player { get; private set; } = new();
    public Enemy? CurrentEnemy { get; private set; }
    public bool IsPlayerDead { get; private set; } = false;
    public double RevivalTimeRemaining { get; private set; } = 0;

    public double PlayerAttackProgress => GetAttackProgress(_playerAttackCooldown, Player.AttacksPerSecond);
    public double EnemyAttackProgress => CurrentEnemy == null ? 0 : GetAttackProgress(_enemyAttackCooldown, CurrentEnemy.AttacksPerSecond);

    public List<Enemy> AvailableMonsters => MonsterTemplates.All;
    public const int MaxEquippedSkills = 4;

    public event Action? OnStateChanged;

    public GameStateService(GameStorage gameStorage)
    {
        _gameStorage = gameStorage;
    }

    public async Task InitializeAsync()
    {
        var loadedPlayer = await _gameStorage.LoadPlayerAsync();
        if (loadedPlayer != null)
        {
            Player = loadedPlayer;
        }

        InitializePlayerState();

        if (CurrentEnemy == null && AvailableMonsters.Any())
        {
            SpawnNewEnemy(AvailableMonsters.First());
        }

        _gameLoopTimer = new System.Timers.Timer(GameLoopIntervalMs);
        _gameLoopTimer.Elapsed += GameLoopTick;
        _gameLoopTimer.AutoReset = true;
        _gameLoopTimer.Start();
    }

    private void GameLoopTick(object? sender, ElapsedEventArgs e)
    {
        double elapsedSeconds = GameLoopIntervalMs / 1000.0;

        if (IsPlayerDead)
        {
            RevivalTimeRemaining -= elapsedSeconds;
            if (RevivalTimeRemaining <= 0)
            {
                RevivePlayer();
            }
            NotifyStateChanged();
            return;
        }

        if (Player != null && CurrentEnemy != null)
        {
            _playerAttackCooldown -= elapsedSeconds;
            if (_playerAttackCooldown <= 0)
            {
                PlayerAttackEnemy();
                _playerAttackCooldown += 1.0 / Player.AttacksPerSecond;
            }

            _enemyAttackCooldown -= elapsedSeconds;
            if (_enemyAttackCooldown <= 0)
            {
                EnemyAttackPlayer();
                _enemyAttackCooldown += 1.0 / CurrentEnemy.AttacksPerSecond;
            }
        }

        NotifyStateChanged();
    }

    private double GetAttackProgress(double currentCooldown, double attacksPerSecond)
    {
        if (attacksPerSecond <= 0) return 0;
        var totalCooldown = 1.0 / attacksPerSecond;
        var progress = (totalCooldown - currentCooldown) / totalCooldown;
        return Math.Clamp(progress * 100, 0, 100);
    }

    private void PlayerAttackEnemy()
    {
        if (Player == null || CurrentEnemy == null) return;

        var equippedSkillIds = Player.EquippedSkills[Player.SelectedBattleProfession];

        // 1. 遍历装备的技能
        foreach (var skillId in equippedSkillIds)
        {
            var currentCooldown = Player.SkillCooldowns.GetValueOrDefault(skillId);

            // 2. 检查技能是否冷却完毕
            if (currentCooldown <= 0)
            {
                //  -> 冷却完毕，释放技能
                var skill = SkillData.GetSkillById(skillId);
                if (skill != null)
                {
                    ApplySkillEffect(skill, isPlayerSkill: true);
                    Player.SkillCooldowns[skillId] = skill.CooldownRounds;
                }
            }
            else
            {
                //  -> 冷却中，冷却时间-1
                Player.SkillCooldowns[skillId]--;
            }
        }

        CurrentEnemy.Health -= Player.GetTotalAttackPower();

        if (CurrentEnemy.Health <= 0)
        {
            // --- 战利品逻辑 ---
            Player.Gold += CurrentEnemy.GetGoldDropAmount();

            // 1. 掉落物品
            var random = new Random();
            foreach (var lootItem in CurrentEnemy.LootTable)
            {
                if (random.NextDouble() <= lootItem.Value) // random.NextDouble() 在 0.0 和 1.0 之间
                {
                    AddItemToInventory(lootItem.Key, 1);
                }
            }

            // 2. 获得经验值
            var profession = Player.SelectedBattleProfession;
            var oldLevel = Player.GetLevel(profession);
            Player.AddBattleXP(profession, CurrentEnemy.XpReward);
            var newLevel = Player.GetLevel(profession);

            if (newLevel > oldLevel)
            {
                CheckForNewSkillUnlocks(profession, newLevel);
            }

            SpawnNewEnemy(CurrentEnemy);
        }
    }

    private void EnemyAttackPlayer()
    {
        if (Player == null || CurrentEnemy == null) return;

        // 1. 遍历怪物的技能
        foreach (var skillId in CurrentEnemy.SkillIds)
        {
            var currentCooldown = CurrentEnemy.SkillCooldowns.GetValueOrDefault(skillId);

            // 2. 检查技能是否冷却完毕
            if (currentCooldown <= 0)
            {
                var skill = SkillData.GetSkillById(skillId);
                if (skill != null)
                {
                    ApplySkillEffect(skill, isPlayerSkill: false);
                    CurrentEnemy.SkillCooldowns[skillId] = skill.CooldownRounds;
                }
            }
            else
            {
                //  -> 冷却中，冷却时间-1
                CurrentEnemy.SkillCooldowns[skillId]--;
            }
        }

        // 3. 执行普通攻击
        Player.Health -= CurrentEnemy.AttackPower;
        if (Player.Health <= 0)
        {
            HandlePlayerDeath();
        }
    }

    private void ApplySkillEffect(Skill skill, bool isPlayerSkill)
    {
        if (Player == null || CurrentEnemy == null) return;

        var caster = isPlayerSkill ? (object)Player : CurrentEnemy;
        var target = isPlayerSkill ? (object)CurrentEnemy : Player;

        switch (skill.EffectType)
        {
            case SkillEffectType.DirectDamage:
                if (target is Player p) p.Health -= (int)skill.EffectValue;
                if (target is Enemy e) e.Health -= (int)skill.EffectValue;
                break;
            case SkillEffectType.Heal:
                if (caster is Player pCaster)
                {
                    var healAmount = skill.EffectValue < 1.0 ? (int)(pCaster.GetTotalMaxHealth() * skill.EffectValue) : (int)skill.EffectValue;
                    pCaster.Health = Math.Min(pCaster.GetTotalMaxHealth(), pCaster.Health + healAmount);
                }
                if (caster is Enemy eCaster)
                {
                    var healAmount = skill.EffectValue < 1.0 ? (int)(eCaster.MaxHealth * skill.EffectValue) : (int)skill.EffectValue;
                    eCaster.Health = Math.Min(eCaster.MaxHealth, eCaster.Health + healAmount);
                }
                break;
        }
    }

    private void InitializePlayerState()
    {
        if (Player == null) return;

        foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
        {
            var currentLevel = Player.GetLevel(profession);
            CheckForNewSkillUnlocks(profession, currentLevel, true);
        }

        ResetPlayerSkillCooldowns();

        NotifyStateChanged();
    }

    private void CheckForNewSkillUnlocks(BattleProfession profession, int level, bool checkAllLevels = false)
    {
        if (Player == null) return;

        var skillsToLearnQuery = SkillData.AllSkills.Where(s => s.RequiredProfession == profession);

        if (checkAllLevels)
        {
            skillsToLearnQuery = skillsToLearnQuery.Where(s => s.RequiredLevel <= level);
        }
        else
        {
            skillsToLearnQuery = skillsToLearnQuery.Where(s => s.RequiredLevel == level);
        }

        var newlyLearnedSkills = skillsToLearnQuery.ToList();

        foreach (var skill in newlyLearnedSkills)
        {
            if (skill.Type == SkillType.Shared)
            {
                Player.LearnedSharedSkills.Add(skill.Id);
            }

            if (skill.Type == SkillType.Fixed)
            {
                if (!Player.EquippedSkills.TryGetValue(profession, out var equipped) || !equipped.Contains(skill.Id))
                {
                    Player.EquippedSkills[profession].Insert(0, skill.Id);
                }
            }
        }
    }

    private void HandlePlayerDeath()
    {
        if (Player == null) return;
        IsPlayerDead = true;
        Player.Health = 0;
        RevivalTimeRemaining = RevivalDuration;
    }

    private void RevivePlayer()
    {
        IsPlayerDead = false;
        if (Player != null)
        {
            Player.Health = Player.GetTotalMaxHealth();
        }
        RevivalTimeRemaining = 0;

        InitializePlayerState();
    }

    public void AddItemToInventory(string itemId, int quantity)
    {
        if (Player == null) return;
        var itemToAdd = ItemData.GetItemById(itemId);
        if (itemToAdd == null) return;

        if (Player.AutoSellItemIds.Contains(itemId))
        {
            Player.Gold += itemToAdd.Value * quantity;
            NotifyStateChanged();
            return;
        }

        if (itemToAdd.IsStackable)
        {
            var existingSlot = Player.Inventory.FirstOrDefault(s => s.ItemId == itemId && s.Quantity < 99);
            if (existingSlot != null)
            {
                existingSlot.Quantity += quantity;
                NotifyStateChanged();
                return;
            }
        }

        var emptySlot = Player.Inventory.FirstOrDefault(s => s.IsEmpty);
        if (emptySlot != null)
        {
            emptySlot.ItemId = itemId;
            emptySlot.Quantity = quantity;
        }
        else
        {
            // 背包满了，暂时不处理
        }
        NotifyStateChanged();
    }

    public void EquipItem(string itemId)
    {
        if (Player == null) return;
        var slotToEquipFrom = Player.Inventory.FirstOrDefault(s => s.ItemId == itemId);
        if (slotToEquipFrom == null) return;

        if (ItemData.GetItemById(itemId) is not Equipment equipmentToEquip) return;

        if (Player.EquippedItems.TryGetValue(equipmentToEquip.Slot, out var currentItemId))
        {
            UnequipItem(equipmentToEquip.Slot);
        }

        slotToEquipFrom.Quantity--;
        if (slotToEquipFrom.Quantity <= 0)
        {
            slotToEquipFrom.ItemId = null;
        }

        Player.EquippedItems[equipmentToEquip.Slot] = itemId;

        Player.Health = Math.Min(Player.Health, Player.GetTotalMaxHealth());
        NotifyStateChanged();
    }

    public void UnequipItem(EquipmentSlot slot)
    {
        if (Player == null) return;
        if (!Player.EquippedItems.TryGetValue(slot, out var itemIdToUnequip)) return;

        Player.EquippedItems.Remove(slot);

        AddItemToInventory(itemIdToUnequip, 1);

        Player.Health = Math.Min(Player.Health, Player.GetTotalMaxHealth());
        NotifyStateChanged();
    }

    public void SellItem(string itemId, int quantity = 1)
    {
        if (Player == null) return;

        var itemData = ItemData.GetItemById(itemId);
        if (itemData == null) return;

        RemoveItemFromInventory(itemId, quantity, out int soldCount);

        if (soldCount > 0)
        {
            Player.Gold += itemData.Value * soldCount;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// 从商店购买物品（重构版）
    /// </summary>
    public bool BuyItem(string itemId)
    {
        if (Player == null) return false;

        var itemToBuy = ItemData.GetItemById(itemId);
        var purchaseInfo = itemToBuy?.ShopPurchaseInfo;
        if (purchaseInfo == null) return false; // 物品不可购买

        // 检查并支付货币
        bool canAfford = false;
        switch (purchaseInfo.Currency)
        {
            case CurrencyType.Gold:
                if (Player.Gold >= purchaseInfo.Price)
                {
                    Player.Gold -= purchaseInfo.Price;
                    canAfford = true;
                }
                break;

            case CurrencyType.Item:
                if (!string.IsNullOrEmpty(purchaseInfo.CurrencyItemId))
                {
                    // 检查玩家是否有足够的物品
                    int ownedAmount = Player.Inventory
                        .Where(s => s.ItemId == purchaseInfo.CurrencyItemId)
                        .Sum(s => s.Quantity);

                    if (ownedAmount >= purchaseInfo.Price)
                    {
                        // 从背包移除作为货币的物品
                        RemoveItemFromInventory(purchaseInfo.CurrencyItemId, purchaseInfo.Price, out _);
                        canAfford = true;
                    }
                }
                break;
        }

        if (canAfford)
        {
            AddItemToInventory(itemId, 1);
            NotifyStateChanged();
            return true;
        }

        return false; // 货币不足
    }

    /// <summary>
    /// 从背包中移除指定数量的物品
    /// </summary>
    private bool RemoveItemFromInventory(string itemId, int quantityToRemove, out int actuallyRemoved)
    {
        actuallyRemoved = 0;
        if (Player == null) return false;

        for (int i = Player.Inventory.Count - 1; i >= 0; i--)
        {
            var slot = Player.Inventory[i];
            if (slot.ItemId == itemId)
            {
                int amountToRemoveFromSlot = Math.Min(quantityToRemove - actuallyRemoved, slot.Quantity);

                slot.Quantity -= amountToRemoveFromSlot;
                actuallyRemoved += amountToRemoveFromSlot;

                if (slot.Quantity <= 0)
                {
                    slot.ItemId = null;
                }

                if (actuallyRemoved >= quantityToRemove)
                {
                    break;
                }
            }
        }

        return actuallyRemoved > 0;
    }

    public void ToggleAutoSellItem(string itemId)
    {
        if (Player == null) return;

        if (Player.AutoSellItemIds.Contains(itemId))
        {
            Player.AutoSellItemIds.Remove(itemId);
        }
        else
        {
            Player.AutoSellItemIds.Add(itemId);
        }
        NotifyStateChanged();
    }

    public void SetBattleProfession(BattleProfession profession)
    {
        if (Player != null)
        {
            Player.SelectedBattleProfession = profession;
            NotifyStateChanged();
        }
    }

    public void SpawnNewEnemy(Enemy enemyTemplate)
    {
        var originalTemplate = AvailableMonsters.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;

        CurrentEnemy = originalTemplate.Clone();

        CurrentEnemy.SkillCooldowns.Clear();
        foreach (var skillId in CurrentEnemy.SkillIds)
        {
            var skill = SkillData.GetSkillById(skillId);
            if (skill != null)
            {
                CurrentEnemy.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
            }
        }

        if (CurrentEnemy != null)
        {
            _enemyAttackCooldown = 1.0 / CurrentEnemy.AttacksPerSecond;
        }
        NotifyStateChanged();
    }

    public void EquipSkill(string skillId)
    {
        if (Player == null) return;
        var profession = Player.SelectedBattleProfession;
        var equipped = Player.EquippedSkills[profession];
        var skill = SkillData.GetSkillById(skillId);

        if (skill == null || skill.Type == SkillType.Fixed) return;
        if (equipped.Contains(skillId)) return;

        var currentSelectableSkills = equipped.Count(id => SkillData.GetSkillById(id)?.Type != SkillType.Fixed);

        if (currentSelectableSkills < MaxEquippedSkills)
        {
            equipped.Add(skillId);
            Player.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
            NotifyStateChanged();
        }
    }

    public void UnequipSkill(string skillId)
    {
        if (Player == null) return;
        var profession = Player.SelectedBattleProfession;
        var skill = SkillData.GetSkillById(skillId);

        if (skill == null || skill.Type == SkillType.Fixed) return;

        if (Player.EquippedSkills[profession].Remove(skillId))
        {
            Player.SkillCooldowns.Remove(skillId);
            NotifyStateChanged();
        }
    }

    private void ResetPlayerSkillCooldowns()
    {
        if (Player == null) return;

        Player.SkillCooldowns.Clear();
        foreach (var skillId in Player.EquippedSkills.Values.SelectMany(s => s))
        {
            var skill = SkillData.GetSkillById(skillId);
            if (skill != null)
            {
                Player.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
            }
        }
    }

    public async Task SaveStateAsync()
    {
        if (Player != null)
        {
            await _gameStorage.SavePlayerAsync(Player);
        }
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    public async ValueTask DisposeAsync()
    {
        if (_gameLoopTimer != null)
        {
            _gameLoopTimer.Stop();
            _gameLoopTimer.Dispose();
        }
        await SaveStateAsync();
    }
}