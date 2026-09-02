using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>人格牌强化规则配表映射器测试（P0-1J）：3 行真实夹具；每级增加原文（+10筹码/+0.3倍率/+10%独立倍率）；价格校验。</summary>
    public sealed class PersonaUpRuleTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToThreeEntries()
        {
            // 与 Docs/人格牌.xlsx「商品_人格牌强化规则」sheet 当前 3 行一致的夹具
            var rows = FixtureRows();

            var result = PersonaUpRuleTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Entries, Has.Count.EqualTo(3));

            // 按强化规则_ID 升序（ordinal：BASE_CHIPS < BASE_MULT < XMULT_RATE）
            Assert.That(result.Entries[0].ruleId, Is.EqualTo("PERSONA_UP_BASE_CHIPS"));
            Assert.That(result.Entries[0].mainAttrType, Is.EqualTo(PersonaUpRuleTableContract.MainAttrTypeChips));
            Assert.That(result.Entries[0].perLevelIncrease, Is.EqualTo("+10筹码")); // 混写原文
            Assert.That(result.Entries[0].basePrice, Is.EqualTo(8));
            Assert.That(result.Entries[0].levelPriceStep, Is.EqualTo(3));

            Assert.That(result.Entries[1].ruleId, Is.EqualTo("PERSONA_UP_BASE_MULT"));
            Assert.That(result.Entries[1].mainAttrType, Is.EqualTo(PersonaUpRuleTableContract.MainAttrTypeMult));
            Assert.That(result.Entries[1].perLevelIncrease, Is.EqualTo("+0.3倍率"));

            Assert.That(result.Entries[2].ruleId, Is.EqualTo("PERSONA_UP_XMULT_RATE"));
            Assert.That(result.Entries[2].mainAttrType, Is.EqualTo(PersonaUpRuleTableContract.MainAttrTypeXMult));
            Assert.That(result.Entries[2].perLevelIncrease, Is.EqualTo("+10%独立倍率"));
            Assert.That(result.Entries[2].basePrice, Is.EqualTo(8));
            Assert.That(result.Entries[2].levelPriceStep, Is.EqualTo(3));
        }

        [Test]
        public void MissingOrDuplicateRuleIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("PERSONA_UP_BASE_CHIPS"),
                Row("PERSONA_UP_BASE_CHIPS"),
                Row("")
            };

            var result = PersonaUpRuleTableMapper.Map(rows);

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
                Row("PERSONA_UP_BASE_CHIPS", mainAttrType: "速度型")
            };

            var result = PersonaUpRuleTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("主属性类型").And.Contain("速度型"));
        }

        [Test]
        public void BadPriceFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("PERSONA_UP_BASE_CHIPS", basePrice: ""),
                Row("PERSONA_UP_BASE_MULT", levelPriceStep: "-1")
            };

            var result = PersonaUpRuleTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            Assert.That(result.Errors[0], Does.Contain("基础价格").And.Contain("非负整数"));
            Assert.That(result.Errors[1], Does.Contain("每级涨价").And.Contain("非负整数"));
        }

        [Test]
        public void EmptyPerLevelIncreaseFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("PERSONA_UP_BASE_CHIPS", perLevelIncrease: null)
            };

            var result = PersonaUpRuleTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("每级增加"));
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = PersonaUpRuleTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（5 列全可选，null = 缺列）。</summary>
        private static Dictionary<string, string> Row(string ruleId, string mainAttrType = "筹码型",
            string perLevelIncrease = "+10筹码", string basePrice = "8", string levelPriceStep = "3")
        {
            var row = new Dictionary<string, string>();
            if (ruleId != null) row[PersonaUpRuleTableContract.ColRuleId] = ruleId;
            if (mainAttrType != null) row[PersonaUpRuleTableContract.ColMainAttrType] = mainAttrType;
            if (perLevelIncrease != null) row[PersonaUpRuleTableContract.ColPerLevelIncrease] = perLevelIncrease;
            if (basePrice != null) row[PersonaUpRuleTableContract.ColBasePrice] = basePrice;
            if (levelPriceStep != null) row[PersonaUpRuleTableContract.ColLevelPriceStep] = levelPriceStep;
            return row;
        }

        /// <summary>与 Docs/人格牌.xlsx「商品_人格牌强化规则」sheet 当前 3 行一致的夹具。</summary>
        private static List<Dictionary<string, string>> FixtureRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("PERSONA_UP_BASE_CHIPS", "筹码型", "+10筹码", "8", "3"),
                Row("PERSONA_UP_BASE_MULT", "倍率型", "+0.3倍率", "8", "3"),
                Row("PERSONA_UP_XMULT_RATE", "独立倍率型", "+10%独立倍率", "8", "3")
            };
        }
    }
}
