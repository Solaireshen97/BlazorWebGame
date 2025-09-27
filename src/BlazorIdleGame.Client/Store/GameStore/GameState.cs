using BlazorIdleGame.Client.Models;
using Fluxor;

namespace BlazorIdleGame.Client.Store.GameStore
{
    [FeatureState]
    public class GameState
    {
        public Models.GameState? CurrentGameState { get; }
        public bool IsLoading { get; }
        public bool IsConnected { get; }
        public string? ErrorMessage { get; }

        public GameState() { }

        public GameState(Models.GameState? currentGameState, bool isLoading, bool isConnected, string? errorMessage)
        {
            CurrentGameState = currentGameState;
            IsLoading = isLoading;
            IsConnected = isConnected;
            ErrorMessage = errorMessage;
        }
    }
}