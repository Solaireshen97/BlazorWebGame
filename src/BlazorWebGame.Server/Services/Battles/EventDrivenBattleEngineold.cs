using BlazorWebGame.Shared.Events;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using BlazorWebGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using BlazorWebGame.Server.Services.GameSystem;
using System.Runtime.InteropServices;

namespace BlazorWebGame.Server.Services.Battles;

/// <summary>
/// 基于事件队列的服务端战斗引擎
/// 优化的战斗逻辑，使用统一事件队列处理所有战斗相关事件
/// </summary>
public class EventDrivenBattleEngineold : IDisposable
{
    private readonly UnifiedEventService _eventService;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<EventDrivenBattleEngineold> _logger;
    
    // 活跃战斗实例
    private readonly Dictionary<Guid, EventDrivenBattleContext> _activeBattles = new();
    
    // 批量事件处理缓冲区
    private readonly UnifiedEvent[] _eventBuffer = new UnifiedEvent[256];
    
    // 性能统计
    private long _totalEventsProcessed = 0;
    private readonly object _statsLock = new();

    public EventDrivenBattleEngineold(
        UnifiedEventService eventService,
        IHubContext<GameHub> hubContext,
        ILogger<EventDrivenBattleEngineold> logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 注册战斗事件处理器
        RegisterBattleEventHandlers();
    }

    /// <summary>
    /// 注册战斗事件处理器
    /// </summary>
    private void RegisterBattleEventHandlers()
    {
        _eventService.RegisterHandler(GameEventTypes.BATTLE_ATTACK, new BattleAttackHandler(this, _logger));
        _eventService.RegisterHandler(GameEventTypes.BATTLE_HEAL, new BattleHealHandler(this, _logger));
        _eventService.RegisterHandler(GameEventTypes.SKILL_USED, new SkillUsageHandler(this, _logger));
        _eventService.RegisterHandler(GameEventTypes.BATTLE_TICK, new BattleTickHandler(this, _logger));
    }

    /// <summary>
    /// 开始新战斗
    /// </summary>
    public async Task<EventDrivenBattleContext> StartBattleAsync(BattleStartRequest request)
    {
        var battleId = Guid.NewGuid();
        var battle = new EventDrivenBattleContext
        {
            BattleId = battleId,
            BattleType = (BattleType)Enum.Parse(typeof(BattleType), request.BattleType.ToString()),
            PartyId = request.PartyId,
            DungeonId = request.DungeonId,
            State = BattleState.Active,
            StartTime = DateTime.UtcNow,
            Players = await GetPlayersForBattleAsync(request),
            Enemies = GenerateEnemiesForBattle(request),
            EventMetrics = new BattleEventMetrics()
        };

        _activeBattles[battleId] = battle;

        // 发送战斗开始事件
        _eventService.EnqueueEvent(GameEventTypes.BATTLE_STARTED, EventPriority.Gameplay, (ulong)battleId.GetHashCode());

        _logger.LogInformation("Started event-driven battle {BattleId} with {PlayerCount} players", 
            battleId, battle.Players.Count);

        return battle;
    }

    /// <summary>
    /// 处理战斗帧更新 - 高性能批量处理
    /// </summary>
    public async Task ProcessBattleFrameAsync(double deltaTimeSeconds)
    {
        if (!_activeBattles.Any()) return;

        var frameStartTime = DateTime.UtcNow;
        var processedEvents = 0;

        // 为每个活跃战斗生成tick事件
        foreach (var battle in _activeBattles.Values.Where(b => b.State == BattleState.Active))
        {
            // 生成战斗tick事件
            var tickData = new BattleTickEventData
            {
                BattleId = (ulong)battle.BattleId.GetHashCode(),
                DeltaTime = (float)deltaTimeSeconds,
                Frame = (uint)_eventService.EventQueue.CurrentFrame
            };

            _eventService.EnqueueEvent(GameEventTypes.BATTLE_TICK, tickData, EventPriority.Gameplay,
                (ulong)battle.BattleId.GetHashCode());

            // 处理玩家攻击冷却
            foreach (var player in battle.Players.Where(p => p.IsAlive))
            {
                if (player.AttackCooldown > 0)
                {
                    player.AttackCooldown -= deltaTimeSeconds;
                    if (player.AttackCooldown <= 0 && player.CurrentTarget != null)
                    {
                        // 生成攻击事件
                        await EnqueuePlayerAttackAsync(battle, player);
                    }
                }
            }

            // 处理敌人攻击冷却
            foreach (var enemy in battle.Enemies.Where(e => e.IsAlive))
            {
                if (enemy.AttackCooldown > 0)
                {
                    enemy.AttackCooldown -= deltaTimeSeconds;
                    if (enemy.AttackCooldown <= 0)
                    {
                        // 生成敌人攻击事件
                        await EnqueueEnemyAttackAsync(battle, enemy);
                    }
                }
            }

            processedEvents++;
        }

        // 统计性能指标
        var processingTime = (DateTime.UtcNow - frameStartTime).TotalMilliseconds;
        if (processingTime > 5.0) // 如果帧处理时间超过5ms，记录警告
        {
            _logger.LogWarning("Battle frame processing took {ProcessingTime}ms for {BattleCount} battles", 
                processingTime, _activeBattles.Count);
        }

        lock (_statsLock)
        {
            _totalEventsProcessed += processedEvents;
        }
    }

