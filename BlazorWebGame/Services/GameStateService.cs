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
using System.Diagnostics; // 添加用于性能监控

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

    // 性能监控相关字段
    private readonly Stopwatch _gameLoopStopwatch = new();
    private long _maxLoopTime = 0;
    private long _totalLoopTime = 0;
    private int _loopCount = 0;
    private DateTime _lastPerformanceLog = DateTime.UtcNow;

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
        StartGameLoop();
    }
    private T GetService<T>() where T : class => _serviceProvider.GetService<T>() ?? throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册");


    public void SetActiveCharacter(string characterId) =>_characterService.SetActiveCharacter(characterId);

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
        switch (character.CurrentAction)
        {
            case PlayerActionState.Combat:
                ProcessCombatState(character, elapsedSeconds);
                break;
                
            case PlayerActionState.Gathering:
                _professionService.ProcessGathering(character, elapsedSeconds);
                break;
                
            case PlayerActionState.Crafting:
                _professionService.ProcessCrafting(character, elapsedSeconds);
                break;
                
            case PlayerActionState.Idle:
                // 空闲状态无需特殊处理
                break;
                
            default:
                LogWarning($"未处理的角色状态: {character.CurrentAction} 用于角色 {character.Name}");
                break;
        }
    }
    
    /// <summary>
    /// 处理战斗状态
    /// </summary>
    private void ProcessCombatState(Player character, double elapsedSeconds)
    {
        var party = GetPartyForCharacter(character.Id);
        _combatService.ProcessCombat(character, elapsedSeconds, party);
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
    public async ValueTask DisposeAsync() { StopGameLoop(); if (_gameLoopTimer != null) { _gameLoopTimer.Dispose(); } }
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}