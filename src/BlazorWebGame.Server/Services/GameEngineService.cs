using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using BlazorWebGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 服务端游戏引擎，处理所有游戏逻辑
/// </summary>
public class GameEngineService
{
    private readonly Dictionary<Guid, BattleStateDto> _activeBattles = new();
    private readonly Dictionary<Guid, ServerBattleContext> _serverBattleContexts = new();
    private readonly ILogger<GameEngineService> _logger;
    private readonly ServerCombatEngine _combatEngine;
    private readonly ServerPartyService _partyService;
    private readonly ServerBattleFlowService _battleFlowService;
    private readonly IHubContext<GameHub> _hubContext;

    public GameEngineService(ILogger<GameEngineService> logger, ServerCombatEngine combatEngine, 
        ServerPartyService partyService, ServerBattleFlowService battleFlowService, IHubContext<GameHub> hubContext)
    {
        _logger = logger;
        _combatEngine = combatEngine;
        _partyService = partyService;
        _battleFlowService = battleFlowService;
        _hubContext = hubContext;
    }

    /// <summary>
    /// 开始新战斗
    /// </summary>
    public BattleStateDto StartBattle(StartBattleRequest request)
    {
        var battleId = Guid.NewGuid();
        
        // 创建服务端战斗上下文
        var serverContext = CreateServerBattleContext(battleId, request);
        _serverBattleContexts[battleId] = serverContext;
        
        // 创建客户端战斗状态DTO
        var battleState = ConvertToDto(serverContext);
        _activeBattles[battleId] = battleState;
        
        _logger.LogInformation("Battle started: {BattleId} for character {CharacterId}", 
            battleId, request.CharacterId);
        
        return battleState;
    }

    /// <summary>
    /// 创建服务端战斗上下文
    /// </summary>
    private ServerBattleContext CreateServerBattleContext(Guid battleId, StartBattleRequest request)
    {
        Guid? partyGuid = null;
        if (!string.IsNullOrEmpty(request.PartyId) && Guid.TryParse(request.PartyId, out var parsedPartyGuid))
        {
            partyGuid = parsedPartyGuid;
        }

        var context = new ServerBattleContext
        {
            BattleId = battleId,
            BattleType = "Normal",
            PartyId = partyGuid
        };

        // 如果有 PartyId，获取组队成员
        List<string> participantIds;
        if (partyGuid.HasValue)
        {
            participantIds = _partyService.GetActivePartyMembers(partyGuid.Value);
            
            // 确保发起者在列表中
            if (!participantIds.Contains(request.CharacterId))
            {
                participantIds.Add(request.CharacterId);
            }
            
            _logger.LogInformation("Starting party battle with {MemberCount} members", participantIds.Count);
        }
        else
        {
            // 单人战斗
            participantIds = new List<string> { request.CharacterId };
        }

        // 为每个参与者创建玩家实例
        foreach (var characterId in participantIds)
        {
            var player = new ServerBattlePlayer
            {
                Id = characterId,
                Name = $"英雄-{characterId[^4..]}", // 使用后4位作为显示名，实际应从数据库获取
                Health = 100,
                MaxHealth = 100,
                BaseAttackPower = 15,
                AttacksPerSecond = 1.2,
                Level = 1,
                SelectedBattleProfession = "Warrior",
                EquippedSkills = new List<string> { "warrior_charge", "warrior_shield_bash" }
            };
            context.Players.Add(player);
        }

        // 创建敌人（根据参与者数量调整敌人强度）
        int enemyCount = Math.Max(1, participantIds.Count / 2); // 每2个玩家对应1个敌人，至少1个
        for (int i = 0; i < enemyCount; i++)
        {
            var enemy = new ServerBattleEnemy
            {
                Id = $"{request.EnemyId}-{i}",
                Name = $"哥布林-{i + 1}",
                Health = 80 + (participantIds.Count * 10), // 根据队伍大小调整血量
                MaxHealth = 80 + (participantIds.Count * 10),
                BaseAttackPower = 12 + (participantIds.Count * 2), // 根据队伍大小调整攻击力
                AttacksPerSecond = 1.0,
                Level = 1,
                XpReward = 25,
                MinGoldReward = 5,
                MaxGoldReward = 15,
                EnemyType = "Goblin",
                LootTable = new Dictionary<string, double>
                {
                    { "iron_sword", 0.1 },
                    { "health_potion", 0.3 }
                },
                EquippedSkills = new List<string> { "goblin_slash" }
            };
            context.Enemies.Add(enemy);
        }

        return context;
    }

