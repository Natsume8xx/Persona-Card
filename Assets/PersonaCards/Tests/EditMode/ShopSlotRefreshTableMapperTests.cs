using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>商店商品槽位刷新规则配表映射器测试（P0-1J）：9 行真实夹具（REFRESH_004 跳号合法）；「人格」归一「人格牌」+ 一条全局警告；数量/权重校验。</summary>
    public sealed class ShopSlotRefreshTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToNineEntries()
        {
            // 与 Docs/人格牌.xlsx「商店_商品槽位刷新规则」sheet 当前 9 行一致的夹具（REFRESH_004 跳号、商品类型旧写法「人格」）
            var rows = FixtureRows();

            var result = ShopSlotRefreshTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(9));
            Assert.That(result.Warnings, Has.Count.EqualTo(1)); // 「人格」归一全局警告
            Assert.That(result.Warnings[0], Does.Contain("人格").And.Contain("3 行"));

            // 按刷新_ID 升序；REFRESH_004 跳号不报错（不透明字符串）
            var expectedIds = new[]
            {
                "REFRESH_001", "REFRESH_002", "REFRESH_003", "REFRESH_005", "REFRESH_006",
                "REFRESH_007", "REFRESH_008", "REFRESH_009", "REFRESH_010"
            };
            for (var index = 0; index < 9; index++)
                Assert.That(result.Entries[index].refreshId, Is.EqualTo(expectedIds[index]), $"第 {index} 条应升序为 {expectedIds[index]}");

            // AI1 行：卡牌 1/45、人格（已归一为「人格牌」）1/20、服务 1/35
            Assert.That(result.Entries[0].node, Is.EqualTo("AI1"));
            Assert.That(result.Entries[0].productType, Is.EqualTo(ShopProductTableContract.ProductTypeCard));
            Assert.That(result.Entries[0].count, Is.EqualTo(1));
            Assert.That(result.Entries[0].weight, Is.EqualTo(45));
            Assert.That(result.Entries[1].productType, Is.EqualTo(ShopProductTableContract.ProductTypePersona)); // 「人格」→「人格牌」
            Assert.That(result.Entries[1].weight, Is.EqualTo(20));
            Assert.That(result.Entries[2].productType, Is.EqualTo(ShopProductTableContract.ProductTypeService));
            Assert.That(result.Entries[2].weight, Is.EqualTo(35));

            // AI3 末行：服务 1/40
            Assert.That(result.Entries[8].node, Is.EqualTo("AI3"));
            Assert.That(result.Entries[8].weight, Is.EqualTo(40));
        }

        [Test]
        public void LegacyPersonaTypeIsNormalizedWithSingleWarning()
        {
            // 多行「人格」→ 全部归一 + 恰一条全局警告（非逐行噪音）
            var rows = new List<Dictionary<string, string>>
            {
                Row("REFRESH_001", "AI1", "人格", "1", "45"),
                Row("REFRESH_002", "AI1", "人格", "1", "20")
            };

            var result = ShopSlotRefreshTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("2 行"));
            Assert.That(result.Entries[0].productType, Is.EqualTo(ShopProductTableContract.ProductTypePersona));
            Assert.That(result.Entries[1].productType, Is.EqualTo(ShopProductTableContract.ProductTypePersona));
        }

        [Test]
        public void UnknownProductTypeFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("REFRESH_001", "AI1", "道具", "1", "45")
            };

            var result = ShopSlotRefreshTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("商品类型").And.Contain("道具"));
        }

        [Test]
        public void BadCountOrWeightFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("REFRESH_001", "AI1", "卡牌", "abc", "45"),
                Row("REFRESH_002", "AI1", "卡牌", "1", "0")
            };

            var result = ShopSlotRefreshTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            Assert.That(result.Errors[0], Does.Contain("出现数量"));
            Assert.That(result.Errors[1], Does.Contain("出现权重").And.Contain("≥1"));
        }

        [Test]
        public void MissingOrDuplicateRefreshIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("REFRESH_001", "AI1", "卡牌", "1", "45"),
                Row("REFRESH_001", "AI2", "卡牌", "1", "40"),
                Row("", "AI3", "卡牌", "1", "30")
            };

            var result = ShopSlotRefreshTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = ShopSlotRefreshTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（与配表 5 列一致）。</summary>
        private static Dictionary<string, string> Row(string refreshId, string node, string productType, string count, string weight)
        {
            return new Dictionary<string, string>
            {
                { ShopSlotRefreshTableContract.ColRefreshId, refreshId },
                { ShopSlotRefreshTableContract.ColNode, node },
                { ShopSlotRefreshTableContract.ColProductType, productType },
                { ShopSlotRefreshTableContract.ColCount, count },
                { ShopSlotRefreshTableContract.ColWeight, weight }
            };
        }

        /// <summary>与 Docs/人格牌.xlsx「商店_商品槽位刷新规则」sheet 当前 9 行一致的夹具（REFRESH_004 跳号、类型旧写法「人格」）。</summary>
        private static List<Dictionary<string, string>> FixtureRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("REFRESH_001", "AI1", "卡牌", "1", "45"),
                Row("REFRESH_002", "AI1", "人格", "1", "20"),
                Row("REFRESH_003", "AI1", "服务", "1", "35"),
                Row("REFRESH_005", "AI2", "卡牌", "1", "40"),
                Row("REFRESH_006", "AI2", "人格", "1", "25"),
                Row("REFRESH_007", "AI2", "服务", "1", "35"),
                Row("REFRESH_008", "AI3", "卡牌", "1", "30"),
                Row("REFRESH_009", "AI3", "人格", "1", "30"),
                Row("REFRESH_010", "AI3", "服务", "1", "40")
            };
        }
    }
}
