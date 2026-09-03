using System;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// ForgeUnlockState 副属性解锁状态测试（UI 重排第二批）：计数/上限钳制/真实扣款/无副作用失败/恢复种子。
    /// </summary>
    public sealed class ForgeUnlockStateTests
    {
        private static JourneyDeckState Deck(int coins)
        {
            return new JourneyDeckState(
                new[] { new PlayingCardInstance("c1", Suit.Hearts, Rank.Five) }, coins);
        }

        [Test]
        public void UnlockedCountOf_初始为0_未知人格也为0()
        {
            var state = new ForgeUnlockState();
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(0));
        }

        [Test]
        public void UnlockedCountOf_空id抛异常()
        {
            var state = new ForgeUnlockState();
            Assert.Throws<ArgumentException>(() => state.UnlockedCountOf(""));
            Assert.Throws<ArgumentException>(() => state.UnlockedCountOf(null));
        }

        [Test]
        public void TryUnlock_成功_计数加一且扣款()
        {
            var state = new ForgeUnlockState();
            var deck = Deck(10);
            Assert.That(state.TryUnlock("PER_001", 2, 5, deck), Is.True);
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(1));
            Assert.That(deck.Coins, Is.EqualTo(5));
        }

        [Test]
        public void TryUnlock_上限钳制_不扣款()
        {
            var state = new ForgeUnlockState();
            var deck = Deck(20);
            Assert.That(state.TryUnlock("PER_001", 2, 5, deck), Is.True);
            Assert.That(state.TryUnlock("PER_001", 2, 8, deck), Is.True);
            Assert.That(state.TryUnlock("PER_001", 2, 8, deck), Is.False); // 第 3 次超上限
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(2));
            Assert.That(deck.Coins, Is.EqualTo(7));
        }

        [Test]
        public void TryUnlock_金币不足_无副作用()
        {
            var state = new ForgeUnlockState();
            var deck = Deck(4);
            Assert.That(state.TryUnlock("PER_001", 2, 5, deck), Is.False);
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(0));
            Assert.That(deck.Coins, Is.EqualTo(4));
        }

        [Test]
        public void TryUnlock_多人格计数独立()
        {
            var state = new ForgeUnlockState();
            var deck = Deck(20);
            Assert.That(state.TryUnlock("PER_001", 2, 5, deck), Is.True);
            Assert.That(state.TryUnlock("PER_002", 2, 5, deck), Is.True);
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(1));
            Assert.That(state.UnlockedCountOf("PER_002"), Is.EqualTo(1));
        }

        [Test]
        public void TryUnlock_参数非法_抛异常()
        {
            var state = new ForgeUnlockState();
            var deck = Deck(10);
            Assert.Throws<ArgumentException>(() => state.TryUnlock("", 2, 5, deck));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.TryUnlock("PER_001", 0, 5, deck));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.TryUnlock("PER_001", 2, -1, deck));
            Assert.Throws<ArgumentNullException>(() => state.TryUnlock("PER_001", 2, 5, null));
        }

        [Test]
        public void UnlockedEntries_只含计数大于零的人格()
        {
            var state = new ForgeUnlockState();
            var deck = Deck(20);
            state.TryUnlock("PER_001", 2, 5, deck);
            state.TryUnlock("PER_001", 2, 8, deck);
            state.TryUnlock("PER_003", 2, 5, deck);
            var entries = state.UnlockedEntries;
            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries.Any(e => e.Key == "PER_001" && e.Value == 2), Is.True);
            Assert.That(entries.Any(e => e.Key == "PER_003" && e.Value == 1), Is.True);
        }

        [Test]
        public void SeedUnlocked_下限1钳制_重复取较大值()
        {
            var state = new ForgeUnlockState();
            state.SeedUnlocked("PER_001", 0);
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(1));
            state.SeedUnlocked("PER_001", 1);
            state.SeedUnlocked("PER_001", 3);
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(3));
            state.SeedUnlocked("PER_001", 2);
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(3));
            Assert.Throws<ArgumentException>(() => state.SeedUnlocked("", 1));
        }

        [Test]
        public void TryUnlock_价格0_免费解锁可成功()
        {
            var state = new ForgeUnlockState();
            var deck = Deck(0);
            Assert.That(state.TryUnlock("PER_001", 2, 0, deck), Is.True);
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(1));
            Assert.That(deck.Coins, Is.EqualTo(0));
        }
    }
}
