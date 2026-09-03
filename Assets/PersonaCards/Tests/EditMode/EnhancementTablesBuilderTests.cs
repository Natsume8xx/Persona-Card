using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Core;
using PersonaCards.Data;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// 三线强化配表翻译器测试（P0-11）：夹具 = 强化 3 资产（PersonaUpRule/SuitUp/HandUp）的真实配表值。
    /// 锁定翻译规则：mainAttrType 映射、百分比 ÷100、Lv.N 提取数字、牌型增量差值、浮点尾巴抹平。
    /// </summary>
    public sealed class EnhancementTablesBuilderTests
    {
        [Test]
        public void RealPersonaRulesMapToEffectKindsAndPrices()
        {
            var result = EnhancementTablesBuilder.Build(EnhancementTestFixtures.RealPersonaRules(), EnhancementTestFixtures.RealSuitUps(), EnhancementTestFixtures.RealHandUps());

            Assert.That(result.Tables.PersonaPerLevelIncrease[PersonaEffectKind.AddChips], Is.EqualTo(10m));
            Assert.That(result.Tables.PersonaPerLevelIncrease[PersonaEffectKind.AddMultiplier], Is.EqualTo(0.3m));
            Assert.That(result.Tables.PersonaPerLevelIncrease[PersonaEffectKind.MultiplyFinal], Is.EqualTo(0.1m)); // +10% → ÷100
            Assert.That(result.Tables.PersonaBasePrice, Is.EqualTo(8));
            Assert.That(result.Tables.PersonaLevelPriceStep, Is.EqualTo(3));
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void RealSuitUpsFillPerLevelChipsPricesAndNames()
        {
            var result = EnhancementTablesBuilder.Build(EnhancementTestFixtures.RealPersonaRules(), EnhancementTestFixtures.RealSuitUps(), EnhancementTestFixtures.RealHandUps());

            Assert.That(result.Tables.SuitChips[Suit.Spades], Is.EqualTo(new[] { 5, 10, 15, 20 }));
            Assert.That(result.Tables.SuitPrices[Suit.Spades], Is.EqualTo(new[] { 8, 11, 14, 17 }));
            Assert.That(result.Tables.SuitNames[Suit.Spades], Is.EqualTo("黑桃"));
            Assert.That(result.Tables.SuitNames[Suit.Clubs], Is.EqualTo("梅花"));
        }

        [Test]
        public void RealHandUpsProduceDeltaAgainstCatalogBase()
        {
            var result = EnhancementTablesBuilder.Build(EnhancementTestFixtures.RealPersonaRules(), EnhancementTestFixtures.RealSuitUps(), EnhancementTestFixtures.RealHandUps());

            // 皇家同花顺：表内绝对底值 110/120/130/140 与 13.2/14.4/15.6/16.8；Lv0 目录值 100/12 → 差值
            Assert.That(result.Tables.HandChipDeltas[HandType.RoyalFlush], Is.EqualTo(new[] { 10, 20, 30, 40 }));
            Assert.That(result.Tables.HandMultDeltas[HandType.RoyalFlush],
                Is.EqualTo(new[] { 1.2m, 2.4m, 3.6m, 4.8m }));
            Assert.That(result.Tables.HandPrices[HandType.RoyalFlush], Is.EqualTo(new[] { 8, 11, 14, 17 }));
            Assert.That(result.Tables.HandNames[HandType.RoyalFlush], Is.EqualTo("皇家同花顺"));
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void HandTargetsFollowTableOrderWithoutDuplicates()
        {
            var result = EnhancementTablesBuilder.Build(EnhancementTestFixtures.RealPersonaRules(), EnhancementTestFixtures.RealSuitUps(), EnhancementTestFixtures.RealHandUps());

            // 表顺序 HAND_01 → HAND_11（每牌型 4 行），去重保序 = 商店轮换顺序
            Assert.That(result.Tables.HandTargets, Is.EqualTo(new[]
            {
                HandType.HighCard, HandType.Pair, HandType.TwoPair, HandType.ThreeOfAKind, HandType.Straight,
                HandType.Flush, HandType.FullHouse, HandType.FourOfAKind, HandType.StraightFlush, HandType.FlushHouse,
                HandType.RoyalFlush
            }));
        }

        [Test]
        public void FloatingPointTailsAreRoundedAway()
        {
            // 真实资产里的浮点尾巴（如 "1.1000000000000001"）：差值必须 Round(6) 抹平
            var handUps = new List<HandUpEntry>
            {
                EnhancementTestFixtures.HandRow("HAND_UP_001", "HAND_01", "高牌", "Lv.1", 56, "1.1000000000000001", 8),
                EnhancementTestFixtures.HandRow("HAND_UP_002", "HAND_01", "高牌", "Lv.2", 57, "1.2000000000000001", 11),
                EnhancementTestFixtures.HandRow("HAND_UP_003", "HAND_01", "高牌", "Lv.3", 58, "1.3000000000000001", 14),
                EnhancementTestFixtures.HandRow("HAND_UP_004", "HAND_01", "高牌", "Lv.4", 59, "1.4000000000000001", 17)
            };

            var result = EnhancementTablesBuilder.Build(EnhancementTestFixtures.RealPersonaRules(), EnhancementTestFixtures.RealSuitUps(), handUps);

            Assert.That(result.Tables.HandChipDeltas[HandType.HighCard], Is.EqualTo(new[] { 1, 2, 3, 4 })); // 目录高牌 55
            Assert.That(result.Tables.HandMultDeltas[HandType.HighCard], Is.EqualTo(new[] { 0.1m, 0.2m, 0.3m, 0.4m }));
        }

        [Test]
        public void InvalidRowsAreSkippedWithWarnings()
        {
            var suitUps = new List<SuitUpEntry>(EnhancementTestFixtures.RealSuitUps());
            suitUps.Add(new SuitUpEntry { suitUpId = "SUIT_UP_BAD_1", suitId = "SUIT_099", suitName = "神秘花色", level = "Lv.1", extraChips = 5, price = 8 });
            suitUps.Add(new SuitUpEntry { suitUpId = "SUIT_UP_BAD_2", suitId = "SUIT_001", suitName = "黑桃", level = "Lv.9", extraChips = 99, price = 99 });

            var result = EnhancementTablesBuilder.Build(EnhancementTestFixtures.RealPersonaRules(), suitUps, EnhancementTestFixtures.RealHandUps());

            Assert.That(result.Warnings, Has.Count.EqualTo(2)); // 无效花色_ID + 等级超出 1~4
            Assert.That(result.Warnings[0], Does.Contain("SUIT_099"));
            Assert.That(result.Warnings[1], Does.Contain("Lv.9"));
            Assert.That(result.Tables.SuitChips[Suit.Spades], Is.EqualTo(new[] { 5, 10, 15, 20 })); // 合法行不受影响
        }

        [Test]
        public void NullTablesYieldEmptyTablesWithWarnings()
        {
            var result = EnhancementTablesBuilder.Build(null, null, null);

            Assert.That(result.Tables.HasContent, Is.False);
            Assert.That(result.Warnings, Has.Count.EqualTo(3)); // 三线各一条空表警告
        }

        [Test]
        public void HandRowBelowCatalogBaseWarnsButStillLoads()
        {
            // 表值低于 Lv0（数据疑似异常）→ 警告但仍装载（运行时兜底不 fail-fast）
            var handUps = new List<HandUpEntry>
            {
                EnhancementTestFixtures.HandRow("HAND_UP_001", "HAND_01", "高牌", "Lv.1", 50, "0.9", 8)
            };

            var result = EnhancementTablesBuilder.Build(EnhancementTestFixtures.RealPersonaRules(), EnhancementTestFixtures.RealSuitUps(), handUps);

            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("低于 Lv0"));
            Assert.That(result.Tables.HandChipDeltas[HandType.HighCard][0], Is.EqualTo(-5));
        }

        [Test]
        public void MismatchedPricesWarnAndUseFirstRow()
        {
            var personaRules = new List<PersonaUpRuleEntry>(EnhancementTestFixtures.RealPersonaRules());
            personaRules[1].basePrice = 99;

            var result = EnhancementTablesBuilder.Build(personaRules, EnhancementTestFixtures.RealSuitUps(), EnhancementTestFixtures.RealHandUps());

            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("基础价格").And.Contain("99"));
            Assert.That(result.Tables.PersonaBasePrice, Is.EqualTo(8)); // 取第一条
        }

    }
}
