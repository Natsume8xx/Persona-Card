using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.Data;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    public sealed class ShopStateTests
    {
        // —— 测试夹具（按当前配表真实形态构造：商品 68 行 / 池规则 65 行 / 槽位规则 9 行）——

        private static ShopProductEntry Product(string id, string type, string effectType, int price = 2,
            string productName = null)
        {
            return new ShopProductEntry
            {
                productId = id,
                productName = productName ?? id,
                productType = type,
                price = price,
                purchaseLimit = 1,
                effectType = effectType,
                effectParam1 = "1",
                effectParam2 = ""
            };
        }

        /// <summary>简化夹具：卡牌池 2 张（可售）+ 人格 1 张（效果未实装，白名单过滤）+ 服务 1 个（移除卡牌）。</summary>
        private static List<ShopProductEntry> FixtureProducts()
        {
            return new List<ShopProductEntry>
            {
                Product("SHOP_CARD_001", "卡牌", ShopState.EffectAddCard, 2, "黑桃A"),
                Product("SHOP_CARD_002", "卡牌", ShopState.EffectAddCard, 2, "梅花2"),
                Product("SHOP_PER_001", "人格牌", "增加人格牌", 13, "人格牌01"),
                Product("SHOP_SERVICE_005", "服务", ShopState.EffectRemoveCard, 5, "卡牌移除"),
                Product("SHOP_SERVICE_001", "服务", "强化卡牌", 5, "筹码强化") // 效果未实装：不得上架
            };
        }

        private static List<ShopPoolRefreshEntry> FixturePoolRules()
        {
            return new List<ShopPoolRefreshEntry>
            {
                new ShopPoolRefreshEntry { poolId = "POLL_CARD_001", productId = "SHOP_CARD_001", weight = 1 },
                new ShopPoolRefreshEntry { poolId = "POLL_CARD_002", productId = "SHOP_CARD_002", weight = 3 },
                new ShopPoolRefreshEntry { poolId = "POOL_PERSONA_001", productId = "SHOP_PER_001", weight = 10 },
                new ShopPoolRefreshEntry { poolId = "POOL_SERVICE_001", productId = "SHOP_SERVICE_001", weight = 20 },
                new ShopPoolRefreshEntry { poolId = "POOL_SERVICE_005", productId = "SHOP_SERVICE_005", weight = 20 },
                // SHOP_SERVICE_006~008（三线强化）不在池：无池规则的商品不上架
            };
        }

        private static List<ShopSlotRefreshEntry> FixtureSlotRules(string group)
        {
            return new List<ShopSlotRefreshEntry>
            {
                new ShopSlotRefreshEntry { refreshId = "REFRESH_001", node = group, productType = "卡牌", count = 1, weight = 45 },
                new ShopSlotRefreshEntry { refreshId = "REFRESH_002", node = group, productType = "人格牌", count = 1, weight = 20 },
                new ShopSlotRefreshEntry { refreshId = "REFRESH_003", node = group, productType = "服务", count = 1, weight = 35 }
            };
        }

        // —— AI 分组映射 ——

        [Test]
        public void GroupNameOfMapsGenerationNodeCountToAiGroups()
        {
            Assert.That(ShopState.GroupNameOf(0), Is.EqualTo("AI1"));
            Assert.That(ShopState.GroupNameOf(1), Is.EqualTo("AI2"));
            Assert.That(ShopState.GroupNameOf(2), Is.EqualTo("AI3"));
            Assert.That(ShopState.GroupNameOf(5), Is.EqualTo("AI3")); // 超界回落 AI3
            Assert.That(ShopState.GroupNameOf(-1), Is.EqualTo("AI1")); // 负数防御性回落 AI1
        }

        // —— 卡商品名解析 ——

        [Test]
        public void TryParseCardNameParsesSuitAndRank()
        {
            Assert.That(ShopState.TryParseCardName("黑桃A", out var suit, out var rank), Is.True);
            Assert.That(suit, Is.EqualTo(Suit.Spades));
            Assert.That(rank, Is.EqualTo(Rank.Ace));

            Assert.That(ShopState.TryParseCardName("梅花2", out suit, out rank), Is.True);
            Assert.That(suit, Is.EqualTo(Suit.Clubs));
            Assert.That(rank, Is.EqualTo(Rank.Two));

            Assert.That(ShopState.TryParseCardName("方片10", out suit, out rank), Is.True);
            Assert.That(suit, Is.EqualTo(Suit.Diamonds));
            Assert.That(rank, Is.EqualTo(Rank.Ten));

            Assert.That(ShopState.TryParseCardName("红桃J", out suit, out rank), Is.True);
            Assert.That(suit, Is.EqualTo(Suit.Hearts));
            Assert.That(rank, Is.EqualTo(Rank.Jack));

            Assert.That(ShopState.TryParseCardName("黑桃Q", out suit, out rank), Is.True);
            Assert.That(rank, Is.EqualTo(Rank.Queen));
            Assert.That(ShopState.TryParseCardName("黑桃K", out suit, out rank), Is.True);
            Assert.That(rank, Is.EqualTo(Rank.King));
        }

        [Test]
        public void TryParseCardNameRejectsInvalidNames()
        {
            Assert.That(ShopState.TryParseCardName("鬼牌", out _, out _), Is.False);
            Assert.That(ShopState.TryParseCardName("黑桃11", out _, out _), Is.False); // 无 11 点数
            Assert.That(ShopState.TryParseCardName("黑桃1", out _, out _), Is.False);
            Assert.That(ShopState.TryParseCardName("", out _, out _), Is.False);
            Assert.That(ShopState.TryParseCardName(null, out _, out _), Is.False);
            Assert.That(ShopState.TryParseCardName("黑桃", out _, out _), Is.False);
        }

        // —— 加权抽取 ——

        [Test]
        public void PickProductReturnsNullWhenNoCandidates()
        {
            // 类型无商品
            Assert.That(ShopState.PickProduct(FixtureProducts(), FixturePoolRules(), "不存在类型", 1u), Is.Null);
            // 商品全被白名单过滤（服务池里只有未实装效果时）
            var products = new List<ShopProductEntry> { Product("SHOP_SERVICE_001", "服务", "强化卡牌", 5, "筹码强化") };
            Assert.That(ShopState.PickProduct(products, FixturePoolRules(), "服务", 1u), Is.Null);
            // 池规则为空：有商品但无池规则 → 不上架
            Assert.That(ShopState.PickProduct(FixtureProducts(), new List<ShopPoolRefreshEntry>(), "卡牌", 1u), Is.Null);
        }

        [Test]
        public void PickProductOnlyPicksPooledAndImplementedProducts()
        {
            for (uint seed = 1u; seed <= 100u; seed++)
            {
                var card = ShopState.PickProduct(FixtureProducts(), FixturePoolRules(), "卡牌", seed);
                Assert.That(card, Is.Not.Null);
                Assert.That(card.productId, Is.EqualTo("SHOP_CARD_001").Or.EqualTo("SHOP_CARD_002")); // 只从池内且白名单内抽

                var service = ShopState.PickProduct(FixtureProducts(), FixturePoolRules(), "服务", seed);
                Assert.That(service, Is.Not.Null);
                Assert.That(service.productId, Is.EqualTo("SHOP_SERVICE_005")); // 强化卡牌未实装不进候选

                // 人格商品效果「增加人格牌」未实装（模板→定义转换待 B7）：白名单过滤后无候选 → null
                Assert.That(ShopState.PickProduct(FixtureProducts(), FixturePoolRules(), "人格牌", seed), Is.Null);
            }
        }

        [Test]
        public void PickProductIsDeterministicForSameSeedAndHonorsWeightsRoughly()
        {
            Assert.That(ShopState.PickProduct(FixtureProducts(), FixturePoolRules(), "卡牌", 7u).productId,
                Is.EqualTo(ShopState.PickProduct(FixtureProducts(), FixturePoolRules(), "卡牌", 7u).productId));

            // 权重 1:3（黑桃A:梅花2）→ 500 个种子中梅花2 明显更多
            var pickCount = 0;
            for (uint seed = 1u; seed <= 500u; seed++)
            {
                if (ShopState.PickProduct(FixtureProducts(), FixturePoolRules(), "卡牌", seed).productId == "SHOP_CARD_002")
                    pickCount++;
            }
            Assert.That(pickCount, Is.GreaterThan(300), "权重 3/4 的梅花2 应显著多于黑桃A");
        }

        // —— 商品位生成 ——

        [Test]
        public void ShopStateGeneratesSlotsInProductTypeOrderWithGroupRules()
        {
            var state = new ShopState(FixtureProducts(), FixturePoolRules(), FixtureSlotRules("AI2"), 1, 100u);

            Assert.That(state.Slots, Has.Count.EqualTo(3));
            Assert.That(state.Slots[0].Product.productType, Is.EqualTo("卡牌"));
            Assert.That(state.Slots[1].Product, Is.Null); // 人格位：效果未实装 → 无货
            Assert.That(state.Slots[2].Product.productId, Is.EqualTo("SHOP_SERVICE_005")); // 服务位：唯一白名单内服务
        }

        [Test]
        public void ShopStateHonorsSlotRuleGroupAndMissingRules()
        {
            // AI1 组规则下，第 3 关后商店（已过 1 个生成节点）应用 AI2 规则——用构造参数直接验证分组过滤：
            // 槽位规则只有 AI3 行时，AI1 分组 → 无任何槽位
            var ai3Only = new List<ShopSlotRefreshEntry>
            {
                new ShopSlotRefreshEntry { refreshId = "REFRESH_007", node = "AI3", productType = "卡牌", count = 1, weight = 30 }
            };
            Assert.That(new ShopState(FixtureProducts(), FixturePoolRules(), ai3Only, 0, 100u).Slots, Has.Count.EqualTo(0));

            // count = 0 的类型不设位
            var zeroCount = new List<ShopSlotRefreshEntry>
            {
                new ShopSlotRefreshEntry { refreshId = "REFRESH_001", node = "AI1", productType = "卡牌", count = 0, weight = 45 }
            };
            Assert.That(new ShopState(FixtureProducts(), FixturePoolRules(), zeroCount, 0, 100u).Slots, Has.Count.EqualTo(0));
        }

        [Test]
        public void ShopStateIsDeterministicForSameSeed()
        {
            var first = new ShopState(FixtureProducts(), FixturePoolRules(), FixtureSlotRules("AI1"), 0, 42u);
            var second = new ShopState(FixtureProducts(), FixturePoolRules(), FixtureSlotRules("AI1"), 0, 42u);

            Assert.That(second.Slots.Select(slot => slot.Product?.productId),
                Is.EqualTo(first.Slots.Select(slot => slot.Product?.productId)));
        }

        // —— 购买校验（策划案 10.6：限购/货币足够/不足不生效）——

        [Test]
        public void TryPurchaseMarksSoldAndRejectsSecondPurchase()
        {
            var state = new ShopState(FixtureProducts(), FixturePoolRules(), FixtureSlotRules("AI1"), 0, 42u);

            Assert.That(state.TryPurchase(0, 100), Is.True); // 卡牌位，余额充足
            Assert.That(state.Slots[0].SoldOut, Is.True);
            Assert.That(state.TryPurchase(0, 100), Is.False); // 限购 1：即买即售罄
        }

        [Test]
        public void TryPurchaseRejectsInsufficientCoinsWithoutMarkingSold()
        {
            var state = new ShopState(FixtureProducts(), FixturePoolRules(), FixtureSlotRules("AI1"), 0, 42u);

            Assert.That(state.TryPurchase(2, 4), Is.False); // 移除卡牌 5 金，4 金不足 → 不生效
            Assert.That(state.Slots[2].SoldOut, Is.False);
            Assert.That(state.TryPurchase(2, 5), Is.True); // 恰好 5 金可购
        }

        [Test]
        public void TryPurchaseRejectsOutOfRangeAndEmptySlots()
        {
            var state = new ShopState(FixtureProducts(), FixturePoolRules(), FixtureSlotRules("AI1"), 0, 42u);

            Assert.That(state.TryPurchase(-1, 100), Is.False);
            Assert.That(state.TryPurchase(3, 100), Is.False);
            Assert.That(state.TryPurchase(1, 100), Is.False); // 人格位无货
        }
    }
}
