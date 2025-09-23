using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

namespace BlazorWebGame.Services;

public class GameStateService : IAsyncDisposable
{
    private readonly GameStorage _gameStorage;
    private readonly QuestService _questService;
    private readonly PartyService _partyService;
    private readonly InventoryService _inventoryService;
    private readonly CombatService _combatService;
    private System.Timers.Timer? _gameLoopTimer;
    private const int GameLoopIntervalMs = 100;
    private const double RevivalDuration = 2;

    public List<Player> AllCharacters { get; private set; } = new();
    public Player? ActiveCharacter { get; private set; }
    
    // 移除Parties属性，改为从PartyService获取
    public List<Party> Parties => _partyService.Parties;
    
    public List<Enemy> AvailableMonsters => MonsterTemplates.All;
    public List<GatheringNode> AvailableGatheringNodes => GatheringData.AllNodes;
    public const int MaxEquippedSkills = 4;
    public List<Quest> DailyQuests { get; private set; } = new();
    public List<Quest> WeeklyQuests { get; private set; } = new();

    public event Action? OnStateChanged;

    public GameStateService(
        GameStorage gameStorage,
        QuestService questService,
        PartyService partyService,
        InventoryService inventoryService,
        CombatService combatService)
    {
        _gameStorage = gameStorage;
        _questService = questService;
        _partyService = partyService;
        _inventoryService = inventoryService;
        _combatService = combatService;

        // 订阅各个服务的状态变更事件
        _partyService.OnStateChanged += () => NotifyStateChanged();
        _inventoryService.OnStateChanged += () => NotifyStateChanged();
        _combatService.OnStateChanged += () => NotifyStateChanged();

        // 注册服务到服务定位器（为了让CombatService能访问QuestService）
        ServiceLocator.RegisterService(_questService);
    }

    public async Task InitializeAsync()
    {
        // 现有代码不变
        AllCharacters.Add(new Player { Name = "索拉尔" });
        AllCharacters.Add(new Player { Name = "阿尔特留斯", Gold = 50 });
        
        ActiveCharacter = AllCharacters.FirstOrDefault();

        foreach (var character in AllCharacters)
        {
            character.EnsureDataConsistency();
            InitializePlayerState(character);
        }

        // 向PartyService提供角色列表引用 (现有代码)
        typeof(PartyService).GetField("_allCharacters", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_partyService, AllCharacters);
        
        // 添加: 向CombatService提供角色列表引用
        typeof(CombatService).GetField("_allCharacters", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_combatService, AllCharacters);

        // 其余代码不变
        DailyQuests = _questService.GetDailyQuests();
        WeeklyQuests = _questService.GetWeeklyQuests();

        _gameLoopTimer = new System.Timers.Timer(GameLoopIntervalMs);
        _gameLoopTimer.Elapsed += GameLoopTick;
        _gameLoopTimer.AutoReset = true;
        _gameLoopTimer.Start();
    }

    public void SetActiveCharacter(string characterId)
    {
        var character = AllCharacters.FirstOrDefault(c => c.Id == characterId);
        if (character != null && ActiveCharacter?.Id != characterId)
        {
            ActiveCharacter = character;
            NotifyStateChanged();
        }
    }

    private void GameLoopTick(object? sender, ElapsedEventArgs e)
    {
        double elapsedSeconds = GameLoopIntervalMs / 1000.0;
        
        foreach (var character in AllCharacters)
        {
            UpdateBuffs(character, elapsedSeconds);
            _inventoryService.UpdateConsumableCooldowns(character, elapsedSeconds);
            
            if (character.IsDead)
            {
                character.RevivalTimeRemaining -= elapsedSeconds;
                if (character.RevivalTimeRemaining <= 0) 
                    _combatService.ReviveCharacter(character);
                continue;
            }
            
            switch (character.CurrentAction)
            {
                case PlayerActionState.Combat:
                    var party = GetPartyForCharacter(character.Id);
                    _combatService.ProcessCombat(character, elapsedSeconds, party);
                    break;
                case PlayerActionState.Gathering: 
                    ProcessGathering(character, elapsedSeconds); 
                    break;
                case PlayerActionState.Crafting: 
                    ProcessCrafting(character, elapsedSeconds); 
                    break;
            }
            
            _inventoryService.ProcessAutoConsumables(character);
        }
        
        NotifyStateChanged();
    }

    // 删除原来的GetPartyForCharacter方法，改用PartyService的方法
    public Party? GetPartyForCharacter(string characterId)
    {
        return _partyService.GetPartyForCharacter(characterId);
    }

