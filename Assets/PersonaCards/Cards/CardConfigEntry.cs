using System;
using System.Collections.Generic;

namespace PersonaCards.Cards
{
    /// <summary>
    /// 卡牌配置纯数据条目（P0-1D 数据驱动）：Data 层资产（CardConfigAsset）与 Cards 层门面（PlayingCardRules）之间的中转契约。
    /// 放 Cards 而非 Core 的原因：Suit/Rank 枚举就在 Cards 程序集，且 Data 已引用 Cards，无需搬枚举。
    /// </summary>
    public sealed class CardConfigEntry
    {
        public CardConfigEntry(
            string cardId,
            string displayName,
            CardKind cardKind,
            Suit suit,
            Rank rank,
            CardParamType paramType,
            int paramValue)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(CardKind), cardKind))
            {
                throw new ArgumentOutOfRangeException(nameof(cardKind), cardKind, "Unknown card kind.");
            }

            if (!Enum.IsDefined(typeof(Suit), suit))
            {
                throw new ArgumentOutOfRangeException(nameof(suit), suit, "Unknown suit.");
            }

            if (!Enum.IsDefined(typeof(Rank), rank))
            {
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "Unknown rank.");
            }

            if (!Enum.IsDefined(typeof(CardParamType), paramType))
            {
                throw new ArgumentOutOfRangeException(nameof(paramType), paramType, "Unknown param type.");
            }

            if (paramValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(paramValue));
            }

            CardId = cardId ?? "";
            DisplayName = displayName;
            CardKind = cardKind;
            Suit = suit;
            Rank = rank;
            ParamType = paramType;
            ParamValue = paramValue;
        }

        /// <summary>卡图绑定 ID（配表「卡牌_ID」列，CARD_xxx；美术接入前仅存值，运行时经 PlayingCardRules.GetCardId 消费）。</summary>
        public string CardId { get; }

        /// <summary>显示名称（配表「卡牌名称」列，如「黑桃A」）。</summary>
        public string DisplayName { get; }

        /// <summary>卡牌类型（配表「卡牌类型」列，当前仅「手牌」）。</summary>
        public CardKind CardKind { get; }

        public Suit Suit { get; }

        public Rank Rank { get; }

        /// <summary>参数类型（配表「参数类型」列，当前仅「筹码」）。</summary>
        public CardParamType ParamType { get; }

        /// <summary>参数值（配表「参数1」列，筹码类型时 = 牌面筹码值，非负整数）。</summary>
        public int ParamValue { get; }

        /// <summary>
        /// 白盒条目（= 配表「卡牌配置」当前初值 52 行：黑桃→红桃→梅花→方块 × A、2~10、J、Q、K；筹码 A=11/J/Q/K=10/其余=点数）。
        /// CardConfigAsset 白盒工厂与 PlayingCardRules 白盒回落同源于此，数值只写一处。
        /// </summary>
        public static List<CardConfigEntry> CreateFallbackList()
        {
            var entries = new List<CardConfigEntry>(52);
            // 表序：黑桃 → 红桃 → 梅花 → 方块；每花色按 A、2~10、J、Q、K
            var suits = new[] { Suit.Spades, Suit.Hearts, Suit.Clubs, Suit.Diamonds };
            var ranks = new[]
            {
                Rank.Ace, Rank.Two, Rank.Three, Rank.Four, Rank.Five, Rank.Six, Rank.Seven,
                Rank.Eight, Rank.Nine, Rank.Ten, Rank.Jack, Rank.Queen, Rank.King
            };
            var index = 1;
            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
                {
                    entries.Add(new CardConfigEntry(
                        $"CARD_{index++:D3}",
                        $"{SuitDisplayName(suit)}{RankDisplayName(rank)}",
                        CardKind.Hand,
                        suit,
                        rank,
                        CardParamType.Chips,
                        GetFallbackChipValue(rank)));
                }
            }

            return entries;
        }

        /// <summary>白盒筹码公式 = 旧 PlayingCardRules.GetFaceChipValue 逻辑（P0-1D 行为零差异的落点）：A=11、J/Q/K=10、其余=点数。</summary>
        private static int GetFallbackChipValue(Rank rank)
        {
            switch (rank)
            {
                case Rank.Jack:
                case Rank.Queen:
                case Rank.King:
                    return 10;
                case Rank.Ace:
                    return 11;
                default:
                    return (int)rank;
            }
        }

        /// <summary>配表「花色」文本 → Suit 枚举（固定映射，CardConfigTableMapper 共用）：黑桃/红桃/梅花/方块。</summary>
        public static bool TryMapSuit(string text, out Suit suit)
        {
            switch (text)
            {
                case "黑桃": suit = Suit.Spades; return true;
                case "红桃": suit = Suit.Hearts; return true;
                case "梅花": suit = Suit.Clubs; return true;
                case "方块": suit = Suit.Diamonds; return true;
                default:
                    suit = default;
                    return false;
            }
        }

        /// <summary>配表「点数」文本 → Rank 枚举（固定映射，CardConfigTableMapper 共用）：A/2~10/J/Q/K。</summary>
        public static bool TryMapRank(string text, out Rank rank)
        {
            switch (text)
            {
                case "A": rank = Rank.Ace; return true;
                case "2": rank = Rank.Two; return true;
                case "3": rank = Rank.Three; return true;
                case "4": rank = Rank.Four; return true;
                case "5": rank = Rank.Five; return true;
                case "6": rank = Rank.Six; return true;
                case "7": rank = Rank.Seven; return true;
                case "8": rank = Rank.Eight; return true;
                case "9": rank = Rank.Nine; return true;
                case "10": rank = Rank.Ten; return true;
                case "J": rank = Rank.Jack; return true;
                case "Q": rank = Rank.Queen; return true;
                case "K": rank = Rank.King; return true;
                default:
                    rank = default;
                    return false;
            }
        }

        /// <summary>Suit → 配表花色文本（白盒工厂生成显示名用）。</summary>
        private static string SuitDisplayName(Suit suit)
        {
            switch (suit)
            {
                case Suit.Spades: return "黑桃";
                case Suit.Hearts: return "红桃";
                case Suit.Clubs: return "梅花";
                case Suit.Diamonds: return "方块";
                default: throw new ArgumentOutOfRangeException(nameof(suit), suit, "Unknown suit.");
            }
        }

        /// <summary>Rank → 配表点数文本（白盒工厂生成显示名用）。</summary>
        private static string RankDisplayName(Rank rank)
        {
            switch (rank)
            {
                case Rank.Ace: return "A";
                case Rank.Jack: return "J";
                case Rank.Queen: return "Q";
                case Rank.King: return "K";
                default: return ((int)rank).ToString();
            }
        }
    }
}
