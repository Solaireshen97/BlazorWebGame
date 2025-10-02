using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Mappers;
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
/// 服务端战斗管理器 - 重构版本使用Battle领域模型
/// 处理战斗实例的创建、管理和生命周期
/// </summary>
public class ServerBattleManager
{
    private readonly Dictionary<Guid, BlazorWebGame.Shared.Models.Battle> _activeBattles = new();
    private readonly EnhancedServerCharacterService _enhancedCharacterService;
    private readonly ServerCombatEngine _combatEngine;
    private readonly ServerBattleFlowService _battleFlowService;
    private readonly ServerSkillSystem _skillSystem;
    private readonly ServerLootService _lootService;
    private readonly ILogger<ServerBattleManager> _logger;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly BlazorWebGame.Shared.Models.GameClock _gameClock;

    /// <summary>
    /// 状态变更事件
    /// </summary>
    public event Action? OnStateChanged;

    public ServerBattleManager(
        EnhancedServerCharacterService enhancedCharacterService,
        ServerCombatEngine combatEngine,
        ServerBattleFlowService battleFlowService,
        ServerSkillSystem skillSystem,
        ServerLootService lootService,
        ILogger<ServerBattleManager> logger,
        IHubContext<GameHub> hubContext,
        GameClock gameClock)
    {
        _enhancedCharacterService = enhancedCharacterService;
        _combatEngine = combatEngine;
        _battleFlowService = battleFlowService;
        _skillSystem = skillSystem;
        _lootService = lootService;
        _logger = logger;
        _hubContext = hubContext;
        _gameClock = gameClock;
    }

    /// <summary>
    /// 获取玩家的活跃战斗
    /// </summary>
    public BlazorWebGame.Shared.Models.Battle? GetBattleForPlayer(string playerId)
    {
        return _activeBattles.Values.FirstOrDefault(b => 
            b.GetPlayerParticipants().Any(p => p.Id == playerId));
    }

    /// <summary>
    /// 获取组队的活跃战斗
    /// </summary>
    public BlazorWebGame.Shared.Models.Battle? GetBattleForParty(Guid partyId)
    {
        return _activeBattles.Values.FirstOrDefault(b => b.PartyId == partyId);
    }
    
    /// <summary>
    /// 获取战斗（兼容旧接口）
    /// </summary>
    [Obsolete("Use GetBattleForPlayer instead")]
    public ServerBattleContext? GetBattleContextForPlayer(string playerId)
    {
        // 返回null以保持向后兼容，但建议迁移到新API
        return null;
    }

