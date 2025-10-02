using BlazorWebGame.Server.Events;
using BlazorWebGame.Server.Services.Character;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Character;
using BlazorWebGame.Shared.Events;
using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BattleEndedEventData = BlazorWebGame.Shared.Events.BattleEndedEventData;

namespace BlazorWebGame.Server.Services.Battle
{
    /// <summary>
    /// 增强版战斗服务 - 集成统一事件系统
    /// </summary>
    public class EnhancedBattleService : IBattleService
    {
        private readonly IDataStorageService _dataStorage;
        private readonly ILogger<EnhancedBattleService> _logger;
        private readonly UnifiedEventQueue _eventQueue;
        private readonly DomainEventAdapter _eventAdapter;
        private readonly GameClock _gameClock;
        private readonly EnhancedServerCharacterService _characterService;
        private readonly BattleAttackCooldownManager _cooldownManager = new();

        // 内存中的活跃战斗缓存
        private readonly Dictionary<string, BattleInstance> _activeBattles = new();
        private readonly object _battleLock = new();

        public EnhancedBattleService(
            IDataStorageService dataStorage,
            ILogger<EnhancedBattleService> logger,
            UnifiedEventQueue eventQueue,
            DomainEventAdapter eventAdapter,
            GameClock gameClock,
            EnhancedServerCharacterService characterService)
        {
            _dataStorage = dataStorage;
            _logger = logger;
            _eventQueue = eventQueue;
            _eventAdapter = eventAdapter;
            _gameClock = gameClock;
            _characterService = characterService;
        }

