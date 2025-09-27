using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorWebGame.Services;

/// <summary>
/// 简化的游戏状态服务 - 仅保留UI状态管理和数据访问，所有游戏逻辑由服务器处理
/// </summary>
public class GameStateService : IAsyncDisposable
{
    private readonly GameStorage _gameStorage;
    private readonly QuestService _questService;
    private readonly PartyService _partyService;
    private readonly InventoryService _inventoryService;
    private readonly CombatService _combatService;
    private readonly ProfessionService _professionService;
    private readonly CharacterService _characterService;

    // 事件管理器
    private readonly GameEventManager _eventManager = new();

    // 保留UI展示需要的数据访问
    public List<Player> AllCharacters => _characterService.AllCharacters;
    public Player? ActiveCharacter => _characterService.ActiveCharacter;

    [Obsolete("本地组队数据已移除，请使用服务器组队API获取")]
    public List<Party> Parties => new List<Party>(); // 返回空列表，应使用服务器API
    
    // 静态数据保留，用于UI展示
    public List<Enemy> AvailableMonsters => MonsterTemplates.All;
    public List<GatheringNode> AvailableGatheringNodes => GatheringData.AllNodes;
    public const int MaxEquippedSkills = 4;
    
    [Obsolete("本地任务数据已移除，请使用服务器任务API获取")]
    public List<Quest> DailyQuests => new List<Quest>(); // 返回空列表，应使用服务器API
    
    [Obsolete("本地任务数据已移除，请使用服务器任务API获取")]
    public List<Quest> WeeklyQuests => new List<Quest>(); // 返回空列表，应使用服务器API

    // 保留事件机制用于UI通知
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

        // 订阅各个服务的状态变更事件，转发到新的事件系统
        _partyService.OnStateChanged += () => RaiseEvent(GameEventType.GenericStateChanged);
        _inventoryService.OnStateChanged += () => RaiseEvent(GameEventType.GenericStateChanged);
        _combatService.OnStateChanged += () => RaiseEvent(GameEventType.GenericStateChanged);
        _professionService.OnStateChanged += () => RaiseEvent(GameEventType.GenericStateChanged);
        _characterService.OnStateChanged += () => RaiseEvent(GameEventType.GenericStateChanged);
        _questService.OnStateChanged += () => RaiseEvent(GameEventType.GenericStateChanged);
        
        // 为兼容性，订阅GenericStateChanged事件到旧的OnStateChanged
        _eventManager.Subscribe(GameEventType.GenericStateChanged, _ => OnStateChanged?.Invoke());

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

    /// <summary>
    /// 简化的初始化方法 - 仅初始化必要的UI组件
    /// </summary>
    public async Task InitializeAsync()
    {
        // 初始化角色系统
        await _characterService.InitializeAsync();

        // 设置角色引用 - 仅用于UI显示
        _partyService.SetAllCharacters(AllCharacters);
        _combatService.SetAllCharacters(AllCharacters);

        // 触发游戏初始化完成事件
        RaiseEvent(GameEventType.GameInitialized);
    }

    /// <summary>
    /// 设置活跃角色 - 仅更新UI状态
    /// </summary>
    public void SetActiveCharacter(string characterId)
    {
        if (_characterService.SetActiveCharacter(characterId))
        {
            // 触发角色变更事件
            RaiseEvent(GameEventType.ActiveCharacterChanged, ActiveCharacter);
        }
    }

    #region 事件管理

    /// <summary>
    /// 订阅事件 - 使用原始事件系统签名
    /// </summary>
    public void Subscribe(GameEventType eventType, Action<GameEventArgs> handler)
    {
        _eventManager.Subscribe(eventType, handler);
    }

    /// <summary>
    /// 取消订阅事件 - 使用原始事件系统签名
    /// </summary>
    public void Unsubscribe(GameEventType eventType, Action<GameEventArgs> handler)
    {
        _eventManager.Unsubscribe(eventType, handler);
    }

    /// <summary>
    /// 订阅事件 - 向后兼容方法
    /// </summary>
    public void SubscribeToEvent(GameEventType eventType, Action<GameEventArgs> handler)
    {
        Subscribe(eventType, handler);
    }

    /// <summary>
    /// 取消订阅事件 - 向后兼容方法
    /// </summary>
    public void UnsubscribeFromEvent(GameEventType eventType, Action<GameEventArgs> handler)
    {
        Unsubscribe(eventType, handler);
    }

    /// <summary>
    /// 触发事件 - 基础版本
    /// </summary>
    public void RaiseEvent(GameEventType eventType, Player? player = null)
    {
        var args = new GameEventArgs(eventType, player);
        _eventManager.Raise(args);
    }

    /// <summary>
    /// 触发事件 - 泛型版本，兼容旧代码
    /// </summary>
    public void RaiseEvent<T>(GameEventType eventType, T eventData = default)
    {
        Player? player = null;
        if (eventData is Player p)
        {
            player = p;
        }
        var args = new GameEventArgs(eventType, player);
        _eventManager.Raise(args);
    }

    #endregion

    #region 向后兼容的废弃方法 - 仅保留方法签名避免编译错误

    /// <summary>
    /// 开始制作 - 本地实现已移除，请使用服务器API
    /// </summary>
    [Obsolete("本地制作系统已移除，请使用服务器API")]
    public void StartCrafting(Recipe recipe)
    {
        // 本地制作逻辑已移除，请使用服务器API
        // 可以通过 ProductionApiServiceNew 调用服务器制作接口
    }

