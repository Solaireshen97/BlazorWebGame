using System;
using System.Threading.Tasks;
using BlazorWebGame.Shared.Events;
using BlazorWebGame.Server.Services.Battle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Events
{
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
            // 注册战斗Tick处理器
            _dispatcher.RegisterHandler(GameEventTypes.BATTLE_TICK, async evt =>
            {
                var battleId = ConvertUlongToString(evt.TargetId);
                await ProcessBattleTickAsync(battleId);
            });

            // 注册战斗开始处理器
            _dispatcher.RegisterHandler(GameEventTypes.BATTLE_STARTED, evt =>
            {
                var battleId = ConvertUlongToString(evt.TargetId);
                _logger.LogInformation("战斗开始事件: {BattleId}", battleId);
            });

            // 注册战斗结束处理器
            _dispatcher.RegisterHandler(GameEventTypes.BATTLE_ENDED, evt =>
            {
                var battleId = ConvertUlongToString(evt.TargetId);
                _logger.LogInformation("战斗结束事件: {BattleId}", battleId);

                // 清理战斗相关资源
                CleanupBattle(battleId);
            });

            // 注册攻击事件处理器
            _dispatcher.RegisterHandler(GameEventTypes.BATTLE_ATTACK, evt =>
            {
                var data = evt.GetData<BattleAttackEventData>();
                _logger.LogDebug("攻击事件: 伤害={Damage}, 暴击={Crit}",
                    data.ActualDamage, data.IsCritical == 1);
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
                var data = evt.GetData<DomainDamageEventData>();
                _logger.LogDebug("领域伤害事件: Source={Source}, Target={Target}, Amount={Amount}",
                    data.SourceId, data.TargetId, data.Amount);

                // 可以在这里更新统计数据等
                UpdateDamageStatistics(data);
            });
        }

        private async Task ProcessBattleTickAsync(string battleId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var battleService = scope.ServiceProvider.GetRequiredService<IBattleService>();
                await battleService.ProcessBattleTickAsync(battleId);
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
        }

        private void UpdateDamageStatistics(DomainDamageEventData data)
        {
            // 更新伤害统计
            // 这里可以实现累计伤害、DPS计算等功能
        }

        private string ConvertUlongToString(ulong value)
        {
            // 简单的转换，实际可能需要更复杂的映射
            return value.ToString();
        }
    }
}