    /// <summary>
    /// 入队玩家攻击事件
    /// </summary>
    private async Task EnqueuePlayerAttackAsync(EventDrivenBattleContext battle, EventDrivenBattlePlayer player)
    {
        if (player.CurrentTarget == null) return;

        var target = battle.Enemies.FirstOrDefault(e => e.Id == player.CurrentTarget);
        if (target == null || !target.IsAlive) return;

        // 计算伤害
        var baseDamage = CalculatePlayerDamage(player);
        var isCritical = Random.Shared.NextDouble() < player.CriticalChance;
        var actualDamage = isCritical ? (int)(baseDamage * player.CriticalMultiplier) : baseDamage;

        var attackData = new BattleAttackEventData
        {
            BaseDamage = baseDamage,
            ActualDamage = actualDamage,
            SkillId = player.CurrentSkillId,
            IsCritical = (byte)(isCritical ? 1 : 0),
            AttackType = 1, // Physical attack
            CritMultiplier = player.CriticalMultiplier,
            RemainingHealth = Math.Max(0, target.Health - actualDamage),
            StatusEffect = 0
        };

        _eventService.EnqueueEvent(GameEventTypes.BATTLE_ATTACK, attackData, EventPriority.Gameplay,
            (ulong)player.Id.GetHashCode(), (ulong)target.Id.GetHashCode());

        // 重置攻击冷却
        player.AttackCooldown = 1.0 / player.AttacksPerSecond;
        
        battle.EventMetrics.TotalAttacks++;
    }

    /// <summary>
    /// 入队敌人攻击事件
    /// </summary>
    private async Task EnqueueEnemyAttackAsync(EventDrivenBattleContext battle, EventDrivenBattleEnemy enemy)
    {
        // 选择目标玩家
        var alivePlayers = battle.Players.Where(p => p.IsAlive).ToList();
        if (!alivePlayers.Any()) return;

        var target = alivePlayers[Random.Shared.Next(alivePlayers.Count)];
        
        // 计算伤害
        var baseDamage = CalculateEnemyDamage(enemy);
        var actualDamage = ApplyPlayerDefense(target, baseDamage);

        var attackData = new BattleAttackEventData
        {
            BaseDamage = baseDamage,
            ActualDamage = actualDamage,
            SkillId = enemy.CurrentSkillId,
            IsCritical = 0,
            AttackType = 2, // Enemy attack
            CritMultiplier = 1.0f,
            RemainingHealth = Math.Max(0, target.Health - actualDamage),
            StatusEffect = 0
        };

        _eventService.EnqueueEvent(GameEventTypes.BATTLE_ATTACK, attackData, EventPriority.Gameplay,
            (ulong)enemy.Id.GetHashCode(), (ulong)target.Id.GetHashCode());

        // 重置攻击冷却
        enemy.AttackCooldown = 1.0 / enemy.AttacksPerSecond;
        
        battle.EventMetrics.TotalAttacks++;
    }

    /// <summary>
    /// 计算玩家伤害
    /// </summary>
    private int CalculatePlayerDamage(EventDrivenBattlePlayer player)
    {
        var baseDamage = player.AttackPower;
        var variance = (int)(baseDamage * 0.1); // 10% 伤害浮动
        return baseDamage + Random.Shared.Next(-variance, variance + 1);
    }

    /// <summary>
    /// 计算敌人伤害
    /// </summary>
    private int CalculateEnemyDamage(EventDrivenBattleEnemy enemy)
    {
        var baseDamage = enemy.AttackPower;
        var variance = (int)(baseDamage * 0.15); // 15% 伤害浮动
        return baseDamage + Random.Shared.Next(-variance, variance + 1);
    }