    /// <summary>
    /// 将服务端上下文转换为DTO
    /// </summary>
    private BattleStateDto ConvertToDto(ServerBattleContext context)
    {
        var dto = new BattleStateDto
        {
            BattleId = context.BattleId,
            CharacterId = context.Players.FirstOrDefault()?.Id ?? "",
            EnemyId = context.Enemies.FirstOrDefault()?.Id ?? "",
            PartyId = context.PartyId?.ToString(),
            IsActive = context.IsActive,
            LastUpdated = context.LastUpdate,
            BattleType = context.BattleType == "Normal" ? BattleType.Normal : BattleType.Dungeon,
            Status = context.Status switch
            {
                "Active" => BattleStatus.Active,
                "Completed" => BattleStatus.Completed,
                _ => BattleStatus.Paused
            }
        };

        // 填充参与者信息
        foreach (var player in context.Players)
        {
            dto.Players.Add(new BattleParticipantDto
            {
                Id = player.Id,
                Name = player.Name,
                Health = player.Health,
                MaxHealth = player.MaxHealth,
                AttackPower = player.BaseAttackPower,
                AttacksPerSecond = player.AttacksPerSecond,
                AttackCooldown = player.AttackCooldown,
                EquippedSkills = player.EquippedSkills,
                SkillCooldowns = player.SkillCooldowns,
                IsPlayer = true
            });
        }

        foreach (var enemy in context.Enemies)
        {
            dto.Enemies.Add(new BattleParticipantDto
            {
                Id = enemy.Id,
                Name = enemy.Name,
                Health = enemy.Health,
                MaxHealth = enemy.MaxHealth,
                AttackPower = enemy.BaseAttackPower,
                AttacksPerSecond = enemy.AttacksPerSecond,
                AttackCooldown = enemy.AttackCooldown,
                EquippedSkills = enemy.EquippedSkills,
                SkillCooldowns = enemy.SkillCooldowns,
                IsPlayer = false
            });
        }

        // 填充最近动作
        dto.RecentActions = context.ActionHistory
            .TakeLast(10)
            .Select(a => new BattleActionDto
            {
                ActorId = a.ActorId,
                ActorName = a.ActorName,
                TargetId = a.TargetId,
                TargetName = a.TargetName,
                ActionType = a.ActionType switch
                {
                    "Attack" => BattleActionType.Attack,
                    "UseSkill" => BattleActionType.UseSkill,
                    "Defend" => BattleActionType.Defend,
                    _ => BattleActionType.Attack
                },
                Damage = a.Damage,
                SkillId = a.SkillId,
                Timestamp = a.Timestamp,
                IsCritical = a.IsCritical
            })
            .ToList();

        dto.PlayerTargets = context.PlayerTargets;

        // 填充组队成员ID列表
        dto.PartyMemberIds = context.Players.Select(p => p.Id).ToList();

        // 设置兼容的简单属性
        var firstPlayer = context.Players.FirstOrDefault();
        var firstEnemy = context.Enemies.FirstOrDefault();
        
        if (firstPlayer != null)
        {
            dto.PlayerHealth = firstPlayer.Health;
            dto.PlayerMaxHealth = firstPlayer.MaxHealth;
        }
        
        if (firstEnemy != null)
        {
            dto.EnemyHealth = firstEnemy.Health;
            dto.EnemyMaxHealth = firstEnemy.MaxHealth;
        }

        return dto;
    }

    /// <summary>
    /// 获取战斗状态
    /// </summary>
    public BattleStateDto? GetBattleState(Guid battleId)
    {
        return _activeBattles.TryGetValue(battleId, out var battle) ? battle : null;
    }