        /// <summary>
        /// 创建战斗
        /// </summary>
        public async Task<ApiResponse<string>> CreateBattleAsync(
            string characterId,
            string enemyId,
            string battleType = "Normal",
            string? regionId = null)
        {
            try
            {
                // 获取角色数据
                var characterResponse = await _characterService.GetCharacterDetails(characterId);
                if (!characterResponse.IsSuccess || characterResponse.Data == null)
                {
                    return ApiResponse<string>.Failure("角色不存在");
                }

                // 创建战斗实体
                var battleEntity = new EnhancedBattleEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    BattleType = battleType,
                    Status = "Preparing",
                    StartTime = _gameClock.CurrentTime,
                    RegionId = regionId,
                    BattleModeType = "RealTime",
                    DifficultyLevel = "Normal",
                    EnvironmentType = "Default"
                };

                // 创建参与者
                var participants = new List<EnhancedBattleParticipantEntity>();

                // 添加玩家参与者
                var playerParticipant = CreatePlayerParticipant(characterResponse.Data, battleEntity.Id);
                participants.Add(playerParticipant);

                // 添加敌人参与者（这里简化处理）
                var enemyParticipant = CreateEnemyParticipant(enemyId, battleEntity.Id);
                participants.Add(enemyParticipant);

                // 序列化参与者数据
                battleEntity.ParticipantsJson = JsonSerializer.Serialize(participants);

                // 保存到数据库
                var saveResult = await _dataStorage.CreateBattleAsync(battleEntity);
                if (!saveResult.IsSuccess)
                {
                    return ApiResponse<string>.Failure($"创建战斗失败: {saveResult.Message}");
                }

                // 保存参与者
                foreach (var participant in participants)
                {
                    await _dataStorage.SaveBattleParticipantAsync(participant);
                }

                // 创建战斗实例并缓存
                var battleInstance = new BattleInstance(battleType, _gameClock, battleEntity.Id);
                lock (_battleLock)
                {
                    _activeBattles[battleEntity.Id] = battleInstance;
                }

                // 发布战斗创建事件
                PublishBattleStartedEvent(battleEntity.Id, characterId);

                _logger.LogInformation("战斗创建成功: {BattleId}, 角色: {CharacterId}",
                    battleEntity.Id, characterId);

                return ApiResponse<string>.Success(battleEntity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建战斗失败");
                return ApiResponse<string>.Failure($"创建战斗时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public async Task<ApiResponse<bool>> StartBattleAsync(string battleId)
        {
            try
            {
                // 获取战斗实体
                var battleResult = await _dataStorage.GetBattleByIdAsync(battleId);
                if (!battleResult.IsSuccess || battleResult.Data == null)
                {
                    return ApiResponse<bool>.Failure("战斗不存在");
                }

                var battleEntity = battleResult.Data;
                if (battleEntity.Status != "Preparing")
                {
                    return ApiResponse<bool>.Failure("战斗状态不正确");
                }

                // 更新状态
                battleEntity.Status = "InProgress";
                battleEntity.StartTime = _gameClock.CurrentTime;
                await _dataStorage.UpdateBattleAsync(battleEntity);

                // 获取战斗实例
                BattleInstance? battleInstance;
                lock (_battleLock)
                {
                    if (!_activeBattles.TryGetValue(battleId, out battleInstance))
                    {
                        // 如果不在缓存中，重建实例
                        battleInstance = new BattleInstance(battleEntity.BattleType, _gameClock, battleId);
                        _activeBattles[battleId] = battleInstance;
                    }
                }

                // 启动战斗tick
                ScheduleBattleTick(battleId);

                _logger.LogInformation("战斗已开始: {BattleId}", battleId);
                return ApiResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始战斗失败: {BattleId}", battleId);
                return ApiResponse<bool>.Failure($"开始战斗失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 战斗攻击冷却管理器
        /// </summary>
        public class BattleAttackCooldownManager
        {
            private readonly ConcurrentDictionary<string, DateTime> _lastAttackTimes = new();

            /// <summary>
            /// 检查参与者是否可以攻击
            /// </summary>
            public bool CanAttack(string participantId, double attacksPerSecond, DateTime currentTime)
            {
                if (attacksPerSecond <= 0)
                    return false;

                var attackInterval = TimeSpan.FromSeconds(1.0 / attacksPerSecond);

                if (_lastAttackTimes.TryGetValue(participantId, out var lastAttackTime))
                {
                    return currentTime >= lastAttackTime.Add(attackInterval);
                }

                return true; // 第一次攻击
            }

            /// <summary>
            /// 记录攻击时间
            /// </summary>
            public void RecordAttack(string participantId, DateTime attackTime)
            {
                _lastAttackTimes[participantId] = attackTime;
            }

            /// <summary>
            /// 清理战斗的攻击记录
            /// </summary>
            public void ClearBattleRecords(string battleId)
            {
                // 实际实现中可能需要根据battleId来清理相关的参与者记录
                _lastAttackTimes.Clear();
            }
        }

        /// <summary>
        /// 处理战斗Tick - 修复版
        /// </summary>
        public async Task ProcessBattleTickAsync(string battleId)
        {
            try
            {
                _logger.LogDebug("ProcessBattleTickAsync开始: {BattleId}", battleId);

                // 获取战斗实例
                BattleInstance? battleInstance;
                lock (_battleLock)
                {
                    if (!_activeBattles.TryGetValue(battleId, out battleInstance))
                    {
                        _logger.LogWarning("战斗实例不存在: {BattleId}", battleId);
                        return;
                    }
                }

                // 获取战斗数据
                var battleResult = await _dataStorage.GetBattleByIdAsync(battleId);
                if (!battleResult.IsSuccess || battleResult.Data == null)
                {
                    _logger.LogWarning("无法获取战斗数据: {BattleId}", battleId);
                    return;
                }

                var battleEntity = battleResult.Data;
                if (battleEntity.Status != "InProgress")
                {
                    _logger.LogDebug("战斗状态不是进行中: {Status}", battleEntity.Status);
                    return;
                }

                // 获取参与者
                var participantsResult = await _dataStorage.GetBattleParticipantsAsync(battleId);
                if (!participantsResult.IsSuccess || participantsResult.Data == null)
                {
                    _logger.LogWarning("无法获取战斗参与者");
                    return;
                }

                var participants = participantsResult.Data.ToList();
                _logger.LogDebug("战斗参与者数量: {Count}, 存活: {AliveCount}",
                    participants.Count, participants.Count(p => p.IsAlive));

                // 处理每个存活参与者的行动
                bool anyActionPerformed = false;
                foreach (var participant in participants.Where(p => p.IsAlive))
                {
                    if (await ProcessParticipantActionFixed(battleEntity, participant, participants))
                    {
                        anyActionPerformed = true;
                    }
                }

                // 检查战斗是否结束
                if (CheckBattleEnd(participants))
                {
                    _logger.LogInformation("战斗即将结束: {BattleId}", battleId);
                    await EndBattleAsync(battleId, participants);
                    return;
                }

                // 更新战斗状态
                battleEntity.CurrentTurn++;
                battleEntity.StateJson = JsonSerializer.Serialize(new
                {
                    Turn = battleEntity.CurrentTurn,
                    AliveParticipants = participants.Count(p => p.IsAlive),
                    UpdatedAt = _gameClock.CurrentTime
                });

                await _dataStorage.UpdateBattleAsync(battleEntity);

                // 调度下一个tick
                _logger.LogDebug("调度下一个战斗Tick: {BattleId}, Turn: {Turn}",
                    battleId, battleEntity.CurrentTurn);
                ScheduleBattleTickDelayed(battleId, 100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理战斗Tick失败: {BattleId}", battleId);
            }
        }

        /// <summary>
        /// 处理参与者行动 - 修复版
        /// </summary>
        private async Task<bool> ProcessParticipantActionFixed(
            EnhancedBattleEntity battle,
            EnhancedBattleParticipantEntity actor,
            List<EnhancedBattleParticipantEntity> allParticipants)
        {
            // 解析战斗属性
            var combatStats = ParseCombatStats(actor.CombatStatsJson);

            // 检查攻击冷却
            if (!_cooldownManager.CanAttack(actor.Id, combatStats.AttacksPerSecond, _gameClock.CurrentTime))
            {
                return false;
            }

            // 选择目标
            var target = SelectTarget(actor, allParticipants);
            if (target == null)
            {
                _logger.LogDebug("参与者 {ActorName} 没有可攻击的目标", actor.Name);
                return false;
            }

            // 执行攻击
            await ExecuteAttackAsync(battle, actor, target);

            // 记录攻击时间
            _cooldownManager.RecordAttack(actor.Id, _gameClock.CurrentTime);

            return true;
        }

        /// <summary>
        /// 处理参与者行动
        /// </summary>
        private async Task ProcessParticipantAction(
            EnhancedBattleEntity battle,
            EnhancedBattleParticipantEntity actor,
            List<EnhancedBattleParticipantEntity> allParticipants)
        {
            // 检查是否可以行动（冷却等）
            if (!CanAct(actor))
            {
                return;
            }

            // 选择目标
            var target = SelectTarget(actor, allParticipants);
            if (target == null)
            {
                return;
            }

            // 执行攻击
            await ExecuteAttackAsync(battle, actor, target);
        }

        /// <summary>
        /// 执行攻击 - 增强版
        /// </summary>
        private async Task ExecuteAttackAsync(
            EnhancedBattleEntity battle,
            EnhancedBattleParticipantEntity attacker,
            EnhancedBattleParticipantEntity target)
        {
            // 解析战斗属性
            var combatStats = ParseCombatStats(attacker.CombatStatsJson);
            var targetStats = ParseCombatStats(target.CombatStatsJson);

            // 计算命中
            var random = new Random();
            if (!combatStats.CheckHit(random, targetStats.DodgeChance))
            {
                _logger.LogDebug("攻击未命中: {Attacker} -> {Target}", attacker.Name, target.Name);

                // 发布未命中事件
                PublishMissEvent(attacker.Id, target.Id);
                return;
            }

            // 计算伤害
            var baseDamage = combatStats.AttackPower;
            var isCritical = random.NextDouble() < combatStats.CriticalChance;
            var damage = isCritical ? (int)(baseDamage * combatStats.CriticalMultiplier) : baseDamage;

            // 应用目标防御
            damage = targetStats.CalculateReceivedDamage(damage, random);

            // 应用伤害
            var previousHealth = target.Health;
            target.Health = Math.Max(0, target.Health - damage);
            var isDead = false;

            if (target.Health <= 0 && target.IsAlive)
            {
                target.IsAlive = false;
                target.DeathTime = _gameClock.CurrentTime;
                isDead = true;
                _logger.LogInformation("{TargetName} 被 {AttackerName} 击败！", target.Name, attacker.Name);
            }

            // 更新参与者数据
            await _dataStorage.UpdateBattleParticipantAsync(target);

            // 创建详细的战斗事件
            var battleEvent = new EnhancedBattleEventEntity
            {
                Id = Guid.NewGuid().ToString(),
                BattleId = battle.Id,
                EventType = "Attack",
                Timestamp = _gameClock.CurrentTime,
                ActorId = attacker.Id,
                TargetId = target.Id,
                TurnNumber = battle.CurrentTurn,
                Damage = damage,
                IsCritical = isCritical,
                DamageType = "Physical",
                EventDetailsJson = JsonSerializer.Serialize(new
                {
                    AttackerName = attacker.Name,
                    TargetName = target.Name,
                    AttackerHealth = attacker.Health,
                    TargetHealth = target.Health,
                    PreviousHealth = previousHealth,
                    BaseDamage = baseDamage,
                    FinalDamage = damage,
                    IsDead = isDead
                })
            };

            await _dataStorage.SaveBattleEventAsync(battleEvent);

            // 发布攻击事件到统一事件队列（包含更多信息）
            PublishDetailedAttackEvent(attacker, target, damage, isCritical);

            _logger.LogDebug("攻击执行: {Attacker}({AttackerHP}) 对 {Target}({TargetHP}) 造成 {Damage} 点伤害 (暴击: {Critical})",
                attacker.Name, attacker.Health, target.Name, target.Health, damage, isCritical);
        }

        /// <summary>
        /// 发布详细的攻击事件
        /// </summary>
        private void PublishDetailedAttackEvent(
            EnhancedBattleParticipantEntity attacker,
            EnhancedBattleParticipantEntity target,
            int damage,
            bool isCritical)
        {
            var data = new BattleAttackEventData
            {
                BaseDamage = damage,
                ActualDamage = damage,
                IsCritical = (byte)(isCritical ? 1 : 0),
                AttackType = 0, // Physical
                CritMultiplier = isCritical ? 1.5f : 1.0f,
                RemainingHealth = target.Health,
                StatusEffect = target.IsAlive ? (ushort)0 : (ushort)1 // 1 = Dead
            };

            _eventQueue.EnqueueEvent(
                GameEventTypes.BATTLE_ATTACK,
                data,
                EventPriority.Gameplay,
                ParseStringToUlong(attacker.Id),
                ParseStringToUlong(target.Id)
            );
        }

        /// <summary>
        /// 发布未命中事件
        /// </summary>
        private void PublishMissEvent(string attackerId, string targetId)
        {
            _eventQueue.EnqueueEvent(
                GameEventTypes.ATTACK_MISSED,
                EventPriority.Gameplay,
                ParseStringToUlong(attackerId),
                ParseStringToUlong(targetId)
            );
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        private async Task EndBattleAsync(string battleId, List<EnhancedBattleParticipantEntity> participants)
        {
            try
            {
                var battleResult = await _dataStorage.GetBattleByIdAsync(battleId);
                if (!battleResult.IsSuccess || battleResult.Data == null)
                {
                    return;
                }

                var battle = battleResult.Data;
                battle.Status = "Completed";
                battle.EndTime = _gameClock.CurrentTime;

                // 判断胜负
                var playersAlive = participants.Any(p => p.ParticipantType == "Player" && p.IsAlive);
                var enemiesAlive = participants.Any(p => p.ParticipantType == "Enemy" && p.IsAlive);

                var outcome = playersAlive && !enemiesAlive ? "Victory" :
                             !playersAlive && enemiesAlive ? "Defeat" : "Draw";

                // 创建战斗结果
                var result = new EnhancedBattleResultEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    BattleId = battleId,
                    Outcome = outcome,
                    Duration = (int)(battle.EndTime.Value - battle.StartTime).TotalSeconds,
                    TotalTurns = battle.CurrentTurn,
                    WinningTeam = playersAlive ? "Players" : "Enemies",
                    CompletedAt = _gameClock.CurrentTime,
                    SurvivorIdsJson = JsonSerializer.Serialize(
                        participants.Where(p => p.IsAlive).Select(p => p.Id).ToList()
                    )
                };

                // 计算奖励
                if (outcome == "Victory")
                {
                    result.RewardsJson = JsonSerializer.Serialize(CalculateRewards(battle, participants));
                }

                await _dataStorage.SaveBattleResultAsync(result);
                await _dataStorage.UpdateBattleAsync(battle);

                // 从缓存中移除
                lock (_battleLock)
                {
                    _activeBattles.Remove(battleId);
                }

                // 发布战斗结束事件
                PublishBattleEndedEvent(battleId, outcome == "Victory");

                _logger.LogInformation("战斗结束: {BattleId}, 结果: {Outcome}", battleId, outcome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "结束战斗失败: {BattleId}", battleId);
            }
        }

        #region 事件发布方法

        /// <summary>
        /// 发布战斗开始事件
        /// </summary>
        private void PublishBattleStartedEvent(string battleId, string characterId)
        {
            var evt = new UnifiedEvent(GameEventTypes.BATTLE_STARTED, EventPriority.Gameplay)
            {
                ActorId = ParseStringToUlong(characterId),
                TargetId = ParseStringToUlong(battleId)
            };
            _eventQueue.Enqueue(ref evt);
        }

        /// <summary>
        /// 发布攻击事件
        /// </summary>
        private void PublishAttackEvent(string attackerId, string targetId, int damage, bool isCritical)
        {
            var data = new BattleAttackEventData
            {
                BaseDamage = damage,
                ActualDamage = damage,
                IsCritical = (byte)(isCritical ? 1 : 0),
                AttackType = 0, // Physical
                CritMultiplier = isCritical ? 1.5f : 1.0f
            };

            _eventQueue.EnqueueEvent(
                GameEventTypes.BATTLE_ATTACK,
                data,
                EventPriority.Gameplay,
                ParseStringToUlong(attackerId),
                ParseStringToUlong(targetId)
            );

            // 同时发布领域事件
            var domainEvent = new DamageEvent
            {
                AggregateId = attackerId,
                SourceId = attackerId,
                TargetId = targetId,
                Amount = damage,
                IsCritical = isCritical
            };
            _eventAdapter.PublishDomainEvent(domainEvent);
        }

        /// <summary>
        /// 发布战斗结束事件
        /// </summary>
        /// <summary>
        /// 发布战斗结束事件
        /// </summary>
        private void PublishBattleEndedEvent(string battleId, bool victory)
        {
            var evt = new UnifiedEvent(GameEventTypes.BATTLE_ENDED, EventPriority.Gameplay)
            {
                TargetId = ParseStringToUlong(battleId)
            };

            var eventData = new BattleEndedEventData
            {
                Victory = (byte)(victory ? 1 : 0),
                BattleIdHash = ParseStringToUlong(battleId)
            };

            evt.SetData(eventData);
            _eventQueue.Enqueue(ref evt);
        }

        /// <summary>
        /// 调度战斗Tick
        /// </summary>
        private void ScheduleBattleTick(string battleId)
        {
            _eventQueue.EnqueueEvent(
                GameEventTypes.BATTLE_TICK,
                EventPriority.Gameplay,
                ParseStringToUlong(battleId)
            );
        }


        /// <summary>
        /// 调度战斗Tick延迟 - 改进版
        /// </summary>
        private void ScheduleBattleTickDelayed(string battleId, int delayMs)
        {
            Task.Run(async () =>
            {
                await Task.Delay(delayMs);

                // 发布Tick事件
                var tickData = new BattleTickEventData
                {
                    BattleId = ParseStringToUlong(battleId),
                    TurnNumber = 0, // 可以从战斗实例获取
                    AliveCount = 0  // 可以从参与者列表获取
                };

                _eventQueue.EnqueueEvent(
                    GameEventTypes.BATTLE_TICK,
                    tickData,
                    EventPriority.Gameplay,
                    tickData.BattleId
                );

                _logger.LogDebug("已调度战斗Tick事件: {BattleId}", battleId);
            });
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建玩家参与者
        /// </summary>
        private EnhancedBattleParticipantEntity CreatePlayerParticipant(
            CharacterFullDto character,
            string battleId)
        {
            return new EnhancedBattleParticipantEntity
            {
                Id = Guid.NewGuid().ToString(),
                BattleId = battleId,
                ParticipantType = "Player",
                Name = character.Name,
                SourceId = character.Id,
                Team = 0,
                Position = 0,
                Level = character.Level,
                Health = character.Vitals.Health,
                MaxHealth = character.Vitals.MaxHealth,
                Mana = character.Vitals.Mana,
                MaxMana = character.Vitals.MaxMana,
                IsAlive = true,
                Initiative = 10 + character.Attributes.Agility,
                CombatStatsJson = JsonSerializer.Serialize(new BattleCombatStats
                {
                    AttackPower = (int)character.Attributes.AttackPower,
                    AttacksPerSecond = character.Attributes.AttackSpeed,
                    CriticalChance = character.Attributes.CriticalChance / 100.0,
                    CriticalMultiplier = character.Attributes.CriticalDamage / 100.0,
                    ArmorValue = (int)character.Attributes.Armor,
                    DodgeChance = character.Attributes.Agility * 0.001
                })
            };
        }

        /// <summary>
        /// 创建敌人参与者
        /// </summary>
        private EnhancedBattleParticipantEntity CreateEnemyParticipant(string enemyId, string battleId)
        {
            // 简化的敌人创建，实际应从敌人数据库加载
            return new EnhancedBattleParticipantEntity
            {
                Id = Guid.NewGuid().ToString(),
                BattleId = battleId,
                ParticipantType = "Enemy",
                Name = $"Enemy_{enemyId}",
                SourceId = enemyId,
                Team = 1,
                Position = 0,
                Level = 1,
                Health = 50,
                MaxHealth = 50,
                Mana = 0,
                MaxMana = 0,
                IsAlive = true,
                Initiative = 5,
                CombatStatsJson = JsonSerializer.Serialize(new BattleCombatStats
                {
                    AttackPower = 5,
                    AttacksPerSecond = 0.8,
                    CriticalChance = 0.05,
                    CriticalMultiplier = 1.5
                })
            };
        }

        /// <summary>
        /// 解析战斗属性
        /// </summary>
        private BattleCombatStats ParseCombatStats(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new BattleCombatStats();
            }

            try
            {
                return JsonSerializer.Deserialize<BattleCombatStats>(json) ?? new BattleCombatStats();
            }
            catch
            {
                return new BattleCombatStats();
            }
        }

        /// <summary>
        /// 检查是否可以行动
        /// </summary>
        private bool CanAct(EnhancedBattleParticipantEntity participant)
        {
            // 简化的行动检查
            return participant.IsAlive && !participant.HasActedThisTurn;
        }

        /// <summary>
        /// 选择目标
        /// </summary>
        private EnhancedBattleParticipantEntity? SelectTarget(
            EnhancedBattleParticipantEntity actor,
            List<EnhancedBattleParticipantEntity> allParticipants)
        {
            // 选择对方队伍的第一个存活目标
            var enemyTeam = actor.Team == 0 ? 1 : 0;
            return allParticipants
                .Where(p => p.Team == enemyTeam && p.IsAlive)
                .OrderBy(p => p.Health) // 优先攻击血量最少的
                .FirstOrDefault();
        }

        /// <summary>
        /// 检查战斗是否结束
        /// </summary>
        private bool CheckBattleEnd(List<EnhancedBattleParticipantEntity> participants)
        {
            var team0Alive = participants.Any(p => p.Team == 0 && p.IsAlive);
            var team1Alive = participants.Any(p => p.Team == 1 && p.IsAlive);

            return !team0Alive || !team1Alive;
        }

        /// <summary>
        /// 计算奖励
        /// </summary>
        private object CalculateRewards(
            EnhancedBattleEntity battle,
            List<EnhancedBattleParticipantEntity> participants)
        {
            var defeatedEnemies = participants
                .Where(p => p.ParticipantType == "Enemy" && !p.IsAlive)
                .ToList();

            var totalExp = defeatedEnemies.Sum(e => e.Level * 10);
            var totalGold = defeatedEnemies.Sum(e => e.Level * 5);

            return new
            {
                Experience = totalExp,
                Gold = totalGold,
                Items = new List<object>() // 简化的物品奖励
            };
        }

        /// <summary>
        /// 字符串转换为ulong
        /// </summary>
        private ulong ParseStringToUlong(string str)
        {
            if (Guid.TryParse(str, out var guid))
            {
                var bytes = guid.ToByteArray();
                return BitConverter.ToUInt64(bytes, 0);
            }
            return (ulong)str.GetHashCode();
        }

        #endregion

        #region 公共接口实现

        /// <summary>
        /// 使用技能
        /// </summary>
        public async Task<ApiResponse<bool>> UseSkillAsync(
            string battleId,
            string casterId,
            string skillId,
            string? targetId)
        {
            try
            {
                // 获取战斗和参与者信息
                var battleResult = await _dataStorage.GetBattleByIdAsync(battleId);
                if (!battleResult.IsSuccess || battleResult.Data == null)
                {
                    return ApiResponse<bool>.Failure("战斗不存在");
                }

                // 创建技能施放事件
                var skillEvent = new SkillCastEvent
                {
                    AggregateId = battleId,
                    SkillId = skillId,
                    CasterId = casterId,
                    TargetId = targetId,
                    ResourceCosts = new Dictionary<string, double>
                    {
                        ["Mana"] = 20
                    }
                };

                // 发布到事件系统
                _eventAdapter.PublishDomainEvent(skillEvent);

                // 同时发布到统一事件队列
                _eventQueue.EnqueueEvent(
                    GameEventTypes.SKILL_USED,
                    EventPriority.Gameplay,
                    ParseStringToUlong(casterId),
                    ParseStringToUlong(targetId ?? "0")
                );

                _logger.LogInformation("技能使用: {SkillId} by {CasterId} in battle {BattleId}",
                    skillId, casterId, battleId);

                return ApiResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "使用技能失败");
                return ApiResponse<bool>.Failure($"使用技能失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取战斗状态
        /// </summary>
        public async Task<ApiResponse<object>> GetBattleStatusAsync(string battleId)
        {
            try
            {
                var battleResult = await _dataStorage.GetBattleByIdAsync(battleId);
                if (!battleResult.IsSuccess || battleResult.Data == null)
                {
                    return ApiResponse<object>.Failure("战斗不存在");
                }

                var participantsResult = await _dataStorage.GetBattleParticipantsAsync(battleId);

                var status = new
                {
                    BattleId = battleId,
                    Status = battleResult.Data.Status,
                    Turn = battleResult.Data.CurrentTurn,
                    Participants = participantsResult.Data?.Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Health,
                        p.MaxHealth,
                        p.IsAlive,
                        p.Team
                    })
                };

                return ApiResponse<object>.Success(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取战斗状态失败");
                return ApiResponse<object>.Failure($"获取战斗状态失败: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 战斗服务接口
    /// </summary>
    public interface IBattleService
    {
        Task<ApiResponse<string>> CreateBattleAsync(string characterId, string enemyId, string battleType = "Normal", string? regionId = null);
        Task<ApiResponse<bool>> StartBattleAsync(string battleId);
        Task ProcessBattleTickAsync(string battleId);
        Task<ApiResponse<bool>> UseSkillAsync(string battleId, string casterId, string skillId, string? targetId);
        Task<ApiResponse<object>> GetBattleStatusAsync(string battleId);
    }
}