using System;
using System.Collections.Generic;

namespace PersonaCards.Cards
{
    /// <summary>
    /// 花色配置纯数据条目（P0-11）：数据层资产（SuitConfigAsset）与显示/强化门面之间的中转契约。
    /// 放 Cards 的原因：Suit 枚举在 Cards 程序集（Core 反向引用会成环），与 CardConfigEntry 同居。
    /// </summary>
    public sealed class SuitConfigEntry
    {
        public SuitConfigEntry(Suit suit, string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            }

            Suit = suit;
            DisplayName = displayName;
        }

        public Suit Suit { get; }

        public string DisplayName { get; }

        /// <summary>白盒条目（= 配表「花色配置」当前初值：4 行 SUIT_001~004）。资产白盒工厂同源于此，数值只写一处。</summary>
        public static List<SuitConfigEntry> CreateFallbackList()
        {
            return new List<SuitConfigEntry>
            {
                new SuitConfigEntry(Suit.Spades, "黑桃"),
                new SuitConfigEntry(Suit.Hearts, "红桃"),
                new SuitConfigEntry(Suit.Clubs, "梅花"),
                new SuitConfigEntry(Suit.Diamonds, "方块")
            };
        }
    }
}
