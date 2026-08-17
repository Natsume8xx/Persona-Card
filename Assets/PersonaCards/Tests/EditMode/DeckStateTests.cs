using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.Core.Random;

namespace PersonaCards.Tests.EditMode
{
    public sealed class DeckStateTests
    {
        [Test]
        public void StandardDeckContainsExactlyFiftyTwoUniqueCards()
        {
            var cards = StandardDeckFactory.Create();

            Assert.That(cards.Count, Is.EqualTo(52));
            Assert.That(cards.Select(card => card.Id).Distinct().Count(), Is.EqualTo(52));
            Assert.That(cards.GroupBy(card => card.Suit).All(group => group.Count() == 13), Is.True);
            Assert.That(cards.GroupBy(card => card.Rank).All(group => group.Count() == 4), Is.True);
        }

        [Test]
        public void SameSeedProducesSameShuffleOrder()
        {
            var first = new DeckState(StandardDeckFactory.Create());
            var second = new DeckState(StandardDeckFactory.Create());

            first.ShuffleDrawPile(new XorShift32Rng(20260811u));
            second.ShuffleDrawPile(new XorShift32Rng(20260811u));

            Assert.That(
                first.DrawPile.Select(card => card.Id),
                Is.EqualTo(second.DrawPile.Select(card => card.Id)));
        }

        [Test]
        public void DifferentSeedsProduceDifferentShuffleOrders()
        {
            var first = new DeckState(StandardDeckFactory.Create());
            var second = new DeckState(StandardDeckFactory.Create());

            first.ShuffleDrawPile(new XorShift32Rng(1u));
            second.ShuffleDrawPile(new XorShift32Rng(2u));

            Assert.That(
                first.DrawPile.Select(card => card.Id),
                Is.Not.EqualTo(second.DrawPile.Select(card => card.Id)));
        }

        [Test]
        public void DrawAndZoneMovesPreserveAllCardsExactlyOnce()
        {
            var deck = new DeckState(StandardDeckFactory.Create());
            deck.ShuffleDrawPile(new XorShift32Rng(42u));

            var openingHand = deck.Draw(8);
            deck.MoveCards(openingHand.Take(5).Select(card => card.Id), CardZone.Hand, CardZone.Played);
            deck.MoveCards(openingHand.Skip(5).Select(card => card.Id), CardZone.Hand, CardZone.Discarded);
            deck.Draw(8);

            deck.ValidateCardConservation();
            Assert.That(deck.TotalCardCount, Is.EqualTo(52));
            Assert.That(deck.DrawPile.Count, Is.EqualTo(36));
            Assert.That(deck.Hand.Count, Is.EqualTo(8));
            Assert.That(deck.Played.Count, Is.EqualTo(5));
            Assert.That(deck.Discarded.Count, Is.EqualTo(3));
        }

        [Test]
        public void FailedMoveDoesNotPartiallyMutateZones()
        {
            var deck = new DeckState(StandardDeckFactory.Create());
            var openingHand = deck.Draw(2);
            var originalFirstId = openingHand[0].Id;

            Assert.Throws<System.InvalidOperationException>(() =>
                deck.MoveCards(
                    new[] { originalFirstId, "missing-card" },
                    CardZone.Hand,
                    CardZone.Played));

            Assert.That(deck.Hand.Select(card => card.Id), Does.Contain(originalFirstId));
            Assert.That(deck.Played, Is.Empty);
            Assert.That(deck.TotalCardCount, Is.EqualTo(52));
        }

        [Test]
        public void DrawingPastTheRemainingPileReturnsOnlyAvailableCards()
        {
            var deck = new DeckState(StandardDeckFactory.Create());

            var drawn = deck.Draw(100);

            Assert.That(drawn.Count, Is.EqualTo(52));
            Assert.That(deck.DrawPile, Is.Empty);
            Assert.That(deck.Hand.Count, Is.EqualTo(52));
            Assert.That(deck.TotalCardCount, Is.EqualTo(52));
        }
    }
}
