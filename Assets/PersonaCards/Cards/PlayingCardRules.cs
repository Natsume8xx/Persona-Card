using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PersonaCards.Cards
{
    /// <summary>
    /// 卡牌规则门面（P0-1D 数据驱动）：权威数据来自配表「卡牌配置」sheet（CardConfigAsset → CardConfigEntry 注入）。
    /// Configure 注入条目；null/空/非法（重复花色点数）时回落白盒（= 配表当前初值），保证计分任何情况下可用。
    /// 本程序集 noEngineReferences 不能引用 ScriptableObject 资产类型，故 Configure 只收本程序集纯数据条目，
    /// 资产→条目的转换由调用方（UI 层）经 CardConfigAsset.BuildEntries 完成。
    /// 牌面筹码按 (花色,点数) 逐卡查表（配表当前同点数四花色筹码相同，结构为将来逐卡差异化留口子）；
    /// 卡牌实例 ID 格式 standard-* 不在此范围（P0-1D 拍板：不改实例 ID，CARD_xxx 经 GetCardId 作美术绑定 ID）。
    /// </summary>
    public static class PlayingCardRules
    {
        /// <summary>白盒回落（= CardConfigEntry.CreateFallbackList 同源，52 张）。</summary>
        private static readonly IReadOnlyDictionary<(Suit, Rank), CardConfigEntry> FallbackConfig =
            BuildConfig(CardConfigEntry.CreateFallbackList());

        private static IReadOnlyDictionary<(Suit, Rank), CardConfigEntry> _config = FallbackConfig;
        private static string _lastConfiguredSummary;

        /// <summary>
        /// 注入卡牌条目：null/空/含 null 条目/重复 (花色,点数) → 静默回落白盒（本程序集无引擎引用不能打日志，
        /// 未配置/校验失败的提示由调用方负责）；缺卡 → 该卡回落白盒值（从白盒 52 张起步，配置条目逐张覆盖）。
        /// </summary>
        public static void Configure(IReadOnlyList<CardConfigEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                ResetToFallback();
                return;
            }

            // 从白盒 52 张起步，配置条目逐张覆盖；重复 (花色,点数) 整体回落白盒（同牌型目录模式）
            var configured = new Dictionary<(Suit, Rank), CardConfigEntry>(FallbackConfig);
            var seen = new HashSet<(Suit, Rank)>();
            var configuredCount = 0;
            try
            {
                foreach (var entry in entries)
                {
                    if (entry == null)
                    {
                        throw new ArgumentException("Entries cannot contain null.", nameof(entries));
                    }

                    var key = (entry.Suit, entry.Rank);
                    if (!seen.Add(key))
                    {
                        throw new ArgumentException($"Duplicate card entry: {entry.Suit}/{entry.Rank}.", nameof(entries));
                    }

                    configured[key] = entry;
                    configuredCount++;
                }
            }
            catch (ArgumentException)
            {
                // 条目重复或非法：整体回落白盒，计分始终可用
                ResetToFallback();
                return;
            }

            _config = new ReadOnlyDictionary<(Suit, Rank), CardConfigEntry>(configured);
            // 成功日志由调用方（UI 层）负责，本程序集无引擎引用
            _lastConfiguredSummary = $"{configured.Count} 张卡（{configuredCount} 张来自配置，{configured.Count - configuredCount} 张回落白盒）";
        }

        /// <summary>最近一次 Configure 的摘要（供调用方打日志；回落白盒时为空串）。</summary>
        public static string LastConfiguredSummary => _lastConfiguredSummary ?? "";

        /// <summary>回到白盒回落（52 张 = 配表当前初值）。</summary>
        private static void ResetToFallback()
        {
            _config = FallbackConfig;
            _lastConfiguredSummary = "";
        }

        /// <summary>取牌面筹码值（配表「参数1」列，筹码类型）；非法枚举抛异常；Configure 后 52 组合必然齐全，查表恒命中。</summary>
        public static int GetFaceChipValue(Suit suit, Rank rank)
        {
            return GetEntry(suit, rank).ParamValue;
        }

        /// <summary>取卡图绑定 ID（配表「卡牌_ID」列，CARD_xxx；美术接入用，当前仅存值）。</summary>
        public static string GetCardId(Suit suit, Rank rank)
        {
            return GetEntry(suit, rank).CardId;
        }

        /// <summary>查 (花色,点数) 条目：枚举非法抛 ArgumentOutOfRangeException（沿用旧语义）；查无抛 InvalidOperationException（正常不会触发）。</summary>
        private static CardConfigEntry GetEntry(Suit suit, Rank rank)
        {
            if (!Enum.IsDefined(typeof(Suit), suit))
            {
                throw new ArgumentOutOfRangeException(nameof(suit), suit, "Unknown suit.");
            }

            if (!Enum.IsDefined(typeof(Rank), rank))
            {
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "Unknown rank.");
            }

            if (!_config.TryGetValue((suit, rank), out var entry))
            {
                throw new InvalidOperationException($"No card config for {suit}/{rank}.");
            }

            return entry;
        }

        /// <summary>条目 → (花色,点数) 字典（重复键抛 ArgumentException；仅供白盒构造）。</summary>
        private static Dictionary<(Suit, Rank), CardConfigEntry> BuildConfig(IEnumerable<CardConfigEntry> entries)
        {
            var config = new Dictionary<(Suit, Rank), CardConfigEntry>();
            foreach (var entry in entries)
            {
                var key = (entry.Suit, entry.Rank);
                if (config.ContainsKey(key))
                {
                    throw new ArgumentException($"Duplicate card entry: {entry.Suit}/{entry.Rank}.", nameof(entries));
                }

                config.Add(key, entry);
            }

            return config;
        }
    }
}
