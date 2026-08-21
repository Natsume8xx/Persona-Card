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
        /// <summary>与配表一致的图片绑定 ID 集合（CARD_053~062 对应 HAND_01~10）。</summary>
        private static readonly ICollection<string> AllCardIds = new HashSet<string>
        {
            "CARD_053", "CARD_054", "CARD_055", "CARD_056", "CARD_057",
            "CARD_058", "CARD_059", "CARD_060", "CARD_061", "CARD_062"
        };

        [Test]
        public void RealTableFixtureMapsToTenEntries()
        {
            // 与 Docs/人格牌.xlsx「牌型配置」sheet 当前 10 行一致的夹具（含两队 2.5 倍率）
            var rows = new List<Dictionary<string, string>>
            {
                Row("HAND_01", "高牌", "1", "55", "1", "1", "CARD_053"),
                Row("HAND_02", "对子", "2", "48", "2", "2", "CARD_054"),
                Row("HAND_03", "两队", "3", "52", "2.5", "3", "CARD_055"),
                Row("HAND_04", "三条", "4", "57", "3", "4", "CARD_056"),
                Row("HAND_05", "顺子", "5", "60", "4", "5", "CARD_057"),
                Row("HAND_06", "同花", "6", "65", "4", "6", "CARD_058"),
                Row("HAND_07", "葫芦", "7", "74", "5", "7", "CARD_059"),
                Row("HAND_08", "四条", "8", "100", "6", "8", "CARD_060"),
                Row("HAND_09", "同花顺", "9", "95", "10", "9", "CARD_061"),
                Row("HAND_10", "同花葫芦", "10", "70", "12", "10", "CARD_062")
            };

            var result = HandTypeTableMapper.Map(rows, AllCardIds);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(10));
            Assert.That(result.Warnings, Is.Empty); // 契约值全部一致，不应有任何警告

            var expectedTypes = new[]
            {
                HandType.HighCard, HandType.Pair, HandType.TwoPair, HandType.ThreeOfAKind, HandType.Straight,
                HandType.Flush, HandType.FullHouse, HandType.FourOfAKind, HandType.StraightFlush, HandType.FlushHouse
            };
            var expectedChips = new[] { 55, 48, 52, 57, 60, 65, 74, 100, 95, 70 };
            var expectedMultipliers = new decimal[] { 1m, 2m, 2.5m, 3m, 4m, 4m, 5m, 6m, 10m, 12m };
            for (var index = 0; index < result.Entries.Count; index++)
            {
                var entry = result.Entries[index];
                Assert.That(entry.HandType, Is.EqualTo(expectedTypes[index]), $"条目 {index} 牌型不符");
                Assert.That(entry.BaseChips, Is.EqualTo(expectedChips[index]), $"条目 {index} 筹码不符");
                Assert.That(entry.BaseMultiplier, Is.EqualTo(expectedMultipliers[index]), $"条目 {index} 倍率不符");
                Assert.That(entry.DisplayOrder, Is.EqualTo(index + 1), $"条目 {index} 显示顺序不符");
                Assert.That(entry.CardId, Is.EqualTo($"CARD_{index + 53:D3}"), $"条目 {index} card_id 不符");
            }
            // 两队 2.5 是 decimal 精确值（配表小数位 ≤2，解析即定型）
            Assert.That(result.Entries[2].BaseMultiplier, Is.EqualTo(2.5m));
        }

        [Test]
        public void UnknownHandIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("HAND_99", "神秘牌型", "1", "55", "1", "1", "CARD_053")
            };

            var result = HandTypeTableMapper.Map(rows, AllCardIds);

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
                Row("HAND_01", "高牌", "1", "55", "1", "1", "CARD_053"),
                Row("HAND_01", "对子", "2", "48", "2", "2", "CARD_054"),
                Row("", "顺子", "5", "60", "4", "5", "CARD_057")
            };

            var result = HandTypeTableMapper.Map(rows, AllCardIds);

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
                Row("HAND_01", "高牌", "1", "-5", "1", "1", "CARD_053"),
                Row("HAND_02", "对子", "2", "abc", "2", "2", "CARD_054"),
                Row("HAND_03", "两队", "3", "52", "0.5", "3", "CARD_055"),
                Row("HAND_04", "三条", "4", "57", "NaN", "4", "CARD_056")
            };

            var result = HandTypeTableMapper.Map(rows, AllCardIds);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(4)); // 4 行错误全收集
            Assert.That(result.Errors[0], Does.Contain("基础筹码"));
            Assert.That(result.Errors[1], Does.Contain("基础筹码"));
            Assert.That(result.Errors[2], Does.Contain("基础倍率"));
            Assert.That(result.Errors[3], Does.Contain("基础倍率"));
        }

        [Test]
        public void ScoringCountMismatchWarnsAndIsNotImported()
        {
            // A4 拍板：「计分牌数」列不导入，与「显示顺序」不一致时警告
            var rows = FullTenRows();
            rows[0][HandTypeTableContract.ColScoringCount] = "99";

            var result = HandTypeTableMapper.Map(rows, AllCardIds);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Entries, Has.Count.EqualTo(10));
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("计分牌数").And.Contain("显示顺序"));
        }

        [Test]
        public void CardIdMissingOrUnsyncedWarnsButSucceeds()
        {
            // A2 拍板：card_id 策划会改，程序警告容错
            var rows = FullTenRows();
            rows[0][HandTypeTableContract.ColCardId] = "";
            rows[1][HandTypeTableContract.ColCardId] = "CARD_999";

            var result = HandTypeTableMapper.Map(rows, AllCardIds);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Entries, Has.Count.EqualTo(10));
            Assert.That(result.Warnings, Has.Count.EqualTo(2));
            Assert.That(result.Warnings[0], Does.Contain("为空"));
            Assert.That(result.Warnings[1], Does.Contain("不在图片配置"));
        }

        [Test]
        public void MissingFiveOfAKindRowsAreToleratedButCoreTypesMustBeComplete()
        {
            // 已拍板容错：五条/同花五条不在表中，目录 Configure 白盒补齐，Mapper 不报错
            var fullTen = FullTenRows();
            var result = HandTypeTableMapper.Map(fullTen, AllCardIds);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Errors, Is.Empty);

            // 但核心牌型误删一行（如 HAND_05 顺子）必须报错，防止白盒兜底掩盖配表缺行
            fullTen.RemoveAt(4);
            var missing = HandTypeTableMapper.Map(fullTen, AllCardIds);

            Assert.That(missing.Succeeded, Is.False);
            Assert.That(missing.Errors, Has.Count.EqualTo(1));
            Assert.That(missing.Errors[0], Does.Contain("Straight").And.Contain("误删"));
        }

        [Test]
        public void Hand11And12MapToSpecialTypes()
        {
            // 策划将来补行五条/同花五条：HAND_11/HAND_12 即生效（表补行后自然覆盖白盒占位）
            var rows = FullTenRows();
            rows.Add(Row("HAND_11", "五条", "11", "100", "8", "11", "CARD_063"));
            rows.Add(Row("HAND_12", "同花五条", "12", "100", "8", "12", "CARD_064"));
            var bindingIds = new HashSet<string>(AllCardIds) { "CARD_063", "CARD_064" };

            var result = HandTypeTableMapper.Map(rows, bindingIds);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(12));
            Assert.That(result.Entries[10].HandType, Is.EqualTo(HandType.FiveOfAKind));
            Assert.That(result.Entries[11].HandType, Is.EqualTo(HandType.FlushFive));
        }

        [Test]
        public void EmptyTableFails()
        {
            var result = HandTypeTableMapper.Map(new List<Dictionary<string, string>>(), AllCardIds);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        [Test]
        public void DisplayOrderDuplicatesWarn()
        {
            var rows = FullTenRows();
            rows[1][HandTypeTableContract.ColDisplayOrder] = "1"; // HAND_02 与 HAND_01 同显示顺序
            rows[1][HandTypeTableContract.ColScoringCount] = "1"; // 两列同步改，避免额外触发「计分牌数与显示顺序不一致」警告

            var result = HandTypeTableMapper.Map(rows, AllCardIds);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("显示顺序").And.Contain("重复"));
        }

        [Test]
        public void EntriesAreSortedByDisplayOrder()
        {
            // 行序打乱不影响产物顺序：目录 All 与资产 Inspector 都按显示顺序呈现
            var rows = FullTenRows();
            rows.Reverse();

            var result = HandTypeTableMapper.Map(rows, AllCardIds);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Entries.Select(e => e.HandType), Is.EqualTo(new[]
            {
                HandType.HighCard, HandType.Pair, HandType.TwoPair, HandType.ThreeOfAKind, HandType.Straight,
                HandType.Flush, HandType.FullHouse, HandType.FourOfAKind, HandType.StraightFlush, HandType.FlushHouse
            }));
        }

        /// <summary>完整 10 行夹具（= 配表「牌型配置」当前内容），供测试按需修改个别行。</summary>
        private static List<Dictionary<string, string>> FullTenRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("HAND_01", "高牌", "1", "55", "1", "1", "CARD_053"),
                Row("HAND_02", "对子", "2", "48", "2", "2", "CARD_054"),
                Row("HAND_03", "两队", "3", "52", "2.5", "3", "CARD_055"),
                Row("HAND_04", "三条", "4", "57", "3", "4", "CARD_056"),
                Row("HAND_05", "顺子", "5", "60", "4", "5", "CARD_057"),
                Row("HAND_06", "同花", "6", "65", "4", "6", "CARD_058"),
                Row("HAND_07", "葫芦", "7", "74", "5", "7", "CARD_059"),
                Row("HAND_08", "四条", "8", "100", "6", "8", "CARD_060"),
                Row("HAND_09", "同花顺", "9", "95", "10", "9", "CARD_061"),
                Row("HAND_10", "同花葫芦", "10", "70", "12", "10", "CARD_062")
            };
        }

        /// <summary>构造一行夹具（列与「牌型配置」sheet 表头一致）。</summary>
        private static Dictionary<string, string> Row(
            string handId, string name, string scoringCount, string chips, string multiplier, string order, string cardId)
        {
            return new Dictionary<string, string>
            {
                { HandTypeTableContract.ColHandId, handId },
                { HandTypeTableContract.ColName, name },
                { HandTypeTableContract.ColScoringCount, scoringCount },
                { HandTypeTableContract.ColChips, chips },
                { HandTypeTableContract.ColMultiplier, multiplier },
                { HandTypeTableContract.ColDisplayOrder, order },
                { HandTypeTableContract.ColCardId, cardId }
            };
        }
    }
}
