using System.Linq;
using NUnit.Framework;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Cards;
using PersonaCards.Cards.Scoring;
using PersonaCards.Core;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>牌型强化计分效果测试（P0-11）：走完整计分管线，锁定「差值增量」口径（表内绝对底值 − Lv0 目录值）。</summary>
    public sealed class HandEnhancementEffectTests
    {
        private ScoringPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            _pipeline = new ScoringPipeline();
            EnhancementConfig.Configure(HandTables());
        }

        [TearDown]
        public void TearDown()
        {
            EnhancementConfig.Configure(EnhancementTables.Empty); // 静态门面复位，防跨测试泄漏
        }

        [Test]
        public void RoyalFlushLevel1AddsDeltaToChipsAndMultiplier()
        {
            var state = new EnhancementState();
            state.SetHandLevel(HandType.RoyalFlush, 1);

            var result = _pipeline.Score(RoyalCards(), new IScoringEffect[] { new HandEnhancementEffect(state) });

            // 皇家 Lv1 表值 110/13.2，Lv0 目录 100/12 → 差值 +10 筹码 +1.2 倍率
            // 基础 100 + 面值 51 + 10 = 161 × 13.2 = 2125.2 → 四舍五入 2125
            Assert.That(result.Chips, Is.EqualTo(161m));
            Assert.That(result.Multiplier, Is.EqualTo(13.2m));
            Assert.That(result.FinalScore, Is.EqualTo(2125));
            Assert.That(result.Events.Any(e => e.DisplayTextKey == "enhancement.hand_chips"), Is.True);
            Assert.That(result.Events.Any(e => e.DisplayTextKey == "enhancement.hand_multiplier"), Is.True);
        }

        [Test]
        public void PairLevel2AddsDelta()
        {
            var state = new EnhancementState();
            state.SetHandLevel(HandType.Pair, 2);

            var result = _pipeline.Score(
                new[]
                {
                    Card("eight-a", Suit.Spades, Rank.Eight),
                    Card("eight-b", Suit.Hearts, Rank.Eight),
                    Card("kicker", Suit.Clubs, Rank.Ace)
                },
                new IScoringEffect[] { new HandEnhancementEffect(state) });

            // 对子 Lv2 差值 +20 筹码 +0.4 倍率 → (48 + 16 + 20) × 2.4 = 201.6 → 202
            Assert.That(result.Chips, Is.EqualTo(84m));
            Assert.That(result.Multiplier, Is.EqualTo(2.4m));
            Assert.That(result.FinalScore, Is.EqualTo(202));
        }

        [Test]
        public void UpgradedOtherHandTypeHasNoEffect()
        {
            var state = new EnhancementState();
            state.SetHandLevel(HandType.Pair, 2); // 升的是对子，打的是皇家

            var result = _pipeline.Score(RoyalCards(), new IScoringEffect[] { new HandEnhancementEffect(state) });

            // 未升级牌型零增量 = 旧值 (100 + 51) × 12 = 1812
            Assert.That(result.FinalScore, Is.EqualTo(1812));
            Assert.That(result.Events.Any(e => e.DisplayTextKey == "enhancement.hand_chips"), Is.False);
            Assert.That(result.Events.Any(e => e.DisplayTextKey == "enhancement.hand_multiplier"), Is.False);
        }

        [Test]
        public void ZeroLevelMatchesLegacyScoring()
        {
            var state = new EnhancementState(); // 全 0 级 = 旧行为

            var result = _pipeline.Score(RoyalCards(), new IScoringEffect[] { new HandEnhancementEffect(state) });

            Assert.That(result.FinalScore, Is.EqualTo(1812));
            Assert.That(result.Events.Any(e => e.DisplayTextKey == "enhancement.hand_chips"), Is.False);
        }

        [Test]
        public void MissingTableRowHasNoEffect()
        {
            // 空表（Bootstrap 未注入/play build 场景）→ TryGetHandDelta false → 零效果不抛
            EnhancementConfig.Configure(EnhancementTables.Empty);
            var state = new EnhancementState();
            state.SetHandLevel(HandType.RoyalFlush, 2);

            var result = _pipeline.Score(RoyalCards(), new IScoringEffect[] { new HandEnhancementEffect(state) });

            Assert.That(result.FinalScore, Is.EqualTo(1812));
        }

        /// <summary>A♠ K♠ Q♠ J♠ 10♠ = 皇家同花顺（面值 11+10+10+10+10 = 51）。</summary>
        private static PlayingCardInstance[] RoyalCards()
        {
            return new[]
            {
                Card("ace", Suit.Spades, Rank.Ace),
                Card("king", Suit.Spades, Rank.King),
                Card("queen", Suit.Spades, Rank.Queen),
                Card("jack", Suit.Spades, Rank.Jack),
                Card("ten", Suit.Spades, Rank.Ten)
            };
        }

        /// <summary>皇家与对子的差值表（= HandUp.asset 真实表值推导的增量：皇家 10/1.2、对子 10/0.2 每级）。</summary>
        private static EnhancementTables HandTables()
        {
            var tables = new EnhancementTables();
            tables.HandChipDeltas[HandType.RoyalFlush] = new[] { 10, 20, 30, 40 };
            tables.HandMultDeltas[HandType.RoyalFlush] = new[] { 1.2m, 2.4m, 3.6m, 4.8m };
            tables.HandPrices[HandType.RoyalFlush] = new[] { 8, 11, 14, 17 };
            tables.HandChipDeltas[HandType.Pair] = new[] { 10, 20, 30, 40 };
            tables.HandMultDeltas[HandType.Pair] = new[] { 0.2m, 0.4m, 0.6m, 0.8m };
            tables.HandPrices[HandType.Pair] = new[] { 8, 11, 14, 17 };
            return tables;
        }

        private static PlayingCardInstance Card(string id, Suit suit, Rank rank)
        {
            return new PlayingCardInstance(id, suit, rank);
        }
    }
}
