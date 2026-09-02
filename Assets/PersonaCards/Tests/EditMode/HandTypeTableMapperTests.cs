using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Core;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>牌型配置配表映射器测试：用行字典夹具（= XlsxTableReader 的输出形状）验证契约解析，不依赖真实 xlsx 文件。</summary>
    public sealed class HandTypeTableMapperTests
    {
        /// <summary>与配表一致的牌型品质定义 ID 集合（NORMAL/RARE 两行）。</summary>
        private static readonly ICollection<string> AllQualityIds = new HashSet<string>
        {
            "NORMAL", "RARE"
        };

        [Test]
        public void RealTableFixtureMapsToElevenEntries()
        {
            // 与 Docs/人格牌.xlsx「牌型配置」sheet 当前 11 行一致的夹具（含两队 2.5 倍率、皇家同花顺）
            var rows = FullElevenRows();

            var result = HandTypeTableMapper.Map(rows, AllQualityIds);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(11));
            Assert.That(result.Warnings, Is.Empty); // 契约值全部一致，不应有任何警告

            var expectedTypes = new[]
            {
                HandType.HighCard, HandType.Pair, HandType.TwoPair, HandType.ThreeOfAKind, HandType.Straight,
                HandType.Flush, HandType.FullHouse, HandType.FourOfAKind, HandType.StraightFlush, HandType.FlushHouse,
                HandType.RoyalFlush
            };
            var expectedChips = new[] { 55, 48, 52, 57, 60, 65, 74, 100, 95, 70, 100 };
            var expectedMultipliers = new decimal[] { 1m, 2m, 2.5m, 3m, 4m, 4m, 5m, 6m, 10m, 12m, 12m };
            var expectedQualities = new[]
            {
                HandQuality.NORMAL, HandQuality.NORMAL, HandQuality.NORMAL, HandQuality.NORMAL, HandQuality.NORMAL,
                HandQuality.RARE, HandQuality.RARE, HandQuality.RARE, HandQuality.RARE, HandQuality.RARE,
                HandQuality.RARE
            };
            for (var index = 0; index < result.Entries.Count; index++)
            {
                var entry = result.Entries[index];
                Assert.That(entry.HandType, Is.EqualTo(expectedTypes[index]), $"条目 {index} 牌型不符");
                Assert.That(entry.BaseChips, Is.EqualTo(expectedChips[index]), $"条目 {index} 筹码不符");
                Assert.That(entry.BaseMultiplier, Is.EqualTo(expectedMultipliers[index]), $"条目 {index} 倍率不符");
                Assert.That(entry.DisplayOrder, Is.EqualTo(index + 1), $"条目 {index} 显示顺序不符");
                Assert.That(entry.Quality, Is.EqualTo(expectedQualities[index]), $"条目 {index} 品质不符");
            }
            // 两队 2.5 是 decimal 精确值（配表小数位 ≤2，解析即定型）
            Assert.That(result.Entries[2].BaseMultiplier, Is.EqualTo(2.5m));
        }

        [Test]
        public void UnknownHandIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("HAND_99", "神秘牌型", "1", "55", "1", "1", "NORMAL")
            };

            var result = HandTypeTableMapper.Map(rows, AllQualityIds);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Entries, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("HAND_01 ~ HAND_12"));
        }

        [Test]
        public void DuplicateOrEmptyHandIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("HAND_01", "高牌", "1", "55", "1", "1", "NORMAL"),
                Row("HAND_01", "对子", "2", "48", "2", "2", "NORMAL"),
                Row("", "顺子", "5", "60", "4", "5", "NORMAL")
            };

            var result = HandTypeTableMapper.Map(rows, AllQualityIds);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void InvalidChipsOrMultiplierFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("HAND_01", "高牌", "1", "-5", "1", "1", "NORMAL"),
                Row("HAND_02", "对子", "2", "abc", "2", "2", "NORMAL"),
                Row("HAND_03", "两队", "3", "52", "0.5", "3", "NORMAL"),
                Row("HAND_04", "三条", "4", "57", "NaN", "4", "NORMAL")
            };

            var result = HandTypeTableMapper.Map(rows, AllQualityIds);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(4)); // 4 行错误全收集
            Assert.That(result.Errors[0], Does.Contain("基础筹码"));
            Assert.That(result.Errors[1], Does.Contain("基础筹码"));
            Assert.That(result.Errors[2], Does.Contain("基础倍率"));
            Assert.That(result.Errors[3], Does.Contain("基础倍率"));
        }

        [Test]
        public void UnknownQualityFails()
        {
            // 品质值不在牌型品质定义表内 → 行级错误（防品质表未同步时静默导入）
            var rows = FullElevenRows();
            rows[0][HandTypeTableContract.ColQuality] = "EPIC";

            var result = HandTypeTableMapper.Map(rows, AllQualityIds);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("牌型品质_ID").And.Contain("EPIC"));
        }

        [Test]
        public void QualityCrossCheckSkippedWhenQualityIdsNull()
        {
            // qualityIds = null（导入命令读品质表失败时的降级路径）：可解析的品质值照常通过
            var rows = FullElevenRows();
            var result = HandTypeTableMapper.Map(rows, null);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Entries, Has.Count.EqualTo(11));
        }

        [Test]
        public void ScoringCountMismatchWarnsAndIsNotImported()
        {
            // A4 拍板：「计分牌数」列不导入，与「显示顺序」不一致时警告
            var rows = FullElevenRows();
            rows[0][HandTypeTableContract.ColScoringCount] = "99";

            var result = HandTypeTableMapper.Map(rows, AllQualityIds);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Entries, Has.Count.EqualTo(11));
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("计分牌数").And.Contain("显示顺序"));
        }

        [Test]
        public void MissingFiveOfAKindRowsAreToleratedButCoreTypesMustBeComplete()
        {
            // 已拍板容错：五条/同花五条不在表中，目录 Configure 白盒补齐，Mapper 不报错
            var fullEleven = FullElevenRows();
            var result = HandTypeTableMapper.Map(fullEleven, AllQualityIds);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Errors, Is.Empty);

            // 但核心牌型误删一行（如 HAND_05 顺子）必须报错，防止白盒兜底掩盖配表缺行
            var missingStraight = new List<Dictionary<string, string>>(fullEleven);
            missingStraight.RemoveAt(4);
            var missing = HandTypeTableMapper.Map(missingStraight, AllQualityIds);

            Assert.That(missing.Succeeded, Is.False);
            Assert.That(missing.Errors, Has.Count.EqualTo(1));
            Assert.That(missing.Errors[0], Does.Contain("Straight").And.Contain("误删"));

            // P0-1J：皇家同花顺（HAND_11）也是核心牌型，误删同样报错
            var missingRoyal = new List<Dictionary<string, string>>(fullEleven);
            missingRoyal.RemoveAt(10);
            var missingRoyalResult = HandTypeTableMapper.Map(missingRoyal, AllQualityIds);

            Assert.That(missingRoyalResult.Succeeded, Is.False);
            Assert.That(missingRoyalResult.Errors, Has.Count.EqualTo(1));
            Assert.That(missingRoyalResult.Errors[0], Does.Contain("RoyalFlush").And.Contain("误删"));
        }

        [Test]
        public void Hand12MapsToFlushFiveAsTolerance()
        {
            // 策划将来补行同花五条：HAND_12 容错映射即生效（表补行后自然覆盖白盒占位）
            var rows = FullElevenRows();
            rows.Add(Row("HAND_12", "同花五条", "12", "100", "8", "12", "RARE"));

            var result = HandTypeTableMapper.Map(rows, AllQualityIds);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(12));
            Assert.That(result.Entries[11].HandType, Is.EqualTo(HandType.FlushFive));
        }

        [Test]
        public void EmptyTableFails()
        {
            var result = HandTypeTableMapper.Map(new List<Dictionary<string, string>>(), AllQualityIds);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        [Test]
        public void DisplayOrderDuplicatesWarn()
        {
            var rows = FullElevenRows();
            rows[1][HandTypeTableContract.ColDisplayOrder] = "1"; // HAND_02 与 HAND_01 同显示顺序
            rows[1][HandTypeTableContract.ColScoringCount] = "1"; // 两列同步改，避免额外触发「计分牌数与显示顺序不一致」警告

            var result = HandTypeTableMapper.Map(rows, AllQualityIds);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("显示顺序").And.Contain("重复"));
        }

        [Test]
        public void EntriesAreSortedByDisplayOrder()
        {
            // 行序打乱不影响产物顺序：目录 All 与资产 Inspector 都按显示顺序呈现
            var rows = FullElevenRows();
            rows.Reverse();

            var result = HandTypeTableMapper.Map(rows, AllQualityIds);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Entries.Select(e => e.HandType), Is.EqualTo(new[]
            {
                HandType.HighCard, HandType.Pair, HandType.TwoPair, HandType.ThreeOfAKind, HandType.Straight,
                HandType.Flush, HandType.FullHouse, HandType.FourOfAKind, HandType.StraightFlush, HandType.FlushHouse,
                HandType.RoyalFlush
            }));
        }

        /// <summary>完整 11 行夹具（= 配表「牌型配置」当前内容），供测试按需修改个别行。</summary>
        private static List<Dictionary<string, string>> FullElevenRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("HAND_01", "高牌", "1", "55", "1", "1", "NORMAL"),
                Row("HAND_02", "对子", "2", "48", "2", "2", "NORMAL"),
                Row("HAND_03", "两队", "3", "52", "2.5", "3", "NORMAL"),
                Row("HAND_04", "三条", "4", "57", "3", "4", "NORMAL"),
                Row("HAND_05", "顺子", "5", "60", "4", "5", "NORMAL"),
                Row("HAND_06", "同花", "6", "65", "4", "6", "RARE"),
                Row("HAND_07", "葫芦", "7", "74", "5", "7", "RARE"),
                Row("HAND_08", "四条", "8", "100", "6", "8", "RARE"),
                Row("HAND_09", "同花顺", "9", "95", "10", "9", "RARE"),
                Row("HAND_10", "同花葫芦", "10", "70", "12", "10", "RARE"),
                Row("HAND_11", "皇家同花顺", "11", "100", "12", "11", "RARE")
            };
        }

        /// <summary>构造一行夹具（列与「牌型配置」sheet 表头一致）。</summary>
        private static Dictionary<string, string> Row(
            string handId, string name, string scoringCount, string chips, string multiplier, string order, string quality)
        {
            return new Dictionary<string, string>
            {
                { HandTypeTableContract.ColHandId, handId },
                { HandTypeTableContract.ColName, name },
                { HandTypeTableContract.ColScoringCount, scoringCount },
                { HandTypeTableContract.ColChips, chips },
                { HandTypeTableContract.ColMultiplier, multiplier },
                { HandTypeTableContract.ColDisplayOrder, order },
                { HandTypeTableContract.ColQuality, quality }
            };
        }
    }
}
