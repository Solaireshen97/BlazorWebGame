using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Utils;
using BlazorWebGame.Client.Services.Api;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
// 移除 System.Timers 导入 - 不再使用定时器
// using System.Timers; // 已移除
// 移除 System.Diagnostics 导入 - 不再进行本地性能监控
// using System.Diagnostics; // 已移除

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
    private readonly ClientPartyService? _clientPartyService; // 新的服务端组队服务
    private readonly System.IServiceProvider _serviceProvider;
    // 移除本地游戏循环相关字段 - 所有游戏逻辑由服务端处理
    // private System.Timers.Timer? _gameLoopTimer; // 已移除
    // private const int GameLoopIntervalMs = 100; // 已移除
    // private const double RevivalDuration = 2; // 已移除

    // 移除性能监控相关字段 - 服务端负责性能监控
    // private readonly Stopwatch _gameLoopStopwatch = new(); // 已移除

    // 新增事件管理器
    private readonly GameEventManager _eventManager = new();

    // 保留UI展示需要的数据访问，但标记为需要迁移
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

    // 保留老的事件机制，但在内部用新系统实现
    public event Action? OnStateChanged;

    public GameStateService(
        GameStorage gameStorage,
        QuestService questService,
        PartyService partyService,
        InventoryService inventoryService,
        CombatService combatService,
        ProfessionService professionService,
        CharacterService characterService,
        System.IServiceProvider serviceProvider)
    {
        _gameStorage = gameStorage;
        _questService = questService;
        _partyService = partyService;
        _inventoryService = inventoryService;
        _combatService = combatService;
        _professionService = professionService;
        _characterService = characterService;
        _serviceProvider = serviceProvider;

        // 尝试获取可选的客户端组队服务
        _clientPartyService = serviceProvider.GetService<ClientPartyService>();

        // 订阅各个服务的状态变更事件，转发到新的事件系统
        _partyService.OnStateChanged += () => RaiseEvent(GameEventType.GenericStateChanged);
        _inventoryService.OnStateChanged += () => RaiseEvent(GameEventType.GenericStateChanged);
        _combatService.OnStateChanged += () => RaiseEvent(GameEventType.GenericStateChanged);
        _professionService.OnStateChanged += () => RaiseEvent(GameEventType.GenericStateChanged);
        _characterService.OnStateChanged += () => RaiseEvent(GameEventType.GenericStateChanged);
        _questService.OnStateChanged += () => RaiseEvent(GameEventType.GenericStateChanged);
        
        // 订阅客户端组队服务的事件
        if (_clientPartyService != null)
        {
            _clientPartyService.OnPartyChanged += _ => RaiseEvent(GameEventType.GenericStateChanged);
            _clientPartyService.OnPartyMessage += message => 
            {
                // 可以在这里显示组队消息
                Console.WriteLine($"[Party] {message}");
            };
        }
        
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

    public async Task InitializeAsync()
    {
        // 初始化角色系统
        await _characterService.InitializeAsync();

        // 不再使用反射，而是使用公开的API来设置角色引用
        _partyService.SetAllCharacters(AllCharacters);
        _combatService.SetAllCharacters(AllCharacters);

        // 初始化组队服务
        await InitializePartyServiceAsync();

        // 移除本地游戏循环启动 - 所有游戏逻辑由服务端处理
        // StartGameLoop(); // 已移除
        
        // 触发游戏初始化完成事件
        RaiseEvent(GameEventType.GameInitialized);
    }
    private T GetService<T>() where T : class => _serviceProvider.GetService<T>() ?? throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册");


    public async void SetActiveCharacter(string characterId)
    {
        if (_characterService.SetActiveCharacter(characterId))
        {
            // 重新初始化组队服务
            await InitializePartyServiceAsync();
            
            // 触发角色变更事件
            RaiseEvent(GameEventType.ActiveCharacterChanged, ActiveCharacter);
        }
    }

    /// <summary>
    /// 本地游戏循环已移除 - 所有游戏逻辑由服务端处理
    /// </summary>
    [Obsolete("本地游戏循环已移除，所有游戏逻辑由服务端处理")]
    private void GameLoopTick()
    {
        // 本地游戏循环已移除，所有游戏逻辑处理由服务端负责
        // 客户端只负责UI展示和用户交互
    }

    /// <summary>
    /// 本地游戏状态更新已移除 - 所有状态由服务端管理
    /// </summary>
    [Obsolete("本地游戏状态更新已移除，所有状态由服务端管理")]
    private void UpdateGlobalGameState(double elapsedSeconds)
    {
        // 本地状态更新已移除
        // 任务重置等逻辑由服务端处理
    }
    
    /// <summary>
    /// 本地角色状态更新已移除 - 所有角色状态由服务端管理
    /// </summary>
    [Obsolete("本地角色状态更新已移除，所有角色状态由服务端管理")]
    private void UpdateCharacter(Player character, double elapsedSeconds)
    {
        // 本地角色状态更新已移除
        // Buff更新、冷却时间、死亡复活等逻辑由服务端处理
    }
    
    /// <summary>
    /// 本地死亡角色处理已移除 - 由服务端处理
    /// </summary>
    [Obsolete("本地死亡角色处理已移除，由服务端处理")]
    private void ProcessDeadCharacter(Player character, double elapsedSeconds)
    {
        // 本地死亡处理已移除
        // 复活逻辑由服务端处理
    }
    
    /// <summary>
    /// 本地活跃角色处理已移除 - 由服务端处理
    /// </summary>
    [Obsolete("本地活跃角色处理已移除，由服务端处理")]
    private void ProcessActiveCharacter(Player character, double elapsedSeconds)
    {
        // 本地角色状态处理已移除
        // 采集、制作、战斗等逻辑由服务端处理
    }

    /// <summary>
    /// 本地战斗状态处理已移除 - 由服务端处理
    /// </summary>
    [Obsolete("本地战斗状态处理已移除，由服务端处理")]
    private void ProcessCombatState(Player character, double elapsedSeconds)
    {
        // 本地战斗状态处理已移除
        // 所有战斗逻辑由服务端处理
    }

    /// <summary>
    /// 性能监控已移除 - 由服务端负责
    /// </summary>
    [Obsolete("性能监控已移除，由服务端负责")]
    private void RecordPerformanceMetrics()
    {
        // 本地性能监控已移除
        // 服务端负责性能监控和日志记录
    }
    
    /// <summary>
    /// 本地错误记录已移除 - 使用标准日志系统
    /// </summary>
    [Obsolete("本地错误记录已移除，使用标准日志系统")]
    private void LogError(Exception ex, string? context = null)
    {
        // 本地日志记录已移除
        // 使用标准的ILogger接口和服务端日志系统
    }
    
    /// <summary>
    /// 本地警告记录已移除 - 使用标准日志系统
    /// </summary>
    [Obsolete("本地警告记录已移除，使用标准日志系统")]
    private void LogWarning(string message)
    {
        Console.WriteLine($"游戏循环警告: {message}");
    }
    
    /// <summary>
    /// 本地游戏循环启动已移除 - 所有游戏逻辑由服务端处理
    /// </summary>
    [Obsolete("本地游戏循环已移除，所有游戏逻辑由服务端处理")]
    private void StartGameLoop()
    {
        // 本地游戏循环已移除
        // 所有游戏逻辑（定时器、状态更新）由服务端处理
        // 客户端只负责UI展示和用户交互
    }
    
    /// <summary>
    /// 本地游戏循环停止已移除 - 不再需要
    /// </summary>
    [Obsolete("本地游戏循环已移除，不再需要停止")]
    private void StopGameLoop()
    {
        // 本地游戏循环已移除，无需停止
    }


    /// <summary>
    /// 获取角色所在的队伍 - 已移除本地实现，请使用服务器API
    /// </summary>
    [Obsolete("本地组队系统已移除，请使用服务器组队API")]
    public Party? GetPartyForCharacter(string characterId)
    {
        // 本地组队系统已移除，请使用服务器API
        return null;
    }

    /// <summary>
    /// 使用当前激活的角色创建一个新队伍，该角色将成为队长。
    /// 现在完全依赖服务器API
    /// </summary>
    public async Task<bool> CreatePartyAsync()
    {
        if (ActiveCharacter == null) return false;

        // 只使用服务端组队服务
        if (_clientPartyService != null)
        {
            return await _clientPartyService.CreatePartyAsync(ActiveCharacter.Id);
        }
        
        return false;
    }

    /// <summary>
    /// 让当前激活的角色加入一个指定的队伍。
    /// 现在完全依赖服务器API
    /// </summary>
    /// <param name="partyId">要加入的队伍的ID</param>
    public async Task<bool> JoinPartyAsync(Guid partyId)
    {
        if (ActiveCharacter == null) return false;

        // 只使用服务端组队服务
        if (_clientPartyService != null)
        {
            return await _clientPartyService.JoinPartyAsync(ActiveCharacter.Id, partyId);
        }
        
        return false;
    }

    /// <summary>
    /// 让当前激活的角色离开他所在的队伍。
    /// 现在完全依赖服务器API
    /// </summary>
    public async Task<bool> LeavePartyAsync()
    {
        if (ActiveCharacter == null) return false;

        // 只使用服务端组队服务
        if (_clientPartyService != null)
        {
            return await _clientPartyService.LeavePartyAsync(ActiveCharacter.Id);
        }
        
        return false;
    }

    #region 兼容性方法 - 同步版本（向后兼容现有UI代码）

    /// <summary>
    /// 创建队伍（同步版本，向后兼容）
    /// </summary>
    [Obsolete("建议使用异步版本 CreatePartyAsync")]
    public void CreateParty()
    {
        if (ActiveCharacter != null)
        {
            _ = Task.Run(async () => await CreatePartyAsync());
        }
    }

    /// <summary>
    /// 加入队伍（同步版本，向后兼容）
    /// </summary>
    [Obsolete("建议使用异步版本 JoinPartyAsync")]
    public void JoinParty(Guid partyId)
    {
        if (ActiveCharacter != null)
        {
            _ = Task.Run(async () => await JoinPartyAsync(partyId));
        }
    }

    /// <summary>
    /// 离开队伍（同步版本，向后兼容）
    /// </summary>
    [Obsolete("建议使用异步版本 LeavePartyAsync")]
    public void LeaveParty()
    {
        if (ActiveCharacter != null)
        {
            _ = Task.Run(async () => await LeavePartyAsync());
        }
    }

    #endregion

    /// <summary>
    /// 初始化组队服务（在游戏启动时调用）
    /// </summary>
    public async Task InitializePartyServiceAsync()
    {
        if (_clientPartyService != null && ActiveCharacter != null)
        {
            await _clientPartyService.InitializeAsync(ActiveCharacter.Id);
        }
    }

    public async Task StartCombatAsync(Enemy enemyTemplate)
    {
        if (ActiveCharacter == null) return;

        // 只使用服务端战斗系统
        var gameApiService = _serviceProvider.GetService<GameApiService>();
        var clientGameStateService = _serviceProvider.GetService<ClientGameStateService>();
        
        if (gameApiService != null && clientGameStateService != null)
        {
            // 获取组队信息
            string? partyId = null;
            if (_clientPartyService != null && _clientPartyService.IsInParty(ActiveCharacter.Id))
            {
                partyId = _clientPartyService.CurrentParty?.Id.ToString();
            }

            // 启动服务端战斗
            await clientGameStateService.StartBattleAsync(enemyTemplate.Name, partyId);
        }
    }

    public void StartCombat(Enemy enemyTemplate)
    {
        // 同步版本，向后兼容
        _ = Task.Run(async () => await StartCombatAsync(enemyTemplate));
    }
    // 本地业务逻辑已移除 - 所有操作应通过服务器API执行
    [Obsolete("本地生产系统已移除，请使用服务器生产API")]
    public void StartGathering(GatheringNode node) 
    { 
        // 本地采集逻辑已移除，请使用服务器API
    }

    [Obsolete("本地生产系统已移除，请使用服务器生产API")]
    public void StartCrafting(Recipe recipe) 
    { 
        // 本地制作逻辑已移除，请使用服务器API
    }
    
    public void StopCurrentAction() => StopCurrentAction(ActiveCharacter);
    
    [Obsolete("本地库存系统已移除，请使用服务器库存API")]
    public void EquipItem(string itemId) 
    { 
        // 本地装备逻辑已移除，请使用服务器API
    }
    
    [Obsolete("本地库存系统已移除，请使用服务器库存API")]
    public void UnequipItem(EquipmentSlot slot) 
    { 
        // 本地装备逻辑已移除，请使用服务器API
    }
    
    [Obsolete("本地库存系统已移除，请使用服务器库存API")]
    public void SellItem(string itemId, int quantity = 1) 
    { 
        // 本地商店逻辑已移除，请使用服务器API
    }
    
    [Obsolete("本地库存系统已移除，请使用服务器库存API")]
    public void UseItem(string itemId) 
    { 
        // 本地物品使用逻辑已移除，请使用服务器API
    }
    
    [Obsolete("本地库存系统已移除，请使用服务器库存API")]
    public bool BuyItem(string itemId) 
    { 
        // 本地购买逻辑已移除，请使用服务器API
        return false;
    }
    
    [Obsolete("本地库存系统已移除，请使用服务器库存API")]
    public void SetQuickSlotItem(ConsumableCategory category, int slotId, string itemId) 
    { 
        // 本地快捷栏逻辑已移除，请使用服务器API
    }
    
    [Obsolete("本地库存系统已移除，请使用服务器库存API")]
    public void ClearQuickSlotItem(ConsumableCategory category, int slotId, FoodType foodType = FoodType.None) 
    { 
        // 本地快捷栏逻辑已移除，请使用服务器API
    }
    
    [Obsolete("本地库存系统已移除，请使用服务器库存API")]
    public void ToggleAutoSellItem(string itemId) 
    { 
        // 本地自动售卖逻辑已移除，请使用服务器API
    }
    // 战斗相关方法已移除 - 请使用服务器API
    [Obsolete("本地战斗系统已移除，请使用服务器API")]
    public void SetBattleProfession(BattleProfession profession) { /* 本地战斗系统已移除 */ }
    [Obsolete("本地战斗系统已移除，请使用服务器API")]
    public void EquipSkill(string skillId) { /* 本地战斗系统已移除 */ }
    [Obsolete("本地战斗系统已移除，请使用服务器API")]
    public void UnequipSkill(string skillId) { /* 本地战斗系统已移除 */ }
    // 修改为标记已移除
    [Obsolete("本地任务系统已移除，请使用服务器任务API")]
    public void TryCompleteQuest(string questId) 
    { 
        // 本地任务系统已移除，请使用服务器API
    }

    private void StopCurrentAction(Player? character, bool keepTarget = false)
    {
        if (character == null) return;

        // 本地操作停止逻辑已简化 - 主要由服务器控制
        // 仅更新UI状态，实际业务逻辑由服务器处理
        character.CurrentAction = PlayerActionState.Idle;
        character.AttackCooldown = 0;
        character.CurrentEnemy = null;

        NotifyStateChanged();
    }

    public async Task SaveStateAsync(Player character) =>await _characterService.SaveStateAsync(character);
    public async ValueTask DisposeAsync() 
    { 
        // 移除本地游戏循环停止和清理 - 已无需处理
        // StopGameLoop(); // 已移除
        // if (_gameLoopTimer != null) // 已移除
        // { 
        //     _gameLoopTimer.Dispose(); // 已移除
        // }
        
        // 清理客户端组队服务
        if (_clientPartyService != null)
        {
            await _clientPartyService.DisposeAsync();
        }
    }

    // 新的事件系统相关方法
    
    /// <summary>
    /// 订阅游戏事件
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="handler">事件处理器</param>
    public void SubscribeToEvent(GameEventType eventType, Action<GameEventArgs> handler)
    {
        _eventManager.Subscribe(eventType, handler);
    }
    
    /// <summary>
    /// 取消事件订阅
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="handler">处理器</param>
    /// <returns>是否成功取消</returns>
    public bool UnsubscribeFromEvent(GameEventType eventType, Action<GameEventArgs> handler)
    {
        return _eventManager.Unsubscribe(eventType, handler);
    }
    
    /// <summary>
    /// 触发游戏事件
    /// </summary>
    /// <param name="args">事件参数</param>
    public void RaiseEvent(GameEventArgs args)
    {
        _eventManager.Raise(args);
    }
    
    /// <summary>
    /// 触发简单游戏事件
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="player">相关玩家，如果有</param>
    public void RaiseEvent(GameEventType eventType, Player? player = null)
    {
        _eventManager.Raise(new GameEventArgs(eventType, player));
    }
    
    /// <summary>
    /// 触发战斗相关事件
    /// </summary>
    public void RaiseCombatEvent(
        GameEventType eventType, 
        Player? player = null, 
        Enemy? enemy = null, 
        int? damage = null, 
        Skill? skill = null, 
        Party? party = null)
    {
        _eventManager.Raise(new CombatEventArgs(eventType, player, enemy, damage, skill, party));
    }
    
    /// <summary>
    /// 触发物品相关事件
    /// </summary>
    public void RaiseItemEvent(
        GameEventType eventType,
        Player? player = null,
        string? itemId = null,
        Item? item = null,
        int quantity = 1,
        int? goldChange = null,
        EquipmentSlot? slot = null)
    {
        _eventManager.Raise(new ItemEventArgs(eventType, player, itemId, item, quantity, goldChange, slot));
    }
    
    /// <summary>
    /// 触发任务相关事件
    /// </summary>
    public void RaiseQuestEvent(
        GameEventType eventType,
        Player? player = null,
        string? questId = null,
        Quest? quest = null,
        int? progress = null,
        bool isDaily = true)
    {
        _eventManager.Raise(new QuestEventArgs(eventType, player, questId, quest, progress, isDaily));
    }
    
    // 保留原有的NotifyStateChanged方法作为兼容层
    private void NotifyStateChanged() => RaiseEvent(GameEventType.GenericStateChanged);

    // 在GameStateService中添加委托方法

    public void AddBattleXP(BattleProfession profession, int amount)
    {
        if (ActiveCharacter != null)
        {
            _characterService.AddBattleXP(ActiveCharacter, profession, amount);
            
            // 在GameStateService中处理事件触发
            if (ActiveCharacter.GetLevel(profession) > 1) // 假设刚刚升级
            {
                RaiseEvent(GameEventType.LevelUp, ActiveCharacter);
            }
            
            RaiseEvent(GameEventType.ExperienceGained, ActiveCharacter);
        }
    }

    public void AddGatheringXP(GatheringProfession profession, int amount)
    {
        if (ActiveCharacter != null)
        {
            _characterService.AddGatheringXP(ActiveCharacter, profession, amount);
            RaiseEvent(GameEventType.ExperienceGained, ActiveCharacter);
        }
    }

    public void AddProductionXP(ProductionProfession profession, int amount)
    {
        if (ActiveCharacter != null)
        {
            _characterService.AddProductionXP(ActiveCharacter, profession, amount);
            RaiseEvent(GameEventType.ExperienceGained, ActiveCharacter);
        }
    }
}