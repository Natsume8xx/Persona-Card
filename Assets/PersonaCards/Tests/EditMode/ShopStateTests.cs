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

        /// <summary>简化夹具：卡牌池 2 张（可售）+ 人格 1 张（效果未实装，白名单过滤）+ 服务 2 个（移除卡牌/筹码强化，同权重）。</summary>
        private static List<ShopProductEntry> FixtureProducts()
        {
            return new List<ShopProductEntry>
            {
                Product("SHOP_CARD_001", "卡牌", ShopState.EffectAddCard, 2, "黑桃A"),
                Product("SHOP_CARD_002", "卡牌", ShopState.EffectAddCard, 2, "梅花2"),
                Product("SHOP_PER_001", "人格牌", "增加人格牌", 13, "人格牌01"),
                Product("SHOP_SERVICE_005", "服务", ShopState.EffectRemoveCard, 5, "卡牌移除"),
                Product("SHOP_SERVICE_001", "服务", ShopState.EffectEnhanceCard, 5, "筹码强化") // 单卡强化已实装（UI 重排第二批）：与移除卡牌同为服务候选
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
            // 权重 100 = 恒满上限：每类型恒 1 槽（卡牌/人格牌/服务各 1，共 3），保证既有购买/顺序断言的确定性
            return new List<ShopSlotRefreshEntry>
            {
                new ShopSlotRefreshEntry { refreshId = "REFRESH_001", node = group, productType = "卡牌", drawCount = 1, refreshCap = 1, weight = 100 },
                new ShopSlotRefreshEntry { refreshId = "REFRESH_002", node = group, productType = "人格牌", drawCount = 1, refreshCap = 1, weight = 100 },
                new ShopSlotRefreshEntry { refreshId = "REFRESH_003", node = group, productType = "服务", drawCount = 1, refreshCap = 1, weight = 100 }
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

        [Test]
        public void TryParseCardNameAcceptsFangKuaiSpelling()
        {
            // 现行配表花色原文「方块」（早期「方片」）——13 张方片全部可解析
            Assert.That(ShopState.TryParseCardName("方块A", out var suit, out var rank), Is.True);
            Assert.That(suit, Is.EqualTo(Suit.Diamonds));
            Assert.That(rank, Is.EqualTo(Rank.Ace));
            for (var value = 2; value <= 10; value++)
            {
                Assert.That(ShopState.TryParseCardName($"方块{value}", out suit, out rank), Is.True);
                Assert.That(suit, Is.EqualTo(Suit.Diamonds));
                Assert.That(rank, Is.EqualTo((Rank)value));
            }
            Assert.That(ShopState.TryParseCardName("方块J", out suit, out rank), Is.True);
            Assert.That(rank, Is.EqualTo(Rank.Jack));
            Assert.That(ShopState.TryParseCardName("方块Q", out suit, out rank), Is.True);
            Assert.That(rank, Is.EqualTo(Rank.Queen));
            Assert.That(ShopState.TryParseCardName("方块K", out suit, out rank), Is.True);
            Assert.That(rank, Is.EqualTo(Rank.King));
        }

        // —— 加权抽取 ——

        [Test]
        public void PickProductReturnsNullWhenNoCandidates()
        {
            // 类型无商品
            Assert.That(ShopState.PickProduct(FixtureProducts(), FixturePoolRules(), "不存在类型", 1u), Is.Null);
            // 商品全被白名单过滤（服务池里只有未实装效果时）
            var products = new List<ShopProductEntry> { Product("SHOP_SERVICE_999", "服务", "未实装效果", 5, "未知服务") };
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
                Assert.That(service.productId, Is.EqualTo("SHOP_SERVICE_001").Or.EqualTo("SHOP_SERVICE_005")); // 两个白名单内服务按权重抽取

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
            Assert.That(state.Slots[2].Product.productId, Is.EqualTo("SHOP_SERVICE_001").Or.EqualTo("SHOP_SERVICE_005")); // 服务位：两个白名单内服务按权重抽取
        }

        [Test]
        public void ShopStateHonorsSlotRuleGroupAndMissingRules()
        {
            // AI1 组规则下，第 3 关后商店（已过 1 个生成节点）应用 AI2 规则——用构造参数直接验证分组过滤：
            // 槽位规则只有 AI3 行时，AI1 分组 → 无任何槽位
            var ai3Only = new List<ShopSlotRefreshEntry>
            {
                new ShopSlotRefreshEntry { refreshId = "REFRESH_007", node = "AI3", productType = "卡牌", drawCount = 1, refreshCap = 1, weight = 100 }
            };
            Assert.That(new ShopState(FixtureProducts(), FixturePoolRules(), ai3Only, 0, 100u).Slots, Has.Count.EqualTo(0));

            // 单次刷新上限 = 0 的类型不设位
            var zeroCap = new List<ShopSlotRefreshEntry>
            {
                new ShopSlotRefreshEntry { refreshId = "REFRESH_001", node = "AI1", productType = "卡牌", drawCount = 1, refreshCap = 0, weight = 100 }
            };
            Assert.That(new ShopState(FixtureProducts(), FixturePoolRules(), zeroCap, 0, 100u).Slots, Has.Count.EqualTo(0));

            // 单次抽取数量 = 0 的类型不设位
            var zeroDraw = new List<ShopSlotRefreshEntry>
            {
                new ShopSlotRefreshEntry { refreshId = "REFRESH_001", node = "AI1", productType = "卡牌", drawCount = 0, refreshCap = 4, weight = 100 }
            };
            Assert.That(new ShopState(FixtureProducts(), FixturePoolRules(), zeroDraw, 0, 100u).Slots, Has.Count.EqualTo(0));
        }

        [Test]
        public void ShopStateIsDeterministicForSameSeed()
        {
            var first = new ShopState(FixtureProducts(), FixturePoolRules(), FixtureSlotRules("AI1"), 0, 42u);
            var second = new ShopState(FixtureProducts(), FixturePoolRules(), FixtureSlotRules("AI1"), 0, 42u);

            Assert.That(second.Slots.Select(slot => slot.Product?.productId),
                Is.EqualTo(first.Slots.Select(slot => slot.Product?.productId)));
        }

        // —— 按权重随机上架 0~上限（策划已确认：单次抽取数量 + 单次刷新上限 + 出现权重）——

        [Test]
        public void SlotsCarryProductTypeEvenWhenEmpty()
        {
            var state = new ShopState(FixtureProducts(), FixturePoolRules(), FixtureSlotRules("AI1"), 0, 42u);

            Assert.That(state.Slots, Has.Count.EqualTo(3));
            Assert.That(state.Slots[0].ProductType, Is.EqualTo("卡牌"));
            Assert.That(state.Slots[1].ProductType, Is.EqualTo("人格牌")); // 无货位也保留类型（UI 按类型切分）
            Assert.That(state.Slots[1].Product, Is.Null);
            Assert.That(state.Slots[2].ProductType, Is.EqualTo("服务"));
        }

        [Test]
        public void SlotCountIsCappedByRefreshCapAndDrawCount()
        {
            // 权重 100：上限 4 次抽签全成功 → 恒 4 槽（任意种子），且全部为卡牌类型
            var fullRules = new List<ShopSlotRefreshEntry>
            {
                new ShopSlotRefreshEntry { refreshId = "REFRESH_001", node = "AI1", productType = "卡牌", drawCount = 1, refreshCap = 4, weight = 100 }
            };
            for (uint seed = 1u; seed <= 20u; seed++)
            {
                var state = new ShopState(FixtureProducts(), FixturePoolRules(), fullRules, 0, seed);
                Assert.That(state.Slots, Has.Count.EqualTo(4), $"seed {seed} 权重 100 应恒满上限");
                Assert.That(state.Slots.All(slot => slot.ProductType == "卡牌"), Is.True);
            }

            // 抽取数量 2 × 上限 2：全成功原始 4 张，钳制到上限 2 → 恒 2 槽
            var drawTwoRules = new List<ShopSlotRefreshEntry>
            {
                new ShopSlotRefreshEntry { refreshId = "REFRESH_001", node = "AI1", productType = "卡牌", drawCount = 2, refreshCap = 2, weight = 100 }
            };
            Assert.That(new ShopState(FixtureProducts(), FixturePoolRules(), drawTwoRules, 0, 1u).Slots, Has.Count.EqualTo(2));
        }

        [Test]
        public void SlotCountIsRandomWithinCapAndHonorsWeight()
        {
            // 权重 1：上限 4 次抽签几乎不可能全成功 → 500 个种子中至少一次不满上限，且槽数恒在 0~4 内
            var rareRules = new List<ShopSlotRefreshEntry>
            {
                new ShopSlotRefreshEntry { refreshId = "REFRESH_001", node = "AI1", productType = "卡牌", drawCount = 1, refreshCap = 4, weight = 1 }
            };
            var sawUnderCap = false;
            for (uint seed = 1u; seed <= 500u; seed++)
            {
                var count = new ShopState(FixtureProducts(), FixturePoolRules(), rareRules, 0, seed).Slots.Count;
                Assert.That(count, Is.InRange(0, 4));
                if (count < 4) sawUnderCap = true;
            }
            Assert.That(sawUnderCap, Is.True, "权重 1 时 500 个种子应至少出现一次不满上限");

            // 权重 50：成功次数 ~ B(4, 0.5)，期望 2 → 300 种子总槽数期望 600（σ≈17），落在宽界 400~800 内
            var midRules = new List<ShopSlotRefreshEntry>
            {
                new ShopSlotRefreshEntry { refreshId = "REFRESH_001", node = "AI1", productType = "卡牌", drawCount = 1, refreshCap = 4, weight = 50 }
            };
            var total = 0;
            for (uint seed = 1u; seed <= 300u; seed++)
                total += new ShopState(FixtureProducts(), FixturePoolRules(), midRules, 0, seed).Slots.Count;
            Assert.That(total, Is.InRange(400, 800), "权重 50 总槽数应在期望 600 附近");
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

            Assert.That(state.TryPurchase(2, 4), Is.False); // 服务位 5 金（两种服务同价），4 金不足 → 不生效
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

        // —— P0-11：三线强化白名单 / 判定 / 仅售罄标记 ——

        [Test]
        public void EnhancementEffectsAreWhitelistedForPick()
        {
            var products = new List<ShopProductEntry>
            {
                Product("SHOP_SERVICE_006", "服务", ShopState.EffectEnhancePersona, 8, "强化人格"),
                Product("SHOP_SERVICE_007", "服务", ShopState.EffectEnhanceSuit, 8, "强化花色"),
                Product("SHOP_SERVICE_008", "服务", ShopState.EffectEnhanceHand, 8, "强化牌型")
            };
            var poolRules = new List<ShopPoolRefreshEntry>
            {
                new ShopPoolRefreshEntry { poolId = "POOL_SERVICE_006", productId = "SHOP_SERVICE_006", weight = 20 },
                new ShopPoolRefreshEntry { poolId = "POOL_SERVICE_007", productId = "SHOP_SERVICE_007", weight = 20 },
                new ShopPoolRefreshEntry { poolId = "POOL_SERVICE_008", productId = "SHOP_SERVICE_008", weight = 20 }
            };

            for (uint seed = 1u; seed <= 20u; seed++)
            {
                var picked = ShopState.PickProduct(products, poolRules, "服务", seed);
                Assert.That(picked, Is.Not.Null); // 白名单放行后按权重必抽中
                Assert.That(ShopState.IsEnhancementEffect(picked.effectType), Is.True);
            }
        }

        [Test]
        public void EnhanceCardEffectIsWhitelistedForPick()
        {
            // 白名单放行「强化卡牌」后（UI 重排第二批）：与移除卡牌同池按权重抽取，任意种子均非空
            var products = new List<ShopProductEntry>
            {
                Product("SHOP_SERVICE_001", "服务", ShopState.EffectEnhanceCard, 5, "筹码强化"),
                Product("SHOP_SERVICE_005", "服务", ShopState.EffectRemoveCard, 5, "卡牌移除")
            };
            var poolRules = new List<ShopPoolRefreshEntry>
            {
                new ShopPoolRefreshEntry { poolId = "POOL_SERVICE_001", productId = "SHOP_SERVICE_001", weight = 20 },
                new ShopPoolRefreshEntry { poolId = "POOL_SERVICE_005", productId = "SHOP_SERVICE_005", weight = 20 }
            };

            for (uint seed = 1u; seed <= 20u; seed++)
            {
                var picked = ShopState.PickProduct(products, poolRules, "服务", seed);
                Assert.That(picked, Is.Not.Null);
                Assert.That(picked.productId, Is.EqualTo("SHOP_SERVICE_001").Or.EqualTo("SHOP_SERVICE_005"));
            }
        }

        [Test]
        public void IsEnhancementEffectRecognizesOnlyThreeKinds()
        {
            Assert.That(ShopState.IsEnhancementEffect(ShopState.EffectEnhancePersona), Is.True);
            Assert.That(ShopState.IsEnhancementEffect(ShopState.EffectEnhanceSuit), Is.True);
            Assert.That(ShopState.IsEnhancementEffect(ShopState.EffectEnhanceHand), Is.True);
            Assert.That(ShopState.IsEnhancementEffect(ShopState.EffectAddCard), Is.False);
            Assert.That(ShopState.IsEnhancementEffect(ShopState.EffectRemoveCard), Is.False);
            Assert.That(ShopState.IsEnhancementEffect("强化卡牌"), Is.False);
            Assert.That(ShopState.IsEnhancementEffect(null), Is.False);
            Assert.That(ShopState.IsEnhancementEffect(""), Is.False);
        }

        [Test]
        public void TryMarkSoldMarksWithoutCoinCheck()
        {
            var state = new ShopState(FixtureProducts(), FixturePoolRules(), FixtureSlotRules("AI1"), 0, 42u);

            Assert.That(state.TryMarkSold(0), Is.True); // 不校验余额（P0-11：强化真实扣款发生在选择确认时）
            Assert.That(state.Slots[0].SoldOut, Is.True);
            Assert.That(state.TryMarkSold(0), Is.False); // 已售罄拒绝
            Assert.That(state.TryMarkSold(-1), Is.False);
            Assert.That(state.TryMarkSold(3), Is.False);
            Assert.That(state.TryMarkSold(1), Is.False); // 无货位拒绝
        }
    }
}
