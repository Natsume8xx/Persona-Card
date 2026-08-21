using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>P0-1I：手牌显示排序器测试（策划案 3.3.10 两种排序规则，只改显示不动牌序）。</summary>
    public sealed class HandDisplaySorterTests
    {
        // 测试牌构造 helper：id 按构造顺序递增，便于断言「结果包含同一组牌」
        private static PlayingCardInstance Card(Suit suit, Rank rank, string id)
        {
            return new PlayingCardInstance(id, suit, rank);
        }

        private static IReadOnlyList<PlayingCardInstance> Sort(IEnumerable<PlayingCardInstance> cards, HandSortMode mode)
        {
            return HandDisplaySorter.Sort(cards, mode);
        }

        private static void AssertOrder(IReadOnlyList<PlayingCardInstance> result, params string[] expectedIds)
        {
            var actual = result.Select(card => card.Id).ToArray();
            Assert.AreEqual(expectedIds, actual, "排序结果顺序不符：实际 [{0}]，期望 [{1}]", string.Join(", ", actual), string.Join(", ", expectedIds));
        }

        [Test]
        public void RankFirstSortsDescendingFromAceToTwo()
        {
            var hand = new[]
            {
                Card(Suit.Spades, Rank.Five, "5S"),
                Card(Suit.Hearts, Rank.Ace, "AH"),
                Card(Suit.Clubs, Rank.King, "KC"),
                Card(Suit.Diamonds, Rank.Two, "2D"),
                Card(Suit.Hearts, Rank.Ten, "10H")
            };

            AssertOrder(Sort(hand, HandSortMode.RankFirst), "AH", "KC", "10H", "5S", "2D");
        }

        [Test]
        public void RankFirstBreaksTiesByHeartsDiamondsClubsSpades()
        {
            // 四张同点数（K），花色不同——同点花色序为 红桃>方块>梅花>黑桃（拍板 1）
            var hand = new[]
            {
                Card(Suit.Spades, Rank.King, "KS"),
                Card(Suit.Clubs, Rank.King, "KC"),
                Card(Suit.Hearts, Rank.King, "KH"),
                Card(Suit.Diamonds, Rank.King, "KD")
            };

            AssertOrder(Sort(hand, HandSortMode.RankFirst), "KH", "KD", "KC", "KS");
        }

        [Test]
        public void SuitGroupedOrdersGroupsHeartsDiamondsClubsSpadesWithDescendingRanks()
        {
            var hand = new[]
            {
                Card(Suit.Clubs, Rank.Four, "4C"),
                Card(Suit.Spades, Rank.Ace, "AS"),
                Card(Suit.Hearts, Rank.Seven, "7H"),
                Card(Suit.Diamonds, Rank.Queen, "QD"),
                Card(Suit.Hearts, Rank.King, "KH"),
                Card(Suit.Clubs, Rank.Jack, "JC"),
                Card(Suit.Diamonds, Rank.Three, "3D"),
                Card(Suit.Spades, Rank.Nine, "9S")
            };

            AssertOrder(Sort(hand, HandSortMode.SuitGrouped), "KH", "7H", "QD", "3D", "JC", "4C", "AS", "9S");
        }

        [Test]
        public void SortDoesNotMutateInputAndReturnsNewInstance()
        {
            var hand = new List<PlayingCardInstance>
            {
                Card(Suit.Spades, Rank.Five, "5S"),
                Card(Suit.Hearts, Rank.Ace, "AH")
            };
            var idsBefore = hand.Select(card => card.Id).ToArray();

            var result = Sort(hand, HandSortMode.RankFirst);

            Assert.AreEqual(new[] { "5S", "AH" }, idsBefore, "输入序列的 id 顺序不应被修改");
            Assert.AreNotSame(hand, result, "排序应返回新的列表实例");
        }

        [Test]
        public void EmptyAndSingleCardHandsAreUnchanged()
        {
            var single = new[] { Card(Suit.Hearts, Rank.Ace, "AH") };

            Assert.AreEqual(0, Sort(System.Array.Empty<PlayingCardInstance>(), HandSortMode.RankFirst).Count);
            AssertOrder(Sort(single, HandSortMode.RankFirst), "AH");
            AssertOrder(Sort(single, HandSortMode.SuitGrouped), "AH");
        }

        [Test]
        public void BothModesReturnTheSameMultisetOfCards()
        {
            // 满手 8 张混合牌：两种模式结果顺序不同但元素集合必须一致（不能丢牌/增牌）
            var hand = new[]
            {
                Card(Suit.Spades, Rank.Ace, "AS"),
                Card(Suit.Hearts, Rank.Two, "2H"),
                Card(Suit.Clubs, Rank.King, "KC"),
                Card(Suit.Diamonds, Rank.Eight, "8D"),
                Card(Suit.Hearts, Rank.Queen, "QH"),
                Card(Suit.Clubs, Rank.Six, "6C"),
                Card(Suit.Spades, Rank.Jack, "JS"),
                Card(Suit.Diamonds, Rank.Ten, "10D")
            };

            var rankFirst = Sort(hand, HandSortMode.RankFirst).Select(card => card.Id).OrderBy(id => id).ToArray();
            var suitGrouped = Sort(hand, HandSortMode.SuitGrouped).Select(card => card.Id).OrderBy(id => id).ToArray();

            Assert.AreEqual(rankFirst, suitGrouped, "两种模式的元素集合应完全一致");
        }
    }
}
