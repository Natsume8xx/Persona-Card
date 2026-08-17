namespace PersonaCards.Cards.Scoring
{
    using PersonaCards.Cards.Hands;

    public interface IScoringEffect
    {
        ScoringPhase Phase { get; }
        int Order { get; }
        ScoringSourceType SourceType { get; }
        string SourceId { get; }
        void Apply(ScoringContext context, HandEvaluationResult evaluation);
    }
}
