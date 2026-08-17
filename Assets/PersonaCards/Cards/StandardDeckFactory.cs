using System;
using System.Collections.Generic;

namespace PersonaCards.Cards
{
    public static class StandardDeckFactory
    {
        public const int StandardCardCount = 52;

        public static IReadOnlyList<PlayingCardInstance> Create()
        {
            var cards = new List<PlayingCardInstance>(StandardCardCount);

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                for (var rankValue = (int)Rank.Two; rankValue <= (int)Rank.Ace; rankValue++)
                {
                    var rank = (Rank)rankValue;
                    cards.Add(new PlayingCardInstance(CreateId(suit, rank), suit, rank));
                }
            }

            return cards.AsReadOnly();
        }

        public static string CreateId(Suit suit, Rank rank)
        {
            return $"standard-{suit.ToString().ToLowerInvariant()}-{(int)rank}";
        }
    }
}
