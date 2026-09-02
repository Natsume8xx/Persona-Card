using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>人格牌主属性配表映射器测试（P0-1J 三表之一）：用行字典夹具（= XlsxTableReader 的输出形状）验证契约解析，不依赖真实 xlsx 文件。</summary>
    public sealed class PersonaMainAttrTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToEightEntries()
        {
            // 与 Docs/人格牌.xlsx「人格牌_主属性」sheet 当前 8 行一致的夹具（参数2 混写 15/1/40/30/20/0.05）
            var rows = FullEightRows();

            var result = PersonaMainAttrTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(8));
            Assert.That(result.Warnings, Is.Empty);

            var first = result.Entries[0];
            Assert.That(first.attrId, Is.EqualTo("MAIN_001"));
            Assert.That(first.attrType, Is.EqualTo("基础筹码"));
            Assert.That(first.param1, Is.EqualTo("增加"));
            Assert.That(first.param2, Is.EqualTo("15"));
            Assert.That(first.unlockNode, Is.EqualTo("默认"));

            // 参数2 原文保留：整数与小数混写不引入格式判定（语义解析留给 B7）
            Assert.That(result.Entries[1].param2, Is.EqualTo("1"));
            Assert.That(result.Entries[7].param2, Is.EqualTo("0.05"));
            Assert.That(result.Entries[7].attrType, Is.EqualTo("独立倍率"));
        }

        [Test]
        public void EmptyParam2IsAllowed()
        {
            // 属性参数2 允许空（原文存储，B7 接线时判定语义）
            var rows = new List<Dictionary<string, string>>
            {
                Row("MAIN_001", "基础筹码", "增加", "", "默认")
            };

            var result = PersonaMainAttrTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries[0].param2, Is.Empty);
        }

        [Test]
        public void MissingOrDuplicateAttrIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("MAIN_001", "基础筹码", "增加", "15", "默认"),
                Row("MAIN_001", "基础倍率", "增加", "1", "默认"),
                Row("", "基础筹码", "增加", "40", "默认")
            };

            var result = PersonaMainAttrTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Entries, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void MissingRequiredColumnsFail()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("MAIN_001", "", "增加", "15", "默认"),
                Row("MAIN_002", "基础倍率", "", "1", "默认"),
                Row("MAIN_003", "基础筹码", "增加", "40", "")
            };

            var result = PersonaMainAttrTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(3)); // 属性类型/属性参数1/开放节点：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("属性类型"));
            Assert.That(result.Errors[1], Does.Contain("属性参数1"));
            Assert.That(result.Errors[2], Does.Contain("开放节点"));
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = PersonaMainAttrTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（与配表 5 列一致）。</summary>
        private static Dictionary<string, string> Row(string attrId, string attrType, string param1, string param2,
            string unlockNode)
        {
            return new Dictionary<string, string>
            {
                { PersonaMainAttrTableContract.ColAttrId, attrId },
                { PersonaMainAttrTableContract.ColAttrType, attrType },
                { PersonaMainAttrTableContract.ColParam1, param1 },
                { PersonaMainAttrTableContract.ColParam2, param2 },
                { PersonaMainAttrTableContract.ColUnlockNode, unlockNode }
            };
        }

        /// <summary>与 Docs/人格牌.xlsx「人格牌_主属性」sheet 当前 8 行一致的夹具。</summary>
        private static List<Dictionary<string, string>> FullEightRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("MAIN_001", "基础筹码", "增加", "15", "默认"),
                Row("MAIN_002", "基础倍率", "增加", "1", "默认"),
                Row("MAIN_003", "基础筹码", "增加", "40", "默认"),
                Row("MAIN_004", "基础筹码", "增加", "30", "默认"),
                Row("MAIN_005", "基础倍率", "增加", "1", "默认"),
                Row("MAIN_006", "基础筹码", "增加", "20", "默认"),
                Row("MAIN_007", "基础筹码", "增加", "20", "默认"),
                Row("MAIN_008", "独立倍率", "增加", "0.05", "默认")
            };
        }
    }
}
