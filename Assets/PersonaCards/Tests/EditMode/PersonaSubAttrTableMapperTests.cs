using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>人格牌次级属性配表映射器测试（P0-1J 三表之一）：用行字典夹具（= XlsxTableReader 的输出形状）验证契约解析，不依赖真实 xlsx 文件。</summary>
    public sealed class PersonaSubAttrTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToFortyEntries()
        {
            // 与 Docs/人格牌.xlsx「人格牌_次级属性」sheet 当前 40 行一致的夹具（每人格 5 条，参数2 混写 8/0.3/0.03/1/5/20）
            var rows = FullFortyRows();

            var result = PersonaSubAttrTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(40));
            Assert.That(result.Warnings, Is.Empty);

            var first = result.Entries[0];
            Assert.That(first.subAttrId, Is.EqualTo("SUB_001"));
            Assert.That(first.ownerPersona, Is.EqualTo("人格牌01"));
            Assert.That(first.weight, Is.EqualTo(40));
            Assert.That(first.attrType, Is.EqualTo("基础筹码"));
            Assert.That(first.param1, Is.EqualTo("增加"));
            Assert.That(first.param2, Is.EqualTo("8"));
            Assert.That(first.unlockNode, Is.EqualTo("AI1"));

            // 参数2 原文保留：整数与小数混写不引入格式判定（SUB_038 基础筹码 20、SUB_040 金币 8）
            Assert.That(result.Entries[37].param2, Is.EqualTo("20"));
            var last = result.Entries[39];
            Assert.That(last.subAttrId, Is.EqualTo("SUB_040"));
            Assert.That(last.ownerPersona, Is.EqualTo("人格牌08"));
            Assert.That(last.attrType, Is.EqualTo("金币"));
            Assert.That(last.param2, Is.EqualTo("8"));
        }

        [Test]
        public void OwnerPersonaIsStoredAsRawName()
        {
            // 「所属人格」列当前填人格牌名称（非 PER_ID），映射器存原文不转换（名称↔ID 映射留给 B7 接线）
            var rows = new List<Dictionary<string, string>>
            {
                Row("SUB_001", "人格牌01", "40", "基础筹码", "增加", "8", "AI1")
            };

            var result = PersonaSubAttrTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries[0].ownerPersona, Is.EqualTo("人格牌01"));
        }

        [Test]
        public void InvalidWeightFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SUB_001", "人格牌01", "-1", "基础筹码", "增加", "8", "AI1"),
                Row("SUB_002", "人格牌01", "abc", "基础倍率", "增加", "0.3", "AI1")
            };

            var result = PersonaSubAttrTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Entries, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 负数/非数字：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("权重"));
            Assert.That(result.Errors[1], Does.Contain("权重"));
        }

        [Test]
        public void MissingOrDuplicateSubAttrIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SUB_001", "人格牌01", "40", "基础筹码", "增加", "8", "AI1"),
                Row("SUB_001", "人格牌01", "25", "基础倍率", "增加", "0.3", "AI1"),
                Row("", "人格牌02", "35", "基础倍率", "增加", "0.3", "AI2")
            };

            var result = PersonaSubAttrTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void MissingRequiredColumnsFail()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SUB_001", "", "40", "基础筹码", "增加", "8", "AI1"),
                Row("SUB_002", "人格牌01", "25", "", "增加", "0.3", "AI1"),
                Row("SUB_003", "人格牌01", "20", "基础筹码", "", "10", "AI2"),
                Row("SUB_004", "人格牌01", "10", "基础筹码", "增加", "20", "")
            };

            var result = PersonaSubAttrTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(4)); // 所属人格/属性类型/属性参数1/开放节点：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("所属人格"));
            Assert.That(result.Errors[1], Does.Contain("属性类型"));
            Assert.That(result.Errors[2], Does.Contain("属性参数1"));
            Assert.That(result.Errors[3], Does.Contain("开放节点"));
        }

        [Test]
        public void EmptyParam2IsAllowed()
        {
            // 属性参数2 允许空（原文存储，B7 接线时判定语义）
            var rows = new List<Dictionary<string, string>>
            {
                Row("SUB_001", "人格牌01", "40", "基础筹码", "增加", "", "AI1")
            };

            var result = PersonaSubAttrTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries[0].param2, Is.Empty);
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = PersonaSubAttrTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（与配表 7 列一致）。</summary>
        private static Dictionary<string, string> Row(string subAttrId, string ownerPersona, string weight,
            string attrType, string param1, string param2, string unlockNode)
        {
            return new Dictionary<string, string>
            {
                { PersonaSubAttrTableContract.ColSubAttrId, subAttrId },
                { PersonaSubAttrTableContract.ColOwnerPersona, ownerPersona },
                { PersonaSubAttrTableContract.ColWeight, weight },
                { PersonaSubAttrTableContract.ColAttrType, attrType },
                { PersonaSubAttrTableContract.ColParam1, param1 },
                { PersonaSubAttrTableContract.ColParam2, param2 },
                { PersonaSubAttrTableContract.ColUnlockNode, unlockNode }
            };
        }

        /// <summary>与 Docs/人格牌.xlsx「人格牌_次级属性」sheet 当前 40 行一致的夹具（每人格 5 条）。</summary>
        private static List<Dictionary<string, string>> FullFortyRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("SUB_001", "人格牌01", "40", "基础筹码", "增加", "8", "AI1"),
                Row("SUB_002", "人格牌01", "25", "基础倍率", "增加", "0.3", "AI1"),
                Row("SUB_003", "人格牌01", "20", "基础筹码", "增加", "10", "AI2"),
                Row("SUB_004", "人格牌01", "10", "基础筹码", "增加", "20", "AI1"),
                Row("SUB_005", "人格牌01", "5", "独立倍率", "增加", "0.03", "AI1"),
                Row("SUB_006", "人格牌02", "35", "基础倍率", "增加", "0.3", "AI2"),
                Row("SUB_007", "人格牌02", "30", "基础筹码", "增加", "8", "AI1"),
                Row("SUB_008", "人格牌02", "20", "基础筹码", "增加", "12", "AI2"),
                Row("SUB_009", "人格牌02", "10", "基础筹码", "增加", "15", "AI3"),
                Row("SUB_010", "人格牌02", "5", "独立倍率", "增加", "0.03", "AI1"),
                Row("SUB_011", "人格牌03", "40", "基础筹码", "增加", "15", "AI2"),
                Row("SUB_012", "人格牌03", "25", "基础倍率", "增加", "0.5", "AI3"),
                Row("SUB_013", "人格牌03", "15", "独立倍率", "增加", "0.03", "AI1"),
                Row("SUB_014", "人格牌03", "10", "出牌次数", "增加", "1", "AI2"),
                Row("SUB_015", "人格牌03", "10", "金币", "增加", "5", "AI3"),
                Row("SUB_016", "人格牌04", "35", "基础筹码", "增加", "10", "AI1"),
                Row("SUB_017", "人格牌04", "25", "弃牌次数", "增加", "1", "AI2"),
                Row("SUB_018", "人格牌04", "20", "基础倍率", "增加", "0.3", "AI3"),
                Row("SUB_019", "人格牌04", "10", "出牌次数", "增加", "1", "AI1"),
                Row("SUB_020", "人格牌04", "10", "金币", "增加", "5", "AI2"),
                Row("SUB_021", "人格牌05", "35", "基础倍率", "增加", "0.5", "AI3"),
                Row("SUB_022", "人格牌05", "25", "基础筹码", "增加", "15", "AI1"),
                Row("SUB_023", "人格牌05", "15", "弃牌次数", "增加", "1", "AI2"),
                Row("SUB_024", "人格牌05", "15", "独立倍率", "增加", "0.03", "AI3"),
                Row("SUB_025", "人格牌05", "10", "出牌次数", "增加", "1", "AI2"),
                Row("SUB_026", "人格牌06", "35", "基础筹码", "增加", "10", "AI1"),
                Row("SUB_027", "人格牌06", "25", "基础倍率", "增加", "0.3", "AI2"),
                Row("SUB_028", "人格牌06", "15", "出牌次数", "增加", "1", "AI2"),
                Row("SUB_029", "人格牌06", "15", "独立倍率", "增加", "0.03", "AI2"),
                Row("SUB_030", "人格牌06", "10", "金币", "增加", "5", "AI3"),
                Row("SUB_031", "人格牌07", "40", "基础筹码", "增加", "8", "AI1"),
                Row("SUB_032", "人格牌07", "25", "基础倍率", "增加", "0.3", "AI1"),
                Row("SUB_033", "人格牌07", "15", "出牌次数", "增加", "1", "AI2"),
                Row("SUB_034", "人格牌07", "10", "独立倍率", "增加", "0.03", "AI2"),
                Row("SUB_035", "人格牌07", "10", "金币", "增加", "5", "AI2"),
                Row("SUB_036", "人格牌08", "35", "独立倍率", "增加", "0.03", "AI1"),
                Row("SUB_037", "人格牌08", "25", "基础倍率", "增加", "0.5", "AI1"),
                Row("SUB_038", "人格牌08", "20", "基础筹码", "增加", "20", "AI2"),
                Row("SUB_039", "人格牌08", "10", "出牌次数", "增加", "1", "AI2"),
                Row("SUB_040", "人格牌08", "10", "金币", "增加", "8", "AI2")
            };
        }
    }
}
