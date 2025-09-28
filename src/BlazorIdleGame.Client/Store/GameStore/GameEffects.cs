using BlazorIdleGame.Client.Services;
using Fluxor;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BlazorIdleGame.Client.Store.GameStore
{
    public class GameEffects
    {
        private readonly IGameService _gameService;
        private readonly ILogger<GameEffects> _logger;

        public GameEffects(IGameService gameService, ILogger<GameEffects> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }

        [EffectMethod]
        public async Task HandleLoadGameStateAction(LoadGameStateAction action, IDispatcher dispatcher)
        {
            try
            {
                await _gameService.InitializeAsync();
                
                if (_gameService.CurrentState != null)
                {
                    dispatcher.Dispatch(new LoadGameStateSuccessAction(_gameService.CurrentState));
                }
                else
                {
                    dispatcher.Dispatch(new LoadGameStateFailureAction("游戏状态为空"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "加载游戏状态失败");
                dispatcher.Dispatch(new LoadGameStateFailureAction(ex.Message));
            }
        }

        [EffectMethod]
        public async Task HandleStartActivityAction(StartActivityAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _gameService.StartActivityAsync(action.Type, action.Target);
                if (!success)
                {
                    dispatcher.Dispatch(new LoadGameStateFailureAction("开始活动失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "开始活动失败");
                dispatcher.Dispatch(new LoadGameStateFailureAction(ex.Message));
            }
        }

        [EffectMethod]
        public async Task HandleStopActivityAction(StopActivityAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _gameService.StopActivityAsync(action.ActivityId);
                if (!success)
                {
                    dispatcher.Dispatch(new LoadGameStateFailureAction("停止活动失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "停止活动失败");
                dispatcher.Dispatch(new LoadGameStateFailureAction(ex.Message));
            }
        }

        [EffectMethod]
        public async Task HandleSendGameActionAction(SendGameActionAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _gameService.SendActionAsync(action.Action);
                if (!success)
                {
                    dispatcher.Dispatch(new LoadGameStateFailureAction("发送游戏操作失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "发送游戏操作失败");
                dispatcher.Dispatch(new LoadGameStateFailureAction(ex.Message));
            }
        }
    }
}