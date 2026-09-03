using System.Collections.Generic;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Core;

namespace PersonaCards.Battle.Enhancements
{
    /// <summary>
    /// 三线强化数值容器（P0-11）：由 UI 层 EnhancementTablesBuilder 从配表资产翻译而来，
    /// Battle 程序集 noEngineReferences 不能直接消费 Data 资产条目，故在此以纯 C# 形态闭环。
    /// 字段由 Builder 填充后视为只读；空表（HasContent=false）时一切查询回落 0/空，强化零效果。
    /// 数组一律长度 4，索引 = 等级 − 1（Lv1~Lv4）。
    /// </summary>
    public sealed class EnhancementTables
    {
        public static readonly EnhancementTables Empty = new EnhancementTables();

        /// <summary>人格牌每级增量（按效果类型）：筹码型 +10、倍率型 +0.3、独立倍率型 +0.1（暂定口径，代策划确认）。</summary>
        public readonly Dictionary<PersonaEffectKind, decimal> PersonaPerLevelIncrease =
            new Dictionary<PersonaEffectKind, decimal>();

        /// <summary>人格牌强化基础价格（表内 basePrice，当前 8）。</summary>
        public int PersonaBasePrice;

        /// <summary>人格牌强化每级涨价（表内 levelPriceStep，当前 3）。</summary>
        public int PersonaLevelPriceStep;

        /// <summary>花色每级额外筹码（每张计分牌），索引 = 等级 − 1。</summary>
        public readonly Dictionary<Suit, int[]> SuitChips = new Dictionary<Suit, int[]>();

        /// <summary>花色每级价格（升到该级的价格），索引 = 等级 − 1。</summary>
        public readonly Dictionary<Suit, int[]> SuitPrices = new Dictionary<Suit, int[]>();

        /// <summary>花色显示名（如「黑桃」）。</summary>
        public readonly Dictionary<Suit, string> SuitNames = new Dictionary<Suit, string>();

        /// <summary>牌型每级筹码增量（= HandUp 表绝对底值 − HandTypeCatalog Lv0 底值），索引 = 等级 − 1。</summary>
        public readonly Dictionary<HandType, int[]> HandChipDeltas = new Dictionary<HandType, int[]>();

        /// <summary>牌型每级倍率增量（同上差值，decimal 已四舍五入到 6 位抹浮点尾巴）。</summary>
        public readonly Dictionary<HandType, decimal[]> HandMultDeltas = new Dictionary<HandType, decimal[]>();

        /// <summary>牌型每级价格，索引 = 等级 − 1。</summary>
        public readonly Dictionary<HandType, int[]> HandPrices = new Dictionary<HandType, int[]>();

        /// <summary>牌型显示名（如「皇家同花顺」）。</summary>
        public readonly Dictionary<HandType, string> HandNames = new Dictionary<HandType, string>();

        /// <summary>可强化的牌型目标，按配表出现顺序去重（商店选择模式轮换顺序）。</summary>
        public readonly List<HandType> HandTargets = new List<HandType>();

        /// <summary>是否含有任何配表内容（Bootstrap 未注入 / play build 无资产 → false，强化服务不上架、效果零值）。</summary>
        public bool HasContent =>
            PersonaPerLevelIncrease.Count > 0 || SuitChips.Count > 0 || HandChipDeltas.Count > 0;
    }
}
