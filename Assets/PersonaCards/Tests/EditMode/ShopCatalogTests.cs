using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Data;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// 商店门面强化池规则合成测试（P0-11）：商品表有强化服务 + 强化表已注入 → 合成 3 条服务池规则（权重 20）；
    /// 表缺失不合成（服务不上架）；策划已补的池规则不重复合成；商品缺失的服务跳过。
    /// </summary>
    public sealed class ShopCatalogTests
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
            ShopCatalog.Configure(null, null, null); // 全局门面复位，防测试间串配置
            EnhancementConfig.Configure(EnhancementTables.Empty);
        }

        [Test]
        public void SyntheticPoolRulesAddedWhenTablesAndProductsPresent()
        {
            var products = EnhancementProducts();
            var poolRules = new List<ShopPoolRefreshEntry>
            {
                new ShopPoolRefreshEntry { poolId = "POOL_SERVICE_001", productId = "SHOP_SERVICE_001", weight = 20 }
            };

            ShopCatalog.Configure(products, poolRules, new List<ShopSlotRefreshEntry>());

            Assert.That(ShopCatalog.PoolRules.Count, Is.EqualTo(4)); // 原 1 条 + 合成 3 条
            var synthetic = ShopCatalog.PoolRules.Where(rule => rule.poolId == "POOL_SERVICE_006"
                || rule.poolId == "POOL_SERVICE_007" || rule.poolId == "POOL_SERVICE_008").ToList();
            Assert.That(synthetic, Has.Count.EqualTo(3));
            Assert.That(synthetic.Select(rule => rule.weight), Is.All.EqualTo(ShopCatalog.EnhancementSyntheticWeight));
            Assert.That(synthetic[0].productId, Is.EqualTo("SHOP_SERVICE_006")); // 强化人格
            Assert.That(synthetic[1].productId, Is.EqualTo("SHOP_SERVICE_007")); // 强化花色
            Assert.That(synthetic[2].productId, Is.EqualTo("SHOP_SERVICE_008")); // 强化牌型
        }

        [Test]
        public void NotSynthesizedWhenTablesMissing()
        {
            EnhancementConfig.Configure(EnhancementTables.Empty); // 表缺失（play build / 资产未就绪）
            var poolRules = new List<ShopPoolRefreshEntry>
            {
                new ShopPoolRefreshEntry { poolId = "POOL_SERVICE_001", productId = "SHOP_SERVICE_001", weight = 20 }
            };

            ShopCatalog.Configure(EnhancementProducts(), poolRules, new List<ShopSlotRefreshEntry>());

            Assert.That(ShopCatalog.PoolRules.Count, Is.EqualTo(1)); // 不合成 → 强化服务不上架（功能缺席不崩溃）
        }

        [Test]
        public void ExistingRulesNotDuplicated()
        {
            var products = EnhancementProducts();
            var poolRules = new List<ShopPoolRefreshEntry>
            {
                new ShopPoolRefreshEntry { poolId = "POOL_SERVICE_006", productId = "SHOP_SERVICE_006", weight = 5 } // 策划已补：权重 5
            };

            ShopCatalog.Configure(products, poolRules, new List<ShopSlotRefreshEntry>());

            var personaRules = ShopCatalog.PoolRules.Where(rule => rule.productId == "SHOP_SERVICE_006").ToList();
            Assert.That(personaRules, Has.Count.EqualTo(1)); // 不重复合成
            Assert.That(personaRules[0].weight, Is.EqualTo(5)); // 保留策划权重
            Assert.That(ShopCatalog.PoolRules.Count, Is.EqualTo(3)); // 其余两线正常合成
        }

        [Test]
        public void MissingProductSkipped()
        {
            var products = EnhancementProducts();
            products.RemoveAt(1); // 缺「强化花色」服务
            var poolRules = new List<ShopPoolRefreshEntry>();

            ShopCatalog.Configure(products, poolRules, new List<ShopSlotRefreshEntry>());

            Assert.That(ShopCatalog.PoolRules.Count, Is.EqualTo(2));
            Assert.That(ShopCatalog.PoolRules.Any(rule => rule.productId == "SHOP_SERVICE_007"), Is.False);
        }

        /// <summary>商品表仅含 3 个强化服务（与合成池规则一一对应）。</summary>
        private static List<ShopProductEntry> EnhancementProducts()
        {
            return new List<ShopProductEntry>
            {
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_006", ShopState.EffectEnhancePersona),
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_007", ShopState.EffectEnhanceSuit),
                EnhancementTestFixtures.EnhancementProduct("SHOP_SERVICE_008", ShopState.EffectEnhanceHand)
            };
        }
    }
}
