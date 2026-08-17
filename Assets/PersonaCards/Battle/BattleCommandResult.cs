using PersonaCards.Cards.Scoring;

namespace PersonaCards.Battle
{
    public sealed class BattleCommandResult
    {
        private BattleCommandResult(bool succeeded, BattleCommandFailure failure, ScoringResult scoringResult)
        {
            Succeeded = succeeded;
            Failure = failure;
            ScoringResult = scoringResult;
        }

        public bool Succeeded { get; }
        public BattleCommandFailure Failure { get; }
        public ScoringResult ScoringResult { get; }

        public static BattleCommandResult Success(ScoringResult scoringResult = null)
        {
            return new BattleCommandResult(true, BattleCommandFailure.None, scoringResult);
        }

        public static BattleCommandResult Rejected(BattleCommandFailure failure)
        {
            return new BattleCommandResult(false, failure, null);
        }
    }
}
