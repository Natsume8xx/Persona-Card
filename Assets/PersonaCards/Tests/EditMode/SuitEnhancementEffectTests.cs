using System.Linq;
using NUnit.Framework;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Cards;
using PersonaCards.Cards.Scoring;
using PersonaCards.Core;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>花色强化计分效果测试（P0-11）：走完整计分管线，锁定「计分牌 × 花色等级」聚合口径。</summary>
    public sealed class SuitEnhancementEffectTests
    {
        private ScoringPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            _pipeline = new ScoringPipeline();
            EnhancementConfig.Configure(SuitTables());
        }

        [TearDown]
        public void TearDown()
        {
            EnhancementConfig.Configure(EnhancementTables.Empty); // 静态门面复位，防跨测试泄漏
        }

        [Test]
        public void UpgradedSuitAddsPerCardChipsToScoringCardsOnly()
        {
            var state = new EnhancementState();
            state.SetSuitLevel(Suit.Spades, 3); // 黑桃 Lv3 = 每张计分黑桃 +15

            var result = _pipeline.Score(
                new[]
                {
                    Card("eight-spade", Suit.Spades, Rank.Eight),
                    Card("eight-heart", Suit.Hearts, Rank.Eight),
                    Card("kicker-spade", Suit.Spades, Rank.Four) // 黑桃但非计分牌（对子只计两张 8）→ 不加
                },
                new IScoringEffect[] { new SuitEnhancementEffect(state) });

            // 对子基础 48 + 面值 16 + 黑桃 Lv3 15（只算一张计分黑桃）= 79 × 2 = 158
            Assert.That(result.Chips, Is.EqualTo(79m));
            Assert.That(result.FinalScore, Is.EqualTo(158));
            Assert.That(result.Events.Count(e => e.DisplayTextKey == "enhancement.suit_chips"), Is.EqualTo(1));
        }

        [Test]
        public void MultipleUpgradedSuitsAggregate()
        {
            var state = new EnhancementState();
            state.SetSuitLevel(Suit.Spades, 2); // +10
            state.SetSuitLevel(Suit.Hearts, 3); // +15

            var result = _pipeline.Score(
                new[]
                {
                    Card("eight-spade", Suit.Spades, Rank.Eight),
                    Card("eight-heart", Suit.Hearts, Rank.Eight)
                },
                new IScoringEffect[] { new SuitEnhancementEffect(state) });

            // 对子 48 + 面值 16 + 10 + 15 = 89 × 2 = 178
            Assert.That(result.Chips, Is.EqualTo(89m));
            Assert.That(result.FinalScore, Is.EqualTo(178));
            Assert.That(result.Events.Count(e => e.DisplayTextKey == "enhancement.suit_chips"), Is.EqualTo(2));
        }

        [Test]
        public void ZeroLevelMatchesLegacyScoring()
        {
            var state = new EnhancementState(); // 全 0 级 = 旧行为

            var result = _pipeline.Score(
                new[] { Card("eight-a", Suit.Spades, Rank.Eight), Card("eight-b", Suit.Hearts, Rank.Eight) },
                new IScoringEffect[] { new SuitEnhancementEffect(state) });

            Assert.That(result.Chips, Is.EqualTo(64m)); // 48 + 16
            Assert.That(result.FinalScore, Is.EqualTo(128));
            Assert.That(result.Events.Any(e => e.DisplayTextKey == "enhancement.suit_chips"), Is.False);
        }

        [Test]
        public void MissingTableRowYieldsZeroChips()
        {
            // 空表（Bootstrap 未注入/play build 场景）→ SuitChipsOf 回落 0 → 零效果不抛
            EnhancementConfig.Configure(EnhancementTables.Empty);
            var state = new EnhancementState();
            state.SetSuitLevel(Suit.Spades, 2);

            var result = _pipeline.Score(
                new[] { Card("eight-a", Suit.Spades, Rank.Eight), Card("eight-b", Suit.Hearts, Rank.Eight) },
                new IScoringEffect[] { new SuitEnhancementEffect(state) });

            Assert.That(result.FinalScore, Is.EqualTo(128));
        }

        /// <summary>四花色统一 5/10/15/20、价格 8/11/14/17（= SuitUp.asset 真实表值）。</summary>
        private static EnhancementTables SuitTables()
        {
            var tables = new EnhancementTables();
            foreach (var suit in new[] { Suit.Spades, Suit.Hearts, Suit.Clubs, Suit.Diamonds })
            {
                tables.SuitChips[suit] = new[] { 5, 10, 15, 20 };
                tables.SuitPrices[suit] = new[] { 8, 11, 14, 17 };
            }

            return tables;
        }

        private static PlayingCardInstance Card(string id, Suit suit, Rank rank)
        {
            return new PlayingCardInstance(id, suit, rank);
        }
    }
}
