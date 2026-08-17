using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;

namespace PersonaCards.Tests.EditMode
{
    public sealed class HandEvaluatorTests
    {
        private HandEvaluator _evaluator;

        [SetUp]
        public void SetUp()
        {
            _evaluator = new HandEvaluator();
        }

        [Test]
        public void RecognizesAllTwelveSupportedHandTypes()
        {
            AssertType(HandType.HighCard,
                Card(Suit.Spades, Rank.Ace), Card(Suit.Hearts, Rank.Nine), Card(Suit.Clubs, Rank.Four));
            AssertType(HandType.Pair,
                Card(Suit.Spades, Rank.Ace), Card(Suit.Hearts, Rank.Ace), Card(Suit.Clubs, Rank.Four));
            AssertType(HandType.TwoPair,
                Card(Suit.Spades, Rank.Ace), Card(Suit.Hearts, Rank.Ace),
                Card(Suit.Clubs, Rank.Four), Card(Suit.Diamonds, Rank.Four), Card(Suit.Spades, Rank.Nine));
            AssertType(HandType.ThreeOfAKind,
                Card(Suit.Spades, Rank.Ace), Card(Suit.Hearts, Rank.Ace), Card(Suit.Clubs, Rank.Ace),
                Card(Suit.Diamonds, Rank.Four), Card(Suit.Spades, Rank.Nine));
            AssertType(HandType.Straight,
                Card(Suit.Spades, Rank.Five), Card(Suit.Hearts, Rank.Six), Card(Suit.Clubs, Rank.Seven),
                Card(Suit.Diamonds, Rank.Eight), Card(Suit.Spades, Rank.Nine));
            AssertType(HandType.Flush,
                Card(Suit.Hearts, Rank.Two), Card(Suit.Hearts, Rank.Five), Card(Suit.Hearts, Rank.Seven),
                Card(Suit.Hearts, Rank.Jack), Card(Suit.Hearts, Rank.Ace));
            AssertType(HandType.FullHouse,
                Card(Suit.Spades, Rank.Queen), Card(Suit.Hearts, Rank.Queen), Card(Suit.Clubs, Rank.Queen),
                Card(Suit.Spades, Rank.Three), Card(Suit.Hearts, Rank.Three));
            AssertType(HandType.FourOfAKind,
                Card(Suit.Spades, Rank.King), Card(Suit.Hearts, Rank.King), Card(Suit.Clubs, Rank.King),
                Card(Suit.Diamonds, Rank.King), Card(Suit.Hearts, Rank.Three));
            AssertType(HandType.StraightFlush,
                Card(Suit.Spades, Rank.Ten), Card(Suit.Spades, Rank.Jack), Card(Suit.Spades, Rank.Queen),
                Card(Suit.Spades, Rank.King), Card(Suit.Spades, Rank.Ace));
            AssertType(HandType.FiveOfAKind,
                Card(Suit.Spades, Rank.Ten), Card(Suit.Hearts, Rank.Ten), Card(Suit.Clubs, Rank.Ten),
                Card(Suit.Diamonds, Rank.Ten), Card(Suit.Spades, Rank.Ten));
            AssertType(HandType.FlushHouse,
                Card(Suit.Hearts, Rank.Queen), Card(Suit.Hearts, Rank.Queen), Card(Suit.Hearts, Rank.Queen),
                Card(Suit.Hearts, Rank.Three), Card(Suit.Hearts, Rank.Three));
            AssertType(HandType.FlushFive,
                Card(Suit.Diamonds, Rank.Seven), Card(Suit.Diamonds, Rank.Seven),
                Card(Suit.Diamonds, Rank.Seven), Card(Suit.Diamonds, Rank.Seven),
                Card(Suit.Diamonds, Rank.Seven));
        }

        [Test]
        public void RecognizesAceLowAndAceHighStraights()
        {
            var aceLow = Evaluate(
                Card(Suit.Spades, Rank.Ace), Card(Suit.Hearts, Rank.Two), Card(Suit.Clubs, Rank.Three),
                Card(Suit.Diamonds, Rank.Four), Card(Suit.Spades, Rank.Five));
            var aceHigh = Evaluate(
                Card(Suit.Spades, Rank.Ten), Card(Suit.Hearts, Rank.Jack), Card(Suit.Clubs, Rank.Queen),
                Card(Suit.Diamonds, Rank.King), Card(Suit.Spades, Rank.Ace));

            Assert.That(aceLow.HandType, Is.EqualTo(HandType.Straight));
            Assert.That(aceHigh.HandType, Is.EqualTo(HandType.Straight));
        }

        [Test]
        public void DoesNotAllowCircularStraight()
        {
            var result = Evaluate(
                Card(Suit.Spades, Rank.Queen), Card(Suit.Hearts, Rank.King), Card(Suit.Clubs, Rank.Ace),
                Card(Suit.Diamonds, Rank.Two), Card(Suit.Spades, Rank.Three));

            Assert.That(result.HandType, Is.EqualTo(HandType.HighCard));
        }