    /// <summary>
    /// 应用玩家防御
    /// </summary>
    private int ApplyPlayerDefense(EventDrivenBattlePlayer player, int incomingDamage)
    {
        var defense = player.Defense;
        var damageReduction = defense / (defense + 100.0); // 防御减伤公式
        return Math.Max(1, (int)(incomingDamage * (1.0 - damageReduction)));
    }

    /// <summary>
    /// 获取参战玩家
    /// </summary>
    private async Task<List<EventDrivenBattlePlayer>> GetPlayersForBattleAsync(BattleStartRequest request)
    {
        // TODO: 从数据库或服务获取玩家数据
        // 这里返回一个简单的测试玩家
        var players = new List<EventDrivenBattlePlayer>();
        
        if (!string.IsNullOrEmpty(request.PlayerId))
        {
            players.Add(new EventDrivenBattlePlayer
            {
                Id = request.PlayerId,
                Name = "测试玩家",
                Level = 1,
                Health = 100,
                MaxHealth = 100,
                AttackPower = 20,
                Defense = 10,
                AttacksPerSecond = 1.0,
                CriticalChance = 0.05,
                CriticalMultiplier = 1.5f,
                IsAlive = true,
                AttackCooldown = 0,
                CurrentSkillId = 1
            });
        }

        return players;
    }

    /// <summary>
    /// 生成战斗敌人
    /// </summary>
    private List<EventDrivenBattleEnemy> GenerateEnemiesForBattle(BattleStartRequest request)
    {
        var enemies = new List<EventDrivenBattleEnemy>();
        
        enemies.Add(new EventDrivenBattleEnemy
        {
            Id = Guid.NewGuid().ToString(),
            Name = "测试敌人",
            Level = 1,
            Health = 80,
            MaxHealth = 80,
            AttackPower = 15,
            Defense = 5,
            AttacksPerSecond = 0.8,
            IsAlive = true,
            AttackCooldown = 0,
            CurrentSkillId = 0
        });

        return enemies;
    }

    /// <summary>
    /// 获取战斗统计信息
    /// </summary>
    public BattleEngineStatsold GetStatistics()
    {
        lock (_statsLock)
        {
            return new BattleEngineStatsold
            {
                ActiveBattles = _activeBattles.Count,
                TotalEventsProcessed = _totalEventsProcessed,
                AverageEventsPerBattle = _activeBattles.Count > 0 ? _totalEventsProcessed / _activeBattles.Count : 0
            };
        }
    }

    /// <summary>
    /// 获取指定战斗的详细信息
    /// </summary>
    public EventDrivenBattleContext? GetBattle(Guid battleId)
    {
        return _activeBattles.TryGetValue(battleId, out var battle) ? battle : null;
    }

    public void Dispose()
    {
        _activeBattles.Clear();
    }
}

/// <summary>
/// 基于事件的战斗上下文
/// </summary>
public class EventDrivenBattleContext
{
    public Guid BattleId { get; set; }
    public BattleType BattleType { get; set; }
    public Guid? PartyId { get; set; }
    public string? DungeonId { get; set; }
    public BattleState State { get; set; }
    public DateTime StartTime { get; set; }
    public List<EventDrivenBattlePlayer> Players { get; set; } = new();
    public List<EventDrivenBattleEnemy> Enemies { get; set; } = new();
    public BattleEventMetrics EventMetrics { get; set; } = new();
    public bool IsVictory { get; set; }
}

/// <summary>
/// 基于事件的战斗玩家
/// </summary>
public class EventDrivenBattlePlayer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int AttackPower { get; set; }
    public int Defense { get; set; }
    public double AttacksPerSecond { get; set; }
    public double CriticalChance { get; set; }
    public float CriticalMultiplier { get; set; }
    public bool IsAlive { get; set; }
    public double AttackCooldown { get; set; }
    public string? CurrentTarget { get; set; }
    public ushort CurrentSkillId { get; set; }
}

/// <summary>
/// 基于事件的战斗敌人
/// </summary>
public class EventDrivenBattleEnemy
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int AttackPower { get; set; }
    public int Defense { get; set; }
    public double AttacksPerSecond { get; set; }
    public bool IsAlive { get; set; }
    public double AttackCooldown { get; set; }
    public ushort CurrentSkillId { get; set; }
}

/// <summary>
/// 战斗事件指标
/// </summary>
public class BattleEventMetrics
{
    public long TotalEvents { get; set; }
    public long TotalAttacks { get; set; }
    public long TotalHeals { get; set; }
    public long TotalSkillsUsed { get; set; }
    public double AverageEventProcessingTime { get; set; }
}

/// <summary>
/// 战斗引擎统计信息
/// </summary>
public struct BattleEngineStatsold
{
    public int ActiveBattles;
    public long TotalEventsProcessed;
    public long AverageEventsPerBattle;
}

