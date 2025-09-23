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
    private readonly ProfessionService _professionService;
    private readonly CharacterService _characterService;
    private System.Timers.Timer? _gameLoopTimer;
    private const int GameLoopIntervalMs = 100;
    private const double RevivalDuration = 2;

    public List<Player> AllCharacters => _characterService.AllCharacters;
    public Player? ActiveCharacter => _characterService.ActiveCharacter;

    public List<Party> Parties => _partyService.Parties;
    public List<Enemy> AvailableMonsters => MonsterTemplates.All;
    public List<GatheringNode> AvailableGatheringNodes => GatheringData.AllNodes;
    public const int MaxEquippedSkills = 4;
    public List<Quest> DailyQuests => _questService.DailyQuests;
    public List<Quest> WeeklyQuests => _questService.WeeklyQuests;

    public event Action? OnStateChanged;

    public GameStateService(
        GameStorage gameStorage,
        QuestService questService,
        PartyService partyService,
        InventoryService inventoryService,
        CombatService combatService,
        ProfessionService professionService,
        CharacterService characterService)
    {
        _gameStorage = gameStorage;
        _questService = questService;
        _partyService = partyService;
        _inventoryService = inventoryService;
        _combatService = combatService;
        _professionService = professionService;
        _characterService = characterService;

        // 订阅各个服务的状态变更事件
        _partyService.OnStateChanged += () => NotifyStateChanged();
        _inventoryService.OnStateChanged += () => NotifyStateChanged();
        _combatService.OnStateChanged += () => NotifyStateChanged();
        _professionService.OnStateChanged += () => NotifyStateChanged();
        _characterService.OnStateChanged += () => NotifyStateChanged();

        // 注册服务到服务定位器
        ServiceLocator.RegisterService(_questService);
    }

    public async Task InitializeAsync()
    {
        // 初始化角色系统
        await _characterService.InitializeAsync();

        // 向PartyService提供角色列表引用
        typeof(PartyService).GetField("_allCharacters", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_partyService, AllCharacters);

        // 向CombatService提供角色列表引用
        typeof(CombatService).GetField("_allCharacters", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_combatService, AllCharacters);

        // 不再需要初始化DailyQuests和WeeklyQuests，因为它们直接使用QuestService的属性

        _gameLoopTimer = new System.Timers.Timer(GameLoopIntervalMs);
        _gameLoopTimer.Elapsed += GameLoopTick;
        _gameLoopTimer.AutoReset = true;
        _gameLoopTimer.Start();
    }

    public void SetActiveCharacter(string characterId) =>_characterService.SetActiveCharacter(characterId);

    private void GameLoopTick(object? sender, ElapsedEventArgs e)
    {
        double elapsedSeconds = GameLoopIntervalMs / 1000.0;

        foreach (var character in AllCharacters)
        {
            // 使用CharacterService更新Buff
            _characterService.UpdateBuffs(character, elapsedSeconds);
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
                    _professionService.ProcessGathering(character, elapsedSeconds);
                    break;
                case PlayerActionState.Crafting:
                    _professionService.ProcessCrafting(character, elapsedSeconds);
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

    public void StartCombat(Enemy enemyTemplate) =>_combatService.StartCombat(ActiveCharacter, enemyTemplate, GetPartyForCharacter(ActiveCharacter?.Id));

    // 修改委托方法使用ProfessionService
    public void StartGathering(GatheringNode node) =>_professionService.StartGathering(ActiveCharacter, node);

    public void StartCrafting(Recipe recipe) =>_professionService.StartCrafting(ActiveCharacter, recipe);
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
    // 修改为委托到QuestService
    public void TryCompleteQuest(string questId) =>_questService.TryCompleteQuest(ActiveCharacter, questId);

    private void StopCurrentAction(Player? character, bool keepTarget = false)
    {
        if (character == null) return;

        var party = GetPartyForCharacter(character.Id);

        if (party != null)
        {
            // 团队战斗相关逻辑保持不变
            party.CurrentEnemy = null;
            foreach (var memberId in party.MemberIds)
            {
                var member = AllCharacters.FirstOrDefault(c => c.Id == memberId);
                if (member != null)
                {
                    // 对于团队成员，使用ProfessionService停止其专业活动
                    _professionService.StopCurrentAction(member);
                    member.CurrentEnemy = null;
                    member.AttackCooldown = 0;
                }
            }
        }
        else if (character.CurrentAction == PlayerActionState.Combat)
        {
            // 对于个人战斗状态
            character.CurrentAction = PlayerActionState.Idle;
            character.CurrentEnemy = null;
            character.AttackCooldown = 0;
        }
        else
        {
            // 对于采集和制作状态，委托给ProfessionService
            _professionService.StopCurrentAction(character);
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

    private void SetBattleProfession(Player? character, BattleProfession profession)
    {
        if (character != null)
        {
            character.SelectedBattleProfession = profession;
            NotifyStateChanged();
        }
    }

    public async Task SaveStateAsync(Player character) =>await _characterService.SaveStateAsync(character);
    public async ValueTask DisposeAsync() { if (_gameLoopTimer != null) { _gameLoopTimer.Stop(); _gameLoopTimer.Dispose(); } }
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}