using System;
using System.Collections.Generic;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Battle.Personas;
using PersonaCards.Data;

namespace PersonaCards.UI
{
    /// <summary>
    /// 人格主词条强化界面会话（UI 重排第二批）：候选 = 装备槽非空人格牌（网页版 7 张示意，程序按装备 4 张已记确认）
    /// 剔除满级/无价；效果文案按主词条类型拼装，确认 = 按当前等级真实价扣款 + 升 1 级。初始未选中。
    /// </summary>
    public sealed class PersonaMainAttrSession : IEnhanceListSession
    {
        public const string TitleText = "人格主词条强化";
        public const string DescriptionText = "选择1张人格牌，按主词条类型强化：筹码+10、倍率+0.3或独立倍率+10%。";

        /// <summary>单个人格候选：展示所需数据（构建时查表算好，会话内不再查表）。</summary>
        public sealed class PersonaEntry
        {
            public PersonaEntry(string templateId, string displayName, PersonaEffectKind effectKind, decimal effectValue, int level, int price)
            {
                TemplateId = templateId;
                DisplayName = displayName;
                EffectKind = effectKind;
                EffectValue = effectValue;
                Level = level;
                Price = price;
            }

            public string TemplateId { get; }
            public string DisplayName { get; }
            public PersonaEffectKind EffectKind { get; }
            /// <summary>主词条原始数值；≤0 视为无词条（文案回落「类型：人格主词条」）。</summary>
            public decimal EffectValue { get; }
            public int Level { get; }
            /// <summary>升到下一级的真实价（8/11/14/17 按表）。</summary>
            public int Price { get; }
        }

        private readonly List<PersonaEntry> _entries = new List<PersonaEntry>();
        private readonly EnhancementState _enhancements;
        private int _selectedIndex = -1;

        private PersonaMainAttrSession(List<PersonaEntry> entries, EnhancementState enhancements)
        {
            _entries = entries;
            _enhancements = enhancements;
        }

        /// <summary>构建会话：非人格强化商品/候选全空（全满级或无价，含强化表未注入）→ null（调用方提示后不弹界面）。</summary>
        public static PersonaMainAttrSession TryCreate(ShopProductEntry product,
            PersonaLoadoutState personas, EnhancementState enhancements)
        {
            if (product == null || personas == null || enhancements == null) return null;
            if (!string.Equals(product.effectType, ShopState.EffectEnhancePersona, StringComparison.Ordinal)) return null;
            var entries = new List<PersonaEntry>();
            foreach (var definition in personas.Slots)
            {
                if (definition == null) continue;
                var level = enhancements.PersonaLevelOf(definition.TemplateId);
                if (level >= EnhancementState.PersonaMaxLevel) continue;
                var price = EnhancementConfig.PersonaPriceOf(definition.EffectKind, level);
                if (price <= 0) continue;
                entries.Add(new PersonaEntry(definition.TemplateId, definition.DisplayName,
                    definition.EffectKind, definition.EffectValue, level, price));
            }

            return entries.Count == 0 ? null : new PersonaMainAttrSession(entries, enhancements);
        }

        public string Title => TitleText;
        public string Description => DescriptionText;

        /// <summary>无独立提示：左下显示价格（网页版「本次价格：8 金币」）。</summary>
        public string Hint => "";

        public int Count => _entries.Count;
        public int SelectedIndex => _selectedIndex;

        public void Select(int index)
        {
            if (index < 0 || index >= _entries.Count) return;
            _selectedIndex = index;
        }

        public string NameText(int index) => _entries[index].DisplayName;

        /// <summary>效果/类型描述（暗金小字）：有词条按类型给单位，无词条回落「类型：人格主词条」。</summary>
        public string DetailText(int index)
        {
            var entry = _entries[index];
            if (entry.EffectValue <= 0m) return "类型：人格主词条";
            switch (entry.EffectKind)
            {
                case PersonaEffectKind.AddChips:
                    return $"效果：+{HandEnhanceScreenSession.FormatNumber(entry.EffectValue)} 筹码";
                case PersonaEffectKind.AddMultiplier:
                    return $"效果：+{HandEnhanceScreenSession.FormatNumber(entry.EffectValue)} 倍率";
                case PersonaEffectKind.MultiplyFinal:
                    return $"效果：+{HandEnhanceScreenSession.FormatNumber(entry.EffectValue * 100m)}% 独立倍率";
                default:
                    return "类型：人格主词条";
            }
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
            return _enhancements.TryUpgradePersona(entry.TemplateId);
        }
    }
}
