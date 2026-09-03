using System;
using System.Collections.Generic;
using PersonaCards.Cards;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 花色配置资产（P0-11）：条目由「花色配置」sheet 导入命令写入，缺失时由白盒工厂兜底。
    /// 运行时零接线（三线强化的花色线数值来自 SuitUp 表，本资产为花色_ID 权威契约与显示名来源）。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/SuitConfig", fileName = "SuitConfig")]
    public sealed class SuitConfigAsset : ScriptableObject
    {
        /// <summary>单条花色配置。</summary>
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("花色（枚举值，与 Suit 枚举一致）。")]
            public Suit suit;

            [Tooltip("显示名称（导入自配表「花色名称」列）。")]
            public string displayName = "";
        }

        /// <summary>花色条目列表（当前配表 4 行，按花色_ID 升序）。</summary>
        [Tooltip("花色条目列表（当前配表 4 行，按花色_ID 升序）。")]
        public List<Entry> entries = new List<Entry>();

        /// <summary>
        /// 轻量校验（OnValidate、导入命令与白盒工厂共用）：条目非空、花色不重复、名称非空。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "花色配置为空：至少需要一个条目。";
                return false;
            }

            var seen = new HashSet<Suit>();
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry == null)
                {
                    error = $"条目 {index} 为 null。";
                    return false;
                }
                if (!seen.Add(entry.suit))
                {
                    error = $"花色 {entry.suit} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.displayName))
                {
                    error = $"花色 {entry.suit} 的显示名称为空。";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 白盒条目（= 配表「花色配置」当前初值：4 行 SUIT_001~004）：
        /// 导入命令创建初始资产与场景重建兜底共用此工厂。数值源为 Cards 的 SuitConfigEntry.CreateFallbackList
        /// （与三线强化门面的中文回落同源，数值只写一处），此处转成资产条目。
        /// </summary>
        public static List<Entry> CreateFallbackEntries()
        {
            var entries = new List<Entry>();
            foreach (var coreEntry in SuitConfigEntry.CreateFallbackList())
            {
                entries.Add(EntryOf(coreEntry.Suit, coreEntry.DisplayName));
            }

            return entries;
        }

        /// <summary>转成无引擎依赖的 Cards 条目（运行时门面的入参）。调用前应先 Validate 拦截非法值。</summary>
        public List<SuitConfigEntry> BuildEntries()
        {
            var result = new List<SuitConfigEntry>();
            foreach (var entry in entries)
            {
                result.Add(new SuitConfigEntry(entry.suit, entry.displayName));
            }

            return result;
        }

        /// <summary>便捷构造：白盒工厂单条条目。</summary>
        private static Entry EntryOf(Suit suit, string displayName)
        {
            return new Entry
            {
                suit = suit,
                displayName = displayName
            };
        }
    }
}
