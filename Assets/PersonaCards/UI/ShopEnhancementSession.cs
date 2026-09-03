using System;
using System.Collections.Generic;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Core;
using PersonaCards.Data;

namespace PersonaCards.UI
{
    /// <summary>三线强化目标类别（P0-11）：人格牌 / 花色 / 牌型。</summary>
    public enum EnhancementKind
    {
        Persona,
        Suit,
        Hand
    }

    /// <summary>
    /// 单个可强化目标（P0-11）：展示所需数据（名称/等级/价格/升级前后文案）+ 升级所需的结构化标识。
    /// 由工厂方法按线构建（价格与文案在构建时从 EnhancementConfig 查表算好，会话内不再查表）。
    /// </summary>
    public sealed class EnhancementTarget
    {
        private EnhancementTarget(EnhancementKind kind, string key, string displayName, int level, int price,
            Suit suitValue, HandType handValue, string templateIdValue, string currentText, string nextText)
        {
            Kind = kind;
            Key = key;
            DisplayName = displayName;
            Level = level;
            Price = price;
            SuitValue = suitValue;
            HandValue = handValue;
            TemplateIdValue = templateIdValue;
            CurrentText = currentText;
            NextText = nextText;
        }

        public EnhancementKind Kind { get; }
        /// <summary>配表行标识（TemplateId / SUIT_00X / HAND_0X），日志与调试用。</summary>
        public string Key { get; }
        public string DisplayName { get; }
        public int Level { get; }
        /// <summary>升到下一级的价格（8 + 3 × 当前等级）；0 表示不可购买（不进入候选）。</summary>
        public int Price { get; }
        public Suit SuitValue { get; }
        public HandType HandValue { get; }
        public string TemplateIdValue { get; }
        /// <summary>升级前数值文案（Lv0 为「无加成」）。</summary>
        public string CurrentText { get; }
        /// <summary>升级后数值文案。</summary>
        public string NextText { get; }

        /// <summary>花色线目标；suitIndex = 配表序（0=黑桃…），用于 Key 的 SUIT_00X。</summary>
        internal static EnhancementTarget ForSuit(Suit suit, int suitIndex, int level)
        {
            var current = level <= 0
                ? "无加成"
                : $"每张 +{EnhancementConfig.SuitChipsOf(suit, level)}筹码";
            return new EnhancementTarget(EnhancementKind.Suit, $"SUIT_{suitIndex + 1:000}",
                EnhancementConfig.SuitNameOf(suit), level, EnhancementConfig.SuitPriceOf(suit, level),
                suit, HandType.HighCard, null,
                current, $"每张 +{EnhancementConfig.SuitChipsOf(suit, level + 1)}筹码");
        }

        /// <summary>牌型线目标；handIndex = HandTargets 表序（0 = HAND_01），用于 Key 的 HAND_0X。</summary>
        internal static EnhancementTarget ForHand(HandType handType, int handIndex, int level)
        {
            EnhancementConfig.TryGetHandDelta(handType, level, out var currentChips, out var currentMult);
            EnhancementConfig.TryGetHandDelta(handType, level + 1, out var nextChips, out var nextMult);
            var current = level <= 0 ? "无加成" : $"+{currentChips}筹码 ×{currentMult}倍率";
            return new EnhancementTarget(EnhancementKind.Hand, $"HAND_{handIndex + 1:00}",
                EnhancementConfig.HandNameOf(handType), level, EnhancementConfig.HandPriceOf(handType, level),
                Suit.Clubs, handType, null,
                current, $"+{nextChips}筹码 ×{nextMult}倍率");
        }

        /// <summary>人格牌线目标（装备槽非空槽）。</summary>
        internal static EnhancementTarget ForPersona(PersonaCardDefinition definition, int level)
        {
            var increase = EnhancementConfig.PersonaPerLevelIncreaseOf(definition.EffectKind);
            var unit = definition.EffectKind == PersonaEffectKind.AddChips ? "筹码"
                : definition.EffectKind == PersonaEffectKind.MultiplyFinal ? "独立倍率"
                : "倍率";
            var current = level <= 0 ? "无加成" : $"+{increase * level}{unit}";
            return new EnhancementTarget(EnhancementKind.Persona, definition.TemplateId, definition.DisplayName,
                level, EnhancementConfig.PersonaPriceOf(definition.EffectKind, level),
                Suit.Clubs, HandType.HighCard, definition.TemplateId,
                current, $"+{increase * (level + 1)}{unit}");
        }
    }

    /// <summary>
    /// 商店强化选择模式会话（P0-11）：纯 UI 逻辑可单测。点强化服务槽位进入，轮换目标（Prev/Next 双向环绕），
    /// 确认 = TrySpend 真实价格（按目标当前等级动态定价）+ TryUpgrade；失败（金币不足等）无副作用。
    /// 进入前 FlowController 调用 TryCreate 构建候选（满级/无价目标剔除），全空返回 null 表示「无可强化对象」。
    /// </summary>
    public sealed class ShopEnhancementSession
    {
        private readonly List<EnhancementTarget> _targets;
        private readonly EnhancementState _enhancements;
        private int _index;

