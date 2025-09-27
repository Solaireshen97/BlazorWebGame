using BlazorIdleGame.Client.Models;

namespace BlazorIdleGame.Client.Store.BattleStore
{
    public class StartBattleAction
    {
        public string EnemyId { get; }
        public bool IsPartyBattle { get; }

        public StartBattleAction(string enemyId, bool isPartyBattle = false)
        {
            EnemyId = enemyId;
            IsPartyBattle = isPartyBattle;
        }
    }

    public class FleeBattleAction { }

    public class UseSkillAction
    {
        public string SkillId { get; }
        public string TargetId { get; }

        public UseSkillAction(string skillId, string targetId)
        {
            SkillId = skillId;
            TargetId = targetId;
        }
    }

    public class UpdateBattleStateAction
    {
        public Models.BattleState BattleState { get; }

        public UpdateBattleStateAction(Models.BattleState battleState)
        {
            BattleState = battleState;
        }
    }

    public class BattleLogAddedAction
    {
        public BattleLog Log { get; }

        public BattleLogAddedAction(BattleLog log)
        {
            Log = log;
        }
    }

    public class BattleCompletedAction
    {
        public BattleRewards Rewards { get; }

        public BattleCompletedAction(BattleRewards rewards)
        {
            Rewards = rewards;
        }
    }

    public class BattleOperationSuccessAction { }

    public class BattleOperationFailureAction
    {
        public string ErrorMessage { get; }

        public BattleOperationFailureAction(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}