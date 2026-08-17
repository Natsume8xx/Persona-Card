using System;
using System.Collections.Generic;

namespace PersonaCards.Cards.Scoring
{
    public sealed class ScoringResult
    {
        public ScoringResult(ScoringContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Chips = context.Chips;
            Multiplier = context.Multiplier;
            FinalMultiplier = context.FinalMultiplier;
            RawScore = context.RawScore;
            FinalScore = context.FinalScore;
            Events = Array.AsReadOnly(new List<ScoringEvent>(context.Events).ToArray());
        }

        public decimal Chips { get; }
        public decimal Multiplier { get; }
        public decimal FinalMultiplier { get; }
        public decimal RawScore { get; }
        public long FinalScore { get; }
        public IReadOnlyList<ScoringEvent> Events { get; }
    }
}
