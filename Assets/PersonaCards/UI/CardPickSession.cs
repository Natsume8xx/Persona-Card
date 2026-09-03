using System;
using System.Collections.Generic;
using System.Linq;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Cards;

namespace PersonaCards.UI
{
    /// <summary>选牌弹窗排序模式（网页版「大小 / 花色」两互斥按钮）。</summary>
    public enum CardPickSortMode
    {
        /// <summary>大小：点数升序，同点数按花色序（默认）。</summary>
        ByRank,
        /// <summary>花色：花色序，同花色按点数升序。</summary>
        BySuit
    }

    /// <summary>
    /// 选牌弹窗会话（UI 重排第二批）：6 种单卡类服务共用（筹码/金币/倍率/独立乘区/花色/移除）。
    /// 展示 = 牌组全部牌（两式排序切换）；可选性规则随服务种类不同；确认 = 真实价扣款 + 应用，失败无副作用。
    /// </summary>
    public sealed class CardPickSession
    {
        public const string TitleText = "选择商品作用的卡牌";

        private readonly ShopServiceKind _kind;
        private readonly int _productPrice;
        private readonly EnhancementState _enhancements;
        private readonly List<PlayingCardInstance> _cards;
        private CardPickSortMode _sortMode = CardPickSortMode.ByRank;
        private string _selectedCardId;

        private CardPickSession(ShopServiceKind kind, int productPrice, EnhancementState enhancements,
            IEnumerable<PlayingCardInstance> cards)
        {
            _kind = kind;
            _productPrice = productPrice;
            _enhancements = enhancements;
            _cards = cards.ToList();
            ApplySort();
        }

        /// <summary>构建会话：非选牌类服务/可选数为 0（无作用目标）→ null（调用方提示后不弹界面）。</summary>
        public static CardPickSession TryCreate(ShopServiceKind kind, int productPrice,
            EnhancementState enhancements, JourneyDeckState deck)
        {
            if (deck == null || enhancements == null) return null;
            if (!ShopServiceResolver.IsCardPickKind(kind)) return null;
            var session = new CardPickSession(kind, productPrice, enhancements, deck.Cards);
            return session.EligibleCount == 0 ? null : session;
        }

        public ShopServiceKind Kind => _kind;

        /// <summary>展示顺序（大小/花色）；切换重排不改变选中。</summary>
        public CardPickSortMode SortMode => _sortMode;

        /// <summary>当前排序下的牌列表（快照，重排仅调顺序）。</summary>
        public IReadOnlyList<PlayingCardInstance> Cards => _cards.AsReadOnly();

        public string SelectedCardId => _selectedCardId;

        public PlayingCardInstance SelectedCard => _selectedCardId == null
            ? null
            : _cards.FirstOrDefault(card => string.Equals(card.Id, _selectedCardId, StringComparison.Ordinal));

        /// <summary>服务名（商品信息栏标题，如「倍率强化」）。</summary>
        public string ServiceName
        {
            get
            {
                switch (_kind)
                {
                    case ShopServiceKind.CardChip: return "筹码强化";
                    case ShopServiceKind.CardCoin: return "金币强化";
                    case ShopServiceKind.CardMult: return "倍率强化";
                    case ShopServiceKind.CardIndependentMult: return "独立乘区强化";
                    case ShopServiceKind.CardSuit: return "花色强化";
                    case ShopServiceKind.CardRemove: return "移除卡牌";
                    default: return "强化";
                }
            }
        }

        /// <summary>
        /// 可选性规则（UI 重排第二批已拍板）：4 种牌级增强 → 牌无增强（单牌单增强槽不覆盖）；
        /// 花色 → 该花色线未满级；移除 → 牌组高于下限（所有牌同规则，故按牌组张数判断）。
        /// </summary>
        public bool IsEligible(string cardId)
        {
            var card = _cards.FirstOrDefault(c => string.Equals(c.Id, cardId, StringComparison.Ordinal));
            if (card == null) return false;
            switch (_kind)
            {
                case ShopServiceKind.CardChip:
                case ShopServiceKind.CardCoin:
                case ShopServiceKind.CardMult:
                case ShopServiceKind.CardIndependentMult:
                    return card.Enhancement == CardEnhancement.None;
                case ShopServiceKind.CardSuit:
                    return _enhancements.SuitLevelOf(card.Suit) < EnhancementState.SuitMaxLevel;
                case ShopServiceKind.CardRemove:
                    return _cards.Count > PersonaCards.Battle.BattleStateMachine.HandLimit;
                default:
                    return false;
            }
        }

        /// <summary>可选牌数（统计文案用）。</summary>
        public int EligibleCount => _cards.Count(card => IsEligible(card.Id));

