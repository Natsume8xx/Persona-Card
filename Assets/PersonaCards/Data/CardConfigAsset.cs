using System;
using System.Collections.Generic;
using PersonaCards.Cards;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 卡牌配置资产（P0-1D 数据驱动）：条目由「卡牌配置」sheet 导入命令写入，缺失时由白盒工厂兜底。
    /// 运行时经 PlayingCardRules.Configure 注入；牌面筹码全 int（无小数问题）。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/CardConfig", fileName = "CardConfig")]
    public sealed class CardConfigAsset : ScriptableObject
    {
        /// <summary>单条卡牌配置。</summary>
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("卡图绑定 ID（配表「卡牌_ID」列，CARD_xxx；美术接入前仅存值）。")]
            public string cardId = "";

            [Tooltip("显示名称（配表「卡牌名称」列，如「黑桃A」）。")]
            public string displayName = "";

            [Tooltip("卡牌类型（配表「卡牌类型」列，当前仅「手牌」）。")]
            public CardKind cardKind = CardKind.Hand;

            [Tooltip("花色（配表「花色」列）。")]
            public Suit suit;

            [Tooltip("点数（配表「点数」列）。")]
            public Rank rank;

            [Tooltip("参数类型（配表「参数类型」列，当前仅「筹码」）。")]
            public CardParamType paramType = CardParamType.Chips;

            [Tooltip("参数值（配表「参数1」列，筹码类型时 = 牌面筹码值，非负整数）。")]
            public int paramValue;
        }

        /// <summary>卡牌条目列表；缺卡时该卡回落白盒值（与牌型目录同模式：白盒起步逐条覆盖）。</summary>
        [Tooltip("卡牌条目列表；缺卡时该卡回落白盒值。")]
        public List<Entry> entries = new List<Entry>();

        /// <summary>
        /// 轻量校验（OnValidate、导入命令与门面注入共用）：条目非空、cardId 非空且唯一、(花色,点数) 不重复、
        /// 名称非空、枚举有效、参数值非负。52 张齐全校验不在此层（缺卡由门面白盒补齐）。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "卡牌配置为空：至少需要一个条目。";
                return false;
            }

            var seenCardIds = new HashSet<string>();
            var seenSuitRanks = new HashSet<(Suit, Rank)>();
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry == null)
                {
                    error = $"条目 {index} 为 null。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.cardId))
                {
                    error = $"条目 {index}（{entry.displayName}）的卡牌 ID 为空。";
                    return false;
                }
                if (!seenCardIds.Add(entry.cardId))
                {
                    error = $"卡牌 ID {entry.cardId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.displayName))
                {
                    error = $"卡牌 {entry.cardId} 的显示名称为空。";
                    return false;
                }
                if (!Enum.IsDefined(typeof(CardKind), entry.cardKind))
                {
                    error = $"卡牌 {entry.cardId} 的卡牌类型无效（{entry.cardKind}）。";
                    return false;
                }
                if (!Enum.IsDefined(typeof(Suit), entry.suit))
                {
                    error = $"卡牌 {entry.cardId} 的花色无效（{entry.suit}）。";
                    return false;
                }
                if (!Enum.IsDefined(typeof(Rank), entry.rank))
                {
                    error = $"卡牌 {entry.cardId} 的点数无效（{entry.rank}）。";
                    return false;
                }
                if (!seenSuitRanks.Add((entry.suit, entry.rank)))
                {
                    error = $"卡牌 {entry.cardId} 与已有条目花色点数重复（{entry.suit}/{entry.rank}）。";
                    return false;
                }
                if (!Enum.IsDefined(typeof(CardParamType), entry.paramType))
                {
                    error = $"卡牌 {entry.cardId} 的参数类型无效（{entry.paramType}）。";
                    return false;
                }
                if (entry.paramValue < 0)
                {
                    error = $"卡牌 {entry.cardId} 的参数值为负数（{entry.paramValue}）。";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 白盒条目（= 配表「卡牌配置」当前初值 52 行）：导入命令创建初始资产与场景重建兜底共用此工厂。
        /// 数值源为 Cards 的 CardConfigEntry.CreateFallbackList（与 PlayingCardRules 白盒回落同源，数值只写一处）。
        /// </summary>
        public static List<Entry> CreateFallbackEntries()
        {
            var entries = new List<Entry>();
            foreach (var coreEntry in CardConfigEntry.CreateFallbackList())
            {
                entries.Add(EntryOf(
                    coreEntry.CardId,
                    coreEntry.DisplayName,
                    coreEntry.CardKind,
                    coreEntry.Suit,
                    coreEntry.Rank,
                    coreEntry.ParamType,
                    coreEntry.ParamValue));
            }

            return entries;
        }

        /// <summary>
        /// 转成无引擎依赖的 Cards 条目（PlayingCardRules.Configure 的入参）。
        /// 调用前应先 Validate 拦截非法值（枚举无效等）。
        /// </summary>
        public List<CardConfigEntry> BuildEntries()
        {
            var result = new List<CardConfigEntry>();
            foreach (var entry in entries)
            {
                result.Add(new CardConfigEntry(
                    entry.cardId,
                    entry.displayName,
                    entry.cardKind,
                    entry.suit,
                    entry.rank,
                    entry.paramType,
                    entry.paramValue));
            }

            return result;
        }

        /// <summary>便捷构造：白盒工厂单条条目。</summary>
        private static Entry EntryOf(string cardId, string displayName, CardKind cardKind, Suit suit, Rank rank,
            CardParamType paramType, int paramValue)
        {
            return new Entry
            {
                cardId = cardId,
                displayName = displayName,
                cardKind = cardKind,
                suit = suit,
                rank = rank,
                paramType = paramType,
                paramValue = paramValue
            };
        }
    }
}
