using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Core;
using PersonaCards.Data;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// 商店强化选择模式会话测试（P0-11）：候选构建（三线）/满级剔除/轮换环绕/文案精确格式/确认扣款升级与金币不足拒绝。
    /// 夹具 = EnhancementTestFixtures 真实表值（花色 5/10/15/20、价格 8/11/14/17、牌型增量、人格 3 规则）。
    /// </summary>
    public sealed class ShopEnhancementSessionTests
    {
        [SetUp]
        public void SetUp()
        {
            var result = EnhancementTablesBuilder.Build(
                EnhancementTestFixtures.RealPersonaRules(),
                EnhancementTestFixtures.RealSuitUps(),
                EnhancementTestFixtures.RealHandUps());
            EnhancementConfig.Configure(result.Tables);
        }

        [TearDown]
        public void TearDown()
        {
            EnhancementConfig.Configure(EnhancementTables.Empty); // 全局门面复位，防测试间串表
        }

        [Test]
        public void SuitSessionBuildsFourTargetsInTableOrder()
        {
            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_007", ShopState.EffectEnhanceSuit),
                DefaultPersonas(), new EnhancementState());

            Assert.That(session, Is.Not.Null);
            Assert.That(session.Count, Is.EqualTo(4));
            Assert.That(session.Current.DisplayName, Is.EqualTo("黑桃")); // 配表序 SUIT_001 = 黑桃
            Assert.That(session.Current.Key, Is.EqualTo("SUIT_001"));
            Assert.That(session.Current.Level, Is.EqualTo(0));
            Assert.That(session.Current.Price, Is.EqualTo(8));
            Assert.That(session.StatusText, Is.EqualTo("黑桃 Lv0→Lv1 · 费用 8"));
        }

        [Test]
        public void SuitStatusTextUsesCurrentLevelPrice()
        {
            var enhancements = new EnhancementState();
            enhancements.SetSuitLevel(Suit.Spades, 2);
            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_007", ShopState.EffectEnhanceSuit),
                DefaultPersonas(), enhancements);

            Assert.That(session.StatusText, Is.EqualTo("黑桃 Lv2→Lv3 · 费用 14"));
            Assert.That(session.DetailText, Is.EqualTo("当前：每张 +10筹码 → 升级后：每张 +15筹码"));
        }

        [Test]
        public void CycleWrapsBothDirections()
        {
            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_007", ShopState.EffectEnhanceSuit),
                DefaultPersonas(), new EnhancementState());

            session.Cycle(-1); // 0 → 3（方块）
            Assert.That(session.Current.DisplayName, Is.EqualTo("方块"));
            session.Cycle(1); // 3 → 0（黑桃）
            Assert.That(session.Current.DisplayName, Is.EqualTo("黑桃"));
            session.Cycle(7); // 0 + 7 ≡ 3（模 4）
            Assert.That(session.Current.DisplayName, Is.EqualTo("方块"));
        }

        [Test]
        public void TryConfirmSpendsAndUpgrades()
        {
            var enhancements = new EnhancementState();
            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_007", ShopState.EffectEnhanceSuit),
                DefaultPersonas(), enhancements);
            var deck = Deck(25);

            Assert.That(session.TryConfirm(deck), Is.True);
            Assert.That(deck.Coins, Is.EqualTo(17)); // 25 − 8
            Assert.That(enhancements.SuitLevelOf(Suit.Spades), Is.EqualTo(1));
        }

        [Test]
        public void TryConfirmRejectsInsufficientCoinsWithoutSideEffects()
        {
            var enhancements = new EnhancementState();
            enhancements.SetSuitLevel(Suit.Spades, 2); // 升 Lv3 要 14 金
            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_007", ShopState.EffectEnhanceSuit),
                DefaultPersonas(), enhancements);
            var deck = Deck(13);

            Assert.That(session.TryConfirm(deck), Is.False);
            Assert.That(deck.Coins, Is.EqualTo(13));
            Assert.That(enhancements.SuitLevelOf(Suit.Spades), Is.EqualTo(2));
        }

        [Test]
        public void FullLevelTargetsAreExcludedFromCandidates()
        {
            var enhancements = new EnhancementState();
            enhancements.SetSuitLevel(Suit.Spades, EnhancementState.SuitMaxLevel);
            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_007", ShopState.EffectEnhanceSuit),
                DefaultPersonas(), enhancements);

            Assert.That(session.Count, Is.EqualTo(3));
            Assert.That(session.Current.DisplayName, Is.EqualTo("红桃")); // 满级黑桃剔除后候选首项 = 红桃
            session.Cycle(1);
            Assert.That(session.Current.DisplayName, Is.EqualTo("梅花"));
        }

        [Test]
        public void AllTargetsFullReturnsNull()
        {
            var enhancements = new EnhancementState();
            foreach (var suit in new[] { Suit.Spades, Suit.Hearts, Suit.Clubs, Suit.Diamonds })
                enhancements.SetSuitLevel(suit, EnhancementState.SuitMaxLevel);

            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_007", ShopState.EffectEnhanceSuit),
                DefaultPersonas(), enhancements);

            Assert.That(session, Is.Null);
        }

        [Test]
        public void HandSessionFollowsHandTargetsOrder()
        {
            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_008", ShopState.EffectEnhanceHand),
                DefaultPersonas(), new EnhancementState());

            Assert.That(session, Is.Not.Null);
            Assert.That(session.Count, Is.EqualTo(11)); // HAND_01 → HAND_11
            Assert.That(session.Current.DisplayName, Is.EqualTo("高牌"));
            Assert.That(session.Current.Key, Is.EqualTo("HAND_01"));
            Assert.That(session.Current.Price, Is.EqualTo(8));
        }

        [Test]
        public void HandStatusAndDetailUseRoyalFlushRealValues()
        {
            var enhancements = new EnhancementState();
            enhancements.SetHandLevel(HandType.RoyalFlush, 1);
            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_008", ShopState.EffectEnhanceHand),
                DefaultPersonas(), enhancements);

            for (var i = 0; i < 11 && session.Current.DisplayName != "皇家同花顺"; i++)
                session.Cycle(1);
            Assert.That(session.Current.DisplayName, Is.EqualTo("皇家同花顺"));
            Assert.That(session.StatusText, Is.EqualTo("皇家同花顺 Lv1→Lv2 · 费用 11"));
            Assert.That(session.DetailText, Is.EqualTo("当前：+10筹码 ×1.2倍率 → 升级后：+20筹码 ×2.4倍率"));
        }

        [Test]
        public void PersonaSessionBuildsFromEquippedSlots()
        {
            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_006", ShopState.EffectEnhancePersona),
                DefaultPersonas(), new EnhancementState());

            Assert.That(session, Is.Not.Null);
            Assert.That(session.Count, Is.EqualTo(3)); // 教学 3 张 + 空槽 1
            Assert.That(session.Current.DisplayName, Is.EqualTo("积累者"));
            Assert.That(session.Current.Key, Is.EqualTo("persona.initial.accumulator"));
            Assert.That(session.StatusText, Is.EqualTo("积累者 Lv0→Lv1 · 费用 8"));
            Assert.That(session.DetailText, Is.EqualTo("当前：无加成 → 升级后：+10筹码"));
        }

        [Test]
        public void PersonaMultiplyFinalUsesIndependentMultiplierUnit()
        {
            var enhancements = new EnhancementState();
            enhancements.SetPersonaLevel("persona.initial.ambitious", 2);
            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_006", ShopState.EffectEnhancePersona),
                DefaultPersonas(), enhancements);

            for (var i = 0; i < 3 && session.Current.DisplayName != "野心者"; i++)
                session.Cycle(1);
            Assert.That(session.Current.DisplayName, Is.EqualTo("野心者"));
            Assert.That(session.Current.Price, Is.EqualTo(14)); // 8 + 3×2
            Assert.That(session.DetailText, Is.EqualTo("当前：+0.2独立倍率 → 升级后：+0.3独立倍率"));
        }

        [Test]
        public void AllPersonasFullReturnsNull()
        {
            var enhancements = new EnhancementState();
            enhancements.SetPersonaLevel("persona.initial.accumulator", EnhancementState.PersonaMaxLevel);
            enhancements.SetPersonaLevel("persona.initial.executor", EnhancementState.PersonaMaxLevel);
            enhancements.SetPersonaLevel("persona.initial.ambitious", EnhancementState.PersonaMaxLevel);

            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_006", ShopState.EffectEnhancePersona),
                DefaultPersonas(), enhancements);

            Assert.That(session, Is.Null);
        }

        [Test]
        public void NonEnhancementProductReturnsNull()
        {
            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_CARD_001", ShopState.EffectAddCard),
                DefaultPersonas(), new EnhancementState());

            Assert.That(session, Is.Null);
        }

        [Test]
        public void EmptyTablesYieldNull()
        {
            EnhancementConfig.Configure(EnhancementTables.Empty); // 表缺失：候选价格全 0 → 无可强化对象

            var session = ShopEnhancementSession.TryCreate(
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_007", ShopState.EffectEnhanceSuit),
                DefaultPersonas(), new EnhancementState());

            Assert.That(session, Is.Null);
        }

        /// <summary>教学 3 张 + 空槽 4（与 CreateDefaultLoadout 装备一致）。</summary>
        private static PersonaLoadoutState DefaultPersonas()
        {
            return new PersonaLoadoutState(new PersonaCardDefinition[]
            {
                InitialPersonaCatalog.Accumulator,
                InitialPersonaCatalog.Executor,
                InitialPersonaCatalog.Ambitious,
                null
            });
        }

        private static JourneyDeckState Deck(int coins)
        {
            return new JourneyDeckState(
                new[] { new PlayingCardInstance("SA", Suit.Spades, Rank.Ace) }, coins);
        }
    }
}