        /// <summary>统计文案：「总数 52 / 可选 52」。</summary>
        public string StatsText => $"总数 {_cards.Count} / 可选 {EligibleCount}";

        /// <summary>选中目标牌：不可选/未知 id 拒绝（无副作用），选中成功替换。</summary>
        public bool Select(string cardId)
        {
            if (!IsEligible(cardId)) return false;
            _selectedCardId = cardId;
            return true;
        }

        public bool CanConfirm => _selectedCardId != null;

        /// <summary>确认价格：花色服务按该花色线当前等级动态定价（8/11/14/17），其余按商品价；未选中为 0。</summary>
        public int ConfirmPrice
        {
            get
            {
                var card = SelectedCard;
                if (card == null) return 0;
                if (_kind == ShopServiceKind.CardSuit)
                    return EnhancementConfig.SuitPriceOf(card.Suit, _enhancements.SuitLevelOf(card.Suit));
                return _productPrice;
            }
        }

        /// <summary>商品信息栏描述文案（暗金小字；花色服务随选中动态显示升级前后与费用）。</summary>
        public string ServiceDetailText
        {
            get
            {
                switch (_kind)
                {
                    case ShopServiceKind.CardChip:
                        return "选择1张牌，基础筹码+5。 当前强化：无。";
                    case ShopServiceKind.CardCoin:
                        return "选择1张牌，胜利结算时每张金币强化牌+2金币。 当前强化：无。";
                    case ShopServiceKind.CardMult:
                        return "选择1张牌，基础倍率+0.5。 当前强化：无。";
                    case ShopServiceKind.CardIndependentMult:
                        return "选择1张牌，最终得分×1.03。 当前强化：无。";
                    case ShopServiceKind.CardSuit:
                    {
                        var card = SelectedCard;
                        if (card == null) return "选择1张牌，其花色强化升1级。";
                        var level = _enhancements.SuitLevelOf(card.Suit);
                        return $"{EnhancementConfig.SuitNameOf(card.Suit)} Lv{level}→Lv{level + 1} · 费用 {ConfirmPrice}";
                    }
                    case ShopServiceKind.CardRemove:
                        return $"选择1张牌，从牌组中移除。 当前牌组 {_cards.Count} 张。";
                    default:
                        return "";
                }
            }
        }

        /// <summary>切换排序（大小 ↔ 花色），不改变选中。</summary>
        public void ToggleSort()
        {
            _sortMode = _sortMode == CardPickSortMode.ByRank ? CardPickSortMode.BySuit : CardPickSortMode.ByRank;
            ApplySort();
        }

        /// <summary>确认购买：扣真实价 + 应用（移除 / 花色升线 / 四种牌级增强）；金币不足/失败无副作用。</summary>
        public bool TryConfirm(JourneyDeckState deck)
        {
            if (deck == null || _selectedCardId == null) return false;
            var card = SelectedCard;
            if (card == null) return false;
            switch (_kind)
            {
                case ShopServiceKind.CardRemove:
                    return deck.TryPurchase(JourneyDeckAction.Delete, card.Id, ConfirmPrice);
                case ShopServiceKind.CardSuit:
                {
                    if (deck.Coins < ConfirmPrice) return false;
                    if (!deck.TrySpend(ConfirmPrice)) return false;
                    return _enhancements.TryUpgradeSuit(card.Suit);
                }
                default:
                {
                    var enhancement = EnhancementOf(_kind);
                    if (!enhancement.HasValue) return false;
                    if (deck.Coins < ConfirmPrice) return false;
                    if (!deck.TrySpend(ConfirmPrice)) return false;
                    return deck.ApplyCardEnhancement(card.Id, enhancement.Value);
                }
            }
        }

        /// <summary>服务种类 → 牌级增强（花色/移除无牌级增强，返回 null）。</summary>
        private static CardEnhancement? EnhancementOf(ShopServiceKind kind)
        {
            switch (kind)
            {
                case ShopServiceKind.CardChip: return CardEnhancement.ChipPlus;
                case ShopServiceKind.CardCoin: return CardEnhancement.CoinBonus;
                case ShopServiceKind.CardMult: return CardEnhancement.MultPlus;
                case ShopServiceKind.CardIndependentMult: return CardEnhancement.IndependentMult;
                default: return null;
            }
        }

        private void ApplySort()
        {
            if (_sortMode == CardPickSortMode.ByRank)
            {
                _cards.Sort((a, b) =>
                {
                    var rank = a.Rank.CompareTo(b.Rank);
                    return rank != 0 ? rank : a.Suit.CompareTo(b.Suit);
                });
            }
            else
            {
                _cards.Sort((a, b) =>
                {
                    var suit = a.Suit.CompareTo(b.Suit);
                    return suit != 0 ? suit : a.Rank.CompareTo(b.Rank);
                });
            }
        }
    }
}
