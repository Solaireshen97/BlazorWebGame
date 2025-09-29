using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using BlazorWebGame.Shared.Events;
using BlazorWebGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 服务端游戏引擎，处理所有游戏逻辑
/// 重构为使用统一事件队列系统
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
    private readonly UnifiedEventService _eventService;

    public GameEngineService(ILogger<GameEngineService> logger, ServerCombatEngine combatEngine, 
        ServerPartyService partyService, ServerBattleFlowService battleFlowService, 
        IHubContext<GameHub> hubContext, UnifiedEventService eventService)
    {
        _logger = logger;
        _combatEngine = combatEngine;
        _partyService = partyService;
        _battleFlowService = battleFlowService;
        _hubContext = hubContext;
        _eventService = eventService;
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
        
        // 使用新事件系统触发战斗开始事件
        _eventService.EnqueueBattleEvent(GameEventTypes.BATTLE_STARTED, (ulong)battleId.GetHashCode(), 
            actorId: HashString(request.CharacterId));
        
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
            BattleType = context.BattleType == "Normal" ? BlazorWebGame.Shared.DTOs.BattleType.Normal : BlazorWebGame.Shared.DTOs.BattleType.Dungeon,
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
    /// 处理战斗逻辑更新 - 重构为使用事件驱动架构
    /// 不再直接处理战斗逻辑，而是收集事件并入队
    /// 优化版本：增强错误处理和性能监控
    /// </summary>
    public async Task ProcessBattleTickAsync(double deltaTime)
    {
        var activeContexts = _serverBattleContexts.Values.Where(c => c.IsActive).ToList();
        var events = new List<UnifiedEvent>();
        var processedBattles = 0;
        var eventsGenerated = 0;
        
        // 收集所有战斗相关事件而非直接处理
        foreach (var context in activeContexts)
        {
            try
            {
                var contextEvents = new List<UnifiedEvent>();
                CollectBattleEvents(context, deltaTime, contextEvents);
                events.AddRange(contextEvents);
                eventsGenerated += contextEvents.Count;
                processedBattles++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting battle events for battle {BattleId}", context.BattleId);
                // 继续处理其他战斗，不让单个战斗错误影响整个系统
            }
        }
        
        // 批量入队事件到统一事件队列
        if (events.Count > 0)
        {
            var eventArray = events.ToArray();
            var enqueuedCount = _eventService.EnqueueBatch(eventArray, events.Count);
            
            if (enqueuedCount != events.Count)
            {
                _logger.LogWarning("Failed to enqueue {FailedCount} out of {TotalCount} battle events", 
                    events.Count - enqueuedCount, events.Count);
            }
            else
            {
                _logger.LogDebug("Successfully processed {BattleCount} battles and generated {EventCount} events", 
                    processedBattles, eventsGenerated);
            }
        }

        // 处理战斗流程管理（刷新、波次进度等）
        try
        {
            await _battleFlowService.ProcessBattleRefreshAsync(deltaTime, this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing battle refresh flow");
        }
    }

    /// <summary>
    /// 收集单个战斗的事件 - 替代直接处理逻辑
    /// </summary>
    private void CollectBattleEvents(ServerBattleContext context, double deltaTime, List<UnifiedEvent> events)
    {
        if (!context.IsActive || !context.HasActiveParticipants) 
        {
            if (context.Status == "Active")
            {
                // 战斗结束事件
                var battleEndEvent = new UnifiedEvent(GameEventTypes.BATTLE_ENDED, EventPriority.Gameplay)
                {
                    ActorId = (ulong)context.BattleId.GetHashCode(),
                    TargetId = 0
                };
                events.Add(battleEndEvent);
                
                CompleteBattle(context);
            }
            return;
        }

        context.LastUpdate = DateTime.UtcNow;

        // 系统tick事件
        var tickEvent = new UnifiedEvent(GameEventTypes.BATTLE_TICK, EventPriority.Gameplay)
        {
            ActorId = (ulong)context.BattleId.GetHashCode()
        };
        events.Add(tickEvent);

        // 处理复活逻辑并收集相关事件
        if (context.AllowAutoRevive)
        {
            CollectRevivalEvents(context, deltaTime, events);
        }

        // 收集战斗动作事件
        var alivePlayers = context.Players.Where(p => p.IsAlive).ToList();
        var aliveEnemies = context.Enemies.Where(e => e.IsAlive).ToList();

        if (alivePlayers.Any() && aliveEnemies.Any())
        {
            foreach (var player in alivePlayers)
            {
                CollectPlayerCombatEvents(context, player, deltaTime, events);
            }

            foreach (var enemy in aliveEnemies)
            {
                CollectEnemyCombatEvents(context, enemy, deltaTime, events);
            }
        }

        // 检查战斗是否结束
        if (!context.HasActiveParticipants)
        {
            var battleEndEvent = new UnifiedEvent(GameEventTypes.BATTLE_ENDED, EventPriority.Gameplay)
            {
                ActorId = (ulong)context.BattleId.GetHashCode()
            };
            events.Add(battleEndEvent);
        }
    }

    /// <summary>
    /// 收集复活相关事件
    /// </summary>
    private void CollectRevivalEvents(ServerBattleContext context, double deltaTime, List<UnifiedEvent> events)
    {
        const double revivalTime = 5.0;

        foreach (var player in context.Players.Where(p => !p.IsAlive))
        {
            if (!player.Attributes.ContainsKey("RevivalTimer"))
            {
                player.Attributes["RevivalTimer"] = 0;
            }

            var revivalTimer = player.Attributes["RevivalTimer"] + (int)(deltaTime * 1000);
            player.Attributes["RevivalTimer"] = revivalTimer;

            if (revivalTimer >= revivalTime * 1000)
            {
                // 复活玩家
                player.Health = player.MaxHealth / 2;
                player.Attributes.Remove("RevivalTimer");

                // 创建复活事件
                var revivalEvent = new UnifiedEvent(GameEventTypes.PLAYER_REVIVED, EventPriority.Gameplay)
                {
                    ActorId = HashString(player.Id),
                    TargetId = (ulong)context.BattleId.GetHashCode()
                };
                events.Add(revivalEvent);
                
                _logger.LogInformation("Player {PlayerName} revival event queued for battle {BattleId}", 
                    player.Name, context.BattleId);
            }
        }
    }

    /// <summary>
    /// 收集玩家战斗事件
    /// </summary>
    private void CollectPlayerCombatEvents(ServerBattleContext context, ServerBattlePlayer player, 
        double deltaTime, List<UnifiedEvent> events)
    {
        // 更新技能冷却
        UpdateSkillCooldowns(player, deltaTime);

        // 尝试使用技能
        if (TryCollectPlayerSkillEvent(context, player, events))
        {
            return; // 使用了技能，跳过普通攻击
        }

        // 收集普通攻击事件
        CollectPlayerAttackEvent(context, player, deltaTime, events);
    }

    /// <summary>
    /// 收集玩家技能使用事件
    /// </summary>
    private bool TryCollectPlayerSkillEvent(ServerBattleContext context, ServerBattlePlayer player, 
        List<UnifiedEvent> events)
    {
        if (Random.Shared.NextDouble() > 0.3)
            return false;

        var availableSkills = player.EquippedSkills.Where(skillId => 
            !player.SkillCooldowns.ContainsKey(skillId) || player.SkillCooldowns[skillId] <= 0).ToList();

        if (!availableSkills.Any())
            return false;

        var selectedSkill = availableSkills[Random.Shared.Next(availableSkills.Count)];
        var target = context.Enemies.FirstOrDefault(e => e.IsAlive);
        if (target == null)
            return false;

        // 创建技能使用事件
        var skillEvent = new UnifiedEvent(GameEventTypes.SKILL_USED, EventPriority.Gameplay)
        {
            ActorId = HashString(player.Id),
            TargetId = HashString(target.Id)
        };
        events.Add(skillEvent);

        return true;
    }

    /// <summary>
    /// 收集玩家攻击事件
    /// </summary>
    private void CollectPlayerAttackEvent(ServerBattleContext context, ServerBattlePlayer player, 
        double deltaTime, List<UnifiedEvent> events)
    {
        if (player.AttackCooldown > 0)
        {
            player.AttackCooldown -= deltaTime;
            return;
        }

        var target = context.Enemies.FirstOrDefault(e => e.IsAlive);
        if (target == null)
            return;

        // 计算伤害
        var damage = CalculatePlayerDamage(player, target);
        var actualDamage = Math.Min(damage, target.Health);
        
        // 应用伤害（立即更新状态，但通过事件通知）
        target.Health -= actualDamage;
        player.AttackCooldown = 1.0 / player.AttacksPerSecond;

        // 创建伤害事件
        var damageData = new DamageEventData
        {
            Damage = damage,
            ActualDamage = actualDamage,
            IsCritical = 0, // 简化，实际应该计算
            DamageType = 1 // 物理伤害
        };

        var damageEvent = new UnifiedEvent(GameEventTypes.DAMAGE_DEALT, EventPriority.Gameplay)
        {
            ActorId = HashString(player.Id),
            TargetId = HashString(target.Id)
        };
        damageEvent.SetData(damageData);
        events.Add(damageEvent);

        // 如果敌人死亡，创建死亡事件
        if (target.Health <= 0)
        {
            var deathEvent = new UnifiedEvent(GameEventTypes.ENEMY_KILLED, EventPriority.Gameplay)
            {
                ActorId = HashString(player.Id),
                TargetId = HashString(target.Id)
            };
            events.Add(deathEvent);
        }
    }

    /// <summary>
    /// 收集敌人战斗事件
    /// </summary>
    private void CollectEnemyCombatEvents(ServerBattleContext context, ServerBattleEnemy enemy, 
        double deltaTime, List<UnifiedEvent> events)
    {
        // 简化敌人AI - 类似玩家逻辑但更简单
        if (enemy.AttackCooldown > 0)
        {
            enemy.AttackCooldown -= deltaTime;
            return;
        }

        var target = context.Players.FirstOrDefault(p => p.IsAlive);
        if (target == null)
            return;

        var damage = CalculateEnemyDamage(enemy, target);
        var actualDamage = Math.Min(damage, target.Health);
        
        target.Health -= actualDamage;
        enemy.AttackCooldown = 1.0 / enemy.AttacksPerSecond;

        var damageData = new DamageEventData
        {
            Damage = damage,
            ActualDamage = actualDamage,
            IsCritical = 0,
            DamageType = 1
        };

        var damageEvent = new UnifiedEvent(GameEventTypes.DAMAGE_DEALT, EventPriority.Gameplay)
        {
            ActorId = HashString(enemy.Id),
            TargetId = HashString(target.Id)
        };
        damageEvent.SetData(damageData);
        events.Add(damageEvent);
    }

    /// <summary>
    /// 计算玩家伤害
    /// </summary>
    private int CalculatePlayerDamage(ServerBattlePlayer player, ServerBattleEnemy enemy)
    {
        return player.BaseAttackPower + Random.Shared.Next(-2, 3);
    }

    /// <summary>
    /// 计算敌人伤害
    /// </summary>
    private int CalculateEnemyDamage(ServerBattleEnemy enemy, ServerBattlePlayer player)
    {
        return enemy.BaseAttackPower + Random.Shared.Next(-2, 3);
    }

    /// <summary>
    /// 字符串哈希函数
    /// </summary>
    private static ulong HashString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return 0;

        const ulong FnvPrime = 1099511628211UL;
        const ulong FnvOffsetBasis = 14695981039346656037UL;

        var hash = FnvOffsetBasis;
        foreach (var c in input)
        {
            hash ^= c;
            hash *= FnvPrime;
        }
        return hash;
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
    /// 处理单个战斗的更新 - 完整的自动战斗逻辑
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

        // 处理玩家复活计时（如果允许自动复活）
        if (context.AllowAutoRevive)
        {
            ProcessPlayerRevival(context, deltaTime);
        }

        // 获取活着的参与者
        var alivePlayers = context.Players.Where(p => p.IsAlive).ToList();
        var aliveEnemies = context.Enemies.Where(e => e.IsAlive).ToList();

        if (alivePlayers.Any() && aliveEnemies.Any())
        {
            // 有存活者时，处理战斗逻辑
            foreach (var player in alivePlayers)
            {
                ProcessPlayerCombat(context, player, deltaTime);
            }

            foreach (var enemy in aliveEnemies)
            {
                ProcessEnemyCombat(context, enemy, deltaTime);
            }
        }
        else if (!alivePlayers.Any())
        {
            // 所有玩家死亡，停止敌人攻击并等待复活
            foreach (var enemy in context.Enemies)
            {
                enemy.AttackCooldown = 1.0 / enemy.AttacksPerSecond;
            }
        }

        // 检查战斗是否结束
        if (!context.HasActiveParticipants)
        {
            CompleteBattle(context);
        }
    }

    /// <summary>
    /// 处理玩家复活逻辑
    /// </summary>
    private void ProcessPlayerRevival(ServerBattleContext context, double deltaTime)
    {
        // 简化的复活系统：死亡5秒后自动复活
        const double revivalTime = 5.0;

        foreach (var player in context.Players.Where(p => !p.IsAlive))
        {
            if (!player.Attributes.ContainsKey("RevivalTimer"))
            {
                player.Attributes["RevivalTimer"] = 0;
            }

            var revivalTimer = player.Attributes["RevivalTimer"] + (int)(deltaTime * 1000);
            player.Attributes["RevivalTimer"] = revivalTimer;

            if (revivalTimer >= revivalTime * 1000)
            {
                // 复活玩家
                player.Health = player.MaxHealth / 2; // 复活时回复50%血量
                player.Attributes.Remove("RevivalTimer");

                var action = new ServerBattleAction
                {
                    ActorId = player.Id,
                    ActorName = player.Name,
                    ActionType = "Revive",
                    Timestamp = DateTime.UtcNow
                };
                context.ActionHistory.Add(action);
                
                _logger.LogInformation("Player {PlayerName} has been revived in battle {BattleId}", 
                    player.Name, context.BattleId);
            }
        }
    }

    /// <summary>
    /// 处理玩家战斗逻辑（包含智能技能使用）
    /// </summary>
    private void ProcessPlayerCombat(ServerBattleContext context, ServerBattlePlayer player, double deltaTime)
    {
        // 更新技能冷却
        UpdateSkillCooldowns(player, deltaTime);

        // 尝试使用技能（有概率）
        if (TryUsePlayerSkill(context, player))
        {
            return; // 使用了技能，跳过普通攻击
        }

        // 处理普通攻击
        _combatEngine.ProcessPlayerAttack(context, player, deltaTime);
    }

    /// <summary>
    /// 处理敌人战斗逻辑（包含AI技能使用）
    /// </summary>
    private void ProcessEnemyCombat(ServerBattleContext context, ServerBattleEnemy enemy, double deltaTime)
    {
        // 更新技能冷却
        UpdateEnemySkillCooldowns(enemy, deltaTime);

        // 尝试使用技能（有概率）
        if (TryUseEnemySkill(context, enemy))
        {
            return; // 使用了技能，跳过普通攻击
        }

        // 处理普通攻击
        _combatEngine.ProcessEnemyAttack(context, enemy, deltaTime);
    }

    /// <summary>
    /// 更新玩家技能冷却时间
    /// </summary>
    private void UpdateSkillCooldowns(ServerBattlePlayer player, double deltaTime)
    {
        var cooldownsToUpdate = player.SkillCooldowns.Keys.ToList();
        foreach (var skillId in cooldownsToUpdate)
        {
            player.SkillCooldowns[skillId] = Math.Max(0, player.SkillCooldowns[skillId] - deltaTime);
        }
    }

    /// <summary>
    /// 更新敌人技能冷却时间
    /// </summary>
    private void UpdateEnemySkillCooldowns(ServerBattleEnemy enemy, double deltaTime)
    {
        var cooldownsToUpdate = enemy.SkillCooldowns.Keys.ToList();
        foreach (var skillId in cooldownsToUpdate)
        {
            enemy.SkillCooldowns[skillId] = Math.Max(0, enemy.SkillCooldowns[skillId] - deltaTime);
        }
    }

    /// <summary>
    /// 尝试让玩家使用技能
    /// </summary>
    private bool TryUsePlayerSkill(ServerBattleContext context, ServerBattlePlayer player)
    {
        // 30%概率尝试使用技能
        if (Random.Shared.NextDouble() > 0.3)
            return false;

        // 查找可用的技能
        var availableSkills = player.EquippedSkills.Where(skillId => 
            !player.SkillCooldowns.ContainsKey(skillId) || player.SkillCooldowns[skillId] <= 0).ToList();

        if (!availableSkills.Any())
            return false;

        // 随机选择一个技能
        var selectedSkill = availableSkills[Random.Shared.Next(availableSkills.Count)];
        
        // 选择目标
        var target = context.Enemies.FirstOrDefault(e => e.IsAlive);
        if (target == null)
            return false;

        // 执行技能
        return _combatEngine.ExecuteSkillAttack(context, player, selectedSkill, target.Id);
    }

    /// <summary>
    /// 尝试让敌人使用技能
    /// </summary>
    private bool TryUseEnemySkill(ServerBattleContext context, ServerBattleEnemy enemy)
    {
        // 20%概率尝试使用技能
        if (Random.Shared.NextDouble() > 0.2)
            return false;

        // 查找可用的技能
        var availableSkills = enemy.EquippedSkills.Where(skillId => 
            !enemy.SkillCooldowns.ContainsKey(skillId) || enemy.SkillCooldowns[skillId] <= 0).ToList();

        if (!availableSkills.Any())
            return false;

        // 随机选择一个技能
        var selectedSkill = availableSkills[Random.Shared.Next(availableSkills.Count)];
        
        // 选择目标
        var target = context.Players.FirstOrDefault(p => p.IsAlive);
        if (target == null)
            return false;

        // 使用 ServerSkillSystem 执行敌人技能
        // 这里需要 ServerSkillSystem 支持敌人技能执行
        return TryExecuteEnemySkill(context, enemy, selectedSkill, target.Id);
    }

    /// <summary>
    /// 执行敌人技能的简化实现
    /// </summary>
    private bool TryExecuteEnemySkill(ServerBattleContext context, ServerBattleEnemy enemy, string skillId, string targetId)
    {
        // 简化的敌人技能实现
        var target = context.Players.FirstOrDefault(p => p.Id == targetId && p.IsAlive);
        if (target == null)
            return false;

        // 设置技能冷却（根据技能类型设定不同冷却时间）
        var cooldown = skillId switch
        {
            "goblin_slash" => 3.0,
            "orc_rage" => 8.0,
            "skeleton_bone_throw" => 5.0,
            _ => 4.0
        };

        enemy.SkillCooldowns[skillId] = cooldown;

        // 技能伤害计算（比普通攻击高50%）
        var skillDamage = (int)(enemy.BaseAttackPower * 1.5);
        var originalHealth = target.Health;
        target.Health = Math.Max(0, target.Health - skillDamage);
        var actualDamage = originalHealth - target.Health;

        // 记录技能使用
        var action = new ServerBattleAction
        {
            ActorId = enemy.Id,
            ActorName = enemy.Name,
            TargetId = target.Id,
            TargetName = target.Name,
            ActionType = "UseSkill",
            SkillId = skillId,
            Damage = actualDamage,
            Timestamp = DateTime.UtcNow
        };

        context.ActionHistory.Add(action);
        
        _logger.LogDebug("Enemy {EnemyName} uses skill {SkillId} on {PlayerName} for {Damage} damage", 
            enemy.Name, skillId, target.Name, actualDamage);

        return true;
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