    /// <summary>
    /// 使用当前激活的角色创建一个新队伍，该角色将成为队长。
    /// </summary>
    public void CreateParty()
    {
        // 安全检查：确保有激活的角色，并且该角色当前不在任何队伍中。
        if (ActiveCharacter == null || GetPartyForCharacter(ActiveCharacter.Id) != null)
        {
            return; // 如果不满足条件，则不执行任何操作
        }

        // 创建一个新的队伍实例
        var newParty = new Party
        {
            CaptainId = ActiveCharacter.Id,
            MemberIds = new List<string> { ActiveCharacter.Id } // 阁长自己也是队伍的第一个成员
        };

        // 将新队伍添加到游戏状态的队伍列表中
        Parties.Add(newParty);

        // 通知UI进行刷新，以显示队伍状态的变化
        NotifyStateChanged();
    }

    /// <summary>
    /// 让当前激活的角色加入一个指定的队伍。
    /// </summary>
    /// <param name="partyId">要加入的队伍的ID</param>
    public void JoinParty(Guid partyId)
    {
        // 1. 安全检查 (保持不变)
        if (ActiveCharacter == null || GetPartyForCharacter(ActiveCharacter.Id) != null)
        {
            return;
        }

        // 2. 查找目标队伍
        var partyToJoin = Parties.FirstOrDefault(p => p.Id == partyId);
        if (partyToJoin == null)
        {
            return; // 队伍不存在
        }

        // --- vvv 新增：检查队伍是否已满 vvv ---
        if (partyToJoin.MemberIds.Count >= Party.MaxMembers)
        {
            return; // 队伍已满，无法加入
        }
        // --- ^^^ 新增结束 ^^^ ---

        // 3. 执行加入操作 (保持不变)
        partyToJoin.MemberIds.Add(ActiveCharacter.Id);

        // 4. 通知UI更新 (保持不变)
        NotifyStateChanged();
    }

    /// <summary>
    /// 让当前激活的角色离开他所在的队伍。
    /// </summary>
    public void LeaveParty()
    {
        // 1. 安全检查
        if (ActiveCharacter == null) return;
        var party = GetPartyForCharacter(ActiveCharacter.Id);
        if (party == null)
        {
            return; // 角色不在任何队伍中
        }

        // 2. 判断是队长离开还是成员离开
        if (party.CaptainId == ActiveCharacter.Id)
        {
            // 如果是队长离开，则解散整个队伍
            Parties.Remove(party);
        }
        else
        {
            // 如果只是普通成员离开，则从成员列表中移除自己
            party.MemberIds.Remove(ActiveCharacter.Id);
        }

        // 3. 通知UI更新
        NotifyStateChanged();
    }

    private void ProcessGathering(Player character, double elapsedSeconds)
    {
        if (character.CurrentGatheringNode == null) return;

        character.GatheringCooldown -= elapsedSeconds;
        if (character.GatheringCooldown <= 0)
        {
            _inventoryService.AddItemToInventory(character, character.CurrentGatheringNode.ResultingItemId, character.CurrentGatheringNode.ResultingItemQuantity);

            double extraLootChance = character.GetTotalExtraLootChance();
            if (extraLootChance > 0 && new Random().NextDouble() < extraLootChance)
            {
                _inventoryService.AddItemToInventory(character, character.CurrentGatheringNode.ResultingItemId, character.CurrentGatheringNode.ResultingItemQuantity);
            }

            character.AddGatheringXP(character.CurrentGatheringNode.RequiredProfession, character.CurrentGatheringNode.XpReward);

            UpdateQuestProgress(character, QuestType.GatherItem, character.CurrentGatheringNode.ResultingItemId, character.CurrentGatheringNode.ResultingItemQuantity);
            UpdateQuestProgress(character, QuestType.GatherItem, "any", 1);

            character.GatheringCooldown += GetCurrentGatheringTime(character);
        }
    }

    private void ProcessCrafting(Player character, double elapsedSeconds)
    {
        if (character.CurrentRecipe == null) return;

        character.CraftingCooldown -= elapsedSeconds;
        if (character.CraftingCooldown <= 0)
        {
            if (!_inventoryService.CanAffordRecipe(character, character.CurrentRecipe))
            {
                StopCurrentAction(character);
                return;
            }

            foreach (var ingredient in character.CurrentRecipe.Ingredients)
            {
                _inventoryService.RemoveItemFromInventory(character, ingredient.Key, ingredient.Value, out _);
            }

            _inventoryService.AddItemToInventory(character, character.CurrentRecipe.ResultingItemId, character.CurrentRecipe.ResultingItemQuantity);
            character.AddProductionXP(character.CurrentRecipe.RequiredProfession, character.CurrentRecipe.XpReward);

            UpdateQuestProgress(character, QuestType.CraftItem, character.CurrentRecipe.ResultingItemId, character.CurrentRecipe.ResultingItemQuantity);
            UpdateQuestProgress(character, QuestType.CraftItem, "any", 1);

            if (_inventoryService.CanAffordRecipe(character, character.CurrentRecipe))
            {
                character.CraftingCooldown += GetCurrentCraftingTime(character);
            }
            else
            {
                StopCurrentAction(character);
            }
        }
    }

