using BlazorWebGame.Server.Services.Battle;
using BlazorWebGame.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BlazorWebGame.Server.Events
{
    /// <summary>
    /// 战斗事件处理器 - 处理从统一事件队列接收的战斗事件
    /// </summary>
    /// <summary>
    /// 战斗事件处理器 - 处理从统一事件队列接收的战斗事件
    /// </summary>
    public class BattleEventProcessor
    {
        private readonly EventDispatcher _dispatcher;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BattleEventProcessor> _logger;

        public BattleEventProcessor(
            EventDispatcher dispatcher,
            IServiceProvider serviceProvider,
            ILogger<BattleEventProcessor> logger)
        {
            _dispatcher = dispatcher;
            _serviceProvider = serviceProvider;
            _logger = logger;

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            // 注册战斗Tick处理器 - 这是关键！
            _dispatcher.RegisterHandler(GameEventTypes.BATTLE_TICK, evt =>
            {
                var battleId = ConvertUlongToString(evt.TargetId);
                _logger.LogDebug("收到战斗Tick事件: {BattleId}", battleId);

                // Fire-and-forget 模式执行异步任务
                Task.Run(async () =>
                {
                    try
                    {
                        await ProcessBattleTickAsync(battleId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理战斗Tick时发生错误: {BattleId}", battleId);
                    }
                });
            });

            // 注册战斗开始处理器
            _dispatcher.RegisterHandler(GameEventTypes.BATTLE_STARTED, evt =>
            {
                var battleId = ConvertUlongToString(evt.TargetId);
                var characterId = ConvertUlongToString(evt.ActorId);
                _logger.LogInformation("战斗开始事件: BattleId={BattleId}, CharacterId={CharacterId}",
                    battleId, characterId);
            });

            // 注册战斗结束处理器
            _dispatcher.RegisterHandler(GameEventTypes.BATTLE_ENDED, evt =>
            {
                try
                {
                    var data = evt.GetData<BattleEndedEventData>();
                    var battleId = ConvertUlongToString(data.BattleIdHash);
                    var victory = data.Victory == 1;

                    _logger.LogInformation("战斗结束事件: BattleId={BattleId}, Victory={Victory}, Duration={Duration}s",
                        battleId, victory, data.Duration);

                    // 清理战斗相关资源
                    CleanupBattle(battleId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理战斗结束事件失败");
                }
            });

            // 注册攻击事件处理器
            _dispatcher.RegisterHandler(GameEventTypes.BATTLE_ATTACK, evt =>
            {
                try
                {
                    var data = evt.GetData<BattleAttackEventData>();
                    _logger.LogDebug("攻击事件: 伤害={Damage}, 暴击={Crit}, 剩余血量={RemainingHealth}",
                        data.ActualDamage, data.IsCritical == 1, data.RemainingHealth);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理攻击事件失败");
                }
            });

            // 注册技能使用处理器
            _dispatcher.RegisterHandler(GameEventTypes.SKILL_USED, evt =>
            {
                var casterId = ConvertUlongToString(evt.ActorId);
                var targetId = ConvertUlongToString(evt.TargetId);
                _logger.LogDebug("技能使用: Caster={Caster}, Target={Target}",
                    casterId, targetId);
            });

            // 注册伤害事件处理器（领域事件）
            _dispatcher.RegisterHandler(DomainEventTypes.DAMAGE_DEALT_DOMAIN, evt =>
            {
                try
                {
                    var data = evt.GetData<DomainDamageEventData>();
                    _logger.LogDebug("领域伤害事件: Source={Source}, Target={Target}, Amount={Amount}",
                        data.SourceId, data.TargetId, data.Amount);

                    // 可以在这里更新统计数据等
                    UpdateDamageStatistics(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理领域伤害事件失败");
                }
            });
        }

        private async Task ProcessBattleTickAsync(string battleId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var battleService = scope.ServiceProvider.GetRequiredService<IBattleService>();

                _logger.LogDebug("开始处理战斗Tick: {BattleId}", battleId);
                await battleService.ProcessBattleTickAsync(battleId);
                _logger.LogDebug("战斗Tick处理完成: {BattleId}", battleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理战斗Tick失败: {BattleId}", battleId);
            }
        }

        private void CleanupBattle(string battleId)
        {
            // 清理战斗相关的事件和资源
            _logger.LogInformation("清理战斗资源: {BattleId}", battleId);

            // TODO: 实际的清理逻辑
            // 1. 从内存中移除战斗实例
            // 2. 取消相关的定时器
            // 3. 释放占用的资源
        }

        private void UpdateDamageStatistics(DomainDamageEventData data)
        {
            // 更新伤害统计
            // 这里可以实现累计伤害、DPS计算等功能
            _logger.LogDebug("更新伤害统计: Source={Source}, Damage={Damage}",
                data.SourceId, data.Amount);
        }

        private string ConvertUlongToString(ulong value)
        {
            // 简单的转换，实际可能需要更复杂的映射
            return value.ToString();
        }
    }

    /// <summary>
    /// 战斗结束事件数据
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BattleEndedEventData
        {
            public byte Victory;           // 1 byte - 是否胜利
            public ulong BattleIdHash;     // 8 bytes - 战斗ID哈希
            public int Duration;           // 4 bytes - 持续时间（秒）
            public int TotalDamage;        // 4 bytes - 总伤害
            public int ExpGained;          // 4 bytes - 获得经验
            public int GoldGained;         // 4 bytes - 获得金币
                                           // Total: 25 bytes
        }

        /// <summary>
        /// 战斗Tick事件数据
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BattleTickEventData
        {
            public ulong BattleId;         // 8 bytes
            public int TurnNumber;         // 4 bytes
            public int AliveCount;         // 4 bytes
                                           // Total: 16 bytes
        }
    }