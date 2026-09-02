using System.Linq;
using NUnit.Framework;
using PersonaCards.Battle;
using PersonaCards.Battle.Bosses;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;
using PersonaCards.Cards.Scoring;
using PersonaCards.Core;
using PersonaCards.Data;

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

        [Test]
        public void CreateFromPoolPrimaryDrawsMirrorKeeperWithPoolAttachment()
        {
            // P0-3 目录框架：Primary 池现有唯一定义为镜厅守门人，且定义自带池归属
            var boss = BossEncounterCatalog.CreateFromPool(BossPoolId.Primary, 7u);

            Assert.That(boss.Definition.EncounterId, Is.EqualTo(BossEncounterCatalog.MirrorKeeperEncounterId));
            Assert.That(boss.Definition.PoolId, Is.EqualTo(BossPoolId.Primary));
        }

        [Test]
        public void CreateFromPoolRejectsNoneSentinelAndPoolsWithoutDefinitions()
        {
            // None 是普通战哨兵值，不是合法池；Intermediate/Advanced 当前无定义（内容待 B1），引用即配置错误
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                BossEncounterCatalog.CreateFromPool(BossPoolId.None, 7u));
            Assert.Throws<System.InvalidOperationException>(() =>
                BossEncounterCatalog.CreateFromPool(BossPoolId.Intermediate, 7u));
            Assert.Throws<System.InvalidOperationException>(() =>
                BossEncounterCatalog.CreateFromPool(BossPoolId.Advanced, 7u));
        }

        [Test]
        public void CreateFromPoolSkipsUsedEncounters()
        {
            // 不重复机制：唯一候选已用 → 池耗尽抛错；无关 id 的已用集合不影响抽取
            Assert.Throws<System.InvalidOperationException>(() =>
                BossEncounterCatalog.CreateFromPool(BossPoolId.Primary, 7u,
                    new[] { BossEncounterCatalog.MirrorKeeperEncounterId }));

            var boss = BossEncounterCatalog.CreateFromPool(BossPoolId.Primary, 7u, new[] { "boss.some.other" });
            Assert.That(boss.Definition.EncounterId, Is.EqualTo(BossEncounterCatalog.MirrorKeeperEncounterId));
        }

        [Test]
        public void PickEncounterIsSeedDeterministicAndSkipsUsed()
        {
            // P0-3 抽取工具：同种子必同结果（揭示与开战一致性的基石）；剔除已用后从剩余抽取；池不匹配视为空
            var candidates = new[]
            {
                Boss("boss.a", BossPoolId.Primary),
                Boss("boss.b", BossPoolId.Primary),
                Boss("boss.c", BossPoolId.Intermediate)
            };

            foreach (var seed in new[] { 1u, 42u, 12345u })
            {
                var first = BossEncounterCatalog.PickEncounter(candidates, BossPoolId.Primary, seed);
                Assert.That(BossEncounterCatalog.PickEncounter(candidates, BossPoolId.Primary, seed), Is.SameAs(first));
            }

            var skipped = BossEncounterCatalog.PickEncounter(candidates, BossPoolId.Primary, 42u, new[] { "boss.a" });
            Assert.That(skipped.EncounterId, Is.EqualTo("boss.b"));
            Assert.Throws<System.InvalidOperationException>(() =>
                BossEncounterCatalog.PickEncounter(candidates, BossPoolId.Primary, 42u, new[] { "boss.a", "boss.b" }));
            Assert.Throws<System.InvalidOperationException>(() =>
                BossEncounterCatalog.PickEncounter(candidates, BossPoolId.Advanced, 42u));
            Assert.Throws<System.ArgumentNullException>(() =>
                BossEncounterCatalog.PickEncounter(null, BossPoolId.Primary, 1u));
        }

        [Test]
        public void RestoreRejectsUnknownEncounterId()
        {
            // Restore 按遭遇 id 查目录：未知 id 抛错，绝不静默回落（非法配置必须暴露）
            var snapshot = new BossEncounterSnapshot("boss.unknown", 1, HandType.Pair);

            Assert.Throws<System.ArgumentException>(() => BossEncounterCatalog.Restore(snapshot));
        }

        [Test]
        public void DefinitionRejectsNonePoolAttachment()
        {
            // 定义必须归属真实难度池，None 哨兵值为非法归属
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Boss("boss.none", BossPoolId.None));
        }

        private static BattleStateMachine CreateBattle(PlayingCardInstance[] cards, BossEncounterRuntime boss)
        {
            return new BattleStateMachine(cards, 1u, long.MaxValue, EmptyLoadout(), bossEncounter: boss);
        }

        /// <summary>测试用 Boss 定义（id 派生规则/介入 id，内容为占位文本）。</summary>
        private static BossEncounterDefinition Boss(string id, BossPoolId pool)
        {
            return new BossEncounterDefinition(id, id, pool, $"rule.{id}", "规则", "规则描述",
                $"intervention.{id}", "介入", "介入描述");
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
