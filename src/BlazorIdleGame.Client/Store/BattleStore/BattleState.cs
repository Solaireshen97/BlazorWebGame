using BlazorIdleGame.Client.Models;
using Fluxor;

namespace BlazorIdleGame.Client.Store.BattleStore
{
    [FeatureState]
    public class BattleState
    {
        public Models.BattleState? CurrentBattle { get; }
        public bool IsLoading { get; }
        public string? ErrorMessage { get; }

        public BattleState() { }

        public BattleState(Models.BattleState? currentBattle, bool isLoading, string? errorMessage)
        {
            CurrentBattle = currentBattle;
            IsLoading = isLoading;
            ErrorMessage = errorMessage;
        }
    }
}