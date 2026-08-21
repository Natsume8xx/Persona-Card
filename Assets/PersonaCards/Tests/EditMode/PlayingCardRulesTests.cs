using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.Data;
using UnityEngine;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>卡牌规则门面测试（P0-1D）：Configure 注入/回落语义、行为零差异锁。门面是静态类，[TearDown] 必须回落白盒防测试间泄漏。</summary>
    public sealed class PlayingCardRulesTests
    {
        [TearDown]
        public void TearDown()
        {
            PlayingCardRules.Configure(null); // 每个测试结束后回到白盒，防静态状态泄漏到其他测试
        }

        [Test]
        public void ConfigureWithValidEntriesOverridesFallback()
        {
            // 只改黑桃A：其余 51 张必须回落白盒（配表逐卡配置，改动不扩散）
            var entries = new List<CardConfigEntry>
            {
                new CardConfigEntry("CARD_001", "黑桃A", CardKind.Hand, Suit.Spades, Rank.Ace, CardParamType.Chips, 12)
            };

            PlayingCardRules.Configure(entries);

            Assert.That(PlayingCardRules.GetFaceChipValue(Suit.Spades, Rank.Ace), Is.EqualTo(12));
            Assert.That(PlayingCardRules.GetFaceChipValue(Suit.Hearts, Rank.Ace), Is.EqualTo(11)); // 红桃A 白盒
            Assert.That(PlayingCardRules.GetFaceChipValue(Suit.Spades, Rank.King), Is.EqualTo(10)); // 黑桃K 白盒
            Assert.That(PlayingCardRules.LastConfiguredSummary, Does.Contain("1 张来自配置").And.Contain("51 张回落白盒"));
        }

        [Test]
        public void ConfigureWithPartialEntriesFillsFallback()
        {
            // 只注入 1 条：52 张字典必须始终齐全（白盒起步），GetCardId 返回表序绑定 ID
            var entries = new List<CardConfigEntry>
            {
                new CardConfigEntry("CARD_027", "梅花A", CardKind.Hand, Suit.Clubs, Rank.Ace, CardParamType.Chips, 11)
            };

            PlayingCardRules.Configure(entries);

            Assert.That(PlayingCardRules.GetFaceChipValue(Suit.Clubs, Rank.King), Is.EqualTo(10)); // 梅花K 白盒补齐
            Assert.That(PlayingCardRules.GetCardId(Suit.Clubs, Rank.King), Is.EqualTo("CARD_039")); // 表序：梅花K = 第 39 张
            Assert.That(PlayingCardRules.GetCardId(Suit.Diamonds, Rank.Ace), Is.EqualTo("CARD_040")); // 方块A = 第 40 张
            Assert.That(PlayingCardRules.LastConfiguredSummary, Does.Contain("1 张来自配置").And.Contain("51 张回落白盒"));
        }

        [Test]
        public void ConfigureWithDuplicateSuitRankFallsBackToWhitelist()
        {
            var entries = new List<CardConfigEntry>
            {
                new CardConfigEntry("CARD_001", "黑桃A", CardKind.Hand, Suit.Spades, Rank.Ace, CardParamType.Chips, 11),
                new CardConfigEntry("CARD_999", "重复黑桃A", CardKind.Hand, Suit.Spades, Rank.Ace, CardParamType.Chips, 99)
            };

            PlayingCardRules.Configure(entries);

            Assert.That(PlayingCardRules.GetFaceChipValue(Suit.Spades, Rank.Ace), Is.EqualTo(11)); // 白盒值
            Assert.That(PlayingCardRules.LastConfiguredSummary, Is.Empty); // 回落白盒时摘要清空
        }

        [Test]
        public void ConfigureWithNullEntryFallsBackToWhitelist()
        {
            var entries = new List<CardConfigEntry> { null };

            PlayingCardRules.Configure(entries);

            Assert.That(PlayingCardRules.GetFaceChipValue(Suit.Spades, Rank.Ace), Is.EqualTo(11)); // 白盒值
            Assert.That(PlayingCardRules.LastConfiguredSummary, Is.Empty);
        }

        [Test]
        public void ConfigureWithEmptyOrNullFallsBack()
        {
            PlayingCardRules.Configure(new List<CardConfigEntry>());
            Assert.That(PlayingCardRules.GetFaceChipValue(Suit.Hearts, Rank.Five), Is.EqualTo(5));

            PlayingCardRules.Configure(null);
            Assert.That(PlayingCardRules.GetFaceChipValue(Suit.Hearts, Rank.Five), Is.EqualTo(5));
            Assert.That(PlayingCardRules.LastConfiguredSummary, Is.Empty);
        }

        [Test]
        public void FallbackMatchesOriginalRules()
        {
            // P0-1D 行为零差异锁：白盒 52 张的筹码 = 旧 PlayingCardRules.GetFaceChipValue 公式（A=11、J/Q/K=10、其余=点数）
            PlayingCardRules.Configure(null);
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                for (var rankValue = (int)Rank.Two; rankValue <= (int)Rank.Ace; rankValue++)
                {
                    var rank = (Rank)rankValue;
                    var expected = rank switch
                    {
                        Rank.Jack => 10,
                        Rank.Queen => 10,
                        Rank.King => 10,
                        Rank.Ace => 11,
                        _ => (int)rank
                    };
                    Assert.That(PlayingCardRules.GetFaceChipValue(suit, rank), Is.EqualTo(expected),
                        $"白盒 {suit}/{rank} 筹码与旧公式不符");
                    Assert.That(PlayingCardRules.GetCardId(suit, rank), Does.StartWith("CARD_"),
                        $"白盒 {suit}/{rank} 缺少绑定 ID");
                }
            }
        }

        [Test]
        public void InvalidEnumThrows()
        {
            PlayingCardRules.Configure(null);
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayingCardRules.GetFaceChipValue((Suit)99, Rank.Ace));
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayingCardRules.GetFaceChipValue(Suit.Spades, (Rank)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayingCardRules.GetCardId((Suit)99, Rank.Ace));
        }

        [Test]
        public void AssetFallbackEntriesMatchCatalogFallback()
        {
            // 防漂移交叉校验：资产白盒工厂与门面白盒回落必须逐字段一致（数值唯一源 CardConfigEntry.CreateFallbackList）
            var asset = ScriptableObject.CreateInstance<CardConfigAsset>();
            asset.entries = CardConfigAsset.CreateFallbackEntries();
            Assert.That(asset.Validate(out var error), Is.True, error);

            PlayingCardRules.Configure(asset.BuildEntries());
            var configured = CardConfigEntry.CreateFallbackList();
            PlayingCardRules.Configure(null); // 门面白盒

            Assert.That(configured, Has.Count.EqualTo(52));
            foreach (var entry in configured)
            {
                Assert.That(PlayingCardRules.GetFaceChipValue(entry.Suit, entry.Rank), Is.EqualTo(entry.ParamValue),
                    $"{entry.CardId} 筹码漂移");
                Assert.That(PlayingCardRules.GetCardId(entry.Suit, entry.Rank), Is.EqualTo(entry.CardId),
                    $"{entry.CardId} 绑定 ID 漂移");
            }

            ScriptableObject.DestroyImmediate(asset);
        }
    }
}
