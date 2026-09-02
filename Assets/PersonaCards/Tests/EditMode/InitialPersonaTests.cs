using System.Linq;
using NUnit.Framework;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Cards.Scoring;
using PersonaCards.Core;

namespace PersonaCards.Tests.EditMode
{
    public sealed class InitialPersonaTests
    {
        private ScoringPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            _pipeline = new ScoringPipeline();
        }

        [Test]
        public void DefaultLoadoutContainsThreeTeachingPersonasAndEmptyFourthSlot()
        {
            var loadout = InitialPersonaCatalog.CreateDefaultLoadout();

            Assert.That(loadout.Slots.Select(slot => slot.Definition?.DisplayName), Is.EqualTo(new[]
            {
                "积累者", "执行者", "野心者", null
            }));
            Assert.That(loadout.CreateScoringEffects().Count, Is.EqualTo(3));
        }

        [Test]
        public void PairTriggersAllThreeInitialPersonasInSlotOrder()
        {
            var result = _pipeline.Score(
                new[]
                {
                    Card("eight-a", Suit.Spades, Rank.Eight),
                    Card("eight-b", Suit.Hearts, Rank.Eight)
                },
                InitialPersonaCatalog.CreateDefaultLoadout().CreateScoringEffects());

            var events = result.Events.Where(scoringEvent => scoringEvent.Phase == ScoringPhase.Persona).ToArray();
            Assert.That(events.Select(scoringEvent => scoringEvent.SourceId), Is.EqualTo(new[]
            {
                "persona.initial.accumulator",
                "persona.initial.executor",
                "persona.initial.ambitious"
            }));
            // P0-1C 新表：对子基础 48/2，牌面 16，人格 +15 筹码 +2 倍率 → 79m × 4 × 1.10 = 347.6 → 348
            Assert.That(result.Chips, Is.EqualTo(79m));
            Assert.That(result.Multiplier, Is.EqualTo(4m));
            Assert.That(result.FinalMultiplier, Is.EqualTo(1.10m));
            Assert.That(result.FinalScore, Is.EqualTo(348));
        }

        [Test]
        public void HighCardSkipsAmbitiousConditionButKeepsItsSlotEvent()
        {
            var result = _pipeline.Score(
                new[] { Card("ace", Suit.Spades, Rank.Ace) },
                InitialPersonaCatalog.CreateDefaultLoadout().CreateScoringEffects());

            var ambitious = result.Events.Single(scoringEvent =>
                scoringEvent.SourceId == "persona.initial.ambitious");
            Assert.That(ambitious.Operation, Is.EqualTo(ScoringOperation.Skip));
            Assert.That(ambitious.DisplayTextKey, Is.EqualTo("persona.condition_not_met"));
            // P0-1C 新表：高牌基础 55/1，A 面值 11，积累者 +15 筹码、执行者 +2 倍率、野心者条件未满足 Skip → 81 × 3 = 243
            Assert.That(result.FinalScore, Is.EqualTo(243));
        }

        [Test]
        public void DisabledSlotIsSkippedWithoutMovingLaterPersonas()
        {
            var original = InitialPersonaCatalog.CreateDefaultLoadout();
            var loadout = new PersonaLoadout(new[]
            {
                original.Slots[0],
                original.Slots[1].WithDisabled(true),
                original.Slots[2],
                original.Slots[3]
            });

            var result = _pipeline.Score(
                new[]
                {
                    Card("queen-a", Suit.Spades, Rank.Queen),
                    Card("queen-b", Suit.Hearts, Rank.Queen)
                },
                loadout.CreateScoringEffects());

            var events = result.Events.Where(scoringEvent => scoringEvent.Phase == ScoringPhase.Persona).ToArray();
            Assert.That(events.Select(scoringEvent => scoringEvent.SourceId), Is.EqualTo(new[]
            {
                "persona.initial.accumulator",
                "persona.initial.executor",
                "persona.initial.ambitious"
            }));
            Assert.That(events[1].Operation, Is.EqualTo(ScoringOperation.Skip));
            Assert.That(events[1].DisplayTextKey, Is.EqualTo("persona.disabled"));
            Assert.That(events[2].Operation, Is.EqualTo(ScoringOperation.MultiplyFinal));
        }

