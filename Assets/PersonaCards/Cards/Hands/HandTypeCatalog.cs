using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PersonaCards.Cards.Hands
{
    public static class HandTypeCatalog
    {
        private static readonly IReadOnlyDictionary<HandType, HandTypeDefinition> Definitions =
            new ReadOnlyDictionary<HandType, HandTypeDefinition>(
                new Dictionary<HandType, HandTypeDefinition>
                {
                    [HandType.HighCard] = new HandTypeDefinition(HandType.HighCard, "高牌", 5, 1),
                    [HandType.Pair] = new HandTypeDefinition(HandType.Pair, "对子", 10, 2),
                    [HandType.TwoPair] = new HandTypeDefinition(HandType.TwoPair, "两对", 20, 2),
                    [HandType.ThreeOfAKind] = new HandTypeDefinition(HandType.ThreeOfAKind, "三条", 30, 3),
                    [HandType.Straight] = new HandTypeDefinition(HandType.Straight, "顺子", 30, 4),
                    [HandType.Flush] = new HandTypeDefinition(HandType.Flush, "同花", 35, 4),
                    [HandType.FullHouse] = new HandTypeDefinition(HandType.FullHouse, "葫芦", 40, 4),
                    [HandType.FourOfAKind] = new HandTypeDefinition(HandType.FourOfAKind, "四条", 60, 7),
                    [HandType.StraightFlush] = new HandTypeDefinition(HandType.StraightFlush, "同花顺", 100, 8),
                    [HandType.FiveOfAKind] = new HandTypeDefinition(HandType.FiveOfAKind, "五条", 100, 8),
                    [HandType.FlushHouse] = new HandTypeDefinition(HandType.FlushHouse, "同花葫芦", 100, 8),
                    [HandType.FlushFive] = new HandTypeDefinition(HandType.FlushFive, "同花五条", 100, 8)
                });

        public static IReadOnlyCollection<HandTypeDefinition> All { get; } =
            new ReadOnlyCollection<HandTypeDefinition>(
                new List<HandTypeDefinition>(Definitions.Values));

        public static HandTypeDefinition Get(HandType handType)
        {
            if (!Definitions.TryGetValue(handType, out var definition))
            {
                throw new ArgumentOutOfRangeException(nameof(handType), handType, "Unknown hand type.");
            }

            return definition;
        }
    }
}
