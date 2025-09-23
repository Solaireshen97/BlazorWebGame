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
    private readonly IServiceProvider _serviceProvider;
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
        _serviceProvider = new ServiceProvider(); // 初始化服务提供者

        // 订阅各个服务的状态变更事件
        _partyService.OnStateChanged += () => NotifyStateChanged();
        _inventoryService.OnStateChanged += () => NotifyStateChanged();
        _combatService.OnStateChanged += () => NotifyStateChanged();
        _professionService.OnStateChanged += () => NotifyStateChanged();
        _characterService.OnStateChanged += () => NotifyStateChanged();
        _questService.OnStateChanged += () => NotifyStateChanged(); // 添加遗漏的订阅

        // 注册所有服务到服务定位器
        RegisterServices();
    }

    /// <summary>
    /// 注册所有服务到服务定位器
    /// </summary>
    private void RegisterServices()
    {
        ServiceLocator.Initialize();
        ServiceLocator.RegisterService(_gameStorage);
        ServiceLocator.RegisterService(_questService);
        ServiceLocator.RegisterService(_partyService);
        ServiceLocator.RegisterService(_inventoryService);
        ServiceLocator.RegisterService(_combatService);
        ServiceLocator.RegisterService(_professionService);
        ServiceLocator.RegisterService(_characterService);
        ServiceLocator.RegisterService(this); // 注册GameStateService自身
    }

    public async Task InitializeAsync()
    {
        // 初始化角色系统
        await _characterService.InitializeAsync();

        // 不再使用反射，而是使用公开的API来设置角色引用
        _partyService.SetAllCharacters(AllCharacters);
        _combatService.SetAllCharacters(AllCharacters);

        // 启动游戏循环
        _gameLoopTimer = new System.Timers.Timer(GameLoopIntervalMs);
        _gameLoopTimer.Elapsed += GameLoopTick;
        _gameLoopTimer.AutoReset = true;
        _gameLoopTimer.Start();
    }
    private T GetService<T>() where T : class => _serviceProvider.GetService<T>() ?? throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册");


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
        if (ActiveCharacter != null)
        {
            _partyService.CreateParty(ActiveCharacter);
        }
    }

    /// <summary>
    /// 让当前激活的角色加入一个指定的队伍。
    /// </summary>
    /// <param name="partyId">要加入的队伍的ID</param>
    public void JoinParty(Guid partyId)
    {
        if (ActiveCharacter != null)
        {
            _partyService.JoinParty(ActiveCharacter, partyId);
        }
    }


    /// <summary>
    /// 让当前激活的角色离开他所在的队伍。
    /// </summary>
    public void LeaveParty()
    {
        if (ActiveCharacter != null)
        {
            _partyService.LeaveParty(ActiveCharacter);
        }
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
            // 停止团队战斗 - 应该委托给PartyService
            _partyService.StopPartyAction(party);

            // 处理团队成员的状态
            foreach (var memberId in party.MemberIds)
            {
                var member = AllCharacters.FirstOrDefault(c => c.Id == memberId);
                if (member != null)
                {
                    // 停止专业活动
                    _professionService.StopCurrentAction(member);

                    // 重置战斗状态
                    if (member.CurrentAction == PlayerActionState.Combat)
                    {
                        member.CurrentAction = PlayerActionState.Idle;
                        member.CurrentEnemy = null;
                        member.AttackCooldown = 0;
                    }
                }
            }
        }
        else if (character.CurrentAction == PlayerActionState.Combat)
        {
            // 停止个人战斗
            character.CurrentAction = PlayerActionState.Idle;
            character.CurrentEnemy = null;
            character.AttackCooldown = 0;
        }
        else
        {
            // 停止采集和制作活动
            _professionService.StopCurrentAction(character);
        }

        NotifyStateChanged();
    }

    public async Task SaveStateAsync(Player character) =>await _characterService.SaveStateAsync(character);
    public async ValueTask DisposeAsync() { if (_gameLoopTimer != null) { _gameLoopTimer.Stop(); _gameLoopTimer.Dispose(); } }
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}