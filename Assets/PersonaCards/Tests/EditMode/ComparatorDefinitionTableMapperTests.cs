using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>比较符定义配表映射器测试（P0-1J）：用行字典夹具（= XlsxTableReader 的输出形状）验证契约解析，不依赖真实 xlsx 文件。</summary>
    public sealed class ComparatorDefinitionTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToEightEntries()
        {
            // 与 Docs/人格牌.xlsx「比较符定义表」sheet 当前 8 行一致的夹具
            var rows = FullEightRows();

            var result = ComparatorDefinitionTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(8));
            Assert.That(result.Warnings, Is.Empty);

            var first = result.Entries[0];
            Assert.That(first.comparatorId, Is.EqualTo("EQ"));
            Assert.That(first.name, Is.EqualTo("等于"));
            Assert.That(first.description, Is.EqualTo("目标值完全相同"));

            var last = result.Entries[7];
            Assert.That(last.comparatorId, Is.EqualTo("NOT_IN"));
            Assert.That(last.name, Is.EqualTo("不包含"));
        }

        [Test]
        public void MissingOrDuplicateComparatorIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("EQ", "等于", "目标值完全相同"),
                Row("EQ", "不等于", "目标值不同"),
                Row("", "大于", "超过目标值")
            };

            var result = ComparatorDefinitionTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Entries, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void EmptyNameFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("EQ", "", "目标值完全相同")
            };

            var result = ComparatorDefinitionTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("中文名称"));
        }

        [Test]
        public void EmptyDescriptionIsAllowed()
        {
            // 说明列允许空（原文存储，仅展示用）
            var rows = new List<Dictionary<string, string>>
            {
                Row("EQ", "等于", "")
            };

            var result = ComparatorDefinitionTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries[0].description, Is.Empty);
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = ComparatorDefinitionTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（与配表 3 列一致）。</summary>
        private static Dictionary<string, string> Row(string comparatorId, string name, string description)
        {
            return new Dictionary<string, string>
            {
                { ComparatorDefinitionTableContract.ColComparatorId, comparatorId },
                { ComparatorDefinitionTableContract.ColName, name },
                { ComparatorDefinitionTableContract.ColDescription, description }
            };
        }

        /// <summary>与 Docs/人格牌.xlsx「比较符定义表」sheet 当前 8 行一致的夹具。</summary>
        private static List<Dictionary<string, string>> FullEightRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("EQ", "等于", "目标值完全相同"),
                Row("NEQ", "不等于", "目标值不同"),
                Row("GT", "大于", "超过目标值"),
                Row("GTE", "大于等于", "达到目标值"),
                Row("LT", "小于", "低于目标值"),
                Row("LTE", "小于等于", "不超过目标值"),
                Row("IN", "包含", "属于指定列表"),
                Row("NOT_IN", "不包含", "不属于指定列表")
            };
        }
    }
}