        [Test]
        public void PairScoresOnlyPairAndExcludesKickers()
        {
            var pairOne = Card(Suit.Spades, Rank.Eight);
            var pairTwo = Card(Suit.Hearts, Rank.Eight);
            var result = Evaluate(
                pairOne,
                pairTwo,
                Card(Suit.Clubs, Rank.Ace),
                Card(Suit.Diamonds, Rank.Four),
                Card(Suit.Spades, Rank.Two));

            Assert.That(result.ScoringCardIds, Is.EquivalentTo(new[] { pairOne.Id, pairTwo.Id }));
        }

        [Test]
        public void TwoPairScoresFourCardsAndExcludesKicker()
        {
            var cards = new[]
            {
                Card(Suit.Spades, Rank.Eight),
                Card(Suit.Hearts, Rank.Eight),
                Card(Suit.Clubs, Rank.Four),
                Card(Suit.Diamonds, Rank.Four),
                Card(Suit.Spades, Rank.Ace)
            };

            var result = Evaluate(cards);

            Assert.That(result.ScoringCardIds.Count, Is.EqualTo(4));
            Assert.That(result.ScoringCardIds, Does.Not.Contain(cards[4].Id));
        }

        [Test]
        public void HighCardScoresOnlyHighestRank()
        {
            var ace = Card(Suit.Spades, Rank.Ace);
            var result = Evaluate(
                Card(Suit.Hearts, Rank.Two),
                ace,
                Card(Suit.Clubs, Rank.Nine));

            Assert.That(result.ScoringCardIds, Is.EqualTo(new[] { ace.Id }));
        }

        [Test]
        public void FiveCardMadeHandsScoreAllFiveCards()
        {
            var cards = new[]
            {
                Card(Suit.Clubs, Rank.Two), Card(Suit.Clubs, Rank.Three), Card(Suit.Clubs, Rank.Four),
                Card(Suit.Clubs, Rank.Five), Card(Suit.Clubs, Rank.Six)
            };

            var result = Evaluate(cards);

            Assert.That(result.HandType, Is.EqualTo(HandType.StraightFlush));
            Assert.That(result.ScoringCardIds, Is.EqualTo(cards.Select(card => card.Id)));
        }

        [Test]
        public void CatalogMatchesFrozenGddValues()
        {
            AssertValues(HandType.HighCard, 5, 1);
            AssertValues(HandType.Pair, 10, 2);
            AssertValues(HandType.TwoPair, 20, 2);
            AssertValues(HandType.ThreeOfAKind, 30, 3);
            AssertValues(HandType.Straight, 30, 4);
            AssertValues(HandType.Flush, 35, 4);
            AssertValues(HandType.FullHouse, 40, 4);
            AssertValues(HandType.FourOfAKind, 60, 7);
            AssertValues(HandType.StraightFlush, 100, 8);
            AssertValues(HandType.FiveOfAKind, 100, 8);
            AssertValues(HandType.FlushHouse, 100, 8);
            AssertValues(HandType.FlushFive, 100, 8);
        }

        [Test]
        public void RejectsSelectionsOutsideOneToFiveCards()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _evaluator.Evaluate(Array.Empty<PlayingCardInstance>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => _evaluator.Evaluate(new[]
            {
                Card(Suit.Spades, Rank.Two), Card(Suit.Hearts, Rank.Three),
                Card(Suit.Clubs, Rank.Four), Card(Suit.Diamonds, Rank.Five),
                Card(Suit.Spades, Rank.Six), Card(Suit.Hearts, Rank.Seven)
            }));
        }

        [Test]
        public void RejectsSelectingSameInstanceTwice()
        {
            var card = Card(Suit.Spades, Rank.Ace);

            Assert.Throws<ArgumentException>(() => _evaluator.Evaluate(new[] { card, card }));
        }

        private void AssertType(HandType expected, params PlayingCardInstance[] cards)
        {
            Assert.That(Evaluate(cards).HandType, Is.EqualTo(expected), expected.ToString());
        }

        private static void AssertValues(HandType handType, int chips, int multiplier)
        {
            var definition = HandTypeCatalog.Get(handType);
            Assert.That(definition.BaseChips, Is.EqualTo(chips), $"{handType} chips");
            Assert.That(definition.BaseMultiplier, Is.EqualTo(multiplier), $"{handType} multiplier");
        }

        private HandEvaluationResult Evaluate(params PlayingCardInstance[] cards)
        {
            return _evaluator.Evaluate(cards);
        }

        private static PlayingCardInstance Card(Suit suit, Rank rank)
        {
            return new PlayingCardInstance(Guid.NewGuid().ToString("N"), suit, rank);
        }
    }
}
