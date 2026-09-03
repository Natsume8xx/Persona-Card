using System;
using System.Collections.Generic;
using System.Linq;
using PersonaCards.Core;

namespace PersonaCards.Cards.Hands
{
    public sealed class HandEvaluator
    {
        public const int MinimumCards = 1;
        public const int MaximumCards = 5;

        public HandEvaluationResult Evaluate(IEnumerable<PlayingCardInstance> selectedCards)
        {
            if (selectedCards == null)
            {
                throw new ArgumentNullException(nameof(selectedCards));
            }

            var cards = selectedCards.ToList();
            ValidateSelection(cards);

            var rankGroups = cards
                .GroupBy(card => card.Rank)
                .OrderByDescending(group => group.Count())
                .ThenByDescending(group => (int)group.Key)
                .ToList();

            var isFiveCardHand = cards.Count == MaximumCards;
            var isFlush = isFiveCardHand && IsFlush(cards);
            var isStraight = isFiveCardHand && IsStraight(cards);
            // 皇家同花顺（P0-1J）：DetermineHandType 没有 rank 信息，调用处算好传入（A2345 轮子不算）
            var isRoyalFlush = isFiveCardHand && isFlush && IsRoyalFlush(cards);
            var groupCounts = rankGroups.Select(group => group.Count()).OrderByDescending(count => count).ToArray();

            var handType = DetermineHandType(groupCounts, isFlush, isStraight, isRoyalFlush);
            var scoringCardIds = SelectScoringCardIds(cards, rankGroups, handType);

            return new HandEvaluationResult(HandTypeCatalog.Get(handType), scoringCardIds, cards);
        }

        private static HandType DetermineHandType(
            IReadOnlyList<int> groupCounts,
            bool isFlush,
            bool isStraight,
            bool isRoyalFlush)
        {
            if (groupCounts[0] == 5 && isFlush)
            {
                return HandType.FlushFive;
            }

            if (IsFullHouse(groupCounts) && isFlush)
            {
                return HandType.FlushHouse;
            }

            if (groupCounts[0] == 5)
            {
                return HandType.FiveOfAKind;
            }

            // 皇家同花顺 = 10~A 同花顺：必须插在 StraightFlush 之前（两者判定条件重叠，皇家更窄）
            if (isRoyalFlush)
            {
                return HandType.RoyalFlush;
            }

            if (isStraight && isFlush)
            {
                return HandType.StraightFlush;
            }

            if (groupCounts[0] == 4)
            {
                return HandType.FourOfAKind;
            }

            if (IsFullHouse(groupCounts))
            {
                return HandType.FullHouse;
            }

            if (isFlush)
            {
                return HandType.Flush;
            }

            if (isStraight)
            {
                return HandType.Straight;
            }

            if (groupCounts[0] == 3)
            {
                return HandType.ThreeOfAKind;
            }

            if (groupCounts.Count(count => count == 2) == 2)
            {
                return HandType.TwoPair;
            }

            if (groupCounts[0] == 2)
            {
                return HandType.Pair;
            }

            return HandType.HighCard;
        }

        private static IReadOnlyList<string> SelectScoringCardIds(
            IReadOnlyList<PlayingCardInstance> cards,
            IReadOnlyList<IGrouping<Rank, PlayingCardInstance>> rankGroups,
            HandType handType)
        {
            switch (handType)
            {
                case HandType.HighCard:
                    return new[]
                    {
                        cards.OrderByDescending(card => (int)card.Rank).First().Id
                    };

                case HandType.Pair:
                    return SelectIdsWithGroupSize(cards, rankGroups, 2, 1);

                case HandType.TwoPair:
                    return SelectIdsWithGroupSize(cards, rankGroups, 2, 2);

                case HandType.ThreeOfAKind:
                    return SelectIdsWithGroupSize(cards, rankGroups, 3, 1);

                case HandType.FourOfAKind:
                    return SelectIdsWithGroupSize(cards, rankGroups, 4, 1);

                case HandType.FiveOfAKind:
                    return SelectIdsWithGroupSize(cards, rankGroups, 5, 1);

                default:
                    return cards.Select(card => card.Id).ToArray();
            }
        }

        private static IReadOnlyList<string> SelectIdsWithGroupSize(
            IReadOnlyList<PlayingCardInstance> cards,
            IEnumerable<IGrouping<Rank, PlayingCardInstance>> rankGroups,
            int groupSize,
            int groupLimit)
        {
            var scoringRanks = new HashSet<Rank>(
                rankGroups
                    .Where(group => group.Count() == groupSize)
                    .Take(groupLimit)
                    .Select(group => group.Key));

            return cards
                .Where(card => scoringRanks.Contains(card.Rank))
                .Select(card => card.Id)
                .ToArray();
        }

        private static bool IsFullHouse(IReadOnlyList<int> groupCounts)
        {
            return groupCounts.Count == 2 && groupCounts[0] == 3 && groupCounts[1] == 2;
        }

        private static bool IsStraight(IEnumerable<PlayingCardInstance> cards)
        {
            var ranks = cards
                .Select(card => (int)card.Rank)
                .Distinct()
                .OrderBy(rank => rank)
                .ToArray();

            if (ranks.Length != MaximumCards)
            {
                return false;
            }

            var isWheel = ranks.SequenceEqual(new[] { 2, 3, 4, 5, 14 });
            if (isWheel)
            {
                return true;
            }

            for (var index = 1; index < ranks.Length; index++)
            {
                if (ranks[index] != ranks[0] + index)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>皇家同花顺：点数集合恰为 {10,J,Q,K,A}（A2345 轮子不算皇家，走 StraightFlush）。</summary>
        private static bool IsRoyalFlush(IEnumerable<PlayingCardInstance> cards)
        {
            var ranks = cards
                .Select(card => (int)card.Rank)
                .Distinct()
                .OrderBy(rank => rank)
                .ToArray();

            return ranks.SequenceEqual(new[]
            {
                (int)Rank.Ten, (int)Rank.Jack, (int)Rank.Queen, (int)Rank.King, (int)Rank.Ace
            });
        }

        private static bool IsFlush(IEnumerable<PlayingCardInstance> cards)
        {
            var fixedSuits = cards
                .Where(card => card.Enhancement != CardEnhancement.WildSuit)
                .Select(card => card.Suit)
                .Distinct()
                .Take(2)
                .Count();

            return fixedSuits <= 1;
        }

        private static void ValidateSelection(IReadOnlyCollection<PlayingCardInstance> cards)
        {
            if (cards.Count < MinimumCards || cards.Count > MaximumCards)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cards),
                    cards.Count,
                    $"A hand must contain {MinimumCards} to {MaximumCards} cards.");
            }

            if (cards.Any(card => card == null))
            {
                throw new ArgumentException("A hand cannot contain null cards.", nameof(cards));
            }

            if (cards.Select(card => card.Id).Distinct(StringComparer.Ordinal).Count() != cards.Count)
            {
                throw new ArgumentException("A card instance cannot be selected more than once.", nameof(cards));
            }
        }
    }
}
