using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>商店人格铸造配表映射器测试（P0-1J）：2 行真实夹具（FORGE_001/002）；功能_ID 唯一、价格必填非负。</summary>
    public sealed class ShopForgeTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToTwoEntries()
        {
            // 与 Docs/人格牌.xlsx「商店_人格铸造」sheet 当前 2 行一致的夹具
            var rows = FixtureRows();

            var result = ShopForgeTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Entries, Has.Count.EqualTo(2));

            Assert.That(result.Entries[0].forgeId, Is.EqualTo("FORGE_001"));
            Assert.That(result.Entries[0].forgeName, Is.EqualTo("解锁第二词条"));
            Assert.That(result.Entries[0].price, Is.EqualTo(5));

            Assert.That(result.Entries[1].forgeId, Is.EqualTo("FORGE_002"));
            Assert.That(result.Entries[1].forgeName, Is.EqualTo("解锁第三词条"));
            Assert.That(result.Entries[1].price, Is.EqualTo(8));
        }

        [Test]
        public void MissingOrDuplicateForgeIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("FORGE_001", "解锁第二词条", "5"),
                Row("FORGE_001", "解锁第三词条", "8"),
                Row("", "解锁第四词条", "10")
            };

            var result = ShopForgeTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Entries, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void BadPriceFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("FORGE_001", "解锁第二词条", ""),
                Row("FORGE_002", "解锁第三词条", "-1")
            };

            var result = ShopForgeTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            Assert.That(result.Errors[0], Does.Contain("价格").And.Contain("非负整数"));
        }

        [Test]
        public void EmptyNameFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("FORGE_001", "", "5")
            };

            var result = ShopForgeTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("功能名称"));
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = ShopForgeTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（与配表 3 列一致）。</summary>
        private static Dictionary<string, string> Row(string forgeId, string forgeName, string price)
        {
            return new Dictionary<string, string>
            {
                { ShopForgeTableContract.ColForgeId, forgeId },
                { ShopForgeTableContract.ColForgeName, forgeName },
                { ShopForgeTableContract.ColPrice, price }
            };
        }

        /// <summary>与 Docs/人格牌.xlsx「商店_人格铸造」sheet 当前 2 行一致的夹具。</summary>
        private static List<Dictionary<string, string>> FixtureRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("FORGE_001", "解锁第二词条", "5"),
                Row("FORGE_002", "解锁第三词条", "8")
            };
        }
    }
}
