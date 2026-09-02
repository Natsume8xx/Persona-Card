using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// 全局配置配表映射器测试（P0-1F）：
    /// 17 行真实夹具（RULE_001~017 真实值）；行级错误全收集不 fail-fast；
    /// 数值类型「整数/小数」与配置数值类型一致性（整数规则整数字面量、小数规则整数文本合格）；
    /// RULE_001~017 齐全校验（缺=错误防误删、多=允许）；decimal 原文精确保存。
    /// </summary>
    public class GlobalConfigTableMapperTests
    {
        /// <summary>构造一行字典（XlsxTableReader 输出形态：表头 → 值）。</summary>
        private static Dictionary<string, string> Row(string ruleId, string ruleName, string valueType, string value)
        {
            return new Dictionary<string, string>
            {
                [GlobalConfigTableContract.ColRuleId] = ruleId,
                [GlobalConfigTableContract.ColRuleName] = ruleName,
                [GlobalConfigTableContract.ColValueType] = valueType,
                [GlobalConfigTableContract.ColValue] = value
            };
        }

        /// <summary>真实配表 17 行夹具（RULE_001~017 真实值）。</summary>
        private static List<Dictionary<string, string>> FixtureRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("RULE_001", "每关基础出牌次数", "整数", "4"),
                Row("RULE_002", "每关基础弃牌次数", "整数", "3"),
                Row("RULE_003", "人格生效槽位", "整数", "4"),
                Row("RULE_004", "基础人格数量", "整数", "8"),
                Row("RULE_005", "商店商品槽数量", "整数", "4"),
                Row("RULE_006", "每局AI人格生成总量", "整数", "3"),
                Row("RULE_007", "每局AI人格可带出数量", "整数", "1"),
                Row("RULE_008", "人格库存上限", "整数", "99"),
                Row("RULE_009", "人格融合消耗数量", "整数", "3"),
                Row("RULE_010", "人格融合生成数量", "整数", "1"),
                Row("RULE_011", "最近3关行为权重", "小数", "0.65"),
                Row("RULE_012", "本局累计行为权重", "小数", "0.35"),
                Row("RULE_013", "雷同人格生成降重", "小数", "0.15"),
                Row("RULE_014", "剩余出牌兑换单位", "整数", "1"),
                Row("RULE_015", "剩余出牌奖励金币", "整数", "1"),
                Row("RULE_016", "剩余弃牌兑换单位", "整数", "1"),
                Row("RULE_017", "剩余弃牌奖励金币", "整数", "1")
            };
        }

        [Test]
        public void MapsAll17RowsAndSortsByRuleId()
        {
            // 乱序输入 → 17 条目 + 按规则_ID 升序 + 全字段与配表一致
            var rows = FixtureRows();
            rows.Reverse();

            var result = GlobalConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Entries.Count, Is.EqualTo(17));

            for (var index = 1; index <= 17; index++)
            {
                var entry = result.Entries[index - 1];
                Assert.That(entry.ruleId, Is.EqualTo($"RULE_{index:D3}"), $"第 {index} 条应升序为 RULE_{index:D3}");
            }

            Assert.That(result.Entries[0].ruleName, Is.EqualTo("每关基础出牌次数"));
            Assert.That(result.Entries[0].valueType, Is.EqualTo("整数"));
            Assert.That(result.Entries[0].valueText, Is.EqualTo("4"));
            Assert.That(result.Entries[4].ruleName, Is.EqualTo("商店商品槽数量"));
            Assert.That(result.Entries[4].valueText, Is.EqualTo("4"));
            Assert.That(result.Entries[7].ruleName, Is.EqualTo("人格库存上限"));
            Assert.That(result.Entries[7].valueText, Is.EqualTo("99"));
            Assert.That(result.Entries[16].ruleName, Is.EqualTo("剩余弃牌奖励金币"));
            Assert.That(result.Entries[16].valueText, Is.EqualTo("1"));
        }

        [Test]
        public void PreservesDecimalTextExactly()
        {
            var result = GlobalConfigTableMapper.Map(FixtureRows());

            Assert.That(result.Succeeded, Is.True);
            // decimal 原文精确保存（0.65/0.35/0.15 与配表一致，不做任何规整）
            Assert.That(result.Entries[10].valueText, Is.EqualTo("0.65"));
            Assert.That(result.Entries[11].valueText, Is.EqualTo("0.35"));
            Assert.That(result.Entries[12].valueText, Is.EqualTo("0.15"));
        }

        [Test]
        public void RejectsUnknownValueType()
        {
            var rows = FixtureRows();
            rows[0][GlobalConfigTableContract.ColValueType] = "百分比";

            var result = GlobalConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("「数值类型」值「百分比」无效"));
        }

        [Test]
        public void RejectsNonIntegerTextForIntegerRule()
        {
            var rows = FixtureRows();
            rows[0][GlobalConfigTableContract.ColValue] = "4.5";

            var result = GlobalConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("整数规则不允许小数点"));
        }

        [Test]
        public void AcceptsIntegerTextForDecimalRule()
        {
            var rows = FixtureRows();
            rows[10][GlobalConfigTableContract.ColValue] = "3"; // RULE_011 最近3关行为权重

            var result = GlobalConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Entries[10].valueText, Is.EqualTo("3"));
        }

        [Test]
        public void RejectsNegativeValue()
        {
            var rows = FixtureRows();
            rows[0][GlobalConfigTableContract.ColValue] = "-1";

            var result = GlobalConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("应为非负整数"));
        }

        [Test]
        public void RejectsNonNumericValue()
        {
            var rows = FixtureRows();
            rows[10][GlobalConfigTableContract.ColValue] = "abc"; // RULE_011 最近3关行为权重

            var result = GlobalConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("应为非负数字"));
        }

        [Test]
        public void CollectsAllRowErrorsWithoutFailFast()
        {
            var rows = FixtureRows();
            rows[1][GlobalConfigTableContract.ColValueType] = "百分比"; // RULE_002 类型坏
            rows[3][GlobalConfigTableContract.ColValue] = ""; // RULE_004 数值空
            rows[5][GlobalConfigTableContract.ColRuleName] = ""; // RULE_006 名称空

            var result = GlobalConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(3));
            Assert.That(result.Errors[0], Does.Contain("RULE_002").And.Contain("数值类型"));
            Assert.That(result.Errors[1], Does.Contain("RULE_004").And.Contain("为空（必填）"));
            Assert.That(result.Errors[2], Does.Contain("RULE_006").And.Contain("规则名称"));
        }

        [Test]
        public void RejectsEmptyRuleId()
        {
            var rows = FixtureRows();
            rows[0][GlobalConfigTableContract.ColRuleId] = "";

            var result = GlobalConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("「规则_ID」为空"));
        }

        [Test]
        public void RejectsBadRuleIdFormat()
        {
            var rows = FixtureRows();
            rows[0][GlobalConfigTableContract.ColRuleId] = "RULE_1";

            var result = GlobalConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("格式无效，应为 RULE_001~RULE_999"));
        }

        [Test]
        public void RejectsDuplicateRuleId()
        {
            var rows = FixtureRows();
            rows.Add(Row("RULE_001", "每关基础出牌次数", "整数", "4"));

            var result = GlobalConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("「规则_ID」重复"));
        }

        [Test]
        public void ReportsMissingRuleId()
        {
            var rows = FixtureRows();
            rows.RemoveAt(6); // 删 RULE_007（防误删检查）

            var result = GlobalConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("缺少 RULE_007 的行"));
        }

        [Test]
        public void AllowsExtraRuleIds()
        {
            var rows = FixtureRows();
            rows.Add(Row("RULE_018", "未来规则占位", "整数", "1"));

            var result = GlobalConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Entries.Count, Is.EqualTo(18));
            Assert.That(result.Entries[17].ruleId, Is.EqualTo("RULE_018"));
        }

        [Test]
        public void RejectsEmptyTable()
        {
            Assert.That(GlobalConfigTableMapper.Map(null).Succeeded, Is.False);
            Assert.That(GlobalConfigTableMapper.Map(null).Errors[0], Does.Contain("没有任何数据行"));

            var result = GlobalConfigTableMapper.Map(new List<Dictionary<string, string>>());
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }
    }
}
