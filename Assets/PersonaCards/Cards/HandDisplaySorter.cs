using System.Collections.Generic;
using System.Linq;

namespace PersonaCards.Cards
{
    /// <summary>手牌显示排序模式（策划案 3.3.10：排序只改变显示顺序，不改变牌本身、选中状态和抽牌顺序）。</summary>
    public enum HandSortMode
    {
        /// <summary>大小排序：A、K、Q、J、10……2 从大到小，点数相同时按花色序（红桃>方块>梅花>黑桃）。</summary>
        RankFirst,

        /// <summary>花色排序：红桃、方块、梅花、黑桃分组，同花色内部再按点数从大到小。</summary>
        SuitGrouped
    }

    /// <summary>
    /// 手牌显示排序器（纯逻辑，EditMode 可测）。
    /// 只生成新的显示顺序列表，绝不修改输入序列——保证抽牌顺序与选中状态不受排序影响。
    /// </summary>
    public static class HandDisplaySorter
    {
        /// <summary>按指定模式排序，返回新数组；输入序列本身不被修改。</summary>
        public static IReadOnlyList<PlayingCardInstance> Sort(IEnumerable<PlayingCardInstance> cards, HandSortMode mode)
        {
            return mode switch
            {
                HandSortMode.RankFirst => RankFirstOrder(cards),
                _ => SuitGroupedOrder(cards)
            };
        }

        /// <summary>大小排序：点数从大到小（Ace=14 → Two=2），同点数按花色序。</summary>
        private static IReadOnlyList<PlayingCardInstance> RankFirstOrder(IEnumerable<PlayingCardInstance> cards)
        {
            // OrderBy/ThenBy 是稳定排序，不改变原序列
            return cards
                .OrderByDescending(card => card.Rank)
                .ThenBy(card => SuitDisplayOrder(card.Suit))
                .ToArray();
        }

        /// <summary>花色排序：按策划案分组顺序 红桃→方块→梅花→黑桃，组内点数从大到小。</summary>
        private static IReadOnlyList<PlayingCardInstance> SuitGroupedOrder(IEnumerable<PlayingCardInstance> cards)
        {
            return cards
                .OrderBy(card => SuitDisplayOrder(card.Suit))
                .ThenByDescending(card => card.Rank)
                .ToArray();
        }

        /// <summary>
        /// 花色显示序映射。
        /// 策划案 3.3.10 的花色顺序为 红桃→方块→梅花→黑桃，与 Suit 枚举定义（梅花0<方块1<红桃2<黑桃3）不同，
        /// 故排序必须走本映射而不能直接用枚举值。
        /// </summary>
        private static int SuitDisplayOrder(Suit suit) => suit switch
        {
            Suit.Hearts => 0,
            Suit.Diamonds => 1,
            Suit.Clubs => 2,
            _ => 3
        };
    }
}