        private ShopEnhancementSession(List<EnhancementTarget> targets, EnhancementState enhancements)
        {
            _targets = targets;
            _enhancements = enhancements;
        }

        public int Count => _targets.Count;

        /// <summary>当前目标（候选非空保证非 null）。</summary>
        public EnhancementTarget Current => _targets[_index];

        /// <summary>状态文案：「黑桃 Lv2→Lv3 · 费用 14」。</summary>
        public string StatusText => $"{Current.DisplayName} Lv{Current.Level}→Lv{Current.Level + 1} · 费用 {Current.Price}";

        /// <summary>细节文案：「当前：每张 +10筹码 → 升级后：每张 +15筹码」。</summary>
        public string DetailText => $"当前：{Current.CurrentText} → 升级后：{Current.NextText}";

        /// <summary>
        /// 构建选择会话：商品为强化服务才进入分派；非强化效果/强化表缺失（候选价格全 0）/全满级 → null。
        /// </summary>
        public static ShopEnhancementSession TryCreate(ShopProductEntry product,
            PersonaLoadoutState personas, EnhancementState enhancements)
        {
            if (product == null || personas == null || enhancements == null) return null;
            var targets = new List<EnhancementTarget>();
            if (string.Equals(product.effectType, ShopState.EffectEnhancePersona, StringComparison.Ordinal))
            {
                BuildPersonaTargets(targets, personas, enhancements);
            }
            else if (string.Equals(product.effectType, ShopState.EffectEnhanceSuit, StringComparison.Ordinal))
            {
                BuildSuitTargets(targets, enhancements);
            }
            else if (string.Equals(product.effectType, ShopState.EffectEnhanceHand, StringComparison.Ordinal))
            {
                BuildHandTargets(targets, enhancements);
            }
            else
            {
                return null; // 非强化效果（防御：调用方已用 IsEnhancementEffect 拦截）
            }
            return targets.Count == 0 ? null : new ShopEnhancementSession(targets, enhancements);
        }

        /// <summary>轮换目标：模候选数双向环绕（delta 可负）。</summary>
        public void Cycle(int delta)
        {
            _index = ((_index + delta) % _targets.Count + _targets.Count) % _targets.Count;
        }

        /// <summary>确认升级：金币不足/扣款失败拒绝且无副作用；成功 = 扣款 + 升 1 级（升级失败属防御性不可达，仅拒绝）。</summary>
        public bool TryConfirm(JourneyDeckState deck)
        {
            if (deck == null || deck.Coins < Current.Price) return false;
            if (!deck.TrySpend(Current.Price)) return false;
            switch (Current.Kind)
            {
                case EnhancementKind.Persona:
                    return _enhancements.TryUpgradePersona(Current.TemplateIdValue);
                case EnhancementKind.Suit:
                    return _enhancements.TryUpgradeSuit(Current.SuitValue);
                case EnhancementKind.Hand:
                    return _enhancements.TryUpgradeHand(Current.HandValue);
                default:
                    return false;
            }
        }

        /// <summary>人格候选 = 装备槽非空槽（未满级且效果类型有价）。</summary>
        private static void BuildPersonaTargets(List<EnhancementTarget> targets,
            PersonaLoadoutState personas, EnhancementState enhancements)
        {
            foreach (var definition in personas.Slots)
            {
                if (definition == null) continue;
                var level = enhancements.PersonaLevelOf(definition.TemplateId);
                if (level >= EnhancementState.PersonaMaxLevel) continue;
                if (EnhancementConfig.PersonaPriceOf(definition.EffectKind, level) <= 0) continue;
                targets.Add(EnhancementTarget.ForPersona(definition, level));
            }
        }

        /// <summary>花色候选 = 4 花色按配表序（黑桃/红桃/梅花/方块）。</summary>
        private static void BuildSuitTargets(List<EnhancementTarget> targets, EnhancementState enhancements)
        {
            var suits = new[] { Suit.Spades, Suit.Hearts, Suit.Clubs, Suit.Diamonds };
            for (var i = 0; i < suits.Length; i++)
            {
                var suit = suits[i];
                var level = enhancements.SuitLevelOf(suit);
                if (level >= EnhancementState.SuitMaxLevel) continue;
                if (EnhancementConfig.SuitPriceOf(suit, level) <= 0) continue;
                targets.Add(EnhancementTarget.ForSuit(suit, i, level));
            }
        }

        /// <summary>牌型候选 = 配表序（HandTargets 去重保序 = HAND_01 → HAND_11）。</summary>
        private static void BuildHandTargets(List<EnhancementTarget> targets, EnhancementState enhancements)
        {
            var hands = EnhancementConfig.HandTargets();
            for (var i = 0; i < hands.Count; i++)
            {
                var handType = hands[i];
                var level = enhancements.HandLevelOf(handType);
                if (level >= EnhancementState.HandMaxLevel) continue;
                if (EnhancementConfig.HandPriceOf(handType, level) <= 0) continue;
                targets.Add(EnhancementTarget.ForHand(handType, i, level));
            }
        }
    }
}
