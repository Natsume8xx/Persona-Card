using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>人格牌强化配表映射器测试（P0-1J）：8 行真实夹具；Lv0~Lv4 混写原文存储（15/1.3/0.05）；主属性类型三值。</summary>
    public sealed class PersonaUpTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToEightEntries()
        {
            // 与 Docs/人格牌.xlsx「商品_人格牌强化」sheet 当前 8 行一致的夹具
            var rows = FixtureRows();

            var result = PersonaUpTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Entries, Has.Count.EqualTo(8));

            // PER_001 筹码型：Lv 全整数原文
            Assert.That(result.Entries[0].personaId, Is.EqualTo("PER_001"));
            Assert.That(result.Entries[0].personaName, Is.EqualTo("人格牌01"));
            Assert.That(result.Entries[0].mainAttrType, Is.EqualTo(PersonaUpRuleTableContract.MainAttrTypeChips));
            Assert.That(result.Entries[0].lv0, Is.EqualTo("15"));
            Assert.That(result.Entries[0].lv4, Is.EqualTo("55"));

            // PER_002 倍率型：小数混写原文
            Assert.That(result.Entries[1].mainAttrType, Is.EqualTo(PersonaUpRuleTableContract.MainAttrTypeMult));
            Assert.That(result.Entries[1].lv0, Is.EqualTo("1"));
            Assert.That(result.Entries[1].lv1, Is.EqualTo("1.3"));
            Assert.That(result.Entries[1].lv4, Is.EqualTo("2.2"));

            // PER_008 独立倍率型：小数原文
            Assert.That(result.Entries[7].personaId, Is.EqualTo("PER_008"));
            Assert.That(result.Entries[7].mainAttrType, Is.EqualTo(PersonaUpRuleTableContract.MainAttrTypeXMult));
            Assert.That(result.Entries[7].lv0, Is.EqualTo("0.05"));
            Assert.That(result.Entries[7].lv4, Is.EqualTo("0.45"));
        }

        [Test]
        public void MissingOrDuplicatePersonaIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("PER_001"),
                Row("PER_001"),
                Row("")
            };

            var result = PersonaUpTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Entries, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void UnknownMainAttrTypeFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("PER_001", mainAttrType: "速度型")
            };

            var result = PersonaUpTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("主属性类型").And.Contain("速度型"));
        }

        [Test]
        public void MissingLvValueFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("PER_001", lv2: null)
            };

            var result = PersonaUpTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("Lv2").And.Contain("为空"));
        }

        [Test]
        public void EmptyNameFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("PER_001", personaName: null)
            };

            var result = PersonaUpTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("人格名称"));
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = PersonaUpTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（8 列全可选，null = 缺列）。</summary>
        private static Dictionary<string, string> Row(string personaId, string personaName = "人格牌01",
            string mainAttrType = "筹码型", string lv0 = "15", string lv1 = "25", string lv2 = "35",
            string lv3 = "45", string lv4 = "55")
        {
            var row = new Dictionary<string, string>();
            if (personaId != null) row[PersonaUpTableContract.ColPersonaId] = personaId;
            if (personaName != null) row[PersonaUpTableContract.ColPersonaName] = personaName;
            if (mainAttrType != null) row[PersonaUpTableContract.ColMainAttrType] = mainAttrType;
            if (lv0 != null) row[PersonaUpTableContract.ColLv0] = lv0;
            if (lv1 != null) row[PersonaUpTableContract.ColLv1] = lv1;
            if (lv2 != null) row[PersonaUpTableContract.ColLv2] = lv2;
            if (lv3 != null) row[PersonaUpTableContract.ColLv3] = lv3;
            if (lv4 != null) row[PersonaUpTableContract.ColLv4] = lv4;
            return row;
        }

        /// <summary>与 Docs/人格牌.xlsx「商品_人格牌强化」sheet 当前 8 行一致的夹具（Lv 混写原文）。</summary>
        private static List<Dictionary<string, string>> FixtureRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("PER_001", "人格牌01", "筹码型", "15", "25", "35", "45", "55"),
                Row("PER_002", "人格牌02", "倍率型", "1", "1.3", "1.6", "1.9", "2.2"),
                Row("PER_003", "人格牌03", "筹码型", "40", "50", "60", "70", "80"),
                Row("PER_004", "人格牌04", "筹码型", "30", "40", "50", "60", "70"),
                Row("PER_005", "人格牌05", "倍率型", "1", "1.3", "1.6", "1.9", "2.2"),
                Row("PER_006", "人格牌06", "筹码型", "20", "30", "40", "50", "60"),
                Row("PER_007", "人格牌07", "筹码型", "20", "30", "40", "50", "60"),
                Row("PER_008", "人格牌08", "独立倍率型", "0.05", "0.15", "0.25", "0.35", "0.45")
            };
        }
    }
}
