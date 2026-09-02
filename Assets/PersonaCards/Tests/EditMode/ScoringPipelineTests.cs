using System;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;
using PersonaCards.Cards.Scoring;
using PersonaCards.Core;

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

            // P0-1C 新表：对子基础 48/2，计分牌两张 8 面值 16 → 64m × 2 = 128
            Assert.That(result.Chips, Is.EqualTo(64m));
            Assert.That(result.Multiplier, Is.EqualTo(2m));
            Assert.That(result.FinalScore, Is.EqualTo(128));
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

            // P0-1C 新表：单 A + 杂牌 4 是高牌（基础 55/1），计分牌 A 面值 11 + 倍率强化 3 → 66m × 4 = 264
            Assert.That(result.Chips, Is.EqualTo(66m));
            Assert.That(result.Multiplier, Is.EqualTo(4m));
            Assert.That(result.FinalScore, Is.EqualTo(264));
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

            // P0-1C 新表：对子基础 48，两张 8 面值 16 + 筹码强化 20 → 84m × 2 = 168
            Assert.That(result.Chips, Is.EqualTo(84m));
            Assert.That(result.FinalScore, Is.EqualTo(168));
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
            // P0-1C 新表：高牌基础 55/1，A 面值 11，+3+2 筹码、+2 倍率 → (55+11+5) × 3 × 0.5 = 106.5 → 107
            Assert.That(result.FinalScore, Is.EqualTo(107));
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

            // P0-1C 新表：高牌基础 55/1，2 面值 2 → (55+2) × 0.5 = 28.5 → 四舍五入 29
            Assert.That(rounded.RawScore, Is.EqualTo(28.5m));
            Assert.That(rounded.FinalScore, Is.EqualTo(29));
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
        public void ScoringCardsPhaseEffectsRunAfterCardFacesBeforeHeld()
        {
            // P0-4：自定义效果可挂 ScoringCards 阶段——事件排在卡面硬编码之后、HeldAndGlobal 之前
            var result = _pipeline.Score(
                new[]
                {
                    Card("ace", Suit.Spades, Rank.Ace),
                    Card("junk", Suit.Hearts, Rank.Four)
                },
                new IScoringEffect[]
                {
                    Effect(ScoringPhase.HeldAndGlobal, 0, "global", c => c.AddChips(3m, "global.chips")),
                    Effect(ScoringPhase.ScoringCards, 0, "suit-check", c => c.AddMultiplier(1m, "suit.review"))
                });

            Assert.That(result.Events.Select(e => (int)e.Phase), Is.Ordered);
            var scoringCardSources = result.Events
                .Where(e => e.Phase == ScoringPhase.ScoringCards)
                .Select(e => e.SourceId)
                .ToArray();
            Assert.That(scoringCardSources, Is.EqualTo(new[] { "ace", "suit-check" }));
            // P0-1C 新表：高牌基础 55/1，A 面值 11，+1 倍率（suit-check）、+3 筹码（global）→ 69 × 2 = 138
            Assert.That(result.Chips, Is.EqualTo(69m));
            Assert.That(result.Multiplier, Is.EqualTo(2m));
            Assert.That(result.FinalScore, Is.EqualTo(138));
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
