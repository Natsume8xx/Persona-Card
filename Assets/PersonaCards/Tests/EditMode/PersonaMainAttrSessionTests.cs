using NUnit.Framework;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Core;
using PersonaCards.Data;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    // UI 重排第二批：人格主词条强化界面会话——装备槽候选/效果文案三单位/真实价/确认语义。
    public sealed class PersonaMainAttrSessionTests
    {
        /// <summary>人格强化表：基础价 8、每级涨价 3（8/11/14/17）；增量按效果类型（筹码 10 / 倍率 0.3 / 独立 0.1）。</summary>
        private static EnhancementTables PersonaTables()
        {
            var tables = new EnhancementTables();
            tables.PersonaBasePrice = 8;
            tables.PersonaLevelPriceStep = 3;
            tables.PersonaPerLevelIncrease[PersonaEffectKind.AddChips] = 10m;
            tables.PersonaPerLevelIncrease[PersonaEffectKind.AddMultiplier] = 0.3m;
            tables.PersonaPerLevelIncrease[PersonaEffectKind.MultiplyFinal] = 0.1m;
            return tables;
        }

        private static ShopProductEntry PersonaProduct()
        {
            return new ShopProductEntry
            {
                productId = "SHOP_SERVICE_008",
                productName = "人格主词条强化",
                productType = "服务",
                price = 8,
                effectType = ShopState.EffectEnhancePersona
            };
        }

        private static PersonaCardDefinition Persona(string id, string name, PersonaEffectKind kind, decimal value)
        {
            return new PersonaCardDefinition(id, name, PersonaConditionKind.Always, HandType.Pair, kind, value);
        }

        private static JourneyDeckState Deck(int coins)
        {
            return new JourneyDeckState(new[] { new PlayingCardInstance("x", Suit.Clubs, Rank.Two) }, coins);
        }

        // —— 候选构建 ——

        [Test]
        public void TryCreateListsEquippedSlotsInSlotOrderExcludingMaxLevel()
        {
            EnhancementConfig.Configure(PersonaTables());
            try
            {
                var loadout = new PersonaLoadoutState(new[]
                {
                    Persona("per_chips", "勇气", PersonaEffectKind.AddChips, 20m),
                    null,
                    Persona("per_mult", "睿智", PersonaEffectKind.AddMultiplier, 1m),
                    Persona("per_final", "命运", PersonaEffectKind.MultiplyFinal, 0.2m)
                });
                var enhancements = new EnhancementState();
                enhancements.SetPersonaLevel("per_mult", EnhancementState.PersonaMaxLevel); // 满级剔除
                enhancements.SetPersonaLevel("per_final", 1);

                var session = PersonaMainAttrSession.TryCreate(PersonaProduct(), loadout, enhancements);

                Assert.That(session, Is.Not.Null);
                Assert.That(session.Count, Is.EqualTo(2));
                Assert.That(session.NameText(0), Is.EqualTo("勇气"));
                Assert.That(session.NameText(1), Is.EqualTo("命运"));
                Assert.That(session.LevelText(0), Is.EqualTo("Lv.0"));
                Assert.That(session.LevelText(1), Is.EqualTo("Lv.1"));
                Assert.That(session.PriceText(0), Is.EqualTo("本次价格：8 金币"));
                Assert.That(session.PriceText(1), Is.EqualTo("本次价格：11 金币"));
            }
            finally
            {
                EnhancementConfig.Configure(EnhancementTables.Empty);
            }
        }

        [Test]
        public void DetailTextFormatsPerEffectKind()
        {
            EnhancementConfig.Configure(PersonaTables());
            try
            {
                var loadout = new PersonaLoadoutState(new[]
                {
                    Persona("a", "筹码型", PersonaEffectKind.AddChips, 20m),
                    Persona("b", "倍率型", PersonaEffectKind.AddMultiplier, 1m),
                    Persona("c", "独立型", PersonaEffectKind.MultiplyFinal, 0.2m),
                    Persona("d", "无词条", PersonaEffectKind.AddChips, 0m)
                });
                var session = PersonaMainAttrSession.TryCreate(PersonaProduct(), loadout, new EnhancementState());

                Assert.That(session.Count, Is.EqualTo(4));
                Assert.That(session.DetailText(0), Is.EqualTo("效果：+20 筹码"));
                Assert.That(session.DetailText(1), Is.EqualTo("效果：+1 倍率"));
                Assert.That(session.DetailText(2), Is.EqualTo("效果：+20% 独立倍率"));
                Assert.That(session.DetailText(3), Is.EqualTo("类型：人格主词条")); // 无词条回落
            }
            finally
            {
                EnhancementConfig.Configure(EnhancementTables.Empty);
            }
        }

        [Test]
        public void TryCreateReturnsNullForInvalidInputs()
        {
            var loadout = new PersonaLoadoutState(new[]
            {
                Persona("a", "勇气", PersonaEffectKind.AddChips, 20m), null, null, null
            });

            // 空表未注入：无价 → 候选全剔除
            EnhancementConfig.Configure(EnhancementTables.Empty);
            Assert.That(PersonaMainAttrSession.TryCreate(PersonaProduct(), loadout, new EnhancementState()), Is.Null);

            Assert.That(PersonaMainAttrSession.TryCreate(null, loadout, new EnhancementState()), Is.Null);
            Assert.That(PersonaMainAttrSession.TryCreate(PersonaProduct(), null, new EnhancementState()), Is.Null);
            Assert.That(PersonaMainAttrSession.TryCreate(PersonaProduct(), loadout, null), Is.Null);

            // 非人格强化商品
            var handProduct = new ShopProductEntry { effectType = ShopState.EffectEnhanceHand };
            Assert.That(PersonaMainAttrSession.TryCreate(handProduct, loadout, new EnhancementState()), Is.Null);

            EnhancementConfig.Configure(PersonaTables());
            try
            {
                // 全空槽 / 全满级
                var emptyLoadout = new PersonaLoadoutState(new PersonaCardDefinition[] { null, null, null, null });
                Assert.That(PersonaMainAttrSession.TryCreate(PersonaProduct(), emptyLoadout, new EnhancementState()), Is.Null);

                var maxed = new EnhancementState();
                maxed.SetPersonaLevel("a", EnhancementState.PersonaMaxLevel);
                Assert.That(PersonaMainAttrSession.TryCreate(PersonaProduct(), loadout, maxed), Is.Null);
            }
            finally
            {
                EnhancementConfig.Configure(EnhancementTables.Empty);
            }
        }

        // —— 选中 ——

        [Test]
        public void InitialStateRequiresSelection()
        {
            EnhancementConfig.Configure(PersonaTables());
            try
            {
                var loadout = new PersonaLoadoutState(new[]
                {
                    Persona("a", "勇气", PersonaEffectKind.AddChips, 20m), null, null, null
                });
                var session = PersonaMainAttrSession.TryCreate(PersonaProduct(), loadout, new EnhancementState());

                Assert.That(session.Title, Is.EqualTo("人格主词条强化"));
                Assert.That(session.Description, Is.EqualTo("选择1张人格牌，按主词条类型强化：筹码+10、倍率+0.3或独立倍率+10%。"));
                Assert.That(session.Hint, Is.EqualTo(""));
                Assert.That(session.SelectedIndex, Is.EqualTo(-1));
                Assert.That(session.CanConfirm, Is.False);
                Assert.That(session.PriceText(-1), Is.EqualTo("本次价格：-- 金币"));

                // 越界选择忽略
                session.Select(-1);
                session.Select(99);
                Assert.That(session.SelectedIndex, Is.EqualTo(-1));

                session.Select(0);
                Assert.That(session.SelectedIndex, Is.EqualTo(0));
                Assert.That(session.CanConfirm, Is.True);
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
            EnhancementConfig.Configure(PersonaTables());
            try
            {
                var loadout = new PersonaLoadoutState(new[]
                {
                    Persona("a", "勇气", PersonaEffectKind.AddChips, 20m), null, null, null
                });
                var enhancements = new EnhancementState();
                var session = PersonaMainAttrSession.TryCreate(PersonaProduct(), loadout, enhancements);
                session.Select(0); // Lv0 价 8

                Assert.That(session.TryConfirm(Deck(8)), Is.True);
                Assert.That(enhancements.PersonaLevelOf("a"), Is.EqualTo(1));

                // 金币不足：无副作用
                enhancements.SetPersonaLevel("a", 0);
                var poorSession = PersonaMainAttrSession.TryCreate(PersonaProduct(), loadout, enhancements);
                poorSession.Select(0);
                var poorDeck = Deck(7);
                Assert.That(poorSession.TryConfirm(poorDeck), Is.False);
                Assert.That(poorDeck.Coins, Is.EqualTo(7));
                Assert.That(enhancements.PersonaLevelOf("a"), Is.EqualTo(0));

                Assert.That(poorSession.TryConfirm(null), Is.False);
            }
            finally
            {
                EnhancementConfig.Configure(EnhancementTables.Empty);
            }
        }
    }
}
