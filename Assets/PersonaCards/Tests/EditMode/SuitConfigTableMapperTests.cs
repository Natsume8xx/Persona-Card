using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>花色配置配表映射器测试（P0-11）：用行字典夹具（= XlsxTableReader 的输出形状）验证契约解析，不依赖真实 xlsx 文件。</summary>
    public sealed class SuitConfigTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToFourEntries()
        {
            // 与 Docs/人格牌.xlsx「花色配置」sheet 当前 4 行一致的夹具
            var rows = FullFourRows();

            var result = SuitConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Entries, Has.Count.EqualTo(4));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Entries.Select(entry => entry.Suit), Is.EqualTo(new[]
            {
                Suit.Spades, Suit.Hearts, Suit.Clubs, Suit.Diamonds
            }));
            Assert.That(result.Entries.Select(entry => entry.DisplayName), Is.EqualTo(new[]
            {
                "黑桃", "红桃", "梅花", "方块"
            }));
        }

        [Test]
        public void UnknownSuitIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SUIT_099", "神秘花色")
            };

            var result = SuitConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Entries, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("SUIT_001 ~ SUIT_004"));
        }

        [Test]
        public void DuplicateOrEmptySuitIdFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SUIT_001", "黑桃"),
                Row("SUIT_001", "红桃"),
                Row("", "梅花")
            };

            var result = SuitConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 重复 + 空：全收集不 fail-fast
            Assert.That(result.Errors[0], Does.Contain("重复"));
            Assert.That(result.Errors[1], Does.Contain("为空"));
        }

        [Test]
        public void EmptyNameFails()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row("SUIT_001", "黑桃"),
                Row("SUIT_002", ""),
                Row("SUIT_003", "梅花"),
                Row("SUIT_004", "方块")
            };

            var result = SuitConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("花色名称"));
        }

        [Test]
        public void MissingSuitRowFails()
        {
            // 4 花色必须齐全（防策划误删行）：缺 SUIT_002 红桃一行必须报错
            var rows = new List<Dictionary<string, string>>
            {
                Row("SUIT_001", "黑桃"),
                Row("SUIT_003", "梅花"),
                Row("SUIT_004", "方块")
            };

            var result = SuitConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Hearts").And.Contain("误删"));
        }

        [Test]
        public void EmptyTableFails()
        {
            var result = SuitConfigTableMapper.Map(new List<Dictionary<string, string>>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("没有任何数据行"));
        }

        [Test]
        public void EntriesFollowSuitIdOrderRegardlessOfRowOrder()
        {
            // 行序打乱不影响产物顺序：产物按花色_ID 升序（黑桃/红桃/梅花/方块）
            var rows = FullFourRows();
            rows.Reverse();

            var result = SuitConfigTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Entries.Select(entry => entry.Suit), Is.EqualTo(new[]
            {
                Suit.Spades, Suit.Hearts, Suit.Clubs, Suit.Diamonds
            }));
        }

        /// <summary>完整 4 行夹具（= 配表「花色配置」当前内容），供测试按需修改个别行。</summary>
        private static List<Dictionary<string, string>> FullFourRows()
        {
            return new List<Dictionary<string, string>>
            {
                Row("SUIT_001", "黑桃"),
                Row("SUIT_002", "红桃"),
                Row("SUIT_003", "梅花"),
                Row("SUIT_004", "方块")
            };
        }

        /// <summary>构造一行夹具（列与「花色配置」sheet 表头一致）。</summary>
        private static Dictionary<string, string> Row(string suitId, string name)
        {
            return new Dictionary<string, string>
            {
                { SuitConfigTableContract.ColSuitId, suitId },
                { SuitConfigTableContract.ColName, name }
            };
        }
    }
}
