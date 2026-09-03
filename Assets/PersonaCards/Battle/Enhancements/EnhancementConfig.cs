using System.Collections.Generic;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Core;

namespace PersonaCards.Battle.Enhancements
{
    /// <summary>
    /// 三线强化数值门面（P0-11）：查询价格/每级增量/牌型差值等。数据由 UI 层
    /// EnhancementTableBootstrap 注入（Editor 反射加载资产 → EnhancementTablesBuilder 翻译）。
    /// 未注入时回落空表：一切查询返回 0/空，强化效果零值、商店不合成强化池规则。
    /// 价格口径：升到 Lv(N+1) 的价格 = 基础 8 + 每级涨价 3 × 当前等级（8/11/14/17）。
    /// </summary>
    public static class EnhancementConfig
    {
        private static EnhancementTables _tables = EnhancementTables.Empty;

        public static bool HasTables => _tables.HasContent;

        public static void Configure(EnhancementTables tables)
        {
            _tables = tables ?? EnhancementTables.Empty;
        }

        /// <summary>人格牌升到下一级的价格；未知效果类型/满级回落 0（调用方按 0 视为不可购买）。</summary>
        public static int PersonaPriceOf(PersonaEffectKind kind, int currentLevel)
        {
            if (currentLevel < 0 || currentLevel >= EnhancementState.PersonaMaxLevel) return 0;
            if (!_tables.PersonaPerLevelIncrease.ContainsKey(kind)) return 0;
            return _tables.PersonaBasePrice + _tables.PersonaLevelPriceStep * currentLevel;
        }

        /// <summary>人格牌每级数值增量（AddChips 型 10 / AddMultiplier 型 0.3 / MultiplyFinal 型 0.1 暂定口径）。</summary>
        public static decimal PersonaPerLevelIncreaseOf(PersonaEffectKind kind)
        {
            return _tables.PersonaPerLevelIncrease.TryGetValue(kind, out var increase) ? increase : 0m;
        }

        /// <summary>花色每张计分牌的额外筹码（等级 0 → 0；缺行 0）。</summary>
        public static int SuitChipsOf(Suit suit, int level)
        {
            if (level <= 0) return 0;
            return _tables.SuitChips.TryGetValue(suit, out var perLevel) && perLevel.Length == 4
                ? perLevel[level - 1]
                : 0;
        }

        /// <summary>花色升到下一级的价格；满级/缺行 0。</summary>
        public static int SuitPriceOf(Suit suit, int currentLevel)
        {
            if (currentLevel < 0 || currentLevel >= EnhancementState.SuitMaxLevel) return 0;
            return _tables.SuitPrices.TryGetValue(suit, out var perLevel) && perLevel.Length == 4
                ? perLevel[currentLevel]
                : 0;
        }

        /// <summary>花色显示名；缺行回落中文常量（配表名称权威，此处仅防御）。</summary>
        public static string SuitNameOf(Suit suit)
        {
            if (_tables.SuitNames.TryGetValue(suit, out var name) && !string.IsNullOrEmpty(name)) return name;
            switch (suit)
            {
                case Suit.Spades: return "黑桃";
                case Suit.Hearts: return "红桃";
                case Suit.Clubs: return "梅花";
                case Suit.Diamonds: return "方块";
                default: return suit.ToString();
            }
        }

        /// <summary>牌型等级 N 的增量（= 表内绝对底值 − HandTypeCatalog Lv0 底值）；等级 0/缺行返回 false。</summary>
        public static bool TryGetHandDelta(HandType handType, int level, out int chipDelta, out decimal multDelta)
        {
            chipDelta = 0;
            multDelta = 0m;
            if (level <= 0) return false;
            if (!_tables.HandChipDeltas.TryGetValue(handType, out var chips) || chips.Length != 4) return false;
            if (!_tables.HandMultDeltas.TryGetValue(handType, out var mults) || mults.Length != 4) return false;
            chipDelta = chips[level - 1];
            multDelta = mults[level - 1];
            return true;
        }

        /// <summary>牌型升到下一级的价格；满级/缺行 0。</summary>
        public static int HandPriceOf(HandType handType, int currentLevel)
        {
            if (currentLevel < 0 || currentLevel >= EnhancementState.HandMaxLevel) return 0;
            return _tables.HandPrices.TryGetValue(handType, out var perLevel) && perLevel.Length == 4
                ? perLevel[currentLevel]
                : 0;
        }

        /// <summary>牌型显示名；缺行回落枚举名（仅防御，配表 11 行正常必填）。</summary>
        public static string HandNameOf(HandType handType)
        {
            return _tables.HandNames.TryGetValue(handType, out var name) && !string.IsNullOrEmpty(name)
                ? name
                : handType.ToString();
        }

        /// <summary>可强化的牌型目标（去重保序；空表回落空列表）。</summary>
        public static IReadOnlyList<HandType> HandTargets()
        {
            return _tables.HandTargets;
        }
    }
}
