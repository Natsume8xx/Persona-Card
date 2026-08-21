using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>卡牌配置配表映射器测试：用行字典夹具（= XlsxTableReader 的输出形状）验证契约解析，不依赖真实 xlsx 文件。</summary>
    public sealed class CardConfigTableMapperTests
    {
        /// <summary>与配表一致的图片绑定 ID 集合（CARD_001~052 均在图片配置「绑定ID」列中）。</summary>
        private static ICollection<string> AllCardIds()
        {
            var ids = new HashSet<string>();
            for (var index = 1; index <= 52; index++)
                ids.Add($"CARD_{index:D3}");
            return ids;
        }

        [Test]
        public void RealTableFixtureMapsToFiftyTwoEntries()
        {
            // 与 Docs/人格牌.xlsx「卡牌配置」sheet 当前 52 行一致的夹具（表序：黑桃→红桃→梅花→方块 × A、2~10、J、Q、K）
            var rows = FullFiftyTwoRows();

            var result = CardConfigTableMapper.Map(rows, AllCardIds());

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(52));
            Assert.That(result.Warnings, Is.Empty); // CARD_001~052 全部在图片配置绑定 ID 中，不应有任何警告

            for (var index = 0; index < result.Entries.Count; index++)
                Assert.That(result.Entries[index].CardId, Is.EqualTo($"CARD_{index + 1:D3}"), $"条目 {index} 排序不符");

            // 抽验各花色关键牌：A=11、J/Q/K=10、面值牌=点数
            AssertEntry(result, 0, Suit.Spades, Rank.Ace, 11, "黑桃A");
            AssertEntry(result, 9, Suit.Spades, Rank.Ten, 10, "黑桃10");
            AssertEntry(result, 10, Suit.Spades, Rank.Jack, 10, "黑桃J");
            AssertEntry(result, 17, Suit.Hearts, Rank.Five, 5, "红桃5");
            AssertEntry(result, 26, Suit.Clubs, Rank.Ace, 11, "梅花A");
            AssertEntry(result, 38, Suit.Clubs, Rank.King, 10, "梅花K");
            AssertEntry(result, 39, Suit.Diamonds, Rank.Ace, 11, "方块A");
            AssertEntry(result, 51, Suit.Diamonds, Rank.King, 10, "方块K");
        }

        [Test]
        public void UnknownSuitFails()
        {
            var rows = FullFiftyTwoRows();
            rows[0][CardConfigTableContract.ColSuit] = "星星";

            var result = CardConfigTableMapper.Map(rows, AllCardIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("花色").And.Contain("黑桃/红桃/梅花/方块"));
        }

        [Test]
        public void UnknownRankFails()
        {
            var rows = FullFiftyTwoRows();
            rows[0][CardConfigTableContract.ColRank] = "15";

            var result = CardConfigTableMapper.Map(rows, AllCardIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("点数").And.Contain("A/2~10/J/Q/K"));
        }

        [Test]
        public void DuplicateOrEmptyCardIdFails()
        {
            var rows = FullFiftyTwoRows();
            rows[1][CardConfigTableContract.ColCardId] = "CARD_001"; // 与首行重复
            rows[2][CardConfigTableContract.ColCardId] = "";         // 空

            var result = CardConfigTableMapper.Map(rows, AllCardIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void DuplicateSuitRankFails()
        {
            // 行为键 = (花色,点数)：ID 不同但花色点数相同 = 错误
            var rows = FullFiftyTwoRows();
            rows[1][CardConfigTableContract.ColSuit] = "黑桃";
            rows[1][CardConfigTableContract.ColRank] = "A";

            var result = CardConfigTableMapper.Map(rows, AllCardIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("重复").And.Contain("行为键"));
        }

        [Test]
        public void MissingCardFails()
        {
            // 52 组合齐全检查：删一行（黑桃K）→ 错误防误删；多删的行也全收集
            var rows = FullFiftyTwoRows();
            rows.RemoveAt(12); // CARD_013 黑桃K
            rows.RemoveAt(40); // CARD_042 方块3（删两行验证多错误全收集）

            var result = CardConfigTableMapper.Map(rows, AllCardIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            // 齐全检查按枚举遍历序收集，不断言错误顺序
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("Spades") && e.Contains("King") && e.Contains("误删")));
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("Diamonds") && e.Contains("Three") && e.Contains("误删")));
        }

        [Test]
        public void UnknownCardKindFails()
        {
            var rows = FullFiftyTwoRows();
            rows[0][CardConfigTableContract.ColCardKind] = "人格牌";

            var result = CardConfigTableMapper.Map(rows, AllCardIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("卡牌类型").And.Contain("手牌"));
        }

        [Test]
        public void UnknownParamTypeFails()
        {
            var rows = FullFiftyTwoRows();
            rows[0][CardConfigTableContract.ColParamType] = "倍率";

            var result = CardConfigTableMapper.Map(rows, AllCardIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("参数类型").And.Contain("筹码"));
        }

        [Test]
        public void InvalidParamValueFails()
        {
            var rows = FullFiftyTwoRows();
            rows[0][CardConfigTableContract.ColParamValue] = "abc";
            rows[1][CardConfigTableContract.ColParamValue] = "-5";

            var result = CardConfigTableMapper.Map(rows, AllCardIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 多行错误全收集
            Assert.That(result.Errors[0], Does.Contain("参数1"));
            Assert.That(result.Errors[1], Does.Contain("参数1"));
        }

        [Test]
        public void EmptyCardNameFails()
        {
            var rows = FullFiftyTwoRows();
            rows[0][CardConfigTableContract.ColName] = "";

            var result = CardConfigTableMapper.Map(rows, AllCardIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("卡牌名称"));
        }

        [Test]
        public void CardIdMissingFromImagesWarnsButSucceeds()
        {
            // 策划改 ID 未同步图片配置：警告容错（程序已存值，美术接入期不阻塞）
            var rows = FullFiftyTwoRows();
            rows[0][CardConfigTableContract.ColCardId] = "CARD_999";

            var result = CardConfigTableMapper.Map(rows, AllCardIds());

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(52));
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("CARD_999").And.Contain("不在图片配置"));
            Assert.That(result.Entries[0].CardId, Is.EqualTo("CARD_002")); // 按 ID 升序，CARD_999 在末尾
            Assert.That(result.Entries[51].CardId, Is.EqualTo("CARD_999"));
        }

        [Test]
        public void EmptyTableFails()
        {
            var result = CardConfigTableMapper.Map(new List<Dictionary<string, string>>(), AllCardIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        [Test]
        public void EntriesAreSortedByCardId()
        {
            // 行序打乱不影响产物顺序：资产 Inspector 与日志按卡牌_ID 呈现
            var rows = FullFiftyTwoRows();
            rows.Reverse();

            var result = CardConfigTableMapper.Map(rows, AllCardIds());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Entries.Select(e => e.CardId), Is.EqualTo(
                Enumerable.Range(1, 52).Select(index => $"CARD_{index:D3}")));
        }

        /// <summary>断言某条目（按卡牌_ID 升序后的索引）。</summary>
        private static void AssertEntry(CardMappingResult result, int index, Suit suit, Rank rank, int chips, string name)
        {
            var entry = result.Entries[index];
            Assert.That(entry.Suit, Is.EqualTo(suit), $"条目 {index} 花色不符");
            Assert.That(entry.Rank, Is.EqualTo(rank), $"条目 {index} 点数不符");
            Assert.That(entry.ParamValue, Is.EqualTo(chips), $"条目 {index} 筹码不符");
            Assert.That(entry.DisplayName, Is.EqualTo(name), $"条目 {index} 名称不符");
            Assert.That(entry.CardKind, Is.EqualTo(CardKind.Hand), $"条目 {index} 类型不符");
            Assert.That(entry.ParamType, Is.EqualTo(CardParamType.Chips), $"条目 {index} 参数类型不符");
        }

        /// <summary>完整 52 行夹具（= 配表「卡牌配置」当前内容，表序：黑桃→红桃→梅花→方块 × A、2~10、J、Q、K），供测试按需修改个别行。</summary>
        private static List<Dictionary<string, string>> FullFiftyTwoRows()
        {
            var suits = new[] { "黑桃", "红桃", "梅花", "方块" };
            var rankTexts = new[] { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
            var chipValues = new[] { 11, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 10, 10 };
            var rows = new List<Dictionary<string, string>>();
            var index = 1;
            foreach (var suit in suits)
            {
                for (var rankIndex = 0; rankIndex < rankTexts.Length; rankIndex++)
                {
                    rows.Add(Row($"CARD_{index++:D3}", $"{suit}{rankTexts[rankIndex]}", "手牌", suit,
                        rankTexts[rankIndex], "筹码", chipValues[rankIndex].ToString()));
                }
            }

            return rows;
        }

        /// <summary>构造一行夹具（列与「卡牌配置」sheet 表头一致）。</summary>
        private static Dictionary<string, string> Row(
            string cardId, string name, string cardKind, string suit, string rank, string paramType, string paramValue)
        {
            return new Dictionary<string, string>
            {
                { CardConfigTableContract.ColCardId, cardId },
                { CardConfigTableContract.ColName, name },
                { CardConfigTableContract.ColCardKind, cardKind },
                { CardConfigTableContract.ColSuit, suit },
                { CardConfigTableContract.ColRank, rank },
                { CardConfigTableContract.ColParamType, paramType },
                { CardConfigTableContract.ColParamValue, paramValue }
            };
        }
    }
}
