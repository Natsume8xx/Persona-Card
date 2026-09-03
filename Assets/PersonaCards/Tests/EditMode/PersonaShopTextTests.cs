using System;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// PersonaShopText 文案拼装测试（UI 重排第二批）：效果描述分类型格式化、参数缺失/非「增加」回退、
    /// 卡牌名与花色符号、解锁节点中文名与到达判定。
    /// </summary>
    public sealed class PersonaShopTextTests
    {
        [Test]
        public void EffectTextOf_基础筹码_整数原文()
        {
            Assert.That(PersonaShopText.EffectTextOf("基础筹码", "增加", "15"), Is.EqualTo("基础筹码 +15"));
        }

        [Test]
        public void EffectTextOf_基础倍率_小数原文保留()
        {
            Assert.That(PersonaShopText.EffectTextOf("基础倍率", "增加", "0.3"), Is.EqualTo("基础倍率 +0.3"));
            Assert.That(PersonaShopText.EffectTextOf("基础倍率", "增加", "1"), Is.EqualTo("基础倍率 +1"));
        }

        [Test]
        public void EffectTextOf_独立倍率_转百分数()
        {
            Assert.That(PersonaShopText.EffectTextOf("独立倍率", "增加", "0.05"), Is.EqualTo("独立倍率 +5%"));
            Assert.That(PersonaShopText.EffectTextOf("独立倍率", "增加", "0.03"), Is.EqualTo("独立倍率 +3%"));
        }

        [Test]
        public void EffectTextOf_次数与金币_整数原文()
        {
            Assert.That(PersonaShopText.EffectTextOf("出牌次数", "增加", "1"), Is.EqualTo("出牌次数 +1"));
            Assert.That(PersonaShopText.EffectTextOf("弃牌次数", "增加", "1"), Is.EqualTo("弃牌次数 +1"));
            Assert.That(PersonaShopText.EffectTextOf("金币", "增加", "5"), Is.EqualTo("金币 +5"));
        }

        [Test]
        public void EffectTextOf_参数1非增加_原样拼接()
        {
            Assert.That(PersonaShopText.EffectTextOf("基础筹码", "减少", "5"), Is.EqualTo("基础筹码 减少5"));
        }

        [Test]
        public void EffectTextOf_参数1为空_按增加处理()
        {
            Assert.That(PersonaShopText.EffectTextOf("基础筹码", "", "5"), Is.EqualTo("基础筹码 +5"));
            Assert.That(PersonaShopText.EffectTextOf("基础筹码", null, "5"), Is.EqualTo("基础筹码 +5"));
        }

        [Test]
        public void EffectTextOf_参数2缺失_只返回类型名()
        {
            Assert.That(PersonaShopText.EffectTextOf("基础筹码", "增加", ""), Is.EqualTo("基础筹码"));
            Assert.That(PersonaShopText.EffectTextOf("基础筹码", "增加", null), Is.EqualTo("基础筹码"));
        }

        [Test]
        public void EffectTextOf_类型为空_抛异常()
        {
            Assert.Throws<ArgumentException>(() => PersonaShopText.EffectTextOf("", "增加", "5"));
            Assert.Throws<ArgumentException>(() => PersonaShopText.EffectTextOf(null, "增加", "5"));
        }

        [Test]
        public void EffectTextOf_独立倍率解析失败_原样返回()
        {
            Assert.That(PersonaShopText.EffectTextOf("独立倍率", "增加", "abc"), Is.EqualTo("独立倍率 +abc"));
        }

        [Test]
        public void CardTextOf_花色中文加点数()
        {
            Assert.That(PersonaShopText.CardTextOf(Suit.Spades, Rank.Ace), Is.EqualTo("黑桃A"));
            Assert.That(PersonaShopText.CardTextOf(Suit.Hearts, Rank.Ten), Is.EqualTo("红桃10"));
            Assert.That(PersonaShopText.CardTextOf(Suit.Clubs, Rank.Jack), Is.EqualTo("梅花J"));
            Assert.That(PersonaShopText.CardTextOf(Suit.Diamonds, Rank.King), Is.EqualTo("方片K"));
        }

        [Test]
        public void CardSymbolOf_四花色符号()
        {
            Assert.That(PersonaShopText.CardSymbolOf(Suit.Spades), Is.EqualTo("♠"));
            Assert.That(PersonaShopText.CardSymbolOf(Suit.Hearts), Is.EqualTo("♥"));
            Assert.That(PersonaShopText.CardSymbolOf(Suit.Clubs), Is.EqualTo("♣"));
            Assert.That(PersonaShopText.CardSymbolOf(Suit.Diamonds), Is.EqualTo("♦"));
        }

        [Test]
        public void UnlockRankOf_节点中文名与回退()
        {
            Assert.That(PersonaShopText.UnlockRankOf("AI1"), Is.EqualTo("第一章"));
            Assert.That(PersonaShopText.UnlockRankOf("AI2"), Is.EqualTo("第二章"));
            Assert.That(PersonaShopText.UnlockRankOf("AI3"), Is.EqualTo("第三章"));
            Assert.That(PersonaShopText.UnlockRankOf("默认"), Is.EqualTo("默认"));
            Assert.That(PersonaShopText.UnlockRankOf("未知"), Is.EqualTo("未知"));
            Assert.That(PersonaShopText.UnlockRankOf(""), Is.EqualTo("默认"));
        }

        [Test]
        public void IsNodeReached_按已过节点数判定()
        {
            Assert.That(PersonaShopText.IsNodeReached("AI1", 0), Is.True);
            Assert.That(PersonaShopText.IsNodeReached("AI2", 0), Is.False);
            Assert.That(PersonaShopText.IsNodeReached("AI2", 1), Is.True);
            Assert.That(PersonaShopText.IsNodeReached("AI3", 1), Is.False);
            Assert.That(PersonaShopText.IsNodeReached("AI3", 2), Is.True);
            Assert.That(PersonaShopText.IsNodeReached("AI3", 3), Is.True);
        }

        [Test]
        public void IsNodeReached_默认与未知节点_不设限()
        {
            Assert.That(PersonaShopText.IsNodeReached("默认", 0), Is.True);
            Assert.That(PersonaShopText.IsNodeReached("未知", 0), Is.True);
            Assert.That(PersonaShopText.IsNodeReached("", 0), Is.True);
        }
    }
}
