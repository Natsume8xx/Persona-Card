using System;
using System.Collections.Generic;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Cards.Hands;
using PersonaCards.Core;
using PersonaCards.Data;

namespace PersonaCards.UI
{
    /// <summary>
    /// 牌型强化界面会话（UI 重排第二批）：候选 = 配表牌型序（HandTargets，全 11 种含皇家同花顺，网页版只列 10 种已记确认）
    /// 剔除满级/无价；底值文案读 HandTypeCatalog（Lv0 底），确认 = 按当前等级真实价扣款 + 升 1 级。
    /// 初始未选中（网页版「请选择目标」+ 确认禁用）。
    /// </summary>
    public sealed class HandEnhanceScreenSession : IEnhanceListSession
    {
        public const string TitleText = "牌型强化";
        public const string DescriptionText = "选择1种牌型，其原始基础筹码和基础倍率各提升10%。";
        public const string HintText = "请选择目标";

        /// <summary>单个牌型候选：展示所需数据（构建时查表算好，会话内不再查表）。</summary>
        public sealed class HandEntry
        {
            public HandEntry(HandType handType, string displayName, int baseChips, decimal baseMultiplier, int level, int price)
            {
                HandType = handType;
                DisplayName = displayName;
                BaseChips = baseChips;
                BaseMultiplier = baseMultiplier;
                Level = level;
                Price = price;
            }

            public HandType HandType { get; }
            public string DisplayName { get; }
            /// <summary>Lv0 基础筹码（HandTypeCatalog 底值，文案用）。</summary>
            public int BaseChips { get; }
            /// <summary>Lv0 基础倍率（HandTypeCatalog 底值，文案用）。</summary>
            public decimal BaseMultiplier { get; }
            public int Level { get; }
            /// <summary>升到下一级的真实价（8/11/14/17 按表）。</summary>
            public int Price { get; }
        }

        private readonly List<HandEntry> _entries = new List<HandEntry>();
        private readonly EnhancementState _enhancements;
        private int _selectedIndex = -1;

        private HandEnhanceScreenSession(List<HandEntry> entries, EnhancementState enhancements)
        {
            _entries = entries;
            _enhancements = enhancements;
        }

        /// <summary>构建会话：非牌型强化商品/候选全空（全满级或无价，含强化表未注入）→ null（调用方提示后不弹界面）。</summary>
        public static HandEnhanceScreenSession TryCreate(ShopProductEntry product, EnhancementState enhancements)
        {
            if (product == null || enhancements == null) return null;
            if (!string.Equals(product.effectType, ShopState.EffectEnhanceHand, StringComparison.Ordinal)) return null;
            var entries = new List<HandEntry>();
            foreach (var handType in EnhancementConfig.HandTargets())
            {
                var level = enhancements.HandLevelOf(handType);
                if (level >= EnhancementState.HandMaxLevel) continue;
                var price = EnhancementConfig.HandPriceOf(handType, level);
                if (price <= 0) continue;
                var definition = HandTypeCatalog.Get(handType);
                entries.Add(new HandEntry(handType, definition.DisplayName, definition.BaseChips, definition.BaseMultiplier, level, price));
            }

            return entries.Count == 0 ? null : new HandEnhanceScreenSession(entries, enhancements);
        }

        public string Title => TitleText;
        public string Description => DescriptionText;
        public string Hint => HintText;
        public int Count => _entries.Count;
        public int SelectedIndex => _selectedIndex;

        public void Select(int index)
        {
            if (index < 0 || index >= _entries.Count) return;
            _selectedIndex = index;
        }

        public string NameText(int index) => _entries[index].DisplayName;

        /// <summary>底值文案：「基础 48 筹码 × 2 倍率」（Lv0 底，与网页版逐项一致）。</summary>
        public string DetailText(int index)
        {
            var entry = _entries[index];
            return $"基础 {entry.BaseChips} 筹码 × {FormatNumber(entry.BaseMultiplier)} 倍率";
        }

        public string LevelText(int index) => $"Lv.{_entries[index].Level}";

        public string PriceText(int index)
        {
            return index < 0 ? "本次价格：-- 金币" : $"本次价格：{_entries[index].Price} 金币";
        }

        public bool CanConfirm => _selectedIndex >= 0;

        public bool TryConfirm(JourneyDeckState deck)
        {
            if (deck == null || _selectedIndex < 0) return false;
            var entry = _entries[_selectedIndex];
            if (deck.Coins < entry.Price) return false;
            if (!deck.TrySpend(entry.Price)) return false;
            return _enhancements.TryUpgradeHand(entry.HandType);
        }

        /// <summary>数值格式化：整数不带小数尾（2.5 → 「2.5」、1 → 「1」）。</summary>
        internal static string FormatNumber(decimal value)
        {
            return value.ToString("0.##");
        }
    }
}
