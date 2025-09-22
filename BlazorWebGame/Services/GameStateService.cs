using BlazorWebGame.Models;
using BlazorWebGame.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace BlazorWebGame.Services;

public class GameStateService : IAsyncDisposable
{
    private readonly GameStorage _gameStorage;
    private readonly QuestService _questService;
    private System.Timers.Timer? _gameLoopTimer;
    private const int GameLoopIntervalMs = 100;
    private const double RevivalDuration = 2;

    // --- vvv 核心架构变更 vvv ---
    public List<Player> AllCharacters { get; private set; } = new();
    public Player? ActiveCharacter { get; private set; }
    // --- ^^^ 变更结束 ^^^ ---
    /// <summary>
    /// 游戏中存在的所有队伍列表。
    /// </summary>
    public List<Party> Parties { get; private set; } = new();
    public List<Enemy> AvailableMonsters => MonsterTemplates.All;
    public List<GatheringNode> AvailableGatheringNodes => GatheringData.AllNodes;
    public const int MaxEquippedSkills = 4;
    public List<Quest> DailyQuests { get; private set; } = new();
    public List<Quest> WeeklyQuests { get; private set; } = new();

    public event Action? OnStateChanged;

    public GameStateService(GameStorage gameStorage, QuestService questService)
    {
        _gameStorage = gameStorage;
        _questService = questService;
    }

    public async Task InitializeAsync()
    {
        // TODO: 实现多角色加载/保存逻辑
        // var loadedCharacters = await _gameStorage.LoadCharactersAsync();
        // if (loadedCharacters != null && loadedCharacters.Any()) {
        //     AllCharacters = loadedCharacters;
        // } else {
        AllCharacters.Add(new Player { Name = "索拉尔" });
        AllCharacters.Add(new Player { Name = "阿尔特留斯", Gold = 50 });
        // }

        ActiveCharacter = AllCharacters.FirstOrDefault();

        foreach (var character in AllCharacters)
        {
            character.EnsureDataConsistency();
            InitializePlayerState(character);
        }

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
            UpdateConsumableCooldowns(character, elapsedSeconds);

            if (character.IsDead)
            {
                character.RevivalTimeRemaining -= elapsedSeconds;
                if (character.RevivalTimeRemaining <= 0) ReviveCharacter(character);
                continue;
            }

            switch (character.CurrentAction)
            {
                case PlayerActionState.Combat: ProcessCombat(character, elapsedSeconds); break;
                case PlayerActionState.Gathering: ProcessGathering(character, elapsedSeconds); break;
                case PlayerActionState.Crafting: ProcessCrafting(character, elapsedSeconds); break;
            }

            ProcessAutoConsumables(character);
        }

