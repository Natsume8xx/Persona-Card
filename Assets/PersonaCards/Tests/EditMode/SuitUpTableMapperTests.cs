using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>花色强化配表映射器测试（P0-1J）：16 行真实夹具（4 花色 × Lv.1~4）；额外筹码/价格 int 校验；等级原文。</summary>
    public sealed class SuitUpTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToSixteenEntries()
        {
            // 与 Docs/人格牌.xlsx「商品_花色强化」sheet 当前 16 行一致的夹具
            var rows = FixtureRows();

            var result = SuitUpTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Entries, Has.Count.EqualTo(16));

            // 首条：黑桃 Lv.1
            Assert.That(result.Entries[0].suitUpId, Is.EqualTo("SUIT_UP_001"));
            Assert.That(result.Entries[0].suitId, Is.EqualTo("SUIT_001"));
            Assert.That(result.Entries[0].suitName, Is.EqualTo("黑桃"));
            Assert.That(result.Entries[0].level, Is.EqualTo("Lv.1")); // 等级原文（带点格式）
            Assert.That(result.Entries[0].extraChips, Is.EqualTo(5));
            Assert.That(result.Entries[0].price, Is.EqualTo(8));

            // 黑桃段末条：Lv.4 20 筹码 / 17 金币
            Assert.That(result.Entries[3].suitUpId, Is.EqualTo("SUIT_UP_004"));
            Assert.That(result.Entries[3].suitName, Is.EqualTo("黑桃"));
            Assert.That(result.Entries[3].level, Is.EqualTo("Lv.4"));
            Assert.That(result.Entries[3].extraChips, Is.EqualTo(20));
            Assert.That(result.Entries[3].price, Is.EqualTo(17));

            // 末条：方块 Lv.4
            Assert.That(result.Entries[15].suitUpId, Is.EqualTo("SUIT_UP_016"));
            Assert.That(result.Entries[15].suitId, Is.EqualTo("SUIT_004"));
            Assert.That(result.Entries[15].suitName, Is.EqualTo("方块"));
            Assert.That(result.Entries[15].extraChips, Is.EqualTo(20));
            Assert.That(result.Entries[15].price, Is.EqualTo(17));
        }

        [Test]
        public void MissingOrDuplicateSuitUpIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SUIT_UP_001"),
                Row("SUIT_UP_001"),
                Row("")
            };

            var result = SuitUpTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Entries, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void BadChipsOrPriceFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SUIT_UP_001", extraChips: "abc"),
                Row("SUIT_UP_002", price: "-1")
            };

            var result = SuitUpTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            Assert.That(result.Errors[0], Does.Contain("额外筹码").And.Contain("非负整数"));
            Assert.That(result.Errors[1], Does.Contain("价格").And.Contain("非负整数"));
        }

        [Test]
        public void EmptySuitIdOrNameFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SUIT_UP_001", suitId: null),
                Row("SUIT_UP_002", suitName: null)
            };

            var result = SuitUpTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            Assert.That(result.Errors[0], Does.Contain("花色_ID"));
            Assert.That(result.Errors[1], Does.Contain("花色名称"));
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = SuitUpTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（6 列全可选，null = 缺列）。</summary>
        private static Dictionary<string, string> Row(string suitUpId, string suitId = "SUIT_001",
            string suitName = "黑桃", string level = "Lv.1", string extraChips = "5", string price = "8")
        {
            var row = new Dictionary<string, string>();
            if (suitUpId != null) row[SuitUpTableContract.ColSuitUpId] = suitUpId;
            if (suitId != null) row[SuitUpTableContract.ColSuitId] = suitId;
            if (suitName != null) row[SuitUpTableContract.ColSuitName] = suitName;
            if (level != null) row[SuitUpTableContract.ColLevel] = level;
            if (extraChips != null) row[SuitUpTableContract.ColExtraChips] = extraChips;
            if (price != null) row[SuitUpTableContract.ColPrice] = price;
            return row;
        }

        /// <summary>与 Docs/人格牌.xlsx「商品_花色强化」sheet 当前 16 行一致的夹具（4 花色 × Lv.1~4）。</summary>
        private static List<Dictionary<string, string>> FixtureRows()
        {
            var rows = new List<Dictionary<string, string>>();
            var suits = new[] { ("SUIT_001", "黑桃"), ("SUIT_002", "红桃"), ("SUIT_003", "梅花"), ("SUIT_004", "方块") };
            foreach (var (suitId, suitName) in suits)
                for (var level = 1; level <= 4; level++)
                    rows.Add(Row($"SUIT_UP_{rows.Count + 1:D3}", suitId, suitName,
                        $"Lv.{level}", $"{level * 5}", $"{8 + (level - 1) * 3}"));
            return rows;
        }
    }
}
