using System.Linq;
using NUnit.Framework;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Cards.Scoring;

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
            Assert.That(result.Chips, Is.EqualTo(41m));
            Assert.That(result.Multiplier, Is.EqualTo(4m));
            Assert.That(result.FinalMultiplier, Is.EqualTo(1.10m));
            Assert.That(result.FinalScore, Is.EqualTo(180));
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
            Assert.That(result.FinalScore, Is.EqualTo(93));
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

        private static PlayingCardInstance Card(string id, Suit suit, Rank rank)
        {
            return new PlayingCardInstance(id, suit, rank);
        }
    }
}
