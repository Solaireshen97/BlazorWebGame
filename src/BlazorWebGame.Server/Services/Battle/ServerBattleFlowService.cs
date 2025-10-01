using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using BlazorWebGame.Server.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using BlazorWebGame.Server.Services.Core;

namespace BlazorWebGame.Server.Services.Battle;

/// <summary>
/// 服务端战斗流程管理服务 - 从客户端迁移而来
/// 处理战斗刷新、波次进度、战斗转换等逻辑
/// </summary>
public class ServerBattleFlowService
{
    private const double BattleRefreshCooldown = 3.0;
    private const double DungeonWaveRefreshCooldown = 2.0;  // 副本波次间隔时间
    private const double DungeonCompleteRefreshCooldown = 5.0;  // 副本完成后刷新时间
    
    private readonly Dictionary<Guid, ServerBattleRefreshState> _battleRefreshStates = new();
    private readonly Dictionary<Guid, ServerDungeonWaveRefreshState> _dungeonWaveRefreshStates = new();
    private readonly ILogger<ServerBattleFlowService> _logger;
    private readonly IHubContext<GameHub> _hubContext;

    public ServerBattleFlowService(ILogger<ServerBattleFlowService> logger, IHubContext<GameHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// 战斗完成处理 - 设置刷新状态
    /// </summary>
    public void OnBattleCompleted(ServerBattleContext battle, List<ServerEnemyInfo> enemyInfos)
    {
        // 如果没有敌人信息，生成默认的
        if (!enemyInfos.Any() && battle.BattleType != "Dungeon")
        {
            if (battle.Players.Any())
            {
                var player = battle.Players.First();
                var playerLevel = player.Level;

                // 简化的敌人选择逻辑（在服务端应该从数据库获取）
                var suitableEnemyName = DetermineSuitableEnemy(playerLevel);
                
                if (!string.IsNullOrEmpty(suitableEnemyName))
                {
                    enemyInfos.Add(new ServerEnemyInfo
                    {
                        Name = suitableEnemyName,
                        Count = battle.BattleType == "Party" ?
                            DetermineEnemyCount(battle.Players.Count) : 1
                    });
                }
            }
        }

        // 确定战斗刷新冷却时间
        double refreshCooldown = battle.BattleType == "Dungeon" 
            ? DungeonCompleteRefreshCooldown 
            : BattleRefreshCooldown;

        // 创建刷新状态
        _battleRefreshStates[battle.BattleId] = new ServerBattleRefreshState
        {
            OriginalBattle = battle,
            RemainingCooldown = refreshCooldown,
            BattleType = battle.BattleType,
            EnemyInfos = enemyInfos,
            DungeonId = battle.DungeonId?.ToString()
        };

        _logger.LogInformation("Battle refresh state created for battle {BattleId}, cooldown: {Cooldown}s", 
            battle.BattleId, refreshCooldown);
    }

    /// <summary>
    /// 处理战斗刷新逻辑 - 在游戏循环中调用
    /// </summary>
    public async Task ProcessBattleRefreshAsync(double elapsedSeconds, GameEngineService gameEngine)
    {
        // 处理普通战斗和副本完成后的刷新
        var refreshesToRemove = new List<Guid>();

        foreach (var kvp in _battleRefreshStates)
        {
            var refreshState = kvp.Value;
            refreshState.RemainingCooldown -= elapsedSeconds;

            if (refreshState.RemainingCooldown <= 0)
            {
                await StartNewBattleAfterCooldownAsync(refreshState, gameEngine);
                refreshesToRemove.Add(kvp.Key);
            }
        }

        foreach (var id in refreshesToRemove)
        {
            _battleRefreshStates.Remove(id);
        }

        // 处理副本波次刷新
        await ProcessDungeonWaveRefreshAsync(elapsedSeconds, gameEngine);
    }

    /// <summary>
    /// 处理副本波次刷新
    /// </summary>
    private async Task ProcessDungeonWaveRefreshAsync(double elapsedSeconds, GameEngineService gameEngine)
    {
        var waveRefreshesToRemove = new List<Guid>();

        foreach (var kvp in _dungeonWaveRefreshStates)
        {
            var waveRefreshState = kvp.Value;
            waveRefreshState.RemainingCooldown -= elapsedSeconds;

            if (waveRefreshState.RemainingCooldown <= 0)
            {
                // 处理下一波副本逻辑（这里简化实现）
                waveRefreshesToRemove.Add(kvp.Key);
            }
        }

        foreach (var id in waveRefreshesToRemove)
        {
            _dungeonWaveRefreshStates.Remove(id);
        }
    }

    /// <summary>
    /// 冷却结束后开始新战斗
    /// </summary>
    private async Task StartNewBattleAfterCooldownAsync(ServerBattleRefreshState refreshState, GameEngineService gameEngine)
    {
        var originalBattle = refreshState.OriginalBattle;

        // 检查是否还有存活的玩家
        if (!originalBattle.Players.Any(p => p.IsAlive))
        {
            _logger.LogInformation("No alive players to continue battle refresh for {BattleId}", originalBattle.BattleId);
            return;
        }

        switch (refreshState.BattleType)
        {
            case "Dungeon":
                if (!string.IsNullOrEmpty(refreshState.DungeonId))
                {
                    await StartNextDungeonRunAsync(originalBattle, refreshState.DungeonId, gameEngine);
                }
                break;

            case "Party":
            case "Normal":
                await StartSimilarBattleAsync(originalBattle, refreshState.EnemyInfos, gameEngine);
                break;
        }
    }

    /// <summary>
    /// 开始下一轮副本战斗
    /// </summary>
    private async Task StartNextDungeonRunAsync(ServerBattleContext originalBattle, string dungeonId, GameEngineService gameEngine)
    {
        var alivePlayers = originalBattle.Players.Where(p => p.IsAlive).ToList();
        if (!alivePlayers.Any()) return;

        var playerIds = alivePlayers.Select(p => p.Id).ToList();
        var newBattle = gameEngine.StartDungeonBattle(dungeonId, playerIds);
        
        _logger.LogInformation("Started new dungeon run {DungeonId} for {PlayerCount} players", 
            dungeonId, playerIds.Count);

        // 通过SignalR通知客户端
        await NotifyBattleStarted(newBattle);
    }

    /// <summary>
    /// 开始类似的战斗
    /// </summary>
    private async Task StartSimilarBattleAsync(ServerBattleContext originalBattle, List<ServerEnemyInfo> enemyInfos, GameEngineService gameEngine)
    {
        var alivePlayers = originalBattle.Players.Where(p => p.IsAlive).ToList();
        if (!alivePlayers.Any()) return;

        // 选择主要敌人类型
        var primaryEnemy = enemyInfos.FirstOrDefault();
        if (primaryEnemy == null)
        {
            // 根据玩家等级选择合适的敌人
            var player = alivePlayers.First();
            primaryEnemy = new ServerEnemyInfo 
            { 
                Name = DetermineSuitableEnemy(player.Level),
                Count = originalBattle.BattleType == "Party" ? 
                    DetermineEnemyCount(alivePlayers.Count) : 1
            };
        }

        // 创建新战斗请求
        var request = new StartBattleRequest
        {
            CharacterId = alivePlayers.First().Id,
            EnemyId = primaryEnemy.Name,
            PartyId = originalBattle.PartyId?.ToString()
        };

        var newBattle = gameEngine.StartBattle(request);
        
        _logger.LogInformation("Started similar battle for {PlayerCount} players against {EnemyName}", 
            alivePlayers.Count, primaryEnemy.Name);

        // 通过SignalR通知客户端
        await NotifyBattleStarted(newBattle);
    }

    /// <summary>
    /// 通知客户端战斗开始
    /// </summary>
    private async Task NotifyBattleStarted(BattleStateDto battleState)
    {
        try
        {
            var groupName = $"battle-{battleState.BattleId}";
            await _hubContext.Clients.Group(groupName).SendAsync("BattleStarted", battleState);
            
            // 也向所有参与玩家发送通知
            foreach (var playerId in battleState.PartyMemberIds)
            {
                await _hubContext.Clients.Group($"player-{playerId}").SendAsync("BattleStarted", battleState);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying battle start for battle {BattleId}", battleState.BattleId);
        }
    }

    /// <summary>
    /// 根据玩家等级确定合适的敌人
    /// </summary>
    private string DetermineSuitableEnemy(int playerLevel)
    {
        // 简化的敌人选择逻辑 - 在实际项目中应该从数据库获取
        return playerLevel switch
        {
            <= 5 => "goblin",
            <= 10 => "orc",
            <= 15 => "skeleton",
            <= 20 => "troll",
            _ => "dragon"
        };
    }

    /// <summary>
    /// 根据队伍规模确定敌人数量
    /// </summary>
    public int DetermineEnemyCount(int memberCount)
    {
        return Math.Max(1, (memberCount + 1) / 2);
    }

    /// <summary>
    /// 检查玩家是否在战斗刷新状态
    /// </summary>
    public bool IsPlayerInBattleRefresh(string playerId)
    {
        // 检查普通战斗刷新
        foreach (var refreshState in _battleRefreshStates.Values)
        {
            if (refreshState.OriginalBattle.Players.Any(p => p.Id == playerId))
            {
                return true;
            }
        }

        // 检查副本波次刷新
        foreach (var waveRefreshState in _dungeonWaveRefreshStates.Values)
        {
            if (waveRefreshState.Players.Any(p => p.Id == playerId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取玩家战斗刷新剩余时间
    /// </summary>
    public double GetPlayerBattleRefreshTime(string playerId)
    {
        // 检查普通战斗刷新
        foreach (var refreshState in _battleRefreshStates.Values)
        {
            if (refreshState.OriginalBattle.Players.Any(p => p.Id == playerId))
            {
                return refreshState.RemainingCooldown;
            }
        }

        // 检查副本波次刷新
        foreach (var waveRefreshState in _dungeonWaveRefreshStates.Values)
        {
            if (waveRefreshState.Players.Any(p => p.Id == playerId))
            {
                return waveRefreshState.RemainingCooldown;
            }
        }

        return 0;
    }

    /// <summary>
    /// 取消战斗刷新
    /// </summary>
    public void CancelBattleRefresh(Guid battleId)
    {
        // 移除普通战斗刷新状态
        var refreshToRemove = _battleRefreshStates
            .Where(kvp => kvp.Value.OriginalBattle.BattleId == battleId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in refreshToRemove)
        {
            _battleRefreshStates.Remove(key);
            _logger.LogInformation("Cancelled battle refresh for battle {BattleId}", battleId);
        }

        // 移除副本波次刷新状态
        var waveRefreshToRemove = _dungeonWaveRefreshStates
            .Where(kvp => kvp.Value.Players.Any()) // 简化的匹配逻辑
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in waveRefreshToRemove)
        {
            _dungeonWaveRefreshStates.Remove(key);
        }
    }

    /// <summary>
    /// 取消玩家的所有战斗刷新
    /// </summary>
    public void CancelPlayerBattleRefresh(string playerId)
    {
        // 取消普通战斗刷新
        var refreshToRemove = _battleRefreshStates
            .Where(kvp => kvp.Value.OriginalBattle.Players.Any(p => p.Id == playerId))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in refreshToRemove)
        {
            _battleRefreshStates.Remove(key);
        }

        // 取消副本波次刷新
        var waveRefreshToRemove = _dungeonWaveRefreshStates
            .Where(kvp => kvp.Value.Players.Any(p => p.Id == playerId))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in waveRefreshToRemove)
        {
            _dungeonWaveRefreshStates.Remove(key);
        }

        _logger.LogInformation("Cancelled all battle refreshes for player {PlayerId}", playerId);
    }

    /// <summary>
    /// 收集战斗中的敌人信息
    /// </summary>
    public List<ServerEnemyInfo> CollectEnemyInfos(ServerBattleContext battle)
    {
        var result = new List<ServerEnemyInfo>();

        // 按敌人名称分组统计
        var groupedEnemies = battle.Enemies.GroupBy(e => e.Name);
        foreach (var group in groupedEnemies)
        {
            result.Add(new ServerEnemyInfo
            {
                Name = group.Key,
                Count = group.Count(),
                Level = group.First().Level,
                EnemyType = group.First().EnemyType
            });
        }

        return result;
    }
}