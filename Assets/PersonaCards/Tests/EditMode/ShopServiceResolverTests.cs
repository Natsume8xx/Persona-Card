using NUnit.Framework;
using PersonaCards.Data;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    public sealed class ShopServiceResolverTests
    {
        // —— 测试夹具：按配表真实形态构造服务商品（effectType + effectParam1 原文）——

        private static ShopProductEntry Product(string id, string effectType, string effectParam1)
        {
            return new ShopProductEntry
            {
                productId = id,
                productName = id,
                productType = "服务",
                price = 5,
                purchaseLimit = 1,
                effectType = effectType,
                effectParam1 = effectParam1,
                effectParam2 = ""
            };
        }

        // —— 8 种服务分派（SHOP_SERVICE_001~008 → 对应强化界面路由键）——

        [Test]
        public void ResolveMapsEightServicesByEffectTypeAndParam()
        {
            Assert.That(ShopServiceResolver.Resolve(Product("S1", ShopState.EffectEnhanceCard, ShopServiceResolver.ParamBaseChips)),
                Is.EqualTo(ShopServiceKind.CardChip));
            Assert.That(ShopServiceResolver.Resolve(Product("S2", ShopState.EffectEnhanceCard, ShopServiceResolver.ParamCoins)),
                Is.EqualTo(ShopServiceKind.CardCoin));
            Assert.That(ShopServiceResolver.Resolve(Product("S3", ShopState.EffectEnhanceCard, ShopServiceResolver.ParamBaseMult)),
                Is.EqualTo(ShopServiceKind.CardMult));
            Assert.That(ShopServiceResolver.Resolve(Product("S4", ShopState.EffectEnhanceCard, ShopServiceResolver.ParamIndependentMult)),
                Is.EqualTo(ShopServiceKind.CardIndependentMult));
            Assert.That(ShopServiceResolver.Resolve(Product("S5", ShopState.EffectEnhanceSuit, "")),
                Is.EqualTo(ShopServiceKind.CardSuit));
            Assert.That(ShopServiceResolver.Resolve(Product("S6", ShopState.EffectRemoveCard, "")),
                Is.EqualTo(ShopServiceKind.CardRemove));
            Assert.That(ShopServiceResolver.Resolve(Product("S7", ShopState.EffectEnhanceHand, "")),
                Is.EqualTo(ShopServiceKind.Hand));
            Assert.That(ShopServiceResolver.Resolve(Product("S8", ShopState.EffectEnhancePersona, "")),
                Is.EqualTo(ShopServiceKind.Persona));
        }

        // —— 防御回落：null / 非服务 / 未知参数 → None（调用方提示后不弹界面）——

        [Test]
        public void ResolveFallsBackToNoneForUnknownCombinations()
        {
            Assert.That(ShopServiceResolver.Resolve(null), Is.EqualTo(ShopServiceKind.None));
            // 非服务效果类型
            Assert.That(ShopServiceResolver.Resolve(Product("C1", ShopState.EffectAddCard, ShopServiceResolver.ParamBaseChips)),
                Is.EqualTo(ShopServiceKind.None));
            // 强化卡牌但参数1未知（配表加新行但路由未接线）
            Assert.That(ShopServiceResolver.Resolve(Product("S1", ShopState.EffectEnhanceCard, "未知参数")),
                Is.EqualTo(ShopServiceKind.None));
            // effectType 空
            Assert.That(ShopServiceResolver.Resolve(Product("S2", "", "")), Is.EqualTo(ShopServiceKind.None));
        }

        // —— 选牌弹窗类判定：6 种单卡服务走弹窗，牌型/人格走列表界面 ——

        [Test]
        public void IsCardPickKindCoversSixSingleCardServicesOnly()
        {
            Assert.That(ShopServiceResolver.IsCardPickKind(ShopServiceKind.CardChip), Is.True);
            Assert.That(ShopServiceResolver.IsCardPickKind(ShopServiceKind.CardCoin), Is.True);
            Assert.That(ShopServiceResolver.IsCardPickKind(ShopServiceKind.CardMult), Is.True);
            Assert.That(ShopServiceResolver.IsCardPickKind(ShopServiceKind.CardIndependentMult), Is.True);
            Assert.That(ShopServiceResolver.IsCardPickKind(ShopServiceKind.CardSuit), Is.True);
            Assert.That(ShopServiceResolver.IsCardPickKind(ShopServiceKind.CardRemove), Is.True);

            Assert.That(ShopServiceResolver.IsCardPickKind(ShopServiceKind.Hand), Is.False);
            Assert.That(ShopServiceResolver.IsCardPickKind(ShopServiceKind.Persona), Is.False);
            Assert.That(ShopServiceResolver.IsCardPickKind(ShopServiceKind.None), Is.False);
        }
    }
}
