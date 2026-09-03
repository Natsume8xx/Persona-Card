using NUnit.Framework;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;
using PersonaCards.Core;
using PersonaCards.Data;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    // UI 重排第二批：牌型强化界面会话——候选过滤/底值文案/真实价/确认语义。
    public sealed class HandEnhanceScreenSessionTests
    {
        /// <summary>每次测试把牌型目录复位到白盒回落（对子 48×2、同花 65×4），文案断言不受其他测试配置泄漏影响。</summary>
        [SetUp]
        public void SetUp()
        {
            HandTypeCatalog.Configure(null);
        }

        /// <summary>可强化目标 [对子, 同花]，价格表 8/11/14/17（升到 Lv1~Lv4 的价格）。</summary>
        private static EnhancementTables HandTables()
        {
            var tables = new EnhancementTables();
            var prices = new[] { 8, 11, 14, 17 };
            tables.HandTargets.Add(HandType.Pair);
            tables.HandTargets.Add(HandType.Flush);
            tables.HandPrices[HandType.Pair] = prices;
            tables.HandPrices[HandType.Flush] = prices;
            tables.HandNames[HandType.Pair] = "对子";
            tables.HandNames[HandType.Flush] = "同花";
            return tables;
        }

        private static ShopProductEntry HandProduct()
        {
            return new ShopProductEntry
            {
                productId = "SHOP_SERVICE_007",
                productName = "牌型强化",
                productType = "服务",
                price = 8,
                effectType = ShopState.EffectEnhanceHand
            };
        }

        private static JourneyDeckState Deck(int coins)
        {
            return new JourneyDeckState(new[] { new PlayingCardInstance("x", Suit.Clubs, Rank.Two) }, coins);
        }

        // —— 候选构建 ——

        [Test]
        public void TryCreateListsTargetsInTableOrderExcludingMaxLevel()
        {
            EnhancementConfig.Configure(HandTables());
            try
            {
                var enhancements = new EnhancementState();
                enhancements.SetHandLevel(HandType.Pair, EnhancementState.HandMaxLevel); // 满级剔除
                var session = HandEnhanceScreenSession.TryCreate(HandProduct(), enhancements);

                Assert.That(session, Is.Not.Null);
                Assert.That(session.Count, Is.EqualTo(1));
                Assert.That(session.NameText(0), Is.EqualTo("同花"));
            }
            finally
            {
                EnhancementConfig.Configure(EnhancementTables.Empty);
            }
        }

        [Test]
        public void DetailAndLevelTextsUseCatalogLv0BaseValues()
        {
            EnhancementConfig.Configure(HandTables());
            try
            {
                var session = HandEnhanceScreenSession.TryCreate(HandProduct(), new EnhancementState());

                Assert.That(session.Count, Is.EqualTo(2));
                Assert.That(session.NameText(0), Is.EqualTo("对子"));
                Assert.That(session.DetailText(0), Is.EqualTo("基础 48 筹码 × 2 倍率"));
                Assert.That(session.LevelText(0), Is.EqualTo("Lv.0"));
                Assert.That(session.NameText(1), Is.EqualTo("同花"));
                Assert.That(session.DetailText(1), Is.EqualTo("基础 65 筹码 × 4 倍率"));
            }
            finally
            {
                EnhancementConfig.Configure(EnhancementTables.Empty);
            }
        }

        [Test]
        public void TryCreateReturnsNullForInvalidInputs()
        {
            // 空表未注入：无可强化目标
            EnhancementConfig.Configure(EnhancementTables.Empty);
            Assert.That(HandEnhanceScreenSession.TryCreate(HandProduct(), new EnhancementState()), Is.Null);

            Assert.That(HandEnhanceScreenSession.TryCreate(null, new EnhancementState()), Is.Null);
            Assert.That(HandEnhanceScreenSession.TryCreate(HandProduct(), null), Is.Null);

            // 非牌型强化商品
            var personaProduct = new ShopProductEntry { effectType = ShopState.EffectEnhancePersona };
            Assert.That(HandEnhanceScreenSession.TryCreate(personaProduct, new EnhancementState()), Is.Null);

            // 全满级
            EnhancementConfig.Configure(HandTables());
            try
            {
                var maxed = new EnhancementState();
                maxed.SetHandLevel(HandType.Pair, EnhancementState.HandMaxLevel);
                maxed.SetHandLevel(HandType.Flush, EnhancementState.HandMaxLevel);
                Assert.That(HandEnhanceScreenSession.TryCreate(HandProduct(), maxed), Is.Null);
            }
            finally
            {
                EnhancementConfig.Configure(EnhancementTables.Empty);
            }
        }

        // —— 选中 / 价格 ——

        [Test]
        public void InitialStateRequiresSelectionAndPricesFollowTable()
        {
            EnhancementConfig.Configure(HandTables());
            try
            {
                var enhancements = new EnhancementState();
                enhancements.SetHandLevel(HandType.Flush, 1);
                var session = HandEnhanceScreenSession.TryCreate(HandProduct(), enhancements);

                Assert.That(session.Title, Is.EqualTo("牌型强化"));
                Assert.That(session.Description, Is.EqualTo("选择1种牌型，其原始基础筹码和基础倍率各提升10%。"));
                Assert.That(session.Hint, Is.EqualTo("请选择目标"));
                Assert.That(session.SelectedIndex, Is.EqualTo(-1));
                Assert.That(session.CanConfirm, Is.False);
                Assert.That(session.PriceText(-1), Is.EqualTo("本次价格：-- 金币"));

                // 越界选择忽略（不支持反选，只能换选）
                session.Select(-1);
                session.Select(99);
                Assert.That(session.SelectedIndex, Is.EqualTo(-1));

                session.Select(1); // 同花 Lv1 → 升 Lv2 价 11
                Assert.That(session.SelectedIndex, Is.EqualTo(1));
                Assert.That(session.CanConfirm, Is.True);
                Assert.That(session.PriceText(1), Is.EqualTo("本次价格：11 金币"));
                Assert.That(session.PriceText(0), Is.EqualTo("本次价格：8 金币")); // 对子 Lv0 → 价 8
            }
            finally
            {
                EnhancementConfig.Configure(EnhancementTables.Empty);
            }
        }

        // —— 确认 ——

        [Test]
        public void TryConfirmChargesRealPriceAndUpgradesOnce()
        {
            EnhancementConfig.Configure(HandTables());
            try
            {
                var enhancements = new EnhancementState();
                enhancements.SetHandLevel(HandType.Flush, 1);
                var session = HandEnhanceScreenSession.TryCreate(HandProduct(), enhancements);
                session.Select(1); // 同花价 11

                Assert.That(session.TryConfirm(Deck(11)), Is.True);
                Assert.That(enhancements.HandLevelOf(HandType.Flush), Is.EqualTo(2));

                // 金币不足：无副作用
                var poorEnhancements = new EnhancementState();
                var poorSession = HandEnhanceScreenSession.TryCreate(HandProduct(), poorEnhancements);
                poorSession.Select(0); // 对子 Lv0 价 8
                var poorDeck = Deck(7);
                Assert.That(poorSession.TryConfirm(poorDeck), Is.False);
                Assert.That(poorDeck.Coins, Is.EqualTo(7));
                Assert.That(poorEnhancements.HandLevelOf(HandType.Pair), Is.EqualTo(0));

                Assert.That(poorSession.TryConfirm(null), Is.False);
            }
            finally
            {
                EnhancementConfig.Configure(EnhancementTables.Empty);
            }
        }
    }
}
