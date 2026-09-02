using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>人格牌配置配表映射器测试（P0-1J 三表之一）：用行字典夹具（= XlsxTableReader 的输出形状）验证契约解析，不依赖真实 xlsx 文件。</summary>
    public sealed class PersonaCardTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToEightEntries()
        {
            // 与 Docs/人格牌.xlsx「人格牌配置」sheet 当前 8 行一致的夹具
            var rows = FullEightRows();

            var result = PersonaCardTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(8));
            Assert.That(result.Warnings, Is.Empty);

            var first = result.Entries[0];
            Assert.That(first.personaId, Is.EqualTo("PER_001"));
            Assert.That(first.personaName, Is.EqualTo("人格牌01"));
            Assert.That(first.entryId, Is.EqualTo("ENTRY_001"));
            Assert.That(first.mainAttrId, Is.EqualTo("MAIN_001"));
            Assert.That(first.subAttrId, Is.EqualTo("SUB_001"));
            Assert.That(first.maxAttrs, Is.EqualTo(3));
            Assert.That(first.maxSubAttrs, Is.EqualTo(2));
            Assert.That(first.subPoolSize, Is.EqualTo(5));

            // 每人格 5 条次级属性池：池起点依次 +5
            var expectedSubPoolStarts = new[]
            {
                "SUB_001", "SUB_006", "SUB_011", "SUB_016", "SUB_021", "SUB_026", "SUB_031", "SUB_036"
            };
            for (var index = 0; index < result.Entries.Count; index++)
                Assert.That(result.Entries[index].subAttrId, Is.EqualTo(expectedSubPoolStarts[index]), $"条目 {index} 池起点不符");
        }

        [Test]
        public void NoCompletenessCheckAcceptsSingleRow()
        {
            // P0-1J 设计：当前配表仅 8 行（后 8 张待补），映射器不做 PER 齐全检查（与旧单表契约的 16 行强制不同）
            var rows = new List<Dictionary<string, string>>
            {
                Row("PER_001", "人格牌01", "ENTRY_001", "MAIN_001", "SUB_001", "3", "2", "5")
            };

            var result = PersonaCardTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(1));
        }

        [Test]
        public void SortsEntriesByIdAscending()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("PER_003", "人格牌03", "ENTRY_003", "MAIN_003", "SUB_011", "3", "2", "5"),
                Row("PER_001", "人格牌01", "ENTRY_001", "MAIN_001", "SUB_001", "3", "2", "5"),
                Row("PER_002", "人格牌02", "ENTRY_002", "MAIN_002", "SUB_006", "3", "2", "5")
            };

            var result = PersonaCardTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries[0].personaId, Is.EqualTo("PER_001"));
            Assert.That(result.Entries[1].personaId, Is.EqualTo("PER_002"));
            Assert.That(result.Entries[2].personaId, Is.EqualTo("PER_003"));
        }

        [Test]
        public void MissingOrDuplicatePersonaIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("PER_001", "人格牌01", "ENTRY_001", "MAIN_001", "SUB_001", "3", "2", "5"),
                Row("PER_001", "人格牌01", "ENTRY_001", "MAIN_001", "SUB_001", "3", "2", "5"),
                Row("", "人格牌02", "ENTRY_002", "MAIN_002", "SUB_006", "3", "2", "5")
            };

            var result = PersonaCardTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Entries, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void MissingNameOrReferenceColumnsFail()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("PER_001", "", "ENTRY_001", "MAIN_001", "SUB_001", "3", "2", "5"),
                Row("PER_002", "人格牌02", "", "MAIN_002", "SUB_006", "3", "2", "5"),
                Row("PER_003", "人格牌03", "ENTRY_003", "", "SUB_011", "3", "2", "5"),
                Row("PER_004", "人格牌04", "ENTRY_004", "MAIN_004", "", "3", "2", "5")
            };

            var result = PersonaCardTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Entries, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(4)); // 名称/词条/主属性/次级属性：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("人格牌名称"));
            Assert.That(result.Errors[1], Does.Contain("词条_ID"));
            Assert.That(result.Errors[2], Does.Contain("主属性_ID"));
            Assert.That(result.Errors[3], Does.Contain("次级属性_ID"));
        }

        [Test]
        public void InvalidCountsFail()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("PER_001", "人格牌01", "ENTRY_001", "MAIN_001", "SUB_001", "-1", "2", "5"),
                Row("PER_002", "人格牌02", "ENTRY_002", "MAIN_002", "SUB_006", "3", "abc", "5"),
                Row("PER_003", "人格牌03", "ENTRY_003", "MAIN_003", "SUB_011", "3", "2", "0.5")
            };

            var result = PersonaCardTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(3)); // 负数/非数字/小数：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("最大属性数量"));
            Assert.That(result.Errors[1], Does.Contain("最大次级属性数量"));
            Assert.That(result.Errors[2], Does.Contain("次级属性池数量"));
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = PersonaCardTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（与配表 8 列一致）。</summary>
        private static Dictionary<string, string> Row(string personaId, string name, string entryId, string mainAttrId,
            string subAttrId, string maxAttrs, string maxSubAttrs, string subPoolSize)
        {
            return new Dictionary<string, string>
            {
                { PersonaCardTableContract.ColPersonaId, personaId },
                { PersonaCardTableContract.ColName, name },
                { PersonaCardTableContract.ColEntryId, entryId },
                { PersonaCardTableContract.ColMainAttrId, mainAttrId },
                { PersonaCardTableContract.ColSubAttrId, subAttrId },
                { PersonaCardTableContract.ColMaxAttrs, maxAttrs },
                { PersonaCardTableContract.ColMaxSubAttrs, maxSubAttrs },
                { PersonaCardTableContract.ColSubPoolSize, subPoolSize }
            };
        }

        /// <summary>与 Docs/人格牌.xlsx「人格牌配置」sheet 当前 8 行一致的夹具。</summary>
        private static List<Dictionary<string, string>> FullEightRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("PER_001", "人格牌01", "ENTRY_001", "MAIN_001", "SUB_001", "3", "2", "5"),
                Row("PER_002", "人格牌02", "ENTRY_002", "MAIN_002", "SUB_006", "3", "2", "5"),
                Row("PER_003", "人格牌03", "ENTRY_003", "MAIN_003", "SUB_011", "3", "2", "5"),
                Row("PER_004", "人格牌04", "ENTRY_004", "MAIN_004", "SUB_016", "3", "2", "5"),
                Row("PER_005", "人格牌05", "ENTRY_005", "MAIN_005", "SUB_021", "3", "2", "5"),
                Row("PER_006", "人格牌06", "ENTRY_006", "MAIN_006", "SUB_026", "3", "2", "5"),
                Row("PER_007", "人格牌07", "ENTRY_007", "MAIN_007", "SUB_031", "3", "2", "5"),
                Row("PER_008", "人格牌08", "ENTRY_008", "MAIN_008", "SUB_036", "3", "2", "5")
            };
        }
    }
}