    /// <summary>
    /// 设置快捷栏物品 - 仅保留UI状态管理
    /// </summary>
    [Obsolete("请直接通过角色对象管理快捷栏")]
    public void SetQuickSlotItem(Player character, int slotIndex, string itemId)
    {
        // 简化实现，仅更新UI状态
        if (character != null && slotIndex >= 0 && slotIndex < character.GatheringFoodQuickSlots.Count)
        {
            character.GatheringFoodQuickSlots[slotIndex] = itemId;
            RaiseEvent(GameEventType.GenericStateChanged);
        }
    }

    /// <summary>
    /// 设置快捷栏物品 - 重载方法处理不同的快捷栏类型
    /// </summary>
    [Obsolete("请直接通过角色对象管理快捷栏")]
    public void SetQuickSlotItem(ConsumableCategory category, int slotIndex, string itemId)
    {
        // 简化实现，仅更新UI状态
        if (ActiveCharacter != null)
        {
            SetQuickSlotItem(ActiveCharacter, slotIndex, itemId);
        }
    }

    /// <summary>
    /// 清除快捷栏物品 - 仅保留UI状态管理
    /// </summary>
    [Obsolete("请直接通过角色对象管理快捷栏")]
    public void ClearQuickSlotItem(Player character, int slotIndex)
    {
        // 简化实现，仅更新UI状态
        if (character != null && slotIndex >= 0 && slotIndex < character.GatheringFoodQuickSlots.Count)
        {
            character.GatheringFoodQuickSlots[slotIndex] = "";
            RaiseEvent(GameEventType.GenericStateChanged);
        }
    }

    /// <summary>
    /// 清除快捷栏物品 - 重载方法处理不同参数
    /// </summary>
    [Obsolete("请直接通过角色对象管理快捷栏")]
    public void ClearQuickSlotItem(Player character, int slotIndex, ConsumableCategory category)
    {
        // 简化实现，忽略category参数
        ClearQuickSlotItem(character, slotIndex);
    }

    /// <summary>
    /// 装备物品 - 本地实现已移除，请使用服务器API
    /// </summary>
    [Obsolete("本地装备系统已移除，请使用服务器API")]
    public void EquipItem(Player character, string itemId)
    {
        // 本地装备系统已移除，请使用服务器API
    }

    /// <summary>
    /// 卸下装备 - 本地实现已移除，请使用服务器API
    /// </summary>
    [Obsolete("本地装备系统已移除，请使用服务器API")]
    public void UnequipItem(Player character, EquipmentSlot slot)
    {
        // 本地装备系统已移除，请使用服务器API
    }

    /// <summary>
    /// 卸下装备 - 重载方法，使用当前活跃角色
    /// </summary>
    [Obsolete("本地装备系统已移除，请使用服务器API")]
    public void UnequipItem(EquipmentSlot slot)
    {
        // 本地装备系统已移除，请使用服务器API
        if (ActiveCharacter != null)
        {
            UnequipItem(ActiveCharacter, slot);
        }
    }

    /// <summary>
    /// 使用物品 - 本地实现已移除，请使用服务器API
    /// </summary>
    [Obsolete("本地物品系统已移除，请使用服务器API")]
    public void UseItem(Player character, string itemId)
    {
        // 本地物品系统已移除，请使用服务器API
    }

    /// <summary>
    /// 出售物品 - 本地实现已移除，请使用服务器API
    /// </summary>
    [Obsolete("本地商店系统已移除，请使用服务器API")]
    public void SellItem(Player character, string itemId, int quantity = 1)
    {
        // 本地商店系统已移除，请使用服务器API
    }

    /// <summary>
    /// 添加战斗经验 - 本地实现已移除，由服务器处理
    /// </summary>
    [Obsolete("经验管理已移除，由服务器处理")]
    public void AddBattleXP(BattleProfession profession, int amount)
    {
        // 本地经验管理已移除，由服务器处理
    }

    /// <summary>
    /// 添加采集经验 - 本地实现已移除，由服务器处理
    /// </summary>
    [Obsolete("经验管理已移除，由服务器处理")]
    public void AddGatheringXP(GatheringProfession profession, int amount)
    {
        // 本地经验管理已移除，由服务器处理
    }

    /// <summary>
    /// 添加生产经验 - 本地实现已移除，由服务器处理
    /// </summary>
    [Obsolete("经验管理已移除，由服务器处理")]
    public void AddProductionXP(ProductionProfession profession, int amount)
    {
        // 本地经验管理已移除，由服务器处理
    }

    /// <summary>
    /// 开始战斗 - 本地实现已移除，请使用服务器API
    /// </summary>
    [Obsolete("本地战斗系统已移除，请使用服务器API")]
    public void StartBattle(Player character, Enemy enemy, string? battleType = null)
    {
        // 本地战斗系统已移除，请使用服务器API
    }

    /// <summary>
    /// 停止战斗 - 本地实现已移除，请使用服务器API
    /// </summary>
    [Obsolete("本地战斗系统已移除，请使用服务器API")]
    public void StopBattle(Player character)
    {
        // 本地战斗系统已移除，请使用服务器API
    }

    /// <summary>
    /// 开始采集 - 本地实现已移除，请使用服务器API
    /// </summary>
    [Obsolete("本地生产系统已移除，请使用服务器API")]
    public void StartGathering(Player character, GatheringNode node)
    {
        // 本地采集系统已移除，请使用服务器API
    }

    /// <summary>
    /// 停止当前活动 - 本地实现已移除，请使用服务器API
    /// </summary>
    [Obsolete("本地生产系统已移除，请使用服务器API")]
    public void StopCurrentAction(Player character)
    {
        // 本地生产系统已移除，请使用服务器API
    }

    #endregion

    #region 资源清理

    /// <summary>
    /// 资源清理
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // 清理事件订阅
        _eventManager?.Dispose();
        
        GC.SuppressFinalize(this);
        await Task.CompletedTask;
    }

    #endregion
}