using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using BlazorWebGame.Server.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using BlazorWebGame.Server.Services.Character;
using BlazorWebGame.Server.Services.Equipments;
using BlazorWebGame.Server.Services.Skill;

namespace BlazorWebGame.Server.Services.Battle;

/// <summary>
/// 服务端战斗管理器 - 从客户端迁移而来
/// 处理战斗实例的创建、管理和生命周期
/// </summary>
public class ServerBattleManager
{
    private readonly Dictionary<Guid, ServerBattleContext> _activeBattles = new();
    private readonly List<ServerBattlePlayer> _allCharacters;
    private readonly ServerCombatEngine _combatEngine;
    private readonly ServerBattleFlowService _battleFlowService;
    private readonly ServerCharacterService _characterService;
    private readonly ServerSkillSystem _skillSystem;
    private readonly ServerLootService _lootService;
    private readonly ILogger<ServerBattleManager> _logger;
    private readonly IHubContext<GameHub> _hubContext;

    /// <summary>
    /// 状态变更事件
    /// </summary>
    public event Action? OnStateChanged;

    public ServerBattleManager(
        List<ServerBattlePlayer> allCharacters,
        ServerCombatEngine combatEngine,
        ServerBattleFlowService battleFlowService,
        ServerCharacterService characterService,
        ServerSkillSystem skillSystem,
        ServerLootService lootService,
        ILogger<ServerBattleManager> logger,
        IHubContext<GameHub> hubContext)
    {
        _allCharacters = allCharacters;
        _combatEngine = combatEngine;
        _battleFlowService = battleFlowService;
        _characterService = characterService;
        _skillSystem = skillSystem;
        _lootService = lootService;
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// 获取活跃战斗上下文
    /// </summary>
    public ServerBattleContext? GetBattleContextForPlayer(string playerId)
    {
        return _activeBattles.Values.FirstOrDefault(b => b.Players.Any(p => p.Id == playerId));
    }

    /// <summary>
    /// 获取活跃战斗上下文
    /// </summary>
    public ServerBattleContext? GetBattleContextForParty(Guid partyId)
    {
        return _activeBattles.Values.FirstOrDefault(b => b.PartyId == partyId);
    }

    /// <summary>
    /// 处理所有活跃战斗
    /// </summary>
    public async Task ProcessAllBattlesAsync(double elapsedSeconds)
    {
        var battlesToRemove = new List<Guid>();

        // 处理活跃战斗
        foreach (var battle in _activeBattles.Values)
        {
            ProcessBattle(battle, elapsedSeconds);

            // 检查战斗是否结束
            if (battle.State == ServerBattleState.Completed)
            {
                // 收集敌人信息用于战斗刷新
                var enemyInfos = _battleFlowService.CollectEnemyInfos(battle);

                // 重置所有参战玩家的状态
                foreach (var player in battle.Players)
                {
                    player.CurrentAction = "Idle";
                    player.CurrentEnemyId = null;
                    player.AttackCooldown = 0;
                }

                // 通知战斗流程服务处理战斗完成
                _battleFlowService.OnBattleCompleted(battle, enemyInfos);

                battlesToRemove.Add(battle.BattleId);

                // 通知客户端战斗结束
                await NotifyBattleCompletedAsync(battle);
            }
        }

        // 移除已完成的战斗
        foreach (var battleId in battlesToRemove)
        {
            _activeBattles.Remove(battleId);
            _logger.LogInformation("Removed completed battle: {BattleId}", battleId);
        }

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 处理单个战斗
    /// </summary>
    private void ProcessBattle(ServerBattleContext battle, double elapsedSeconds)
    {
        if (battle.State != ServerBattleState.Active)
            return;

        // 处理玩家行动
        foreach (var player in battle.Players.Where(p => p.IsAlive))
        {
            _combatEngine.ProcessPlayerAttack(battle, player, elapsedSeconds);
        }

        // 处理敌人行动
        foreach (var enemy in battle.Enemies.Where(e => e.IsAlive))
        {
            _combatEngine.ProcessEnemyAttack(battle, enemy, elapsedSeconds);
        }

        // 检查战斗胜利条件
        CheckBattleVictoryConditions(battle);
    }

    /// <summary>
    /// 检查战斗胜利条件
    /// </summary>
    private void CheckBattleVictoryConditions(ServerBattleContext battle)
    {
        var alivePlayers = battle.Players.Where(p => p.IsAlive).ToList();
        var aliveEnemies = battle.Enemies.Where(e => e.IsAlive).ToList();

        if (!aliveEnemies.Any())
        {
            // 玩家胜利
            battle.State = ServerBattleState.Completed;
            battle.IsVictory = true;
            _logger.LogInformation("Battle {BattleId} completed - Victory!", battle.BattleId);

            // 给予战斗奖励
            GrantBattleRewards(battle);
        }
        else if (!alivePlayers.Any())
        {
            // 玩家失败
            battle.State = ServerBattleState.Completed;
            battle.IsVictory = false;
            _logger.LogInformation("Battle {BattleId} completed - Defeat!", battle.BattleId);
        }
    }

    /// <summary>
    /// 给予战斗奖励
    /// </summary>
    private void GrantBattleRewards(ServerBattleContext battle)
    {
        foreach (var player in battle.Players.Where(p => p.IsAlive))
        {
            // 经验奖励计算
            var expReward = CalculateExperienceReward(battle, player);
            player.Experience += expReward;

            // 检查升级
            _characterService.CheckLevelUp(player);

            // 掉落奖励处理
            var lootRewards = _lootService.GenerateBattleLoot(battle, player);
            // TODO: 将掉落物品添加到玩家背包

            _logger.LogInformation("Player {PlayerId} received {Exp} exp and {Loot} items", 
                player.Id, expReward, lootRewards.Count);
        }
    }

    /// <summary>
    /// 计算经验奖励
    /// </summary>
    private int CalculateExperienceReward(ServerBattleContext battle, ServerBattlePlayer player)
    {
        // 基础经验值
        int baseExp = battle.Enemies.Sum(e => e.Level * 10);
        
        // 等级差异调整
        var avgEnemyLevel = battle.Enemies.Average(e => e.Level);
        var levelDiff = avgEnemyLevel - player.Level;
        var levelMultiplier = Math.Max(0.1, 1.0 + levelDiff * 0.1);
        
        return (int)(baseExp * levelMultiplier);
    }

    /// <summary>
    /// 开始新战斗
    /// </summary>
    public async Task<ServerBattleContext> StartBattleAsync(BattleStartRequest request)
    {
        var battleId = Guid.NewGuid();
        
        var battle = new ServerBattleContext
        {
            BattleId = battleId,
            BattleType = request.BattleType,
            PartyId = request.PartyId,
            DungeonId = request.DungeonId,
            State = ServerBattleState.Active,
            StartTime = DateTime.UtcNow,
            Players = GetPlayersForBattle(request),
            Enemies = GenerateEnemiesForBattle(request),
            PlayerTargets = new Dictionary<string, string>()
        };

        _activeBattles[battleId] = battle;
        
        _logger.LogInformation("Started new battle: {BattleId} with {PlayerCount} players and {EnemyCount} enemies", 
            battleId, battle.Players.Count, battle.Enemies.Count);

        // 通知客户端战斗开始
        await NotifyBattleStartedAsync(battle);

        OnStateChanged?.Invoke();
        
        return battle;
    }

    /// <summary>
    /// 获取参战玩家
    /// </summary>
    private List<ServerBattlePlayer> GetPlayersForBattle(BattleStartRequest request)
    {
        // 根据请求类型获取玩家
        if (request.PartyId.HasValue)
        {
            // 组队战斗 - 获取组队成员
            return _allCharacters.Where(p => p.PartyId == request.PartyId).ToList();
        }
        else if (!string.IsNullOrEmpty(request.PlayerId))
        {
            // 单人战斗
            var player = _allCharacters.FirstOrDefault(p => p.Id == request.PlayerId);
            return player != null ? new List<ServerBattlePlayer> { player } : new List<ServerBattlePlayer>();
        }
        
        return new List<ServerBattlePlayer>();
    }

    /// <summary>
    /// 生成战斗敌人
    /// </summary>
    private List<ServerBattleEnemy> GenerateEnemiesForBattle(BattleStartRequest request)
    {
        // TODO: 根据战斗类型、玩家等级等生成合适的敌人
        // 这里先返回一个简单的敌人列表
        var enemies = new List<ServerBattleEnemy>();
        
        var enemy = new ServerBattleEnemy
        {
            Id = Guid.NewGuid().ToString(),
            Name = "测试敌人",
            Level = 1,
            Health = 100,
            MaxHealth = 100,
            AttacksPerSecond = 1.0,
            AttackCooldown = 0,
            IsAlive = true
        };
        
        enemies.Add(enemy);
        return enemies;
    }

    /// <summary>
    /// 通知客户端战斗开始
    /// </summary>
    private async Task NotifyBattleStartedAsync(ServerBattleContext battle)
    {
        var playerIds = battle.Players.Select(p => p.Id).ToList();
        await _hubContext.Clients.Users(playerIds).SendAsync("BattleStarted", battle);
    }

    /// <summary>
    /// 通知客户端战斗结束
    /// </summary>
    private async Task NotifyBattleCompletedAsync(ServerBattleContext battle)
    {
        var playerIds = battle.Players.Select(p => p.Id).ToList();
        await _hubContext.Clients.Users(playerIds).SendAsync("BattleCompleted", battle);
    }

    /// <summary>
    /// 获取所有活跃战斗
    /// </summary>
    public IEnumerable<ServerBattleContext> GetAllActiveBattles()
    {
        return _activeBattles.Values.Where(b => b.State == ServerBattleState.Active);
    }

    /// <summary>
    /// 强制结束战斗
    /// </summary>
    public async Task<bool> ForceEndBattleAsync(Guid battleId)
    {
        if (_activeBattles.TryGetValue(battleId, out var battle))
        {
            battle.State = ServerBattleState.Completed;
            battle.IsVictory = false;
            
            await NotifyBattleCompletedAsync(battle);
            
            _logger.LogInformation("Force ended battle: {BattleId}", battleId);
            return true;
        }
        
        return false;
    }
}