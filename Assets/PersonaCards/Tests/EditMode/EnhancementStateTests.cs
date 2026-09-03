using NUnit.Framework;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Cards;
using PersonaCards.Core;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>三线强化等级状态测试（P0-11）：纯数据类，无引擎依赖。</summary>
    public sealed class EnhancementStateTests
    {
        [Test]
        public void DefaultsAreZeroForUnknownKeys()
        {
            var state = new EnhancementState();

            Assert.That(state.PersonaLevelOf("PER_001"), Is.EqualTo(0));
            Assert.That(state.SuitLevelOf(Suit.Spades), Is.EqualTo(0));
            Assert.That(state.HandLevelOf(HandType.Flush), Is.EqualTo(0));
        }

        [Test]
        public void PersonaLevelOfTreatsNullKeyAsZero()
        {
            var state = new EnhancementState();
            state.TryUpgradePersona("PER_001");

            Assert.That(state.PersonaLevelOf(null), Is.EqualTo(0));
            Assert.That(state.PersonaLevelOf("PER_001"), Is.EqualTo(1));
        }

        [Test]
        public void TryUpgradeIncrementsAndCapsAtMaxLevel()
        {
            var state = new EnhancementState();

            for (var i = 0; i < 4; i++)
            {
                Assert.That(state.TryUpgradeSuit(Suit.Spades), Is.True);
            }

            Assert.That(state.SuitLevelOf(Suit.Spades), Is.EqualTo(4));
            Assert.That(state.TryUpgradeSuit(Suit.Spades), Is.False); // 满级拒绝
            Assert.That(state.SuitLevelOf(Suit.Spades), Is.EqualTo(4)); // 无副作用
        }

        [Test]
        public void TryUpgradePersonaRejectsNullOrEmptyKey()
        {
            var state = new EnhancementState();

            Assert.That(state.TryUpgradePersona(null), Is.False);
            Assert.That(state.TryUpgradePersona(""), Is.False);
        }

        [Test]
        public void ThreeLinesAreIndependent()
        {
            var state = new EnhancementState();
            state.TryUpgradeSuit(Suit.Spades);
            state.TryUpgradeHand(HandType.Flush);

            Assert.That(state.SuitLevelOf(Suit.Spades), Is.EqualTo(1));
            Assert.That(state.SuitLevelOf(Suit.Hearts), Is.EqualTo(0)); // 同线不同键互不影响
            Assert.That(state.HandLevelOf(HandType.Flush), Is.EqualTo(1));
            Assert.That(state.HandLevelOf(HandType.Pair), Is.EqualTo(0));
            Assert.That(state.PersonaLevelOf("PER_001"), Is.EqualTo(0)); // 线间互不影响
        }

        [Test]
        public void SetLevelsClampToRange()
        {
            var state = new EnhancementState();

            state.SetSuitLevel(Suit.Spades, -5);
            state.SetHandLevel(HandType.Flush, 99);
            state.SetPersonaLevel("PER_001", 2);

            Assert.That(state.SuitLevelOf(Suit.Spades), Is.EqualTo(0)); // 钳制到下限
            Assert.That(state.HandLevelOf(HandType.Flush), Is.EqualTo(4)); // 钳制到上限
            Assert.That(state.PersonaLevelOf("PER_001"), Is.EqualTo(2));
        }

        [Test]
        public void CloneIsIndependent()
        {
            var state = new EnhancementState();
            state.TryUpgradeSuit(Suit.Spades);
            var clone = state.Clone();

            clone.TryUpgradeSuit(Suit.Spades); // 改克隆
            clone.SetHandLevel(HandType.Pair, 3);

            Assert.That(state.SuitLevelOf(Suit.Spades), Is.EqualTo(1)); // 原对象不变
            Assert.That(state.HandLevelOf(HandType.Pair), Is.EqualTo(0));
            Assert.That(clone.SuitLevelOf(Suit.Spades), Is.EqualTo(2));
            Assert.That(clone.HandLevelOf(HandType.Pair), Is.EqualTo(3));
        }
    }
}
