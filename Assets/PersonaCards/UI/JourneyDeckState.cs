using System;
using System.Collections.Generic;
using System.Linq;
using PersonaCards.Cards;

namespace PersonaCards.UI
{
    public enum JourneyDeckAction
    {
        Delete,
        Reforge,
        Enhance
    }

    public sealed class JourneyDeckState
    {
        private const int ShopCost = 2;
        private readonly List<PlayingCardInstance> _cards;

        public JourneyDeckState(IEnumerable<PlayingCardInstance> cards, int startingCoins = 3)
        {
            _cards = (cards ?? throw new ArgumentNullException(nameof(cards))).ToList();
            if (_cards.Count == 0) throw new ArgumentException("Journey deck cannot be empty.", nameof(cards));
            if (_cards.Select(card => card.Id).Distinct(StringComparer.Ordinal).Count() != _cards.Count)
                throw new ArgumentException("Journey card ids must be unique.", nameof(cards));
            if (startingCoins < 0) throw new ArgumentOutOfRangeException(nameof(startingCoins));
            Coins = startingCoins;
        }

        public IReadOnlyList<PlayingCardInstance> Cards => _cards.AsReadOnly();
        public int Coins { get; private set; }

        /// <summary>发放金币（P0-6 战斗奖励入账）：负数拒绝，非法调用必须暴露。</summary>
        public void AddCoins(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Coins += amount;
        }

        public bool GrantRewardEnhancement(string cardId)
        {
            return ReplaceCard(cardId, card => new PlayingCardInstance(card.Id, card.Suit, card.Rank, CardEnhancement.ChipBoost));
        }

        /// <summary>按固定商店价（2 金币）购买旧三类服务；成功才扣款。</summary>
        public bool TryPurchase(JourneyDeckAction action, string cardId)
        {
            return TryPurchase(action, cardId, ShopCost);
        }

        /// <summary>按商品价购买旧三类服务（P0-7 商店商品位分派用）：成功才扣款，扣款失败不生效（策划案 10.6）。负数价格拒绝。</summary>
        public bool TryPurchase(JourneyDeckAction action, string cardId, int cost)
        {
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (Coins < cost) return false;
            var succeeded = action switch
            {
                JourneyDeckAction.Delete => Delete(cardId),
                JourneyDeckAction.Reforge => ReplaceCard(cardId, card => new PlayingCardInstance(card.Id, NextSuit(card.Suit), card.Rank, card.Enhancement)),
                JourneyDeckAction.Enhance => ReplaceCard(cardId, card => new PlayingCardInstance(card.Id, card.Suit, card.Rank, CardEnhancement.MultBoost)),
                _ => false
            };
            if (succeeded) Coins -= cost;
            return succeeded;
        }

        /// <summary>按商品价扣款（P0-7 增加卡牌类商品分派用，效果应用成功后调用）：金币不足返回 false 不扣款。负数价格拒绝。</summary>
        public bool TrySpend(int cost)
        {
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (Coins < cost) return false;
            Coins -= cost;
            return true;
        }

        /// <summary>商店购买「增加卡牌」（P0-7）：牌组未含同 id 牌时加入（同一张牌不可重复持有）；返回是否成功。</summary>
        public bool AddCard(PlayingCardInstance card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (FindIndex(card.Id) >= 0) return false;
            _cards.Add(card);
            return true;
        }

        public IReadOnlyList<PlayingCardInstance> CreateBattleDeck()
        {
            return _cards.Select(card => new PlayingCardInstance(card.Id, card.Suit, card.Rank, card.Enhancement)).ToArray();
        }

        private bool Delete(string cardId)
        {
            if (_cards.Count <= PersonaCards.Battle.BattleStateMachine.HandLimit) return false;
            var index = FindIndex(cardId);
            if (index < 0) return false;
            _cards.RemoveAt(index);
            return true;
        }

        private bool ReplaceCard(string cardId, Func<PlayingCardInstance, PlayingCardInstance> replacement)
        {
            var index = FindIndex(cardId);
            if (index < 0) return false;
            _cards[index] = replacement(_cards[index]);
            return true;
        }

        private int FindIndex(string cardId)
        {
            return _cards.FindIndex(card => string.Equals(card.Id, cardId, StringComparison.Ordinal));
        }

        private static Suit NextSuit(Suit suit)
        {
            return suit switch
            {
                Suit.Clubs => Suit.Diamonds,
                Suit.Diamonds => Suit.Hearts,
                Suit.Hearts => Suit.Spades,
                _ => Suit.Clubs
            };
        }
    }
}
