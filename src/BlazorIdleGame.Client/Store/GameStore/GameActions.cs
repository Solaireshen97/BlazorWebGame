using BlazorIdleGame.Client.Models;

namespace BlazorIdleGame.Client.Store.GameStore
{
    public class LoadGameStateAction { }

    public class LoadGameStateSuccessAction
    {
        public Models.GameState GameState { get; }

        public LoadGameStateSuccessAction(Models.GameState gameState)
        {
            GameState = gameState;
        }
    }

    public class LoadGameStateFailureAction
    {
        public string ErrorMessage { get; }

        public LoadGameStateFailureAction(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }

    public class UpdateGameStateAction
    {
        public Models.GameState GameState { get; }

        public UpdateGameStateAction(Models.GameState gameState)
        {
            GameState = gameState;
        }
    }

    public class SetConnectionStatusAction
    {
        public bool IsConnected { get; }

        public SetConnectionStatusAction(bool isConnected)
        {
            IsConnected = isConnected;
        }
    }

    public class StartActivityAction
    {
        public ActivityType Type { get; }
        public string Target { get; }

        public StartActivityAction(ActivityType type, string target)
        {
            Type = type;
            Target = target;
        }
    }

    public class StopActivityAction
    {
        public string ActivityId { get; }

        public StopActivityAction(string activityId)
        {
            ActivityId = activityId;
        }
    }

    public class SendGameActionAction
    {
        public GameAction Action { get; }

        public SendGameActionAction(GameAction action)
        {
            Action = action;
        }
    }
}