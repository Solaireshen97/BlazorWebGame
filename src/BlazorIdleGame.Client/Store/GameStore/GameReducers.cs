using Fluxor;

namespace BlazorIdleGame.Client.Store.GameStore
{
    public static class GameReducers
    {
        [ReducerMethod]
        public static GameState ReduceLoadGameStateAction(GameState state, LoadGameStateAction action) =>
            new GameState(state.CurrentGameState, true, state.IsConnected, null);

        [ReducerMethod]
        public static GameState ReduceLoadGameStateSuccessAction(GameState state, LoadGameStateSuccessAction action) =>
            new GameState(action.GameState, false, true, null);

        [ReducerMethod]
        public static GameState ReduceLoadGameStateFailureAction(GameState state, LoadGameStateFailureAction action) =>
            new GameState(state.CurrentGameState, false, false, action.ErrorMessage);

        [ReducerMethod]
        public static GameState ReduceUpdateGameStateAction(GameState state, UpdateGameStateAction action) =>
            new GameState(action.GameState, state.IsLoading, state.IsConnected, null);

        [ReducerMethod]
        public static GameState ReduceSetConnectionStatusAction(GameState state, SetConnectionStatusAction action) =>
            new GameState(state.CurrentGameState, state.IsLoading, action.IsConnected, state.ErrorMessage);
    }
}