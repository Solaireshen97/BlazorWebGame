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
using System.Timers;
using System.Diagnostics; // 添加用于性能监控


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
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
    private System.Timers.Timer? _gameLoopTimer;
    private const int GameLoopIntervalMs = 100;
    private const double RevivalDuration = 2;

    // 性能监控相关字段
    private readonly Stopwatch _gameLoopStopwatch = new();
    private long _maxLoopTime = 0;
    private long _totalLoopTime = 0;
    private int _loopCount = 0;
    private DateTime _lastPerformanceLog = DateTime.UtcNow;

    // 新增事件管理器
    private readonly GameEventManager _eventManager = new();

    public List<Player> AllCharacters => _characterService.AllCharacters;
    public Player? ActiveCharacter => _characterService.ActiveCharacter;

    public List<Party> Parties => _partyService.Parties;
    public List<Enemy> AvailableMonsters => MonsterTemplates.All;
    public List<GatheringNode> AvailableGatheringNodes => GatheringData.AllNodes;
    public const int MaxEquippedSkills = 4;
    public List<Quest> DailyQuests => _questService.DailyQuests;
    public List<Quest> WeeklyQuests => _questService.WeeklyQuests;

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

        // 启动游戏循环
        StartGameLoop();
        
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
    /// 游戏循环主入口点
    /// </summary>
    private void GameLoopTick(object? sender, ElapsedEventArgs e)
    {
        try
        {
            // 启动性能监控
            _gameLoopStopwatch.Restart();

            // 计算上次循环后经过的时间
            double elapsedSeconds = GameLoopIntervalMs / 1000.0;

            // 更新全局游戏状态
            UpdateGlobalGameState(elapsedSeconds);

            // 处理所有活跃战斗 - 添加这一行
            _combatService.ProcessAllBattles(elapsedSeconds);

            // 更新所有角色
            foreach (var character in AllCharacters)
            {
                UpdateCharacter(character, elapsedSeconds);
            }

            // 触发UI更新
            NotifyStateChanged();

            // 停止性能监控并记录数据
            _gameLoopStopwatch.Stop();
            RecordPerformanceMetrics();
        }
        catch (Exception ex)
        {
            // 记录错误但不中断游戏循环
            LogError(ex);
        }
    }

    /// <summary>
    /// 更新全局游戏状态
    /// </summary>
    private void UpdateGlobalGameState(double elapsedSeconds)
    {
        // 检查任务重置
        _questService.CheckAndResetDailyQuests();
        _questService.CheckAndResetWeeklyQuests();
        
        // 这里可以添加其他全局状态更新，如游戏时间、世界事件等
    }
    
    /// <summary>
    /// 更新单个角色的状态
    /// </summary>
    private void UpdateCharacter(Player character, double elapsedSeconds)
    {
        if (character == null) return;
        
        try
        {
            // 更新buff和消耗品冷却
            _characterService.UpdateBuffs(character, elapsedSeconds);
            _inventoryService.UpdateConsumableCooldowns(character, elapsedSeconds);
            
            // 处理死亡状态
            if (character.IsDead)
            {
                ProcessDeadCharacter(character, elapsedSeconds);
                return;
            }
            
            // 处理活跃状态
            ProcessActiveCharacter(character, elapsedSeconds);
            
            // 处理自动消耗品
            _inventoryService.ProcessAutoConsumables(character);
        }
        catch (Exception ex)
        {
            // 记录单个角色处理错误，但继续处理其他角色
            LogError(ex, $"处理角色 {character.Name} 时发生错误");
        }
    }
    
    /// <summary>
    /// 处理死亡角色的逻辑
    /// </summary>
    private void ProcessDeadCharacter(Player character, double elapsedSeconds)
    {
        // 更新复活倒计时
        character.RevivalTimeRemaining -= elapsedSeconds;
        
        // 检查是否可以复活
        if (character.RevivalTimeRemaining <= 0)
        {
            _combatService.ReviveCharacter(character);
        }
    }
    
    /// <summary>
    /// 处理活跃角色的逻辑
    /// </summary>
    private void ProcessActiveCharacter(Player character, double elapsedSeconds)
    {
        var actionState = character.CurrentAction.ToString();
        
        if (character.CurrentAction == PlayerActionState.Combat)
        {
            ProcessCombatState(character, elapsedSeconds);
        }
        else if (actionState.StartsWith("Gathering"))
        {
            _professionService.ProcessGathering(character, elapsedSeconds);
        }
        else if (actionState.StartsWith("Crafting"))
        {
            _professionService.ProcessCrafting(character, elapsedSeconds);
        }
        else if (character.CurrentAction == PlayerActionState.Idle)
        {
            // 空闲状态无需特殊处理
        }
        else
        {
            LogWarning($"未处理的角色状态: {character.CurrentAction} 用于角色 {character.Name}");
        }
    }

    /// <summary>
    /// 处理战斗状态
    /// </summary>
    private void ProcessCombatState(Player character, double elapsedSeconds)
    {
        // 无需再检查角色是否在新战斗系统中
        // 所有战斗都会由CombatService.ProcessAllBattles统一处理
        
        // 只处理那些不在新战斗系统中的角色
        var combatService = ServiceLocator.GetService<CombatService>();
        var battleContext = combatService?.GetBattleContextForPlayer(character.Id);
        
        if (battleContext == null && character.CurrentAction == PlayerActionState.Combat)
        {
            var party = GetPartyForCharacter(character.Id);
            _combatService.ProcessCombat(character, elapsedSeconds, party);
        }
        // 新战斗系统的处理在CombatService.ProcessAllBattles中完成
    }

    /// <summary>
    /// 记录性能指标
    /// </summary>
    private void RecordPerformanceMetrics()
    {
        long elapsedMs = _gameLoopStopwatch.ElapsedMilliseconds;
        
        // 更新统计信息
        _maxLoopTime = Math.Max(_maxLoopTime, elapsedMs);
        _totalLoopTime += elapsedMs;
        _loopCount++;
        
        // 每分钟记录一次性能日志
        if ((DateTime.UtcNow - _lastPerformanceLog).TotalMinutes >= 1)
        {
            double avgLoopTime = (double)_totalLoopTime / Math.Max(1, _loopCount);
            
            // 记录性能统计
            Console.WriteLine($"游戏循环性能统计: 平均={avgLoopTime:F2}ms, 最大={_maxLoopTime}ms, 目标={GameLoopIntervalMs}ms");
            
            // 如果平均执行时间接近或超过循环间隔，发出警告
            if (avgLoopTime > GameLoopIntervalMs * 0.8)
            {
                LogWarning($"游戏循环执行时间接近或超过目标间隔: {avgLoopTime:F2}ms > {GameLoopIntervalMs * 0.8:F2}ms");
            }
            
            // 重置统计
            _maxLoopTime = 0;
            _totalLoopTime = 0;
            _loopCount = 0;
            _lastPerformanceLog = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// 记录错误信息
    /// </summary>
    private void LogError(Exception ex, string? context = null)
    {
        string message = context != null ? $"{context}: {ex.Message}" : ex.Message;
        Console.WriteLine($"游戏循环错误: {message}");
        Console.WriteLine(ex.StackTrace);
        
        // 这里可以添加更复杂的日志记录，如保存到文件或发送到服务器
    }
    
    /// <summary>
    /// 记录警告信息
    /// </summary>
    private void LogWarning(string message)
    {
        Console.WriteLine($"游戏循环警告: {message}");
    }
    
    /// <summary>
    /// 启动游戏循环
    /// </summary>
    private void StartGameLoop()
    {
        _gameLoopTimer = new System.Timers.Timer(GameLoopIntervalMs);
        _gameLoopTimer.Elapsed += GameLoopTick;
        _gameLoopTimer.AutoReset = true;
        _gameLoopTimer.Start();
        
        Console.WriteLine($"游戏循环已启动，间隔: {GameLoopIntervalMs}ms");
    }
    
    /// <summary>
    /// 停止游戏循环
    /// </summary>
    private void StopGameLoop()
    {
        if (_gameLoopTimer != null)
        {
            _gameLoopTimer.Stop();
            _gameLoopTimer.Elapsed -= GameLoopTick;
            Console.WriteLine("游戏循环已停止");
        }
    }


    // 删除原来的GetPartyForCharacter方法，改用PartyService的方法
    public Party? GetPartyForCharacter(string characterId)
    {
        return _partyService.GetPartyForCharacter(characterId);
    }

    /// <summary>
    /// 使用当前激活的角色创建一个新队伍，该角色将成为队长。
    /// 优先使用服务端组队服务，如果不可用则回退到客户端服务
    /// </summary>
    public async Task<bool> CreatePartyAsync()
    {
        if (ActiveCharacter == null) return false;

        // 优先使用服务端组队服务
        if (_clientPartyService != null && await IsServerAvailableAsync())
        {
            return await _clientPartyService.CreatePartyAsync(ActiveCharacter.Id);
        }
        else
        {
            // 回退到客户端组队服务
            return _partyService.CreateParty(ActiveCharacter);
        }
    }

    /// <summary>
    /// 让当前激活的角色加入一个指定的队伍。
    /// 优先使用服务端组队服务，如果不可用则回退到客户端服务
    /// </summary>
    /// <param name="partyId">要加入的队伍的ID</param>
    public async Task<bool> JoinPartyAsync(Guid partyId)
    {
        if (ActiveCharacter == null) return false;

        // 优先使用服务端组队服务
        if (_clientPartyService != null && await IsServerAvailableAsync())
        {
            return await _clientPartyService.JoinPartyAsync(ActiveCharacter.Id, partyId);
        }
        else
        {
            // 回退到客户端组队服务
            return _partyService.JoinParty(ActiveCharacter, partyId);
        }
    }

    /// <summary>
    /// 让当前激活的角色离开他所在的队伍。
    /// 优先使用服务端组队服务，如果不可用则回退到客户端服务
    /// </summary>
    public async Task<bool> LeavePartyAsync()
    {
        if (ActiveCharacter == null) return false;

        // 优先使用服务端组队服务
        if (_clientPartyService != null && await IsServerAvailableAsync())
        {
            return await _clientPartyService.LeavePartyAsync(ActiveCharacter.Id);
        }
        else
        {
            // 回退到客户端组队服务
            return _partyService.LeaveParty(ActiveCharacter);
        }
    }

    /// <summary>
    /// 检查服务器是否可用
    /// </summary>
    private async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            var gameApiService = _serviceProvider.GetService<GameApiService>();
            return gameApiService != null && await gameApiService.IsServerAvailableAsync();
        }
        catch
        {
            return false;
        }
    }

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

    #region 兼容性方法 - 同步版本

    /// <summary>
    /// 使用当前激活的角色创建一个新队伍（同步版本，向后兼容）
    /// </summary>
    public void CreateParty()
    {
        if (ActiveCharacter != null)
        {
            // 尝试异步调用，但不等待结果
            _ = Task.Run(async () => await CreatePartyAsync());
        }
    }

    /// <summary>
    /// 让当前激活的角色加入一个指定的队伍（同步版本，向后兼容）
    /// </summary>
    /// <param name="partyId">要加入的队伍的ID</param>
    public void JoinParty(Guid partyId)
    {
        if (ActiveCharacter != null)
        {
            // 尝试异步调用，但不等待结果
            _ = Task.Run(async () => await JoinPartyAsync(partyId));
        }
    }

    /// <summary>
    /// 让当前激活的角色离开他所在的队伍（同步版本，向后兼容）
    /// </summary>
    public void LeaveParty()
    {
        if (ActiveCharacter != null)
        {
            // 尝试异步调用，但不等待结果
            _ = Task.Run(async () => await LeavePartyAsync());
        }
    }

    #endregion

    public async Task StartCombatAsync(Enemy enemyTemplate)
    {
        if (ActiveCharacter == null) return;

        // 检查是否可以使用服务端战斗系统
        if (_clientPartyService != null && await IsServerAvailableAsync())
        {
            var gameApiService = _serviceProvider.GetService<GameApiService>();
            var clientGameStateService = _serviceProvider.GetService<ClientGameStateService>();
            
            if (gameApiService != null && clientGameStateService != null)
            {
                // 获取组队信息
                string? partyId = null;
                if (_clientPartyService.IsInParty(ActiveCharacter.Id))
                {
                    partyId = _clientPartyService.CurrentParty?.Id.ToString();
                }

                // 启动服务端战斗
                await clientGameStateService.StartBattleAsync(enemyTemplate.Name, partyId);
                return;
            }
        }

        // 回退到客户端战斗系统
        var party = GetPartyForCharacter(ActiveCharacter.Id);
        _combatService.SmartStartBattle(ActiveCharacter, enemyTemplate, party);
    }

    public void StartCombat(Enemy enemyTemplate)
    {
        // 同步版本，向后兼容
        _ = Task.Run(async () => await StartCombatAsync(enemyTemplate));
    }
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

        // 检查是否在新战斗系统中
        var battleContext = _combatService.GetBattleContextForPlayer(character.Id);
        if (battleContext != null)
        {
            // 停止新战斗系统的战斗
            _combatService.StopBattle(battleContext);
            NotifyStateChanged();
            return;
        }

        // 新增：检查是否在战斗刷新状态
        if (_combatService.IsPlayerInBattleRefresh(character.Id))
        {
            // 取消玩家的战斗刷新状态
            _combatService.CancelPlayerBattleRefresh(character.Id);

            // 确保角色状态为空闲
            character.CurrentAction = PlayerActionState.Idle;
            character.CurrentEnemy = null;
            character.AttackCooldown = 0;

            NotifyStateChanged();
            return;
        }

        // 获取当前状态的字符串表示
        var actionState = character.CurrentAction.ToString();

        // 如果不在战斗中，可能在进行专业活动
        if (actionState.StartsWith("Gathering") || actionState.StartsWith("Crafting"))
        {
            _professionService.StopCurrentAction(character);
        }

        // 确保角色状态为空闲
        character.CurrentAction = PlayerActionState.Idle;
        character.CurrentEnemy = null;
        character.AttackCooldown = 0;

        NotifyStateChanged();
    }

    public async Task SaveStateAsync(Player character) =>await _characterService.SaveStateAsync(character);
    public async ValueTask DisposeAsync() 
    { 
        StopGameLoop(); 
        if (_gameLoopTimer != null) 
        { 
            _gameLoopTimer.Dispose(); 
        }
        
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