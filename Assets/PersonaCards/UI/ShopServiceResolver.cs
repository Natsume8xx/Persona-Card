using System;
using PersonaCards.Data;

namespace PersonaCards.UI
{
    /// <summary>商店服务商品类别（UI 重排第二批）：购买后跳转对应强化界面的路由键。</summary>
    public enum ShopServiceKind
    {
        /// <summary>非服务/未知（防御回落，不上架不路由）。</summary>
        None,
        /// <summary>筹码强化（强化卡牌 + 基础筹码）：选牌弹窗，+5 筹码。</summary>
        CardChip,
        /// <summary>金币强化（强化卡牌 + 金币）：选牌弹窗，胜利结算按牌库张数入账。</summary>
        CardCoin,
        /// <summary>倍率强化（强化卡牌 + 基础倍率）：选牌弹窗，+0.5 倍率。</summary>
        CardMult,
        /// <summary>独立乘区强化（强化卡牌 + 独立倍率）：选牌弹窗，最终 ×1.03。</summary>
        CardIndependentMult,
        /// <summary>花色强化：选牌弹窗，选中牌花色线升 1 级（SuitUp 整线）。</summary>
        CardSuit,
        /// <summary>移除卡牌：选牌弹窗，删除选中牌。</summary>
        CardRemove,
        /// <summary>牌型强化：牌型强化界面（三线之一）。</summary>
        Hand,
        /// <summary>人格主词条强化：主词条强化界面（三线之一）。</summary>
        Persona
    }

    /// <summary>
    /// 商店服务商品分派（UI 重排第二批）：纯静态解析器，按 effectType + effectParam1 原文把
    /// 8 种服务商品（SHOP_SERVICE_001~008）映射到 ShopServiceKind，供购买路由选择对应界面。
    /// 解析失败/非服务类型 → None（防御，调用方提示后不弹界面）。
    /// </summary>
    public static class ShopServiceResolver
    {
        /// <summary>效果参数1原文常量（配表「效果参数1」列，混写原文直接比对）。</summary>
        public const string ParamBaseChips = "基础筹码";
        public const string ParamCoins = "金币";
        public const string ParamBaseMult = "基础倍率";
        public const string ParamIndependentMult = "独立倍率";

        /// <summary>商品 → 服务类别分派；null / 非服务效果 / 未知参数 → None。</summary>
        public static ShopServiceKind Resolve(ShopProductEntry product)
        {
            if (product == null || string.IsNullOrEmpty(product.effectType)) return ShopServiceKind.None;
            if (string.Equals(product.effectType, ShopState.EffectEnhanceCard, StringComparison.Ordinal))
            {
                if (string.Equals(product.effectParam1, ParamBaseChips, StringComparison.Ordinal)) return ShopServiceKind.CardChip;
                if (string.Equals(product.effectParam1, ParamCoins, StringComparison.Ordinal)) return ShopServiceKind.CardCoin;
                if (string.Equals(product.effectParam1, ParamBaseMult, StringComparison.Ordinal)) return ShopServiceKind.CardMult;
                if (string.Equals(product.effectParam1, ParamIndependentMult, StringComparison.Ordinal)) return ShopServiceKind.CardIndependentMult;
                return ShopServiceKind.None;
            }
            if (string.Equals(product.effectType, ShopState.EffectEnhanceSuit, StringComparison.Ordinal)) return ShopServiceKind.CardSuit;
            if (string.Equals(product.effectType, ShopState.EffectRemoveCard, StringComparison.Ordinal)) return ShopServiceKind.CardRemove;
            if (string.Equals(product.effectType, ShopState.EffectEnhanceHand, StringComparison.Ordinal)) return ShopServiceKind.Hand;
            if (string.Equals(product.effectType, ShopState.EffectEnhancePersona, StringComparison.Ordinal)) return ShopServiceKind.Persona;
            return ShopServiceKind.None;
        }

        /// <summary>是否为选牌弹窗类服务（6 种单卡类：筹码/金币/倍率/独立乘区/花色/移除）。</summary>
        public static bool IsCardPickKind(ShopServiceKind kind)
        {
            return kind == ShopServiceKind.CardChip || kind == ShopServiceKind.CardCoin
                || kind == ShopServiceKind.CardMult || kind == ShopServiceKind.CardIndependentMult
                || kind == ShopServiceKind.CardSuit || kind == ShopServiceKind.CardRemove;
        }
    }
}
