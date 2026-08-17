using NUnit.Framework;
using PersonaCards.Battle;
using PersonaCards.Cards;

namespace PersonaCards.Tests.PlayMode
{
    public sealed class BattleCompletionPlayModeTests
    {
        [Test]
        public void CanAutomaticallyCompleteWinningBattle()
        {
            var battle = new BattleStateMachine(StandardDeckFactory.Create(), 7u, 1);
            battle.TryToggleSelection(battle.Deck.Hand[0].Id);

            var result = battle.TryPlaySelected();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(battle.Status, Is.EqualTo(BattleStatus.Won));
        }

        [Test]
        public void CanAutomaticallyCompleteLosingBattle()
        {
            var battle = new BattleStateMachine(StandardDeckFactory.Create(), 7u, long.MaxValue);

            while (battle.Status == BattleStatus.PlayerTurn)
            {
                battle.TryToggleSelection(battle.Deck.Hand[0].Id);
                Assert.That(battle.TryPlaySelected().Succeeded, Is.True);
            }

            Assert.That(battle.Status, Is.EqualTo(BattleStatus.Lost));
            Assert.That(battle.PlaysRemaining, Is.Zero);
        }
    }
}