    public void StartCombat(Enemy enemyTemplate) =>_combatService.StartCombat(ActiveCharacter, enemyTemplate, GetPartyForCharacter(ActiveCharacter?.Id));

    public void StartGathering(GatheringNode node) => StartGathering(ActiveCharacter, node);
    public void StartCrafting(Recipe recipe) => StartCrafting(ActiveCharacter, recipe);
    public void StopCurrentAction() => StopCurrentAction(ActiveCharacter);
    public void EquipItem(string itemId) => _inventoryService.EquipItem(ActiveCharacter, itemId);
    public void UnequipItem(EquipmentSlot slot) => _inventoryService.UnequipItem(ActiveCharacter, slot);
    public void SellItem(string itemId, int quantity = 1) => _inventoryService.SellItem(ActiveCharacter, itemId, quantity);
    public void UseItem(string itemId) => _inventoryService.UseItem(ActiveCharacter, itemId);
    public bool BuyItem(string itemId) => ActiveCharacter != null && _inventoryService.BuyItem(ActiveCharacter, itemId);
    public void SetQuickSlotItem(ConsumableCategory category, int slotId, string itemId) => _inventoryService.SetQuickSlotItem(ActiveCharacter, category, slotId, itemId);
    public void ClearQuickSlotItem(ConsumableCategory category, int slotId, FoodType foodType = FoodType.None) => _inventoryService.ClearQuickSlotItem(ActiveCharacter, category, slotId, foodType);
    public void ToggleAutoSellItem(string itemId) => _inventoryService.ToggleAutoSellItem(ActiveCharacter, itemId);
    public void SetBattleProfession(BattleProfession profession) =>_combatService.SetBattleProfession(ActiveCharacter, profession);
    public void EquipSkill(string skillId) =>_combatService.EquipSkill(ActiveCharacter, skillId, MaxEquippedSkills);
    public void UnequipSkill(string skillId) =>_combatService.UnequipSkill(ActiveCharacter, skillId);
    public void TryCompleteQuest(string questId) => TryCompleteQuest(ActiveCharacter, questId);

    private void StartGathering(Player? character, GatheringNode? node)
    {
        if (character == null || node == null) return;
        if (character.CurrentAction == PlayerActionState.Gathering && character.CurrentGatheringNode?.Id == node.Id) return;

        StopCurrentAction(character);
        character.CurrentAction = PlayerActionState.Gathering;
        character.CurrentGatheringNode = node;
        character.GatheringCooldown = GetCurrentGatheringTime(character);
        NotifyStateChanged();
    }

    private void StartCrafting(Player? character, Recipe? recipe)
    {
        if (character == null || recipe == null || !_inventoryService.CanAffordRecipe(character, recipe)) return;

        StopCurrentAction(character);
        character.CurrentAction = PlayerActionState.Crafting;
        character.CurrentRecipe = recipe;
        character.CraftingCooldown = GetCurrentCraftingTime(character);
        NotifyStateChanged();
    }

    private void StopCurrentAction(Player? character, bool keepTarget = false)
    {
        if (character == null) return;

        var party = GetPartyForCharacter(character.Id);

        // --- vvv 核心修改：不再检查是否为队长 vvv ---
        if (party != null)
        {
            // 只要角色在队伍中，他的停止指令就会解散整个团队的战斗状态。
            party.CurrentEnemy = null;
            foreach (var memberId in party.MemberIds)
            {
                var member = AllCharacters.FirstOrDefault(c => c.Id == memberId);
                if (member != null)
                {
                    member.CurrentAction = PlayerActionState.Idle;
                    member.CurrentEnemy = null; // 确保个人目标也被清空
                    member.AttackCooldown = 0;
                    member.GatheringCooldown = 0;
                    member.CraftingCooldown = 0;
                }
            }
        }
        // --- ^^^ 修改结束 ^^^ ---
        else
        {
            // 个人停止行动的逻辑 (保持原样)
            character.CurrentAction = PlayerActionState.Idle;
            if (!keepTarget)
            {
                character.CurrentEnemy = null;
                character.CurrentGatheringNode = null;
                character.CurrentRecipe = null;
            }
            character.AttackCooldown = 0;
            character.GatheringCooldown = 0;
            character.CraftingCooldown = 0;
        }

        NotifyStateChanged();
    }

