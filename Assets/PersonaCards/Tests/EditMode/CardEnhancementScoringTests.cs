using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.Cards.Scoring;

namespace PersonaCards.Tests.EditMode
{
    // UI 重排第二批：商店单卡强化服务（SHOP_SERVICE_001~004）的结算语义锁定——ChipPlus/MultPlus/IndependentMult/CoinBonus。
    public sealed class CardEnhancementScoringTests
    {
        private ScoringPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            _pipeline = new ScoringPipeline();
        }

        [Test]
        public void ChipPlusAddsFiveChipsOnScoringCard()
        {
            var result = _pipeline.Score(new[]
            {
                Card("eight-a", Suit.Spades, Rank.Eight, CardEnhancement.ChipPlus),
                Card("eight-b", Suit.Hearts, Rank.Eight)
            });

            // 对子基础 48，两张 8 面值 16 + 筹码强化 5 → 69 × 2 = 138
            Assert.That(result.Chips, Is.EqualTo(69m));
            Assert.That(result.Multiplier, Is.EqualTo(2m));
            Assert.That(result.FinalScore, Is.EqualTo(138));
        }

        [Test]
        public void MultPlusAddsHalfMultiplierOnScoringCard()
        {
            var result = _pipeline.Score(new[]
            {
                Card("ace", Suit.Spades, Rank.Ace, CardEnhancement.MultPlus),
                Card("junk", Suit.Hearts, Rank.Four)
            });

            // 高牌基础 55/1，A 面值 11 + 倍率强化 0.5 → 66 × 1.5 = 99
            Assert.That(result.Chips, Is.EqualTo(66m));
            Assert.That(result.Multiplier, Is.EqualTo(1.5m));
            Assert.That(result.FinalScore, Is.EqualTo(99));
        }

        [Test]
        public void IndependentMultMultipliesFinalScoreByOnePointZeroThree()
        {
            var result = _pipeline.Score(new[]
            {
                Card("ace", Suit.Spades, Rank.Ace, CardEnhancement.IndependentMult),
                Card("junk", Suit.Hearts, Rank.Four)
            });

            // 高牌 66 × 1（倍率不变）× 1.03 = 67.98 → 四舍五入 68
            Assert.That(result.Multiplier, Is.EqualTo(1m)); // 独立乘区不动倍率本身
            Assert.That(result.FinalScore, Is.EqualTo(68));
        }

        [Test]
        public void IndependentMultStacksPerEnhancedScoringCard()
        {
            var result = _pipeline.Score(new[]
            {
                Card("eight-a", Suit.Spades, Rank.Eight, CardEnhancement.IndependentMult),
                Card("eight-b", Suit.Hearts, Rank.Eight, CardEnhancement.IndependentMult)
            });

            // 对子 64 × 2 × 1.03 × 1.03 = 135.7952 → 四舍五入 136（多张叠乘）
            Assert.That(result.FinalScore, Is.EqualTo(136));
        }

        [Test]
        public void CoinBonusHasNoScoringEffect()
        {
            var result = _pipeline.Score(new[]
            {
                Card("eight-a", Suit.Spades, Rank.Eight, CardEnhancement.CoinBonus),
                Card("eight-b", Suit.Hearts, Rank.Eight)
            });

            // 与无增强的对子完全一致：64 × 2 = 128，且不产生任何增强事件
            Assert.That(result.Chips, Is.EqualTo(64m));
            Assert.That(result.Multiplier, Is.EqualTo(2m));
            Assert.That(result.FinalScore, Is.EqualTo(128));
            Assert.That(result.Events.All(e => e.SourceType != ScoringSourceType.CardEnhancement), Is.True);
        }

        [Test]
        public void NewEnhancementsTriggerOnlyOnScoringCards()
        {
            var result = _pipeline.Score(new[]
            {
                Card("ace", Suit.Spades, Rank.Ace),
                Card("junk", Suit.Hearts, Rank.Four, CardEnhancement.ChipPlus)
            });

            // 高牌 55 + A 面值 11 = 66 × 1 = 66：杂牌的筹码强化不触发
            Assert.That(result.FinalScore, Is.EqualTo(66));
            Assert.That(result.Events.Any(e => e.SourceId == "junk"), Is.False);
        }

        [Test]
        public void LegacyEnhancementValuesAndEffectsUnchangedForSaveCompatibility()
        {
            // 旧值 0~3 语义与顺序永久不变（SavedPlayingCard.enhancement 按 int 序列化）
            Assert.That((int)CardEnhancement.None, Is.EqualTo(0));
            Assert.That((int)CardEnhancement.ChipBoost, Is.EqualTo(1));
            Assert.That((int)CardEnhancement.MultBoost, Is.EqualTo(2));
            Assert.That((int)CardEnhancement.WildSuit, Is.EqualTo(3));

            // ChipBoost 仍为 +20 筹码（奖励节点平衡不回归）
            var result = _pipeline.Score(new[]
            {
                Card("eight-a", Suit.Spades, Rank.Eight, CardEnhancement.ChipBoost),
                Card("eight-b", Suit.Hearts, Rank.Eight)
            });
            Assert.That(result.FinalScore, Is.EqualTo(168)); // 48+16+20=84 × 2
        }

        private static PlayingCardInstance Card(
            string id,
            Suit suit,
            Rank rank,
            CardEnhancement enhancement = CardEnhancement.None)
        {
            return new PlayingCardInstance(id, suit, rank, enhancement);
        }
    }
}
