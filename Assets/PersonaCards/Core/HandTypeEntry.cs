using System;
using System.Collections.Generic;

namespace PersonaCards.Core
{
    /// <summary>
    /// 牌型配置纯数据条目（P0-1C）：Data 层资产（HandTypeAsset，double 倍率）与 Cards 层目录（decimal 倍率）之间的中转契约。
    /// 放 Core 的原因：Cards 程序集 noEngineReferences 无法引用 ScriptableObject 资产类型，而 Data 不能反向引用 Cards（循环依赖）。
    /// </summary>
    public sealed class HandTypeEntry
    {
        public HandTypeEntry(
            HandType handType,
            string displayName,
            int baseChips,
            decimal baseMultiplier,
            int displayOrder,
            string cardId)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            }

            if (baseChips < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseChips));
            }

            if (baseMultiplier < 1m)
            {
                throw new ArgumentOutOfRangeException(nameof(baseMultiplier));
            }

            if (displayOrder < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(displayOrder));
            }

            HandType = handType;
            DisplayName = displayName;
            BaseChips = baseChips;
            BaseMultiplier = baseMultiplier;
            DisplayOrder = displayOrder;
            CardId = cardId ?? "";
        }

        public HandType HandType { get; }

        public string DisplayName { get; }

        public int BaseChips { get; }

        /// <summary>基础倍率（decimal：配表含 2.5 等小数，计分管线全 decimal）。</summary>
        public decimal BaseMultiplier { get; }

        /// <summary>显示顺序（配表「显示顺序」列，1 起；0 = 回落枚举序）。</summary>
        public int DisplayOrder { get; }

        /// <summary>卡图绑定 ID（配表 card_id 列；美术接入前仅存值）。</summary>
        public string CardId { get; }

        /// <summary>
        /// 白盒条目（= 配表「牌型配置」当前初值：策划案 5.2.2 正文 10 行 + 五条/同花五条占位 100/8）。
        /// HandTypeAsset 白盒工厂与 HandTypeCatalog 白盒回落同源于此，数值只写一处。
        /// </summary>
        public static List<HandTypeEntry> CreateFallbackList()
        {
            return new List<HandTypeEntry>
            {
                new HandTypeEntry(HandType.HighCard, "高牌", 55, 1m, 1, "HAND_01"),
                new HandTypeEntry(HandType.Pair, "对子", 48, 2m, 2, "HAND_02"),
                new HandTypeEntry(HandType.TwoPair, "两队", 52, 2.5m, 3, "HAND_03"),
                new HandTypeEntry(HandType.ThreeOfAKind, "三条", 57, 3m, 4, "HAND_04"),
                new HandTypeEntry(HandType.Straight, "顺子", 60, 4m, 5, "HAND_05"),
                new HandTypeEntry(HandType.Flush, "同花", 65, 4m, 6, "HAND_06"),
                new HandTypeEntry(HandType.FullHouse, "葫芦", 74, 5m, 7, "HAND_07"),
                new HandTypeEntry(HandType.FourOfAKind, "四条", 100, 6m, 8, "HAND_08"),
                new HandTypeEntry(HandType.StraightFlush, "同花顺", 95, 10m, 9, "HAND_09"),
                // 五条/同花五条不在配表中（标准 52 张打不出）：保留代码判定与占位值，表补行后自然覆盖
                new HandTypeEntry(HandType.FiveOfAKind, "五条", 100, 8m, 11, ""),
                new HandTypeEntry(HandType.FlushHouse, "同花葫芦", 70, 12m, 10, "HAND_10"),
                new HandTypeEntry(HandType.FlushFive, "同花五条", 100, 8m, 12, "")
            };
        }
    }
}
