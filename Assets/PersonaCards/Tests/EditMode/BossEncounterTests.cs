using System.Linq;
using NUnit.Framework;
using PersonaCards.Battle;
using PersonaCards.Battle.Bosses;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;
using PersonaCards.Cards.Scoring;
using PersonaCards.Core;

namespace PersonaCards.Tests.EditMode
{
    public sealed class BossEncounterTests
    {
        [Test]
        public void FirstHandEncouragementAddsThirtyChipsInSharedPreviewAndResolution()
        {
            var boss = BossEncounterCatalog.CreateMirrorKeeper();
            var cards = PairCards();
            var battle = CreateBattle(cards, boss);
            SelectAll(battle);

            var preview = battle.PreviewSelected();
            var result = battle.TryPlaySelected();

            // P0-1C 新表：对子基础 48 + 牌面 16 + 首手鼓励 30 = 94
            Assert.That(preview.Chips, Is.EqualTo(94m));
            Assert.That(result.ScoringResult.FinalScore, Is.EqualTo(preview.FinalScore));
            Assert.That(result.ScoringResult.Events.Any(e =>
                e.SourceId == BossEncounterCatalog.FirstHandEncouragementId &&
                e.Operation == ScoringOperation.AddChips), Is.True);
            Assert.That(boss.HandsPlayed, Is.EqualTo(1));
            Assert.That(boss.PreviousHandType, Is.EqualTo(HandType.Pair));
        }

        [Test]
        public void RepeatedJudgmentMultipliesOnlyConsecutiveMatchingHandType()
        {
            var boss = BossEncounterCatalog.CreateMirrorKeeper();
            boss.CommitHand(HandType.Pair);
            var pipeline = new ScoringPipeline();

            var repeated = pipeline.Score(PairCards(), boss.CreateScoringEffects());
            var changed = pipeline.Score(new[] { Card("high", Rank.Ace) }, boss.CreateScoringEffects());

            Assert.That(repeated.FinalMultiplier, Is.EqualTo(0.60m));
            Assert.That(repeated.Events.Any(e => e.SourceId == BossEncounterCatalog.RepeatedJudgmentRuleId &&
                e.Operation == ScoringOperation.MultiplyFinal), Is.True);
            Assert.That(changed.FinalMultiplier, Is.EqualTo(1m));
            Assert.That(changed.Events.Any(e => e.SourceId == BossEncounterCatalog.RepeatedJudgmentRuleId &&
                e.Operation == ScoringOperation.Skip), Is.True);
        }

        [Test]
        public void BossSnapshotRestoresHandHistoryAndDoesNotRepeatFirstHandIntervention()
        {
            var cards = PairCards();
            var original = CreateBattle(cards, BossEncounterCatalog.CreateMirrorKeeper());
            SelectAll(original);
            original.TryPlaySelected();
            var bossSnapshot = original.BossEncounter.CreateSnapshot();
            var snapshot = new BattleStateSnapshot(original.Deck.DrawPile, original.Deck.Hand,
                original.Deck.Played, original.Deck.Discarded, original.SelectedCardIds,
                original.TargetScore, original.TotalScore, original.PlaysRemaining,
                original.DiscardsRemaining, original.Status, bossSnapshot);

            var restored = new BattleStateMachine(snapshot, EmptyLoadout());

            Assert.That(restored.BossEncounter.HandsPlayed, Is.EqualTo(1));
            Assert.That(restored.BossEncounter.PreviousHandType, Is.EqualTo(HandType.Pair));
            var effects = restored.BossEncounter.CreateScoringEffects();
            var result = new ScoringPipeline().Score(PairCards(), effects);
            Assert.That(result.Events.Single(e => e.SourceId == BossEncounterCatalog.FirstHandEncouragementId).Operation,
                Is.EqualTo(ScoringOperation.Skip));
            Assert.That(result.FinalMultiplier, Is.EqualTo(0.60m));
        }

        private static BattleStateMachine CreateBattle(PlayingCardInstance[] cards, BossEncounterRuntime boss)
        {
            return new BattleStateMachine(cards, 1u, long.MaxValue, EmptyLoadout(), bossEncounter: boss);
        }

        private static PersonaLoadout EmptyLoadout()
        {
            return new PersonaLoadout(Enumerable.Range(0, PersonaLoadout.SlotCount)
                .Select(index => new PersonaSlot(index, null)));
        }

        private static PlayingCardInstance[] PairCards()
        {
            return new[] { Card("pair-a", Rank.Eight, Suit.Spades), Card("pair-b", Rank.Eight, Suit.Hearts) };
        }

        private static PlayingCardInstance Card(string id, Rank rank, Suit suit = Suit.Spades)
        {
            return new PlayingCardInstance(id, suit, rank);
        }

        private static void SelectAll(BattleStateMachine battle)
        {
            foreach (var card in battle.Deck.Hand) Assert.That(battle.TryToggleSelection(card.Id).Succeeded, Is.True);
        }
    }
}
