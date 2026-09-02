using System;
using System.Collections.Generic;
using PersonaCards.Core;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 牌型配置资产（P0-1C 数据驱动）：条目由「牌型配置」sheet 导入命令写入，缺失时由白盒工厂兜底。
    /// 运行时经 HandTypeCatalog.Configure 注入；判定层（HandEvaluator）与 Priority 语义不在此资产范围。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/HandTypeCatalog", fileName = "HandTypeCatalog")]
    public sealed class HandTypeAsset : ScriptableObject
    {
        /// <summary>单条牌型配置。</summary>
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("牌型（枚举值 = 判定强度，与 HandType 枚举一致）。")]
            public HandType handType;

            [Tooltip("显示名称（导入自配表「牌型名称」列）。")]
            public string displayName = "";

            [Tooltip("基础筹码（非负整数，配表「基础筹码」列）。")]
            public int baseChips;

            [Tooltip("基础倍率（≥1，配表「基础倍率」列）。Unity 不序列化 decimal，资产存 double；运行时由 HandTypeCatalog 转 decimal（配表值小数位 ≤2，转换无损）。")]
            public double baseMultiplier = 1;

            [Tooltip("显示顺序（配表「显示顺序」列，1 起；0 = 回落枚举序）。")]
            public int displayOrder;

            [Tooltip("牌型品质（配表「牌型品质_ID」列，NORMAL/RARE；词条条件「牌型品质」判定依赖）。")]
            public HandQuality quality = HandQuality.NORMAL;
        }

        /// <summary>牌型条目列表；缺五条/同花五条时该牌型回落白盒占位值（代策划确认 A5 容错精神，已拍板）。</summary>
        [Tooltip("牌型条目列表；缺五条/同花五条时该牌型回落白盒占位值。")]
        public List<Entry> entries = new List<Entry>();

        /// <summary>
        /// 轻量校验（OnValidate、导入命令与目录注入共用）：条目非空、牌型不重复、名称非空、筹码非负、倍率 ≥1 且有限、显示顺序非负。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "牌型配置为空：至少需要一个条目。";
                return false;
            }

            var seen = new HashSet<HandType>();
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry == null)
                {
                    error = $"条目 {index} 为 null。";
                    return false;
                }
                if (!seen.Add(entry.handType))
                {
                    error = $"牌型 {entry.handType} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.displayName))
                {
                    error = $"牌型 {entry.handType} 的显示名称为空。";
                    return false;
                }
                if (entry.baseChips < 0)
                {
                    error = $"牌型 {entry.handType} 的基础筹码为负数（{entry.baseChips}）。";
                    return false;
                }
                // double 需同时有限、≥1、且不超出 decimal 上界（目录转换时 (decimal) 才安全）
                if (double.IsNaN(entry.baseMultiplier) || double.IsInfinity(entry.baseMultiplier)
                    || entry.baseMultiplier < 1 || entry.baseMultiplier > (double)decimal.MaxValue)
                {
                    error = $"牌型 {entry.handType} 的基础倍率无效（{entry.baseMultiplier}），必须为 ≥1 的有限数值。";
                    return false;
                }
                if (entry.displayOrder < 0)
                {
                    error = $"牌型 {entry.handType} 的显示顺序为负数（{entry.displayOrder}）。";
                    return false;
                }
                if (!Enum.IsDefined(typeof(HandQuality), entry.quality))
                {
                    error = $"牌型 {entry.handType} 的品质无效（{(int)entry.quality}）。";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 白盒条目（= 配表「牌型配置」当前初值，11 行 + 五条/同花五条占位）：
        /// 导入命令创建初始资产与场景重建兜底共用此工厂。数值源为 Core 的 HandTypeEntry.CreateFallbackList
        /// （与 HandTypeCatalog 白盒回落同源，数值只写一处），此处转成资产条目（decimal→double，白盒值 ≤2 位小数无损）。
        /// </summary>
        public static List<Entry> CreateFallbackEntries()
        {
            var entries = new List<Entry>();
            foreach (var coreEntry in HandTypeEntry.CreateFallbackList())
            {
                entries.Add(EntryOf(
                    coreEntry.HandType,
                    coreEntry.DisplayName,
                    coreEntry.BaseChips,
                    (double)coreEntry.BaseMultiplier,
                    coreEntry.DisplayOrder,
                    coreEntry.Quality));
            }

            return entries;
        }

        /// <summary>
        /// 转成无引擎依赖的 Core 条目（HandTypeCatalog.Configure 的入参），double→decimal 转换在此完成。
        /// 调用前应先 Validate 拦截非法值；未经 Validate 的超界倍率可能在 (decimal) 转换时抛异常。
        /// </summary>
        public List<HandTypeEntry> BuildEntries()
        {
            var result = new List<HandTypeEntry>();
            foreach (var entry in entries)
            {
                result.Add(new HandTypeEntry(
                    entry.handType,
                    entry.displayName,
                    entry.baseChips,
                    (decimal)entry.baseMultiplier,
                    entry.displayOrder,
                    entry.quality));
            }

            return result;
        }

        /// <summary>便捷构造：白盒工厂单条条目。</summary>
        private static Entry EntryOf(HandType handType, string displayName, int baseChips, double baseMultiplier, int displayOrder, HandQuality quality)
        {
            return new Entry
            {
                handType = handType,
                displayName = displayName,
                baseChips = baseChips,
                baseMultiplier = baseMultiplier,
                displayOrder = displayOrder,
                quality = quality
            };
        }
    }
}
