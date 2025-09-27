using Fluxor;
using System.Linq;

namespace BlazorIdleGame.Client.Store.PartyStore
{
    public static class PartyReducers
    {
        [ReducerMethod]
        public static PartyState ReduceLoadAvailablePartiesAction(PartyState state, LoadAvailablePartiesAction action) =>
            new PartyState(state.CurrentParty, state.AvailableParties, state.PendingInvites, true, null);

        [ReducerMethod]
        public static PartyState ReduceLoadAvailablePartiesSuccessAction(PartyState state, LoadAvailablePartiesSuccessAction action) =>
            new PartyState(state.CurrentParty, action.Parties, state.PendingInvites, false, null);

        [ReducerMethod]
        public static PartyState ReduceUpdatePartyAction(PartyState state, UpdatePartyAction action) =>
            new PartyState(action.Party, state.AvailableParties, state.PendingInvites, state.IsLoading, null);

        [ReducerMethod]
        public static PartyState ReduceReceiveInviteAction(PartyState state, ReceiveInviteAction action)
        {
            var newInvites = state.PendingInvites.ToList();
            newInvites.Add(action.Invite);
            return new PartyState(state.CurrentParty, state.AvailableParties, newInvites, state.IsLoading, null);
        }

        [ReducerMethod]
        public static PartyState ReducePartyOperationSuccessAction(PartyState state, PartyOperationSuccessAction action) =>
            new PartyState(state.CurrentParty, state.AvailableParties, state.PendingInvites, false, null);

        [ReducerMethod]
        public static PartyState ReducePartyOperationFailureAction(PartyState state, PartyOperationFailureAction action) =>
            new PartyState(state.CurrentParty, state.AvailableParties, state.PendingInvites, false, action.ErrorMessage);

        [ReducerMethod]
        public static PartyState ReduceLeavePartyAction(PartyState state, LeavePartyAction action) =>
            new PartyState(null, state.AvailableParties, state.PendingInvites, state.IsLoading, null);
    }
}