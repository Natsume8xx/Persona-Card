using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;
using PersonaCards.UI;
using PersonaCards.Core;

namespace PersonaCards.Tests.EditMode
{
    public sealed class JourneyDeckStateTests
    {
        [Test]
        public void RewardEnhancementPersistsIntoBattleDeckCopy()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create());
            var target = journey.Cards.First();

            Assert.That(journey.GrantRewardEnhancement(target.Id), Is.True);
            Assert.That(journey.CreateBattleDeck().Single(card => card.Id == target.Id).Enhancement,
                Is.EqualTo(CardEnhancement.ChipBoost));
        }

        [Test]
        public void PurchaseConsumesCoinsAndMutatesSelectedCardOnly()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create());
            var target = journey.Cards.First();

            Assert.That(journey.TryPurchase(JourneyDeckAction.Enhance, target.Id), Is.True);
            Assert.That(journey.Coins, Is.EqualTo(1));
            Assert.That(journey.Cards.Single(card => card.Id == target.Id).Enhancement,
                Is.EqualTo(CardEnhancement.MultBoost));
            Assert.That(journey.Cards.Count(card => card.Enhancement != CardEnhancement.None), Is.EqualTo(1));
        }

        [Test]
        public void CannotPurchaseWithoutEnoughCoins()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create(), 1);
            var target = journey.Cards.First();

            Assert.That(journey.TryPurchase(JourneyDeckAction.Delete, target.Id), Is.False);
            Assert.That(journey.Coins, Is.EqualTo(1));
            Assert.That(journey.Cards.Count, Is.EqualTo(52));
        }

        [Test]
        public void PersonaSlotCycleSwapsDefinitionsAndPreservesUniqueLoadout()
        {
            var state = new PersonaLoadoutState();

            state.CycleSlot(0);

            Assert.That(state.Slots[0], Is.EqualTo(PersonaCards.Battle.Personas.InitialPersonaCatalog.Executor));
            Assert.That(state.Slots[1], Is.EqualTo(PersonaCards.Battle.Personas.InitialPersonaCatalog.Accumulator));
            Assert.That(state.CreateLoadout().Slots.Count, Is.EqualTo(4));
            Assert.That(state.Slots.Where(definition => definition != null).Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void EquippingOwnedPersonaToAnOccupiedSlotMovesExistingCopyInsteadOfDuplicatingIt()
        {
            var state = new PersonaLoadoutState();

            state.EquipAt(PersonaCards.Battle.Personas.InitialPersonaCatalog.Accumulator, 2);

            Assert.That(state.Slots[2], Is.EqualTo(PersonaCards.Battle.Personas.InitialPersonaCatalog.Accumulator));
            Assert.That(state.Slots[0], Is.EqualTo(PersonaCards.Battle.Personas.InitialPersonaCatalog.Ambitious));
            Assert.That(state.Slots.Count(definition => definition == PersonaCards.Battle.Personas.InitialPersonaCatalog.Accumulator), Is.EqualTo(1));
        }

        [Test]
        public void PersonaSlotCanBeUnequipped()
        {
            var state = new PersonaLoadoutState();

            state.Unequip(1);

            Assert.That(state.Slots[1], Is.Null);
            Assert.That(state.CreateLoadout().Slots.Count, Is.EqualTo(4));
        }

        [Test]
        public void AutomaticEquipNeverOverwritesAFullLoadout()
        {
            var state = new PersonaLoadoutState();
            var forged = new PersonaCards.Battle.Personas.PersonaCardDefinition(
                "persona.test.forged", "测试人格",
                PersonaCards.Battle.Personas.PersonaConditionKind.Always, HandType.HighCard,
                PersonaCards.Battle.Personas.PersonaEffectKind.AddChips, 20m);
            var before = state.Slots.ToArray();

            var result = state.Equip(forged);

            Assert.That(result, Is.EqualTo(3));
            Assert.That(state.Slots[3], Is.EqualTo(forged));
            state.EquipAt(PersonaCards.Battle.Personas.InitialPersonaCatalog.Ambitious, 3);
            var fullLoadout = state.Slots.ToArray();
            var anotherForged = new PersonaCards.Battle.Personas.PersonaCardDefinition(
                "persona.test.another", "另一测试人格",
                PersonaCards.Battle.Personas.PersonaConditionKind.Always, HandType.HighCard,
                PersonaCards.Battle.Personas.PersonaEffectKind.AddChips, 30m);
            Assert.That(state.Equip(anotherForged), Is.EqualTo(-1));
            Assert.That(state.Slots, Is.EqualTo(fullLoadout));
            Assert.That(before[0], Is.EqualTo(state.Slots[0]));
        }

        [Test]
        public void BehaviorReportUsesRecordedActionsAndForgeProducesThreeDifferentCandidates()
        {
            var tracker = new RunBehaviorTracker();
            tracker.RecordPlay(HandType.Pair, 2, 120);
            tracker.RecordPlay(HandType.Pair, 2, 180);
            tracker.RecordDiscard(3);

            var report = tracker.CreateReport();
            var forge = new PersonaForgeState(report, 42u);

            Assert.That(report.Plays, Is.EqualTo(2));
            Assert.That(report.Discards, Is.EqualTo(1));
            Assert.That(report.DominantHand, Is.EqualTo(HandType.Pair));
            Assert.That(forge.Rolls, Has.Count.EqualTo(3));
            Assert.That(forge.Rolls.All(roll => roll >= 1 && roll <= 20), Is.True);
            Assert.That(forge.Candidates.Select(candidate => candidate.EffectKind).Distinct().Count(), Is.EqualTo(3));
        }
    }
}
