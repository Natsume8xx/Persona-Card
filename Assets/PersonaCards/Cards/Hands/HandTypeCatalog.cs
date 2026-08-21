using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PersonaCards.Core;

namespace PersonaCards.Cards.Hands
{
    /// <summary>
    /// 牌型目录门面（P0-1C 数据驱动）：权威数据来自配表「牌型配置」sheet（HandTypeAsset → HandTypeEntry 注入）。
    /// Configure 注入条目；null/空/非法时回落白盒（= 配表当前初值），保证判定层任何情况下可用。
    /// 本程序集 noEngineReferences 不能引用 ScriptableObject 资产类型，故 Configure 只收 Core 纯数据条目，
    /// 资产→条目的转换由调用方（UI 层）经 HandTypeAsset.BuildEntries 完成（已拍板）。
    /// 判定层（HandEvaluator 的组合判定与计分牌选取）与 Priority 语义不配表化，保持代码实现。
    /// </summary>
    public static class HandTypeCatalog
    {
        /// <summary>白盒回落（= HandTypeEntry.CreateFallbackList 同源，12 个牌型含五条/同花五条占位）。</summary>
        private static readonly IReadOnlyDictionary<HandType, HandTypeDefinition> FallbackDefinitions =
            BuildDefinitions(HandTypeEntry.CreateFallbackList());

        private static IReadOnlyDictionary<HandType, HandTypeDefinition> _definitions = FallbackDefinitions;
        private static IReadOnlyCollection<HandTypeDefinition> _all = BuildAll(FallbackDefinitions);

        /// <summary>全部牌型定义，按显示顺序（配表「显示顺序」列）排列。</summary>
        public static IReadOnlyCollection<HandTypeDefinition> All => _all;

        /// <summary>
        /// 注入牌型条目：null/空/非法（重复牌型等）→ 静默回落白盒（本程序集无引擎引用不能打日志，
        /// 未配置/校验失败的提示由调用方负责）；缺五条/同花五条等条目 → 该牌型回落白盒占位（已拍板容错）。
        /// </summary>
        public static void Configure(IReadOnlyList<HandTypeEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                ResetToFallback();
                return;
            }

            Dictionary<HandType, HandTypeDefinition> configured;
            try
            {
                configured = BuildDefinitions(entries);
            }
            catch (ArgumentException)
            {
                // 条目重复或非法：整体回落白盒，判定层始终可用
                ResetToFallback();
                return;
            }

            var fallbackCount = 0;

            // 条目缺失（如五条/同花五条不在配表）的牌型用白盒补齐，判定层始终拿到完整 12 个定义
            foreach (var pair in FallbackDefinitions)
            {
                if (configured.ContainsKey(pair.Key)) continue;
                configured[pair.Key] = pair.Value;
                fallbackCount++;
            }

            _definitions = new ReadOnlyDictionary<HandType, HandTypeDefinition>(configured);
            _all = BuildAll(_definitions);
            // 成功日志由调用方（UI 层）负责，本程序集无引擎引用
            _lastConfiguredSummary = $"{configured.Count} 个牌型（{configured.Count - fallbackCount} 个来自配置，{fallbackCount} 个回落白盒）";
        }

        /// <summary>最近一次 Configure 的摘要（供调用方打日志；回落白盒时为空串）。</summary>
        public static string LastConfiguredSummary => _lastConfiguredSummary ?? "";

        private static string _lastConfiguredSummary;

        /// <summary>回到白盒回落（12 个牌型 = 配表当前初值）。</summary>
        private static void ResetToFallback()
        {
            _definitions = FallbackDefinitions;
            _all = BuildAll(FallbackDefinitions);
            _lastConfiguredSummary = "";
        }

        /// <summary>取指定牌型定义；未知牌型抛 ArgumentOutOfRangeException（Configure 后必然齐全，正常不会触发）。</summary>
        public static HandTypeDefinition Get(HandType handType)
        {
            if (!_definitions.TryGetValue(handType, out var definition))
            {
                throw new ArgumentOutOfRangeException(nameof(handType), handType, "Unknown hand type.");
            }

            return definition;
        }

        /// <summary>
        /// Core 条目 → 定义字典（可变：供白盒补齐后包 ReadOnly；重复牌型抛 ArgumentException）。
        /// decimal 倍率在 HandTypeEntry 已定型，此处无 double 转换。
        /// </summary>
        private static Dictionary<HandType, HandTypeDefinition> BuildDefinitions(IEnumerable<HandTypeEntry> entries)
        {
            var definitions = new Dictionary<HandType, HandTypeDefinition>();
            foreach (var entry in entries)
            {
                if (definitions.ContainsKey(entry.HandType))
                {
                    throw new ArgumentException($"Duplicate hand type entry: {entry.HandType}.", nameof(entries));
                }

                definitions.Add(entry.HandType, new HandTypeDefinition(
                    entry.HandType,
                    entry.DisplayName,
                    entry.BaseChips,
                    entry.BaseMultiplier,
                    entry.DisplayOrder,
                    entry.CardId));
            }

            return definitions;
        }

        /// <summary>按显示顺序（再按枚举序兜底）整理 All 集合。</summary>
        private static IReadOnlyCollection<HandTypeDefinition> BuildAll(IReadOnlyDictionary<HandType, HandTypeDefinition> definitions)
        {
            return new ReadOnlyCollection<HandTypeDefinition>(
                definitions.Values
                    .OrderBy(definition => definition.DisplayOrder)
                    .ThenBy(definition => definition.HandType)
                    .ToList());
        }
    }
}
