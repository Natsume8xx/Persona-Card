using System.Linq;
using NUnit.Framework;
using PersonaCards.Battle;
using PersonaCards.Cards;

namespace PersonaCards.Tests.EditMode
{
    public sealed class BattleStateMachineTests
    {
        [Test]
        public void StartsWithEightCardsAndFrozenResources()
        {
            var battle = CreateBattle(350);

            Assert.That(battle.Deck.Hand.Count, Is.EqualTo(8));
            Assert.That(battle.PlaysRemaining, Is.EqualTo(4));
            Assert.That(battle.DiscardsRemaining, Is.EqualTo(3));
            Assert.That(battle.Status, Is.EqualTo(BattleStatus.PlayerTurn));
        }

        [Test]
        public void RejectsSixthSelectedCardWithExplicitReason()
        {
            var battle = CreateBattle(350);
            foreach (var card in battle.Deck.Hand.Take(5))
            {
                Assert.That(battle.TryToggleSelection(card.Id).Succeeded, Is.True);
            }

            var result = battle.TryToggleSelection(battle.Deck.Hand[5].Id);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(BattleCommandFailure.SelectionLimitReached));
        }

        [Test]
        public void PlayScoresMovesCardsAndRefillsHand()
        {
            var battle = CreateBattle(long.MaxValue);
            var selectedIds = battle.Deck.Hand.Take(3).Select(card => card.Id).ToArray();
            foreach (var id in selectedIds)
            {
                battle.TryToggleSelection(id);
            }

            var result = battle.TryPlaySelected();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ScoringResult, Is.Not.Null);
            Assert.That(battle.TotalScore, Is.EqualTo(result.ScoringResult.FinalScore));
            Assert.That(battle.Deck.Played.Select(card => card.Id), Is.EquivalentTo(selectedIds));
            Assert.That(battle.Deck.Hand.Count, Is.EqualTo(8));
            Assert.That(battle.PlaysRemaining, Is.EqualTo(3));
        }

        [Test]
        public void DiscardConsumesResourceWithoutScoringAndRefills()
        {
            var battle = CreateBattle(350);
            var cardId = battle.Deck.Hand[0].Id;
            battle.TryToggleSelection(cardId);

            var result = battle.TryDiscardSelected();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(battle.TotalScore, Is.Zero);
            Assert.That(battle.DiscardsRemaining, Is.EqualTo(2));
            Assert.That(battle.Deck.Discarded.Single().Id, Is.EqualTo(cardId));
            Assert.That(battle.Deck.Hand.Count, Is.EqualTo(8));
        }

        [Test]
        public void EmptySelectionIsRejected()
        {
            var result = CreateBattle(350).TryPlaySelected();

            Assert.That(result.Failure, Is.EqualTo(BattleCommandFailure.NoCardsSelected));
        }

        [Test]
        public void PresentationLockRejectsAllPlayerCommands()
        {
            var battle = CreateBattle(350);
            battle.SetPresentationLock(true);

            var select = battle.TryToggleSelection(battle.Deck.Hand[0].Id);
            var play = battle.TryPlaySelected();
            var discard = battle.TryDiscardSelected();

            Assert.That(select.Failure, Is.EqualTo(BattleCommandFailure.PresentationInProgress));
            Assert.That(play.Failure, Is.EqualTo(BattleCommandFailure.PresentationInProgress));
            Assert.That(discard.Failure, Is.EqualTo(BattleCommandFailure.PresentationInProgress));
        }

        [Test]
        public void SnapshotRestorePreservesEveryZoneResourcesScoreAndSelection()
        {
            var original = CreateBattle(long.MaxValue);
            original.TryToggleSelection(original.Deck.Hand[0].Id);
            original.TryDiscardSelected();
            original.TryToggleSelection(original.Deck.Hand[0].Id);
            original.TryToggleSelection(original.Deck.Hand[1].Id);
            original.TryPlaySelected();
            original.TryToggleSelection(original.Deck.Hand[2].Id);
            var snapshot = new BattleStateSnapshot(original.Deck.DrawPile, original.Deck.Hand,
                original.Deck.Played, original.Deck.Discarded, original.SelectedCardIds,
                original.TargetScore, original.TotalScore, original.PlaysRemaining,
                original.DiscardsRemaining, original.Status);

            var restored = new BattleStateMachine(snapshot);

            Assert.That(restored.Deck.DrawPile.Select(card => card.Id), Is.EqualTo(original.Deck.DrawPile.Select(card => card.Id)));
            Assert.That(restored.Deck.Hand.Select(card => card.Id), Is.EqualTo(original.Deck.Hand.Select(card => card.Id)));
            Assert.That(restored.Deck.Played.Select(card => card.Id), Is.EqualTo(original.Deck.Played.Select(card => card.Id)));
            Assert.That(restored.Deck.Discarded.Select(card => card.Id), Is.EqualTo(original.Deck.Discarded.Select(card => card.Id)));
            Assert.That(restored.SelectedCardIds, Is.EquivalentTo(original.SelectedCardIds));
            Assert.That(restored.TotalScore, Is.EqualTo(original.TotalScore));
            Assert.That(restored.PlaysRemaining, Is.EqualTo(original.PlaysRemaining));
            Assert.That(restored.DiscardsRemaining, Is.EqualTo(original.DiscardsRemaining));
            Assert.That(restored.Status, Is.EqualTo(original.Status));
        }

        [Test]
        public void RestoredDrawPileContinuesWithTheSameNextCard()
        {
            var original = CreateBattle(long.MaxValue);
            var snapshot = new BattleStateSnapshot(original.Deck.DrawPile, original.Deck.Hand,
                original.Deck.Played, original.Deck.Discarded, original.SelectedCardIds,
                original.TargetScore, original.TotalScore, original.PlaysRemaining,
                original.DiscardsRemaining, original.Status);
            var restored = new BattleStateMachine(snapshot);
            original.TryToggleSelection(original.Deck.Hand[0].Id);
            restored.TryToggleSelection(restored.Deck.Hand[0].Id);

            original.TryDiscardSelected();
            restored.TryDiscardSelected();

            Assert.That(restored.Deck.Hand.Select(card => card.Id), Is.EqualTo(original.Deck.Hand.Select(card => card.Id)));
            Assert.That(restored.Deck.DrawPile.Select(card => card.Id), Is.EqualTo(original.Deck.DrawPile.Select(card => card.Id)));
        }

        [Test]
        public void CustomLimitsAreHonored()
        {
            var battle = new BattleStateMachine(StandardDeckFactory.Create(), 12345u, 350, playsLimit: 5, discardsLimit: 2);

            Assert.That(battle.PlaysLimit, Is.EqualTo(5));
            Assert.That(battle.DiscardsLimit, Is.EqualTo(2));
            Assert.That(battle.PlaysRemaining, Is.EqualTo(5));
            Assert.That(battle.DiscardsRemaining, Is.EqualTo(2));
        }

        [Test]
        public void LimitsBelowOneAreRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new BattleStateMachine(StandardDeckFactory.Create(), 12345u, 350, playsLimit: 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new BattleStateMachine(StandardDeckFactory.Create(), 12345u, 350, discardsLimit: -1));
        }

        [Test]
        public void SnapshotRestoreRejectsPlaysAboveSnapshotLimit()
        {
            // 快照记录了本场上限 2，但剩余出牌写成 3：恢复构造器必须按快照自身 limit 校验并拒绝
            var original = CreateBattle(long.MaxValue);
            var snapshot = new BattleStateSnapshot(original.Deck.DrawPile, original.Deck.Hand,
                original.Deck.Played, original.Deck.Discarded, original.SelectedCardIds,
                original.TargetScore, original.TotalScore, playsRemaining: 3,
                discardsRemaining: 1, status: original.Status, playsLimit: 2, discardsLimit: 2);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => new BattleStateMachine(snapshot));
        }

        [Test]
        public void SnapshotWithoutLimitsDefaultsToConstants()
        {
            // 旧式快照（未记录 limit 字段）回落默认 4/3，并保持原有行为
            var original = CreateBattle(long.MaxValue);
            var snapshot = new BattleStateSnapshot(original.Deck.DrawPile, original.Deck.Hand,
                original.Deck.Played, original.Deck.Discarded, original.SelectedCardIds,
                original.TargetScore, original.TotalScore, original.PlaysRemaining,
                original.DiscardsRemaining, original.Status);

            var restored = new BattleStateMachine(snapshot);

            Assert.That(restored.PlaysLimit, Is.EqualTo(BattleStateMachine.StartingPlays));
            Assert.That(restored.DiscardsLimit, Is.EqualTo(BattleStateMachine.StartingDiscards));
        }

        [Test]
        public void SnapshotWithCustomLimitsRestoresThem()
        {
            // 自定义上限的存档恢复后，上限与剩余值都来自快照
            var original = new BattleStateMachine(StandardDeckFactory.Create(), 12345u, long.MaxValue,
                playsLimit: 6, discardsLimit: 1);
            original.TryToggleSelection(original.Deck.Hand[0].Id);
            original.TryDiscardSelected();
            var snapshot = new BattleStateSnapshot(original.Deck.DrawPile, original.Deck.Hand,
                original.Deck.Played, original.Deck.Discarded, original.SelectedCardIds,
                original.TargetScore, original.TotalScore, original.PlaysRemaining,
                original.DiscardsRemaining, original.Status, playsLimit: original.PlaysLimit,
                discardsLimit: original.DiscardsLimit);

            var restored = new BattleStateMachine(snapshot);

            Assert.That(restored.PlaysLimit, Is.EqualTo(6));
            Assert.That(restored.DiscardsLimit, Is.EqualTo(1));
            Assert.That(restored.PlaysRemaining, Is.EqualTo(6));
            Assert.That(restored.DiscardsRemaining, Is.EqualTo(0));
        }

        private static BattleStateMachine CreateBattle(long target)
        {
            return new BattleStateMachine(StandardDeckFactory.Create(), 12345u, target);
        }
    }
}
