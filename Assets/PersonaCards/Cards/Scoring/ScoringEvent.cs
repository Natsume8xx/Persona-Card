namespace PersonaCards.Cards.Scoring
{
    public sealed class ScoringEvent
    {
        public ScoringEvent(
            ScoringPhase phase,
            ScoringSourceType sourceType,
            string sourceId,
            ScoringOperation operation,
            decimal value,
            decimal before,
            decimal after,
            string displayTextKey)
        {
            Phase = phase;
            SourceType = sourceType;
            SourceId = sourceId;
            Operation = operation;
            Value = value;
            Before = before;
            After = after;
            DisplayTextKey = displayTextKey;
        }

        public ScoringPhase Phase { get; }
        public ScoringSourceType SourceType { get; }
        public string SourceId { get; }
        public ScoringOperation Operation { get; }
        public decimal Value { get; }
        public decimal Before { get; }
        public decimal After { get; }
        public string DisplayTextKey { get; }
    }
}
