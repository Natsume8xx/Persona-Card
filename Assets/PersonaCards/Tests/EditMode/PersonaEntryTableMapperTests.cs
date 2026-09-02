using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>人格牌词条配表映射器测试（P0-1J 三表之一）：用行字典夹具（= XlsxTableReader 的输出形状）验证契约解析，不依赖真实 xlsx 文件。</summary>
    public sealed class PersonaEntryTableMapperTests
    {
        /// <summary>与配表一致的比较符定义 ID 集合（比较符定义表 8 行）。</summary>
        private static readonly ICollection<string> AllComparatorIds = new HashSet<string>
        {
            "EQ", "NEQ", "GT", "GTE", "LT", "LTE", "IN", "NOT_IN"
        };

        [Test]
        public void RealTableFixtureMapsToEightEntries()
        {
            // 与 Docs/人格牌.xlsx「人格牌_词条」sheet 当前 8 行一致的夹具（参数混写 2/4/NORMAL/0/1/RARE）
            var rows = FullEightRows();

            var result = PersonaEntryTableMapper.Map(rows, AllComparatorIds);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(8));
            Assert.That(result.Warnings, Is.Empty); // 比较符都在定义表中，不应有任何警告

            var first = result.Entries[0];
            Assert.That(first.entryId, Is.EqualTo("ENTRY_001"));
            Assert.That(first.description, Is.EqualTo("连续两次使用相同牌型"));
            Assert.That(first.conditionType, Is.EqualTo("连续牌型"));
            Assert.That(first.comparator, Is.EqualTo("EQ"));
            Assert.That(first.conditionParam, Is.EqualTo("2"));

            // 条件参数原文保留：数值 0 不落空、枚举文本 NORMAL/RARE 不规范化
            Assert.That(result.Entries[2].conditionParam, Is.EqualTo("NORMAL"));
            Assert.That(result.Entries[3].conditionParam, Is.EqualTo("0"));
            Assert.That(result.Entries[7].conditionParam, Is.EqualTo("RARE"));
        }

        [Test]
        public void UnknownComparatorWarnsButSucceeds()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("ENTRY_001", "连续两次使用相同牌型", "连续牌型", "XYZ", "2")
            };

            var result = PersonaEntryTableMapper.Map(rows, AllComparatorIds);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(1));
            Assert.That(result.Entries[0].comparator, Is.EqualTo("XYZ")); // 原文保留
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("XYZ"));
            Assert.That(result.Warnings[0], Does.Contain("比较符定义表"));
        }

        [Test]
        public void NullComparatorIdsSkipsCrossCheck()
        {
            // comparatorIds = null（导入命令读比较符定义表失败时的降级路径）：跳过对照，不产生警告
            var rows = new List<Dictionary<string, string>>
            {
                Row("ENTRY_001", "连续两次使用相同牌型", "连续牌型", "ANY", "2")
            };

            var result = PersonaEntryTableMapper.Map(rows, null);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void MissingOrDuplicateEntryIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("ENTRY_001", "连续两次使用相同牌型", "连续牌型", "EQ", "2"),
                Row("ENTRY_001", "本次计分牌数量 ≥4", "计分牌数量", "GTE", "4"),
                Row("", "出牌数量不足5张", "出牌数量", "LTE", "4")
            };

            var result = PersonaEntryTableMapper.Map(rows, AllComparatorIds);

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
                Row("ENTRY_001", "", "连续牌型", "EQ", "2"),
                Row("ENTRY_002", "本次计分牌数量 ≥4", "", "GTE", "4"),
                Row("ENTRY_003", "打出牌型，品质为普通", "牌型品质", "", "NORMAL")
            };

            var result = PersonaEntryTableMapper.Map(rows, AllComparatorIds);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(3)); // 描述/条件类型/比较符：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("触发条件描述"));
            Assert.That(result.Errors[1], Does.Contain("条件类型"));
            Assert.That(result.Errors[2], Does.Contain("比较符"));
        }

        [Test]
        public void EmptyConditionParamIsAllowed()
        {
            // 条件参数允许空（原文存储，B7 接线时判定语义）
            var rows = new List<Dictionary<string, string>>
            {
                Row("ENTRY_001", "连续两次使用相同牌型", "连续牌型", "EQ", "")
            };

            var result = PersonaEntryTableMapper.Map(rows, AllComparatorIds);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries[0].conditionParam, Is.Empty);
        }

        [Test]
        public void EmptyRowsFail()
        {
            var result = PersonaEntryTableMapper.Map(new List<Dictionary<string, string>>(), AllComparatorIds);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        /// <summary>单行夹具（与配表 5 列一致）。</summary>
        private static Dictionary<string, string> Row(string entryId, string description, string conditionType,
            string comparator, string conditionParam)
        {
            return new Dictionary<string, string>
            {
                { PersonaEntryTableContract.ColEntryId, entryId },
                { PersonaEntryTableContract.ColDescription, description },
                { PersonaEntryTableContract.ColConditionType, conditionType },
                { PersonaEntryTableContract.ColComparator, comparator },
                { PersonaEntryTableContract.ColConditionParam, conditionParam }
            };
        }

        /// <summary>与 Docs/人格牌.xlsx「人格牌_词条」sheet 当前 8 行一致的夹具。</summary>
        private static List<Dictionary<string, string>> FullEightRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("ENTRY_001", "连续两次使用相同牌型", "连续牌型", "EQ", "2"),
                Row("ENTRY_002", "本次计分牌数量 ≥4", "计分牌数量", "GTE", "4"),
                Row("ENTRY_003", "打出牌型，品质为普通", "牌型品质", "EQ", "NORMAL"),
                Row("ENTRY_004", "本回合没有使用弃牌", "弃牌次数", "EQ", "0"),
                Row("ENTRY_005", "使用弃牌后下一次出牌", "弃牌后出牌", "EQ", "1"),
                Row("ENTRY_006", "连续两次使用不同牌型", "连续牌型", "NEQ", "2"),
                Row("ENTRY_007", "出牌数量不足5张", "出牌数量", "LTE", "4"),
                Row("ENTRY_008", "打出牌型，品质为稀有", "牌型品质", "EQ", "RARE")
            };
        }
    }
}