        NotifyStateChanged();
    }

    // --- vvv 以下所有方法都被重构为接收一个 'Player character' 参数 vvv ---
    private void HandleCharacterDeath(Player character)
    {
        // 如果角色已经死了，就没必要再执行一次死亡逻辑了
        if (character.IsDead) return;

        character.IsDead = true;
        character.Health = 0;
        character.RevivalTimeRemaining = RevivalDuration; // 使用您定义的常量

        // 之前这里会调用 StopCurrentAction，我们彻底删掉它。
        // 玩家的 CurrentAction 保持为 Combat，这样他复活后才知道自己应该继续战斗。

        // 死亡时移除大部分buff，但保留食物buff
        character.ActiveBuffs.RemoveAll(buff =>
        {
            var item = ItemData.GetItemById(buff.SourceItemId);
            return item is Consumable consumable && consumable.Category != ConsumableCategory.Food;
        });
    }

    /// <summary>
    /// 根据角色ID查找他所在的队伍。
    /// 如果角色不在任何队伍中，则返回 null。
    /// </summary>
    /// <param name="characterId">要查找的角色ID</param>
    /// <returns>角色所在的队伍对象，或 null</returns>
    public Party? GetPartyForCharacter(string characterId)
    {
        return Parties.FirstOrDefault(p => p.MemberIds.Contains(characterId));
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
            MemberIds = new List<string> { ActiveCharacter.Id } // 队长自己也是队伍的第一个成员
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

    private void ReviveCharacter(Player character)
    {
        character.IsDead = false;
        character.Health = character.GetTotalMaxHealth();
        character.RevivalTimeRemaining = 0;

        if (character.CurrentEnemy != null)
        {
            StartCombat(character, character.CurrentEnemy);
        }
        else if (character.CurrentGatheringNode != null)
        {
            StartGathering(character, character.CurrentGatheringNode);
        }
        else if (character.CurrentRecipe != null)
        {
            StartCrafting(character, character.CurrentRecipe);
        }
        else
        {
            InitializePlayerState(character);
        }
    }

    private void ProcessCombat(Player character, double elapsedSeconds)
    {
        // --- vvv 新增：死亡的角色不参与任何战斗计算 vvv ---
        if (character.IsDead)
        {
            return; // 直接跳过此人的本轮战斗循环
        }
        // --- ^^^ 新增结束 ^^^ ---

        var party = GetPartyForCharacter(character.Id);
        var targetEnemy = party?.CurrentEnemy ?? character.CurrentEnemy;

        if (targetEnemy == null)
        {
            // 只有在没有目标时才停止行动。
            // 只要 party.CurrentEnemy 存在，战斗就不会因为任何成员死亡而停止。
            StopCurrentAction(character);
            return;
        }

        // --- vvv 核心修正：移除团队全灭判断逻辑 vvv ---
        // 我们删除了之前在这里添加的 isPartyWiped 判断。
        // 战斗现在会无条件继续，直到有玩家主动离开或解散队伍。
        // --- ^^^ 修正结束 ^^^ ---

        // 玩家攻击逻辑 (只有活着的角色能执行到这里)
        character.AttackCooldown -= elapsedSeconds;
        if (character.AttackCooldown <= 0)
        {
            PlayerAttackEnemy(character, targetEnemy, party);
            character.AttackCooldown += 1.0 / character.AttacksPerSecond;
        }

        // 敌人攻击逻辑
        targetEnemy.EnemyAttackCooldown -= elapsedSeconds;
        if (targetEnemy.EnemyAttackCooldown <= 0)
        {
            Player? playerToAttack = null;
            if (party != null)
            {
                // 敌人只会选择活着的成员进行攻击
                var aliveMembers = AllCharacters.Where(c => party.MemberIds.Contains(c.Id) && !c.IsDead).ToList();
                if (aliveMembers.Any())
                {
                    playerToAttack = aliveMembers[new Random().Next(aliveMembers.Count)];
                }
                // 如果所有人都死了，playerToAttack 会是 null，敌人本轮就不会攻击，这符合逻辑。
            }
            else
            {
                playerToAttack = character; // 单人模式
            }

            if (playerToAttack != null)
            {
                EnemyAttackPlayer(targetEnemy, playerToAttack);
            }

            // 只有当敌人确实攻击了，才重置它的冷却
            if (playerToAttack != null)
            {
                targetEnemy.EnemyAttackCooldown += 1.0 / targetEnemy.AttacksPerSecond;
            }
        }
    }

    private void PlayerAttackEnemy(Player character, Enemy enemy, Party? party)
    {
        // 技能和攻击逻辑 (不变)
        ApplyCharacterSkills(character, enemy);
        enemy.Health -= character.GetTotalAttackPower();

        if (enemy.Health <= 0)
        {
            // 敌人被击败
            var originalTemplate = AvailableMonsters.FirstOrDefault(m => m.Name == enemy.Name) ?? enemy;

            if (party != null)
            {
                // --- vvv 全新的团队奖励分配逻辑 vvv ---

                // 1. 获取所有队伍成员，无论死活
                var partyMembers = AllCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
                if (!partyMembers.Any()) // 如果队伍是空的，直接结束
                {
                    party.CurrentEnemy = originalTemplate.Clone(); // 重置怪物
                    return;
                }
                var memberCount = partyMembers.Count;
                var random = new Random();

                // 2. 分配金币 (平分 + 随机分配余数)
                var totalGold = enemy.GetGoldDropAmount();
                var goldPerMember = totalGold / memberCount;
                var remainderGold = totalGold % memberCount;

                foreach (var member in partyMembers)
                {
                    member.Gold += goldPerMember;
                }
                if (remainderGold > 0)
                {
                    var luckyMemberForGold = partyMembers[random.Next(memberCount)];
                    luckyMemberForGold.Gold += remainderGold;
                }

                // 3. 分配战利品 (随机roll给一个幸运儿)
                foreach (var lootItem in enemy.LootTable)
                {
                    // 先判断这个物品是否掉落
                    if (random.NextDouble() <= lootItem.Value)
                    {
                        // 如果掉落，再随机选择一个成员获得它
                        var luckyMemberForLoot = partyMembers[random.Next(memberCount)];
                        AddItemToInventory(luckyMemberForLoot, lootItem.Key, 1);
                    }
                }

                // 4. 分配经验和任务进度 (给所有人，无论死活)
                foreach (var member in partyMembers)
                {
                    var profession = member.SelectedBattleProfession;
                    var oldLevel = member.GetLevel(profession);
                    member.AddBattleXP(profession, enemy.XpReward);
                    if (member.GetLevel(profession) > oldLevel)
                    {
                        CheckForNewSkillUnlocks(member, profession, member.GetLevel(profession));
                    }

                    UpdateQuestProgress(member, QuestType.KillMonster, enemy.Name, 1);
                    UpdateQuestProgress(member, QuestType.KillMonster, "any", 1);
                    member.DefeatedMonsterIds.Add(enemy.Name);
                }

                // --- ^^^ 奖励分配结束 ^^^ ---

                // 为团队生成新敌人
                party.CurrentEnemy = originalTemplate.Clone();
            }
            else
            {
                // --- 个人奖励分配 (逻辑不变) ---
                character.Gold += enemy.GetGoldDropAmount();
                var random = new Random();
                foreach (var lootItem in enemy.LootTable) { if (random.NextDouble() <= lootItem.Value) { AddItemToInventory(character, lootItem.Key, 1); } }
                var profession = character.SelectedBattleProfession;
                var oldLevel = character.GetLevel(profession);
                character.AddBattleXP(profession, enemy.XpReward);
                if (character.GetLevel(profession) > oldLevel) { CheckForNewSkillUnlocks(character, profession, character.GetLevel(profession)); }
                UpdateQuestProgress(character, QuestType.KillMonster, enemy.Name, 1);
                UpdateQuestProgress(character, QuestType.KillMonster, "any", 1);
                character.DefeatedMonsterIds.Add(enemy.Name);

                // 为个人生成新敌人
                SpawnNewEnemyForCharacter(character, originalTemplate);
            }
        }
    }

    private void EnemyAttackPlayer(Enemy enemy, Player character)
    {
        ApplyEnemySkills(enemy, character);
        character.Health -= enemy.AttackPower;
        if (character.Health <= 0)
        {
            HandleCharacterDeath(character);
        }
    }


    private void ProcessGathering(Player character, double elapsedSeconds)
    {
        if (character.CurrentGatheringNode == null) return;

        character.GatheringCooldown -= elapsedSeconds;
        if (character.GatheringCooldown <= 0)
        {
            AddItemToInventory(character, character.CurrentGatheringNode.ResultingItemId, character.CurrentGatheringNode.ResultingItemQuantity);
            double extraLootChance = character.GetTotalExtraLootChance();
            if (extraLootChance > 0 && new Random().NextDouble() < extraLootChance)
            {
                AddItemToInventory(character, character.CurrentGatheringNode.ResultingItemId, character.CurrentGatheringNode.ResultingItemQuantity);
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
            if (!CanAffordRecipe(character, character.CurrentRecipe))
            {
                StopCurrentAction(character);
                return;
            }

            foreach (var ingredient in character.CurrentRecipe.Ingredients)
            {
                RemoveItemFromInventory(character, ingredient.Key, ingredient.Value, out _);
            }
            AddItemToInventory(character, character.CurrentRecipe.ResultingItemId, character.CurrentRecipe.ResultingItemQuantity);
            character.AddProductionXP(character.CurrentRecipe.RequiredProfession, character.CurrentRecipe.XpReward);

            UpdateQuestProgress(character, QuestType.CraftItem, character.CurrentRecipe.ResultingItemId, character.CurrentRecipe.ResultingItemQuantity);
            UpdateQuestProgress(character, QuestType.CraftItem, "any", 1);

            if (CanAffordRecipe(character, character.CurrentRecipe))
            {
                character.CraftingCooldown += GetCurrentCraftingTime(character);
            }
            else
            {
                StopCurrentAction(character);
            }
        }
    }

    // --- vvv 公共操作方法现在都作用于 ActiveCharacter vvv ---

    public void StartCombat(Enemy enemyTemplate) => StartCombat(ActiveCharacter, enemyTemplate);
    public void StartGathering(GatheringNode node) => StartGathering(ActiveCharacter, node);
    public void StartCrafting(Recipe recipe) => StartCrafting(ActiveCharacter, recipe);
    public void StopCurrentAction() => StopCurrentAction(ActiveCharacter);
    public void EquipItem(string itemId) => EquipItem(ActiveCharacter, itemId);
    public void UnequipItem(EquipmentSlot slot) => UnequipItem(ActiveCharacter, slot);
    public void SellItem(string itemId, int quantity = 1) => SellItem(ActiveCharacter, itemId, quantity);
    public void UseItem(string itemId) => UseItem(ActiveCharacter, itemId);
    public void SetBattleProfession(BattleProfession profession) => SetBattleProfession(ActiveCharacter, profession);
    public void EquipSkill(string skillId) => EquipSkill(ActiveCharacter, skillId);
    public void UnequipSkill(string skillId) => UnequipSkill(ActiveCharacter, skillId);
    public void TryCompleteQuest(string questId) => TryCompleteQuest(ActiveCharacter, questId);


    // --- vvv 真正的实现逻辑，现在都接收character参数 vvv ---

    public void StartCombat(Player? character, Enemy? enemyTemplate)
    {
        if (character == null || enemyTemplate == null) return;

        var party = GetPartyForCharacter(character.Id);

        if (party != null)
        {
            // --- 团队作战逻辑 ---

            if (party.CaptainId != character.Id)
            {
                return;
            }

            if (party.CurrentEnemy?.Name == enemyTemplate.Name)
            {
                return;
            }

            var originalTemplate = AvailableMonsters.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
            party.CurrentEnemy = originalTemplate.Clone();

            party.CurrentEnemy.SkillCooldowns.Clear();
            foreach (var skillId in party.CurrentEnemy.SkillIds)
            {
                var skill = SkillData.GetSkillById(skillId);
                if (skill != null) party.CurrentEnemy.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
            }
            party.CurrentEnemy.EnemyAttackCooldown = 1.0 / party.CurrentEnemy.AttacksPerSecond;

            // --- vvv 核心修正：不再调用 StopCurrentAction vvv ---
            foreach (var memberId in party.MemberIds)
            {
                var member = AllCharacters.FirstOrDefault(c => c.Id == memberId);
                if (member != null && !member.IsDead)
                {
                    // 如果成员正在做采集或制作等非战斗、非空闲的活动，我们让他停下来
                    if (member.CurrentAction != PlayerActionState.Idle && member.CurrentAction != PlayerActionState.Combat)
                    {
                        // 这里我们只重置他个人的状态，而不调用会影响全队的 StopCurrentAction
                        member.CurrentGatheringNode = null;
                        member.CurrentRecipe = null;
                        member.GatheringCooldown = 0;
                        member.CraftingCooldown = 0;
                    }

                    // 统一设置所有符合条件的成员进入战斗状态
                    member.CurrentAction = PlayerActionState.Combat;
                    member.AttackCooldown = 0; // 重置攻击冷却，确保能立即攻击
                }
            }
            // --- ^^^ 修正结束 ^^^ ---
        }
        else
        {
            // --- 个人作战逻辑 (保持原样) ---
            if (character.CurrentAction == PlayerActionState.Combat && character.CurrentEnemy?.Name == enemyTemplate.Name) return;
            StopCurrentAction(character);
            character.CurrentAction = PlayerActionState.Combat;
            SpawnNewEnemyForCharacter(character, enemyTemplate);
        }

        NotifyStateChanged();
    }

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
        if (character == null || recipe == null || !CanAffordRecipe(character, recipe)) return;

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

    private void UpdateConsumableCooldowns(Player character, double elapsedSeconds)
    {
        if (!character.ConsumableCooldowns.Any()) return;
        var keys = character.ConsumableCooldowns.Keys.ToList();
        foreach (var key in keys)
        {
            character.ConsumableCooldowns[key] -= elapsedSeconds;
            if (character.ConsumableCooldowns[key] <= 0) character.ConsumableCooldowns.Remove(key);
        }
    }

    private bool CanAffordRecipe(Player character, Recipe recipe)
    {
        foreach (var ingredient in recipe.Ingredients)
        {
            if (character.Inventory.Where(s => s.ItemId == ingredient.Key).Sum(s => s.Quantity) < ingredient.Value) return false;
        }
        return true;
    }

    private void AddItemToInventory(Player character, string itemId, int quantity)
    {
        var itemToAdd = ItemData.GetItemById(itemId);
        if (itemToAdd == null) return;
        if (character.AutoSellItemIds.Contains(itemId)) { character.Gold += itemToAdd.Value * quantity; return; }
        if (itemToAdd.IsStackable)
        {
            var existingSlot = character.Inventory.FirstOrDefault(s => s.ItemId == itemId && s.Quantity < 99);
            if (existingSlot != null) { existingSlot.Quantity += quantity; return; }
        }
        var emptySlot = character.Inventory.FirstOrDefault(s => s.IsEmpty);
        if (emptySlot != null) { emptySlot.ItemId = itemId; emptySlot.Quantity = quantity; }
    }

    private bool RemoveItemFromInventory(Player character, string itemId, int quantityToRemove, out int actuallyRemoved)
    {
        actuallyRemoved = 0;
        for (int i = character.Inventory.Count - 1; i >= 0; i--)
        {
            var slot = character.Inventory[i];
            if (slot.ItemId == itemId)
            {
                int amountToRemoveFromSlot = Math.Min(quantityToRemove - actuallyRemoved, slot.Quantity);
                slot.Quantity -= amountToRemoveFromSlot;
                actuallyRemoved += amountToRemoveFromSlot;
                if (slot.Quantity <= 0) slot.ItemId = null;
                if (actuallyRemoved >= quantityToRemove) break;
            }
        }
        return actuallyRemoved > 0;
    }

    private void EquipItem(Player? character, string itemId)
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

        // --- vvv 这是唯一的修正区域 vvv ---

        // 1. 优先寻找一个空的可用槽位
        var emptySlot = targetSlots.FirstOrDefault(slot => !character.EquippedItems.ContainsKey(slot));

        // 2. 如果找不到空槽位（`FirstOrDefault` 会返回枚举的默认值，通常是0，即 Head），
        //    或者找到了一个（`emptySlot` 不是默认值），我们需要确定最终槽位。
        //    这里我们直接用 `FindIndex` 更清晰。
        EquipmentSlot? finalSlot;
        int emptySlotIndex = targetSlots.FindIndex(slot => !character.EquippedItems.ContainsKey(slot));

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

        // --- ^^^ 修正结束 ^^^ ---

        // 如果最终确定的槽位上已经有装备，则先卸下它
        if (character.EquippedItems.TryGetValue(finalSlot.Value, out var currentItemId))
        {
            UnequipItem(character, finalSlot.Value);
        }

        // 从背包中减少物品数量
        slotToEquipFrom.Quantity--;
        if (slotToEquipFrom.Quantity <= 0) slotToEquipFrom.ItemId = null;

        // 将新物品装备到最终确定的槽位上
        character.EquippedItems[finalSlot.Value] = itemId;
        character.Health = Math.Min(character.Health, character.GetTotalMaxHealth());
        NotifyStateChanged();
    }

    private void UnequipItem(Player? character, EquipmentSlot slot)
    {
        if (character == null || !character.EquippedItems.TryGetValue(slot, out var itemIdToUnequip)) return;
        character.EquippedItems.Remove(slot);
        AddItemToInventory(character, itemIdToUnequip, 1);
        character.Health = Math.Min(character.Health, character.GetTotalMaxHealth());
        NotifyStateChanged();
    }

    private void SellItem(Player? character, string itemId, int quantity = 1)
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

    private void UseItem(Player? character, string itemId)
    {
        if (character == null || ItemData.GetItemById(itemId) is not Consumable consumable) return;
        if (consumable.CooldownSeconds > 0 && character.ConsumableCooldowns.ContainsKey(consumable.Id)) return;
        if (RemoveItemFromInventory(character, itemId, 1, out int removedCount) && removedCount > 0)
        {
            ApplyConsumableEffect(character, consumable);
        }
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
            foreach (var itemReward in quest.ItemRewards) AddItemToInventory(character, itemReward.Key, itemReward.Value);

            character.CompletedQuestIds.Add(questId);
            character.QuestProgress.Remove(questId);
            NotifyStateChanged();
        }
    }

    public void SetQuickSlotItem(ConsumableCategory category, int slotId, string itemId) => SetQuickSlotItem(ActiveCharacter, category, slotId, itemId);
    public void ClearQuickSlotItem(ConsumableCategory category, int slotId, FoodType foodType = FoodType.None) => ClearQuickSlotItem(ActiveCharacter, category, slotId, foodType);
    public void ToggleAutoSellItem(string itemId) => ToggleAutoSellItem(ActiveCharacter, itemId);
    private void SetBattleProfession(Player? character, BattleProfession profession)
    {
        if (character != null)
        {
            character.SelectedBattleProfession = profession;
            NotifyStateChanged();
        }
    }

    private void SetQuickSlotItem(Player? character, ConsumableCategory category, int slotId, string itemId)
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

    private void ClearQuickSlotItem(Player? character, ConsumableCategory category, int slotId, FoodType foodType = FoodType.None)
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

    private void ToggleAutoSellItem(Player? character, string itemId)
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
    /// 处理当前激活角色购买商品的逻辑
    /// </summary>
    /// <param name="itemId">要购买的物品ID</param>
    /// <returns>如果购买成功则返回 true，否则返回 false</returns>
    public bool BuyItem(string itemId)
    {
        // 1. 确保有激活的角色
        if (ActiveCharacter is not Player character)
        {
            return false;
        }

        // 2. 查找物品及其商店信息
        var itemToBuy = ItemData.GetItemById(itemId);
        if (itemToBuy?.ShopPurchaseInfo == null)
        {
            return false; // 物品不存在或不可购买
        }

        var purchaseInfo = itemToBuy.ShopPurchaseInfo;

        // 3. 检查货币是否足够
        if (purchaseInfo.Currency == CurrencyType.Gold)
        {
            if (character.Gold < purchaseInfo.Price)
            {
                return false; // 金币不足
            }
        }
        else // 如果货币是物品
        {
            int ownedAmount = character.Inventory
                .Where(s => s.ItemId == purchaseInfo.CurrencyItemId)
                .Sum(s => s.Quantity);
            if (ownedAmount < purchaseInfo.Price)
            {
                return false; // 物品货币不足
            }
        }

        // 4. 扣除花费
        if (purchaseInfo.Currency == CurrencyType.Gold)
        {
            character.Gold -= purchaseInfo.Price;
        }
        else
        {
            // 调用辅助方法来移除指定数量的物品
            RemoveItem(character, purchaseInfo.CurrencyItemId!, purchaseInfo.Price);
        }

        // 5. 将购买的物品添加到背包
        AddItemToInventory(character, itemToBuy.Id, 1);

        // 6. 通知UI更新并保存游戏
        NotifyStateChanged();
        return true;
    }

    /// <summary>
    /// 从指定角色的背包中移除指定数量的物品
    /// </summary>
    private void RemoveItem(Player character, string itemId, int quantity)
    {
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
                    character.Inventory.RemoveAt(i);
                }

                if (quantityToRemove == 0)
                {
                    break;
                }
            }
        }
    }

    private void EquipSkill(Player? character, string skillId)
    {
        if (character == null) return;
        var profession = character.SelectedBattleProfession;
        var equipped = character.EquippedSkills[profession];
        var skill = SkillData.GetSkillById(skillId);
        if (skill == null || skill.Type == SkillType.Fixed || equipped.Contains(skillId)) return;
        if (equipped.Count(id => SkillData.GetSkillById(id)?.Type != SkillType.Fixed) < MaxEquippedSkills)
        {
            equipped.Add(skillId);
            character.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
            NotifyStateChanged();
        }
    }

    private void UnequipSkill(Player? character, string skillId)
    {
        if (character == null) return;
        var skill = SkillData.GetSkillById(skillId);
        if (skill == null || skill.Type == SkillType.Fixed) return;
        if (character.EquippedSkills[character.SelectedBattleProfession].Remove(skillId))
        {
            character.SkillCooldowns.Remove(skillId);
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
            CheckForNewSkillUnlocks(character, profession, character.GetLevel(profession), true);
        }
        ResetPlayerSkillCooldowns(character);
        NotifyStateChanged();
    }
    private void CheckForNewSkillUnlocks(Player character,BattleProfession profession, int level, bool checkAllLevels = false)
    {
        if (character == null) return;
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
                character.LearnedSharedSkills.Add(skill.Id); 
            }
            if (skill.Type == SkillType.Fixed) 
            {
                if (!character.EquippedSkills.TryGetValue(profession, out var equipped) || !equipped.Contains(skill.Id))
                {
                    character.EquippedSkills[profession].Insert(0, skill.Id);
                }
            }
        }
    }
    private void ResetPlayerSkillCooldowns(Player character)
    {
        if (character == null) return;
        character.SkillCooldowns.Clear();
        foreach (var skillId in character.EquippedSkills.Values.SelectMany(s => s))
        {
            var skill = SkillData.GetSkillById(skillId);
            if (skill != null) 
            {
                character.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
            }
        }
    }
    private void ApplyCharacterSkills(Player character, Enemy enemy)
    {
        var profession = character.SelectedBattleProfession;
        if (!character.EquippedSkills.ContainsKey(profession)) return;

        var equippedSkillIds = character.EquippedSkills[profession];

        foreach (var skillId in equippedSkillIds)
        {
            var cooldown = character.SkillCooldowns.GetValueOrDefault(skillId, 0);

            if (cooldown == 0)
            {
                var skill = SkillData.GetSkillById(skillId);
                if (skill == null) continue;

                // --- 技能效果处理 ---
                switch (skill.EffectType)
                {
                    case SkillEffectType.DirectDamage:
                        enemy.Health -= (int)skill.EffectValue;
                        break;
                    case SkillEffectType.Heal:
                        var healAmount = skill.EffectValue < 1.0
                            ? (int)(character.GetTotalMaxHealth() * skill.EffectValue)
                            : (int)skill.EffectValue;
                        character.Health = Math.Min(character.GetTotalMaxHealth(), character.Health + healAmount);
                        break;
                }
                // --- 技能触发后进入冷却 ---
                character.SkillCooldowns[skillId] = skill.CooldownRounds;
            }
            else if (cooldown > 0)
            {
                // 仅冷却减一
                character.SkillCooldowns[skillId] = cooldown - 1;
            }
            // 如果 cooldown < 0 理论上不会出现
        }
    }

    private void ApplyEnemySkills(Enemy enemy, Player character)
    {
        foreach (var skillId in enemy.SkillIds)
        {
            var cooldown = enemy.SkillCooldowns.GetValueOrDefault(skillId, 0);

            if (cooldown == 0)
            {
                var skill = SkillData.GetSkillById(skillId);
                if (skill == null) continue;

                switch (skill.EffectType)
                {
                    case SkillEffectType.DirectDamage:
                        character.Health -= (int)skill.EffectValue;
                        break;
                    case SkillEffectType.Heal:
                        var healAmount = skill.EffectValue < 1.0
                            ? (int)(enemy.MaxHealth * skill.EffectValue)
                            : (int)skill.EffectValue;
                        enemy.Health = Math.Min(enemy.MaxHealth, enemy.Health + healAmount);
                        break;
                }
                enemy.SkillCooldowns[skillId] = skill.CooldownRounds;
            }
            else if (cooldown > 0)
            {
                enemy.SkillCooldowns[skillId] = cooldown - 1;
            }
        }
    }
    private void ApplyConsumableEffect(Player character,Consumable consumable)
    {
        switch (consumable.Effect)
        {
            case ConsumableEffectType.Heal:
                character.Health = Math.Min(character.GetTotalMaxHealth(), character.Health + (int)consumable.EffectValue);
                break;
            case ConsumableEffectType.StatBuff:
                if (consumable.BuffType.HasValue && consumable.DurationSeconds.HasValue)
                {
                    character.ActiveBuffs.RemoveAll(b => b.FoodType == consumable.FoodType && b.BuffType == consumable.BuffType.Value);
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
    private void ProcessAutoConsumables(Player character)
    {
        if (character == null) return;
        var allQuickSlotItems = character.PotionQuickSlots
            .Concat(character.CombatFoodQuickSlots)
            .Concat(character.GatheringFoodQuickSlots)
            .Concat(character.ProductionFoodQuickSlots);
        foreach (var slot in allQuickSlotItems)
        {
            var itemId = slot.Value;
            if (string.IsNullOrEmpty(itemId) || character.ConsumableCooldowns.ContainsKey(itemId)) continue;
            if (ItemData.GetItemById(itemId) is not Consumable item) continue;
            bool shouldUse = false;
            switch (item.Category)
            {
                case ConsumableCategory.Potion:
                    if ((double)character.Health / character.GetTotalMaxHealth() < 0.7) shouldUse = true;
                    break;
                case ConsumableCategory.Food:
                    if (item.FoodType == FoodType.Combat && character.CurrentAction == PlayerActionState.Combat ||
                        item.FoodType == FoodType.Gathering && character.CurrentAction == PlayerActionState.Gathering ||
                        item.FoodType == FoodType.Production && character.CurrentAction == PlayerActionState.Crafting)
                    {
                        if (!character.ActiveBuffs.Any(b => b.BuffType == item.BuffType)) shouldUse = true;
                    }
                    break;
            }
            if (shouldUse && RemoveItemFromInventory(character,itemId, 1, out int removedCount) && removedCount > 0)
            {
                ApplyConsumableEffect(character,item);
            }
        }
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