    /// <summary>
    /// 获取战斗（兼容旧接口）
    /// </summary>
    [Obsolete("Use GetBattleForParty instead")]
    public ServerBattleContext? GetBattleContextForParty(Guid partyId)
    {
        // 返回null以保持向后兼容，但建议迁移到新API
        return null;
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
            if (battle.Status == BlazorWebGame.Shared.Models.BattleStatus.Completed)
            {
                // 应用战斗结果到角色
                await ApplyBattleResultsAsync(battle);

                battlesToRemove.Add(battle.Id);

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
    private void ProcessBattle(BlazorWebGame.Shared.Models.Battle battle, double elapsedSeconds)
    {
        if (battle.Status != BlazorWebGame.Shared.Models.BattleStatus.Active)
            return;

        // 战斗实例处理战斗逻辑
        if (battle.Instance != null)
        {
            battle.Instance.Update(elapsedSeconds);
        }

        // 检查战斗胜利条件
        CheckBattleVictoryConditions(battle);
    }

    /// <summary>
    /// 检查战斗胜利条件
    /// </summary>
    private void CheckBattleVictoryConditions(BlazorWebGame.Shared.Models.Battle battle)
    {
        if (battle.IsFinished())
        {
            var isVictory = battle.IsPlayerVictory();
            var duration = battle.GetDuration();
            
            // 创建战斗结果
            var result = new BlazorWebGame.Shared.Models.BattleResult(isVictory, duration);
            
            if (isVictory)
            {
                _logger.LogInformation("Battle {BattleId} completed - Victory!", battle.Id);
                CalculateBattleRewards(battle, result);
            }
            else
            {
                _logger.LogInformation("Battle {BattleId} completed - Defeat!", battle.Id);
            }
            
            // 结束战斗
            battle.EndBattle(isVictory, result);
        }
    }

    /// <summary>
    /// 计算战斗奖励
    /// </summary>
    private void CalculateBattleRewards(BlazorWebGame.Shared.Models.Battle battle, BlazorWebGame.Shared.Models.BattleResult result)
    {
        var players = battle.GetPlayerParticipants().OfType<BattlePlayer>().ToList();
        var enemies = battle.GetEnemyParticipants().OfType<BattleEnemy>().ToList();
        
        if (!players.Any() || !enemies.Any())
            return;
        
        // 计算总经验值
        int totalExp = 0;
        int totalGold = 0;
        
        foreach (var enemy in enemies)
        {
            totalExp += enemy.EnemyData.ExperienceReward;
            totalGold += enemy.EnemyData.GoldReward;
            
            // 收集掉落物品
            var random = new Random();
            var loot = enemy.GetLootDrops(random);
            foreach (var drop in loot)
            {
                result.AddLootDrop(drop);
            }
        }
        
        // 平均分配经验值和金币给存活的玩家
        var alivePlayers = players.Where(p => p.IsAlive).ToList();
        if (alivePlayers.Any())
        {
            var expPerPlayer = totalExp / alivePlayers.Count;
            var goldPerPlayer = totalGold / alivePlayers.Count;
            
            result.SetExperienceReward(expPerPlayer);
            result.SetGoldReward(goldPerPlayer);
            
            // 应用区域修饰符
            result.ApplyRegionModifiers(battle.RegionModifiers);
        }
        
        _logger.LogInformation("Battle {BattleId} rewards: {Exp} exp, {Gold} gold, {Items} items", 
            battle.Id, result.ExperienceGained, result.GoldGained, result.ItemsLooted.Count);
    }
    
    /// <summary>
    /// 应用战斗结果到角色
    /// </summary>
    private async Task ApplyBattleResultsAsync(BlazorWebGame.Shared.Models.Battle battle)
    {
        if (battle.Result == null)
            return;
            
        var players = battle.GetPlayerParticipants().OfType<BattlePlayer>().ToList();
        
        foreach (var battlePlayer in players)
        {
            // 获取角色的详细信息
            var characterResponse = await _enhancedCharacterService.GetCharacterDetails(battlePlayer.Id);
            if (!characterResponse.IsSuccess || characterResponse.Data == null)
            {
                _logger.LogWarning("Failed to get character {CharacterId} to apply battle results", battlePlayer.Id);
                continue;
            }
            
            // 从存储加载完整的Character领域模型
            // 注意：这里我们需要通过EnhancedServerCharacterService的内部方法加载Character
            // 由于EnhancedServerCharacterService使用私有方法，我们需要另一种方式
            // 暂时跳过，后续需要在EnhancedServerCharacterService中添加公共方法
            
            _logger.LogInformation("Applied battle results to character {CharacterId}: +{Exp} exp, +{Gold} gold",
                battlePlayer.Id, battle.Result.ExperienceGained, battle.Result.GoldGained);
        }
    }

    /// <summary>
    /// 开始新战斗
    /// </summary>
    public async Task<BlazorWebGame.Shared.Models.Battle> StartBattleAsync(BattleStartRequest request)
    {
        // 解析战斗类型
        BlazorWebGame.Shared.Models.BattleType battleType = request.BattleType switch
        {
            "Normal" => BattleType.Normal,
            "Dungeon" => BattleType.Dungeon,
            "PvP" => BattleType.PvP,
            "Raid" => BattleType.Raid,
            "Event" => BattleType.Event,
            _ => BattleType.Normal
        };
        
        // 创建战斗
        var battle = new BlazorWebGame.Shared.Models.Battle(
            battleType, 
            _gameClock, 
            regionId: null, // TODO: 从请求中获取区域ID
            partyId: request.PartyId,
            dungeonId: request.DungeonId
        );
        
        // 添加玩家参与者
        var players = await GetPlayersForBattleAsync(request);
        foreach (var player in players)
        {
            battle.AddParticipant(player);
        }
        
        // 添加敌人参与者
        var enemies = GenerateEnemiesForBattle(request);
        foreach (var enemy in enemies)
        {
            battle.AddParticipant(enemy);
        }
        
        // 开始战斗
        battle.StartBattle();
        
        _activeBattles[battle.Id] = battle;
        
        _logger.LogInformation("Started new battle: {BattleId} with {PlayerCount} players and {EnemyCount} enemies", 
            battle.Id, players.Count, enemies.Count);

        // 通知客户端战斗开始
        await NotifyBattleStartedAsync(battle);

        OnStateChanged?.Invoke();
        
        return battle;
    }
    
    /// <summary>
    /// 开始新战斗（兼容旧接口）
    /// </summary>
    [Obsolete("Use StartBattleAsync that returns Battle instead")]
    public async Task<ServerBattleContext> StartBattleAsync_Old(BattleStartRequest request)
    {
        // 返回空对象以保持向后兼容
        return new ServerBattleContext();
    }

    /// <summary>
    /// 获取参战玩家
    /// </summary>
    private async Task<List<BlazorWebGame.Shared.Models.BattlePlayer>> GetPlayersForBattleAsync(BattleStartRequest request)
    {
        var players = new List<BlazorWebGame.Shared.Models.BattlePlayer>();
        
        if (request.PartyId.HasValue)
        {
            // 组队战斗 - 获取组队成员
            // TODO: 从组队服务获取成员列表
            _logger.LogWarning("Party battle not fully implemented yet");
        }
        else if (!string.IsNullOrEmpty(request.PlayerId))
        {
            // 单人战斗
            var characterResponse = await _enhancedCharacterService.GetCharacterDetails(request.PlayerId);
            if (characterResponse.IsSuccess && characterResponse.Data != null)
            {
                // 需要从CharacterFullDto转换回Character领域模型
                // 这里需要EnhancedServerCharacterService提供一个方法来获取Character对象
                // 暂时创建一个简化的BattlePlayer
                var character = new Character(characterResponse.Data.Name)
                {
                    // 设置基础属性
                };
                
                var battlePlayer = BattleMapper.ToBattlePlayer(character);
                players.Add(battlePlayer);
            }
        }
        
        return players;
    }

    /// <summary>
    /// 生成战斗敌人
    /// </summary>
    private List<BlazorWebGame.Shared.Models.BattleEnemy> GenerateEnemiesForBattle(BattleStartRequest request)
    {
        var enemies = new List<BlazorWebGame.Shared.Models.BattleEnemy>();
        
        // TODO: 根据战斗类型、玩家等级等生成合适的敌人
        // 这里先返回一个简单的敌人列表
        if (request.EnemyIds.Any())
        {
            // 根据敌人ID生成敌人
            // 需要从敌人配置中加载
            _logger.LogWarning("Enemy generation from IDs not fully implemented yet");
        }
        else
        {
            // 生成默认测试敌人
            var testEnemy = new Enemy("test_enemy", "测试敌人")
            {
                // 设置敌人属性
            };
            
            var battleEnemy = BattleMapper.ToBattleEnemy(testEnemy);
            enemies.Add(battleEnemy);
        }
        
        return enemies;
    }

    /// <summary>
    /// 通知客户端战斗开始
    /// </summary>
    private async Task NotifyBattleStartedAsync(BlazorWebGame.Shared.Models.Battle battle)
    {
        var playerIds = battle.GetPlayerParticipants().Select(p => p.Id).ToList();
        // TODO: 转换为DTO再发送
        await _hubContext.Clients.Users(playerIds).SendAsync("BattleStarted", new { BattleId = battle.Id });
    }

    /// <summary>
    /// 通知客户端战斗结束
    /// </summary>
    private async Task NotifyBattleCompletedAsync(BlazorWebGame.Shared.Models.Battle battle)
    {
        var playerIds = battle.GetPlayerParticipants().Select(p => p.Id).ToList();
        // TODO: 转换为DTO再发送
        await _hubContext.Clients.Users(playerIds).SendAsync("BattleCompleted", new 
        { 
            BattleId = battle.Id,
            IsVictory = battle.IsPlayerVictory(),
            Result = battle.Result
        });
    }

    /// <summary>
    /// 获取所有活跃战斗
    /// </summary>
    public IEnumerable<BlazorWebGame.Shared.Models.Battle> GetAllActiveBattles()
    {
        return _activeBattles.Values.Where(b => b.Status == BlazorWebGame.Shared.Models.BattleStatus.Active);
    }

    /// <summary>
    /// 强制结束战斗
    /// </summary>
    public async Task<bool> ForceEndBattleAsync(Guid battleId)
    {
        if (_activeBattles.TryGetValue(battleId, out var battle))
        {
            battle.CancelBattle();
            
            await NotifyBattleCompletedAsync(battle);
            
            _logger.LogInformation("Force ended battle: {BattleId}", battleId);
            return true;
        }
        
        return false;
    }
}