    /// <summary>
    /// 处理战斗逻辑更新 - 在游戏循环中调用
    /// </summary>
    public async Task ProcessBattleTickAsync(double deltaTime)
    {
        var contextsToUpdate = _serverBattleContexts.Values.Where(c => c.IsActive).ToList();
        
        foreach (var context in contextsToUpdate)
        {
            ProcessSingleBattle(context, deltaTime);
            
            // 更新对应的DTO
            if (_activeBattles.ContainsKey(context.BattleId))
            {
                var previousState = _activeBattles[context.BattleId];
                var newState = ConvertToDto(context);
                _activeBattles[context.BattleId] = newState;
                
                // 通过SignalR发送实时更新
                await BroadcastBattleUpdate(newState);
            }
        }

        // 处理战斗流程管理（刷新、波次进度等）
        await _battleFlowService.ProcessBattleRefreshAsync(deltaTime, this);
    }

    /// <summary>
    /// 向客户端广播战斗更新
    /// </summary>
    private async Task BroadcastBattleUpdate(BattleStateDto battleState)
    {
        try
        {
            var groupName = $"battle-{battleState.BattleId}";
            await _hubContext.Clients.Group(groupName).SendAsync("BattleUpdate", battleState);
            
            _logger.LogDebug("Battle update broadcasted for battle {BattleId}", battleState.BattleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting battle update for battle {BattleId}", battleState.BattleId);
        }
    }

    /// <summary>
    /// 处理单个战斗的更新
    /// </summary>
    private void ProcessSingleBattle(ServerBattleContext context, double deltaTime)
    {
        if (!context.IsActive || !context.HasActiveParticipants) 
        {
            if (context.Status == "Active")
            {
                CompleteBattle(context);
            }
            return;
        }

        context.LastUpdate = DateTime.UtcNow;

        // 处理玩家攻击
        foreach (var player in context.Players.Where(p => p.IsAlive))
        {
            _combatEngine.ProcessPlayerAttack(context, player, deltaTime);
        }

        // 处理敌人攻击
        foreach (var enemy in context.Enemies.Where(e => e.IsAlive))
        {
            _combatEngine.ProcessEnemyAttack(context, enemy, deltaTime);
        }

        // 检查战斗是否结束
        if (!context.HasActiveParticipants)
        {
            CompleteBattle(context);
        }
    }

    /// <summary>
    /// 完成战斗
    /// </summary>
    private void CompleteBattle(ServerBattleContext context)
    {
        context.Status = "Completed";
        
        // 判断胜负
        bool victory = context.Players.Any(p => p.IsAlive) && !context.Enemies.Any(e => e.IsAlive);
        
        // 计算奖励
        var result = _combatEngine.CalculateBattleRewards(context, victory);
        
        // 更新DTO
        if (_activeBattles.TryGetValue(context.BattleId, out var dto))
        {
            dto.Result = result;
            dto.Status = BattleStatus.Completed;
            dto.IsActive = false;
        }
        
        // 收集敌人信息用于战斗流程管理
        var enemyInfos = CollectEnemyInfos(context);
        
        // 通知战斗流程服务处理战斗完成
        _battleFlowService.OnBattleCompleted(context, enemyInfos);
        
        _logger.LogInformation("Battle completed: {BattleId}, Victory: {Victory}", 
            context.BattleId, victory);
    }

    /// <summary>
    /// 收集战斗中的敌人信息
    /// </summary>
    private List<ServerEnemyInfo> CollectEnemyInfos(ServerBattleContext context)
    {
        var result = new List<ServerEnemyInfo>();

        // 按敌人名称分组统计
        var groupedEnemies = context.Enemies.GroupBy(e => e.Name);
        foreach (var group in groupedEnemies)
        {
            result.Add(new ServerEnemyInfo
            {
                Name = group.Key,
                Count = group.Count()
            });
        }

        return result;
    }

    /// <summary>
    /// 获取所有需要更新的战斗状态
    /// </summary>
    public List<BattleStateDto> GetAllBattleUpdates()
    {
        return _activeBattles.Values.Where(b => b.IsActive).ToList();
    }

    /// <summary>
    /// 停止战斗
    /// </summary>
    public bool StopBattle(Guid battleId)
    {
        bool success = false;
        
        if (_activeBattles.TryGetValue(battleId, out var battle))
        {
            battle.IsActive = false;
            battle.Status = BattleStatus.Completed;
            success = true;
        }
        
        if (_serverBattleContexts.TryGetValue(battleId, out var context))
        {
            context.Status = "Completed";
            success = true;
        }
        
        if (success)
        {
            // 取消相关的战斗刷新
            _battleFlowService.CancelBattleRefresh(battleId);
            
            _logger.LogInformation("Battle manually stopped: {BattleId}", battleId);
        }
        
        return success;
    }

    /// <summary>
    /// 执行战斗动作
    /// </summary>
    public bool ExecuteBattleAction(BattleActionRequest request)
    {
        if (!_serverBattleContexts.TryGetValue(request.BattleId, out var context))
        {
            return false;
        }

        if (!context.IsActive)
        {
            return false;
        }

        var player = context.Players.FirstOrDefault(p => p.Id == request.PlayerId);
        if (player == null || !player.IsAlive)
        {
            return false;
        }

        switch (request.ActionType)
        {
            case BattleActionType.Attack:
                return ExecuteAttackAction(context, player, request.TargetId);
            
            case BattleActionType.UseSkill:
                return ExecuteSkillAction(context, player, request.SkillId, request.TargetId);
            
            case BattleActionType.Defend:
                return ExecuteDefendAction(context, player);
            
            default:
                return false;
        }
    }

    /// <summary>
    /// 执行攻击动作
    /// </summary>
    private bool ExecuteAttackAction(ServerBattleContext context, ServerBattlePlayer player, string? targetId)
    {
        if (player.AttackCooldown > 0)
        {
            return false; // 还在冷却中
        }

        var target = context.Enemies.FirstOrDefault(e => e.Id == targetId && e.IsAlive);
        if (target == null)
        {
            // 自动选择目标
            target = context.Enemies.FirstOrDefault(e => e.IsAlive);
        }

        if (target != null)
        {
            context.PlayerTargets[player.Id] = target.Id;
            _combatEngine.ProcessPlayerAttack(context, player, 0); // 立即执行攻击
            return true;
        }

        return false;
    }

    /// <summary>
    /// 执行技能动作
    /// </summary>
    private bool ExecuteSkillAction(ServerBattleContext context, ServerBattlePlayer player, string? skillId, string? targetId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        return _combatEngine.ExecuteSkillAttack(context, player, skillId, targetId);
    }

    /// <summary>
    /// 执行防御动作
    /// </summary>
    private bool ExecuteDefendAction(ServerBattleContext context, ServerBattlePlayer player)
    {
        // 简化的防御系统：增加临时躲避率
        player.DodgeChance = Math.Min(0.8, player.DodgeChance + 0.3);
        
        var action = new ServerBattleAction
        {
            ActorId = player.Id,
            ActorName = player.Name,
            ActionType = "Defend",
            Timestamp = DateTime.UtcNow
        };
        
        context.ActionHistory.Add(action);
        return true;
    }

    /// <summary>
    /// 检查玩家是否在战斗刷新状态（用于战斗流程服务）
    /// </summary>
    public bool IsPlayerInBattleRefresh(string playerId)
    {
        return _battleFlowService.IsPlayerInBattleRefresh(playerId);
    }

    /// <summary>
    /// 获取玩家战斗刷新剩余时间（用于战斗流程服务）
    /// </summary>
    public double GetPlayerBattleRefreshTime(string playerId)
    {
        return _battleFlowService.GetPlayerBattleRefreshTime(playerId);
    }

    /// <summary>
    /// 创建副本战斗（简化版本，用于战斗流程服务）
    /// </summary>
    public BattleStateDto StartDungeonBattle(string dungeonId, List<string> playerIds)
    {
        var primaryPlayerId = playerIds.FirstOrDefault() ?? "";
        var request = new StartBattleRequest
        {
            CharacterId = primaryPlayerId,
            EnemyId = dungeonId,
            PartyId = playerIds.Count > 1 ? Guid.NewGuid().ToString() : null
        };

        // 设置副本特定的战斗类型
        var battle = StartBattle(request);
        
        // 更新服务端上下文为副本类型
        if (_serverBattleContexts.TryGetValue(battle.BattleId, out var context))
        {
            context.BattleType = "Dungeon";
            context.DungeonId = dungeonId;
            context.AllowAutoRevive = true;
        }

        return battle;
    }

    /// <summary>
    /// 取消战斗刷新（用于战斗流程服务）
    /// </summary>
    public void CancelBattleRefresh(Guid battleId)
    {
        _battleFlowService.CancelBattleRefresh(battleId);
    }
}