using BlazorIdleGame.Client.Services;
using Fluxor;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BlazorIdleGame.Client.Store.PartyStore
{
    public class PartyEffects
    {
        private readonly IPartyService _partyService;
        private readonly ILogger<PartyEffects> _logger;

        public PartyEffects(IPartyService partyService, ILogger<PartyEffects> logger)
        {
            _partyService = partyService;
            _logger = logger;
        }

        [EffectMethod]
        public async Task HandleLoadAvailablePartiesAction(LoadAvailablePartiesAction action, IDispatcher dispatcher)
        {
            try
            {
                var parties = await _partyService.GetAvailablePartiesAsync();
                dispatcher.Dispatch(new LoadAvailablePartiesSuccessAction(parties));
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "加载可用队伍失败");
                dispatcher.Dispatch(new PartyOperationFailureAction(ex.Message));
            }
        }

        [EffectMethod]
        public async Task HandleCreatePartyAction(CreatePartyAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _partyService.CreatePartyAsync(action.Name, action.MaxMembers);
                if (success)
                {
                    dispatcher.Dispatch(new PartyOperationSuccessAction());
                }
                else
                {
                    dispatcher.Dispatch(new PartyOperationFailureAction("创建队伍失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "创建队伍失败");
                dispatcher.Dispatch(new PartyOperationFailureAction(ex.Message));
            }
        }

        [EffectMethod]
        public async Task HandleJoinPartyAction(JoinPartyAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _partyService.JoinPartyAsync(action.PartyId);
                if (success)
                {
                    dispatcher.Dispatch(new PartyOperationSuccessAction());
                }
                else
                {
                    dispatcher.Dispatch(new PartyOperationFailureAction("加入队伍失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "加入队伍失败");
                dispatcher.Dispatch(new PartyOperationFailureAction(ex.Message));
            }
        }

        [EffectMethod]
        public async Task HandleLeavePartyAction(LeavePartyAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _partyService.LeavePartyAsync();
                if (success)
                {
                    dispatcher.Dispatch(new PartyOperationSuccessAction());
                }
                else
                {
                    dispatcher.Dispatch(new PartyOperationFailureAction("离开队伍失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "离开队伍失败");
                dispatcher.Dispatch(new PartyOperationFailureAction(ex.Message));
            }
        }

        [EffectMethod]
        public async Task HandleInvitePlayerAction(InvitePlayerAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _partyService.InvitePlayerAsync(action.PlayerId);
                if (success)
                {
                    dispatcher.Dispatch(new PartyOperationSuccessAction());
                }
                else
                {
                    dispatcher.Dispatch(new PartyOperationFailureAction("邀请玩家失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "邀请玩家失败");
                dispatcher.Dispatch(new PartyOperationFailureAction(ex.Message));
            }
        }

        [EffectMethod]
        public async Task HandleKickMemberAction(KickMemberAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _partyService.KickMemberAsync(action.MemberId);
                if (success)
                {
                    dispatcher.Dispatch(new PartyOperationSuccessAction());
                }
                else
                {
                    dispatcher.Dispatch(new PartyOperationFailureAction("踢出成员失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "踢出成员失败");
                dispatcher.Dispatch(new PartyOperationFailureAction(ex.Message));
            }
        }

        [EffectMethod]
        public async Task HandlePromoteToLeaderAction(PromoteToLeaderAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _partyService.PromoteToLeaderAsync(action.MemberId);
                if (success)
                {
                    dispatcher.Dispatch(new PartyOperationSuccessAction());
                }
                else
                {
                    dispatcher.Dispatch(new PartyOperationFailureAction("转让队长失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "转让队长失败");
                dispatcher.Dispatch(new PartyOperationFailureAction(ex.Message));
            }
        }

        [EffectMethod]
        public async Task HandleSetReadyStatusAction(SetReadyStatusAction action, IDispatcher dispatcher)
        {
            try
            {
                var success = await _partyService.SetReadyStatusAsync(action.IsReady);
                if (success)
                {
                    dispatcher.Dispatch(new PartyOperationSuccessAction());
                }
                else
                {
                    dispatcher.Dispatch(new PartyOperationFailureAction("设置准备状态失败"));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "设置准备状态失败");
                dispatcher.Dispatch(new PartyOperationFailureAction(ex.Message));
            }
        }
    }
}