/// <summary>
/// 战斗tick事件数据
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BattleTickEventData
{
    public ulong BattleId;       // 8 bytes
    public float DeltaTime;      // 4 bytes
    public uint Frame;           // 4 bytes
    // Total: 16 bytes (fits in 28-byte limit)
}

/// <summary>
/// 战斗状态枚举
/// </summary>
public enum BattleState
{
    Preparing,
    Active,
    Paused,
    Completed,
    Cancelled
}

/// <summary>
/// 战斗类型枚举
/// </summary>
public enum BattleType
{
    Normal,
    Boss,
    Dungeon,
    Raid,
    PvP
}

/// <summary>
/// 战斗攻击事件处理器
/// </summary>
public class BattleAttackHandler : IUnifiedEventHandler
{
    private readonly EventDrivenBattleEngineold _battleEngine;
    private readonly ILogger _logger;

    public BattleAttackHandler(EventDrivenBattleEngineold battleEngine, ILogger logger)
    {
        _battleEngine = battleEngine;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        var attackData = evt.GetData<BattleAttackEventData>();
        // 简化的ID生成方法，生成16字节的GUID
        var bytes = new byte[16];
        var hash = (uint)evt.ActorId;
        Array.Copy(BitConverter.GetBytes(hash), 0, bytes, 0, 4);
        var battleId = new Guid(bytes);
        
        var battle = _battleEngine.GetBattle(battleId);
        if (battle == null) return;

        // 应用伤害
        var targetId = evt.TargetId.ToString();
        
        // 查找目标并应用伤害
        var targetEnemy = battle.Enemies.FirstOrDefault(e => e.Id.GetHashCode() == (int)evt.TargetId);
        var targetPlayer = battle.Players.FirstOrDefault(p => p.Id.GetHashCode() == (int)evt.TargetId);

        if (targetEnemy != null)
        {
            targetEnemy.Health = Math.Max(0, targetEnemy.Health - attackData.ActualDamage);
            if (targetEnemy.Health <= 0)
            {
                targetEnemy.IsAlive = false;
                // 生成敌人死亡事件
                // TODO: 实现敌人死亡逻辑
            }
        }
        else if (targetPlayer != null)
        {
            targetPlayer.Health = Math.Max(0, targetPlayer.Health - attackData.ActualDamage);
            if (targetPlayer.Health <= 0)
            {
                targetPlayer.IsAlive = false;
                // 生成玩家死亡事件
                // TODO: 实现玩家死亡逻辑
            }
        }

        battle.EventMetrics.TotalEvents++;
    }
}

/// <summary>
/// 战斗治疗事件处理器
/// </summary>
public class BattleHealHandler : IUnifiedEventHandler
{
    private readonly EventDrivenBattleEngineold _battleEngine;
    private readonly ILogger _logger;

    public BattleHealHandler(EventDrivenBattleEngineold battleEngine, ILogger logger)
    {
        _battleEngine = battleEngine;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        // TODO: 实现治疗逻辑
        await Task.CompletedTask;
    }
}

/// <summary>
/// 技能使用事件处理器
/// </summary>
public class SkillUsageHandler : IUnifiedEventHandler
{
    private readonly EventDrivenBattleEngineold _battleEngine;
    private readonly ILogger _logger;

    public SkillUsageHandler(EventDrivenBattleEngineold battleEngine, ILogger logger)
    {
        _battleEngine = battleEngine;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        // TODO: 实现技能使用逻辑
        await Task.CompletedTask;
    }
}

/// <summary>
/// 战斗tick事件处理器
/// </summary>
public class BattleTickHandler : IUnifiedEventHandler
{
    private readonly EventDrivenBattleEngineold _battleEngine;
    private readonly ILogger _logger;

    public BattleTickHandler(EventDrivenBattleEngineold battleEngine, ILogger logger)
    {
        _battleEngine = battleEngine;
        _logger = logger;
    }

    public async Task HandleAsync(UnifiedEvent evt)
    {
        var tickData = evt.GetData<BattleTickEventData>();
        // 简化的ID生成方法，生成16字节的GUID
        var bytes = new byte[16];
        var hash = (uint)tickData.BattleId;
        Array.Copy(BitConverter.GetBytes(hash), 0, bytes, 0, 4);
        var battleId = new Guid(bytes);
        
        var battle = _battleEngine.GetBattle(battleId);
        if (battle == null) return;

        // 处理战斗tick逻辑，如状态效果更新、回血等
        // TODO: 实现详细的tick逻辑
        
        battle.EventMetrics.TotalEvents++;
        await Task.CompletedTask;
    }
}