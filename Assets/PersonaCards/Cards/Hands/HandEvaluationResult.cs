using System;
using System.Collections.Generic;
using PersonaCards.Core;

namespace PersonaCards.Cards.Hands
{
    public sealed class HandEvaluationResult
    {
        private readonly IReadOnlyList<string> _scoringCardIds;
        private readonly IReadOnlyList<PlayingCardInstance> _playedCards;

        public HandEvaluationResult(
            HandTypeDefinition definition,
            IEnumerable<string> scoringCardIds,
            IReadOnlyList<PlayingCardInstance> playedCards = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));

            if (scoringCardIds == null)
            {
                throw new ArgumentNullException(nameof(scoringCardIds));
            }

            _scoringCardIds = Array.AsReadOnly(new List<string>(scoringCardIds).ToArray());
            // 本手已打出的牌（P0-11 花色强化按计分牌花色逐张加筹码用）；null → 空列表，旧调用点零差异
            _playedCards = playedCards == null
                ? Array.Empty<PlayingCardInstance>()
                : Array.AsReadOnly(new List<PlayingCardInstance>(playedCards).ToArray());
        }

        public HandTypeDefinition Definition { get; }

        public HandType HandType => Definition.HandType;

        public int Priority => Definition.Priority;

        public int BaseChips => Definition.BaseChips;

        public decimal BaseMultiplier => Definition.BaseMultiplier;

        public IReadOnlyList<string> ScoringCardIds => _scoringCardIds;

        /// <summary>本手已打出的牌（P0-11：花色强化效果按此逐张查花色；旧调用点未传则为空列表）。</summary>
        public IReadOnlyList<PlayingCardInstance> PlayedCards => _playedCards;
    }
}
