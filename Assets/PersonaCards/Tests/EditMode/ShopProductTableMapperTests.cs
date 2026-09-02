using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>商店商品配表映射器测试（P0-1J）：68 行真实夹具（52 卡牌 + 8 人格牌 + 8 服务）；价格必填/限购空=0/效果参数原文/效果类型未知警告照存。</summary>
    public sealed class ShopProductTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToSixtyEightEntries()
        {
            // 与 Docs/人格牌.xlsx「商品_商品配置表」sheet 当前 68 行一致的夹具
            var rows = FixtureRows();

            var result = ShopProductTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Entries, Has.Count.EqualTo(68));

            // 卡牌段（前 52 行）：价格 2 / 限购 1 / 增加卡牌 / 参数1 = 1 / 参数2 空
            for (var index = 0; index < 52; index++)
            {
                var card = result.Entries[index];
                Assert.That(card.productId, Is.EqualTo($"SHOP_CARD_{index + 1:D3}"), $"第 {index} 条应升序为 SHOP_CARD_{index + 1:D3}");
                Assert.That(card.productType, Is.EqualTo(ShopProductTableContract.ProductTypeCard));
                Assert.That(card.price, Is.EqualTo(2));
                Assert.That(card.purchaseLimit, Is.EqualTo(1));
                Assert.That(card.effectType, Is.EqualTo("增加卡牌"));
                Assert.That(card.effectParam1, Is.EqualTo("1"));
                Assert.That(card.effectParam2, Is.Empty);
            }

            // 人格牌段（52~59）：价格 13 / 增加人格牌
            for (var index = 0; index < 8; index++)
            {
                var persona = result.Entries[52 + index];
                Assert.That(persona.productId, Is.EqualTo($"SHOP_PER_{index + 1:D3}"));
                Assert.That(persona.productType, Is.EqualTo(ShopProductTableContract.ProductTypePersona));
                Assert.That(persona.price, Is.EqualTo(13));
                Assert.That(persona.effectType, Is.EqualTo("增加人格牌"));
            }

            // 服务段（60~67）：价格 5/6/6/8/5/8/8/8；参数原文混写（0.5/Lv+1 等）精确保存
            var servicePrices = new[] { 5, 6, 6, 8, 5, 8, 8, 8 };
            for (var index = 0; index < 8; index++)
            {
                var service = result.Entries[60 + index];
                Assert.That(service.productId, Is.EqualTo($"SHOP_SERVICE_{index + 1:D3}"));
                Assert.That(service.productType, Is.EqualTo(ShopProductTableContract.ProductTypeService));
                Assert.That(service.price, Is.EqualTo(servicePrices[index]));
            }
            Assert.That(result.Entries[62].effectParam1, Is.EqualTo("基础倍率"));
            Assert.That(result.Entries[62].effectParam2, Is.EqualTo("0.5"));
            Assert.That(result.Entries[63].effectParam2, Is.EqualTo("0.03"));
            Assert.That(result.Entries[65].effectType, Is.EqualTo("强化人格"));
            Assert.That(result.Entries[65].effectParam1, Is.EqualTo("人格_ID"));
            Assert.That(result.Entries[65].effectParam2, Is.EqualTo("Lv+1"));
            Assert.That(result.Entries[66].effectType, Is.EqualTo("强化花色"));
            Assert.That(result.Entries[67].effectType, Is.EqualTo("强化牌型"));
            Assert.That(result.Entries[67].productId, Is.EqualTo("SHOP_SERVICE_008"));
        }

        [Test]
        public void MissingOrDuplicateProductIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SHOP_CARD_001"),
                Row("SHOP_CARD_001"),
                Row("")
            };

            var result = ShopProductTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Entries, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void UnknownProductTypeFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SHOP_CARD_001", productType: "道具")
            };

            var result = ShopProductTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("商品类型").And.Contain("道具"));
        }

        [Test]
        public void BadPriceFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SHOP_CARD_001", price: ""),
                Row("SHOP_CARD_002", price: "-1"),
                Row("SHOP_CARD_003", price: "abc")
            };

            var result = ShopProductTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(3));
            Assert.That(result.Errors[0], Does.Contain("价格").And.Contain("非负整数"));
        }

        [Test]
        public void BadPurchaseLimitFails()
        {
            // 坏值 → 错误（缺列的静默回落见 EmptyPurchaseLimitColumnMapsToZeroSilently）
            var rows = new List<Dictionary<string, string>>
            {
                Row("SHOP_CARD_001", purchaseLimit: null),
                Row("SHOP_CARD_002", purchaseLimit: "abc")
            };

            var result = ShopProductTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("购买次数限制"));
        }

        [Test]
        public void EmptyPurchaseLimitColumnMapsToZeroSilently()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SHOP_CARD_001", purchaseLimit: null)
            };

            var result = ShopProductTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Entries[0].purchaseLimit, Is.EqualTo(0));
        }

        [Test]
        public void UnknownEffectTypeWarnsButStores()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SHOP_SERVICE_001", effectType: "测试效果")
            };

            var result = ShopProductTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("效果类型").And.Contain("测试效果"));
            Assert.That(result.Entries[0].effectType, Is.EqualTo("测试效果")); // 原文照存
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = ShopProductTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（8 列全可选，null = 缺列）。</summary>
        private static Dictionary<string, string> Row(string productId, string productName = "商品",
            string productType = "卡牌", string price = "2", string purchaseLimit = "1",
            string effectType = "增加卡牌", string effectParam1 = "1", string effectParam2 = "")
        {
            var row = new Dictionary<string, string>();
            if (productId != null) row[ShopProductTableContract.ColProductId] = productId;
            if (productName != null) row[ShopProductTableContract.ColProductName] = productName;
            if (productType != null) row[ShopProductTableContract.ColProductType] = productType;
            if (price != null) row[ShopProductTableContract.ColPrice] = price;
            if (purchaseLimit != null) row[ShopProductTableContract.ColPurchaseLimit] = purchaseLimit;
            if (effectType != null) row[ShopProductTableContract.ColEffectType] = effectType;
            if (effectParam1 != null) row[ShopProductTableContract.ColEffectParam1] = effectParam1;
            if (effectParam2 != null) row[ShopProductTableContract.ColEffectParam2] = effectParam2;
            return row;
        }

        /// <summary>与 Docs/人格牌.xlsx「商品_商品配置表」sheet 当前 68 行一致的夹具。</summary>
        private static List<Dictionary<string, string>> FixtureRows()
        {
            var rows = new List<Dictionary<string, string>>();

            // 52 卡牌：4 花色 × 13 点数（价格 2 / 限购 1 / 增加卡牌 / 参数1 = 1）
            var suits = new[] { "黑桃", "红桃", "梅花", "方块" };
            var ranks = new[] { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
            foreach (var suit in suits)
                foreach (var rank in ranks)
                    rows.Add(Row($"SHOP_CARD_{rows.Count + 1:D3}", $"{suit}{rank}"));

            // 8 人格牌（价格 13 / 增加人格牌）
            for (var index = 1; index <= 8; index++)
                rows.Add(Row($"SHOP_PER_{index:D3}", $"人格牌{index:00}", productType: "人格牌", price: "13", effectType: "增加人格牌"));

            // 8 服务（强化类 + 移除类；参数原文混写）
            rows.Add(Row("SHOP_SERVICE_001", "筹码强化", productType: "服务", price: "5", effectType: "强化卡牌", effectParam1: "基础筹码", effectParam2: "5"));
            rows.Add(Row("SHOP_SERVICE_002", "金币强化", productType: "服务", price: "6", effectType: "强化卡牌", effectParam1: "金币", effectParam2: "2"));
            rows.Add(Row("SHOP_SERVICE_003", "倍率强化", productType: "服务", price: "6", effectType: "强化卡牌", effectParam1: "基础倍率", effectParam2: "0.5"));
            rows.Add(Row("SHOP_SERVICE_004", "独立乘区强化", productType: "服务", price: "8", effectType: "强化卡牌", effectParam1: "独立倍率", effectParam2: "0.03"));
            rows.Add(Row("SHOP_SERVICE_005", "卡牌移除", productType: "服务", price: "5", effectType: "移除卡牌", effectParam1: "1"));
            rows.Add(Row("SHOP_SERVICE_006", "人格主词条强化", productType: "服务", price: "8", effectType: "强化人格", effectParam1: "人格_ID", effectParam2: "Lv+1"));
            rows.Add(Row("SHOP_SERVICE_007", "花色强化", productType: "服务", price: "8", effectType: "强化花色", effectParam1: "花色强化_ID", effectParam2: "Lv+1"));
            rows.Add(Row("SHOP_SERVICE_008", "牌型强化", productType: "服务", price: "8", effectType: "强化牌型", effectParam1: "牌型强化_ID", effectParam2: "Lv+1"));

            return rows;
        }
    }
}
