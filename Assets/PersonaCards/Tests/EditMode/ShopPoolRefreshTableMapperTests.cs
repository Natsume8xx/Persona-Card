using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>商店商品刷新规则配表映射器测试（P0-1J）：65 行真实夹具（权重三档 1/10/20）；POLL_/POOL_ 前缀混用仅一条全局警告；权重必填 ≥1。</summary>
    public sealed class ShopPoolRefreshTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToSixtyFiveEntries()
        {
            // 与 Docs/人格牌.xlsx「商店_商品刷新规则」sheet 当前 65 行一致的夹具（含真实前缀混用 POLL_CARD_* 与 POOL_*）
            var rows = FixtureRows();

            var result = ShopPoolRefreshTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(65));
            Assert.That(result.Warnings, Has.Count.EqualTo(1)); // 前缀混用全局警告
            Assert.That(result.Warnings[0], Does.Contain("前缀混用"));

            // 卡牌池段（前 52 行）：POLL_CARD_001~052 / 权重 1 / 引用 SHOP_CARD_*
            for (var index = 0; index < 52; index++)
            {
                var entry = result.Entries[index];
                Assert.That(entry.poolId, Is.EqualTo($"POLL_CARD_{index + 1:D3}"), $"第 {index} 条应升序为 POLL_CARD_{index + 1:D3}");
                Assert.That(entry.productId, Is.EqualTo($"SHOP_CARD_{index + 1:D3}"));
                Assert.That(entry.weight, Is.EqualTo(1));
            }

            // 人格牌池段（52~59）：权重 10
            for (var index = 0; index < 8; index++)
            {
                var entry = result.Entries[52 + index];
                Assert.That(entry.poolId, Is.EqualTo($"POOL_PERSONA_{index + 1:D3}"));
                Assert.That(entry.productId, Is.EqualTo($"SHOP_PER_{index + 1:D3}"));
                Assert.That(entry.weight, Is.EqualTo(10));
            }

            // 服务池段（60~64）：权重 20
            for (var index = 0; index < 5; index++)
            {
                var entry = result.Entries[60 + index];
                Assert.That(entry.poolId, Is.EqualTo($"POOL_SERVICE_{index + 1:D3}"));
                Assert.That(entry.productId, Is.EqualTo($"SHOP_SERVICE_{index + 1:D3}"));
                Assert.That(entry.weight, Is.EqualTo(20));
            }
        }

        [Test]
        public void UniformPrefixProducesNoWarning()
        {
            // 前缀统一（全 POOL_）→ 无警告；混用才发一条全局提示
            var rows = new List<Dictionary<string, string>>
            {
                Row("POOL_CARD_001", "SHOP_CARD_001", "1"),
                Row("POOL_CARD_002", "SHOP_CARD_002", "1")
            };

            var result = ShopPoolRefreshTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void ZeroOrNegativeWeightFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("POOL_CARD_001", "SHOP_CARD_001", "0"),
                Row("POOL_CARD_002", "SHOP_CARD_002", "-1")
            };

            var result = ShopPoolRefreshTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            Assert.That(result.Errors[0], Does.Contain("权重").And.Contain("≥1"));
        }

        [Test]
        public void MissingOrDuplicatePoolIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("POOL_CARD_001", "SHOP_CARD_001", "1"),
                Row("POOL_CARD_001", "SHOP_CARD_002", "1"),
                Row("", "SHOP_CARD_003", "1")
            };

            var result = ShopPoolRefreshTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void MissingProductIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("POOL_CARD_001", "", "1")
            };

            var result = ShopPoolRefreshTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("商品_ID").And.Contain("为空"));
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = ShopPoolRefreshTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（与配表 3 列一致）。</summary>
        private static Dictionary<string, string> Row(string poolId, string productId, string weight)
        {
            return new Dictionary<string, string>
            {
                { ShopPoolRefreshTableContract.ColPoolId, poolId },
                { ShopPoolRefreshTableContract.ColProductId, productId },
                { ShopPoolRefreshTableContract.ColWeight, weight }
            };
        }

        /// <summary>与 Docs/人格牌.xlsx「商店_商品刷新规则」sheet 当前 65 行一致的夹具（含真实前缀混用）。</summary>
        private static List<Dictionary<string, string>> FixtureRows()
        {
            var rows = new List<Dictionary<string, string>>();
            for (var index = 1; index <= 52; index++)
                rows.Add(Row($"POLL_CARD_{index:D3}", $"SHOP_CARD_{index:D3}", "1"));
            for (var index = 1; index <= 8; index++)
                rows.Add(Row($"POOL_PERSONA_{index:D3}", $"SHOP_PER_{index:D3}", "10"));
            for (var index = 1; index <= 5; index++)
                rows.Add(Row($"POOL_SERVICE_{index:D3}", $"SHOP_SERVICE_{index:D3}", "20"));
            return rows;
        }
    }
}
