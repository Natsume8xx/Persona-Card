using System;
using System.Collections.Generic;
using PersonaCards.Core;

namespace PersonaCards.Cards.Hands
{
    public sealed class HandEvaluationResult
    {
        private readonly IReadOnlyList<string> _scoringCardIds;

        public HandEvaluationResult(
            HandTypeDefinition definition,
            IEnumerable<string> scoringCardIds)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));

            if (scoringCardIds == null)
            {
                throw new ArgumentNullException(nameof(scoringCardIds));
            }

            _scoringCardIds = Array.AsReadOnly(new List<string>(scoringCardIds).ToArray());
        }

        public HandTypeDefinition Definition { get; }

        public HandType HandType => Definition.HandType;

        public int Priority => Definition.Priority;

        public int BaseChips => Definition.BaseChips;

        public decimal BaseMultiplier => Definition.BaseMultiplier;

        public IReadOnlyList<string> ScoringCardIds => _scoringCardIds;
    }
}