    private void SpawnNewEnemyForCharacter(Player character, Enemy enemyTemplate)
    {
        var originalTemplate = AvailableMonsters.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
        character.CurrentEnemy = originalTemplate.Clone();
        character.CurrentEnemy.SkillCooldowns.Clear();
        foreach (var skillId in character.CurrentEnemy.SkillIds)
        {
            var skill = SkillData.GetSkillById(skillId);
            if (skill != null) character.CurrentEnemy.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
        }

        // --- vvv 修正点：初始化敌人实例的攻击冷却 vvv ---
        // 从设置 character.EnemyAttackCooldown 改为设置 character.CurrentEnemy.EnemyAttackCooldown
        character.CurrentEnemy.EnemyAttackCooldown = 1.0 / character.CurrentEnemy.AttacksPerSecond;
        // --- ^^^ 修正结束 ^^^ ---
    }

    private void UpdateBuffs(Player character, double elapsedSeconds)
    {
        if (!character.ActiveBuffs.Any()) return;
        bool buffsChanged = false;
        for (int i = character.ActiveBuffs.Count - 1; i >= 0; i--)
        {
            var buff = character.ActiveBuffs[i];
            buff.TimeRemainingSeconds -= elapsedSeconds;
            if (buff.TimeRemainingSeconds <= 0)
            {
                character.ActiveBuffs.RemoveAt(i);
                buffsChanged = true;
            }
        }
        if (buffsChanged) character.Health = Math.Min(character.Health, character.GetTotalMaxHealth());
    }

    private void UpdateQuestProgress(Player character, QuestType type, string targetId, int amount)
    {
        var allQuests = DailyQuests.Concat(WeeklyQuests);
        foreach (var quest in allQuests)
        {
            if (character.CompletedQuestIds.Contains(quest.Id)) continue;
            if (quest.Type == type && (quest.TargetId == targetId || quest.TargetId == "any"))
            {
                var currentProgress = character.QuestProgress.GetValueOrDefault(quest.Id, 0);
                character.QuestProgress[quest.Id] = Math.Min(currentProgress + amount, quest.RequiredAmount);
            }
        }
    }

    private void TryCompleteQuest(Player? character, string questId)
    {
        if (character == null) return;
        var quest = DailyQuests.Concat(WeeklyQuests).FirstOrDefault(q => q.Id == questId);
        if (quest == null || character.CompletedQuestIds.Contains(questId)) return;

        if (character.QuestProgress.GetValueOrDefault(questId, 0) >= quest.RequiredAmount)
        {
            character.Gold += quest.GoldReward;
            if (quest.ReputationReward > 0)
            {
                character.Reputation[quest.Faction] = character.Reputation.GetValueOrDefault(quest.Faction, 0) + quest.ReputationReward;
            }

            foreach (var itemReward in quest.ItemRewards)
            {
                _inventoryService.AddItemToInventory(character, itemReward.Key, itemReward.Value);
            }

            character.CompletedQuestIds.Add(questId);
            character.QuestProgress.Remove(questId);
            NotifyStateChanged();
        }
    }

    private void SetBattleProfession(Player? character, BattleProfession profession)
    {
        if (character != null)
        {
            character.SelectedBattleProfession = profession;
            NotifyStateChanged();
        }
    }

    // --- 其他辅助方法 ---
    private double GetCurrentGatheringTime(Player character)
    {
        if (character.CurrentGatheringNode == null) return 0;
        double speedBonus = character.GetTotalGatheringSpeedBonus();
        return character.CurrentGatheringNode.GatheringTimeSeconds / (1 + speedBonus);
    }
    private double GetCurrentCraftingTime(Player character)
    {
        if (character.CurrentRecipe == null) return 0;
        double speedBonus = character.GetTotalCraftingSpeedBonus();
        return character.CurrentRecipe.CraftingTimeSeconds / (1 + speedBonus);
    }
    private void InitializePlayerState(Player character)
    {
        foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
        {
            _combatService.CheckForNewSkillUnlocks(character, profession, character.GetLevel(profession), true);
        }
        _combatService.ResetPlayerSkillCooldowns(character);
        NotifyStateChanged();
    }

    public async Task SaveStateAsync(Player character)
    {
        if (character != null)
        {
            await _gameStorage.SavePlayerAsync(character);
        }
    }
    public async ValueTask DisposeAsync() { if (_gameLoopTimer != null) { _gameLoopTimer.Stop(); _gameLoopTimer.Dispose(); } }
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}