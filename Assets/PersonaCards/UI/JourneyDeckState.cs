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

        public bool TryPurchase(JourneyDeckAction action, string cardId)
        {
            if (Coins < ShopCost) return false;
            var succeeded = action switch
            {
                JourneyDeckAction.Delete => Delete(cardId),
                JourneyDeckAction.Reforge => ReplaceCard(cardId, card => new PlayingCardInstance(card.Id, NextSuit(card.Suit), card.Rank, card.Enhancement)),
                JourneyDeckAction.Enhance => ReplaceCard(cardId, card => new PlayingCardInstance(card.Id, card.Suit, card.Rank, CardEnhancement.MultBoost)),
                _ => false
            };
            if (succeeded) Coins -= ShopCost;
            return succeeded;
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
