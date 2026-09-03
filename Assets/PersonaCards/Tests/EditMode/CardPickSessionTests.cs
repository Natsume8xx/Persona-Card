using System.Linq;
using NUnit.Framework;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Cards;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    // UI 重排第二批：选牌弹窗会话——6 种单卡类服务的排序/可选性/动态价/确认语义。
    public sealed class CardPickSessionTests
    {
        private static PlayingCardInstance Card(string id, Suit suit, Rank rank, CardEnhancement enhancement = CardEnhancement.None)
        {
            return new PlayingCardInstance(id, suit, rank, enhancement);
        }

        private static JourneyDeckState Deck(int coins, params PlayingCardInstance[] cards)
        {
            return new JourneyDeckState(cards, coins);
        }

        /// <summary>小牌组：K♠、2♣、A♥、2♦（4 张，足够覆盖两式排序）。</summary>
        private static JourneyDeckState SmallDeck()
        {
            return Deck(100,
                Card("ks", Suit.Spades, Rank.King),
                Card("c2", Suit.Clubs, Rank.Two),
                Card("ha", Suit.Hearts, Rank.Ace),
                Card("d2", Suit.Diamonds, Rank.Two));
        }

        /// <summary>标准花色价格表（8/11/14/17，黑桃 5/10/15/20），花色服务动态价用。</summary>
        private static EnhancementTables SuitTables()
        {
            var tables = new EnhancementTables();
            var prices = new[] { 8, 11, 14, 17 };
            var chips = new[] { 5, 10, 15, 20 };
            tables.SuitPrices[Suit.Spades] = prices;
            tables.SuitPrices[Suit.Hearts] = prices;
            tables.SuitPrices[Suit.Clubs] = prices;
            tables.SuitPrices[Suit.Diamonds] = prices;
            tables.SuitChips[Suit.Spades] = chips;
            tables.SuitChips[Suit.Hearts] = chips;
            tables.SuitChips[Suit.Clubs] = chips;
            tables.SuitChips[Suit.Diamonds] = chips;
            tables.SuitNames[Suit.Spades] = "黑桃";
            tables.SuitNames[Suit.Hearts] = "红桃";
            tables.SuitNames[Suit.Clubs] = "梅花";
            tables.SuitNames[Suit.Diamonds] = "方块";
            return tables;
        }

        // —— 排序 ——

        [Test]
        public void SortByRankOrdersByRankThenSuitAndToggleSwitchesToSuit()
        {
            var session = CardPickSession.TryCreate(ShopServiceKind.CardChip, 5, new EnhancementState(), SmallDeck());

            Assert.That(session.SortMode, Is.EqualTo(CardPickSortMode.ByRank));
            Assert.That(session.Cards.Select(card => card.Id),
                Is.EqualTo(new[] { "c2", "d2", "ks", "ha" })); // 点数升序，同点 2 按花色序（♣ 先于 ♦），K 先于 A

            session.ToggleSort();
            Assert.That(session.SortMode, Is.EqualTo(CardPickSortMode.BySuit));
            Assert.That(session.Cards.Select(card => card.Id),
                Is.EqualTo(new[] { "c2", "d2", "ha", "ks" })); // 花色序（♣♦♥♠），同花色点数升序

            session.ToggleSort(); // 再切回大小
            Assert.That(session.SortMode, Is.EqualTo(CardPickSortMode.ByRank));
        }

        // —— 可选性规则 ——

        [Test]
        public void EligibilityRulesDifferPerServiceKind()
        {
            var deck = Deck(100,
                Card("a", Suit.Spades, Rank.Ace),
                Card("b", Suit.Hearts, Rank.King, CardEnhancement.ChipBoost));

            // 牌级增强：无增强才可选（单牌单增强槽不覆盖）
            var chip = CardPickSession.TryCreate(ShopServiceKind.CardChip, 5, new EnhancementState(), deck);
            Assert.That(chip.IsEligible("a"), Is.True);
            Assert.That(chip.IsEligible("b"), Is.False);
            Assert.That(chip.IsEligible("未知"), Is.False);
            Assert.That(chip.EligibleCount, Is.EqualTo(1));

            // 花色：该花色线未满级才可选（黑桃满级不可选，红桃 Lv0 可选）
            var enhancements = new EnhancementState();
            enhancements.SetSuitLevel(Suit.Spades, 4);
            var suit = CardPickSession.TryCreate(ShopServiceKind.CardSuit, 8, enhancements, deck);
            Assert.That(suit.IsEligible("a"), Is.False);
            Assert.That(suit.IsEligible("b"), Is.True);
            Assert.That(suit.EligibleCount, Is.EqualTo(1));

            // 移除：牌组高于下限（8 张）时全部可选
            var nine = Enumerable.Range(0, 9).Select(i => Card("r" + i, Suit.Clubs, Rank.Four)).ToArray();
            var remove = CardPickSession.TryCreate(ShopServiceKind.CardRemove, 5, new EnhancementState(), Deck(100, nine));
            Assert.That(remove, Is.Not.Null);
            Assert.That(remove.IsEligible("r0"), Is.True);
            Assert.That(remove.EligibleCount, Is.EqualTo(9));
        }

        // —— 构建回落 ——

        [Test]
        public void TryCreateReturnsNullForNonCardPickKindsAndEmptyEligibility()
        {
            Assert.That(CardPickSession.TryCreate(ShopServiceKind.Hand, 8, new EnhancementState(), SmallDeck()), Is.Null);
            Assert.That(CardPickSession.TryCreate(ShopServiceKind.Persona, 8, new EnhancementState(), SmallDeck()), Is.Null);
            Assert.That(CardPickSession.TryCreate(ShopServiceKind.None, 8, new EnhancementState(), SmallDeck()), Is.Null);
            Assert.That(CardPickSession.TryCreate(ShopServiceKind.CardChip, 5, new EnhancementState(), null), Is.Null);
            Assert.That(CardPickSession.TryCreate(ShopServiceKind.CardChip, 5, null, SmallDeck()), Is.Null);

            // 牌级增强：全牌已有增强 → 可选 0 → null
            var allEnhanced = Deck(100,
                Card("a", Suit.Spades, Rank.Ace, CardEnhancement.ChipBoost),
                Card("b", Suit.Hearts, Rank.King, CardEnhancement.MultBoost));
            Assert.That(CardPickSession.TryCreate(ShopServiceKind.CardChip, 5, new EnhancementState(), allEnhanced), Is.Null);

            // 花色：四花色全满级 → null
            var enhancements = new EnhancementState();
            enhancements.SetSuitLevel(Suit.Spades, 4);
            enhancements.SetSuitLevel(Suit.Hearts, 4);
            enhancements.SetSuitLevel(Suit.Clubs, 4);
            enhancements.SetSuitLevel(Suit.Diamonds, 4);
            Assert.That(CardPickSession.TryCreate(ShopServiceKind.CardSuit, 8, enhancements, SmallDeck()), Is.Null);

            // 移除：牌组恰为下限 8 张 → null
            var eight = Enumerable.Range(0, 8).Select(i => Card("e" + i, Suit.Clubs, Rank.Four)).ToArray();
            Assert.That(CardPickSession.TryCreate(ShopServiceKind.CardRemove, 5, new EnhancementState(), Deck(100, eight)), Is.Null);
        }

        // —— 选中 / 统计 / 价格 ——

        [Test]
        public void StatsSelectionAndConfirmPrice()
        {
            var session = CardPickSession.TryCreate(ShopServiceKind.CardChip, 5, new EnhancementState(), SmallDeck());

            Assert.That(session.StatsText, Is.EqualTo("总数 4 / 可选 4"));
            Assert.That(session.CanConfirm, Is.False);
            Assert.That(session.ServiceName, Is.EqualTo("筹码强化"));
            Assert.That(session.ServiceDetailText, Is.EqualTo("选择1张牌，基础筹码+5。 当前强化：无。"));

            Assert.That(session.Select("未知"), Is.False);
            Assert.That(session.CanConfirm, Is.False);
            Assert.That(session.Select("ks"), Is.True);
            Assert.That(session.SelectedCardId, Is.EqualTo("ks"));
            Assert.That(session.SelectedCard.Id, Is.EqualTo("ks"));
            Assert.That(session.CanConfirm, Is.True);
            Assert.That(session.ConfirmPrice, Is.EqualTo(5)); // 牌级增强按商品价

            // 切换排序不改变选中
            session.ToggleSort();
            Assert.That(session.SelectedCardId, Is.EqualTo("ks"));
        }

        [Test]
        public void SuitServiceUsesDynamicPriceAndDetailText()
        {
            EnhancementConfig.Configure(SuitTables());
            try
            {
                var enhancements = new EnhancementState();
                enhancements.SetSuitLevel(Suit.Spades, 1); // 黑桃 Lv1 → 升 Lv2 价 11
                var session = CardPickSession.TryCreate(ShopServiceKind.CardSuit, 8, enhancements, SmallDeck());

                Assert.That(session.ServiceDetailText, Is.EqualTo("选择1张牌，其花色强化升1级。")); // 未选中引导文案
                Assert.That(session.Select("ks"), Is.True); // 黑桃 K
                Assert.That(session.ConfirmPrice, Is.EqualTo(11)); // 动态价 ≠ 商品价 8
                Assert.That(session.ServiceDetailText, Is.EqualTo("黑桃 Lv1→Lv2 · 费用 11"));
            }
            finally
            {
                EnhancementConfig.Configure(EnhancementTables.Empty);
            }
        }

        // —— 确认五路径 ——

        [Test]
        public void TryConfirmAppliesCardEnhancementsAndChargesProductPrice()
        {
            var cases = new[]
            {
                new { Kind = ShopServiceKind.CardChip, Expected = CardEnhancement.ChipPlus },
                new { Kind = ShopServiceKind.CardCoin, Expected = CardEnhancement.CoinBonus },
                new { Kind = ShopServiceKind.CardMult, Expected = CardEnhancement.MultPlus },
                new { Kind = ShopServiceKind.CardIndependentMult, Expected = CardEnhancement.IndependentMult }
            };
            foreach (var item in cases)
            {
                var deck = SmallDeck();
                var session = CardPickSession.TryCreate(item.Kind, 6, new EnhancementState(), deck);
                Assert.That(session.Select("ha"), Is.True);
                Assert.That(session.TryConfirm(deck), Is.True);
                Assert.That(deck.Coins, Is.EqualTo(94)); // 扣商品价 6
                Assert.That(deck.Cards.Single(card => card.Id == "ha").Enhancement, Is.EqualTo(item.Expected));
            }
        }

        [Test]
        public void TryConfirmSuitUpgradesSuitLineWithDynamicPrice()
        {
            EnhancementConfig.Configure(SuitTables());
            try
            {
                var enhancements = new EnhancementState();
                enhancements.SetSuitLevel(Suit.Spades, 1);
                var deck = SmallDeck();
                var session = CardPickSession.TryCreate(ShopServiceKind.CardSuit, 8, enhancements, deck);

                Assert.That(session.Select("ks"), Is.True);
                Assert.That(session.TryConfirm(deck), Is.True);
                Assert.That(deck.Coins, Is.EqualTo(89)); // 扣动态价 11，非商品价 8
                Assert.That(enhancements.SuitLevelOf(Suit.Spades), Is.EqualTo(2));
            }
            finally
            {
                EnhancementConfig.Configure(EnhancementTables.Empty);
            }
        }

        [Test]
        public void TryConfirmRemoveDeletesSelectedCard()
        {
            var nine = Enumerable.Range(0, 9).Select(i => Card("r" + i, Suit.Clubs, Rank.Four)).ToArray();
            var deck = Deck(100, nine);
            var session = CardPickSession.TryCreate(ShopServiceKind.CardRemove, 5, new EnhancementState(), deck);

            Assert.That(session.Select("r0"), Is.True);
            Assert.That(session.TryConfirm(deck), Is.True);
            Assert.That(deck.Coins, Is.EqualTo(95));
            Assert.That(deck.Cards.Count, Is.EqualTo(8));
            Assert.That(deck.Cards.Any(card => card.Id == "r0"), Is.False);
        }

        [Test]
        public void TryConfirmRejectsInsufficientCoinsWithoutSideEffects()
        {
            // 牌级增强：4 金币 < 商品价 5
            var deck = Deck(4,
                Card("a", Suit.Spades, Rank.Ace),
                Card("b", Suit.Hearts, Rank.King));
            var session = CardPickSession.TryCreate(ShopServiceKind.CardChip, 5, new EnhancementState(), deck);
            Assert.That(session.Select("a"), Is.True);
            Assert.That(session.TryConfirm(deck), Is.False);
            Assert.That(deck.Coins, Is.EqualTo(4));
            Assert.That(deck.Cards.Single(card => card.Id == "a").Enhancement, Is.EqualTo(CardEnhancement.None));

            // 花色：4 金币 < 动态价 8
            EnhancementConfig.Configure(SuitTables());
            try
            {
                var enhancements = new EnhancementState();
                var suit = CardPickSession.TryCreate(ShopServiceKind.CardSuit, 8, enhancements, deck);
                Assert.That(suit.Select("a"), Is.True);
                Assert.That(suit.TryConfirm(deck), Is.False);
                Assert.That(enhancements.SuitLevelOf(Suit.Spades), Is.EqualTo(0));
            }
            finally
            {
                EnhancementConfig.Configure(EnhancementTables.Empty);
            }

            // 移除：4 金币 < 商品价 5，牌不删
            var nine = Enumerable.Range(0, 9).Select(i => Card("r" + i, Suit.Clubs, Rank.Four)).ToArray();
            var removeDeck = Deck(4, nine);
            var remove = CardPickSession.TryCreate(ShopServiceKind.CardRemove, 5, new EnhancementState(), removeDeck);
            Assert.That(remove.Select("r0"), Is.True);
            Assert.That(remove.TryConfirm(removeDeck), Is.False);
            Assert.That(removeDeck.Cards.Count, Is.EqualTo(9));
        }
    }
}
