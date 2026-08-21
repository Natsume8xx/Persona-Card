using System;
using PersonaCards.Core;

namespace PersonaCards.Cards.Hands
{
    /// <summary>
    /// 单个牌型的静态定义（P0-1C 数据驱动）：名称/筹码/倍率/显示顺序/卡图绑定来自配表，
    /// 判定强度 Priority 永远等于 HandType 枚举序（人格牌"最低牌型"条件判定依赖，切勿写入显示顺序）。
    /// </summary>
    public sealed class HandTypeDefinition
    {
        public HandTypeDefinition(
            HandType handType,
            string displayName,
            int baseChips,
            decimal baseMultiplier,
            int displayOrder = 0,
            string cardId = "")
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

            HandType = handType;
            DisplayName = displayName;
            Priority = (int)handType;
            BaseChips = baseChips;
            BaseMultiplier = baseMultiplier;
            // 显示顺序未配置（0）时回落枚举序，保证旧调用与白盒占位条目都有可用值
            DisplayOrder = displayOrder > 0 ? displayOrder : (int)handType;
            CardId = cardId ?? "";
        }

        public HandType HandType { get; }

        public string DisplayName { get; }

        /// <summary>判定强度 = 枚举序（人格牌 MinimumHandPriority 用），不是配表"显示顺序"。</summary>
        public int Priority { get; }

        public int BaseChips { get; }

        /// <summary>基础倍率（decimal：配表含 2.5 等小数，计分管线全 decimal）。</summary>
        public decimal BaseMultiplier { get; }

        /// <summary>展示/排序顺序（配表"显示顺序"列，1 起）。</summary>
        public int DisplayOrder { get; }

        /// <summary>卡图绑定 ID（配表 card_id 列；美术接入前仅存值，运行时未消费）。</summary>
        public string CardId { get; }
    }
}
