using System;
using System.Collections.Generic;
using System.Linq;
using PersonaCards.Core.Random;

namespace PersonaCards.Cards
{
    public sealed class DeckState
    {
        private readonly List<PlayingCardInstance> _drawPile;
        private readonly List<PlayingCardInstance> _hand = new List<PlayingCardInstance>();
        private readonly List<PlayingCardInstance> _played = new List<PlayingCardInstance>();
        private readonly List<PlayingCardInstance> _discarded = new List<PlayingCardInstance>();

        private readonly IReadOnlyList<PlayingCardInstance> _drawPileView;
        private readonly IReadOnlyList<PlayingCardInstance> _handView;
        private readonly IReadOnlyList<PlayingCardInstance> _playedView;
        private readonly IReadOnlyList<PlayingCardInstance> _discardedView;

        public DeckState(IEnumerable<PlayingCardInstance> cards)
            : this(cards, Array.Empty<PlayingCardInstance>(), Array.Empty<PlayingCardInstance>(),
                Array.Empty<PlayingCardInstance>())
        {
        }

        public DeckState(IEnumerable<PlayingCardInstance> drawPile, IEnumerable<PlayingCardInstance> hand,
            IEnumerable<PlayingCardInstance> played, IEnumerable<PlayingCardInstance> discarded)
        {
            _drawPile = (drawPile ?? throw new ArgumentNullException(nameof(drawPile))).ToList();
            _hand.AddRange(hand ?? throw new ArgumentNullException(nameof(hand)));
            _played.AddRange(played ?? throw new ArgumentNullException(nameof(played)));
            _discarded.AddRange(discarded ?? throw new ArgumentNullException(nameof(discarded)));
            EnsureValidStartingCards(_drawPile.Concat(_hand).Concat(_played).Concat(_discarded).ToArray());

            _drawPileView = _drawPile.AsReadOnly();
            _handView = _hand.AsReadOnly();
            _playedView = _played.AsReadOnly();
            _discardedView = _discarded.AsReadOnly();
            ValidateCardConservation();
        }

        public IReadOnlyList<PlayingCardInstance> DrawPile => _drawPileView;

        public IReadOnlyList<PlayingCardInstance> Hand => _handView;

        public IReadOnlyList<PlayingCardInstance> Played => _playedView;

        public IReadOnlyList<PlayingCardInstance> Discarded => _discardedView;

        public int TotalCardCount => _drawPile.Count + _hand.Count + _played.Count + _discarded.Count;

        public void ShuffleDrawPile(ISeededRng rng)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            for (var index = _drawPile.Count - 1; index > 0; index--)
            {
                var swapIndex = rng.NextInt(index + 1);
                (_drawPile[index], _drawPile[swapIndex]) = (_drawPile[swapIndex], _drawPile[index]);
            }
        }

        public IReadOnlyList<PlayingCardInstance> Draw(int requestedCount)
        {
            if (requestedCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedCount),
                    requestedCount,
                    "Draw count cannot be negative.");
            }

            var drawCount = Math.Min(requestedCount, _drawPile.Count);
            var drawnCards = new PlayingCardInstance[drawCount];

            for (var index = 0; index < drawCount; index++)
            {
                var drawIndex = _drawPile.Count - 1;
                var card = _drawPile[drawIndex];
                _drawPile.RemoveAt(drawIndex);
                _hand.Add(card);
                drawnCards[index] = card;
            }

            return drawnCards;
        }

        public void MoveCards(IEnumerable<string> cardIds, CardZone from, CardZone to)
        {
            if (cardIds == null)
            {
                throw new ArgumentNullException(nameof(cardIds));
            }

            if (from == to)
            {
                throw new ArgumentException("Source and destination zones must be different.", nameof(to));
            }

            var source = GetMutableZone(from);
            var destination = GetMutableZone(to);
            var requestedIds = cardIds.ToList();

            if (requestedIds.Count != requestedIds.Distinct(StringComparer.Ordinal).Count())
            {
                throw new ArgumentException("A card cannot be moved more than once in one operation.", nameof(cardIds));
            }

            var cardsToMove = new List<PlayingCardInstance>(requestedIds.Count);
            foreach (var cardId in requestedIds)
            {
                var card = source.Find(candidate => string.Equals(candidate.Id, cardId, StringComparison.Ordinal));
                if (card == null)
                {
                    throw new InvalidOperationException($"Card '{cardId}' is not in {from}.");
                }

                cardsToMove.Add(card);
            }

            foreach (var card in cardsToMove)
            {
                source.Remove(card);
                destination.Add(card);
            }
        }

        public IReadOnlyList<PlayingCardInstance> GetCards(CardZone zone)
        {
            return zone switch
            {
                CardZone.DrawPile => DrawPile,
                CardZone.Hand => Hand,
                CardZone.Played => Played,
                CardZone.Discarded => Discarded,
                _ => throw new ArgumentOutOfRangeException(nameof(zone), zone, "Unknown card zone.")
            };
        }

        public void ValidateCardConservation()
        {
            var allCards = _drawPile.Concat(_hand).Concat(_played).Concat(_discarded).ToList();

            if (allCards.Any(card => card == null))
            {
                throw new InvalidOperationException("A card zone contains a null card.");
            }

            if (allCards.Select(card => card.Id).Distinct(StringComparer.Ordinal).Count() != allCards.Count)
            {
                throw new InvalidOperationException("A card appears in more than one zone, or duplicate ids exist.");
            }
        }

        private static void EnsureValidStartingCards(IReadOnlyCollection<PlayingCardInstance> cards)
        {
            if (cards.Any(card => card == null))
            {
                throw new ArgumentException("Starting cards cannot contain null entries.", nameof(cards));
            }

            if (cards.Select(card => card.Id).Distinct(StringComparer.Ordinal).Count() != cards.Count)
            {
                throw new ArgumentException("Starting card ids must be unique.", nameof(cards));
            }
        }

        private List<PlayingCardInstance> GetMutableZone(CardZone zone)
        {
            return zone switch
            {
                CardZone.DrawPile => _drawPile,
                CardZone.Hand => _hand,
                CardZone.Played => _played,
                CardZone.Discarded => _discarded,
                _ => throw new ArgumentOutOfRangeException(nameof(zone), zone, "Unknown card zone.")
            };
        }
    }
}
