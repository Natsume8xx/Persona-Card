using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>牌型强化配表映射器测试（P0-1J）：44 行真实夹具（11 牌型 × Lv.1~4）；基础倍率混写原文（3/3.25/11）；筹码/价格 int 校验。</summary>
    public sealed class HandUpTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToFortyFourEntries()
        {
            // 与 Docs/人格牌.xlsx「商品_牌型强化」sheet 当前 44 行一致的夹具
            var rows = FixtureRows();

            var result = HandUpTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Entries, Has.Count.EqualTo(44));

            // 首条：高牌 Lv.1
            Assert.That(result.Entries[0].handUpId, Is.EqualTo("HAND_UP_001"));
            Assert.That(result.Entries[0].handId, Is.EqualTo("HAND_01"));
            Assert.That(result.Entries[0].handName, Is.EqualTo("高牌"));
            Assert.That(result.Entries[0].level, Is.EqualTo("Lv.1"));
            Assert.That(result.Entries[0].baseChips, Is.EqualTo(61));
            Assert.That(result.Entries[0].baseMult, Is.EqualTo("1.1")); // 倍率原文
            Assert.That(result.Entries[0].price, Is.EqualTo(8));

            // 混写原文：两对 Lv.2 基础倍率「3」（整数写法）、Lv.3「3.25」（小数写法）精确保存
            Assert.That(result.Entries[9].handUpId, Is.EqualTo("HAND_UP_010"));
            Assert.That(result.Entries[9].baseMult, Is.EqualTo("3"));
            Assert.That(result.Entries[10].handUpId, Is.EqualTo("HAND_UP_011"));
            Assert.That(result.Entries[10].baseMult, Is.EqualTo("3.25"));

            // 同花顺 Lv.1 倍率「11」（整数）原文
            Assert.That(result.Entries[32].handUpId, Is.EqualTo("HAND_UP_033"));
            Assert.That(result.Entries[32].handId, Is.EqualTo("HAND_09"));
            Assert.That(result.Entries[32].baseMult, Is.EqualTo("11"));

            // 末条：皇家同花顺 Lv.4
            Assert.That(result.Entries[43].handUpId, Is.EqualTo("HAND_UP_044"));
            Assert.That(result.Entries[43].handId, Is.EqualTo("HAND_11"));
            Assert.That(result.Entries[43].handName, Is.EqualTo("皇家同花顺"));
            Assert.That(result.Entries[43].level, Is.EqualTo("Lv.4"));
            Assert.That(result.Entries[43].baseChips, Is.EqualTo(140));
            Assert.That(result.Entries[43].baseMult, Is.EqualTo("16.8"));
            Assert.That(result.Entries[43].price, Is.EqualTo(17));
        }

        [Test]
        public void MissingOrDuplicateHandUpIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("HAND_UP_001"),
                Row("HAND_UP_001"),
                Row("")
            };

            var result = HandUpTableMapper.Map(rows);

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
                Row("HAND_UP_001", baseChips: "abc"),
                Row("HAND_UP_002", price: "-1")
            };

            var result = HandUpTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            Assert.That(result.Errors[0], Does.Contain("基础筹码").And.Contain("非负整数"));
            Assert.That(result.Errors[1], Does.Contain("价格").And.Contain("非负整数"));
        }

        [Test]
        public void EmptyHandIdOrNameFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("HAND_UP_001", handId: null),
                Row("HAND_UP_002", handName: null)
            };

            var result = HandUpTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            Assert.That(result.Errors[0], Does.Contain("牌型_ID"));
            Assert.That(result.Errors[1], Does.Contain("牌型名称"));
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = HandUpTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（7 列全可选，null = 缺列）。</summary>
        private static Dictionary<string, string> Row(string handUpId, string handId = "HAND_01",
            string handName = "高牌", string level = "Lv.1", string baseChips = "61", string baseMult = "1.1", string price = "8")
        {
            var row = new Dictionary<string, string>();
            if (handUpId != null) row[HandUpTableContract.ColHandUpId] = handUpId;
            if (handId != null) row[HandUpTableContract.ColHandId] = handId;
            if (handName != null) row[HandUpTableContract.ColHandName] = handName;
            if (level != null) row[HandUpTableContract.ColLevel] = level;
            if (baseChips != null) row[HandUpTableContract.ColBaseChips] = baseChips;
            if (baseMult != null) row[HandUpTableContract.ColBaseMult] = baseMult;
            if (price != null) row[HandUpTableContract.ColPrice] = price;
            return row;
        }

        /// <summary>与 Docs/人格牌.xlsx「商品_牌型强化」sheet 当前 44 行一致的夹具（11 牌型 × Lv.1~4；倍率混写原文）。</summary>
        private static List<Dictionary<string, string>> FixtureRows()
        {
            var rows = new List<Dictionary<string, string>>();

            // (牌型_ID, 牌型名称, 基础筹码 Lv1~4, 基础倍率原文 Lv1~4)
            var hands = new[]
            {
                ("HAND_01", "高牌", new[] { 61, 67, 73, 79 }, new[] { "1.1", "1.2", "1.3", "1.4" }),
                ("HAND_02", "对子", new[] { 53, 58, 63, 68 }, new[] { "2.2", "2.4", "2.6", "2.8" }),
                ("HAND_03", "两对", new[] { 57, 62, 67, 72 }, new[] { "2.75", "3", "3.25", "3.5" }),
                ("HAND_04", "三条", new[] { 63, 69, 75, 81 }, new[] { "3.3", "3.6", "3.9", "4.2" }),
                ("HAND_05", "顺子", new[] { 66, 72, 78, 84 }, new[] { "4.4", "4.8", "5.2", "5.6" }),
                ("HAND_06", "同花", new[] { 72, 79, 86, 93 }, new[] { "4.4", "4.8", "5.2", "5.6" }),
                ("HAND_07", "葫芦", new[] { 81, 88, 95, 102 }, new[] { "5.5", "6", "6.5", "7" }),
                ("HAND_08", "四条", new[] { 110, 120, 130, 140 }, new[] { "6.6", "7.2", "7.8", "8.4" }),
                ("HAND_09", "同花顺", new[] { 105, 115, 125, 135 }, new[] { "11", "12", "13", "14" }),
                ("HAND_10", "同花葫芦", new[] { 77, 84, 91, 98 }, new[] { "13.2", "14.4", "15.6", "16.8" }),
                ("HAND_11", "皇家同花顺", new[] { 110, 120, 130, 140 }, new[] { "13.2", "14.4", "15.6", "16.8" })
            };

            foreach (var (handId, handName, chips, mults) in hands)
                for (var level = 1; level <= 4; level++)
                    rows.Add(Row($"HAND_UP_{rows.Count + 1:D3}", handId, handName,
                        $"Lv.{level}", $"{chips[level - 1]}", mults[level - 1], $"{8 + (level - 1) * 3}"));
            return rows;
        }
    }
}