        [Test]
        public void LoadoutRejectsMissingOrDuplicateSlotNumbers()
        {
            Assert.Throws<System.ArgumentException>(() => new PersonaLoadout(new[]
            {
                new PersonaSlot(0, InitialPersonaCatalog.Accumulator),
                new PersonaSlot(1, InitialPersonaCatalog.Executor),
                new PersonaSlot(2, InitialPersonaCatalog.Ambitious)
            }));

            Assert.Throws<System.ArgumentException>(() => new PersonaLoadout(new[]
            {
                new PersonaSlot(0, InitialPersonaCatalog.Accumulator),
                new PersonaSlot(1, InitialPersonaCatalog.Executor),
                new PersonaSlot(1, InitialPersonaCatalog.Ambitious),
                new PersonaSlot(3, null)
            }));
        }

        [Test]
        public void WithDisabledSlotsDerivesLoadoutAndRejectsOutOfRangeIndices()
        {
            // P0-5 封印：槽 1 禁用派生——只影响目标槽，其余槽原样；空槽禁用无害
            var original = InitialPersonaCatalog.CreateDefaultLoadout();
            var derived = original.WithDisabledSlots(new[] { 1 });

            Assert.That(derived.Slots[0].IsDisabled, Is.False);
            Assert.That(derived.Slots[1].IsDisabled, Is.True);
            Assert.That(derived.Slots[2].IsDisabled, Is.False);
            Assert.That(derived.Slots[3].IsDisabled, Is.False);
            Assert.That(derived.Slots[1].Definition.TemplateId, Is.EqualTo("persona.initial.executor"));
            Assert.That(original.Slots[1].IsDisabled, Is.False); // 原 loadout 不变（不可变派生）

            // 空集合 = 无变化；空槽（3）禁用无害
            var untouched = original.WithDisabledSlots(new int[0]);
            Assert.That(untouched.Slots[1].IsDisabled, Is.False);
            var emptySlotDisabled = original.WithDisabledSlots(new[] { 3 });
            Assert.That(emptySlotDisabled.Slots[3].IsDisabled, Is.True);

            // 槽号越界必须拒绝
            Assert.Throws<System.ArgumentOutOfRangeException>(() => original.WithDisabledSlots(new[] { 4 }));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => original.WithDisabledSlots(new[] { -1 }));
        }

        [Test]
        public void DelayedPersonaSkipsUntilEnoughHandsPlayed()
        {
            // P0-5 人格延迟：DelayHands=2 → 已出手数 1 时 Skip，2 时生效
            var delayed = new PersonaCardDefinition("test.delayed", "延迟测试",
                PersonaConditionKind.Always, HandType.HighCard, PersonaEffectKind.AddChips, 10m, delayHands: 2);
            var slot = new PersonaSlot(0, delayed);

            var beforeReady = _pipeline.Score(
                new[] { Card("ace", Suit.Spades, Rank.Ace) },
                new IScoringEffect[] { new PersonaScoringEffect(slot, () => 1) });

            var delayEvent = beforeReady.Events.Single(scoringEvent => scoringEvent.SourceId == "test.delayed");
            Assert.That(delayEvent.Operation, Is.EqualTo(ScoringOperation.Skip));
            Assert.That(delayEvent.DisplayTextKey, Is.EqualTo("persona.delayed"));
            // 高牌基础 55/1 + A 面值 11，延迟未生效 → 66
            Assert.That(beforeReady.FinalScore, Is.EqualTo(66));

            var afterReady = _pipeline.Score(
                new[] { Card("ace", Suit.Spades, Rank.Ace) },
                new IScoringEffect[] { new PersonaScoringEffect(slot, () => 2) });

            Assert.That(afterReady.Events.Any(scoringEvent =>
                scoringEvent.SourceId == "test.delayed" && scoringEvent.Operation == ScoringOperation.AddChips), Is.True);
            // 66 + 10 = 76
            Assert.That(afterReady.FinalScore, Is.EqualTo(76));
        }

        private static PlayingCardInstance Card(string id, Suit suit, Rank rank)
        {
            return new PlayingCardInstance(id, suit, rank);
        }
    }
}
