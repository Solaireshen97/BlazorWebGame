using BlazorIdleGame.Client.Services;
using Fluxor;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BlazorIdleGame.Client.Store.BattleStore
{
    public class BattleEffects
    {
        private readonly IBattleService _battleService;
        private readonly ILogger<BattleEffects> _logger;

        public BattleEffects(IBattleService battleService, ILogger<BattleEffects> logger)
        {
            _battleService = battleService;
            _logger = logger;
        }

        [EffectMethod]
        public async Task HandleStartBattleAction(StartBattleAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _battleService.StartBattleAsync(action.EnemyId, action.IsPartyBattle);
                if (success)
                {
                    dispatcher.Dispatch(new BattleOperationSuccessAction());
                }
                else
                {
                    dispatcher.Dispatch(new BattleOperationFailureAction("开始战斗失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "开始战斗失败");
                dispatcher.Dispatch(new BattleOperationFailureAction(ex.Message));
            }
        }

        [EffectMethod]
        public async Task HandleFleeBattleAction(FleeBattleAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _battleService.FleeBattleAsync();
                if (success)
                {
                    dispatcher.Dispatch(new BattleOperationSuccessAction());
                }
                else
                {
                    dispatcher.Dispatch(new BattleOperationFailureAction("逃离战斗失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "逃离战斗失败");
                dispatcher.Dispatch(new BattleOperationFailureAction(ex.Message));
            }
        }

        [EffectMethod]
        public async Task HandleUseSkillAction(UseSkillAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _battleService.UseSkillAsync(action.SkillId, action.TargetId);
                if (success)
                {
                    dispatcher.Dispatch(new BattleOperationSuccessAction());
                }
                else
                {
                    dispatcher.Dispatch(new BattleOperationFailureAction("使用技能失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "使用技能失败");
                dispatcher.Dispatch(new BattleOperationFailureAction(ex.Message));
            }
        }
    }
}