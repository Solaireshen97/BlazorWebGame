using Fluxor;

namespace BlazorIdleGame.Client.Store.BattleStore
{
    public static class BattleReducers
    {
        [ReducerMethod]
        public static BattleState ReduceStartBattleAction(BattleState state, StartBattleAction action) =>
            new BattleState(state.CurrentBattle, true, null);

        [ReducerMethod]
        public static BattleState ReduceUpdateBattleStateAction(BattleState state, UpdateBattleStateAction action) =>
            new BattleState(action.BattleState, false, null);

        [ReducerMethod]
        public static BattleState ReduceFleeBattleAction(BattleState state, FleeBattleAction action) =>
            new BattleState(null, false, null);

        [ReducerMethod]
        public static BattleState ReduceBattleOperationSuccessAction(BattleState state, BattleOperationSuccessAction action) =>
            new BattleState(state.CurrentBattle, false, null);

        [ReducerMethod]
        public static BattleState ReduceBattleOperationFailureAction(BattleState state, BattleOperationFailureAction action) =>
            new BattleState(state.CurrentBattle, false, action.ErrorMessage);
    }
}