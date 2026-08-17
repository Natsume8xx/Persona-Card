using System;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;
using PersonaCards.Cards.Scoring;

namespace PersonaCards.Tests.EditMode
{
    // These tests lock the authoritative scoring trace, not presentation behavior.
    public sealed class ScoringPipelineTests
    {
        private ScoringPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            _pipeline = new ScoringPipeline();
        }

        [Test]
        public void PairUsesBaseAndOnlyPairFaceChips()
        {
            var result = _pipeline.Score(new[]
            {
                Card("pair-a", Suit.Spades, Rank.Eight),
                Card("pair-b", Suit.Hearts, Rank.Eight),
                Card("kicker", Suit.Clubs, Rank.Ace)
            });

            Assert.That(result.Chips, Is.EqualTo(26m));
            Assert.That(result.Multiplier, Is.EqualTo(2m));
            Assert.That(result.FinalScore, Is.EqualTo(52));
            Assert.That(result.Events.Count(e => e.DisplayTextKey == "card.face_chips"), Is.EqualTo(2));
        }

        [Test]
        public void EnhancementsTriggerOnlyOnScoringCards()
        {
            var result = _pipeline.Score(new[]
            {
                Card("ace", Suit.Spades, Rank.Ace, CardEnhancement.MultBoost),
                Card("junk", Suit.Hearts, Rank.Four, CardEnhancement.ChipBoost)
            });

            Assert.That(result.Chips, Is.EqualTo(16m));
            Assert.That(result.Multiplier, Is.EqualTo(4m));
            Assert.That(result.FinalScore, Is.EqualTo(64));
            Assert.That(result.Events.Any(e => e.SourceId == "junk"), Is.False);
        }

        [Test]
        public void ChipBoostAddsTwentyChips()
        {
            var result = _pipeline.Score(new[]
            {
                Card("eight-a", Suit.Spades, Rank.Eight, CardEnhancement.ChipBoost),
                Card("eight-b", Suit.Hearts, Rank.Eight)
            });

            Assert.That(result.Chips, Is.EqualTo(46m));
            Assert.That(result.FinalScore, Is.EqualTo(92));
        }

        [Test]
        public void EffectsAndSystemEventsFollowSevenPhaseOrder()
        {
            var result = _pipeline.Score(
                new[] { Card("ace", Suit.Spades, Rank.Ace) },
                new IScoringEffect[]
                {
                    Effect(ScoringPhase.BossFinal, 0, "boss", c => c.MultiplyFinal(0.5m, "boss.half")),
                    Effect(ScoringPhase.Persona, 1, "persona-2", c => c.AddMultiplier(2m, "persona.mult")),
                    Effect(ScoringPhase.HeldAndGlobal, 0, "global", c => c.AddChips(3m, "global.chips")),
                    Effect(ScoringPhase.Persona, 0, "persona-1", c => c.AddChips(2m, "persona.chips"))
                });

            Assert.That(result.Events.Select(e => (int)e.Phase), Is.Ordered);
            Assert.That(
                result.Events.Where(e => e.Phase == ScoringPhase.Persona).Select(e => e.SourceId),
                Is.EqualTo(new[] { "persona-1", "persona-2" }));
            Assert.That(result.FinalScore, Is.EqualTo(32));
        }

        [Test]
        public void CommitRoundsHalfAwayFromZeroAndClampsMinimumToOne()
        {
            var rounded = _pipeline.Score(
                new[] { Card("two", Suit.Spades, Rank.Two) },
                new[] { Effect(ScoringPhase.BossFinal, 0, "boss", c => c.MultiplyFinal(0.5m, "boss.half")) });
            var clamped = _pipeline.Score(
                new[] { Card("ace", Suit.Spades, Rank.Ace) },
                new[] { Effect(ScoringPhase.BossFinal, 0, "boss", c => c.MultiplyFinal(0m, "boss.zero")) });

            Assert.That(rounded.RawScore, Is.EqualTo(3.5m));
            Assert.That(rounded.FinalScore, Is.EqualTo(4));
            Assert.That(clamped.FinalScore, Is.EqualTo(1));
        }

        [Test]
        public void SameInputProducesIdenticalEventTrace()
        {
            var cards = new[]
            {
                Card("queen-a", Suit.Spades, Rank.Queen),
                Card("queen-b", Suit.Hearts, Rank.Queen, CardEnhancement.MultBoost)
            };

            var first = _pipeline.Score(cards);
            var second = _pipeline.Score(cards);

            Assert.That(second.FinalScore, Is.EqualTo(first.FinalScore));
            Assert.That(
                second.Events.Select(EventSignature),
                Is.EqualTo(first.Events.Select(EventSignature)));
        }

        [Test]
        public void RejectsEffectsInPipelineReservedPhases()
        {
            var effect = Effect(ScoringPhase.ScoreCommit, 0, "invalid", c => c.AddChips(1m, "invalid"));

            Assert.Throws<ArgumentException>(() =>
                _pipeline.Score(new[] { Card("ace", Suit.Spades, Rank.Ace) }, new[] { effect }));
        }

        [Test]
        public void WildSuitCompletesFlushWithoutChangingPrintedSuit()
        {
            var wild = Card("wild", Suit.Spades, Rank.Seven, CardEnhancement.WildSuit);
            var cards = new[]
            {
                Card("h2", Suit.Hearts, Rank.Two),
                Card("h4", Suit.Hearts, Rank.Four),
                wild,
                Card("h9", Suit.Hearts, Rank.Nine),
                Card("hk", Suit.Hearts, Rank.King)
            };

            var evaluation = new HandEvaluator().Evaluate(cards);

            Assert.That(evaluation.HandType, Is.EqualTo(HandType.Flush));
            Assert.That(wild.Suit, Is.EqualTo(Suit.Spades));
        }

        private static string EventSignature(ScoringEvent scoringEvent)
        {
            return $"{scoringEvent.Phase}|{scoringEvent.SourceType}|{scoringEvent.SourceId}|" +
                   $"{scoringEvent.Operation}|{scoringEvent.Value}|{scoringEvent.Before}|{scoringEvent.After}";
        }

        private static PlayingCardInstance Card(
            string id,
            Suit suit,
            Rank rank,
            CardEnhancement enhancement = CardEnhancement.None)
        {
            return new PlayingCardInstance(id, suit, rank, enhancement);
        }

        private static IScoringEffect Effect(
            ScoringPhase phase,
            int order,
            string id,
            Action<ScoringContext> apply)
        {
            var sourceType = phase == ScoringPhase.Persona
                ? ScoringSourceType.Persona
                : phase == ScoringPhase.BossFinal
                    ? ScoringSourceType.Boss
                    : ScoringSourceType.HeldOrGlobal;
            return new TestEffect(phase, order, sourceType, id, apply);
        }

        private sealed class TestEffect : IScoringEffect
        {
            private readonly Action<ScoringContext> _apply;

            public TestEffect(
                ScoringPhase phase,
                int order,
                ScoringSourceType sourceType,
                string sourceId,
                Action<ScoringContext> apply)
            {
                Phase = phase;
                Order = order;
                SourceType = sourceType;
                SourceId = sourceId;
                _apply = apply;
            }

            public ScoringPhase Phase { get; }
            public int Order { get; }
            public ScoringSourceType SourceType { get; }
            public string SourceId { get; }

            public void Apply(ScoringContext context, HandEvaluationResult evaluation)
            {
                _apply(context);
            }
        }
    }
}
